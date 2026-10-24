using UnityEngine;
using UnityEngine.InputSystem;
using RutaSegura.Runtime.Mission;
using RutaSegura.Runtime.Threat;

namespace RutaSegura.Runtime.UI
{
    /// <summary>
    /// Interfaz diegética del teléfono del protagonista según la Sección 5 del GDD.
    /// Incorpora la app de navegación 'RutaLibre' y el feed social 'Eco Urbano'.
    /// Diseñado con estética sobria y profesional de videojuego, sin emojis.
    /// Se despliega y oculta pulsando la tecla [TAB].
    /// </summary>
    [AddComponentMenu("Ruta Segura/UI/Phone HUD")]
    public class PhoneHUD : MonoBehaviour
    {
        public static PhoneHUD Instance { get; private set; }

        [Header("Configuración")]
        [Tooltip("Si el teléfono comienza abierto")]
        [SerializeField] private bool isOpen = false;

        public bool IsOpen => isOpen;

        [Header("Pestaña Activa")]
        [SerializeField] private PhoneApp currentApp = PhoneApp.RutaLibre;

        public enum PhoneApp
        {
            RutaLibre,  // Navegación y estimación de rutas
            EcoUrbano   // Avisos y reportes de la ciudad
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            // Alternar teléfono con TAB
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                isOpen = !isOpen;

                // Liberar el cursor para poder pulsar las apps del teléfono
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
        }

        private void OnGUI()
        {
            DrawCompactObjectiveBar();

            if (isOpen)
            {
                DrawSmartphone();
            }
        }

        private void DrawCompactObjectiveBar()
        {
            // Barra superior izquierda sobria y limpia
            float width = 360f;
            float height = 32f;
            float x = 25f;
            float y = 25f;

            GUI.color = new Color(0.06f, 0.08f, 0.11f, 0.90f);
            GUI.Box(new Rect(x, y, width, height), GUIContent.none);

            GUI.color = new Color(0.2f, 0.8f, 0.9f, 0.7f);
            GUI.DrawTexture(new Rect(x, y, 3f, height), Texture2D.whiteTexture);

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            style.normal.textColor = new Color(0.9f, 0.92f, 0.95f);

            string status = isOpen ? "[TAB] CERRAR DISPOSITIVO" : "[TAB] DISPOSITIVO";
            GUI.Label(new Rect(x + 12f, y, width - 20f, height), $"{status}  |  DESTINO: FLORERÍA EL JAZMÍN", style);
            GUI.color = Color.white;
        }

        private void DrawSmartphone()
        {
            float phoneW = 320f;
            float phoneH = 490f;
            float x = Screen.width - phoneW - 35f;
            float y = Screen.height - phoneH - 35f;

            // Carcasa exterior del móvil
            GUI.color = new Color(0.08f, 0.09f, 0.11f, 0.98f);
            GUI.Box(new Rect(x - 5, y - 5, phoneW + 10, phoneH + 10), GUIContent.none);

            // Pantalla interior
            GUI.color = new Color(0.12f, 0.14f, 0.17f, 1f);
            GUI.Box(new Rect(x, y, phoneW, phoneH), GUIContent.none);

            // Barra de estado superior del móvil
            GUI.color = new Color(0.08f, 0.09f, 0.11f, 1f);
            GUI.Box(new Rect(x, y, phoneW, 24f), GUIContent.none);

            var statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft
            };
            statusStyle.normal.textColor = new Color(0.75f, 0.78f, 0.85f);
            GUI.Label(new Rect(x + 12f, y + 2f, 80f, 20f), "17:42", statusStyle);

            statusStyle.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(x + phoneW - 110f, y + 2f, 100f, 20f), "LTE  |  BAT: 78%", statusStyle);

            // Pestañas de apps
            float tabY = y + 28f;
            float tabW = (phoneW - 16f) / 2f;

            GUI.color = currentApp == PhoneApp.RutaLibre ? new Color(0.2f, 0.45f, 0.8f) : new Color(0.16f, 0.18f, 0.22f);
            if (GUI.Button(new Rect(x + 8f, tabY, tabW, 28f), "[ RUTALIBRE ]"))
            {
                currentApp = PhoneApp.RutaLibre;
            }

            GUI.color = currentApp == PhoneApp.EcoUrbano ? new Color(0.85f, 0.4f, 0.15f) : new Color(0.16f, 0.18f, 0.22f);
            if (GUI.Button(new Rect(x + 8f + tabW, tabY, tabW, 28f), "[ ECO URBANO ]"))
            {
                currentApp = PhoneApp.EcoUrbano;
            }

            GUI.color = Color.white;

            // Contenido de la app seleccionada
            Rect contentRect = new Rect(x + 10f, tabY + 34f, phoneW - 20f, phoneH - 80f);
            if (currentApp == PhoneApp.RutaLibre)
            {
                DrawRutaLibreContent(contentRect);
            }
            else
            {
                DrawEcoUrbanoContent(contentRect);
            }

            // Barra Home inferior
            GUI.color = new Color(0.5f, 0.5f, 0.55f, 0.5f);
            GUI.Box(new Rect(x + phoneW / 2f - 35f, y + phoneH - 10f, 70f, 3f), GUIContent.none);
            GUI.color = Color.white;
        }

        private void DrawRutaLibreContent(Rect r)
        {
            var headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = new Color(0.35f, 0.7f, 1f);

            var bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true
            };
            bodyStyle.normal.textColor = Color.white;

            float curY = r.y + 8f;

            GUI.Label(new Rect(r.x, curY, r.width, 18f), "DESTINO SELECCIONADO", headerStyle);
            curY += 20f;
            GUI.Label(new Rect(r.x, curY, r.width, 34f), "Florería 'El Jazmín'\nDistancia estimada: 150 metros", bodyStyle);
            curY += 38f;

            // Ruta A
            GUI.color = new Color(0.18f, 0.16f, 0.20f, 0.9f);
            GUI.Box(new Rect(r.x, curY, r.width, 90f), GUIContent.none);
            GUI.color = Color.white;

            var routeHeader = new GUIStyle(headerStyle) { fontSize = 11 };
            routeHeader.normal.textColor = new Color(1f, 0.6f, 0.25f);
            GUI.Label(new Rect(r.x + 8f, curY + 6f, r.width - 16f, 18f), "Ruta A: Av. Juárez (Principal)", routeHeader);
            GUI.Label(new Rect(r.x + 8f, curY + 24f, r.width - 16f, 60f), 
                "• Tiempo: 2 min (Directa)\n• Estado: [AVISO] Tráfico detenido\n• Riesgo: CONGESTIÓN / CORTE ACTIVO", bodyStyle);
            curY += 98f;

            // Ruta B
            GUI.color = new Color(0.14f, 0.20f, 0.16f, 0.9f);
            GUI.Box(new Rect(r.x, curY, r.width, 90f), GUIContent.none);
            GUI.color = Color.white;

            routeHeader.normal.textColor = new Color(0.35f, 0.88f, 0.45f);
            GUI.Label(new Rect(r.x + 8f, curY + 6f, r.width - 16f, 18f), "Ruta B: Callejón Libertad", routeHeader);
            GUI.Label(new Rect(r.x + 8f, curY + 24f, r.width - 16f, 60f), 
                "• Tiempo: 4 min (Rodeo)\n• Estado: [LIBRE] Vía despejada\n• Riesgo: MÍNIMO / DESPEJADO", bodyStyle);
        }

        private void DrawEcoUrbanoContent(Rect r)
        {
            var headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = new Color(1f, 0.55f, 0.25f);

            var feedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true
            };
            feedStyle.normal.textColor = new Color(0.9f, 0.9f, 0.92f);

            var timeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Italic
            };
            timeStyle.normal.textColor = new Color(0.55f, 0.58f, 0.65f);

            float curY = r.y + 6f;
            GUI.Label(new Rect(r.x, curY, r.width, 18f), "FEED LOCAL EN TIEMPO REAL", headerStyle);
            curY += 22f;

            // Noticia 1
            DrawFeedCard(r.x, curY, r.width, 66f, "Hace 3 min • @transito_gdl", 
                "Cierre preventivo en cruce con Av. Juárez. Eviten la zona centro a pie o en auto.", timeStyle, feedStyle);
            curY += 72f;

            // Noticia 2
            DrawFeedCard(r.x, curY, r.width, 66f, "Hace 11 min • @vecinos_centro", 
                "Varios comercios bajando cortinas metálicas sobre la avenida principal.", timeStyle, feedStyle);
            curY += 72f;

            // Noticia 3
            DrawFeedCard(r.x, curY, r.width, 66f, "Hace 22 min • @movilidad_urbana", 
                "Movilización pacífica avanza con desvíos en rutas normales de transporte.", timeStyle, feedStyle);
        }

        private void DrawFeedCard(float x, float y, float w, float h, string time, string text, GUIStyle tStyle, GUIStyle bStyle)
        {
            GUI.color = new Color(0.16f, 0.18f, 0.22f, 0.85f);
            GUI.Box(new Rect(x, y, w, h), GUIContent.none);
            GUI.color = Color.white;

            GUI.Label(new Rect(x + 6f, y + 4f, w - 12f, 15f), time, tStyle);
            GUI.Label(new Rect(x + 6f, y + 18f, w - 12f, h - 22f), text, bStyle);
        }
    }
}
