using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace RutaSegura.Editor
{
    /// <summary>
    /// Generador y ensamblador del entorno urbano real de Guadalajara (Av. Juarez y Calle Galeana).
    /// Reemplaza los cubos grises de prueba por fachadas arquitectonicas coloniales/comerciales autenticas.
    /// </summary>
    public static class GuadalajaraStreetBuilder
    {
        private const string ModelsArchDir = "Assets/_Project/Models/Architecture";
        private const string ModelsPropsDir = "Assets/_Project/Models/Props";
        private const string MaterialsDir = "Assets/_Project/Materials";

        [MenuItem("Tools/Ruta Segura/Construir Entorno Real Guadalajara (1 Clic)")]
        public static void BuildGuadalajaraStreet()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.isLoaded) return;

            // 1. Asegurar paleta de materiales actualizada
            UrbanPaletteGenerator.GenerateUrbanMaterials();
            RoadSignTextureGenerator.GenerateRoadSigns();

            // 2. Cargar prefabs / modelos FBX
            GameObject fbCanteraA = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsArchDir}/SM_Fachada_Cantera_A.fbx");
            GameObject fbColonialB = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsArchDir}/SM_Fachada_Colonial_B.fbx");
            GameObject fbFloreria = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsArchDir}/SM_Fachada_Floreria_Destino.fbx");
            GameObject fbFarola = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsPropsDir}/SM_Farola_Colonial_GDL.fbx");
            GameObject fbBanqueta = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsArchDir}/SM_Banqueta_GDL.fbx");
            GameObject fbCono = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsPropsDir}/SM_Cono_Trafico.fbx");

            Material matAsfalto = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/M_Asfalto_Calle.mat");

            // 3. Limpiar cubos grises primitivos previos si existen
            string[] obsoleteNames = { "MuroCentral", "Cube", "BanquetaIzquierda", "BanquetaDerecha", "Entorno_Guadalajara" };
            foreach (var name in obsoleteNames)
            {
                var oldObj = GameObject.Find(name);
                if (oldObj != null)
                {
                    Undo.DestroyObjectImmediate(oldObj);
                }
            }

            // 4. Crear Contenedor Raiz de Entorno
            GameObject root = new GameObject("Entorno_Guadalajara");
            Undo.RegisterCreatedObjectUndo(root, "Crear Entorno Guadalajara");

            // 5. Ajustar o crear Calzada de Asfalto (Avenida Juarez)
            var plane = GameObject.Find("Plane");
            if (plane != null)
            {
                plane.transform.position = new Vector3(3f, 0f, 10f);
                plane.transform.localScale = new Vector3(3.5f, 1f, 4.0f); // 35m ancho x 40m largo
                var r = plane.GetComponent<Renderer>();
                if (r != null && matAsfalto != null)
                {
                    r.sharedMaterial = matAsfalto;
                }
            }

            // 6. Colocar Banquetas (Machuelos de Guadalajara)
            // Banqueta Izquierda (Av. Juarez Norte)
            for (int z = -4; z <= 26; z += 6)
            {
                if (fbBanqueta != null)
                {
                    var b = (GameObject)PrefabUtility.InstantiatePrefab(fbBanqueta);
                    b.name = $"Banqueta_Izq_{z}";
                    b.transform.parent = root.transform;
                    b.transform.position = new Vector3(-3.5f, 0f, z);
                    b.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    AddBoxColliderIfMissing(b, new Vector3(6f, 0.2f, 2.4f), new Vector3(0f, 0.1f, 1.2f));
                }
            }

            // Banqueta Derecha (Av. Juarez Sur con esquina en Galeana)
            for (int z = -4; z <= 2; z += 6)
            {
                if (fbBanqueta != null)
                {
                    var b = (GameObject)PrefabUtility.InstantiatePrefab(fbBanqueta);
                    b.name = $"Banqueta_Der_{z}";
                    b.transform.parent = root.transform;
                    b.transform.position = new Vector3(3.5f, 0f, z);
                    b.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                    AddBoxColliderIfMissing(b, new Vector3(6f, 0.2f, 2.4f), new Vector3(0f, 0.1f, 1.2f));
                }
            }

            // 7. Colocar Fila de Edificios Izquierda (Av. Juarez Norte)
            // Modelos autenticos tapatios con zocalo de cantera, cortinas metalicas de comercios y balcones
            float leftX = -4.7f;
            int[] leftZCoords = { -2, 4, 10, 16, 22 };
            for (int i = 0; i < leftZCoords.Length; i++)
            {
                GameObject prefab = (i % 2 == 0) ? fbCanteraA : fbColonialB;
                if (prefab != null)
                {
                    var ed = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    ed.name = $"Edificio_JuarezNorte_{i}";
                    ed.transform.parent = root.transform;
                    ed.transform.position = new Vector3(leftX, 0f, leftZCoords[i]);
                    ed.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    AddBoxColliderIfMissing(ed, new Vector3(6.0f, 8.5f, 2.5f), new Vector3(0f, 4.25f, 1.0f));
                }
            }

            // 8. Colocar Fila Derecha con ESQUINA Y CRUCE a Calle Galeana
            // Esquina inicial
            if (fbCanteraA != null)
            {
                var edEsquina = (GameObject)PrefabUtility.InstantiatePrefab(fbCanteraA);
                edEsquina.name = "Edificio_Esquina_Galeana";
                edEsquina.transform.parent = root.transform;
                edEsquina.transform.position = new Vector3(4.7f, 0f, -1f);
                edEsquina.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                AddBoxColliderIfMissing(edEsquina, new Vector3(6.0f, 8.5f, 2.5f), new Vector3(0f, 4.25f, 1.0f));
            }

            // Cruce peatonal / vehicular a Calle Galeana: Z = 4 a Z = 8 libre para cruzar a la derecha!
            // Fachada a lo largo de Calle Galeana (recorrido alternativo):
            if (fbColonialB != null)
            {
                var edGaleana1 = (GameObject)PrefabUtility.InstantiatePrefab(fbColonialB);
                edGaleana1.name = "Edificio_CalleGaleana_1";
                edGaleana1.transform.parent = root.transform;
                edGaleana1.transform.position = new Vector3(9.5f, 0f, 4.5f);
                edGaleana1.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                AddBoxColliderIfMissing(edGaleana1, new Vector3(6.0f, 8.0f, 2.5f), new Vector3(0f, 4.0f, 1.0f));
            }

            // 9. Colocar el DESTINO: FLORERÍA EL JAZMÍN (SM_Fachada_Floreria_Destino)
            // Ubicada al final de la ruta alternativa en Calle Galeana / Pedro Moreno
            Vector3 floreriaPos = new Vector3(8.5f, 0f, 14.5f);
            if (fbFloreria != null)
            {
                var floreriaEd = (GameObject)PrefabUtility.InstantiatePrefab(fbFloreria);
                floreriaEd.name = "Edificio_Floreria_ElJazmin";
                floreriaEd.transform.parent = root.transform;
                floreriaEd.transform.position = floreriaPos;
                floreriaEd.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                AddBoxColliderIfMissing(floreriaEd, new Vector3(6.0f, 8.0f, 2.5f), new Vector3(0f, 4.0f, 1.0f));
            }

            // 10. Actualizar posicion del Trigger y Meta "Destino_Floreria"
            var destinoObj = GameObject.Find("Destino_Floreria");
            if (destinoObj != null)
            {
                destinoObj.transform.position = new Vector3(8.5f, 1f, 12.8f);
                var destCol = destinoObj.GetComponent<BoxCollider>();
                if (destCol != null)
                {
                    destCol.size = new Vector3(4f, 2.5f, 2.5f);
                    destCol.isTrigger = true;
                }
            }

            // 11. Colocar Farolas Coloniales Tapatías
            if (fbFarola != null)
            {
                Vector3[] farolaPositions = {
                    new Vector3(-3.2f, 0.18f, 0f),
                    new Vector3(-3.2f, 0.18f, 12f),
                    new Vector3(3.2f, 0.18f, 0f),
                    new Vector3(7.0f, 0.18f, 10f)
                };

                for (int f = 0; f < farolaPositions.Length; f++)
                {
                    var farola = (GameObject)PrefabUtility.InstantiatePrefab(fbFarola);
                    farola.name = $"Farola_Colonial_{f + 1}";
                    farola.transform.parent = root.transform;
                    farola.transform.position = farolaPositions[f];
                    farola.transform.rotation = Quaternion.Euler(0f, (farolaPositions[f].x < 0) ? 90f : -90f, 0f);
                }
            }

            // 12. Configurar la Valla de Bloqueo con sus Conos de Tráfico
            var vallaObj = GameObject.Find("SM_Valla_Bloqueo");
            if (vallaObj != null)
            {
                vallaObj.transform.position = new Vector3(-1.0f, 0f, 7.2f);
                vallaObj.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

                // Colocar conos al lado si no existen
                if (fbCono != null && vallaObj.transform.Find("Cono_Trafico_1") == null)
                {
                    var cono1 = (GameObject)PrefabUtility.InstantiatePrefab(fbCono);
                    cono1.name = "Cono_Trafico_1";
                    cono1.transform.parent = vallaObj.transform;
                    cono1.transform.position = new Vector3(-2.8f, 0f, 7.0f);

                    var cono2 = (GameObject)PrefabUtility.InstantiatePrefab(fbCono);
                    cono2.name = "Cono_Trafico_2";
                    cono2.transform.parent = vallaObj.transform;
                    cono2.transform.position = new Vector3(0.6f, 0f, 7.0f);
                }
            }

            // 13. Ajustar Triggers de Evento y Marcador AR
            var triggerCierre = GameObject.Find("Trigger_Cierre");
            if (triggerCierre != null)
            {
                triggerCierre.transform.position = new Vector3(-1.0f, 1f, 5.0f);
                var tCol = triggerCierre.GetComponent<BoxCollider>();
                if (tCol != null)
                {
                    tCol.size = new Vector3(6f, 3f, 2f);
                }
            }

            var marcadorObj = GameObject.Find("Marcador_Ruta_Alternativa");
            if (marcadorObj != null)
            {
                marcadorObj.transform.position = new Vector3(2.5f, 0.1f, 5.2f);
            }

            var dangerZone = GameObject.Find("Zona_Peligro_Calle");
            if (dangerZone != null)
            {
                dangerZone.transform.position = new Vector3(-1.0f, 1f, 10.0f);
                var dCol = dangerZone.GetComponent<BoxCollider>();
                if (dCol != null)
                {
                    dCol.size = new Vector3(6f, 3f, 8f);
                }
            }

            // 14. Guardar cambios en la escena
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=green>[GUADALAJARA BUILDER]</color> Entorno urbano de Guadalajara ensamblado con exito en la escena.");
        }

        private static void AddBoxColliderIfMissing(GameObject obj, Vector3 size, Vector3 center)
        {
            var col = obj.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = obj.AddComponent<BoxCollider>();
            }
            col.size = size;
            col.center = center;
        }
    }
}
