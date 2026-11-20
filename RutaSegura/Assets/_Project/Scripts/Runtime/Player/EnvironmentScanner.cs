using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using RutaSegura.Runtime.Audio;
using RutaSegura.Runtime.Core;
using RutaSegura.Runtime.Navigation;
using RutaSegura.Runtime.UI;

namespace RutaSegura.Runtime.Player
{
    /// <summary>
    /// Mecanica activa 'Leyendo Entorno...' inspirada en el video de referencia:
    /// Mantener [E] o [Q] ralentiza el paso, estrecha sutilmente el FOV, llena un anillo radial
    /// y revela rutas seguras o analiza riesgos urbanos cercanos.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [AddComponentMenu("Ruta Segura/Player/Environment Scanner")]
    public class EnvironmentScanner : MonoBehaviour
    {
        [Header("Calibracion de Escaneo")]
        [SerializeField] private float scanDuration = 1.35f;
        [SerializeField] private float speedMultiplierWhileScanning = 0.55f;
        [SerializeField] private float fovZoomDelta = -5f;
        [SerializeField] private float revealRange = 45f;
        [SerializeField] private float resultDisplayDuration = 4.5f;

        private PlayerController player;
        private PlayerCameraController cameraController;
        private Camera playerCamera;
        private AudioSource audioSource;
        private float baseFov = 60f;

        private float scanProgress;
        private float tickTimer;
        private bool scanCompletedThisHold;
        private float resultTimer;

        // UI uGUI
        private GameObject scanRingGroup;
        private Image radialFillImage;
        private Text scanLabelText;
        private GameObject resultCardGroup;
        private Text resultTitleText;
        private Text resultBodyText;

        private RectTransform hudRootRt;
        private PoiCallout[] poiCallouts;
        private float poiTimer;

        private struct PoiCallout
        {
            public string Label;
            public Vector3 WorldPos;
            public GameObject RootObj;
            public RectTransform Rt;
            public Text LabelText;
        }

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            cameraController = GetComponent<PlayerCameraController>();
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                baseFov = playerCamera.fieldOfView;
            }

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        private void Start()
        {
            BuildUI();
        }

        private void OnDisable()
        {
            if (player != null) player.SetExternalSpeedMultiplier(1f);
            if (cameraController != null) cameraController.SetScannerFovOffset(0f);
            else if (playerCamera != null) playerCamera.fieldOfView = baseFov;
        }

        private void BuildUI()
        {
            var hud = TacticalHUDCanvas.GetOrCreate();
            if (hud == null || hud.Root == null) return;

            // Anillo centrado 'LEYENDO ENTORNO...'
            scanRingGroup = new GameObject("Scanner_RadialRing");
            var ringRootRt = scanRingGroup.AddComponent<RectTransform>();
            ringRootRt.SetParent(hud.Root, false);
            ringRootRt.anchorMin = new Vector2(0.5f, 0.5f);
            ringRootRt.anchorMax = new Vector2(0.5f, 0.5f);
            ringRootRt.anchoredPosition = Vector2.zero;
            ringRootRt.sizeDelta = new Vector2(260f, 160f);

            Sprite ringSprite = TacticalHUDCanvas.CreateRingSprite(128, 0.72f, 0.90f);

            var bgRingGo = new GameObject("RingBg");
            var bgRingRt = bgRingGo.AddComponent<RectTransform>();
            bgRingRt.SetParent(ringRootRt, false);
            bgRingRt.anchoredPosition = new Vector2(0f, 16f);
            bgRingRt.sizeDelta = new Vector2(78f, 78f);
            var bgRingImg = bgRingGo.AddComponent<Image>();
            bgRingImg.sprite = ringSprite;
            bgRingImg.color = new Color(0.12f, 0.25f, 0.30f, 0.55f);
            bgRingImg.raycastTarget = false;

            var fillRingGo = new GameObject("RingFill");
            var fillRingRt = fillRingGo.AddComponent<RectTransform>();
            fillRingRt.SetParent(ringRootRt, false);
            fillRingRt.anchoredPosition = new Vector2(0f, 16f);
            fillRingRt.sizeDelta = new Vector2(78f, 78f);
            radialFillImage = fillRingGo.AddComponent<Image>();
            radialFillImage.sprite = ringSprite;
            radialFillImage.color = new Color(0.22f, 0.92f, 1f, 0.95f);
            radialFillImage.type = Image.Type.Filled;
            radialFillImage.fillMethod = Image.FillMethod.Radial360;
            radialFillImage.fillOrigin = (int)Image.Origin360.Top;
            radialFillImage.fillClockwise = true;
            radialFillImage.fillAmount = 0f;
            radialFillImage.raycastTarget = false;

            scanLabelText = hud.CreateText("ScanLabel", ringRootRt, "LEYENDO ENTORNO... 0%", 15, new Color(0.85f, 0.97f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            scanLabelText.rectTransform.anchoredPosition = new Vector2(0f, -44f);
            scanLabelText.rectTransform.sizeDelta = new Vector2(260f, 34f);

            scanRingGroup.SetActive(false);

            // Tarjeta de resultado abajo del centro
            var cardImg = hud.CreatePanel("Scanner_ResultCard", hud.Root, new Color(0.05f, 0.08f, 0.12f, 0.92f));
            var cardRt = cardImg.rectTransform;
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = new Vector2(0f, -185f);
            cardRt.sizeDelta = new Vector2(540f, 86f);
            cardImg.raycastTarget = false;

            var borderImg = hud.CreatePanel("TopAccent", cardRt, new Color(0.20f, 0.90f, 1f, 0.95f));
            borderImg.rectTransform.anchorMin = new Vector2(0f, 1f);
            borderImg.rectTransform.anchorMax = new Vector2(1f, 1f);
            borderImg.rectTransform.pivot = new Vector2(0.5f, 1f);
            borderImg.rectTransform.anchoredPosition = Vector2.zero;
            borderImg.rectTransform.sizeDelta = new Vector2(0f, 3f);
            borderImg.raycastTarget = false;

            resultTitleText = hud.CreateText("ResultTitle", cardRt, "", 14, new Color(0.30f, 0.94f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            resultTitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            resultTitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            resultTitleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            resultTitleText.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            resultTitleText.rectTransform.sizeDelta = new Vector2(-28f, 22f);

            resultBodyText = hud.CreateText("ResultBody", cardRt, "", 15, Color.white, TextAnchor.UpperLeft, FontStyle.Normal);
            resultBodyText.rectTransform.anchorMin = new Vector2(0f, 0f);
            resultBodyText.rectTransform.anchorMax = new Vector2(1f, 1f);
            resultBodyText.rectTransform.offsetMin = new Vector2(14f, 8f);
            resultBodyText.rectTransform.offsetMax = new Vector2(-14f, -32f);

            resultCardGroup = cardImg.gameObject;
            resultCardGroup.SetActive(false);

            BuildPoiCallouts(hud);
        }

        private void BuildPoiCallouts(TacticalHUDCanvas hud)
        {
            hudRootRt = hud.Root;
            Vector3 floreriaPos = new Vector3(77f, 3.4f, 15f);
            var destObj = GameObject.Find("Destino_Floreria");
            if (destObj != null)
            {
                floreriaPos = destObj.transform.position + Vector3.up * 2.8f;
            }

            Vector3 bloqueoPos = new Vector3(0f, 3.2f, 86f);
            var cierreObj = GameObject.Find("Trigger_Cierre");
            if (cierreObj != null)
            {
                bloqueoPos = cierreObj.transform.position + new Vector3(0f, 2.6f, 16f);
            }

            var defs = new[]
            {
                ("BLOQUEO EN AV. HIDALGO", bloqueoPos, new Color(0.96f, 0.32f, 0.24f, 0.95f)),
                ("CORDON POLICIAL GALEANA", new Vector3(48.5f, 3.0f, 74f), new Color(0.96f, 0.68f, 0.16f, 0.95f)),
                ("FLORERIA EL JAZMIN", floreriaPos, new Color(0.24f, 0.92f, 0.56f, 0.95f))
            };

            poiCallouts = new PoiCallout[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                var bg = hud.CreatePanel($"Scanner_POI_{i}", hud.Root, new Color(0.05f, 0.08f, 0.12f, 0.88f));
                var rt = bg.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(228f, 44f);
                bg.raycastTarget = false;

                var accent = hud.CreatePanel("Accent", rt, defs[i].Item3);
                accent.rectTransform.anchorMin = new Vector2(0f, 0f);
                accent.rectTransform.anchorMax = new Vector2(0f, 1f);
                accent.rectTransform.pivot = new Vector2(0f, 0.5f);
                accent.rectTransform.anchoredPosition = Vector2.zero;
                accent.rectTransform.sizeDelta = new Vector2(4f, 0f);
                accent.raycastTarget = false;

                var txt = hud.CreateText("Label", rt, defs[i].Item1, 12, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                txt.rectTransform.anchorMin = Vector2.zero;
                txt.rectTransform.anchorMax = Vector2.one;
                txt.rectTransform.offsetMin = new Vector2(10f, 4f);
                txt.rectTransform.offsetMax = new Vector2(-8f, -4f);

                bg.gameObject.SetActive(false);

                poiCallouts[i] = new PoiCallout
                {
                    Label = defs[i].Item1,
                    WorldPos = defs[i].Item2,
                    RootObj = bg.gameObject,
                    Rt = rt,
                    LabelText = txt
                };
            }
        }

        private void Update()
        {
            if (resultTimer > 0f)
            {
                resultTimer -= Time.deltaTime;
                if (resultTimer <= 0f && resultCardGroup != null)
                {
                    resultCardGroup.SetActive(false);
                }
            }

            bool blocked = (PhoneHUD.Instance != null && PhoneHUD.Instance.IsOpen) ||
                           (GameLoopManager.Instance != null && (GameLoopManager.Instance.IsPaused || GameLoopManager.Instance.CurrentPhase == GamePhase.Outcome)) ||
                           (player != null && !player.enabled);

            bool holdingKey = !blocked && Keyboard.current != null &&
                              (Keyboard.current.eKey.isPressed || Keyboard.current.qKey.isPressed);

            if (holdingKey)
            {
                player?.SetExternalSpeedMultiplier(speedMultiplierWhileScanning);
                if (cameraController != null)
                {
                    cameraController.SetScannerFovOffset(fovZoomDelta);
                }
                else if (playerCamera != null)
                {
                    playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, baseFov + fovZoomDelta, Time.deltaTime * 8f);
                }

                if (!scanCompletedThisHold)
                {
                    scanProgress = Mathf.Clamp01(scanProgress + Time.deltaTime / Mathf.Max(0.2f, scanDuration));

                    tickTimer += Time.deltaTime;
                    if (tickTimer >= 0.34f)
                    {
                        tickTimer = 0f;
                        PlayClip(ProceduralAudioLibrary.GetScanTickClip(), 0.25f);
                    }

                    if (scanRingGroup != null && !scanRingGroup.activeSelf) scanRingGroup.SetActive(true);
                    if (radialFillImage != null) radialFillImage.fillAmount = scanProgress;
                    if (scanLabelText != null) scanLabelText.text = $"LEYENDO ENTORNO... {Mathf.RoundToInt(scanProgress * 100f)}%";

                    if (scanProgress >= 1f)
                    {
                        scanCompletedThisHold = true;
                        if (scanRingGroup != null) scanRingGroup.SetActive(false);
                        CompleteScan();
                    }
                }
            }
            else
            {
                scanProgress = Mathf.MoveTowards(scanProgress, 0f, Time.deltaTime * 4f);
                tickTimer = 0f;
                scanCompletedThisHold = false;
                player?.SetExternalSpeedMultiplier(1f);

                if (cameraController != null)
                {
                    cameraController.SetScannerFovOffset(0f);
                }
                else if (playerCamera != null)
                {
                    playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, baseFov, Time.deltaTime * 8f);
                }
                if (scanRingGroup != null && scanRingGroup.activeSelf)
                {
                    scanRingGroup.SetActive(false);
                }
            }
        }

        private void CompleteScan()
        {
            PlayClip(ProceduralAudioLibrary.GetScanCompleteClip(), 0.42f);

            // Revelar marcadores de ruta cercanos al frente del jugador
            var markers = FindObjectsByType<AlternativeRouteMarker>(FindObjectsSortMode.None);
            Vector3 fwd = transform.forward;
            Vector3 origin = transform.position;

            foreach (var marker in markers)
            {
                if (marker == null) continue;
                Vector3 toMarker = marker.transform.position - origin;
                float dist = toMarker.magnitude;
                if (dist <= revealRange && Vector3.Dot(fwd, toMarker.normalized) > 0.1f)
                {
                    marker.ActivateMarker();
                }
            }

            EvaluateContext(origin, out string title, out string body);
            if (resultTitleText != null) resultTitleText.text = title;
            if (resultBodyText != null) resultBodyText.text = body;
            if (resultCardGroup != null) resultCardGroup.SetActive(true);
            resultTimer = resultDisplayDuration;
            poiTimer = 6.5f;
        }

        private void LateUpdate()
        {
            if (poiCallouts == null) return;

            bool pausedOrOutcome = GameLoopManager.Instance != null &&
                                   (GameLoopManager.Instance.IsPaused || GameLoopManager.Instance.CurrentPhase == GamePhase.Outcome);

            if (poiTimer <= 0f || pausedOrOutcome)
            {
                for (int i = 0; i < poiCallouts.Length; i++)
                {
                    if (poiCallouts[i].RootObj != null && poiCallouts[i].RootObj.activeSelf)
                    {
                        poiCallouts[i].RootObj.SetActive(false);
                    }
                }
                return;
            }

            poiTimer -= Time.deltaTime;
            Camera cam = Camera.main != null ? Camera.main : playerCamera;
            if (cam == null || hudRootRt == null) return;

            for (int i = 0; i < poiCallouts.Length; i++)
            {
                var poi = poiCallouts[i];
                if (poi.RootObj == null) continue;

                float dist = Vector3.Distance(cam.transform.position, poi.WorldPos);
                Vector3 sp = cam.WorldToScreenPoint(poi.WorldPos);

                bool visible = dist <= 85f &&
                               sp.z > 0.5f &&
                               sp.x >= 20f && sp.x <= Screen.width - 20f &&
                               sp.y >= 20f && sp.y <= Screen.height - 20f;

                poi.RootObj.SetActive(visible);
                if (visible)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(hudRootRt, sp, null, out Vector2 localPos);
                    poi.Rt.anchoredPosition = localPos;
                    poi.LabelText.text = $"{poi.Label}\n{Mathf.RoundToInt(dist)} m";
                }
            }
        }

        private static void EvaluateContext(Vector3 pos, out string title, out string body)
        {
            if (pos.x < 10f && pos.z > 50f && pos.z < 85f)
            {
                title = "LECTURA DE ENTORNO: BLOQUEO EN AV. HIDALGO";
                body = "Vallas y contingente cierran el paso al frente. Callejón del Refugio despejado a tu derecha.";
            }
            else if (pos.x >= 10f && pos.x <= 45f && pos.z >= 65f)
            {
                title = "LECTURA DE ENTORNO: CORDON POLICIAL ADELANTE";
                body = "Calle Galeana cerrada por operativo. Gira hacia el sur por el segundo callejón.";
            }
            else if (pos.x >= 22f && pos.x <= 38f && pos.z >= 20f && pos.z < 65f)
            {
                title = "LECTURA DE ENTORNO: CORREDOR DE DESVIO";
                body = "Tránsito peatonal bajo. Continúa hacia el sur para enlazar con Calle Hospital.";
            }
            else if (pos.z < 22f && pos.x >= 20f)
            {
                title = "LECTURA DE ENTORNO: APROXIMACION FINAL";
                body = "Acordonamiento menor en extremo oeste. Sigue al este hacia Florería El Jazmín.";
            }
            else
            {
                title = "LECTURA DE ENTORNO: AV. JUAREZ";
                body = "Cortinas cerradas y reportes de concentración adelante. Mantén atención a rutas alternas.";
            }
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }
    }
}
