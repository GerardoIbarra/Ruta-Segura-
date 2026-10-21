using UnityEngine;

namespace RutaSegura.Runtime.Threat
{
    /// <summary>
    /// Zona espacial que altera el nivel de amenaza cuando el jugador se encuentra en ella.
    /// Utilizada para simular bloqueos, zonas congestionadas o presencia policial/manifestación.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Ruta Segura/Threat/Threat Zone")]
    public class ThreatZone : MonoBehaviour
    {
        [Header("Configuración de Zona")]
        [Tooltip("Nombre descriptivo de la zona")]
        [SerializeField] private string zoneName = "Bloqueo Avenida Principal";

        [Tooltip("Velocidad a la que aumenta la amenaza por segundo mientras estás dentro")]
        [SerializeField] private float threatIncreaseRate = 18f;

        [Tooltip("Golpe inmediato de amenaza al entrar por primera vez")]
        [SerializeField] private float entryBurstThreat = 10f;

        [Header("Alerta / Señal")]
        [Tooltip("Aviso que se muestra en consola/HUD cuando entras")]
        [SerializeField] private string warningMessage = "¡Cuidado! Concentración y calle bloqueada al frente.";

        private bool hasTriggeredBurst = false;

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
            if (other.CompareTag("Player") || other.GetComponentInParent<Player.PlayerController>() != null)
            {
                if (ThreatSystem.Instance != null)
                {
                    ThreatSystem.Instance.RegisterThreatRate(threatIncreaseRate);

                    if (!hasTriggeredBurst && entryBurstThreat > 0f)
                    {
                        ThreatSystem.Instance.AddThreat(entryBurstThreat);
                        hasTriggeredBurst = true;
                    }
                }

                if (!string.IsNullOrEmpty(warningMessage))
                {
                    Debug.LogWarning($"<color=orange>[ALERTA ZONA]</color> {zoneName}: {warningMessage}");
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponentInParent<Player.PlayerController>() != null)
            {
                if (ThreatSystem.Instance != null)
                {
                    ThreatSystem.Instance.UnregisterThreatRate();
                }
            }
        }

        private void OnDrawGizmos()
        {
            // Visualizar el área de peligro en el editor
            Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.35f);
            var col = GetComponent<BoxCollider>();
            if (col != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(col.center, col.size);
                Gizmos.DrawWireCube(col.center, col.size);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 3f);
            }
        }
    }
}
