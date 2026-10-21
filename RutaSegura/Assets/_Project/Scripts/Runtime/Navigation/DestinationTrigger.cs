using UnityEngine;

namespace RutaSegura.Runtime.Navigation
{
    /// <summary>
    /// Zona de llegada / destino de la misión (ej. La Florería).
    /// Detecta la entrada del jugador y dispara el evento de éxito.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Ruta Segura/Navigation/Destination Trigger")]
    public class DestinationTrigger : MonoBehaviour
    {
        [Header("Configuración del Destino")]
        [Tooltip("Nombre legible del destino objetivo")]
        [SerializeField] private string destinationName = "Florería 'El Jazmín'";

        [Tooltip("Mensaje de éxito que verá el jugador")]
        [SerializeField] private string completionMessage = "¡Llegaste a tu destino a salvo!";

        [Header("Estado")]
        [SerializeField] private bool hasReachedDestination = false;

        public bool HasReachedDestination => hasReachedDestination;
        public string DestinationName => destinationName;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasReachedDestination) return;

            // Detectar si el objeto que entra tiene el PlayerController o tag Player
            if (other.CompareTag("Player") || other.GetComponentInParent<Player.PlayerController>() != null)
            {
                hasReachedDestination = true;
                Debug.Log($"<color=green>[MISIÓN COMPLETADA]</color> {completionMessage} en: {destinationName}");
            }
        }

        private void OnGUI()
        {
            // UI provisional en pantalla para feedback inmediato durante el prototipo
            if (hasReachedDestination)
            {
                var originalColor = GUI.color;
                GUI.color = Color.green;

                var rect = new Rect(Screen.width / 2f - 200f, 60f, 400f, 80f);
                var style = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                GUI.Box(rect, $"✓ {destinationName}\n{completionMessage}", style);
                GUI.color = originalColor;
            }
        }

        private void OnDrawGizmos()
        {
            // Visualizar el volumen del destino en la vista de escena
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
