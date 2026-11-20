using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using RutaSegura.Runtime.Core;

namespace RutaSegura.Runtime.UI
{
    /// <summary>
    /// Interfaz diegetica uGUI del telefono del protagonista:
    /// - Barra compacta superior izquierda con indicador de mensajes nuevos [!].
    /// - App 'RutaLibre': mini-mapa tactico 2D en vivo de Guadalajara con marcador del jugador,
    ///   bloqueos en tiempo real y distancia restante por la ruta alterna (~200 m).
    /// - App 'Eco Urbano': feed civico dinamico sincronizado con GameLoopManager.
    /// </summary>
    [AddComponentMenu("Ruta Segura/UI/Phone HUD")]
    public class PhoneHUD : MonoBehaviour
    {
        public static PhoneHUD Instance { get; private set; }

        [Header("Configuracion")]
        [SerializeField] private bool isOpen = false;
        [SerializeField] private PhoneApp currentApp = PhoneApp.RutaLibre;

        public bool IsOpen => isOpen;

        public enum PhoneApp
        {
            RutaLibre,
            EcoUrbano
        }

        private struct FeedPost
        {
            public string Meta;
            public string Body;
            public Color Accent;
        }

        // Constantes geograficas exactas del mapa (2.5 px/m isotropico en 320x240)
        private const int MapW = 320;
        private const int MapH = 240;
        private const float MapXMin = -22f;
        private const float MapXMax = 106f;
        private const float MapZMin = 0f;
        private const float MapZMax = 96f;

        private Transform playerTransform;
        private bool hasUnreadAlert;

        private readonly List<FeedPost> feedPosts = new List<FeedPost>();
        private readonly List<GameObject> feedCardObjects = new List<GameObject>();

        // uGUI
        private Text topBarText;
        private GameObject topBarBadge;
        private GameObject phoneFrame;
        private Image rutaTabBg;
        private Image ecoTabBg;
        private GameObject rutaContent;
        private GameObject ecoContent;
        private Text distanceText;
        private Text routeAStatusText;
        private Text routeBStatusText;
        private RawImage mapImage;
        private Texture2D mapTexture;
        private RectTransform playerArrowRt;

        private GameObject pushToastGroup;
        private Image pushToastAccent;
        private Text pushToastMetaText;
        private Text pushToastBodyText;
        private float toastTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            if (GameLoopManager.Instance != null)
            {
                GameLoopManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }
            if (mapTexture != null)
            {
                Destroy(mapTexture);
            }
        }

        private void Start()
        {
            var playerCtrl = FindFirstObjectByType<Player.PlayerController>();
            if (playerCtrl != null) playerTransform = playerCtrl.transform;

            SeedInitialFeed();
            BuildUI();

            if (GameLoopManager.Instance != null)
            {
                GameLoopManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            }

            SyncOpenState(applyCursor: false);
            SwitchTab(currentApp);
        }

        private void Update()
        {
            if (GameLoopManager.Instance != null && GameLoopManager.Instance.IsPaused)
            {
                return;
            }

            if (toastTimer > 0f)
            {
                toastTimer -= Time.deltaTime;
                if (toastTimer <= 0f && pushToastGroup != null)
                {
                    pushToastGroup.SetActive(false);
                }
            }

            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                isOpen = !isOpen;
                if (isOpen)
                {
                    if (pushToastGroup != null) pushToastGroup.SetActive(false);
                    toastTimer = 0f;
                    if (currentApp == PhoneApp.EcoUrbano)
                    {
                        hasUnreadAlert = false;
                    }
                }
                SyncOpenState(applyCursor: true);
            }

            if (isOpen)
            {
                UpdateLiveNavigation();
            }
        }

        private void SeedInitialFeed()
        {
            feedPosts.Clear();
            feedPosts.Add(new FeedPost
            {
                Meta = "Hace 3 min - @transito_gdl",
                Body = "Cierre preventivo reportado en cruce con Av. Hidalgo. Eviten Av. Juarez.",
                Accent = new Color(0.95f, 0.55f, 0.20f)
            });
            feedPosts.Add(new FeedPost
            {
                Meta = "Hace 11 min - @vecinos_centro",
                Body = "Varios comercios bajando cortinas metalicas sobre la avenida principal.",
                Accent = new Color(0.45f, 0.72f, 0.95f)
            });
            feedPosts.Add(new FeedPost
            {
                Meta = "Hace 22 min - @movilidad_urbana",
                Body = "Movilizacion avanza hacia el centro con desvios en rutas de transporte.",
                Accent = new Color(0.45f, 0.72f, 0.95f)
            });
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Exploration:
                    PrependFeedPost(
                        "Ahora - @movilidad_gdl",
                        "Trafico detenido adelante por Av. Hidalgo. Considera calles secundarias.",
                        new Color(0.35f, 0.78f, 0.95f),
                        markUnread: false);
                    break;
                case GamePhase.Incident:
                    PrependFeedPost(
                        "URGENTE - @eco_urbano",
                        "AHORA: Vallas y patrullas cierran Av. Juarez. Callejones al oriente habilitados.",
                        new Color(0.98f, 0.45f, 0.14f),
                        markUnread: true);
                    if (routeAStatusText != null)
                    {
                        routeAStatusText.text = "Estado: BLOQUEADA por vallas y operativo\nRiesgo: ALTO / SIN PASO";
                    }
                    break;
                case GamePhase.Detour:
                    PrependFeedPost(
                        "AVISO - @ruta_segura",
                        "Cordon policial cierra Calle Galeana. Desvio activo hacia el sur por Calle Hospital.",
                        new Color(0.95f, 0.75f, 0.18f),
                        markUnread: true);
                    if (routeBStatusText != null)
                    {
                        routeBStatusText.text = "Estado: EN RUTA hacia Calle Hospital\nRiesgo: MINIMO / VIA LIBRE";
                    }
                    break;
                case GamePhase.Outcome:
                    PrependFeedPost(
                        "CIERRE - @eco_urbano",
                        "Reporte finalizado en zona centro. Mantente en zona resguardada.",
                        new Color(0.28f, 0.85f, 0.48f),
                        markUnread: false);
                    break;
            }
        }

        private void PrependFeedPost(string meta, string body, Color accent, bool markUnread)
        {
            feedPosts.Insert(0, new FeedPost { Meta = meta, Body = body, Accent = accent });
            if (feedPosts.Count > 5)
            {
                feedPosts.RemoveAt(feedPosts.Count - 1);
            }

            if (markUnread && (!isOpen || currentApp != PhoneApp.EcoUrbano))
            {
                hasUnreadAlert = true;
            }

            if (markUnread && !isOpen && pushToastGroup != null)
            {
                if (pushToastAccent != null) pushToastAccent.color = accent;
                if (pushToastMetaText != null) pushToastMetaText.text = $"ECO URBANO  |  {meta}";
                if (pushToastBodyText != null) pushToastBodyText.text = body;
                pushToastGroup.SetActive(true);
                toastTimer = 4.5f;
            }

            RebuildFeedCards();
            RefreshTopBar();
        }

        private void BuildUI()
        {
            var hud = TacticalHUDCanvas.GetOrCreate();
            if (hud == null || hud.Root == null) return;

            // 1. Barra compacta superior izquierda
            var topImg = hud.CreatePanel("Phone_TopObjectiveBar", hud.Root, new Color(0.05f, 0.08f, 0.11f, 0.90f));
            var topRt = topImg.rectTransform;
            topRt.anchorMin = new Vector2(0f, 1f);
            topRt.anchorMax = new Vector2(0f, 1f);
            topRt.pivot = new Vector2(0f, 1f);
            topRt.anchoredPosition = new Vector2(30f, -26f);
            topRt.sizeDelta = new Vector2(720f, 40f);
            topImg.raycastTarget = false;

            var topBorder = hud.CreatePanel("CyanLeft", topRt, new Color(0.20f, 0.82f, 0.92f, 0.85f));
            topBorder.rectTransform.anchorMin = new Vector2(0f, 0f);
            topBorder.rectTransform.anchorMax = new Vector2(0f, 1f);
            topBorder.rectTransform.pivot = new Vector2(0f, 0.5f);
            topBorder.rectTransform.anchoredPosition = Vector2.zero;
            topBorder.rectTransform.sizeDelta = new Vector2(4f, 0f);
            topBorder.raycastTarget = false;

            topBarText = hud.CreateText("TopBarText", topRt, "[TAB] DISPOSITIVO  |  [E] ESCANEAR  |  [V] CAMARA  |  [RMB] ZOOM  |  FLORERIA EL JAZMIN", 13, new Color(0.92f, 0.94f, 0.97f), TextAnchor.MiddleLeft, FontStyle.Bold);
            topBarText.rectTransform.anchorMin = Vector2.zero;
            topBarText.rectTransform.anchorMax = Vector2.one;
            topBarText.rectTransform.offsetMin = new Vector2(14f, 0f);
            topBarText.rectTransform.offsetMax = new Vector2(-38f, 0f);

            var badgeImg = hud.CreatePanel("UnreadBadge", topRt, new Color(0.95f, 0.45f, 0.12f, 0.95f));
            badgeImg.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            badgeImg.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            badgeImg.rectTransform.pivot = new Vector2(1f, 0.5f);
            badgeImg.rectTransform.anchoredPosition = new Vector2(-8f, 0f);
            badgeImg.rectTransform.sizeDelta = new Vector2(24f, 22f);
            badgeImg.raycastTarget = false;
            var badgeLbl = hud.CreateText("BadgeText", badgeImg.rectTransform, "!", 14, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            TacticalHUDCanvas.Stretch(badgeLbl.rectTransform);
            topBarBadge = badgeImg.gameObject;
            topBarBadge.SetActive(false);

            // 1B. Notificacion flotante push de Eco Urbano (cuando el telefono esta cerrado)
            var toastImg = hud.CreatePanel("Phone_PushToast", hud.Root, new Color(0.06f, 0.08f, 0.12f, 0.94f));
            var toastRt = toastImg.rectTransform;
            toastRt.anchorMin = new Vector2(1f, 0.5f);
            toastRt.anchorMax = new Vector2(1f, 0.5f);
            toastRt.pivot = new Vector2(1f, 0.5f);
            toastRt.anchoredPosition = new Vector2(-32f, 20f);
            toastRt.sizeDelta = new Vector2(340f, 96f);
            toastImg.raycastTarget = false;

            pushToastAccent = hud.CreatePanel("ToastAccent", toastRt, new Color(0.98f, 0.45f, 0.14f, 0.98f));
            pushToastAccent.rectTransform.anchorMin = new Vector2(0f, 0f);
            pushToastAccent.rectTransform.anchorMax = new Vector2(0f, 1f);
            pushToastAccent.rectTransform.pivot = new Vector2(0f, 0.5f);
            pushToastAccent.rectTransform.anchoredPosition = Vector2.zero;
            pushToastAccent.rectTransform.sizeDelta = new Vector2(5f, 0f);
            pushToastAccent.raycastTarget = false;

            pushToastMetaText = hud.CreateText("ToastMeta", toastRt, "ECO URBANO  |  ALERTA", 12, new Color(0.98f, 0.68f, 0.25f), TextAnchor.UpperLeft, FontStyle.Bold);
            pushToastMetaText.rectTransform.anchorMin = new Vector2(0f, 1f);
            pushToastMetaText.rectTransform.anchorMax = new Vector2(1f, 1f);
            pushToastMetaText.rectTransform.pivot = new Vector2(0.5f, 1f);
            pushToastMetaText.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            pushToastMetaText.rectTransform.sizeDelta = new Vector2(-28f, 20f);

            pushToastBodyText = hud.CreateText("ToastBody", toastRt, "", 12, new Color(0.92f, 0.94f, 0.97f), TextAnchor.UpperLeft, FontStyle.Normal);
            pushToastBodyText.rectTransform.anchorMin = Vector2.zero;
            pushToastBodyText.rectTransform.anchorMax = Vector2.one;
            pushToastBodyText.rectTransform.offsetMin = new Vector2(14f, 10f);
            pushToastBodyText.rectTransform.offsetMax = new Vector2(-12f, -32f);

            pushToastGroup = toastImg.gameObject;
            pushToastGroup.SetActive(false);

            // 2. Carcasa del telefono (abajo a la derecha)
            var chassis = hud.CreatePanel("Phone_Chassis", hud.Root, new Color(0.06f, 0.07f, 0.09f, 0.98f));
            var phoneRt = chassis.rectTransform;
            phoneRt.anchorMin = new Vector2(1f, 0f);
            phoneRt.anchorMax = new Vector2(1f, 0f);
            phoneRt.pivot = new Vector2(1f, 0f);
            phoneRt.anchoredPosition = new Vector2(-36f, 36f);
            phoneRt.sizeDelta = new Vector2(368f, 594f);
            phoneFrame = chassis.gameObject;

            var screen = hud.CreatePanel("Screen", phoneRt, new Color(0.11f, 0.13f, 0.17f, 1f));
            TacticalHUDCanvas.Stretch(screen.rectTransform, 6f);
            var screenRt = screen.rectTransform;

            // Barra de estado superior del smartphone
            var statusBg = hud.CreatePanel("StatusBar", screenRt, new Color(0.07f, 0.08f, 0.11f, 1f));
            statusBg.rectTransform.anchorMin = new Vector2(0f, 1f);
            statusBg.rectTransform.anchorMax = new Vector2(1f, 1f);
            statusBg.rectTransform.pivot = new Vector2(0.5f, 1f);
            statusBg.rectTransform.anchoredPosition = Vector2.zero;
            statusBg.rectTransform.sizeDelta = new Vector2(0f, 28f);

            var clockTxt = hud.CreateText("Clock", statusBg.rectTransform, "17:42", 12, new Color(0.78f, 0.82f, 0.88f), TextAnchor.MiddleLeft, FontStyle.Bold);
            clockTxt.rectTransform.anchorMin = Vector2.zero;
            clockTxt.rectTransform.anchorMax = Vector2.one;
            clockTxt.rectTransform.offsetMin = new Vector2(12f, 0f);
            clockTxt.rectTransform.offsetMax = new Vector2(-120f, 0f);

            var signalTxt = hud.CreateText("Signal", statusBg.rectTransform, "LTE  |  BAT 78%", 12, new Color(0.78f, 0.82f, 0.88f), TextAnchor.MiddleRight);
            signalTxt.rectTransform.anchorMin = Vector2.zero;
            signalTxt.rectTransform.anchorMax = Vector2.one;
            signalTxt.rectTransform.offsetMin = new Vector2(100f, 0f);
            signalTxt.rectTransform.offsetMax = new Vector2(-12f, 0f);

            // Pestanas clickeables
            var btnRuta = hud.CreateButton("Tab_RutaLibre", screenRt, "[ RUTALIBRE ]", 13, new Color(0.18f, 0.44f, 0.80f), Color.white, out rutaTabBg, out _);
            rutaTabBg.rectTransform.anchorMin = new Vector2(0f, 1f);
            rutaTabBg.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rutaTabBg.rectTransform.pivot = new Vector2(0f, 1f);
            rutaTabBg.rectTransform.anchoredPosition = new Vector2(8f, -34f);
            rutaTabBg.rectTransform.sizeDelta = new Vector2(-12f, 32f);
            btnRuta.onClick.AddListener(() => SwitchTab(PhoneApp.RutaLibre));

            var btnEco = hud.CreateButton("Tab_EcoUrbano", screenRt, "[ ECO URBANO ]", 13, new Color(0.16f, 0.18f, 0.22f), Color.white, out ecoTabBg, out _);
            ecoTabBg.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            ecoTabBg.rectTransform.anchorMax = new Vector2(1f, 1f);
            ecoTabBg.rectTransform.pivot = new Vector2(1f, 1f);
            ecoTabBg.rectTransform.anchoredPosition = new Vector2(-8f, -34f);
            ecoTabBg.rectTransform.sizeDelta = new Vector2(-12f, 32f);
            btnEco.onClick.AddListener(() => SwitchTab(PhoneApp.EcoUrbano));

            // Contenedor RutaLibre
            var rutaPanel = TacticalHUDCanvas.CreateRect("Content_RutaLibre", screenRt);
            rutaPanel.anchorMin = Vector2.zero;
            rutaPanel.anchorMax = Vector2.one;
            rutaPanel.offsetMin = new Vector2(12f, 18f);
            rutaPanel.offsetMax = new Vector2(-12f, -74f);
            rutaContent = rutaPanel.gameObject;
            BuildRutaLibreContent(hud, rutaPanel);

            // Contenedor Eco Urbano
            var ecoPanel = TacticalHUDCanvas.CreateRect("Content_EcoUrbano", screenRt);
            ecoPanel.anchorMin = Vector2.zero;
            ecoPanel.anchorMax = Vector2.one;
            ecoPanel.offsetMin = new Vector2(12f, 18f);
            ecoPanel.offsetMax = new Vector2(-12f, -74f);
            ecoContent = ecoPanel.gameObject;
            BuildEcoUrbanoContent(hud, ecoPanel);

            // Barra inferior home
            var homeBar = hud.CreatePanel("HomeBar", screenRt, new Color(0.55f, 0.58f, 0.65f, 0.5f));
            homeBar.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            homeBar.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            homeBar.rectTransform.pivot = new Vector2(0.5f, 0f);
            homeBar.rectTransform.anchoredPosition = new Vector2(0f, 6f);
            homeBar.rectTransform.sizeDelta = new Vector2(96f, 4f);
            homeBar.raycastTarget = false;
        }

        private void BuildRutaLibreContent(TacticalHUDCanvas hud, RectTransform parent)
        {
            var destHeader = hud.CreateText("DestHeader", parent, "DESTINO: FLORERIA EL JAZMIN", 13, new Color(0.35f, 0.78f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            destHeader.rectTransform.anchorMin = new Vector2(0f, 1f);
            destHeader.rectTransform.anchorMax = new Vector2(1f, 1f);
            destHeader.rectTransform.pivot = new Vector2(0.5f, 1f);
            destHeader.rectTransform.anchoredPosition = new Vector2(0f, -2f);
            destHeader.rectTransform.sizeDelta = new Vector2(0f, 20f);

            distanceText = hud.CreateText("DistLabel", parent, "Distancia restante: 200 m (via alterna)", 13, Color.white, TextAnchor.MiddleLeft);
            distanceText.rectTransform.anchorMin = new Vector2(0f, 1f);
            distanceText.rectTransform.anchorMax = new Vector2(1f, 1f);
            distanceText.rectTransform.pivot = new Vector2(0.5f, 1f);
            distanceText.rectTransform.anchoredPosition = new Vector2(0f, -22f);
            distanceText.rectTransform.sizeDelta = new Vector2(0f, 20f);

            // Mini-mapa tactico 2D en vivo
            BuildCityMapTexture();
            var mapGo = new GameObject("CityMiniMap");
            var mapRt = mapGo.AddComponent<RectTransform>();
            mapRt.SetParent(parent, false);
            mapRt.anchorMin = new Vector2(0.5f, 1f);
            mapRt.anchorMax = new Vector2(0.5f, 1f);
            mapRt.pivot = new Vector2(0.5f, 1f);
            mapRt.anchoredPosition = new Vector2(0f, -48f);
            mapRt.sizeDelta = new Vector2(MapW, MapH);
            mapImage = mapGo.AddComponent<RawImage>();
            mapImage.texture = mapTexture;
            mapImage.raycastTarget = false;

            // Pin fijo de destino (Floreria El Jazmin)
            var destPin = hud.CreatePanel("DestPin", mapRt, new Color(0.22f, 0.92f, 0.45f, 1f));
            destPin.rectTransform.anchorMin = Vector2.zero;
            destPin.rectTransform.anchorMax = Vector2.zero;
            destPin.rectTransform.sizeDelta = new Vector2(11f, 11f);
            destPin.rectTransform.anchoredPosition = WorldToMapPx(new Vector3(94f, 0f, 15f));
            destPin.raycastTarget = false;

            // Flecha viva del jugador
            var playerDot = hud.CreatePanel("PlayerDot", mapRt, new Color(0.25f, 0.95f, 1f, 1f));
            playerArrowRt = playerDot.rectTransform;
            playerArrowRt.anchorMin = Vector2.zero;
            playerArrowRt.anchorMax = Vector2.zero;
            playerArrowRt.sizeDelta = new Vector2(10f, 10f);
            playerDot.raycastTarget = false;

            var nose = hud.CreatePanel("HeadingTip", playerArrowRt, Color.white);
            nose.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            nose.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            nose.rectTransform.pivot = new Vector2(0.5f, 0f);
            nose.rectTransform.anchoredPosition = Vector2.zero;
            nose.rectTransform.sizeDelta = new Vector2(4f, 8f);
            nose.raycastTarget = false;

            // Tarjeta Ruta A
            var cardA = hud.CreatePanel("CardRutaA", parent, new Color(0.20f, 0.14f, 0.16f, 0.94f));
            cardA.rectTransform.anchorMin = new Vector2(0f, 1f);
            cardA.rectTransform.anchorMax = new Vector2(1f, 1f);
            cardA.rectTransform.pivot = new Vector2(0.5f, 1f);
            cardA.rectTransform.anchoredPosition = new Vector2(0f, -298f);
            cardA.rectTransform.sizeDelta = new Vector2(0f, 78f);
            cardA.raycastTarget = false;

            var tA = hud.CreateText("TitleA", cardA.rectTransform, "Ruta A: Av. Juarez (Directa)", 13, new Color(1f, 0.56f, 0.25f), TextAnchor.MiddleLeft, FontStyle.Bold);
            tA.rectTransform.anchorMin = new Vector2(0f, 1f);
            tA.rectTransform.anchorMax = new Vector2(1f, 1f);
            tA.rectTransform.pivot = new Vector2(0.5f, 1f);
            tA.rectTransform.anchoredPosition = new Vector2(0f, -6f);
            tA.rectTransform.sizeDelta = new Vector2(-18f, 20f);

            routeAStatusText = hud.CreateText("BodyA", cardA.rectTransform, "Estado: CONGESTION / CORTE ACTIVO en Hidalgo\nRiesgo: ALTO", 12, Color.white, TextAnchor.UpperLeft);
            routeAStatusText.rectTransform.anchorMin = Vector2.zero;
            routeAStatusText.rectTransform.anchorMax = Vector2.one;
            routeAStatusText.rectTransform.offsetMin = new Vector2(9f, 6f);
            routeAStatusText.rectTransform.offsetMax = new Vector2(-9f, -28f);

            // Tarjeta Ruta B
            var cardB = hud.CreatePanel("CardRutaB", parent, new Color(0.13f, 0.20f, 0.16f, 0.94f));
            cardB.rectTransform.anchorMin = new Vector2(0f, 1f);
            cardB.rectTransform.anchorMax = new Vector2(1f, 1f);
            cardB.rectTransform.pivot = new Vector2(0.5f, 1f);
            cardB.rectTransform.anchoredPosition = new Vector2(0f, -384f);
            cardB.rectTransform.sizeDelta = new Vector2(0f, 82f);
            cardB.raycastTarget = false;

            var tB = hud.CreateText("TitleB", cardB.rectTransform, "Ruta B: Callejon del Refugio -> C. Hospital", 12, new Color(0.35f, 0.90f, 0.48f), TextAnchor.MiddleLeft, FontStyle.Bold);
            tB.rectTransform.anchorMin = new Vector2(0f, 1f);
            tB.rectTransform.anchorMax = new Vector2(1f, 1f);
            tB.rectTransform.pivot = new Vector2(0.5f, 1f);
            tB.rectTransform.anchoredPosition = new Vector2(0f, -6f);
            tB.rectTransform.sizeDelta = new Vector2(-18f, 20f);

            routeBStatusText = hud.CreateText("BodyB", cardB.rectTransform, "Estado: DESPEJADA por callejones laterales\nRiesgo: MINIMO (Recomendada)", 12, Color.white, TextAnchor.UpperLeft);
            routeBStatusText.rectTransform.anchorMin = Vector2.zero;
            routeBStatusText.rectTransform.anchorMax = Vector2.one;
            routeBStatusText.rectTransform.offsetMin = new Vector2(9f, 6f);
            routeBStatusText.rectTransform.offsetMax = new Vector2(-9f, -28f);
        }

        private void BuildEcoUrbanoContent(TacticalHUDCanvas hud, RectTransform parent)
        {
            var header = hud.CreateText("EcoHeader", parent, "FEED CIVICO EN TIEMPO REAL - GDL CENTRO", 13, new Color(1f, 0.58f, 0.26f), TextAnchor.MiddleLeft, FontStyle.Bold);
            header.rectTransform.anchorMin = new Vector2(0f, 1f);
            header.rectTransform.anchorMax = new Vector2(1f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -4f);
            header.rectTransform.sizeDelta = new Vector2(0f, 22f);

            RebuildFeedCards();
        }

        private void RebuildFeedCards()
        {
            if (ecoContent == null) return;
            var hud = TacticalHUDCanvas.GetOrCreate();
            if (hud == null) return;

            for (int i = 0; i < feedCardObjects.Count; i++)
            {
                if (feedCardObjects[i] != null) Destroy(feedCardObjects[i]);
            }
            feedCardObjects.Clear();

            float y = -32f;
            int maxCards = Mathf.Min(4, feedPosts.Count);
            for (int i = 0; i < maxCards; i++)
            {
                var post = feedPosts[i];
                var card = hud.CreatePanel($"FeedCard_{i}", ecoContent.transform, new Color(0.15f, 0.17f, 0.21f, 0.92f));
                card.rectTransform.anchorMin = new Vector2(0f, 1f);
                card.rectTransform.anchorMax = new Vector2(1f, 1f);
                card.rectTransform.pivot = new Vector2(0.5f, 1f);
                card.rectTransform.anchoredPosition = new Vector2(0f, y);
                card.rectTransform.sizeDelta = new Vector2(0f, 96f);
                card.raycastTarget = false;

                var accent = hud.CreatePanel("Accent", card.rectTransform, post.Accent);
                accent.rectTransform.anchorMin = new Vector2(0f, 0f);
                accent.rectTransform.anchorMax = new Vector2(0f, 1f);
                accent.rectTransform.pivot = new Vector2(0f, 0.5f);
                accent.rectTransform.anchoredPosition = Vector2.zero;
                accent.rectTransform.sizeDelta = new Vector2(4f, 0f);
                accent.raycastTarget = false;

                var meta = hud.CreateText("Meta", card.rectTransform, post.Meta, 11, new Color(0.65f, 0.70f, 0.78f), TextAnchor.MiddleLeft, FontStyle.Italic);
                meta.rectTransform.anchorMin = new Vector2(0f, 1f);
                meta.rectTransform.anchorMax = new Vector2(1f, 1f);
                meta.rectTransform.pivot = new Vector2(0.5f, 1f);
                meta.rectTransform.anchoredPosition = new Vector2(0f, -6f);
                meta.rectTransform.sizeDelta = new Vector2(-20f, 18f);

                var body = hud.CreateText("Body", card.rectTransform, post.Body, 13, new Color(0.94f, 0.94f, 0.96f), TextAnchor.UpperLeft);
                body.rectTransform.anchorMin = Vector2.zero;
                body.rectTransform.anchorMax = Vector2.one;
                body.rectTransform.offsetMin = new Vector2(12f, 6f);
                body.rectTransform.offsetMax = new Vector2(-10f, -26f);

                feedCardObjects.Add(card.gameObject);
                y -= 104f;
            }
        }

        private void BuildCityMapTexture()
        {
            mapTexture = new Texture2D(MapW, MapH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32 bg = new Color32(15, 20, 27, 255);
            Color32 blockCol = new Color32(24, 30, 39, 255);
            Color32 roadCol = new Color32(58, 66, 78, 255);
            Color32 alleyCol = new Color32(72, 76, 84, 255);
            Color32 detourCol = new Color32(45, 210, 235, 255);
            Color32 redBlock = new Color32(230, 52, 52, 255);
            Color32 workOrange = new Color32(238, 130, 32, 255);

            Color32[] px = new Color32[MapW * MapH];
            for (int i = 0; i < px.Length; i++) px[i] = bg;

            // Cuadricula sutil de manzanas
            for (int y = 0; y < MapH; y++)
            {
                for (int x = 0; x < MapW; x++)
                {
                    if (x % 32 == 0 || y % 32 == 0) px[y * MapW + x] = blockCol;
                }
            }

            // Vias principales y callejones en escala real (2.5 px/m)
            PaintWorldRect(px, -5f, 5f, 0f, 96f, roadCol);        // Av. Juarez
            PaintWorldRect(px, -22f, 37f, 84.5f, 91.5f, roadCol); // Av. Hidalgo
            PaintWorldRect(px, 47.5f, 52.5f, 40f, 96f, roadCol);  // Calle Galeana
            PaintWorldRect(px, 18f, 95f, 10f, 20f, roadCol);      // Calle Hospital
            PaintWorldRect(px, 5f, 47.5f, 70f, 75f, alleyCol);    // Callejon 1
            PaintWorldRect(px, 27f, 32f, 20f, 72.5f, alleyCol);   // Callejon 2

            // Trazado cian de la ruta recomendada
            PaintWorldRect(px, -0.8f, 0.8f, 22f, 72.5f, detourCol);
            PaintWorldRect(px, 0f, 29.5f, 71.7f, 73.3f, detourCol);
            PaintWorldRect(px, 28.7f, 30.3f, 15f, 72.5f, detourCol);
            PaintWorldRect(px, 29.5f, 92.5f, 14.2f, 15.8f, detourCol);

            // Cierres activos en rojo y obra en naranja
            PaintWorldRect(px, -6.5f, 6.5f, 78.6f, 81.2f, redBlock);   // Barricada Juarez
            PaintWorldRect(px, 45.8f, 48.2f, 69.5f, 75.5f, redBlock);  // Policia Galeana
            PaintWorldRect(px, 20.0f, 22.0f, 10.0f, 20.0f, workOrange); // Obra Hospital oeste

            mapTexture.SetPixels32(px);
            mapTexture.Apply();
        }

        private static void PaintWorldRect(Color32[] px, float x0, float x1, float z0, float z1, Color32 col)
        {
            Vector2 p0 = WorldToMapPx(new Vector3(x0, 0f, z0));
            Vector2 p1 = WorldToMapPx(new Vector3(x1, 0f, z1));
            int ix0 = Mathf.Clamp(Mathf.RoundToInt(p0.x), 0, MapW - 1);
            int ix1 = Mathf.Clamp(Mathf.RoundToInt(p1.x), 0, MapW - 1);
            int iy0 = Mathf.Clamp(Mathf.RoundToInt(p0.y), 0, MapH - 1);
            int iy1 = Mathf.Clamp(Mathf.RoundToInt(p1.y), 0, MapH - 1);

            for (int y = iy0; y <= iy1; y++)
            {
                for (int x = ix0; x <= ix1; x++)
                {
                    px[y * MapW + x] = col;
                }
            }
        }

        private static Vector2 WorldToMapPx(Vector3 worldPos)
        {
            float u = Mathf.Clamp01((worldPos.x - MapXMin) / (MapXMax - MapXMin));
            float v = Mathf.Clamp01((worldPos.z - MapZMin) / (MapZMax - MapZMin));
            return new Vector2(u * MapW, v * MapH);
        }

        private void UpdateLiveNavigation()
        {
            if (playerTransform == null)
            {
                var playerCtrl = FindFirstObjectByType<Player.PlayerController>();
                if (playerCtrl != null) playerTransform = playerCtrl.transform;
                if (playerTransform == null) return;
            }

            Vector3 pos = playerTransform.position;
            if (playerArrowRt != null)
            {
                playerArrowRt.anchoredPosition = WorldToMapPx(pos);
                playerArrowRt.localRotation = Quaternion.Euler(0f, 0f, -playerTransform.eulerAngles.y);
            }

            if (distanceText != null)
            {
                float rem = ComputeRemainingRouteMeters(pos);
                distanceText.text = $"Distancia restante: {rem:F0} m (via alterna)";
            }
        }

        private static float ComputeRemainingRouteMeters(Vector3 pos)
        {
            Vector2 p = new Vector2(pos.x, pos.z);
            Vector2 w1 = new Vector2(0f, 72.5f);
            Vector2 w2 = new Vector2(29.5f, 72.5f);
            Vector2 w3 = new Vector2(29.5f, 15f);
            Vector2 w4 = new Vector2(92.5f, 15f);

            if (pos.x < 10f)
            {
                return Vector2.Distance(p, w1) + 29.5f + 57.5f + 63.0f;
            }
            if (pos.z > 66f && pos.x >= 10f)
            {
                return Vector2.Distance(p, w2) + 57.5f + 63.0f;
            }
            if (pos.z >= 21f && pos.x >= 22f && pos.x <= 38f)
            {
                return Vector2.Distance(p, w3) + 63.0f;
            }
            return Vector2.Distance(p, w4);
        }

        private void SwitchTab(PhoneApp app)
        {
            currentApp = app;
            if (app == PhoneApp.EcoUrbano)
            {
                hasUnreadAlert = false;
                RefreshTopBar();
            }

            if (rutaContent != null) rutaContent.SetActive(app == PhoneApp.RutaLibre);
            if (ecoContent != null) ecoContent.SetActive(app == PhoneApp.EcoUrbano);

            if (rutaTabBg != null)
            {
                rutaTabBg.color = app == PhoneApp.RutaLibre
                    ? new Color(0.18f, 0.44f, 0.80f)
                    : new Color(0.16f, 0.18f, 0.22f);
            }
            if (ecoTabBg != null)
            {
                ecoTabBg.color = app == PhoneApp.EcoUrbano
                    ? new Color(0.84f, 0.40f, 0.14f)
                    : new Color(0.16f, 0.18f, 0.22f);
            }
        }

        private void SyncOpenState(bool applyCursor)
        {
            if (phoneFrame != null)
            {
                phoneFrame.SetActive(isOpen);
            }
            RefreshTopBar();

            if (!applyCursor) return;

            if (isOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void RefreshTopBar()
        {
            if (topBarText != null)
            {
                string toggleLabel = isOpen ? "[TAB] CERRAR" : "[TAB] DISPOSITIVO";
                topBarText.text = $"{toggleLabel}  |  [E] ESCANEAR  |  [V] CAMARA  |  [RMB] ZOOM  |  FLORERIA EL JAZMIN";
            }
            if (topBarBadge != null)
            {
                topBarBadge.SetActive(hasUnreadAlert);
            }
        }
    }
}
