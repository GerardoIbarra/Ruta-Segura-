using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RutaSegura.Runtime.Core;
using RutaSegura.Runtime.Mission;
using RutaSegura.Runtime.UI;

namespace RutaSegura.Runtime.Threat
{
    /// <summary>
    /// Sistema de calculo de riesgo contextual (0 a 100), transiciones de estado
    /// y condicion de fracaso por sobreexposicion con pantalla uGUI.
    /// </summary>
    [AddComponentMenu("Ruta Segura/Threat/Threat System")]
    public class ThreatSystem : MonoBehaviour
    {
        public static ThreatSystem Instance { get; private set; }

        [Header("Configuracion de Amenaza (0 - 100)")]
        [Range(0f, 100f)]
        [SerializeField] private float currentThreat = 0f;

        [SerializeField] private float decayRatePerSecond = 5f;

        [Header("Umbrales de Estado")]
        [SerializeField] private float moderateThreshold = 25f;
        [SerializeField] private float highThreshold = 50f;
        [SerializeField] private float criticalThreshold = 75f;

        [Header("Condicion de Fracaso")]
        [SerializeField] private bool isMissionFailed = false;

        private float activeThreatRate;
        private ThreatLevel currentLevel = ThreatLevel.Low;
        private GameObject failureOverlay;

        public float CurrentThreat => currentThreat;
        public ThreatLevel CurrentLevel => currentLevel;
        public bool IsMissionFailed => isMissionFailed;

        public event Action<float, ThreatLevel> OnThreatChanged;
        public event Action<ThreatLevel> OnThreatLevelChanged;
        public event Action OnMissionFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            UpdateThreatLevel(silent: true);
        }

        private void Start()
        {
            BuildFailureUI();
        }

        private void BuildFailureUI()
        {
            var hud = TacticalHUDCanvas.GetOrCreate();
            if (hud == null || hud.Root == null) return;

            var bg = hud.CreatePanel("Failure_Overlay", hud.Root, new Color(0.04f, 0.02f, 0.03f, 0.90f));
            TacticalHUDCanvas.Stretch(bg.rectTransform);
            bg.raycastTarget = false;

            var card = hud.CreatePanel("Failure_Card", bg.rectTransform, new Color(0.12f, 0.05f, 0.06f, 0.97f));
            var rt = card.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(580f, 200f);
            card.raycastTarget = false;

            var border = hud.CreatePanel("RedAccent", rt, new Color(0.92f, 0.2f, 0.2f, 1f));
            border.rectTransform.anchorMin = new Vector2(0f, 1f);
            border.rectTransform.anchorMax = new Vector2(1f, 1f);
            border.rectTransform.pivot = new Vector2(0.5f, 1f);
            border.rectTransform.anchoredPosition = Vector2.zero;
            border.rectTransform.sizeDelta = new Vector2(0f, 4f);
            border.raycastTarget = false;

            var lbl = hud.CreateText(
                "FailureLabel",
                rt,
                "[ MISION FALLIDA ]\n\nQuedaste atrapado en el tumulto / corte de calle.\n\nPresiona [ R ] para reintentar",
                21,
                Color.white,
                TextAnchor.MiddleCenter,
                FontStyle.Bold);
            TacticalHUDCanvas.Stretch(lbl.rectTransform, 18f);

            failureOverlay = bg.gameObject;
            failureOverlay.SetActive(isMissionFailed);
        }

        private void Update()
        {
            if (isMissionFailed)
            {
                // Si no hay GameLoopManager activo, este script respalda [R]
                if (GameLoopManager.Instance == null && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                {
                    RestartMission();
                }
                return;
            }

            float previousThreat = currentThreat;

            if (activeThreatRate > 0f)
            {
                currentThreat += activeThreatRate * Time.deltaTime;
            }
            else if (currentThreat > 0f)
            {
                currentThreat -= decayRatePerSecond * Time.deltaTime;
            }

            currentThreat = Mathf.Clamp(currentThreat, 0f, 100f);

            if (!Mathf.Approximately(previousThreat, currentThreat))
            {
                UpdateThreatLevel(silent: false);
            }

            if (currentThreat >= 100f && !isMissionFailed)
            {
                TriggerMissionFailure();
            }
        }

        private void TriggerMissionFailure()
        {
            isMissionFailed = true;
            OnMissionFailed?.Invoke();

            MissionController.Instance?.FailMission();
            GameLoopManager.Instance?.SetPhase(GamePhase.Outcome);

            if (failureOverlay != null)
            {
                failureOverlay.SetActive(true);
            }

            var player = FindFirstObjectByType<Player.PlayerController>();
            if (player != null)
            {
                player.enabled = false;
            }

            Debug.LogError("<color=red>[FRACASO]</color> Quedaste atrapado en el tumulto / corte de calle. Mision fallida.");
        }

        public void RestartMission()
        {
            if (GameLoopManager.Instance != null)
            {
                GameLoopManager.Instance.RestartLevel();
                return;
            }
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void AddThreat(float amount)
        {
            currentThreat = Mathf.Clamp(currentThreat + amount, 0f, 100f);
            UpdateThreatLevel(silent: false);
        }

        public void RegisterThreatRate(float rate)
        {
            activeThreatRate = Mathf.Max(activeThreatRate, rate);
        }

        public void UnregisterThreatRate()
        {
            activeThreatRate = 0f;
        }

        private void UpdateThreatLevel(bool silent)
        {
            ThreatLevel newLevel;
            if (currentThreat >= criticalThreshold)
                newLevel = ThreatLevel.Critical;
            else if (currentThreat >= highThreshold)
                newLevel = ThreatLevel.High;
            else if (currentThreat >= moderateThreshold)
                newLevel = ThreatLevel.Moderate;
            else
                newLevel = ThreatLevel.Low;

            bool levelChanged = newLevel != currentLevel;
            currentLevel = newLevel;

            if (!silent)
            {
                OnThreatChanged?.Invoke(currentThreat, currentLevel);
                if (levelChanged)
                {
                    OnThreatLevelChanged?.Invoke(currentLevel);
                    Debug.Log($"<color={GetLevelColorHex(currentLevel)}>[AMENAZA]</color> Nivel cambiado a: {currentLevel} ({currentThreat:F0}%)");
                }
            }
        }

        public Color GetLevelColor(ThreatLevel level)
        {
            switch (level)
            {
                case ThreatLevel.Low: return new Color(0.2f, 0.85f, 0.3f);
                case ThreatLevel.Moderate: return new Color(0.95f, 0.8f, 0.2f);
                case ThreatLevel.High: return new Color(1.0f, 0.5f, 0.1f);
                case ThreatLevel.Critical: return new Color(0.95f, 0.2f, 0.2f);
                default: return Color.white;
            }
        }

        private string GetLevelColorHex(ThreatLevel level)
        {
            switch (level)
            {
                case ThreatLevel.Low: return "#33DA4C";
                case ThreatLevel.Moderate: return "#F2CC33";
                case ThreatLevel.High: return "#FF801A";
                case ThreatLevel.Critical: return "#F23333";
                default: return "#FFFFFF";
            }
        }
    }
}
