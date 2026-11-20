using UnityEngine;
using UnityEngine.UI;
using RutaSegura.Runtime.Core;
using RutaSegura.Runtime.Threat;
using RutaSegura.Runtime.Navigation;
using RutaSegura.Runtime.UI;

namespace RutaSegura.Runtime.Events
{
    /// <summary>
    /// Evento dinamico que altera las condiciones de la ruta principal.
    /// Activa o asegura la barrera fisica, enciende el marcador de desvio,
    /// notifica al GameLoopManager la fase Incident y muestra aviso superior uGUI.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Ruta Segura/Events/Dynamic Roadblock Event")]
    public class DynamicRoadblockEvent : MonoBehaviour
    {
        [Header("Elementos del Evento")]
        [SerializeField] private GameObject roadblockBarrier;
        [SerializeField] private ThreatZone associatedThreatZone;
        [SerializeField] private AlternativeRouteMarker alternativeRouteMarker;

        [Header("Mensaje Diegetico (Eco Urbano / Alerta)")]
        [TextArea(2, 4)]
        [SerializeField] private string alertNotification = "[ECO URBANO - ALERTA VIAL]: Cierre inmediato de Av. Juarez por contingente. Busca ruta alterna.";

        [SerializeField] private float threatSpike = 20f;
        [SerializeField] private float notificationDuration = 6f;

        private bool eventTriggered;
        private float notificationTimer;

        private GameObject toastPanel;
        private Text toastText;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Start()
        {
            BuildToastUI();
        }

        private void BuildToastUI()
        {
            var hud = TacticalHUDCanvas.GetOrCreate();
            if (hud == null || hud.Root == null) return;

            var panel = hud.CreatePanel("Roadblock_AlertToast", hud.Root, new Color(0.08f, 0.10f, 0.14f, 0.94f));
            var rt = panel.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -24f);
            rt.sizeDelta = new Vector2(680f, 70f);
            panel.raycastTarget = false;

            var border = hud.CreatePanel("OrangeAccent", rt, new Color(0.95f, 0.45f, 0.12f, 0.95f));
            border.rectTransform.anchorMin = new Vector2(0f, 1f);
            border.rectTransform.anchorMax = new Vector2(1f, 1f);
            border.rectTransform.pivot = new Vector2(0.5f, 1f);
            border.rectTransform.anchoredPosition = Vector2.zero;
            border.rectTransform.sizeDelta = new Vector2(0f, 3f);
            border.raycastTarget = false;

            toastText = hud.CreateText("AlertLabel", rt, alertNotification, 16, new Color(0.98f, 0.72f, 0.30f), TextAnchor.MiddleCenter, FontStyle.Bold);
            TacticalHUDCanvas.Stretch(toastText.rectTransform, 16f);

            toastPanel = panel.gameObject;
            toastPanel.SetActive(false);
        }

        private void Update()
        {
            if (notificationTimer > 0f)
            {
                notificationTimer -= Time.deltaTime;
                if (notificationTimer <= 0f && toastPanel != null)
                {
                    toastPanel.SetActive(false);
                }
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

            if (roadblockBarrier != null && !roadblockBarrier.activeSelf)
            {
                roadblockBarrier.SetActive(true);
            }

            if (alternativeRouteMarker != null)
            {
                alternativeRouteMarker.ActivateMarker();
            }

            if (ThreatSystem.Instance != null && threatSpike > 0f)
            {
                ThreatSystem.Instance.AddThreat(threatSpike);
            }

            GameLoopManager.Instance?.SetPhase(GamePhase.Incident);

            if (toastPanel != null)
            {
                if (toastText != null) toastText.text = alertNotification;
                toastPanel.SetActive(true);
            }

            Debug.LogWarning($"<color=yellow>[EVENTO DINAMICO]</color> {alertNotification}");
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
