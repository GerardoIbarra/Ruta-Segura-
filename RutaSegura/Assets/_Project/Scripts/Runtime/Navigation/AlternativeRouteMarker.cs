using UnityEngine;
using UnityEngine.UI;
using RutaSegura.Runtime.UI;

namespace RutaSegura.Runtime.Navigation
{
    /// <summary>
    /// Marcador AR de ruta alternativa:
    /// 1. Anillo holografico 3D proyectado plano sobre el asfalto (Shader HologramRouteRing).
    /// 2. Tarjeta informativa uGUI proyectada desde la posicion 3D del marcador.
    /// </summary>
    [AddComponentMenu("Ruta Segura/Navigation/Alternative Route Marker")]
    public class AlternativeRouteMarker : MonoBehaviour
    {
        [Header("Contenido")]
        [SerializeField] private string routeTitle = "Ruta Alternativa Detectada";
        [SerializeField] private string routeSubtitle = "Callejon del Refugio ->";

        [Header("Visibilidad")]
        [SerializeField] private bool isActiveOnStart = false;
        [SerializeField] private float pulseSpeed = 2.5f;

        private bool isMarkerActive;
        private float alpha;
        private Camera mainCamera;

        // Proyeccion en el asfalto 3D
        private GameObject floorQuad;
        private Material ringMaterial;

        // Tarjeta uGUI
        private RectTransform cardRect;
        private Image cardBg;
        private Image cardAccent;
        private Text titleText;
        private Text subText;

        public bool IsMarkerActive => isMarkerActive;

        private void Awake()
        {
            isMarkerActive = isActiveOnStart;
            alpha = isActiveOnStart ? 1f : 0f;
            mainCamera = Camera.main;
        }

        private void Start()
        {
            BuildFloorHologram();
            BuildLabelCardUI();
            SyncVisualState();
        }

        private void OnDestroy()
        {
            if (ringMaterial != null) Destroy(ringMaterial);
            if (cardRect != null) Destroy(cardRect.gameObject);
        }

        private void BuildFloorHologram()
        {
            floorQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            floorQuad.name = "AR_AsphaltRing";
            var col = floorQuad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            floorQuad.transform.SetParent(transform, false);
            floorQuad.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            floorQuad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            floorQuad.transform.localScale = new Vector3(2.2f, 2.2f, 1f);

            var mr = floorQuad.GetComponent<MeshRenderer>();
            Shader shader = Shader.Find("RutaSegura/HologramRouteRing");
            if (shader != null && shader.isSupported)
            {
                ringMaterial = new Material(shader);
                ringMaterial.SetColor("_Color", new Color(0.18f, 0.90f, 1.0f, 0.95f));
                ringMaterial.SetFloat("_PulseSpeed", pulseSpeed);
            }
            else
            {
                // Respaldo seguro URP Unlit transparente con textura circular
                Shader fallback = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
                ringMaterial = new Material(fallback);
                ringMaterial.color = new Color(0.18f, 0.90f, 1.0f, 0.85f);
                ringMaterial.mainTexture = TacticalHUDCanvas.CreateRingSprite(128, 0.68f, 0.92f).texture;
            }
            mr.sharedMaterial = ringMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private void BuildLabelCardUI()
        {
            var hud = TacticalHUDCanvas.GetOrCreate();
            if (hud == null || hud.Root == null) return;

            cardBg = hud.CreatePanel($"MarkerCard_{gameObject.name}", hud.Root, new Color(0.04f, 0.08f, 0.12f, 0.92f));
            cardRect = cardBg.rectTransform;
            cardRect.anchorMin = Vector2.zero;
            cardRect.anchorMax = Vector2.zero;
            cardRect.pivot = new Vector2(0.5f, 0f);
            cardRect.sizeDelta = new Vector2(290f, 64f);
            cardBg.raycastTarget = false;

            cardAccent = hud.CreatePanel("CyanTop", cardRect, new Color(0.20f, 0.90f, 1f, 0.95f));
            cardAccent.rectTransform.anchorMin = new Vector2(0f, 1f);
            cardAccent.rectTransform.anchorMax = new Vector2(1f, 1f);
            cardAccent.rectTransform.pivot = new Vector2(0.5f, 1f);
            cardAccent.rectTransform.anchoredPosition = Vector2.zero;
            cardAccent.rectTransform.sizeDelta = new Vector2(0f, 3f);
            cardAccent.raycastTarget = false;

            titleText = hud.CreateText("Title", cardRect, $"[ {routeTitle.ToUpper()} ]", 12, new Color(0.25f, 0.95f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            titleText.rectTransform.anchoredPosition = new Vector2(0f, -6f);
            titleText.rectTransform.sizeDelta = new Vector2(-12f, 20f);

            subText = hud.CreateText("Subtitle", cardRect, routeSubtitle, 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            subText.rectTransform.anchorMin = new Vector2(0f, 0f);
            subText.rectTransform.anchorMax = new Vector2(1f, 0f);
            subText.rectTransform.pivot = new Vector2(0.5f, 0f);
            subText.rectTransform.anchoredPosition = new Vector2(0f, 6f);
            subText.rectTransform.sizeDelta = new Vector2(-12f, 28f);
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            float targetAlpha = isMarkerActive ? 1f : 0f;
            alpha = Mathf.MoveTowards(alpha, targetAlpha, Time.deltaTime * 2.5f);
            SyncVisualState();
        }

        private void SyncVisualState()
        {
            bool visible = alpha > 0.01f;
            if (floorQuad != null && floorQuad.activeSelf != visible)
            {
                floorQuad.SetActive(visible);
            }

            if (ringMaterial != null && visible)
            {
                if (ringMaterial.HasProperty("_Alpha"))
                {
                    ringMaterial.SetFloat("_Alpha", alpha);
                }
            }

            if (cardRect == null || mainCamera == null) return;

            if (!visible)
            {
                if (cardRect.gameObject.activeSelf) cardRect.gameObject.SetActive(false);
                return;
            }

            Vector3 worldPos = transform.position + Vector3.up * 0.65f;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            bool inFront = screenPos.z > 0.35f;

            if (!inFront)
            {
                if (cardRect.gameObject.activeSelf) cardRect.gameObject.SetActive(false);
                return;
            }

            if (!cardRect.gameObject.activeSelf) cardRect.gameObject.SetActive(true);

            // Convertir pixel de pantalla a coordenada del Canvas (ScreenSpaceOverlay usa pixels reales via position)
            cardRect.position = new Vector3(screenPos.x, screenPos.y + 18f, 0f);

            float pulse = 0.85f + Mathf.Sin(Time.time * pulseSpeed) * 0.15f;
            float a = alpha * pulse;

            cardBg.color = new Color(0.04f, 0.08f, 0.12f, 0.90f * a);
            cardAccent.color = new Color(0.20f, 0.90f, 1f, a);
            titleText.color = new Color(0.25f, 0.95f, 1f, a);
            subText.color = new Color(1f, 1f, 1f, a);
        }

        public void ActivateMarker()
        {
            isMarkerActive = true;
            Debug.Log($"<color=cyan>[NAVEGACION AR]</color> Marcador activo: {routeSubtitle}");
        }

        public void DeactivateMarker()
        {
            isMarkerActive = false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, 1.2f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.5f);
        }
    }
}
