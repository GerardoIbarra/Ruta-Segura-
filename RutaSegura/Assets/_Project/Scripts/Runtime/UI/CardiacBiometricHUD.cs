using UnityEngine;
using RutaSegura.Runtime.Threat;

namespace RutaSegura.Runtime.UI
{
    /// <summary>
    /// HUD biométrico de pulso cardíaco y estado emocional del protagonista.
    /// Diseñado según el prototipo de referencia para evitar estética genérica.
    /// Muestra frecuencia cardíaca (BPM), trazo de electrocardiograma y estado:
    /// 'Tranquilo' -> 'Nervioso' -> 'Alerta' -> 'Pánico'.
    /// </summary>
    [AddComponentMenu("Ruta Segura/UI/Cardiac Biometric HUD")]
    public class CardiacBiometricHUD : MonoBehaviour
    {
        [Header("Calibración de Ritmo Cardíaco")]
        [Tooltip("Pulsaciones en estado de reposo / calma")]
        [SerializeField] private float minBPM = 68f;

        [Tooltip("Pulsaciones en estado de pánico / sobreexposición")]
        [SerializeField] private float maxBPM = 148f;

        [Header("Estilo Visual")]
        [SerializeField] private Vector2 screenOffset = new Vector2(35f, 35f);
        [SerializeField] private Vector2 widgetSize = new Vector2(230f, 78f);

        // Estado interno
        private float currentBPM = 68f;
        private float beatTimer = 0f;
        private float heartIconScale = 1.0f;
        private float ecgPhase = 0f;

        // Colores según la paleta de referencia
        private readonly Color colTranquilo = new Color(0.18f, 0.82f, 0.75f); // Cian clínico
        private readonly Color colNervioso  = new Color(0.95f, 0.78f, 0.22f); // Ámbar tensión
        private readonly Color colAlerta    = new Color(0.96f, 0.46f, 0.15f); // Naranja óxido
        private readonly Color colPanico    = new Color(0.94f, 0.20f, 0.20f); // Rojo carmesí

        private void Update()
        {
            float threatPercent = 0f;
            if (ThreatSystem.Instance != null)
            {
                threatPercent = ThreatSystem.Instance.CurrentThreat / 100f;
            }

            // El ritmo cardíaco sube proporcionalmente al estrés
            float targetBPM = Mathf.Lerp(minBPM, maxBPM, threatPercent);
            currentBPM = Mathf.Lerp(currentBPM, targetBPM, Time.deltaTime * 3f);

            // Temporizador de latido (un ciclo completo cada (60 / BPM) segundos)
            float secondsPerBeat = 60f / currentBPM;
            beatTimer += Time.deltaTime;
            ecgPhase += (Time.deltaTime / secondsPerBeat) * 2f;

            if (beatTimer >= secondsPerBeat)
            {
                beatTimer -= secondsPerBeat;
                // Disparo de latido visual (sístole)
                heartIconScale = 1.25f;
            }

            // Retorno suave del icono de corazón a su tamaño base
            heartIconScale = Mathf.Lerp(heartIconScale, 1.0f, Time.deltaTime * 9f);
        }

        private void OnGUI()
        {
            // Ocultar si la misión ya fracasó para no sobrecargar la pantalla de Game Over
            if (ThreatSystem.Instance != null && ThreatSystem.Instance.IsMissionFailed)
            {
                return;
            }

            float threat = ThreatSystem.Instance != null ? ThreatSystem.Instance.CurrentThreat : 0f;
            ThreatLevel level = ThreatSystem.Instance != null ? ThreatSystem.Instance.CurrentLevel : ThreatLevel.Low;

            string statusText;
            Color statusColor;

            switch (level)
            {
                case ThreatLevel.Low:
                    statusText = "Tranquilo";
                    statusColor = colTranquilo;
                    break;
                case ThreatLevel.Moderate:
                    statusText = "Nervioso";
                    statusColor = colNervioso;
                    break;
                case ThreatLevel.High:
                    statusText = "Alerta";
                    statusColor = colAlerta;
                    break;
                default:
                    statusText = "Pánico";
                    statusColor = colPanico;
                    break;
            }

            float x = screenOffset.x;
            float y = Screen.height - widgetSize.y - screenOffset.y;

            DrawBiometricCard(x, y, widgetSize.x, widgetSize.y, statusText, statusColor, threat);
        }

        private void DrawBiometricCard(float x, float y, float w, float h, string statusText, Color color, float threatPercent)
        {
            // 1. Fondo de cristal oscuro con borde fino sobrio
            GUI.color = new Color(0.06f, 0.08f, 0.11f, 0.88f);
            GUI.Box(new Rect(x, y, w, h), GUIContent.none);

            // Borde superior con acento sutil del color de estado
            GUI.color = new Color(color.r, color.g, color.b, 0.7f);
            GUI.DrawTexture(new Rect(x, y, w, 2f), Texture2D.whiteTexture);

            // 2. Línea de Electrocardiograma (ECG) procedural animada
            DrawECGWave(x + 12f, y + 10f, 90f, 22f, color);

            // Texto de BPM al lado del trazo
            var bpmStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            bpmStyle.normal.textColor = new Color(color.r, color.g, color.b, 0.9f);
            GUI.Label(new Rect(x + w - 100f, y + 8f, 88f, 20f), $"{currentBPM:F0} BPM", bpmStyle);

            // 3. Icono de Cruz/Corazón biométrico que late al compás del pulso
            float iconSize = 22f * heartIconScale;
            float iconX = x + 12f - (iconSize - 22f) / 2f;
            float iconY = y + 36f - (iconSize - 22f) / 2f;

            GUI.color = color;
            var iconStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(16 * heartIconScale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            iconStyle.normal.textColor = color;
            GUI.Label(new Rect(iconX, iconY, 22f, 22f), "+", iconStyle);

            // 4. Etiqueta grande del Estado Emocional
            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            labelStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x + 38f, y + 35f, 150f, 24f), statusText, labelStyle);

            // 5. Barra de exposición / estrés en la base del widget
            float barX = x + 12f;
            float barY = y + h - 10f;
            float barW = w - 24f;
            float barH = 4f;

            // Fondo de la barra
            GUI.color = new Color(0.18f, 0.20f, 0.25f, 0.8f);
            GUI.DrawTexture(new Rect(barX, barY, barW, barH), Texture2D.whiteTexture);

            // Relleno dinámico
            GUI.color = color;
            float fillW = (threatPercent / 100f) * barW;
            GUI.DrawTexture(new Rect(barX, barY, fillW, barH), Texture2D.whiteTexture);

            GUI.color = Color.white;
        }

        private void DrawECGWave(float x, float y, float w, float h, Color color)
        {
            // Dibuja una línea de pulso cardíaco simulada (línea base con complejo QRS periódico)
            GUI.color = new Color(color.r, color.g, color.b, 0.5f);
            float midY = y + h / 2f;

            // Línea base
            GUI.DrawTexture(new Rect(x, midY, w, 1f), Texture2D.whiteTexture);

            // Pico de latido que avanza a la velocidad del pulso
            float pulsePos = (ecgPhase % 1.0f) * w;
            float peakW = 12f;
            float peakH = 10f;

            // Pico QRS (sube y baja rápidamente)
            GUI.color = color;
            GUI.DrawTexture(new Rect(x + pulsePos, midY - peakH, 2f, peakH * 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + pulsePos + 2f, midY - peakH / 2f, 3f, peakH), Texture2D.whiteTexture);

            GUI.color = Color.white;
        }
    }
}
