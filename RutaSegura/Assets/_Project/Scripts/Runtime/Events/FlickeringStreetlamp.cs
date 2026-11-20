using UnityEngine;

namespace RutaSegura.Runtime.Events
{
    /// <summary>
    /// Controla el parpadeo electrico de la farola en la entrada de Callejon 1 ("Luz_Callejon_1")
    /// y el pulso calido de faro seguro en Floreria El Jazmin ("Luz_Floreria_Faro").
    /// </summary>
    [AddComponentMenu("Ruta Segura/Events/Flickering Streetlamp")]
    public class FlickeringStreetlamp : MonoBehaviour
    {
        [Header("Nombres en Escena")]
        [SerializeField] private string alleyLampName = "Luz_Callejon_1";
        [SerializeField] private string beaconLampName = "Luz_Floreria_Faro";

        private Light alleyLight;
        private float alleyBaseIntensity = 3.5f;

        private Light beaconLight;
        private float beaconBaseIntensity = 4.0f;

        private void Start()
        {
            alleyLight = FindLightByObjectName(alleyLampName);
            if (alleyLight != null)
            {
                alleyBaseIntensity = Mathf.Max(0.5f, alleyLight.intensity);
            }

            beaconLight = FindLightByObjectName(beaconLampName);
            if (beaconLight != null)
            {
                beaconBaseIntensity = Mathf.Max(0.5f, beaconLight.intensity);
            }
        }

        private void Update()
        {
            float t = Time.time;

            if (alleyLight != null)
            {
                // Parpadeo de luminaria de sodio fatigada con micro-cortes irregulares
                float noise = Mathf.PerlinNoise(t * 9.5f, 1.7f);
                float buzz = Mathf.Sin(t * 27f) * 0.08f;
                float gate = Mathf.Sin(t * 1.9f + noise * 3.1f);
                float factor = gate < -0.58f
                    ? Mathf.Lerp(0.18f, 0.42f, noise)
                    : Mathf.Clamp(0.78f + noise * 0.34f + buzz, 0.25f, 1.18f);

                alleyLight.intensity = alleyBaseIntensity * factor;
            }

            if (beaconLight != null)
            {
                // Faro verde-calido suave que invita al resguardo en Floreria El Jazmin
                float pulse = 0.86f + 0.22f * (0.5f + 0.5f * Mathf.Sin(t * 1.8f));
                beaconLight.intensity = beaconBaseIntensity * pulse;
            }
        }

        private static Light FindLightByObjectName(string objName)
        {
            if (string.IsNullOrEmpty(objName)) return null;

            var go = GameObject.Find(objName);
            if (go != null)
            {
                var found = go.GetComponentInChildren<Light>(true);
                if (found != null) return found;
            }

            var allLights = FindObjectsByType<Light>(FindObjectsInactive.Include);
            for (int i = 0; i < allLights.Length; i++)
            {
                if (allLights[i].gameObject.name.Contains(objName))
                {
                    return allLights[i];
                }
            }

            return null;
        }
    }
}
