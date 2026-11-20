using UnityEngine;
using RutaSegura.Runtime.Player;

namespace RutaSegura.Runtime.Audio
{
    /// <summary>
    /// Reproduce pasos diegeticos del jugador sobre asfalto/banqueta sincronizados
    /// con la velocidad y postura (caminar, trotar, paso cauto).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [AddComponentMenu("Ruta Segura/Audio/Footstep Audio Controller")]
    public class FootstepAudioController : MonoBehaviour
    {
        [Header("Cadencia y Volumen")]
        [SerializeField] private float walkStepInterval = 0.54f;
        [SerializeField] private float sprintStepInterval = 0.36f;
        [SerializeField] private float cautiousStepInterval = 0.78f;

        [SerializeField] private float walkVolume = 0.28f;
        [SerializeField] private float sprintVolume = 0.42f;
        [SerializeField] private float cautiousVolume = 0.14f;

        [Header("Clips Opcionales")]
        [SerializeField] private AudioClip[] footstepOverrides;

        private PlayerController player;
        private AudioSource audioSource;
        private float stepTimer;
        private int lastVariant = -1;

        private void Awake()
        {
            player = GetComponent<PlayerController>();

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        private void Update()
        {
            if (player == null || !player.enabled) return;
            if (!player.IsGrounded || player.CurrentSpeed < 0.4f)
            {
                stepTimer = 0f;
                return;
            }

            float interval = walkStepInterval;
            float volume = walkVolume;
            if (player.IsCautious)
            {
                interval = cautiousStepInterval;
                volume = cautiousVolume;
            }
            else if (player.IsSprinting)
            {
                interval = sprintStepInterval;
                volume = sprintVolume;
            }

            stepTimer += Time.deltaTime;
            if (stepTimer >= interval)
            {
                stepTimer -= interval;
                PlayFootstep(volume);
            }
        }

        private void PlayFootstep(float volume)
        {
            AudioClip clip = null;
            if (footstepOverrides != null && footstepOverrides.Length > 0)
            {
                clip = footstepOverrides[Random.Range(0, footstepOverrides.Length)];
            }

            if (clip == null)
            {
                int variant = Random.Range(0, 4);
                if (variant == lastVariant) variant = (variant + 1) % 4;
                lastVariant = variant;
                clip = ProceduralAudioLibrary.GetFootstepClip(variant);
            }

            if (clip != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(0.90f, 1.12f);
                audioSource.PlayOneShot(clip, volume);
            }
        }
    }
}
