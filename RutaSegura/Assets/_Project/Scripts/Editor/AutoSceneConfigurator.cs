using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RutaSegura.Runtime.UI;
using RutaSegura.Runtime.Navigation;
using RutaSegura.Runtime.Events;

namespace RutaSegura.Editor
{
    /// <summary>
    /// Automatización completa de la escena para configurar todos los componentes,
    /// referencias y señalética sin necesidad de trabajo manual en el inspector.
    /// Se ejecuta automáticamente al cargar el proyecto o mediante el menú Tools.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoSceneConfigurator
    {
        static AutoSceneConfigurator()
        {
            // Ejecutar automáticamente tras compilar en Unity
            EditorApplication.delayCall += ExecuteAutoSetupSilent;
        }

        [MenuItem("Tools/Ruta Segura/Auto-Configurar Todo (1 Clic)")]
        public static void ExecuteAutoSetupManual()
        {
            PerformSetup(showDialog: true);
        }

        private static void ExecuteAutoSetupSilent()
        {
            PerformSetup(showDialog: false);
        }

        public static void PerformSetup(bool showDialog)
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.isLoaded) return;

            bool modified = false;

            // 1. Generar texturas y materiales de señalética vial
            RoadSignTextureGenerator.GenerateRoadSigns();

            // 2. Configurar GameManager con CardiacBiometricHUD
            var gmObj = GameObject.Find("GameManager");
            if (gmObj != null)
            {
                if (gmObj.GetComponent<CardiacBiometricHUD>() == null)
                {
                    Undo.AddComponent<CardiacBiometricHUD>(gmObj);
                    modified = true;
                    Debug.Log("<color=green>[AUTO-SETUP]</color> CardiacBiometricHUD añadido a GameManager.");
                }
            }

            // 3. Crear o encontrar el Marcador AR de Ruta Alternativa
            var markerObj = GameObject.Find("Marcador_Ruta_Alternativa");
            AlternativeRouteMarker markerComponent = null;

            if (markerObj == null)
            {
                markerObj = new GameObject("Marcador_Ruta_Alternativa");
                markerObj.transform.position = new Vector3(3f, 0.1f, 5f);
                markerComponent = markerObj.AddComponent<AlternativeRouteMarker>();
                Undo.RegisterCreatedObjectUndo(markerObj, "Crear Marcador AR");
                modified = true;
                Debug.Log("<color=green>[AUTO-SETUP]</color> Marcador AR creado en el asfalto (X: 3, Z: 5).");
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

            // 4. Conectar el evento de cierre al marcador y a la valla
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

                    // Asegurar que la valla tiene BoxCollider
                    var col = vallaObj.GetComponent<BoxCollider>();
                    if (col == null)
                    {
                        col = vallaObj.AddComponent<BoxCollider>();
                        col.center = new Vector3(0f, 0.6f, 0f);
                        col.size = new Vector3(2.4f, 1.2f, 0.6f);
                        modified = true;
                    }

                    // Asegurar que la valla empiece inactiva si el evento no ha ocurrido
                    if (vallaObj.activeSelf)
                    {
                        vallaObj.SetActive(false);
                        modified = true;
                    }
                }

                so.ApplyModifiedProperties();
            }

            // 5. Guardar la escena si hubo cambios
            if (modified)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("<color=cyan>[AUTO-SETUP COMPLETO]</color> Escena configurada y guardada al 100%.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Ruta Segura", 
                    "Configuración completada con éxito:\n\n" +
                    "• HUD biométrico de pulso activado en GameManager\n" +
                    "• Marcador AR proyectado en asfalto conectado\n" +
                    "• Señalética vial y valla vinculadas\n\n" +
                    "Presiona Play para probar.", "Aceptar");
            }
        }
    }
}
