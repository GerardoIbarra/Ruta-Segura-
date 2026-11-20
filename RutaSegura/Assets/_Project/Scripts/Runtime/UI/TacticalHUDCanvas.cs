using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RutaSegura.Runtime.UI
{
    /// <summary>
    /// Lienzo central uGUI responsivo (1920x1080) que reemplaza todas las llamadas OnGUI().
    /// Incluye utilidades de construccion de paneles, texto y sprites circulares,
    /// y garantiza EventSystem con InputSystemUIInputModule.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [AddComponentMenu("Ruta Segura/UI/Tactical HUD Canvas")]
    public class TacticalHUDCanvas : MonoBehaviour
    {
        public static TacticalHUDCanvas Instance { get; private set; }

        private Canvas canvas;
        private RectTransform rootRect;
        private Font uiFont;
        private Sprite whiteSprite;

        public RectTransform Root => rootRect;
        public Font UIFont => uiFont;
        public Sprite WhiteSprite => whiteSprite;

        public static TacticalHUDCanvas GetOrCreate()
        {
            if (Instance != null)
            {
                Instance.EnsureInitialized();
                return Instance;
            }

            var existing = FindFirstObjectByType<TacticalHUDCanvas>();
            if (existing != null)
            {
                Instance = existing;
                existing.EnsureInitialized();
                return existing;
            }

            var go = new GameObject("TacticalHUDCanvas");
            var created = go.AddComponent<TacticalHUDCanvas>();
            created.EnsureInitialized();
            return created;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void EnsureInitialized()
        {
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (uiFont == null)
                {
                    uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 16);
                }
            }

            if (whiteSprite == null)
            {
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            }

            if (canvas == null)
            {
                var canvasGo = new GameObject("HUD_RootCanvas");
                canvasGo.transform.SetParent(transform, false);
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 50;

                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                canvasGo.AddComponent<GraphicRaycaster>();
                rootRect = canvasGo.GetComponent<RectTransform>();
            }

            EnsureEventSystem();
        }

        private static void EnsureEventSystem()
        {
            var es = EventSystem.current;
            if (es == null)
            {
                var esGo = new GameObject("EventSystem");
                es = esGo.AddComponent<EventSystem>();
            }

            if (es.GetComponent<InputSystemUIInputModule>() == null)
            {
                es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public Image CreatePanel(string name, Transform parent, Color color)
        {
            var rect = CreateRect(name, parent);
            var img = rect.gameObject.AddComponent<Image>();
            img.sprite = whiteSprite;
            img.color = color;
            return img;
        }

        public Text CreateText(string name, Transform parent, string content, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var rect = CreateRect(name, parent);
            var txt = rect.gameObject.AddComponent<Text>();
            txt.font = uiFont;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;
            txt.fontStyle = style;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            return txt;
        }

        public Button CreateButton(string name, Transform parent, string label, int fontSize, Color bgColor, Color textColor, out Image bgImage, out Text labelText)
        {
            bgImage = CreatePanel(name, parent, bgColor);
            var btn = bgImage.gameObject.AddComponent<Button>();
            btn.targetGraphic = bgImage;

            labelText = CreateText("Label", bgImage.transform, label, fontSize, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(labelText.rectTransform);
            return btn;
        }

        public static void Stretch(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        public static Sprite CreateRingSprite(int size, float innerRadiusNorm, float outerRadiusNorm)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            float invRadius = 1f / (size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) * invRadius;
                    float dy = (y - center) * invRadius;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float innerAlpha = Mathf.SmoothStep(innerRadiusNorm - 0.03f, innerRadiusNorm + 0.03f, r);
                    float outerAlpha = 1f - Mathf.SmoothStep(outerRadiusNorm - 0.03f, outerRadiusNorm + 0.03f, r);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(innerAlpha * outerAlpha)));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
