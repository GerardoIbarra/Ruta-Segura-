using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using RutaSegura.Runtime.Audio;
using RutaSegura.Runtime.Core;
using RutaSegura.Runtime.Player;
using RutaSegura.Runtime.Threat;

namespace RutaSegura.Runtime.UI
{
    /// <summary>
    /// Panel de telemetria tecnica [F3] para QA y perfilado en vivo:
    /// FPS, tiempo de cuadro (ms), fase del ciclo, amenaza/BPM, perspectiva y coordenadas.
    /// </summary>
    [AddComponentMenu("Ruta Segura/UI/Performance Telemetry HUD")]
    public class PerformanceTelemetryHUD : MonoBehaviour
    {
        private GameObject panelGroup;
        private Text telemetryText;

        private bool isVisible;
        private float smoothedDelta = 1f / 60f;
        private float refreshTimer;

        private PlayerController player;
        private PlayerCameraController camController;

        private void Start()
        {
            BuildUI();
            player = FindAnyObjectByType<PlayerController>();
            camController = FindAnyObjectByType<PlayerCameraController>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
            {
                isVisible = !isVisible;
                if (panelGroup != null)
                {
                    panelGroup.SetActive(isVisible);
                }
                if (isVisible)
                {
                    refreshTimer = 0f;
                }
            }

            float dt = Time.unscaledDeltaTime;
            if (dt > 0.0001f && dt < 0.25f)
            {
                smoothedDelta = Mathf.Lerp(smoothedDelta, dt, 0.12f);
            }

            if (!isVisible || telemetryText == null) return;

            refreshTimer -= dt;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.2f;

            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (camController == null) camController = FindAnyObjectByType<PlayerCameraController>();

            float fps = smoothedDelta > 0.0001f ? 1f / smoothedDelta : 0f;
            float ms = smoothedDelta * 1000f;
            string phase = GameLoopManager.Instance != null ? GameLoopManager.Instance.CurrentPhase.ToString() : "N/A";
            float threat = ThreatSystem.Instance != null ? ThreatSystem.Instance.CurrentThreat : 0f;
            float bpm = BiometricAudioSystem.Instance != null ? BiometricAudioSystem.Instance.CurrentBPM : 68f;
            string camMode = camController != null ? (camController.IsThirdPerson ? "3ra Persona" : "1ra Persona") : "1ra Persona";
            float zoom = camController != null ? camController.CurrentZoomMultiplier : 1f;
            Vector3 pos = player != null ? player.transform.position : Vector3.zero;

            telemetryText.text =
                $"QA TELEMETRIA [F3]\n" +
                $"FPS: {fps:F0} ({ms:F1} ms)\n" +
                $"FASE: {phase}  |  AMENAZA: {threat:F0}% ({bpm:F0} BPM)\n" +
                $"CAMARA: {camMode}  |  ZOOM: {zoom:F1}x\n" +
                $"POS: X={pos.x:F1}  Y={pos.y:F2}  Z={pos.z:F1}";
        }

        private void BuildUI()
        {
            var hud = TacticalHUDCanvas.GetOrCreate();
            if (hud == null || hud.Root == null) return;

            var panel = hud.CreatePanel("Telemetry_HUD_Panel", hud.Root, new Color(0.04f, 0.06f, 0.09f, 0.88f));
            var rt = panel.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(30f, -76f);
            rt.sizeDelta = new Vector2(335f, 108f);
            panel.raycastTarget = false;

            var bar = hud.CreatePanel("GreenAccent", rt, new Color(0.32f, 0.92f, 0.56f, 0.90f));
            bar.rectTransform.anchorMin = new Vector2(0f, 0f);
            bar.rectTransform.anchorMax = new Vector2(0f, 1f);
            bar.rectTransform.pivot = new Vector2(0f, 0.5f);
            bar.rectTransform.anchoredPosition = Vector2.zero;
            bar.rectTransform.sizeDelta = new Vector2(4f, 0f);
            bar.raycastTarget = false;

            telemetryText = hud.CreateText("TelemetryText", rt, "QA TELEMETRIA [F3]", 12, new Color(0.84f, 0.95f, 0.88f), TextAnchor.UpperLeft, FontStyle.Bold);
            TacticalHUDCanvas.Stretch(telemetryText.rectTransform, 10f);

            panelGroup = panel.gameObject;
            panelGroup.SetActive(false);
        }
    }
}
