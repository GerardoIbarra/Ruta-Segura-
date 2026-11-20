using UnityEngine;
using RutaSegura.Runtime.Threat;

namespace RutaSegura.Runtime.Audio
{
    /// <summary>
    /// Audio biometrico del protagonista: sincroniza el latido cardiaco con el estres
    /// calculado por ThreatSystem (68 BPM en calma hasta 148 BPM en panico).
    /// </summary>
    [AddComponentMenu("Ruta Segura/Audio/Biometric Audio System")]
    public class BiometricAudioSystem : MonoBehaviour
    {
        public static BiometricAudioSystem Instance { get; private set; }

        [Header("Calibracion Cardiaca")]
        [SerializeField] private float minBPM = 68f;
        [SerializeField] private float maxBPM = 148f;
        [SerializeField] private float minVolume = 0.06f;
        [SerializeField] private float maxVolume = 0.70f;

        [Header("Clip Opcional")]
        [Tooltip("Si se deja vacio, se sintetiza un latido fisiologico en memoria.")]
        [SerializeField] private AudioClip heartbeatClipOverride;

        private AudioSource audioSource;
        private AudioClip activeClip;
        private float currentBPM = 68f;
        private float beatTimer;
        private float beatPulse;

        public float CurrentBPM => currentBPM;
        public float BeatPulse => beatPulse;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;

            activeClip = heartbeatClipOverride != null
                ? heartbeatClipOverride
                : ProceduralAudioLibrary.GetHeartbeatClip();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            float threatNormalized = 0f;
            if (ThreatSystem.Instance != null)
            {
                threatNormalized = Mathf.Clamp01(ThreatSystem.Instance.CurrentThreat / 100f);
            }

            float targetBPM = Mathf.Lerp(minBPM, maxBPM, threatNormalized);
            currentBPM = Mathf.Lerp(currentBPM, targetBPM, Time.deltaTime * 3.5f);

            float secondsPerBeat = 60f / Mathf.Max(40f, currentBPM);
            beatTimer += Time.deltaTime;
            beatPulse = Mathf.MoveTowards(beatPulse, 0f, Time.deltaTime * 4.5f);

            if (beatTimer >= secondsPerBeat)
            {
                beatTimer -= secondsPerBeat;
                beatPulse = 1f;

                if (audioSource != null && activeClip != null)
                {
                    // Escala no lineal para que en calma sea sutil y en alerta/panico domine los oidos
                    float vol = Mathf.Lerp(minVolume, maxVolume, Mathf.Pow(threatNormalized, 0.85f));
                    audioSource.pitch = Mathf.Lerp(0.95f, 1.10f, threatNormalized);
                    audioSource.PlayOneShot(activeClip, vol);
                }
            }
        }
    }
}
