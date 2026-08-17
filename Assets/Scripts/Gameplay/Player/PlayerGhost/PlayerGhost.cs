using Unity.Cinemachine;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using static FirstPersonController;

namespace Unity.MP_FPS
{
    public partial class PlayerGhost : GhostMonoBehaviour, IUpdateClient, IUpdateServer
    {
        [field: SerializeField] public AssetReferenceGameObject ProjectilePrefabAR { get; private set; }
        [SerializeField] private Vector3 m_CameraRotation;
        [SerializeField] private GameObject m_OwnerVisuals;
        [SerializeField] private GameObject m_OtherPlayerVisuals;
        [SerializeField] private SoundDef m_SpawnSFX;
        [field: SerializeField] public Transform CameraTarget { get; private set; }
        [field: SerializeField] public Transform ReticlePoint { get; private set; }
        [field: SerializeField] public Transform ShotOrigin { get; private set; }
        [field: FormerlySerializedAs("<VisualShotOrigin>k__BackingField")] [field: SerializeField] 
        public Transform VisualShotOrigin1P { get; private set; }
        [field: SerializeField] public Transform VisualShotOrigin3P { get; private set; }

        [Header("Manual Aiming Setup")]
        [SerializeField] private Animator m_Animator3P;
        private static readonly int AimPitchHash = Animator.StringToHash("AimPitch");
        
        public int PlayerIndex { get; private set; }
        public int InputUserId { get; set; } = -1;
        public PlayerInput ServerMovementInput { get; set; }
        public ControllerConsts ControllerConsts { get; private set; }

        #region Cached Player Components
        private FirstPersonController m_Controller;
        public FirstPersonController Controller => m_Controller;

        #endregion

        [field: SerializeField] public GameObject MainCameraPrefab { get; private set; }

        private Camera m_PlayerCamera;
        private Animator _animatorCharacter;
        private Vector3 m_ReticleVector;

        private float m_HeadBobTimer = 0f;
        private float m_CurrentCameraRoll = 0f;
        [SerializeField, Range(60f, 120f), Tooltip("Horizontal FOV in degrees (Call of Duty standard, e.g. 90, 105, 120)")]
        private float m_HorizontalFOV = 90f;
        private float m_BaseFOV = 59f;

        // Viewmodel Sway, Harmonic Recoil Springs & ADS
        private Vector3 m_InitialOwnerVisualsPos;
        private Quaternion m_InitialOwnerVisualsRot = Quaternion.identity;
        private Vector3 m_CurrentSwayPos;
        private Quaternion m_CurrentSwayRot = Quaternion.identity;
        private Vector3 m_RecoilPos;
        private Vector3 m_RecoilRot;
        private uint _lastProcessedClientShotTick = 0;
        private float m_CurrentADSFactor = 0f;

        private CinemachineTargetGroup m_TargetGroup;
        private CinemachinePositionComposer m_PositionComposer;
        private CinemachineCamera m_CinemachineCamera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            s_NextPredictionId = 1;
        }
        
        private static uint s_NextPredictionId = 1;

        public static uint GetNextPredictionID()
        {
            return s_NextPredictionId++;
        }

        public Camera GetPlayerCamera()
        {
            return m_PlayerCamera;
        }

        public CinemachineCamera GetPlayerCinemachineCamera()
        {
            return m_CinemachineCamera;
        }

        public CinemachinePositionComposer GetPositionComposer()
        {
            return m_PositionComposer;
        }

        public struct PlayerData : IComponentData
        {
            [GhostField] public FixedString128Bytes Name;
            public Entity ViewEntity;
            public Entity ControlledEntity;
        }

        public void Awake()
        {
            GetRequiredComponent(out m_Controller);
            m_ReticleVector = ReticlePoint.localPosition;
        }
        
        private void LateUpdate()
        {
            // This logic is for visual clients only
            if ((Role != MultiplayerRole.ClientProxy && Role != MultiplayerRole.ClientOwned) || m_Animator3P == null)
            {
                return;
            }

            var predictedPlayerGhost = ReadGhostComponentData<PredictedPlayerGhost>();
            var controllerState = predictedPlayerGhost.ControllerState;
            
            // matches vertical angle value in ClientInputReaderSystem
            float normalizedPitch = controllerState.PitchDegrees / 85f;
            
            m_Animator3P.SetFloat(AimPitchHash, normalizedPitch);
        }

        public override void OnGhostLinked()
        {
            bool isClientOwned = (Role == MultiplayerRole.ClientOwned);
            m_OwnerVisuals.SetActive(isClientOwned);
            m_OtherPlayerVisuals.SetActive(!isClientOwned);

            if (m_OwnerVisuals != null)
            {
                m_InitialOwnerVisualsPos = m_OwnerVisuals.transform.localPosition;
                m_InitialOwnerVisualsRot = m_OwnerVisuals.transform.localRotation;
            }

            if (Role != MultiplayerRole.Server)
            {
                _animatorCharacter = GetComponent<Animator>();
                // spawn SFX
                if (m_SpawnSFX != null)
                {
                    GameManager.Instance.SoundSystem.CreateEmitter(m_SpawnSFX, transform.position);
                }
            }

            if (isClientOwned)
            {
                var predictedPlayer = ReadGhostComponentData<PredictedPlayerGhost>();
                PlayerIndex = predictedPlayer.InputIndex;
                // create camera
                CreateClientCamera();

                // Add AudioListener to client position and ensure all other AudioListeners are disabled
                var audioListeners = Resources.FindObjectsOfTypeAll<AudioListener>();
                foreach (var a in audioListeners)
                {
                    a.enabled = false;
                }
                m_OwnerVisuals.AddComponent<AudioListener>();
                
                // Attach the listener to the player model rather than the camera
                GameManager.Instance.SoundSystem.SetListenerTransform(m_OwnerVisuals.transform);    
            }
            else if (Role == MultiplayerRole.ClientProxy)
            {
                // no physics required
                Controller.CharacterController.enabled = false;
            }

            gameObject.layer = (Role == MultiplayerRole.Server)
                ? (int)LayerIndex.ServerPlayer
                : (int)LayerIndex.ClientPlayer;

            PlayerGhostManager.TryGetInstanceByRole(Role, out var playerManager);
            playerManager.Register(this);
        }

        public override void OnGhostPreDestroy()
        {
            if (PlayerGhostManager.TryGetInstanceByRole(Role, out var playerManager))
            {
                playerManager.Unregister(this);
            }
        }

        void AttachPlayerViewCamera()
        {
            var playerCamera = GameObject.FindAnyObjectByType<Camera>();
            if (playerCamera != null)
            {
                playerCamera.transform.parent = transform.Find("ViewPoint");
                playerCamera.transform.localPosition = Vector3.zero;
                playerCamera.transform.localRotation = Quaternion.identity;
            }
        }

        private void CreateClientCamera()
        {
            //Disable current camera
            var existingCamera = FindAnyObjectByType<Camera>();
            if (existingCamera != null)
            {
                existingCamera.enabled = false;
            }

            // spawn the camera
            var mainCameraInstance = Instantiate(MainCameraPrefab, CameraTarget.transform);
            mainCameraInstance.transform.localPosition = Vector3.zero;
            mainCameraInstance.name = $"MainCamera_{PlayerIndex}";

            m_PlayerCamera = mainCameraInstance.GetComponent<Camera>();
            if (m_PlayerCamera != null)
            {
                float aspect = m_PlayerCamera.aspect > 0 ? m_PlayerCamera.aspect : (16f / 9f);
                m_BaseFOV = Utils.HorizontalToVerticalFOV(m_HorizontalFOV, aspect);
                m_PlayerCamera.fieldOfView = m_BaseFOV;
            }

            var audioListener = mainCameraInstance.GetComponent<AudioListener>();
            if (audioListener != null)
            {
                GameManager.Instance.SoundSystem.SetListenerTransform(audioListener.transform);
            }

            Utils.SetCursorVisible(false);
        }

        public void UpdateServer(float deltaTime)
        {
            // Server does not need to do anything for now regarding PlayerGhost
        }

        public void UpdateClient(float deltaTime)
        {
            var predictedPlayerGhost = ReadGhostComponentData<PredictedPlayerGhost>();
            var controllerState = predictedPlayerGhost.ControllerState;
            if (Role == MultiplayerRole.ClientOwned)
            {
                bool isAiming = (Mouse.current != null && Mouse.current.rightButton.isPressed) || 
                                (Gamepad.current != null && Gamepad.current.leftTrigger.isPressed);
                m_CurrentADSFactor = Mathf.MoveTowards(m_CurrentADSFactor, isAiming ? 1.0f : 0.0f, 6.0f * deltaTime);

                // 1. Procedural Camera Roll (Strafe & Slide Banking)
                bool isMoving = (controllerState.AnimatorTargetSpeed > 0.1f);
                float targetRoll = 0f;
                if (controllerState.IsSliding)
                {
                    targetRoll = -3.0f;
                }
                else if (isMoving && !isAiming)
                {
                    // Roll into strafe direction (bank when pressing A/D)
                    targetRoll = -ServerMovementInput.MoveInput.x * 1.5f;
                }
                m_CurrentCameraRoll = Mathf.Lerp(m_CurrentCameraRoll, targetRoll, 10f * deltaTime);

                CameraTarget.transform.rotation = Quaternion.Euler(
                    controllerState.PitchDegrees,
                    Camera.main != null ? Camera.main.transform.rotation.eulerAngles.y : 0f,
                    m_CurrentCameraRoll);

                // 2. Dual-Harmonic Headbob Engine (dampened during ADS)
                float adsBobDamp = Mathf.Lerp(1.0f, 0.15f, m_CurrentADSFactor);
                float targetCamHeight = controllerState.IsSliding ? 0.80f : (controllerState.IsCrouching ? 0.90f : 1.65f);
                float bobFreq = controllerState.IsSliding ? 14f : (controllerState.IsSprinting ? 12f : (controllerState.IsCrouching ? 6f : (isMoving ? 8.5f : 1.2f)));
                float bobAmpY = (controllerState.IsSliding ? 0.008f : (controllerState.IsSprinting ? 0.035f : (controllerState.IsCrouching ? 0.012f : (isMoving ? 0.020f : 0.003f)))) * adsBobDamp;
                float bobAmpX = bobAmpY * 0.5f;

                m_HeadBobTimer += deltaTime * bobFreq;
                float bobY = Mathf.Sin(m_HeadBobTimer) * bobAmpY;
                float bobX = Mathf.Cos(m_HeadBobTimer * 0.5f) * bobAmpX;

                Vector3 currentCamPos = CameraTarget.localPosition;
                currentCamPos.y = Mathf.MoveTowards(currentCamPos.y, targetCamHeight + bobY, 8.0f * deltaTime);
                currentCamPos.x = Mathf.MoveTowards(currentCamPos.x, bobX, 8.0f * deltaTime);
                CameraTarget.localPosition = currentCamPos;

                // 3. Dynamic FOV Scaling (with ADS Zoom)
                if (m_PlayerCamera != null)
                {
                    float aspect = m_PlayerCamera.aspect > 0 ? m_PlayerCamera.aspect : (16f / 9f);
                    m_BaseFOV = Utils.HorizontalToVerticalFOV(m_HorizontalFOV, aspect);

                    float sprintFOV = controllerState.IsSprinting ? (m_BaseFOV * 1.08f) : (controllerState.IsSliding ? (m_BaseFOV * 1.05f) : m_BaseFOV);
                    float targetFOV = Mathf.Lerp(sprintFOV, m_BaseFOV * 0.75f, m_CurrentADSFactor);
                    m_PlayerCamera.fieldOfView = Mathf.Lerp(m_PlayerCamera.fieldOfView, targetFOV, 10f * deltaTime);
                }

                // 4. Procedural Viewmodel Sway (Mouse Inertia)
                Vector2 lookDelta = UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.delta.ReadValue() : Vector2.zero;
                float swayAmount = Mathf.Lerp(1.0f, 0.25f, m_CurrentADSFactor);
                Quaternion targetSwayRot = Quaternion.Euler(-lookDelta.y * 0.12f * swayAmount, lookDelta.x * 0.16f * swayAmount, lookDelta.x * 0.08f * swayAmount);
                Vector3 targetSwayPos = new Vector3(-lookDelta.x * 0.0002f * swayAmount, -lookDelta.y * 0.0002f * swayAmount, 0f);

                m_CurrentSwayRot = Quaternion.Slerp(m_CurrentSwayRot, targetSwayRot, 12f * deltaTime);
                m_CurrentSwayPos = Vector3.Lerp(m_CurrentSwayPos, targetSwayPos, 12f * deltaTime);

                // 5. Procedural Harmonic Recoil Spring Kick
                if (predictedPlayerGhost.LastShotTick > _lastProcessedClientShotTick)
                {
                    float kickPitch = UnityEngine.Random.Range(-2.0f, -3.0f);
                    float kickYaw = UnityEngine.Random.Range(-0.6f, 0.6f);
                    float kickRoll = UnityEngine.Random.Range(-0.8f, 0.8f);
                    m_RecoilRot += new Vector3(kickPitch, kickYaw, kickRoll);
                    m_RecoilPos += new Vector3(0f, 0.003f, -0.025f);
                    _lastProcessedClientShotTick = predictedPlayerGhost.LastShotTick;
                }

                // Recoil return springs (tension & damping)
                m_RecoilRot = Vector3.Lerp(m_RecoilRot, Vector3.zero, 16f * deltaTime);
                m_RecoilPos = Vector3.Lerp(m_RecoilPos, Vector3.zero, 16f * deltaTime);

                // 6. Apply Composed Transform to 1P Viewmodel
                if (m_OwnerVisuals != null)
                {
                    // ADS Sight Alignment (smoothly centers the weapon for optic aim)
                    Vector3 adsOffset = new Vector3(-0.045f, 0.022f, 0.02f);
                    Vector3 basePosWithADS = Vector3.Lerp(m_InitialOwnerVisualsPos, m_InitialOwnerVisualsPos + adsOffset, m_CurrentADSFactor);

                    m_OwnerVisuals.transform.localPosition = basePosWithADS + m_CurrentSwayPos + m_RecoilPos;
                    m_OwnerVisuals.transform.localRotation = m_InitialOwnerVisualsRot * m_CurrentSwayRot * Quaternion.Euler(m_RecoilRot);
                }
            }

            var rot = Quaternion.Euler(controllerState.PitchDegrees, 0.0f, 0.0f);
            ReticlePoint.localPosition = rot * m_ReticleVector;

            //TODO: The following is a temporary fix for animation root moves (Robot Jump for example)
            m_OtherPlayerVisuals.transform.localPosition = Vector3.zero;
            m_OtherPlayerVisuals.transform.localRotation = Quaternion.identity;
        }

        public bool SetPlayerPositionFromRPC(float3 rpcPosition, float positionErrorSq)
        {
            var predictedPlayerGhost = ReadGhostComponentData<PredictedPlayerGhost>();
            var controllerState = predictedPlayerGhost.ControllerState;

            float positionError = math.distancesq(controllerState.CurrentPosition, rpcPosition);

            //allow the current player position to be altered by the client but only within a certain tolerance
            //(this is to avoid sliding during some position locked animations caused by the player predicting ahead of the server)
            if (positionError <= (positionErrorSq))
            {
                controllerState.CurrentPosition = rpcPosition;
                predictedPlayerGhost.ControllerState = controllerState;
                WriteGhostComponentData(predictedPlayerGhost);

                return true;
            }

            return false;
        }
    }
}