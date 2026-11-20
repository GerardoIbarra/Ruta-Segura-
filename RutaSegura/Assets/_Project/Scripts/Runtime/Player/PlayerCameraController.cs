using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using RutaSegura.Runtime.Core;
using RutaSegura.Runtime.UI;

namespace RutaSegura.Runtime.Player
{
    /// <summary>
    /// Controlador unificado de camara para el protagonista:
    /// - Alterna suavemente entre 1ra y 3ra persona (sobre el hombro, anticolision con muros) con [V] / [F5].
    /// - Zoom optico progresivo en 1ra persona manteniendo [Boton Derecho] o [Z] (rueda ajusta 1.4x - 3.0x).
    /// - Coordina FOV con EnvironmentScanner y suaviza la sensibilidad al hacer zoom.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [AddComponentMenu("Ruta Segura/Player/Player Camera Controller")]
    public class PlayerCameraController : MonoBehaviour
    {
        [Header("Vista Primera Persona")]
        [SerializeField] private float firstPersonEyeHeight = 0.60f;
        [SerializeField] private float crouchEyeDrop = 0.22f;

        [Header("Vista Tercera Persona")]
        [SerializeField] private float thirdPersonDistance = 2.75f;
        [SerializeField] private float shoulderRightOffset = 0.42f;
        [SerializeField] private float shoulderUpOffset = 0.18f;
        [SerializeField] private float cameraSphereRadius = 0.22f;
        [SerializeField] private float wallClearance = 0.14f;
        [SerializeField] private float minSafeDistance = 0.45f;
        [SerializeField] private float perspectiveTransitionSpeed = 7f;

        [Header("Zoom en Primera Persona")]
        [SerializeField] private float defaultZoomFov = 28f;
        [SerializeField] private float minZoomFov = 20f;
        [SerializeField] private float maxZoomFov = 42f;
        [SerializeField] private float zoomFovLerpSpeed = 12f;

        private PlayerController player;
        private PlayerAvatarVisual avatarVisual;
        private Camera cam;

        private bool isThirdPerson = false;
        private float thirdPersonBlend = 0f;
        private float baseFov = 60f;
        private float targetZoomFov = 28f;
        private float scannerFovOffset = 0f;
        private float perspectivePillTimer = 0f;

        // UI uGUI
        private GameObject zoomOverlayGroup;
        private Text zoomReadoutText;
        private GameObject perspectivePillGroup;
        private Text perspectivePillText;

        public bool IsThirdPerson => isThirdPerson;
        public float CurrentZoomMultiplier => cam != null && cam.fieldOfView > 1f ? Mathf.Clamp(baseFov / cam.fieldOfView, 1f, 4f) : 1f;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            avatarVisual = GetComponent<PlayerAvatarVisual>() ?? gameObject.AddComponent<PlayerAvatarVisual>();
            cam = player.PlayerCamera != null ? player.PlayerCamera : GetComponentInChildren<Camera>();

            if (cam != null)
            {
                baseFov = cam.fieldOfView;
            }
            targetZoomFov = Mathf.Clamp(defaultZoomFov, minZoomFov, maxZoomFov);
            player.SetExternalCameraControl(true);
        }

        private void OnEnable()
        {
            if (player != null)
            {
                player.SetExternalCameraControl(true);
            }
        }

        private void OnDisable()
        {
            if (player != null)
            {
                player.SetExternalCameraControl(false);
                player.SetExternalLookSensitivityMultiplier(1f);
            }
            if (cam != null)
            {
                cam.fieldOfView = baseFov;
            }
        }

        private void OnDestroy()
        {
            if (player != null)
            {
                player.SetExternalCameraControl(false);
                player.SetExternalLookSensitivityMultiplier(1f);
            }
        }

        private void Start()
        {
            BuildCameraUI();
        }

        public void SetScannerFovOffset(float delta)
        {
            scannerFovOffset = delta;
        }

        private void BuildCameraUI()
        {
            var hud = TacticalHUDCanvas.GetOrCreate();
            if (hud == null || hud.Root == null) return;

            // 1. Pildora breve de cambio de vista encima de la tarjeta biometrica inferior izquierda
            var pill = hud.CreatePanel("Camera_PerspectivePill", hud.Root, new Color(0.05f, 0.08f, 0.12f, 0.92f));
            var pillRt = pill.rectTransform;
            pillRt.anchorMin = Vector2.zero;
            pillRt.anchorMax = Vector2.zero;
            pillRt.pivot = Vector2.zero;
            pillRt.anchoredPosition = new Vector2(36f, 152f);
            pillRt.sizeDelta = new Vector2(236f, 34f);
            pill.raycastTarget = false;

            var pillAccent = hud.CreatePanel("CyanLeft", pillRt, new Color(0.20f, 0.88f, 0.96f, 0.90f));
            pillAccent.rectTransform.anchorMin = new Vector2(0f, 0f);
            pillAccent.rectTransform.anchorMax = new Vector2(0f, 1f);
            pillAccent.rectTransform.pivot = new Vector2(0f, 0.5f);
            pillAccent.rectTransform.anchoredPosition = Vector2.zero;
            pillAccent.rectTransform.sizeDelta = new Vector2(3f, 0f);
            pillAccent.raycastTarget = false;

            perspectivePillText = hud.CreateText("PerspectiveLabel", pillRt, "VISTA: 1RA PERSONA", 13, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            perspectivePillText.rectTransform.anchorMin = Vector2.zero;
            perspectivePillText.rectTransform.anchorMax = Vector2.one;
            perspectivePillText.rectTransform.offsetMin = new Vector2(12f, 0f);
            perspectivePillText.rectTransform.offsetMax = new Vector2(-10f, 0f);

            perspectivePillGroup = pill.gameObject;
            perspectivePillGroup.SetActive(false);

            // 2. Mira minima cian de Zoom en 1ra persona
            var overlayRect = TacticalHUDCanvas.CreateRect("Camera_ZoomViewfinder", hud.Root);
            TacticalHUDCanvas.Stretch(overlayRect);

            Color tickCol = new Color(0.22f, 0.92f, 1.0f, 0.65f);
            CreateCornerTick(hud, overlayRect, new Vector2(0.32f, 0.68f), true, true, tickCol);
            CreateCornerTick(hud, overlayRect, new Vector2(0.68f, 0.68f), false, true, tickCol);
            CreateCornerTick(hud, overlayRect, new Vector2(0.32f, 0.32f), true, false, tickCol);
            CreateCornerTick(hud, overlayRect, new Vector2(0.68f, 0.32f), false, false, tickCol);

            var zoomBadge = hud.CreatePanel("ZoomReadoutBg", overlayRect, new Color(0.05f, 0.08f, 0.12f, 0.85f));
            zoomBadge.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            zoomBadge.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            zoomBadge.rectTransform.pivot = new Vector2(0.5f, 0f);
            zoomBadge.rectTransform.anchoredPosition = new Vector2(0f, 38f);
            zoomBadge.rectTransform.sizeDelta = new Vector2(300f, 30f);
            zoomBadge.raycastTarget = false;

            zoomReadoutText = hud.CreateText("ZoomReadoutText", zoomBadge.rectTransform, "ZOOM 2.1x  |  RUEDA PARA AJUSTAR", 13, new Color(0.80f, 0.96f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            TacticalHUDCanvas.Stretch(zoomReadoutText.rectTransform, 6f);

            zoomOverlayGroup = overlayRect.gameObject;
            zoomOverlayGroup.SetActive(false);
        }

        private static void CreateCornerTick(TacticalHUDCanvas hud, RectTransform parent, Vector2 anchor, bool leftSide, bool topSide, Color col)
        {
            var hBar = hud.CreatePanel("TickH", parent, col);
            hBar.rectTransform.anchorMin = anchor;
            hBar.rectTransform.anchorMax = anchor;
            hBar.rectTransform.pivot = new Vector2(leftSide ? 0f : 1f, topSide ? 1f : 0f);
            hBar.rectTransform.anchoredPosition = Vector2.zero;
            hBar.rectTransform.sizeDelta = new Vector2(22f, 2f);
            hBar.raycastTarget = false;

            var vBar = hud.CreatePanel("TickV", parent, col);
            vBar.rectTransform.anchorMin = anchor;
            vBar.rectTransform.anchorMax = anchor;
            vBar.rectTransform.pivot = new Vector2(leftSide ? 0f : 1f, topSide ? 1f : 0f);
            vBar.rectTransform.anchoredPosition = Vector2.zero;
            vBar.rectTransform.sizeDelta = new Vector2(2f, 22f);
            vBar.raycastTarget = false;
        }

        private void Update()
        {
            bool blocked = (PhoneHUD.Instance != null && PhoneHUD.Instance.IsOpen) ||
                           (GameLoopManager.Instance != null && (GameLoopManager.Instance.IsPaused || GameLoopManager.Instance.CurrentPhase == GamePhase.Outcome)) ||
                           (player != null && !player.enabled);

            if (!blocked && Keyboard.current != null &&
                (Keyboard.current.vKey.wasPressedThisFrame || Keyboard.current.f5Key.wasPressedThisFrame))
            {
                isThirdPerson = !isThirdPerson;
                perspectivePillTimer = 1.8f;
                if (perspectivePillText != null)
                {
                    perspectivePillText.text = isThirdPerson ? "VISTA: 3RA PERSONA" : "VISTA: 1RA PERSONA";
                }
                if (perspectivePillGroup != null)
                {
                    perspectivePillGroup.SetActive(true);
                }
            }

            if (perspectivePillTimer > 0f)
            {
                perspectivePillTimer -= Time.deltaTime;
                if (perspectivePillTimer <= 0f && perspectivePillGroup != null)
                {
                    perspectivePillGroup.SetActive(false);
                }
            }

            float targetBlend = isThirdPerson ? 1f : 0f;
            thirdPersonBlend = Mathf.MoveTowards(thirdPersonBlend, targetBlend, Time.deltaTime * perspectiveTransitionSpeed);

            UpdateZoomAndFov(blocked);
        }

        private void UpdateZoomAndFov(bool blocked)
        {
            if (cam == null) return;

            bool zoomInput = !blocked && !isThirdPerson && (
                (Mouse.current != null && Mouse.current.rightButton.isPressed) ||
                (Keyboard.current != null && Keyboard.current.zKey.isPressed));

            if (zoomInput && Mouse.current != null)
            {
                float scrollY = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scrollY) > 0.01f)
                {
                    targetZoomFov = Mathf.Clamp(targetZoomFov - Mathf.Sign(scrollY) * 2.5f, minZoomFov, maxZoomFov);
                }
            }

            float desiredBaseFov = zoomInput ? targetZoomFov : baseFov;
            float targetFov = Mathf.Clamp(desiredBaseFov + scannerFovOffset, 16f, 75f);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * zoomFovLerpSpeed);

            if (player != null)
            {
                player.SetExternalLookSensitivityMultiplier(cam.fieldOfView / Mathf.Max(20f, baseFov));
            }

            if (zoomOverlayGroup != null)
            {
                if (zoomOverlayGroup.activeSelf != zoomInput)
                {
                    zoomOverlayGroup.SetActive(zoomInput);
                }
                if (zoomInput && zoomReadoutText != null)
                {
                    float ratio = baseFov / Mathf.Max(10f, cam.fieldOfView);
                    zoomReadoutText.text = $"ZOOM {ratio:F1}x  |  RUEDA PARA AJUSTAR";
                }
            }
        }

        private void LateUpdate()
        {
            if (cam == null || player == null) return;

            float crouchOffset = player.IsCautious ? crouchEyeDrop : 0f;
            Vector3 eyeLocal = new Vector3(0f, firstPersonEyeHeight - crouchOffset, 0f);
            Vector3 eyeWorld = transform.TransformPoint(eyeLocal);

            Quaternion camRot = Quaternion.Euler(player.CameraPitch, transform.eulerAngles.y, 0f);
            cam.transform.rotation = camRot;

            if (thirdPersonBlend <= 0.001f)
            {
                cam.transform.position = eyeWorld;
                avatarVisual?.SetVisibleInCamera(false);
                return;
            }

            Vector3 desiredThirdPerson = eyeWorld
                + transform.right * shoulderRightOffset
                + Vector3.up * shoulderUpOffset
                + camRot * Vector3.back * thirdPersonDistance;

            Vector3 resolvedThirdPerson = ResolveWallCollision(eyeWorld, desiredThirdPerson);
            float t = Mathf.SmoothStep(0f, 1f, thirdPersonBlend);
            Vector3 finalWorldPos = Vector3.Lerp(eyeWorld, resolvedThirdPerson, t);
            cam.transform.position = finalWorldPos;

            float distFromEye = Vector3.Distance(eyeWorld, finalWorldPos);
            avatarVisual?.SetVisibleInCamera(thirdPersonBlend >= 0.18f && distFromEye > 0.65f);
        }

        private Vector3 ResolveWallCollision(Vector3 pivot, Vector3 desiredPos)
        {
            Vector3 delta = desiredPos - pivot;
            float fullDist = delta.magnitude;
            if (fullDist <= 0.01f) return desiredPos;

            Vector3 dir = delta / fullDist;
            RaycastHit[] hits = Physics.SphereCastAll(pivot, cameraSphereRadius, dir, fullDist, ~0, QueryTriggerInteraction.Ignore);

            float nearest = fullDist;
            for (int i = 0; i < hits.Length; i++)
            {
                Transform hitT = hits[i].transform;
                if (hitT == transform || hitT.IsChildOf(transform)) continue;
                if (hits[i].distance < nearest)
                {
                    nearest = hits[i].distance;
                }
            }

            float safeDist = Mathf.Max(minSafeDistance, nearest - wallClearance);
            return pivot + dir * Mathf.Min(fullDist, safeDist);
        }
    }
}
