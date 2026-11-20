using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RutaSegura.Runtime.Audio;
using RutaSegura.Runtime.Core;
using RutaSegura.Runtime.Events;
using RutaSegura.Runtime.Mission;
using RutaSegura.Runtime.Navigation;
using RutaSegura.Runtime.Player;
using RutaSegura.Runtime.UI;

namespace RutaSegura.Editor
{
    /// <summary>
    /// Automatizacion completa de la escena para configurar todos los sistemas del estudio:
    /// arquitectura PBR, iluminacion, audio biometrico/urbano, FSM, HUD uGUI y escaner de entorno.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoSceneConfigurator
    {
        static AutoSceneConfigurator()
        {
            EditorApplication.delayCall += ExecuteAutoSetupSilent;
        }

        [MenuItem("Tools/Ruta Segura/Auto-Configurar Todo (1 Clic)")]
        public static void ExecuteAutoSetupManual()
        {
            PerformSetup(showDialog: true);
        }

        private static void ExecuteAutoSetupSilent()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            PerformSetup(showDialog: false);
        }

        public static void PerformSetup(bool showDialog)
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.isLoaded) return;

            bool modified = false;

            // 1. Generar texturas PBR, senaletica vial y rotulacion tapatia
            RoadSignTextureGenerator.GenerateRoadSigns(showDialog: false);

            // 2. Construir, orientar, remapear materiales y volver estatico el entorno
            GuadalajaraStreetBuilder.BuildGuadalajaraStreet();
            modified = true;

            // 3. Configurar iluminacion de tarde nublada tapatia y neblina atmosferica
            UrbanLightingSetup.SetupSunsetAtmosphere(showDialog: false);

            // 4. Configurar GameManager con todos los subsistemas Runtime
            var gmObj = GameObject.Find("GameManager");
            if (gmObj != null)
            {
                modified |= EnsureComponent<MissionController>(gmObj);
                modified |= EnsureComponent<GameLoopManager>(gmObj);
                modified |= EnsureComponent<TacticalHUDCanvas>(gmObj);
                modified |= EnsureComponent<CardiacBiometricHUD>(gmObj);
                modified |= EnsureComponent<PhoneHUD>(gmObj);
                modified |= EnsureComponent<BiometricAudioSystem>(gmObj);
                modified |= EnsureComponent<UrbanSoundscapeController>(gmObj);
                modified |= EnsureComponent<ProtestAtmosphereController>(gmObj);
            }

            // 5. Configurar Player con audio de pasos, escaner y camara 1ra/3ra + zoom
            var playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                modified |= EnsureComponent<FootstepAudioController>(playerObj);
                modified |= EnsureComponent<EnvironmentScanner>(playerObj);
                modified |= EnsureComponent<PlayerCameraController>(playerObj);
            }

            // 6. Crear o encontrar el Marcador AR de Ruta Alternativa
            var markerObj = GameObject.Find("Marcador_Ruta_Alternativa");
            AlternativeRouteMarker markerComponent = null;

            if (markerObj == null)
            {
                markerObj = new GameObject("Marcador_Ruta_Alternativa");
                markerObj.transform.position = new Vector3(6.2f, 0.1f, 72.5f);
                markerComponent = markerObj.AddComponent<AlternativeRouteMarker>();
                Undo.RegisterCreatedObjectUndo(markerObj, "Crear Marcador AR");
                modified = true;
            }
            else
            {
                markerComponent = markerObj.GetComponent<AlternativeRouteMarker>();
                if (markerComponent == null)
                {
                    markerComponent = markerObj.AddComponent<AlternativeRouteMarker>();
                    modified = true;
                }
            }

            // 7. Conectar el evento de cierre al marcador y mantener la barricada activa desde el inicio
            var triggerCierre = Object.FindFirstObjectByType<DynamicRoadblockEvent>();
            var vallaObj = GameObject.Find("SM_Valla_Bloqueo");

            if (triggerCierre != null)
            {
                var so = new SerializedObject(triggerCierre);

                if (markerComponent != null)
                {
                    var propMarker = so.FindProperty("alternativeRouteMarker");
                    if (propMarker != null && propMarker.objectReferenceValue != markerComponent)
                    {
                        propMarker.objectReferenceValue = markerComponent;
                        modified = true;
                    }
                }

                if (vallaObj != null)
                {
                    var propBarrier = so.FindProperty("roadblockBarrier");
                    if (propBarrier != null && propBarrier.objectReferenceValue != vallaObj)
                    {
                        propBarrier.objectReferenceValue = vallaObj;
                        modified = true;
                    }

                    var col = vallaObj.GetComponent<BoxCollider>();
                    if (col == null)
                    {
                        col = vallaObj.AddComponent<BoxCollider>();
                        col.center = new Vector3(0f, 0.65f, 0f);
                        col.size = new Vector3(15f, 1.3f, 0.6f);
                        modified = true;
                    }
                }

                so.ApplyModifiedProperties();
            }

            // 8. Guardar la escena si hubo cambios
            if (modified)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("<color=cyan>[AUTO-SETUP COMPLETO]</color> Vertical Slice configurado (Audio, PBR, Scanner, FSM, uGUI, Protesta).");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Ruta Segura",
                    "Configuracion completada con exito:\n\n" +
                    "- Texturas PBR (Albedo + Normals), SSAO y GPU Instancing aplicados\n" +
                    "- Audio biometrico (BPM), pasos, tunel auditivo y ambiente 3D de protesta\n" +
                    "- Torretas policiacas, manta de protesta y rotulacion Floreria El Jazmin\n" +
                    "- Escaner activo [E]/[Q] ('Leyendo Entorno...') y HUD uGUI con mini-mapa en vivo\n" +
                    "- Cambio de camara [V]/[F5] (1ra/3ra persona) y zoom [RMB]/[Z] + rueda\n\n" +
                    "Presiona Play para probar.", "Aceptar");
            }
        }

        private static bool EnsureComponent<T>(GameObject target) where T : Component
        {
            if (target.GetComponent<T>() != null) return false;
            Undo.AddComponent<T>(target);
            return true;
        }
    }
}
