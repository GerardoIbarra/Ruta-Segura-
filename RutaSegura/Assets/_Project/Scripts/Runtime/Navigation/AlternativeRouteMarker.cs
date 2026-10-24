using UnityEngine;

namespace RutaSegura.Runtime.Navigation
{
    /// <summary>
    /// Marcador holográfico / AR proyectado sobre el asfalto.
    /// Replicado según el prototipo en video del usuario:
    /// 'Ruta Alternativa Detectada - Callejón Libertad'.
    /// Se activa cuando ocurre el bloqueo para guiar al jugador.
    /// Diseñado con estética sobria y profesional de videojuego, sin emojis.
    /// </summary>
    [AddComponentMenu("Ruta Segura/Navigation/Alternative Route Marker")]
    public class AlternativeRouteMarker : MonoBehaviour
    {
        [Header("Contenido")]
        [SerializeField] private string routeTitle = "Ruta Alternativa Detectada";
        [SerializeField] private string routeSubtitle = "Calle Galeana ->";

        [Header("Visibilidad")]
        [SerializeField] private bool isActiveOnStart = false;
        [SerializeField] private float pulseSpeed = 2.5f;

        private bool isMarkerActive = false;
        private float alpha = 0f;
        private Camera mainCamera;

        public bool IsMarkerActive => isMarkerActive;

        private void Awake()
        {
            isMarkerActive = isActiveOnStart;
            alpha = isActiveOnStart ? 1f : 0f;
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // Transición suave de aparición
            float targetAlpha = isMarkerActive ? 1f : 0f;
            alpha = Mathf.MoveTowards(alpha, targetAlpha, Time.deltaTime * 2.5f);
        }

        public void ActivateMarker()
        {
            isMarkerActive = true;
            Debug.Log("<color=cyan>[NAVEGACIÓN AR]</color> Marcador de ruta alternativa proyectado en el asfalto.");
        }

        public void DeactivateMarker()
        {
            isMarkerActive = false;
        }

        private void OnGUI()
        {
            if (alpha <= 0.01f || mainCamera == null) return;

            // Proyectar la posición 3D del marcador a coordenadas de pantalla 2D
            Vector3 worldPos = transform.position + Vector3.up * 0.4f;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

            // Solo dibujar si está frente a la cámara
            if (screenPos.z < 0.2f) return;

            float pulse = 0.85f + Mathf.Sin(Time.time * pulseSpeed) * 0.15f;
            float currentAlpha = alpha * pulse;

            // Coordenadas GUI (eje Y invertido)
            float guiX = screenPos.x;
            float guiY = Screen.height - screenPos.y;

            // Dibujar círculo/anillo holográfico en el suelo
            float ringSize = 44f * pulse;
            GUI.color = new Color(0.15f, 0.85f, 0.95f, currentAlpha * 0.7f);
            GUI.Box(new Rect(guiX - ringSize / 2f, guiY - ringSize / 2f, ringSize, ringSize), GUIContent.none);

            // Tarjeta de información holográfica
            float cardW = 230f;
            float cardH = 50f;
            float cardX = guiX - cardW / 2f;
            float cardY = guiY - cardH - 20f;

            // Fondo semitransparente oscuro con borde cian brillante
            GUI.color = new Color(0.04f, 0.08f, 0.12f, currentAlpha * 0.92f);
            GUI.Box(new Rect(cardX, cardY, cardW, cardH), GUIContent.none);

            // Borde cian superior
            GUI.color = new Color(0.2f, 0.9f, 1f, currentAlpha);
            GUI.DrawTexture(new Rect(cardX, cardY, cardW, 2f), Texture2D.whiteTexture);

            // Título
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(0.25f, 0.95f, 1f, currentAlpha);
            GUI.Label(new Rect(cardX, cardY + 4f, cardW, 16f), $"[ {routeTitle.ToUpper()} ]", titleStyle);

            // Subtítulo con dirección
            var subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            subStyle.normal.textColor = new Color(1f, 1f, 1f, currentAlpha);
            GUI.Label(new Rect(cardX, cardY + 22f, cardW, 20f), routeSubtitle, subStyle);

            GUI.color = Color.white;
        }

        private void OnDrawGizmos()
        {
            // Visualización en el editor
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, 1.2f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.5f);
        }
    }
}
