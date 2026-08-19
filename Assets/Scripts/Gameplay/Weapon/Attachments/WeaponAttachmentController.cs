using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.MP_FPS
{
    public class WeaponAttachmentController : MonoBehaviour
    {
        [Header("Mounting Sockets")]
        [Tooltip("Socket Transform at the tip of the barrel for Suppressors, Compensators, Flash Hiders.")]
        public Transform Socket_Muzzle;

        [Tooltip("Socket Transform underneath the handguard for Foregrips, Angled Grips, Bipods.")]
        public Transform Socket_Underbarrel;

        [Tooltip("Socket Transform on the top Picatinny rail for Red Dot, Holographic, Scopes.")]
        public Transform Socket_Optic;

        [Tooltip("Socket Transform at the rear for Stocks / Buffer Tube.")]
        public Transform Socket_Stock;

        [Tooltip("Socket Transform at the magwell for Standard / Extended Magazines.")]
        public Transform Socket_Magazine;

        [Tooltip("Socket Transform on the side rail for Tactical Lasers / Flashlights.")]
        public Transform Socket_Laser;

        [Header("Equipped Loadout")]
        [SerializeField] private List<AttachmentSO> initialAttachments = new List<AttachmentSO>();

        private readonly Dictionary<AttachmentSlot, AttachmentSO> _equippedAttachments = new Dictionary<AttachmentSlot, AttachmentSO>();
        private readonly Dictionary<AttachmentSlot, GameObject> _spawnedAttachmentInstances = new Dictionary<AttachmentSlot, GameObject>();

        public event Action OnAttachmentsChanged;

        private void Start()
        {
            InitializeSockets();

            // Equip starting attachments
            foreach (var attachment in initialAttachments)
            {
                if (attachment != null)
                {
                    EquipAttachment(attachment);
                }
            }
        }

        private void InitializeSockets()
        {
            // Auto-create sockets as child transforms if not explicitly wired in Inspector
            if (Socket_Muzzle == null) Socket_Muzzle = CreateOrFindSocket("Socket_Muzzle", new Vector3(0f, 0.04f, 0.45f));
            if (Socket_Underbarrel == null) Socket_Underbarrel = CreateOrFindSocket("Socket_Underbarrel", new Vector3(0f, -0.04f, 0.22f));
            if (Socket_Optic == null) Socket_Optic = CreateOrFindSocket("Socket_Optic", new Vector3(0f, 0.085f, 0.05f));
            if (Socket_Stock == null) Socket_Stock = CreateOrFindSocket("Socket_Stock", new Vector3(0f, 0.02f, -0.25f));
            if (Socket_Magazine == null) Socket_Magazine = CreateOrFindSocket("Socket_Magazine", new Vector3(0f, -0.10f, 0.10f));
            if (Socket_Laser == null) Socket_Laser = CreateOrFindSocket("Socket_Laser", new Vector3(0.04f, 0.04f, 0.30f));
        }

        private Transform CreateOrFindSocket(string socketName, Vector3 defaultLocalPos)
        {
            var existing = transform.Find(socketName);
            if (existing != null) return existing;

            var newSocket = new GameObject(socketName).transform;
            newSocket.SetParent(transform, false);
            newSocket.localPosition = defaultLocalPos;
            newSocket.localRotation = Quaternion.identity;
            newSocket.localScale = Vector3.one;
            return newSocket;
        }

        public void EquipAttachment(AttachmentSO attachment)
        {
            if (attachment == null) return;

            // Remove existing attachment in that slot if present
            RemoveAttachment(attachment.Slot);

            _equippedAttachments[attachment.Slot] = attachment;

            Transform socket = GetSocketForSlot(attachment.Slot);
            if (socket != null && attachment.AttachmentPrefab != null)
            {
                GameObject instance = Instantiate(attachment.AttachmentPrefab, socket);
                instance.transform.localPosition = attachment.LocalPositionOffset;
                instance.transform.localRotation = Quaternion.Euler(attachment.LocalRotationOffset);
                instance.transform.localScale = attachment.LocalScale;
                _spawnedAttachmentInstances[attachment.Slot] = instance;
            }

            OnAttachmentsChanged?.Invoke();
        }

        public void RemoveAttachment(AttachmentSlot slot)
        {
            if (_spawnedAttachmentInstances.TryGetValue(slot, out GameObject instance) && instance != null)
            {
                Destroy(instance);
                _spawnedAttachmentInstances.Remove(slot);
            }

            if (_equippedAttachments.ContainsKey(slot))
            {
                _equippedAttachments.Remove(slot);
                OnAttachmentsChanged?.Invoke();
            }
        }

        public Transform GetSocketForSlot(AttachmentSlot slot)
        {
            return slot switch
            {
                AttachmentSlot.Muzzle => Socket_Muzzle,
                AttachmentSlot.Underbarrel => Socket_Underbarrel,
                AttachmentSlot.Optic => Socket_Optic,
                AttachmentSlot.Stock => Socket_Stock,
                AttachmentSlot.Magazine => Socket_Magazine,
                AttachmentSlot.Laser => Socket_Laser,
                _ => null
            };
        }

        #region Calculated Stat Modifiers

        public float TotalRecoilMultiplier
        {
            get
            {
                float multiplier = 1.0f;
                foreach (var att in _equippedAttachments.Values)
                {
                    multiplier *= att.RecoilMultiplier;
                }
                return multiplier;
            }
        }

        public float TotalADSSpeedMultiplier
        {
            get
            {
                float multiplier = 1.0f;
                foreach (var att in _equippedAttachments.Values)
                {
                    multiplier *= att.ADSSpeedMultiplier;
                }
                return multiplier;
            }
        }

        public float TotalMovementSpeedMultiplier
        {
            get
            {
                float multiplier = 1.0f;
                foreach (var att in _equippedAttachments.Values)
                {
                    multiplier *= att.MovementSpeedMultiplier;
                }
                return multiplier;
            }
        }

        public float GetOpticZoomFOV(float defaultZoomFOV)
        {
            if (_equippedAttachments.TryGetValue(AttachmentSlot.Optic, out var optic) && optic != null && optic.OpticZoomFOV > 0f)
            {
                return optic.OpticZoomFOV;
            }
            return defaultZoomFOV;
        }

        public Vector3 GetCustomADSOffset(Vector3 defaultOffset)
        {
            if (_equippedAttachments.TryGetValue(AttachmentSlot.Optic, out var optic) && optic != null && optic.CustomADSOffset != Vector3.zero)
            {
                return optic.CustomADSOffset;
            }
            return defaultOffset;
        }

        public SoundDef GetFireSoundOverride()
        {
            foreach (var att in _equippedAttachments.Values)
            {
                if (att.FireSoundOverride != null) return att.FireSoundOverride;
            }
            return null;
        }

        public bool IsMuzzleFlashHidden()
        {
            foreach (var att in _equippedAttachments.Values)
            {
                if (att.HideMuzzleFlash) return true;
            }
            return false;
        }

        #endregion
    }
}
