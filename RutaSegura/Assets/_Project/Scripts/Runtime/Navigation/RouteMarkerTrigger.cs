using UnityEngine;
using RutaSegura.Runtime.Core;

namespace RutaSegura.Runtime.Navigation
{
    /// <summary>
    /// Activa un marcador de ruta cuando el jugador entra al volumen
    /// y opcionalmente avanza el GameLoopManager a la fase Detour.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [AddComponentMenu("Ruta Segura/Navigation/Route Marker Trigger")]
    public class RouteMarkerTrigger : MonoBehaviour
    {
        [SerializeField] private AlternativeRouteMarker marker;
        [SerializeField] private AlternativeRouteMarker markerToHide;
        [SerializeField] private bool entersDetourPhase = false;

        private bool triggered;

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggered) return;
            if (!other.CompareTag("Player") && other.GetComponentInParent<Player.PlayerController>() == null) return;

            triggered = true;
            if (markerToHide != null) markerToHide.DeactivateMarker();
            if (marker != null) marker.ActivateMarker();
            if (entersDetourPhase) GameLoopManager.Instance?.SetPhase(GamePhase.Detour);
        }

        private void OnDrawGizmos()
        {
            var col = GetComponent<BoxCollider>();
            if (col == null) return;
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.25f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
        }
    }
}
