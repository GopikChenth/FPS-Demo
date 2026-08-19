using UnityEngine;

namespace Unity.MP_FPS
{
    public enum AttachmentSlot
    {
        Muzzle,
        Underbarrel,
        Optic,
        Stock,
        Magazine,
        Laser
    }

    [CreateAssetMenu(fileName = "NewAttachment", menuName = "Gunsmith/Attachment", order = 1)]
    public class AttachmentSO : ScriptableObject
    {
        [Header("General Info")]
        public string AttachmentName = "Tactical Attachment";
        [TextArea(2, 4)] public string Description = "Modifies weapon performance and handling.";
        public AttachmentSlot Slot = AttachmentSlot.Muzzle;

        [Header("3D Visual Mesh")]
        [Tooltip("The 3D prefab spawned on the weapon socket.")]
        public GameObject AttachmentPrefab;
        public Vector3 LocalPositionOffset = Vector3.zero;
        public Vector3 LocalRotationOffset = Vector3.zero;
        public Vector3 LocalScale = Vector3.one;

        [Header("Stat Modifiers")]
        [Tooltip("Multiplier for weapon vertical & horizontal recoil kick (e.g. 0.80 = -20% recoil).")]
        [Range(0.2f, 2.0f)] public float RecoilMultiplier = 1.0f;

        [Tooltip("Multiplier for Aim Down Sights transition speed (e.g. 1.15 = +15% faster ADS).")]
        [Range(0.5f, 2.0f)] public float ADSSpeedMultiplier = 1.0f;

        [Tooltip("Multiplier for player movement speed while carrying this weapon.")]
        [Range(0.7f, 1.3f)] public float MovementSpeedMultiplier = 1.0f;

        [Tooltip("Extra bullets added to or subtracted from magazine capacity.")]
        public int MagazineCapacityDelta = 0;

        [Header("Optic Configuration")]
        [Tooltip("If greater than 0, overrides the weapon's default ADS camera zoom FOV.")]
        public float OpticZoomFOV = 0f;

        [Tooltip("Optional custom reticle glass center offset in local coordinates for ADS eye alignment.")]
        public Vector3 CustomADSOffset = Vector3.zero;

        [Header("Audio & VFX")]
        [Tooltip("Custom firing sound when this attachment is equipped (e.g. Suppressed gunshot).")]
        public SoundDef FireSoundOverride;

        [Tooltip("If true, suppresses and hides the weapon's muzzle flash effect.")]
        public bool HideMuzzleFlash = false;
    }
}
