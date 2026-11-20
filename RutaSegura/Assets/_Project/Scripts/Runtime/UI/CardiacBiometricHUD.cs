using UnityEngine;
using UnityEngine.UI;
using RutaSegura.Runtime.Audio;
using RutaSegura.Runtime.Threat;

namespace RutaSegura.Runtime.UI
{
    /// <summary>
    /// HUD biometrico de pulso cardiaco y estado emocional en uGUI (Canvas).
    /// Muestra trazo ECG en vivo sincronizado al latido auditivo, BPM actual y nivel de exposicion.
    /// </summary>
    [AddComponentMenu("Ruta Segura/UI/Cardiac Biometric HUD")]
    public class CardiacBiometricHUD : MonoBehaviour
    {
        [Header("Calibracion de Ritmo Cardiaco")]
        [SerializeField] private float minBPM = 68f;
        [SerializeField] private float maxBPM = 148f;

        [Header("Estilo Visual")]
        [SerializeField] private Vector2 screenOffset = new Vector2(36f, 36f);
        [SerializeField] private Vector2 widgetSize = new Vector2(310f, 106f);

        private float currentBPM = 68f;
        private float beatTimer;
        private float heartIconScale = 1.0f;
        private float ecgCyclePhase;

        private readonly Color colTranquilo = new Color(0.18f, 0.82f, 0.75f);
        private readonly Color colNervioso = new Color(0.95f, 0.78f, 0.22f);
        private readonly Color colAlerta = new Color(0.96f, 0.46f, 0.15f);
        private readonly Color colPanico = new Color(0.94f, 0.20f, 0.20f);

        // uGUI
        private GameObject cardRoot;
        private Image topAccent;
        private Image fillBar;
        private Text bpmText;
        private Text iconText;
        private Text statusText;
        private RawImage ecgImage;

        // Buffer ECG prealojado (cero GC por frame)
        private const int EcgW = 148;
        private const int EcgH = 38;
        private Texture2D ecgTex;
        private Color32[] ecgPixels;
        private int cursorX;
        private int prevY = EcgH / 2;
        private float ecgSampleAccum;

        private void Start()
        {
            BuildUI();
        }

        private void OnDestroy()
        {
            if (ecgTex != null)
            {
                Destroy(ecgTex);
            }
        }

        private void BuildUI()
        {
            var hud = TacticalHUDCanvas.GetOrCreate();
            if (hud == null || hud.Root == null) return;

            var cardImg = hud.CreatePanel("Biometric_Card", hud.Root, new Color(0.05f, 0.08f, 0.11f, 0.90f));
            var rt = cardImg.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = screenOffset;
            rt.sizeDelta = widgetSize;
            cardImg.raycastTarget = false;
            cardRoot = cardImg.gameObject;

            topAccent = hud.CreatePanel("TopAccent", rt, colTranquilo);
            topAccent.rectTransform.anchorMin = new Vector2(0f, 1f);
            topAccent.rectTransform.anchorMax = new Vector2(1f, 1f);
            topAccent.rectTransform.pivot = new Vector2(0.5f, 1f);
            topAccent.rectTransform.anchoredPosition = Vector2.zero;
            topAccent.rectTransform.sizeDelta = new Vector2(0f, 3f);
            topAccent.raycastTarget = false;

            ecgTex = new Texture2D(EcgW, EcgH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            ecgPixels = new Color32[EcgW * EcgH];
            var ecgGo = new GameObject("ECG_Wave");
            var ecgRt = ecgGo.AddComponent<RectTransform>();
            ecgRt.SetParent(rt, false);
            ecgRt.anchorMin = new Vector2(0f, 1f);
            ecgRt.anchorMax = new Vector2(0f, 1f);
            ecgRt.pivot = new Vector2(0f, 1f);
            ecgRt.anchoredPosition = new Vector2(14f, -10f);
            ecgRt.sizeDelta = new Vector2(EcgW, EcgH);
            ecgImage = ecgGo.AddComponent<RawImage>();
            ecgImage.texture = ecgTex;
            ecgImage.raycastTarget = false;

            bpmText = hud.CreateText("BPM_Label", rt, "68 BPM", 15, colTranquilo, TextAnchor.MiddleRight, FontStyle.Bold);
            bpmText.rectTransform.anchorMin = new Vector2(1f, 1f);
            bpmText.rectTransform.anchorMax = new Vector2(1f, 1f);
            bpmText.rectTransform.pivot = new Vector2(1f, 1f);
            bpmText.rectTransform.anchoredPosition = new Vector2(-14f, -12f);
            bpmText.rectTransform.sizeDelta = new Vector2(120f, 28f);

            iconText = hud.CreateText("PulseIcon", rt, "+", 22, colTranquilo, TextAnchor.MiddleCenter, FontStyle.Bold);
            iconText.rectTransform.anchorMin = new Vector2(0f, 0f);
            iconText.rectTransform.anchorMax = new Vector2(0f, 0f);
            iconText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            iconText.rectTransform.anchoredPosition = new Vector2(26f, 36f);
            iconText.rectTransform.sizeDelta = new Vector2(30f, 30f);

            statusText = hud.CreateText("StatusLabel", rt, "Tranquilo", 24, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            statusText.rectTransform.anchorMin = new Vector2(0f, 0f);
            statusText.rectTransform.anchorMax = new Vector2(1f, 0f);
            statusText.rectTransform.pivot = new Vector2(0f, 0f);
            statusText.rectTransform.anchoredPosition = new Vector2(46f, 20f);
            statusText.rectTransform.sizeDelta = new Vector2(-60f, 34f);

            var barBg = hud.CreatePanel("BarBg", rt, new Color(0.18f, 0.20f, 0.25f, 0.85f));
            barBg.rectTransform.anchorMin = new Vector2(0f, 0f);
            barBg.rectTransform.anchorMax = new Vector2(1f, 0f);
            barBg.rectTransform.pivot = new Vector2(0.5f, 0f);
            barBg.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            barBg.rectTransform.sizeDelta = new Vector2(-28f, 5f);
            barBg.raycastTarget = false;

            fillBar = hud.CreatePanel("BarFill", barBg.rectTransform, colTranquilo);
            fillBar.rectTransform.anchorMin = new Vector2(0f, 0f);
            fillBar.rectTransform.anchorMax = new Vector2(0f, 1f);
            fillBar.rectTransform.pivot = new Vector2(0f, 0.5f);
            fillBar.rectTransform.anchoredPosition = Vector2.zero;
            fillBar.rectTransform.sizeDelta = Vector2.zero;
            fillBar.raycastTarget = false;
        }

        private void Update()
        {
            if (cardRoot == null) return;

            bool failed = ThreatSystem.Instance != null && ThreatSystem.Instance.IsMissionFailed;
            if (cardRoot.activeSelf == failed)
            {
                cardRoot.SetActive(!failed);
            }
            if (failed) return;

            float threat = ThreatSystem.Instance != null ? ThreatSystem.Instance.CurrentThreat : 0f;
            float threatNorm = Mathf.Clamp01(threat / 100f);

            if (BiometricAudioSystem.Instance != null)
            {
                currentBPM = BiometricAudioSystem.Instance.CurrentBPM;
                float beatImpulse = BiometricAudioSystem.Instance.BeatPulse;
                heartIconScale = 1f + beatImpulse * 0.28f;
            }
            else
            {
                float targetBPM = Mathf.Lerp(minBPM, maxBPM, threatNorm);
                currentBPM = Mathf.Lerp(currentBPM, targetBPM, Time.deltaTime * 3.5f);
                float spb = 60f / Mathf.Max(40f, currentBPM);
                beatTimer += Time.deltaTime;
                if (beatTimer >= spb)
                {
                    beatTimer -= spb;
                    heartIconScale = 1.28f;
                }
                heartIconScale = Mathf.Lerp(heartIconScale, 1.0f, Time.deltaTime * 9f);
            }

            float secondsPerBeat = 60f / Mathf.Max(40f, currentBPM);
            ecgCyclePhase = (ecgCyclePhase + Time.deltaTime / secondsPerBeat) % 1f;

            ThreatLevel level = ThreatSystem.Instance != null ? ThreatSystem.Instance.CurrentLevel : ThreatLevel.Low;
            ResolveStatusVisual(level, out string label, out Color col);

            topAccent.color = new Color(col.r, col.g, col.b, 0.85f);
            bpmText.color = col;
            bpmText.text = $"{currentBPM:F0} BPM";
            iconText.color = col;
            iconText.rectTransform.localScale = Vector3.one * heartIconScale;
            statusText.text = label;
            fillBar.color = col;
            fillBar.rectTransform.anchorMax = new Vector2(Mathf.Max(0.02f, threatNorm), 1f);

            AdvanceEcgWave(col);
        }

        private void ResolveStatusVisual(ThreatLevel level, out string text, out Color col)
        {
            switch (level)
            {
                case ThreatLevel.Low:
                    text = "Tranquilo";
                    col = colTranquilo;
                    break;
                case ThreatLevel.Moderate:
                    text = "Nervioso";
                    col = colNervioso;
                    break;
                case ThreatLevel.High:
                    text = "Alerta";
                    col = colAlerta;
                    break;
                default:
                    text = "Panico";
                    col = colPanico;
                    break;
            }
        }

        private void AdvanceEcgWave(Color col)
        {
            if (ecgTex == null) return;

            ecgSampleAccum += Time.deltaTime * 68f;
            int steps = Mathf.Min(4, Mathf.FloorToInt(ecgSampleAccum));
            if (steps <= 0) return;
            ecgSampleAccum -= steps;

            Color32 lineCol = new Color32(
                (byte)(col.r * 255f),
                (byte)(col.g * 255f),
                (byte)(col.b * 255f),
                255);
            Color32 clearCol = new Color32(0, 0, 0, 0);

            for (int s = 0; s < steps; s++)
            {
                cursorX = (cursorX + 1) % EcgW;

                // Limpiar una franja de 6 px delante del haz
                for (int clearOff = 0; clearOff < 6; clearOff++)
                {
                    int cx = (cursorX + clearOff) % EcgW;
                    for (int y = 0; y < EcgH; y++)
                    {
                        ecgPixels[y * EcgW + cx] = clearCol;
                    }
                }

                int targetY = SampleQrsHeight(ecgCyclePhase);
                int yMin = Mathf.Min(prevY, targetY);
                int yMax = Mathf.Max(prevY, targetY);
                for (int y = yMin; y <= yMax; y++)
                {
                    if (y >= 0 && y < EcgH)
                    {
                        ecgPixels[y * EcgW + cursorX] = lineCol;
                    }
                }
                if (targetY + 1 < EcgH)
                {
                    ecgPixels[(targetY + 1) * EcgW + cursorX] = new Color32(lineCol.r, lineCol.g, lineCol.b, 140);
                }
                prevY = targetY;
            }

            ecgTex.SetPixels32(ecgPixels);
            ecgTex.Apply(false);
        }

        private static int SampleQrsHeight(float p)
        {
            float amp = 0f;
            if (p > 0.10f && p < 0.16f)
            {
                amp = Mathf.Sin((p - 0.10f) / 0.06f * Mathf.PI) * 0.18f; // Onda P
            }
            else if (p >= 0.20f && p < 0.23f)
            {
                amp = -0.22f * Mathf.Sin((p - 0.20f) / 0.03f * Mathf.PI); // Q
            }
            else if (p >= 0.23f && p < 0.28f)
            {
                amp = 0.92f * Mathf.Sin((p - 0.23f) / 0.05f * Mathf.PI); // Pico R
            }
            else if (p >= 0.28f && p < 0.32f)
            {
                amp = -0.35f * Mathf.Sin((p - 0.28f) / 0.04f * Mathf.PI); // S
            }
            else if (p >= 0.42f && p < 0.54f)
            {
                amp = 0.25f * Mathf.Sin((p - 0.42f) / 0.12f * Mathf.PI); // Onda T
            }

            return Mathf.Clamp(Mathf.RoundToInt(EcgH * 0.45f + amp * (EcgH * 0.45f)), 1, EcgH - 2);
        }
    }
}
