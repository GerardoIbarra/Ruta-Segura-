using UnityEngine;
using RutaSegura.Runtime.Threat;
using RutaSegura.Runtime.Navigation;

namespace RutaSegura.Runtime.Events
{
    /// <summary>
    /// Evento dinámico que altera las condiciones de una ruta en tiempo real.
    /// Simula el cierre repentino de una calle por vallas o cuerpos de seguridad,
    /// activa el marcador holográfico de ruta alternativa y obliga al jugador a adaptarse.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Ruta Segura/Events/Dynamic Roadblock Event")]
    public class DynamicRoadblockEvent : MonoBehaviour
    {
        [Header("Elementos del Evento")]
        [Tooltip("Objeto que se activará para bloquear físicamente el paso (valla, reja o barricada)")]
        [SerializeField] private GameObject roadblockBarrier;

        [Tooltip("Zona de amenaza que se activará o intensificará")]
        [SerializeField] private ThreatZone associatedThreatZone;

        [Tooltip("Marcador AR que aparecerá proyectado en el asfalto frente al callejón")]
        [SerializeField] private AlternativeRouteMarker alternativeRouteMarker;

        [Header("Mensaje Diegético (Eco Urbano / Alerta)")]
        [TextArea(2, 4)]
        [SerializeField] private string alertNotification = "📢 [ECO URBANO]: Cierre inmediato de la avenida principal por contingente. Rutas bloqueadas.";

        [Tooltip("Aumento inmediato de amenaza al presenciar el cierre")]
        [SerializeField] private float threatSpike = 20f;

        [Tooltip("Duración en pantalla del aviso (segundos)")]
        [SerializeField] private float notificationDuration = 6f;

        private bool eventTriggered = false;
        private float notificationTimer = 0f;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Update()
        {
            if (notificationTimer > 0f)
            {
                notificationTimer -= Time.deltaTime;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (eventTriggered) return;

            if (other.CompareTag("Player") || other.GetComponentInParent<Player.PlayerController>() != null)
            {
                TriggerEvent();
            }
        }

        public void TriggerEvent()
        {
            eventTriggered = true;
            notificationTimer = notificationDuration;

            // Activar la barrera física si estaba oculta
            if (roadblockBarrier != null)
            {
                roadblockBarrier.SetActive(true);
            }

            // Activar el marcador AR en el suelo como en el video de referencia
            if (alternativeRouteMarker != null)
            {
                alternativeRouteMarker.ActivateMarker();
            }

            // Aplicar pico de tensión al jugador
            if (ThreatSystem.Instance != null && threatSpike > 0f)
            {
                ThreatSystem.Instance.AddThreat(threatSpike);
            }

            Debug.LogWarning($"<color=yellow>[EVENTO DINÁMICO]</color> {alertNotification}");
        }

        private void OnGUI()
        {
            // Banner de notificación en la parte superior central simulando alerta del móvil
            if (notificationTimer > 0f)
            {
                float bannerWidth = 550f;
                float bannerHeight = 65f;
                float x = (Screen.width - bannerWidth) / 2f;
                float y = 15f;

                var origColor = GUI.color;
                GUI.color = new Color(0.95f, 0.4f, 0.1f, 0.95f);

                var boxStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };

                GUI.Box(new Rect(x, y, bannerWidth, bannerHeight), alertNotification, boxStyle);
                GUI.color = origColor;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = eventTriggered ? Color.gray : new Color(1f, 0.8f, 0f, 0.4f);
            var col = GetComponent<BoxCollider>();
            if (col != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(col.center, col.size);
                Gizmos.DrawWireCube(col.center, col.size);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 2f);
            }
        }
    }
}
