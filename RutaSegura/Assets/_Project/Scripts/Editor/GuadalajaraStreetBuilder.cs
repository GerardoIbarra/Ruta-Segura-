using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace RutaSegura.Editor
{
    /// <summary>
    /// Ensamblador del entorno urbano solido y horizontal de Guadalajara (Av. Juarez y Calle Galeana).
    /// Utiliza manzanas urbanas continuas horizontales en el piso, banqueta integrada y MeshColliders precisos.
    /// </summary>
    public static class GuadalajaraStreetBuilder
    {
        private const string ModelsArchDir = "Assets/_Project/Models/Architecture";
        private const string ModelsPropsDir = "Assets/_Project/Models/Props";

        [MenuItem("Tools/Ruta Segura/Construir Entorno Real Guadalajara (1 Clic)")]
        public static void BuildGuadalajaraStreet()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.isLoaded) return;

            // 1. Asegurar materiales y texturas
            UrbanPaletteGenerator.GenerateUrbanMaterials();
            RoadSignTextureGenerator.GenerateRoadSigns();

            // 2. Cargar bloques maestros de arquitectura continua
            GameObject fbCalzada = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsArchDir}/SM_Calzada_Asfalto_Completa.fbx");
            GameObject fbNorte = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsArchDir}/SM_Manzana_Juarez_Norte.fbx");
            GameObject fbSur = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsArchDir}/SM_Manzana_Juarez_Sur_Esquina.fbx");
            GameObject fbFloreria = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsArchDir}/SM_Manzana_Floreria_Destino.fbx");
            GameObject fbFarola = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsPropsDir}/SM_Farola_Colonial_GDL.fbx");
            GameObject fbValla = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsPropsDir}/SM_Valla_Bloqueo.fbx");
            GameObject fbCono = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsPropsDir}/SM_Cono_Trafico.fbx");

            // 3. Limpiar cualquier entorno o plano primitivo previo
            var existingEntorno = GameObject.Find("Entorno_Guadalajara");
            if (existingEntorno != null)
            {
                Undo.DestroyObjectImmediate(existingEntorno);
            }

            string[] obsoleteNames = { "MuroCentral", "Cube", "Plane", "BanquetaIzquierda", "BanquetaDerecha" };
            foreach (var name in obsoleteNames)
            {
                var oldObj = GameObject.Find(name);
                if (oldObj != null)
                {
                    Undo.DestroyObjectImmediate(oldObj);
                }
            }

            // 4. Crear Contenedor Raiz Limpio
            GameObject root = new GameObject("Entorno_Guadalajara");
            Undo.RegisterCreatedObjectUndo(root, "Crear Entorno Guadalajara Horizontal");

            // 5. Instanciar Bloques Maestros (encajan horizontalmente en el piso en 0, 0, 0)
            InstantiateMasterBlock(fbCalzada, "Calzada_Asfalto_Completa", root.transform, Vector3.zero);
            InstantiateMasterBlock(fbNorte, "Manzana_Juarez_Norte", root.transform, Vector3.zero);
            InstantiateMasterBlock(fbSur, "Manzana_Juarez_Sur_Esquina", root.transform, Vector3.zero);
            InstantiateMasterBlock(fbFloreria, "Manzana_Floreria_Destino", root.transform, Vector3.zero);

            // 6. Colocar al Jugador en el inicio de la calzada
            var player = GameObject.Find("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(0f, 1.0f, 0f);
                player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }

            // 7. Colocar Farolas Coloniales Tapatías sobre las banquetas
            if (fbFarola != null)
            {
                Vector3[] farolaPositions = {
                    new Vector3(-5.8f, 0.18f, 2.0f),
                    new Vector3(-5.8f, 0.18f, 14.0f),
                    new Vector3(-5.8f, 0.18f, 26.0f),
                    new Vector3(5.8f, 0.18f, 2.0f),
                    new Vector3(5.8f, 0.18f, 20.0f),
                    new Vector3(12.0f, 0.18f, 6.5f),
                    new Vector3(12.0f, 0.18f, 15.5f)
                };

                for (int f = 0; f < farolaPositions.Length; f++)
                {
                    var farola = (GameObject)PrefabUtility.InstantiatePrefab(fbFarola);
                    farola.name = $"Farola_Colonial_{f + 1}";
                    farola.transform.parent = root.transform;
                    farola.transform.position = farolaPositions[f];
                }
            }

            // 8. Colocar Valla de Bloqueo en Av. Juárez (en Z = 13.5m)
            var vallaObj = GameObject.Find("SM_Valla_Bloqueo");
            if (vallaObj == null && fbValla != null)
            {
                vallaObj = (GameObject)PrefabUtility.InstantiatePrefab(fbValla);
                vallaObj.name = "SM_Valla_Bloqueo";
            }

            if (vallaObj != null)
            {
                vallaObj.transform.parent = root.transform;
                vallaObj.transform.position = new Vector3(0f, 0f, 13.5f);
                vallaObj.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

                // Colocar conos a los costados de la valla
                if (fbCono != null && vallaObj.transform.Find("Cono_Trafico_1") == null)
                {
                    var cono1 = (GameObject)PrefabUtility.InstantiatePrefab(fbCono);
                    cono1.name = "Cono_Trafico_1";
                    cono1.transform.parent = vallaObj.transform;
                    cono1.transform.position = new Vector3(-2.2f, 0f, 13.3f);

                    var cono2 = (GameObject)PrefabUtility.InstantiatePrefab(fbCono);
                    cono2.name = "Cono_Trafico_2";
                    cono2.transform.parent = vallaObj.transform;
                    cono2.transform.position = new Vector3(2.2f, 0f, 13.3f);
                }
            }

            // 9. Configurar Triggers y Marcadores
            var triggerCierre = GameObject.Find("Trigger_Cierre");
            if (triggerCierre != null)
            {
                triggerCierre.transform.position = new Vector3(0f, 1f, 7.5f);
                var tCol = triggerCierre.GetComponent<BoxCollider>();
                if (tCol != null)
                {
                    tCol.size = new Vector3(10f, 3f, 2f);
                }
            }

            var marcadorObj = GameObject.Find("Marcador_Ruta_Alternativa");
            if (marcadorObj != null)
            {
                marcadorObj.transform.position = new Vector3(3.0f, 0.1f, 8.5f);
            }

            var destinoObj = GameObject.Find("Destino_Floreria");
            if (destinoObj != null)
            {
                destinoObj.transform.position = new Vector3(18.0f, 1f, 11.0f);
                var destCol = destinoObj.GetComponent<BoxCollider>();
                if (destCol != null)
                {
                    destCol.size = new Vector3(4f, 3f, 4f);
                    destCol.isTrigger = true;
                }
            }

            var dangerZone = GameObject.Find("Zona_Peligro_Calle");
            if (dangerZone != null)
            {
                dangerZone.transform.position = new Vector3(0f, 1f, 20.0f);
                var dCol = dangerZone.GetComponent<BoxCollider>();
                if (dCol != null)
                {
                    dCol.size = new Vector3(10f, 3f, 12f);
                }
            }

            // 10. Guardar la escena
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=green>[GUADALAJARA BUILDER]</color> Manzanas urbanas horizontales ensambladas con éxito.");
        }

        private static void InstantiateMasterBlock(GameObject prefab, string name, Transform parent, Vector3 localPos)
        {
            if (prefab == null) return;
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            obj.name = name;
            obj.transform.parent = parent;
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.identity;

            // Asegurar MeshCollider en cada malla para colisión sólida en el piso y muros
            var meshFilters = obj.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (mf.GetComponent<Collider>() == null)
                {
                    var col = mf.gameObject.AddComponent<MeshCollider>();
                    col.sharedMesh = mf.sharedMesh;
                }
            }
        }
    }
}
