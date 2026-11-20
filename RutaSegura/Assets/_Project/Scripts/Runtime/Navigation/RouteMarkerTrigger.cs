using UnityEngine;

namespace RutaSegura.Runtime.Navigation
{
    /// <summary>
    /// Activa un marcador de ruta cuando el jugador entra al volumen.
    /// Se usa para el segundo aviso de navegacion ("Nueva Ruta: Calle Hospital").
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [AddComponentMenu("Ruta Segura/Navigation/Route Marker Trigger")]
    public class RouteMarkerTrigger : MonoBehaviour
    {
        [SerializeField] private AlternativeRouteMarker marker;
        [SerializeField] private AlternativeRouteMarker markerToHide;

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
