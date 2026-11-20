using UnityEngine;
using RutaSegura.Runtime.Threat;

namespace RutaSegura.Runtime.Audio
{
    /// <summary>
    /// Atmósfera sonora de Guadalajara:
    /// - Ambiente urbano 2D con filtro pasa-bajas (efecto tunel auditivo al subir la amenaza).
    /// - Dos focos 3D espacializados (protesta en Av. Hidalgo y cordon en Calle Galeana).
    /// </summary>
    [AddComponentMenu("Ruta Segura/Audio/Urban Soundscape Controller")]
    public class UrbanSoundscapeController : MonoBehaviour
    {
        [Header("Clips Opcionales")]
        [SerializeField] private AudioClip cityAmbienceOverride;
        [SerializeField] private AudioClip protestSirenOverride;

        [Header("Canal Ambiental y Tunel Auditivo")]
        [SerializeField] private float calmCutoffHz = 22000f;
        [SerializeField] private float panicCutoffHz = 750f;
        [SerializeField] private float ambienceVolume = 0.22f;

        [Header("Focos 3D de Protesta / Patrullas")]
        [SerializeField] private Vector3 hidalgoFocusPos = new Vector3(0f, 2.2f, 88f);
        [SerializeField] private Vector3 galeanaFocusPos = new Vector3(49f, 2.2f, 74f);
        [SerializeField] private float spatialVolume = 0.42f;

        private AudioSource ambienceSource;
        private AudioLowPassFilter lowPassFilter;

        private void Awake()
        {
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.clip = cityAmbienceOverride != null
                ? cityAmbienceOverride
                : ProceduralAudioLibrary.GetCityAmbienceClip();
            ambienceSource.loop = true;
            ambienceSource.spatialBlend = 0f;
            ambienceSource.volume = ambienceVolume;
            ambienceSource.playOnAwake = false;

            lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
            lowPassFilter.cutoffFrequency = calmCutoffHz;

            ambienceSource.Play();

            AudioClip protestClip = protestSirenOverride != null
                ? protestSirenOverride
                : ProceduralAudioLibrary.GetProtestSirenClip();

            SpawnSpatialSource("Foco_Audio_Hidalgo", hidalgoFocusPos, protestClip);
            SpawnSpatialSource("Foco_Audio_Galeana", galeanaFocusPos, protestClip);
        }

        private void SpawnSpatialSource(string childName, Vector3 worldPos, AudioClip clip)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, true);
            go.transform.position = worldPos;

            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 8f;
            src.maxDistance = 68f;
            src.volume = spatialVolume;
            src.playOnAwake = false;
            src.Play();
        }

        private void Update()
        {
            if (lowPassFilter == null) return;

            float threatNormalized = 0f;
            if (ThreatSystem.Instance != null)
            {
                threatNormalized = Mathf.Clamp01(ThreatSystem.Instance.CurrentThreat / 100f);
            }

            // Curva logaritmica percibida: cae rapido al entrar a niveles altos/criticos
            float targetCutoff = Mathf.Lerp(calmCutoffHz, panicCutoffHz, Mathf.Pow(threatNormalized, 1.35f));
            lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, targetCutoff, Time.deltaTime * 4f);
        }
    }
}
