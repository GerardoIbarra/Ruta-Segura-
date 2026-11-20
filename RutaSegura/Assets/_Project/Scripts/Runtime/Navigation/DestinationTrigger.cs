using UnityEngine;
using UnityEngine.UI;
using RutaSegura.Runtime.Core;
using RutaSegura.Runtime.Mission;
using RutaSegura.Runtime.UI;

namespace RutaSegura.Runtime.Navigation
{
    /// <summary>
    /// Zona de llegada / destino de la mision (Floreria 'El Jazmin').
    /// Muestra pantalla de exito en uGUI y notifica al GameLoopManager para reiniciar con [R].
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Ruta Segura/Navigation/Destination Trigger")]
    public class DestinationTrigger : MonoBehaviour
    {
        [Header("Configuracion del Destino")]
        [SerializeField] private string destinationName = "Floreria 'El Jazmin'";
        [SerializeField] private string completionMessage = "Llegaste a tu destino a salvo.";

        [Header("Estado")]
        [SerializeField] private bool hasReachedDestination = false;

        public bool HasReachedDestination => hasReachedDestination;
        public string DestinationName => destinationName;

        private GameObject victoryPanel;
        private Text victoryText;

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
            BuildVictoryUI();
        }

        private void BuildVictoryUI()
        {
            var hud = TacticalHUDCanvas.GetOrCreate();
            if (hud == null || hud.Root == null) return;

            var panel = hud.CreatePanel("Victory_Banner", hud.Root, new Color(0.06f, 0.09f, 0.12f, 0.95f));
            var rt = panel.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 80f);
            rt.sizeDelta = new Vector2(560f, 180f);
            panel.raycastTarget = false;

            var border = hud.CreatePanel("GreenAccent", rt, new Color(0.2f, 0.88f, 0.45f, 0.95f));
            border.rectTransform.anchorMin = new Vector2(0f, 1f);
            border.rectTransform.anchorMax = new Vector2(1f, 1f);
            border.rectTransform.pivot = new Vector2(0.5f, 1f);
            border.rectTransform.anchoredPosition = Vector2.zero;
            border.rectTransform.sizeDelta = new Vector2(0f, 4f);
            border.raycastTarget = false;

            victoryText = hud.CreateText("VictoryLabel", rt, "", 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            TacticalHUDCanvas.Stretch(victoryText.rectTransform, 18f);

            victoryPanel = panel.gameObject;
            victoryPanel.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasReachedDestination) return;

            if (other.CompareTag("Player") || other.GetComponentInParent<Player.PlayerController>() != null)
            {
                hasReachedDestination = true;

                if (MissionController.Instance != null)
                {
                    MissionController.Instance.CompleteMission();
                }

                GameLoopManager.Instance?.SetPhase(GamePhase.Outcome);

                string timeInfo = MissionController.Instance != null
                    ? $"\nTiempo empleado: {MissionController.Instance.ElapsedTime:F1} s"
                    : "";

                if (victoryText != null)
                {
                    victoryText.text = $"[ DESTINO ALCANZADO ]\n{destinationName}\n{completionMessage}{timeInfo}\n\nPresiona [ R ] para jugar de nuevo";
                }
                if (victoryPanel != null)
                {
                    victoryPanel.SetActive(true);
                }

                Debug.Log($"<color=green>[MISION COMPLETADA]</color> {completionMessage} en: {destinationName}");
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = hasReachedDestination ? new Color(0f, 1f, 0f, 0.4f) : new Color(0.2f, 0.8f, 1f, 0.35f);
            var col = GetComponent<BoxCollider>();
            if (col != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(col.center, col.size);
                Gizmos.DrawWireCube(col.center, col.size);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 1.5f);
            }
        }
    }
}
