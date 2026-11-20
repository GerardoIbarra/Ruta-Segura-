using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RutaSegura.Runtime.UI;

namespace RutaSegura.Runtime.Core
{
    public enum GamePhase
    {
        Intro,
        Exploration,
        Incident,
        Detour,
        Outcome
    }

    /// <summary>
    /// Maquina de estados finita del ciclo principal de Ruta Segura:
    /// Intro -> Exploration -> Incident -> Detour -> Outcome.
    /// Coordina tarjeta cinematica de inicio, menu de pausa [ESC] con volumen y reinicio [R].
    /// </summary>
    [DefaultExecutionOrder(-60)]
    [AddComponentMenu("Ruta Segura/Core/Game Loop Manager")]
    public class GameLoopManager : MonoBehaviour
    {
        private const string PrefMasterVolume = "RutaSegura_MasterVol";

        public static GameLoopManager Instance { get; private set; }

        [Header("Fases de Partida")]
        [SerializeField] private float introDuration = 3.2f;
        [SerializeField] private GamePhase currentPhase = GamePhase.Intro;

        private float introTimer;
        private float masterVolume = 1f;
        private bool uiBuilt;

        private GameObject introCardGroup;
        private CanvasGroup introCanvasGroup;

        private GameObject pauseMenuGroup;
        private Slider volumeSlider;
        private Text volumeLabelText;

        public GamePhase CurrentPhase => currentPhase;
        public bool IsPaused { get; private set; }

        public event Action<GamePhase> OnPhaseChanged;
        public event Action<bool> OnPausedChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            Time.timeScale = 1f;
            masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefMasterVolume, 1f));
            AudioListener.volume = masterVolume;
        }

        private void Start()
        {
            BuildUI();
            if (introCardGroup != null)
            {
                introCardGroup.SetActive(currentPhase == GamePhase.Intro);
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!uiBuilt) BuildUI();

            if (currentPhase != GamePhase.Outcome &&
                Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetPaused(!IsPaused);
            }

            if (IsPaused)
            {
                return;
            }

            if (currentPhase == GamePhase.Intro)
            {
                introTimer += Time.deltaTime;
                if (introCanvasGroup != null)
                {
                    float remaining = introDuration - introTimer;
                    introCanvasGroup.alpha = remaining < 0.6f ? Mathf.Clamp01(remaining / 0.6f) : 1f;
                }

                bool skipPressed = Keyboard.current != null &&
                    (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);

                if (skipPressed || introTimer >= introDuration)
                {
                    SetPhase(GamePhase.Exploration);
                }
            }
            else if (currentPhase == GamePhase.Outcome)
            {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                {
                    RestartLevel();
                }
            }
        }

        public void SetPaused(bool paused)
        {
            if (paused == IsPaused) return;
            if (paused && currentPhase == GamePhase.Outcome) return;

            IsPaused = paused;
            Time.timeScale = IsPaused ? 0f : 1f;

            if (pauseMenuGroup != null)
            {
                pauseMenuGroup.SetActive(IsPaused);
            }

            if (introCardGroup != null && currentPhase == GamePhase.Intro)
            {
                introCardGroup.SetActive(!IsPaused);
            }

            if (IsPaused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                bool phoneOpen = PhoneHUD.Instance != null && PhoneHUD.Instance.IsOpen;
                Cursor.lockState = phoneOpen ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = phoneOpen;
            }

            OnPausedChanged?.Invoke(IsPaused);
        }

        public void SetPhase(GamePhase nextPhase)
        {
            if (nextPhase == currentPhase) return;
            if (currentPhase == GamePhase.Outcome) return;

            currentPhase = nextPhase;

            if (introCardGroup != null)
            {
                introCardGroup.SetActive(currentPhase == GamePhase.Intro && !IsPaused);
            }

            OnPhaseChanged?.Invoke(currentPhase);
            Debug.Log($"<color=cyan>[GAMELOOP]</color> Fase cambiada a: {currentPhase}");
        }

        public void RestartLevel()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void BuildUI()
        {
            if (uiBuilt) return;
            var hud = TacticalHUDCanvas.GetOrCreate();
            if (hud == null || hud.Root == null) return;

            BuildIntroCard(hud);
            BuildPauseMenu(hud);
            uiBuilt = true;
        }

        private void BuildIntroCard(TacticalHUDCanvas hud)
        {
            var cardImg = hud.CreatePanel("Intro_MissionCard", hud.Root, new Color(0.05f, 0.08f, 0.12f, 0.90f));
            var rt = cardImg.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.72f);
            rt.anchorMax = new Vector2(0.5f, 0.72f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(640f, 152f);
            cardImg.raycastTarget = false;

            var accentBar = hud.CreatePanel("TopLine", rt, new Color(0.22f, 0.88f, 0.96f, 0.95f));
            accentBar.rectTransform.anchorMin = new Vector2(0f, 1f);
            accentBar.rectTransform.anchorMax = new Vector2(1f, 1f);
            accentBar.rectTransform.pivot = new Vector2(0.5f, 1f);
            accentBar.rectTransform.anchoredPosition = Vector2.zero;
            accentBar.rectTransform.sizeDelta = new Vector2(0f, 4f);
            accentBar.raycastTarget = false;

            var subtitle = hud.CreateText("Subtitle", rt, "17:42  |  CENTRO HISTORICO DE GUADALAJARA", 13, new Color(0.32f, 0.90f, 0.98f), TextAnchor.MiddleCenter, FontStyle.Bold);
            subtitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            subtitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            subtitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            subtitle.rectTransform.anchoredPosition = new Vector2(0f, -16f);
            subtitle.rectTransform.sizeDelta = new Vector2(-36f, 22f);

            var title = hud.CreateText("Title", rt, "MISION 1: LAS FLORES", 26, new Color(0.98f, 0.90f, 0.62f), TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -44f);
            title.rectTransform.sizeDelta = new Vector2(-36f, 36f);

            var goal = hud.CreateText("Objective", rt, "Cruza Av. Juarez y encuentra un desvio seguro hasta Floreria El Jazmin.", 14, new Color(0.90f, 0.93f, 0.96f), TextAnchor.MiddleCenter, FontStyle.Normal);
            goal.rectTransform.anchorMin = new Vector2(0f, 0f);
            goal.rectTransform.anchorMax = new Vector2(1f, 0f);
            goal.rectTransform.pivot = new Vector2(0.5f, 0f);
            goal.rectTransform.anchoredPosition = new Vector2(0f, 34f);
            goal.rectTransform.sizeDelta = new Vector2(-36f, 26f);

            var hint = hud.CreateText("Hint", rt, "[ESPACIO] INICIAR  |  [TAB] DISPOSITIVO  |  [ESC] PAUSA", 11, new Color(0.64f, 0.72f, 0.80f), TextAnchor.MiddleCenter, FontStyle.Bold);
            hint.rectTransform.anchorMin = new Vector2(0f, 0f);
            hint.rectTransform.anchorMax = new Vector2(1f, 0f);
            hint.rectTransform.pivot = new Vector2(0.5f, 0f);
            hint.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            hint.rectTransform.sizeDelta = new Vector2(-36f, 20f);

            introCanvasGroup = cardImg.gameObject.AddComponent<CanvasGroup>();
            introCanvasGroup.interactable = false;
            introCanvasGroup.blocksRaycasts = false;

            introCardGroup = cardImg.gameObject;
            introCardGroup.SetActive(currentPhase == GamePhase.Intro);
        }

        private void BuildPauseMenu(TacticalHUDCanvas hud)
        {
            var backdrop = hud.CreatePanel("Pause_Backdrop", hud.Root, new Color(0.03f, 0.04f, 0.06f, 0.74f));
            TacticalHUDCanvas.Stretch(backdrop.rectTransform);
            backdrop.raycastTarget = true;

            var card = hud.CreatePanel("Pause_Card", backdrop.rectTransform, new Color(0.08f, 0.10f, 0.14f, 0.97f));
            var rt = card.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(480f, 430f);

            var topBar = hud.CreatePanel("HeaderAccent", rt, new Color(0.20f, 0.84f, 0.95f, 0.95f));
            topBar.rectTransform.anchorMin = new Vector2(0f, 1f);
            topBar.rectTransform.anchorMax = new Vector2(1f, 1f);
            topBar.rectTransform.pivot = new Vector2(0.5f, 1f);
            topBar.rectTransform.anchoredPosition = Vector2.zero;
            topBar.rectTransform.sizeDelta = new Vector2(0f, 5f);
            topBar.raycastTarget = false;

            var heading = hud.CreateText("Heading", rt, "PAUSA  |  RUTA SEGURA", 20, new Color(0.95f, 0.97f, 0.99f), TextAnchor.MiddleCenter, FontStyle.Bold);
            heading.rectTransform.anchorMin = new Vector2(0f, 1f);
            heading.rectTransform.anchorMax = new Vector2(1f, 1f);
            heading.rectTransform.pivot = new Vector2(0.5f, 1f);
            heading.rectTransform.anchoredPosition = new Vector2(0f, -18f);
            heading.rectTransform.sizeDelta = new Vector2(-40f, 32f);

            var controlsBox = hud.CreatePanel("ControlsBox", rt, new Color(0.05f, 0.07f, 0.10f, 0.90f));
            controlsBox.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            controlsBox.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            controlsBox.rectTransform.pivot = new Vector2(0.5f, 1f);
            controlsBox.rectTransform.anchoredPosition = new Vector2(0f, -64f);
            controlsBox.rectTransform.sizeDelta = new Vector2(420f, 154f);
            controlsBox.raycastTarget = false;

            string controlsStr =
                "[W A S D] Moverse   |   [SHIFT] Correr\n" +
                "[E / Q] Escanear entorno y detectar riesgos\n" +
                "[TAB] Abrir / cerrar telefono Eco Urbano\n" +
                "[V / F5] Cambiar vista 1ra / 3ra persona\n" +
                "[Click Der / Z + Rueda] Zoom en 1ra persona\n" +
                "[F3] Telemetria QA   |   [ESC] Reanudar";

            var controlsText = hud.CreateText("ControlsText", controlsBox.rectTransform, controlsStr, 13, new Color(0.84f, 0.89f, 0.94f), TextAnchor.MiddleLeft, FontStyle.Normal);
            TacticalHUDCanvas.Stretch(controlsText.rectTransform, 14f);

            // Control de volumen general
            volumeLabelText = hud.CreateText("VolumeLabel", rt, $"VOLUMEN GENERAL: {Mathf.RoundToInt(masterVolume * 100f)}%", 13, new Color(0.75f, 0.92f, 0.98f), TextAnchor.MiddleLeft, FontStyle.Bold);
            volumeLabelText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            volumeLabelText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            volumeLabelText.rectTransform.pivot = new Vector2(0.5f, 1f);
            volumeLabelText.rectTransform.anchoredPosition = new Vector2(0f, -234f);
            volumeLabelText.rectTransform.sizeDelta = new Vector2(420f, 22f);

            volumeSlider = BuildVolumeSlider(hud, rt, new Vector2(0f, -268f), masterVolume);
            volumeSlider.onValueChanged.AddListener(HandleVolumeChanged);

            var resumeBtn = hud.CreateButton("Btn_Resume", rt, "REANUDAR", 15, new Color(0.14f, 0.44f, 0.56f), Color.white, out _, out _);
            resumeBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            resumeBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            resumeBtn.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            resumeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 78f);
            resumeBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 44f);
            resumeBtn.onClick.AddListener(() => SetPaused(false));

            var restartBtn = hud.CreateButton("Btn_Restart", rt, "REINICIAR MISION", 14, new Color(0.45f, 0.16f, 0.18f), Color.white, out _, out _);
            restartBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            restartBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            restartBtn.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            restartBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 24f);
            restartBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 40f);
            restartBtn.onClick.AddListener(RestartLevel);

            pauseMenuGroup = backdrop.gameObject;
            pauseMenuGroup.SetActive(false);
        }

        private Slider BuildVolumeSlider(TacticalHUDCanvas hud, RectTransform parent, Vector2 anchoredPos, float initialVal)
        {
            var rootRt = TacticalHUDCanvas.CreateRect("MasterVolumeSlider", parent);
            rootRt.anchorMin = new Vector2(0.5f, 1f);
            rootRt.anchorMax = new Vector2(0.5f, 1f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = anchoredPos;
            rootRt.sizeDelta = new Vector2(420f, 26f);

            var bg = hud.CreatePanel("TrackBg", rootRt, new Color(0.16f, 0.20f, 0.26f, 1f));
            bg.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            bg.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            bg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            bg.rectTransform.anchoredPosition = Vector2.zero;
            bg.rectTransform.sizeDelta = new Vector2(0f, 8f);

            var fillArea = TacticalHUDCanvas.CreateRect("FillArea", rootRt);
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.sizeDelta = new Vector2(-12f, 8f);

            var fillImg = hud.CreatePanel("Fill", fillArea, new Color(0.22f, 0.85f, 0.96f, 1f));
            fillImg.rectTransform.anchorMin = Vector2.zero;
            fillImg.rectTransform.anchorMax = new Vector2(initialVal, 1f);
            fillImg.rectTransform.offsetMin = Vector2.zero;
            fillImg.rectTransform.offsetMax = Vector2.zero;

            var handleArea = TacticalHUDCanvas.CreateRect("HandleArea", rootRt);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(10f, 0f);
            handleArea.offsetMax = new Vector2(-10f, 0f);

            var handleImg = hud.CreatePanel("Handle", handleArea, new Color(0.96f, 0.98f, 1f, 1f));
            handleImg.rectTransform.sizeDelta = new Vector2(18f, 22f);

            var slider = rootRt.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fillImg.rectTransform;
            slider.handleRect = handleImg.rectTransform;
            slider.targetGraphic = handleImg;
            slider.SetValueWithoutNotify(initialVal);

            return slider;
        }

        private void HandleVolumeChanged(float val)
        {
            masterVolume = Mathf.Clamp01(val);
            AudioListener.volume = masterVolume;
            PlayerPrefs.SetFloat(PrefMasterVolume, masterVolume);
            PlayerPrefs.Save();

            if (volumeLabelText != null)
            {
                volumeLabelText.text = $"VOLUMEN GENERAL: {Mathf.RoundToInt(masterVolume * 100f)}%";
            }
        }
    }
}
