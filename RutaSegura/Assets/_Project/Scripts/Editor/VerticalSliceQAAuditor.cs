using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using RutaSegura.Runtime.Audio;
using RutaSegura.Runtime.Core;
using RutaSegura.Runtime.Events;
using RutaSegura.Runtime.Mission;
using RutaSegura.Runtime.Player;
using RutaSegura.Runtime.Threat;
using RutaSegura.Runtime.UI;

namespace RutaSegura.EditorTools
{
    /// <summary>
    /// Departamento 5 (QA Automatizado): verifica la integridad de escena, componentes Runtime,
    /// fisicas, un solo AudioListener, InputSystem UI y materiales generados antes de Play Mode.
    /// </summary>
    public static class VerticalSliceQAAuditor
    {
        [MenuItem("Tools/Ruta Segura/Ejecutar Auditoria QA (Vertical Slice)", priority = 120)]
        public static void RunAudit()
        {
            var passes = new List<string>();
            var fails = new List<string>();

            Check(fails, passes, !EditorUtility.scriptCompilationFailed,
                "Compilacion C# sin errores");

            // 1. Entorno horizontal y colisionadores en los 4 bloques principales
            string[] blocks =
            {
                "SM_Ruta_Piso_Completo",
                "SM_Manzanas_Juarez_Hidalgo",
                "SM_Manzanas_Callejones_Galeana",
                "SM_Manzanas_Hospital_Floreria"
            };

            foreach (string blockName in blocks)
            {
                var go = GameObject.Find(blockName);
                bool exists = go != null;
                Check(fails, passes, exists, $"Bloque arquitectonico presente: {blockName}");
                if (exists)
                {
                    var col = go.GetComponentInChildren<Collider>(true);
                    Check(fails, passes, col != null && col.enabled,
                        $"Collider activo en {blockName}");

                    Vector3 euler = go.transform.rotation.eulerAngles;
                    float tiltX = Mathf.Abs(Mathf.DeltaAngle(0f, euler.x));
                    float tiltZ = Mathf.Abs(Mathf.DeltaAngle(0f, euler.z));
                    Check(fails, passes, tiltX < 1f && tiltZ < 1f,
                        $"Orientacion horizontal correcta en {blockName}");
                }
            }

            // 2. Protagonista y controladores
            var player = GameObject.Find("Player");
            Check(fails, passes, player != null, "Objeto Player en escena");
            if (player != null)
            {
                Check(fails, passes, player.GetComponent<CharacterController>() != null, "Player tiene CharacterController");
                Check(fails, passes, player.GetComponent<PlayerController>() != null, "Player tiene PlayerController");
                Check(fails, passes, player.GetComponent<PlayerCameraController>() != null, "Player tiene PlayerCameraController");
                Check(fails, passes, player.GetComponent<PlayerAvatarVisual>() != null, "Player tiene PlayerAvatarVisual");
                Check(fails, passes, player.GetComponent<EnvironmentScanner>() != null, "Player tiene EnvironmentScanner");
            }

            // 3. GameManager y subsistemas
            var gm = GameObject.Find("GameManager");
            Check(fails, passes, gm != null, "Objeto GameManager en escena");
            if (gm != null)
            {
                Check(fails, passes, gm.GetComponent<MissionController>() != null, "GM tiene MissionController");
                Check(fails, passes, gm.GetComponent<GameLoopManager>() != null, "GM tiene GameLoopManager");
                Check(fails, passes, gm.GetComponent<TacticalHUDCanvas>() != null, "GM tiene TacticalHUDCanvas");
                Check(fails, passes, gm.GetComponent<CardiacBiometricHUD>() != null, "GM tiene CardiacBiometricHUD");
                Check(fails, passes, gm.GetComponent<PhoneHUD>() != null, "GM tiene PhoneHUD");
                Check(fails, passes, gm.GetComponent<BiometricAudioSystem>() != null, "GM tiene BiometricAudioSystem");
                Check(fails, passes, gm.GetComponent<UrbanSoundscapeController>() != null, "GM tiene UrbanSoundscapeController");
                Check(fails, passes, gm.GetComponent<ProtestAtmosphereController>() != null, "GM tiene ProtestAtmosphereController");
                Check(fails, passes, gm.GetComponent<FlickeringStreetlamp>() != null, "GM tiene FlickeringStreetlamp");
                Check(fails, passes, gm.GetComponent<WindblownDebrisController>() != null, "GM tiene WindblownDebrisController");
                Check(fails, passes, gm.GetComponent<UrbanLifeController>() != null, "GM tiene UrbanLifeController");
                Check(fails, passes, gm.GetComponent<PerformanceTelemetryHUD>() != null, "GM tiene PerformanceTelemetryHUD");
            }

            // 4. Triggers narrativos, poblacion y mobiliario en ruta
            Check(fails, passes, Object.FindAnyObjectByType<ThreatSystem>() != null, "ThreatSystem presente");
            Check(fails, passes, GameObject.Find("Trigger_Cierre") != null, "Trigger_Cierre presente");
            Check(fails, passes, GameObject.Find("Destino_Floreria") != null, "Destino_Floreria presente");
            Check(fails, passes, GameObject.Find("Zona_Peligro_Calle") != null, "Zona_Peligro_Calle presente");
            Check(fails, passes, GameObject.Find("Marcador_Ruta_Alternativa") != null, "Marcador_Ruta_Alternativa presente");
            Check(fails, passes, GameObject.Find("Trafico_Detenido") != null, "Trafico_Detenido presente");
            Check(fails, passes, GameObject.Find("Mobiliario_Urbano") != null, "Mobiliario_Urbano presente");
            Check(fails, passes, GameObject.Find("Vegetacion_Urbana") != null, "Vegetacion_Urbana presente");

            // 5. AudioListener unico e InputSystem UI
            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude);
            int enabledListeners = 0;
            foreach (var l in listeners)
            {
                if (l.enabled) enabledListeners++;
            }
            Check(fails, passes, enabledListeners == 1, $"AudioListener unico activo (actual={enabledListeners})");

            var es = Object.FindAnyObjectByType<EventSystem>();
            bool validEventSystem = es == null ||
                (es.GetComponent<InputSystemUIInputModule>() != null && es.GetComponent<StandaloneInputModule>() == null);
            Check(fails, passes, validEventSystem,
                "EventSystem compatible con InputSystemUIInputModule (sin StandaloneInputModule)");

            // 6. Materiales PBR y rotulacion tapatia clave
            string[] reqMats =
            {
                "M_Asfalto_Calle",
                "M_Letrero_Floreria",
                "M_Cartel_Neveria",
                "M_Cartel_Carmen",
                "M_Cartel_Periodico",
                "M_Grafiti_Consigna",
                "M_Placa_CalleCerrada",
                "M_Manta_Protesta",
                "M_Auto_Taxi",
                "M_Bus_Verde",
                "M_Follaje_Urbano",
                "M_Charco_Agua"
            };
            foreach (string matName in reqMats)
            {
                string path = $"Assets/_Project/Materials/{matName}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                Check(fails, passes, mat != null, $"Material existente: {matName}");
            }

            int total = passes.Count + fails.Count;
            string status = fails.Count == 0 ? "PASS" : "FAIL";
            Debug.Log($"<color={(fails.Count == 0 ? "green" : "red")}>[QA AUDITOR] {status} ({passes.Count}/{total} checks)</color>");
            foreach (var f in fails)
            {
                Debug.LogError($"[QA AUDITOR FAIL] {f}");
            }

            string summary = fails.Count == 0
                ? $"AUDITORIA QA EXITOSA: {passes.Count}/{total} verificaciones correctas.\nListo para Play Mode."
                : $"AUDITORIA CON {fails.Count} FALLOS ({passes.Count}/{total} correctos):\n- " + string.Join("\n- ", fails);

            EditorUtility.DisplayDialog($"Ruta Segura - QA Auditor ({status})", summary, "Aceptar");
        }

        private static void Check(List<string> fails, List<string> passes, bool cond, string label)
        {
            if (cond) passes.Add(label);
            else fails.Add(label);
        }
    }
}
