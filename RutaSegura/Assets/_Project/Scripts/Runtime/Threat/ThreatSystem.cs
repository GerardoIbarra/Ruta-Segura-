using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace RutaSegura.Runtime.Threat
{
    /// <summary>
    /// Sistema de cálculo de riesgo contextual según la Sección 4.4 del GDD.
    /// Administra una escala determinista de 0 a 100, transiciones de estado
    /// y condición de fracaso por sobreexposición.
    /// </summary>
    [AddComponentMenu("Ruta Segura/Threat/Threat System")]
    public class ThreatSystem : MonoBehaviour
    {
        public static ThreatSystem Instance { get; private set; }

        [Header("Configuración de Amenaza (0 - 100)")]
        [Tooltip("Amenaza actual")]
        [Range(0f, 100f)]
        [SerializeField] private float currentThreat = 0f;

        [Tooltip("Velocidad de recuperación / calma por segundo en zonas seguras")]
        [SerializeField] private float decayRatePerSecond = 5f;

        [Header("Umbrales de Estado")]
        [SerializeField] private float moderateThreshold = 25f;
        [SerializeField] private float highThreshold = 50f;
        [SerializeField] private float criticalThreshold = 75f;

        [Header("Condición de Fracaso")]
        [SerializeField] private bool isMissionFailed = false;

        [Header("HUD Visual en Prototipo")]
        [SerializeField] private bool showDebugHUD = true;

        // Tasa activa de cambio aplicada por zonas actuales
        private float activeThreatRate = 0f;
        private ThreatLevel currentLevel = ThreatLevel.Low;

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

        private void Update()
        {
            if (isMissionFailed)
            {
                // Permitir reiniciar con la tecla R
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                {
                    RestartMission();
                }
                return;
            }

            float previousThreat = currentThreat;

            if (activeThreatRate > 0f)
            {
                // Estamos dentro de una zona de tensión: sube la amenaza
                currentThreat += activeThreatRate * Time.deltaTime;
            }
            else if (currentThreat > 0f)
            {
                // Estamos en zona tranquila: la amenaza decae progresivamente
                currentThreat -= decayRatePerSecond * Time.deltaTime;
            }

            currentThreat = Mathf.Clamp(currentThreat, 0f, 100f);

            if (!Mathf.Approximately(previousThreat, currentThreat))
            {
                UpdateThreatLevel(silent: false);
            }

            // Comprobar condición de fracaso al llegar al 100%
            if (currentThreat >= 100f && !isMissionFailed)
            {
                TriggerMissionFailure();
            }
        }

        private void TriggerMissionFailure()
        {
            isMissionFailed = true;
            OnMissionFailed?.Invoke();
            Debug.LogError("<color=red>[FRACASO]</color> Quedaste atrapado en el tumulto / corte de calle. Misión fallida.");

            // Desactivar el movimiento del jugador
            var player = FindFirstObjectByType<Player.PlayerController>();
            if (player != null)
            {
                player.enabled = false;
            }
        }

        public void RestartMission()
        {
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
                case ThreatLevel.Low: return new Color(0.2f, 0.85f, 0.3f);     // Verde
                case ThreatLevel.Moderate: return new Color(0.95f, 0.8f, 0.2f); // Amarillo
                case ThreatLevel.High: return new Color(1.0f, 0.5f, 0.1f);      // Naranja
                case ThreatLevel.Critical: return new Color(0.95f, 0.2f, 0.2f); // Rojo
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

        private void OnGUI()
        {
            if (!showDebugHUD) return;

            // HUD minimalista en esquina superior derecha
            float width = 220f;
            float height = 55f;
            float x = Screen.width - width - 20f;
            float y = 20f;

            Color levelColor = GetLevelColor(currentLevel);

            // Fondo del widget
            GUI.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);
            GUI.Box(new Rect(x, y, width, height), GUIContent.none);

            // Barra de progreso de amenaza
            float barPadding = 8f;
            float barWidth = width - (barPadding * 2);
            float barHeight = 10f;
            float barY = y + 36f;

            // Fondo de la barra
            GUI.color = new Color(0.2f, 0.2f, 0.25f, 1f);
            GUI.DrawTexture(new Rect(x + barPadding, barY, barWidth, barHeight), Texture2D.whiteTexture);

            // Barra llena
            GUI.color = levelColor;
            float fillWidth = (currentThreat / 100f) * barWidth;
            GUI.DrawTexture(new Rect(x + barPadding, barY, fillWidth, barHeight), Texture2D.whiteTexture);

            // Texto descriptivo del estado
            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };
            labelStyle.normal.textColor = levelColor;

            string statusText = $"Tensión: {currentLevel} ({currentThreat:F0}%)";
            GUI.Label(new Rect(x + barPadding, y + 8f, barWidth, 22f), statusText, labelStyle);

            GUI.color = Color.white;

            // Pantalla de Fracaso si se alcanza el 100% de amenaza
            if (isMissionFailed)
            {
                var originalColor = GUI.color;
                GUI.color = new Color(0.1f, 0.05f, 0.05f, 0.85f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

                var boxRect = new Rect(Screen.width / 2f - 290f, Screen.height / 2f - 90f, 580f, 180f);
                GUI.color = new Color(0.9f, 0.2f, 0.2f, 0.95f);
                var failureStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };

                GUI.Box(boxRect, "⚠️ MISIÓN FALLIDA\n\nQuedaste atrapado en el tumulto / corte de calle.\n\nPresiona [ R ] para reintentar", failureStyle);
                GUI.color = originalColor;
            }
        }
    }
}
