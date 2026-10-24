using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace RutaSegura.Editor
{
    /// <summary>
    /// Generador y ensamblador del entorno urbano real de Guadalajara (Av. Juarez y Calle Galeana).
    /// Diseno limpio, proporcional y probado: calle amplia de 15m, banquetas transitables y fachadas alineadas.
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

            // 1. Asegurar materiales actualizados
            UrbanPaletteGenerator.GenerateUrbanMaterials();
            RoadSignTextureGenerator.GenerateRoadSigns();

            // 2. Cargar prefabs / modelos FBX
            GameObject fbCanteraA = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsArchDir}/SM_Fachada_Cantera_A.fbx");
            GameObject fbColonialB = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsArchDir}/SM_Fachada_Colonial_B.fbx");
            GameObject fbFloreria = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsArchDir}/SM_Fachada_Floreria_Destino.fbx");
            GameObject fbFarola = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsPropsDir}/SM_Farola_Colonial_GDL.fbx");
            GameObject fbBanqueta = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsArchDir}/SM_Banqueta_GDL.fbx");
            GameObject fbValla = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsPropsDir}/SM_Valla_Bloqueo.fbx");
            GameObject fbCono = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsPropsDir}/SM_Cono_Trafico.fbx");

            Material matAsfalto = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/M_Asfalto_Calle.mat");

            // 3. Destruir entorno anterior para evitar objetos duplicados o traspapelados
            var existingEntorno = GameObject.Find("Entorno_Guadalajara");
            if (existingEntorno != null)
            {
                Undo.DestroyObjectImmediate(existingEntorno);
            }

            string[] obsoleteNames = { "MuroCentral", "Cube", "BanquetaIzquierda", "BanquetaDerecha" };
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
            Undo.RegisterCreatedObjectUndo(root, "Crear Entorno Guadalajara");

            // 5. Calzada Principal de Asfalto (Av. Juarez)
            var plane = GameObject.Find("Plane");
            if (plane != null)
            {
                plane.transform.position = new Vector3(4f, 0f, 15f);
                plane.transform.localScale = new Vector3(3.0f, 1f, 5.0f); // 30m ancho x 50m largo
                var r = plane.GetComponent<Renderer>();
                if (r != null && matAsfalto != null)
                {
                    r.sharedMaterial = matAsfalto;
                }
            }

            // 6. Configurar Jugador en posición inicial limpia
            var player = GameObject.Find("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(0f, 1f, 0f);
                player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }

            // =========================================================================
            // 7. ACERA Y FACHADAS IZQUIERDAS (Av. Juarez Norte) - X = -7.5m
            // =========================================================================
            float leftX = -7.5f;
            int[] zSteps = { -2, 4, 10, 16, 22 };

            for (int i = 0; i < zSteps.Length; i++)
            {
                int z = zSteps[i];

                // Banqueta izquierda
                if (fbBanqueta != null)
                {
                    var b = (GameObject)PrefabUtility.InstantiatePrefab(fbBanqueta);
                    b.name = $"Banqueta_Izq_{z}";
                    b.transform.parent = root.transform;
                    b.transform.position = new Vector3(-5.0f, 0f, z);
                    b.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                }

                // Edificio izquierdo (frente hacia +X)
                GameObject prefab = (i % 2 == 0) ? fbCanteraA : fbColonialB;
                if (prefab != null)
                {
                    var ed = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    ed.name = $"Edificio_JuarezNorte_{i}";
                    ed.transform.parent = root.transform;
                    ed.transform.position = new Vector3(leftX, 0f, z);
                    ed.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    AddBoxCollider(ed, new Vector3(6f, 8.5f, 2.0f), new Vector3(0f, 4.25f, -1.0f));
                }
            }

            // =========================================================================
            // 8. ACERA Y FACHADAS DERECHAS (Av. Juarez Sur con CRUCE a Calle Galeana)
            // =========================================================================
            float rightX = 7.5f;

            // Esquina inicial previa al cruce (Z = -2 y Z = 4)
            if (fbCanteraA != null)
            {
                var edEsquina = (GameObject)PrefabUtility.InstantiatePrefab(fbCanteraA);
                edEsquina.name = "Edificio_Esquina_Galeana_Sur";
                edEsquina.transform.parent = root.transform;
                edEsquina.transform.position = new Vector3(rightX, 0f, 0f);
                edEsquina.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                AddBoxCollider(edEsquina, new Vector3(6f, 8.5f, 2.0f), new Vector3(0f, 4.25f, -1.0f));
            }

            // Continuación de Av. Juárez después del cruce de Galeana (Z = 18 y 24)
            if (fbColonialB != null)
            {
                var edJuarezSurPost = (GameObject)PrefabUtility.InstantiatePrefab(fbColonialB);
                edJuarezSurPost.name = "Edificio_JuarezSur_PostCruce";
                edJuarezSurPost.transform.parent = root.transform;
                edJuarezSurPost.transform.position = new Vector3(rightX, 0f, 20f);
                edJuarezSurPost.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                AddBoxCollider(edJuarezSurPost, new Vector3(6f, 8.5f, 2.0f), new Vector3(0f, 4.25f, -1.0f));
            }

            // Banquetas del lado derecho (respetando la bocacalle de Galeana entre Z = 4 y Z = 14)
            if (fbBanqueta != null)
            {
                var bDer0 = (GameObject)PrefabUtility.InstantiatePrefab(fbBanqueta);
                bDer0.name = "Banqueta_Der_0";
                bDer0.transform.parent = root.transform;
                bDer0.transform.position = new Vector3(5.0f, 0f, 0f);
                bDer0.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

                var bDer20 = (GameObject)PrefabUtility.InstantiatePrefab(fbBanqueta);
                bDer20.name = "Banqueta_Der_20";
                bDer20.transform.parent = root.transform;
                bDer20.transform.position = new Vector3(5.0f, 0f, 20f);
                bDer20.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }

            // =========================================================================
            // 9. RECORRIDO ALTERNATIVO: CALLE GALEANA Y FLORERÍA EL JAZMÍN
            // =========================================================================
            // Fachada norte de Calle Galeana
            if (fbColonialB != null)
            {
                var edGaleanaNorte = (GameObject)PrefabUtility.InstantiatePrefab(fbColonialB);
                edGaleanaNorte.name = "Edificio_CalleGaleana_Norte";
                edGaleanaNorte.transform.parent = root.transform;
                edGaleanaNorte.transform.position = new Vector3(12.0f, 0f, 15.0f);
                edGaleanaNorte.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                AddBoxCollider(edGaleanaNorte, new Vector3(6f, 8.0f, 2.0f), new Vector3(0f, 4.0f, -1.0f));
            }

            // DESTINO: FLORERÍA EL JAZMÍN (SM_Fachada_Floreria_Destino)
            // Ubicada al final de Calle Galeana (X = 16.0, Z = 9.0), de frente al jugador que dobla a la derecha
            Vector3 floreriaPos = new Vector3(16.0f, 0f, 9.0f);
            if (fbFloreria != null)
            {
                var floreriaEd = (GameObject)PrefabUtility.InstantiatePrefab(fbFloreria);
                floreriaEd.name = "Edificio_Floreria_ElJazmin";
                floreriaEd.transform.parent = root.transform;
                floreriaEd.transform.position = floreriaPos;
                floreriaEd.transform.rotation = Quaternion.Euler(0f, -90f, 0f); // Frente hacia -X (hacia donde viene el jugador)
                AddBoxCollider(floreriaEd, new Vector3(6f, 7.5f, 2.0f), new Vector3(0f, 3.75f, -1.0f));
            }

            // 10. Actualizar Meta "Destino_Floreria"
            var destinoObj = GameObject.Find("Destino_Floreria");
            if (destinoObj != null)
            {
                destinoObj.transform.position = new Vector3(14.0f, 1f, 9.0f);
                var destCol = destinoObj.GetComponent<BoxCollider>();
                if (destCol != null)
                {
                    destCol.size = new Vector3(3f, 2.5f, 4f);
                    destCol.isTrigger = true;
                }
            }

            // 11. Farolas Coloniales Tapatías
            if (fbFarola != null)
            {
                Vector3[] farolaPositions = {
                    new Vector3(-4.8f, 0f, 2f),
                    new Vector3(-4.8f, 0f, 14f),
                    new Vector3(4.8f, 0f, 2f),
                    new Vector3(10.0f, 0f, 7.5f)
                };

                for (int f = 0; f < farolaPositions.Length; f++)
                {
                    var farola = (GameObject)PrefabUtility.InstantiatePrefab(fbFarola);
                    farola.name = $"Farola_Colonial_{f + 1}";
                    farola.transform.parent = root.transform;
                    farola.transform.position = farolaPositions[f];
                }
            }

            // 12. Valla de Bloqueo y Conos (Bloqueando Av. Juárez a la altura de Z = 12)
            var vallaObj = GameObject.Find("SM_Valla_Bloqueo");
            if (vallaObj != null)
            {
                vallaObj.transform.position = new Vector3(-1.0f, 0f, 12.0f);
                vallaObj.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

                // Colocar conos
                if (fbCono != null && vallaObj.transform.Find("Cono_Trafico_1") == null)
                {
                    var cono1 = (GameObject)PrefabUtility.InstantiatePrefab(fbCono);
                    cono1.name = "Cono_Trafico_1";
                    cono1.transform.parent = vallaObj.transform;
                    cono1.transform.position = new Vector3(-2.8f, 0f, 11.8f);

                    var cono2 = (GameObject)PrefabUtility.InstantiatePrefab(fbCono);
                    cono2.name = "Cono_Trafico_2";
                    cono2.transform.parent = vallaObj.transform;
                    cono2.transform.position = new Vector3(0.8f, 0f, 11.8f);
                }
            }

            // 13. Ajustar Triggers de Cierre y Marcador AR
            var triggerCierre = GameObject.Find("Trigger_Cierre");
            if (triggerCierre != null)
            {
                triggerCierre.transform.position = new Vector3(0f, 1f, 6.5f);
                var tCol = triggerCierre.GetComponent<BoxCollider>();
                if (tCol != null)
                {
                    tCol.size = new Vector3(10f, 3f, 2f);
                }
            }

            var marcadorObj = GameObject.Find("Marcador_Ruta_Alternativa");
            if (marcadorObj != null)
            {
                marcadorObj.transform.position = new Vector3(4.0f, 0.1f, 8.0f);
            }

            var dangerZone = GameObject.Find("Zona_Peligro_Calle");
            if (dangerZone != null)
            {
                dangerZone.transform.position = new Vector3(0f, 1f, 18.0f);
                var dCol = dangerZone.GetComponent<BoxCollider>();
                if (dCol != null)
                {
                    dCol.size = new Vector3(10f, 3f, 12f);
                }
            }

            // 14. Guardar cambios en la escena
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=green>[GUADALAJARA BUILDER]</color> Entorno limpio y proporcionado construido con éxito.");
        }

        private static void AddBoxCollider(GameObject obj, Vector3 size, Vector3 center)
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
