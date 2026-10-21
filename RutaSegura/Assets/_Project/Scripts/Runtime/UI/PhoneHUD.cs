using UnityEngine;
using UnityEngine.InputSystem;
using RutaSegura.Runtime.Mission;
using RutaSegura.Runtime.Threat;

namespace RutaSegura.Runtime.UI
{
    /// <summary>
    /// Interfaz diegética del teléfono del protagonista según la Sección 5 del GDD.
    /// Incorpora la app de navegación 'RutaLibre' y el feed social 'Eco Urbano'.
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
            // Barra superior izquierda discreta
            float width = 360f;
            float height = 34f;
            float x = 20f;
            float y = 20f;

            GUI.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);
            GUI.Box(new Rect(x, y, width, height), GUIContent.none);

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            style.normal.textColor = Color.white;

            string status = isOpen ? "[TAB] Cerrar Móvil" : "[TAB] Móvil";
            GUI.Label(new Rect(x + 10f, y, width - 20f, height), $"📱 {status}  |  Destino: Florería 'El Jazmín'", style);
            GUI.color = Color.white;
        }

        private void DrawSmartphone()
        {
            float phoneW = 320f;
            float phoneH = 500f;
            float x = Screen.width - phoneW - 30f;
            float y = Screen.height - phoneH - 30f;

            // Carcasa exterior del móvil (borde oscuro redondeado simulado)
            GUI.color = new Color(0.12f, 0.12f, 0.14f, 0.98f);
            GUI.Box(new Rect(x - 6, y - 6, phoneW + 12, phoneH + 12), GUIContent.none);

            // Pantalla interior
            GUI.color = new Color(0.16f, 0.17f, 0.2f, 1f);
            GUI.Box(new Rect(x, y, phoneW, phoneH), GUIContent.none);

            // Barra de estado superior del móvil (Hora, batería, 4G)
            GUI.color = new Color(0.1f, 0.1f, 0.12f, 1f);
            GUI.Box(new Rect(x, y, phoneW, 26f), GUIContent.none);

            var statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft
            };
            statusStyle.normal.textColor = new Color(0.85f, 0.85f, 0.9f);
            GUI.Label(new Rect(x + 12f, y + 3f, 80f, 20f), "17:42", statusStyle);

            statusStyle.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(x + phoneW - 90f, y + 3f, 80f, 20f), "4G  |  78% 🔋", statusStyle);

            // Pestañas de apps (RutaLibre vs Eco Urbano)
            float tabY = y + 30f;
            float tabW = (phoneW - 16f) / 2f;

            GUI.color = currentApp == PhoneApp.RutaLibre ? new Color(0.25f, 0.5f, 0.85f) : new Color(0.2f, 0.22f, 0.26f);
            if (GUI.Button(new Rect(x + 8f, tabY, tabW, 30f), "🗺️ RutaLibre"))
            {
                currentApp = PhoneApp.RutaLibre;
            }

            GUI.color = currentApp == PhoneApp.EcoUrbano ? new Color(0.9f, 0.45f, 0.2f) : new Color(0.2f, 0.22f, 0.26f);
            if (GUI.Button(new Rect(x + 8f + tabW, tabY, tabW, 30f), "📢 Eco Urbano"))
            {
                currentApp = PhoneApp.EcoUrbano;
            }

            GUI.color = Color.white;

            // Contenido de la app seleccionada
            Rect contentRect = new Rect(x + 10f, tabY + 36f, phoneW - 20f, phoneH - 85f);
            if (currentApp == PhoneApp.RutaLibre)
            {
                DrawRutaLibreContent(contentRect);
            }
            else
            {
                DrawEcoUrbanoContent(contentRect);
            }

            // Barra inferior Home del smartphone
            GUI.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            GUI.Box(new Rect(x + phoneW / 2f - 40f, y + phoneH - 12f, 80f, 4f), GUIContent.none);
            GUI.color = Color.white;
        }

        private void DrawRutaLibreContent(Rect r)
        {
            var headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = new Color(0.35f, 0.7f, 1f);

            var bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true
            };
            bodyStyle.normal.textColor = Color.white;

            float curY = r.y + 10f;

            GUI.Label(new Rect(r.x, curY, r.width, 20f), "📍 DESTINO SELECCIONADO", headerStyle);
            curY += 22f;
            GUI.Label(new Rect(r.x, curY, r.width, 36f), "Florería 'El Jazmín'\nDistancia aprox: 150 metros", bodyStyle);
            curY += 42f;

            // Ruta A
            GUI.color = new Color(0.22f, 0.2f, 0.24f, 0.9f);
            GUI.Box(new Rect(r.x, curY, r.width, 95f), GUIContent.none);
            GUI.color = Color.white;

            var routeHeader = new GUIStyle(headerStyle) { fontSize = 12 };
            routeHeader.normal.textColor = new Color(1f, 0.6f, 0.3f);
            GUI.Label(new Rect(r.x + 8f, curY + 6f, r.width - 16f, 18f), "Ruta A: Av. Juárez (Principal)", routeHeader);
            GUI.Label(new Rect(r.x + 8f, curY + 26f, r.width - 16f, 65f), 
                "• Tiempo: 2 minutos (Directa)\n• Estado: ⚠️ Tráfico detenido\n• Riesgo: CONGESTIÓN / CORTE ACTIVO", bodyStyle);
            curY += 105f;

            // Ruta B
            GUI.color = new Color(0.18f, 0.24f, 0.2f, 0.9f);
            GUI.Box(new Rect(r.x, curY, r.width, 95f), GUIContent.none);
            GUI.color = Color.white;

            routeHeader.normal.textColor = new Color(0.4f, 0.9f, 0.5f);
            GUI.Label(new Rect(r.x + 8f, curY + 6f, r.width - 16f, 18f), "Ruta B: Callejón Libertad", routeHeader);
            GUI.Label(new Rect(r.x + 8f, curY + 26f, r.width - 16f, 65f), 
                "• Tiempo: 4 minutos (Rodeo)\n• Estado: ✓ Libre para peatones\n• Riesgo: MÍNIMO / DESPEJADO", bodyStyle);
        }

        private void DrawEcoUrbanoContent(Rect r)
        {
            var headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = new Color(1f, 0.5f, 0.3f);

            var feedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true
            };
            feedStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

            var timeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Italic
            };
            timeStyle.normal.textColor = new Color(0.6f, 0.6f, 0.65f);

            float curY = r.y + 8f;
            GUI.Label(new Rect(r.x, curY, r.width, 20f), "FEED CIUDADANO EN TIEMPO REAL", headerStyle);
            curY += 26f;

            // Noticia 1
            DrawFeedCard(r.x, curY, r.width, 70f, "Hace 3 min • @transito_gdl", 
                "Cierre preventivo en cruce con Av. Juárez. Eviten la zona centro a pie o en auto.", timeStyle, feedStyle);
            curY += 76f;

            // Noticia 2
            DrawFeedCard(r.x, curY, r.width, 70f, "Hace 11 min • @vecinos_centro", 
                "Varios comercios bajando cortinas metálicas sobre la avenida principal.", timeStyle, feedStyle);
            curY += 76f;

            // Noticia 3
            DrawFeedCard(r.x, curY, r.width, 70f, "Hace 22 min • @movilidad_urbana", 
                "Movilización pacífica avanza con desvíos en rutas normales de transporte.", timeStyle, feedStyle);
        }

        private void DrawFeedCard(float x, float y, float w, float h, string time, string text, GUIStyle tStyle, GUIStyle bStyle)
        {
            GUI.color = new Color(0.2f, 0.22f, 0.26f, 0.85f);
            GUI.Box(new Rect(x, y, w, h), GUIContent.none);
            GUI.color = Color.white;

            GUI.Label(new Rect(x + 6f, y + 4f, w - 12f, 16f), time, tStyle);
            GUI.Label(new Rect(x + 6f, y + 20f, w - 12f, h - 24f), text, bStyle);
        }
    }
}
