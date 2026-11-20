using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RutaSegura.Runtime.Events;
using RutaSegura.Runtime.Navigation;

namespace RutaSegura.Editor
{
    /// <summary>
    /// Ensamblador de la ruta larga de Guadalajara (~200 m):
    /// Av. Juarez -> barricada/protesta en Av. Hidalgo -> Callejon 1 -> Callejon 2 -> Calle Hospital -> Floreria El Jazmin.
    /// Remapea materiales PBR con GPU Instancing, activa Static Batching en bloques urbanos,
    /// coloca puntos de luz en cruces/floreria y conecta triggers y marcadores secuenciales.
    /// </summary>
    public static class GuadalajaraStreetBuilder
    {
        private const string ModelsDir = "Assets/_Project/Models";
        private const string ArchDir = ModelsDir + "/Architecture";
        private const string PropsDir = ModelsDir + "/Props";
        private const string MaterialsDir = "Assets/_Project/Materials";
        private const string BoundsFile = "_Project/Models/layout_bounds.json";

        public static readonly Vector3 PlayerStart = new Vector3(0f, 1.0f, 22f);

        [Serializable] private class BoundsItem { public string name; public Vector3 min; public Vector3 max; }
        [Serializable] private class BoundsList { public BoundsItem[] items; }

        private static Dictionary<string, Bounds> expected;

        [MenuItem("Tools/Ruta Segura/Construir Entorno Real Guadalajara (1 Clic)")]
        public static void BuildGuadalajaraStreet()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.isLoaded) return;

            UrbanPaletteGenerator.GenerateUrbanMaterials(forceRegenerate: false);
            RoadSignTextureGenerator.GenerateRoadSigns(showDialog: false);
            LoadExpectedBounds();
            var materialMap = LoadMaterialMap();

            // 1. Limpiar entorno previo (incluye objetos inactivos en la raiz)
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name == "Entorno_Guadalajara" || go.name == "SM_Valla_Bloqueo" ||
                    go.name == "MuroCentral" || go.name == "Cube" || go.name == "Plane" ||
                    go.name == "BanquetaIzquierda" || go.name == "BanquetaDerecha")
                {
                    Undo.DestroyObjectImmediate(go);
                }
            }

            var root = new GameObject("Entorno_Guadalajara");
            Undo.RegisterCreatedObjectUndo(root, "Crear Entorno Guadalajara");

            // 2. Bloques urbanos maestros (estaticos para Static Batching y oclusion)
            var bloques = new GameObject("Bloques_Urbanos").transform;
            bloques.SetParent(root.transform, false);
            MarkStaticRecursive(Place(ArchDir, "SM_Ruta_Piso_Completo", bloques, Vector3.zero, 0f, "Piso_Ruta_Completa", true));
            MarkStaticRecursive(Place(ArchDir, "SM_Manzanas_Juarez_Hidalgo", bloques, Vector3.zero, 0f, "Manzanas_Juarez_Hidalgo", true));
            MarkStaticRecursive(Place(ArchDir, "SM_Manzanas_Callejones_Galeana", bloques, Vector3.zero, 0f, "Manzanas_Callejones_Galeana", true));
            MarkStaticRecursive(Place(ArchDir, "SM_Manzanas_Hospital_Floreria", bloques, Vector3.zero, 0f, "Manzanas_Hospital_Floreria", true));

            // 3. Farolas (y = 0.18 sobre la banqueta). Brazo local hacia +X.
            var farolas = new GameObject("Farolas").transform;
            farolas.SetParent(root.transform, false);
            int fi = 0;
            for (float z = -36f; z <= 66f; z += 14f)
            {
                Place(PropsDir, "SM_Farola_Colonial_GDL", farolas, new Vector3(-6.8f, 0.18f, z), 0f, $"Farola_{++fi}", true);
                Place(PropsDir, "SM_Farola_Colonial_GDL", farolas, new Vector3(6.8f, 0.18f, z + 7f), 180f, $"Farola_{++fi}", true);
            }
            for (float x = 38f; x <= 90f; x += 13f)
            {
                Place(PropsDir, "SM_Farola_Colonial_GDL", farolas, new Vector3(x, 0.18f, 21.7f), 90f, $"Farola_{++fi}", true);
                Place(PropsDir, "SM_Farola_Colonial_GDL", farolas, new Vector3(x + 6f, 0.18f, 8.3f), -90f, $"Farola_{++fi}", true);
            }
            Place(PropsDir, "SM_Farola_Colonial_GDL", farolas, new Vector3(46.3f, 0.18f, 52f), 0f, $"Farola_{++fi}", true);
            Place(PropsDir, "SM_Farola_Colonial_GDL", farolas, new Vector3(53.7f, 0.18f, 60f), 180f, $"Farola_{++fi}", true);
            Place(PropsDir, "SM_Farola_Colonial_GDL", farolas, new Vector3(53.7f, 0.18f, 88f), 180f, $"Farola_{++fi}", true);

            // 4. Luces calidas y halos volumetricos en farolas y Floreria (sin sombras extra para 90-120 FPS)
            var luces = new GameObject("Luces_Guia").transform;
            luces.SetParent(root.transform, false);
            Color warmStreet = new Color(1.0f, 0.78f, 0.48f);
            materialMap.TryGetValue("M_Halo_Farola", out Material haloWarm);
            materialMap.TryGetValue("M_Halo_Floreria", out Material haloFloreria);
            Mesh haloConeMesh = GetOrCreateHaloConeMesh();

            AddPointLight(luces, "Luz_Juarez_1", new Vector3(-5.8f, 3.8f, 34f), warmStreet, 15f, 4.2f, haloWarm, haloConeMesh);
            AddPointLight(luces, "Luz_Juarez_2", new Vector3(5.8f, 3.8f, 48f), warmStreet, 15f, 4.2f, haloWarm, haloConeMesh);
            AddPointLight(luces, "Luz_Juarez_3", new Vector3(-5.8f, 3.8f, 62f), warmStreet, 15f, 4.2f, haloWarm, haloConeMesh);
            AddPointLight(luces, "Luz_Callejon_1", new Vector3(6.5f, 3.6f, 71.0f), warmStreet, 14f, 4.0f, haloWarm, haloConeMesh);
            AddPointLight(luces, "Luz_Callejon_2", new Vector3(29.5f, 3.6f, 71.5f), warmStreet, 14f, 4.0f, haloWarm, haloConeMesh);
            AddPointLight(luces, "Luz_Callejon_Mid", new Vector3(29.5f, 3.6f, 46.0f), warmStreet, 14f, 3.6f, haloWarm, haloConeMesh);
            AddPointLight(luces, "Luz_Hospital_Entrada", new Vector3(29.5f, 3.6f, 20.5f), warmStreet, 15f, 4.0f, haloWarm, haloConeMesh);
            AddPointLight(luces, "Luz_Hospital_Mid", new Vector3(62.0f, 3.8f, 15.0f), warmStreet, 16f, 4.0f, haloWarm, haloConeMesh);
            AddPointLight(luces, "Luz_Floreria_Faro", new Vector3(93.2f, 3.6f, 15.0f), new Color(0.82f, 1.0f, 0.76f), 18f, 5.5f, haloFloreria ?? haloWarm, haloConeMesh);

            // 5. Barricada de Av. Juarez visible desde el inicio con senaletica vial
            var barricada = new GameObject("SM_Valla_Bloqueo");
            barricada.transform.SetParent(root.transform, false);
            barricada.transform.position = new Vector3(0f, 0f, 79.6f);
            for (int i = -2; i <= 2; i++)
            {
                Place(PropsDir, "SM_Valla_Bloqueo", barricada.transform, new Vector3(i * 2.45f, 0f, 79.6f), 0f, $"Valla_{i + 3}", false);
            }
            Place(PropsDir, "SM_Cono_Obra", barricada.transform, new Vector3(-6.2f, 0f, 78.8f), 0f, "Cono_Valla_1", false);
            Place(PropsDir, "SM_Cono_Obra", barricada.transform, new Vector3(6.2f, 0f, 78.8f), 0f, "Cono_Valla_2", false);
            var bCol = barricada.AddComponent<BoxCollider>();
            bCol.center = new Vector3(0f, 0.65f, 0f);
            bCol.size = new Vector3(15f, 1.3f, 0.6f);

            // 6. Manifestacion y policia (Av. Hidalgo + Calle Galeana)
            var multitud = new GameObject("Manifestacion").transform;
            multitud.SetParent(root.transform, false);
            Place(PropsDir, "SM_Linea_Policia", multitud, new Vector3(0f, 0f, 83.2f), 0f, "Policia_Juarez", true);
            Place(PropsDir, "SM_Multitud_Protesta", multitud, new Vector3(0f, 0f, 89f), 0f, "Multitud_Juarez", true);
            Place(PropsDir, "SM_Multitud_Protesta", multitud, new Vector3(-16f, 0f, 88.5f), 20f, "Multitud_Hidalgo", true);
            Place(PropsDir, "SM_Multitud_Protesta", multitud, new Vector3(15f, 0f, 88.5f), -20f, "Multitud_Hidalgo_Este", true);
            Place(PropsDir, "SM_Patrulla", multitud, new Vector3(12f, 0f, 88f), 90f, "Patrulla_Hidalgo_1", true);
            Place(PropsDir, "SM_Patrulla", multitud, new Vector3(-28f, 0f, 87.5f), 80f, "Patrulla_Hidalgo_2", true);
            Place(PropsDir, "SM_Linea_Policia", multitud, new Vector3(47.2f, 0f, 72.5f), 90f, "Policia_Galeana", true);
            Place(PropsDir, "SM_Multitud_Protesta", multitud, new Vector3(51f, 0f, 78f), 90f, "Multitud_Galeana", true);
            Place(PropsDir, "SM_Patrulla", multitud, new Vector3(50f, 0f, 63f), 10f, "Patrulla_Galeana_1", true);
            Place(PropsDir, "SM_Patrulla", multitud, new Vector3(50.5f, 0f, 90f), -5f, "Patrulla_Galeana_2", true);

            // 7. Callejones: contenedores y bolsas de basura orientados hacia el interior del pasillo
            var callejon = new GameObject("Utileria_Callejones").transform;
            callejon.SetParent(root.transform, false);
            Place(PropsDir, "SM_Contenedor_Basura", callejon, new Vector3(11f, 0f, 70.75f), 180f, "Contenedor_1", true);
            Place(PropsDir, "SM_Contenedor_Basura", callejon, new Vector3(21f, 0f, 74.25f), 0f, "Contenedor_2", true);
            Place(PropsDir, "SM_Contenedor_Basura", callejon, new Vector3(27.75f, 0f, 56f), -90f, "Contenedor_3", true);
            Place(PropsDir, "SM_Contenedor_Basura", callejon, new Vector3(31.25f, 0f, 44f), 90f, "Contenedor_4", true);
            Place(PropsDir, "SM_Contenedor_Basura", callejon, new Vector3(27.75f, 0f, 34f), -90f, "Contenedor_5", true);

            // 8. Cruce de Calle Hospital: conos y cinta de precaucion (acordonando de pared a pared el extremo oeste)
            var obra = new GameObject("Obra_Calle_Hospital").transform;
            obra.SetParent(root.transform, false);
            Place(PropsDir, "SM_Cinta_Precaucion", obra, new Vector3(20.5f, 0f, 12.5f), 90f, "Cinta_Hospital_Oeste_1", true);
            Place(PropsDir, "SM_Cinta_Precaucion", obra, new Vector3(20.5f, 0f, 17.5f), 90f, "Cinta_Hospital_Oeste_2", true);
            Place(PropsDir, "SM_Cinta_Precaucion", obra, new Vector3(23.5f, 0f, 19.2f), 0f, "Cinta_Hospital_Norte", true);
            Vector3[] conos = {
                new Vector3(21.8f, 0f, 11f), new Vector3(21.8f, 0f, 13.5f), new Vector3(21.8f, 0f, 16.5f),
                new Vector3(21.8f, 0f, 19f), new Vector3(26.4f, 0f, 19.3f), new Vector3(33f, 0f, 19.3f),
                new Vector3(35f, 0f, 16f)
            };
            for (int i = 0; i < conos.Length; i++)
            {
                Place(PropsDir, "SM_Cono_Obra", obra, conos[i], 0f, $"Cono_Obra_{i + 1}", false);
            }

            // 9. Bloqueos invisibles anti-exploit (barricada, policia y limites de mapa)
            AddBlocker(root.transform, "Bloqueo_Invisible_Juarez", new Vector3(0f, 2f, 80.6f), new Vector3(15.5f, 4f, 0.5f));
            AddBlocker(root.transform, "Bloqueo_Invisible_Galeana", new Vector3(45.3f, 2f, 72.5f), new Vector3(0.5f, 4f, 5.5f));
            AddBlocker(root.transform, "Bloqueo_Sur_Juarez", new Vector3(0f, 2f, -40f), new Vector3(16f, 4f, 1f));
            AddBlocker(root.transform, "Bloqueo_Norte_Juarez", new Vector3(0f, 2f, 122f), new Vector3(16f, 4f, 1f));
            AddBlocker(root.transform, "Bloqueo_Oeste_Hospital", new Vector3(19.5f, 2f, 15f), new Vector3(1f, 4f, 12f));

            // 10. Jugador en Av. Juarez mirando hacia +Z
            var player = GameObject.Find("Player");
            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.transform.SetPositionAndRotation(PlayerStart, Quaternion.identity);
                if (cc != null) cc.enabled = true;
            }

            // 11. Triggers, marcadores AR secuenciales y destino (~200 m totales)
            SetBox("Trigger_Cierre", new Vector3(0f, 1.5f, 56f), new Vector3(10f, 3f, 2f), true);
            SetBox("Zona_Peligro_Calle", new Vector3(0f, 1.5f, 75f), new Vector3(10f, 3f, 9f), true);
            SetBox("Destino_Floreria", new Vector3(92.5f, 1.5f, 15f), new Vector3(4f, 3f, 10f), true);

            AlternativeRouteMarker marker1 = null;
            var marcadorObj = GameObject.Find("Marcador_Ruta_Alternativa");
            if (marcadorObj != null)
            {
                marcadorObj.transform.position = new Vector3(6.2f, 0.1f, 72.5f);
                marker1 = marcadorObj.GetComponent<AlternativeRouteMarker>();
                SetMarkerText(marker1, "Ruta Alternativa Detectada", "Callejon del Refugio ->");
            }

            var roadblockEvent = UnityEngine.Object.FindAnyObjectByType<DynamicRoadblockEvent>();
            if (roadblockEvent != null)
            {
                var soEvt = new SerializedObject(roadblockEvent);
                var pBar = soEvt.FindProperty("roadblockBarrier");
                if (pBar != null) pBar.objectReferenceValue = barricada;
                if (marker1 != null)
                {
                    var pMrk = soEvt.FindProperty("alternativeRouteMarker");
                    if (pMrk != null) pMrk.objectReferenceValue = marker1;
                }
                soEvt.ApplyModifiedPropertiesWithoutUndo();
            }

            // Segundo marcador: giro al sur en Callejon 2 antes del cordon policial en Galeana (dispara fase Detour)
            var markerCallejon2Obj = new GameObject("Marcador_Callejon_2");
            markerCallejon2Obj.transform.SetParent(root.transform, false);
            markerCallejon2Obj.transform.position = new Vector3(29.5f, 0.1f, 70.5f);
            var markerCallejon2 = markerCallejon2Obj.AddComponent<AlternativeRouteMarker>();
            SetMarkerText(markerCallejon2, "Desvio por Callejon", "Girar a la derecha ->");
            CreateMarkerTrigger(root.transform, "Trigger_Callejon_1", new Vector3(16f, 1.5f, 72.5f), new Vector3(2f, 3f, 5f), markerCallejon2, marker1, entersDetourPhase: true);

            // Tercer marcador: salida a Calle Hospital hacia Floreria El Jazmin
            var markerHospitalObj = new GameObject("Marcador_Calle_Hospital");
            markerHospitalObj.transform.SetParent(root.transform, false);
            markerHospitalObj.transform.position = new Vector3(29.5f, 0.1f, 18.0f);
            var markerHospital = markerHospitalObj.AddComponent<AlternativeRouteMarker>();
            SetMarkerText(markerHospital, "Nueva Ruta", "Calle Hospital ->");
            CreateMarkerTrigger(root.transform, "Trigger_Callejon_2", new Vector3(29.5f, 1.5f, 38f), new Vector3(5f, 3f, 2f), markerHospital, markerCallejon2, entersDetourPhase: false);

            // 11B. Ciudad Viva: trafico detenido, mobiliario urbano, vegetacion y semaforos
            var carPaintOverrides = new List<(GameObject carObj, string paintMatName)>();

            var trafico = new GameObject("Trafico_Detenido").transform;
            trafico.SetParent(root.transform, false);
            PlaceCar(trafico, "Auto_Juarez_Taxi_1", new Vector3(-3.2f, 0f, 33.5f), 176f, "M_Auto_Taxi", carPaintOverrides);
            PlaceCar(trafico, "Auto_Juarez_Rojo_2", new Vector3(3.2f, 0f, 41.0f), 184f, "M_Auto_Rojo", carPaintOverrides);
            Place(PropsDir, "SM_Camion_Urbano", trafico, new Vector3(-2.8f, 0f, 52.0f), 180f, "Camion_Urbano_Juarez", true);
            PlaceCar(trafico, "Auto_Juarez_Gris_3", new Vector3(3.2f, 0f, 62.5f), 168f, "M_Auto_Gris", carPaintOverrides);
            PlaceCar(trafico, "Auto_Juarez_Azul_4", new Vector3(-3.2f, 0f, 68.0f), 182f, "M_Auto_Azul", carPaintOverrides);
            PlaceCar(trafico, "Auto_Juarez_Sur_5", new Vector3(-3.2f, 0f, 12.0f), 180f, "M_Auto_Blanco", carPaintOverrides);
            PlaceCar(trafico, "Auto_Juarez_Sur_6", new Vector3(3.3f, 0f, 4.0f), 180f, "M_Auto_Taxi", carPaintOverrides);
            PlaceCar(trafico, "Auto_Hospital_1", new Vector3(43.0f, 0f, 11.4f), -90f, "M_Auto_Blanco", carPaintOverrides);
            PlaceCar(trafico, "Auto_Hospital_2", new Vector3(54.5f, 0f, 18.6f), 90f, "M_Auto_Taxi", carPaintOverrides);
            PlaceCar(trafico, "Auto_Hospital_3", new Vector3(67.5f, 0f, 11.4f), -88f, "M_Auto_Azul", carPaintOverrides);
            PlaceCar(trafico, "Auto_Hospital_4", new Vector3(78.5f, 0f, 18.6f), 92f, "M_Auto_Rojo", carPaintOverrides);
            Place(PropsDir, "SM_Moto_Reparto", trafico, new Vector3(4.4f, 0f, 26.5f), 165f, "Moto_Reparto_Juarez", true);
            Place(PropsDir, "SM_Moto_Reparto", trafico, new Vector3(15.5f, 0f, 70.65f), -85f, "Moto_Reparto_Callejon1", true);
            Place(PropsDir, "SM_Moto_Reparto", trafico, new Vector3(31.3f, 0f, 61.0f), 175f, "Moto_Reparto_Callejon2", true);
            Place(PropsDir, "SM_Moto_Reparto", trafico, new Vector3(89.2f, 0f, 11.2f), -65f, "Moto_Reparto_Floreria", true);

            var mobiliario = new GameObject("Mobiliario_Urbano").transform;
            mobiliario.SetParent(root.transform, false);
            Place(PropsDir, "SM_Puesto_Periodicos", mobiliario, new Vector3(-6.5f, 0.18f, 29.0f), -90f, "Puesto_Periodicos_Juarez", true);
            Place(PropsDir, "SM_Parada_Camion", mobiliario, new Vector3(6.4f, 0.18f, 36.0f), 90f, "Parada_Camion_Juarez", true);
            Place(PropsDir, "SM_Puesto_Periodicos", mobiliario, new Vector3(58.5f, 0.18f, 21.4f), 0f, "Puesto_Periodicos_Hospital", true);
            Place(PropsDir, "SM_Parada_Camion", mobiliario, new Vector3(72.0f, 0.18f, 8.6f), 180f, "Parada_Camion_Hospital", true);
            var benches = new (string name, Vector3 pos, float yaw)[]
            {
                ("Banca_Juarez_1", new Vector3(-6.5f, 0.18f, 43.0f), -90f),
                ("Banca_Juarez_2", new Vector3(6.5f, 0.18f, 51.0f), 90f),
                ("Banca_Juarez_3", new Vector3(-6.5f, 0.18f, 58.0f), -90f),
                ("Banca_Juarez_4", new Vector3(6.5f, 0.18f, 23.0f), 90f),
                ("Banca_Callejon2_1", new Vector3(27.55f, 0.0f, 48.0f), -90f),
                ("Banca_Callejon2_2", new Vector3(31.40f, 0.0f, 28.0f), 90f),
                ("Banca_Hospital_1", new Vector3(47.5f, 0.18f, 21.4f), 0f),
                ("Banca_Hospital_2", new Vector3(76.0f, 0.18f, 21.4f), 0f),
                ("Banca_Hospital_3", new Vector3(80.0f, 0.18f, 8.6f), 180f)
            };
            foreach (var b in benches)
            {
                Place(PropsDir, "SM_Banca_Colonial", mobiliario, b.pos, b.yaw, b.name, true);
            }
            MarkStaticRecursive(mobiliario.gameObject);

            var vegetacion = new GameObject("Vegetacion_Urbana").transform;
            vegetacion.SetParent(root.transform, false);
            var trees = new (string name, Vector3 pos)[]
            {
                ("Arbol_Juarez_1", new Vector3(-6.35f, 0.18f, 14.0f)),
                ("Arbol_Juarez_2", new Vector3(6.35f, 0.18f, 30.5f)),
                ("Arbol_Juarez_3", new Vector3(-6.35f, 0.18f, 49.5f)),
                ("Arbol_Juarez_4", new Vector3(6.35f, 0.18f, 60.5f)),
                ("Arbol_Juarez_5", new Vector3(-6.35f, 0.18f, 66.5f)),
                ("Arbol_Hospital_1", new Vector3(41.0f, 0.18f, 21.3f)),
                ("Arbol_Hospital_2", new Vector3(49.0f, 0.18f, 8.7f)),
                ("Arbol_Hospital_3", new Vector3(63.5f, 0.18f, 8.7f)),
                ("Arbol_Hospital_4", new Vector3(68.0f, 0.18f, 21.3f)),
                ("Arbol_Hospital_5", new Vector3(82.5f, 0.18f, 21.3f)),
                ("Arbol_Hospital_6", new Vector3(86.0f, 0.18f, 8.7f))
            };
            foreach (var tr in trees)
            {
                Place(PropsDir, "SM_Arbol_Jardinera", vegetacion, tr.pos, 0f, tr.name, true);
            }
            MarkStaticRecursive(vegetacion.gameObject);

            var semaforos = new GameObject("Semaforos").transform;
            semaforos.SetParent(root.transform, false);
            Place(PropsDir, "SM_Semaforo_GDL", semaforos, new Vector3(5.35f, 0.18f, 76.2f), 0f, "Semaforo_Juarez_Hidalgo", true);
            Place(PropsDir, "SM_Semaforo_GDL", semaforos, new Vector3(-5.35f, 0.18f, 18.0f), 180f, "Semaforo_Juarez_Sur", true);
            Place(PropsDir, "SM_Semaforo_GDL", semaforos, new Vector3(32.6f, 0.18f, 20.4f), 90f, "Semaforo_Hospital", true);

            // 11C. Sondas de reflexion proyectadas en caja (OnAwake 128px para charcos, ventanas y Clear Coat sin costo continuo)
            var probes = new GameObject("Reflection_Probes").transform;
            probes.SetParent(root.transform, false);
            AddReflectionProbe(probes, "Probe_Juarez_Sur", new Vector3(0f, 3.2f, 32f), new Vector3(22f, 16f, 44f));
            AddReflectionProbe(probes, "Probe_Juarez_Norte", new Vector3(0f, 3.2f, 68f), new Vector3(24f, 16f, 42f));
            AddReflectionProbe(probes, "Probe_Callejon_1", new Vector3(22f, 2.8f, 72.5f), new Vector3(32f, 12f, 14f));
            AddReflectionProbe(probes, "Probe_Callejon_2", new Vector3(29.5f, 2.8f, 46f), new Vector3(14f, 12f, 48f));
            AddReflectionProbe(probes, "Probe_Hospital", new Vector3(62f, 3.2f, 15f), new Vector3(74f, 16f, 24f));

            // 12. Remapear todos los materiales al set PBR con normales, emision y GPU Instancing
            RemapAllMaterials(root, materialMap);

            foreach (var entry in carPaintOverrides)
            {
                OverrideCarPaint(entry.carObj, materialMap, entry.paintMatName);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=green>[GUADALAJARA BUILDER]</color> Ruta larga ensamblada con PBR, Static Batching, torretas y rotulacion tapatia.");
        }

        private static void PlaceCar(
            Transform parent,
            string instanceName,
            Vector3 worldPos,
            float yaw,
            string paintMatName,
            List<(GameObject carObj, string paintMatName)> overrides)
        {
            var obj = Place(PropsDir, "SM_Auto_Urbano", parent, worldPos, yaw, instanceName, true);
            if (obj != null && !string.IsNullOrEmpty(paintMatName))
            {
                overrides.Add((obj, paintMatName));
            }
        }

        private static void OverrideCarPaint(GameObject carObj, Dictionary<string, Material> map, string paintMatName)
        {
            if (carObj == null || map == null || !map.TryGetValue(paintMatName, out var paintMat)) return;

            foreach (var mr in carObj.GetComponentsInChildren<MeshRenderer>(true))
            {
                var shared = mr.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] != null && shared[i].name.Contains("M_Auto_Carroceria"))
                    {
                        shared[i] = paintMat;
                        changed = true;
                    }
                }
                if (changed)
                {
                    mr.sharedMaterials = shared;
                }
            }
        }

        private static GameObject Place(string folder, string assetName, Transform parent, Vector3 worldPos, float yaw, string name, bool addCollider)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{assetName}.fbx");
            if (prefab == null)
            {
                Debug.LogWarning($"[GUADALAJARA BUILDER] Falta el modelo {folder}/{assetName}.fbx");
                return null;
            }

            var holder = new GameObject(name);
            holder.transform.SetParent(parent, true);
            holder.transform.SetPositionAndRotation(worldPos, Quaternion.Euler(0f, yaw, 0f));

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.SetParent(holder.transform, false);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;

            ValidateBounds(inst.transform, assetName);

            if (addCollider)
            {
                foreach (var mf in inst.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh != null && mf.GetComponent<Collider>() == null)
                    {
                        mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                    }
                }
            }
            return holder;
        }

        private static Dictionary<string, Material> LoadMaterialMap()
        {
            var map = new Dictionary<string, Material>();
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsDir });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    map[mat.name] = mat;
                }
            }
            return map;
        }

        private static void RemapAllMaterials(GameObject root, Dictionary<string, Material> map)
        {
            if (root == null || map == null || map.Count == 0) return;

            var sortedKeys = new List<string>(map.Keys);
            sortedKeys.Sort((a, b) => b.Length.CompareTo(a.Length));

            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var shared = mr.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < shared.Length; i++)
                {
                    var cur = shared[i];
                    if (cur == null) continue;

                    string cleanName = cur.name.Replace(" (Instance)", "");
                    Material target = null;

                    if (!map.TryGetValue(cleanName, out target))
                    {
                        int dotIdx = cleanName.IndexOf('.');
                        if (dotIdx > 0)
                        {
                            map.TryGetValue(cleanName.Substring(0, dotIdx), out target);
                        }
                    }

                    if (target == null)
                    {
                        for (int k = 0; k < sortedKeys.Count; k++)
                        {
                            string key = sortedKeys[k];
                            if (cleanName.Contains(key))
                            {
                                target = map[key];
                                break;
                            }
                        }
                    }

                    if (target != null && cur != target)
                    {
                        shared[i] = target;
                        changed = true;
                    }
                }

                if (changed)
                {
                    mr.sharedMaterials = shared;
                }
            }
        }

        private static void MarkStaticRecursive(GameObject go)
        {
            if (go == null) return;
            StaticEditorFlags flags = StaticEditorFlags.BatchingStatic |
                                      StaticEditorFlags.OccludeeStatic |
                                      StaticEditorFlags.OccluderStatic;
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
            }
        }

        private static void AddPointLight(Transform parent, string name, Vector3 pos, Color color, float range, float intensity, Material haloMat = null, Mesh haloConeMesh = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;

            if (haloMat != null && haloConeMesh != null)
            {
                var haloGo = new GameObject("Volumetric_Halo");
                haloGo.transform.SetParent(go.transform, false);
                haloGo.transform.localPosition = new Vector3(0f, -1.55f, 0f);
                haloGo.AddComponent<MeshFilter>().sharedMesh = haloConeMesh;
                var mr = haloGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = haloMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
        }

        private static void AddReflectionProbe(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.resolution = 128;
            probe.boxProjection = true;
            probe.size = size;
            probe.intensity = 0.95f;
            probe.blendDistance = 3f;
            probe.importance = 1;
        }

        private static Mesh GetOrCreateHaloConeMesh()
        {
            const string meshPath = "Assets/_Project/Materials/Halo_Cone.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing != null) return existing;

            const int segments = 16;
            const float topRadius = 0.16f;
            const float bottomRadius = 2.65f;
            const float halfH = 1.65f;

            var mesh = new Mesh { name = "Halo_Cone_Mesh" };
            var verts = new Vector3[(segments + 1) * 2];
            var uvs = new Vector2[(segments + 1) * 2];
            var tris = new int[segments * 6];

            for (int i = 0; i <= segments; i++)
            {
                float u = i / (float)segments;
                float ang = u * Mathf.PI * 2f;
                float c = Mathf.Cos(ang);
                float s = Mathf.Sin(ang);

                int topIdx = i * 2;
                int botIdx = topIdx + 1;
                verts[topIdx] = new Vector3(c * topRadius, +halfH, s * topRadius);
                verts[botIdx] = new Vector3(c * bottomRadius, -halfH, s * bottomRadius);
                uvs[topIdx] = new Vector2(u, 1f);
                uvs[botIdx] = new Vector2(u, 0f);
            }

            int ti = 0;
            for (int i = 0; i < segments; i++)
            {
                int t0 = i * 2;
                int b0 = t0 + 1;
                int t1 = t0 + 2;
                int b1 = t0 + 3;
                tris[ti++] = t0; tris[ti++] = b1; tris[ti++] = b0;
                tris[ti++] = t0; tris[ti++] = t1; tris[ti++] = b1;
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, meshPath);
            AssetDatabase.SaveAssets();
            return mesh;
        }

        private static void ValidateBounds(Transform t, string assetName)
        {
            if (expected == null || !expected.TryGetValue(assetName, out var target)) return;

            var toLocal = t.worldToLocalMatrix;
            bool has = false;
            var actual = new Bounds();
            foreach (var mf in t.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                var m = toLocal * mf.transform.localToWorldMatrix;
                foreach (var c in Corners(mb))
                {
                    var p = m.MultiplyPoint3x4(c);
                    if (!has) { actual = new Bounds(p, Vector3.zero); has = true; }
                    else actual.Encapsulate(p);
                }
            }
            if (!has) return;

            float maxDiff = Mathf.Max(
                MaxAbsComponent(actual.min - target.min),
                MaxAbsComponent(actual.max - target.max));
            if (maxDiff > 0.15f)
            {
                Debug.LogWarning($"[GUADALAJARA BUILDER] {assetName}: limites importados ({actual.min:F2}..{actual.max:F2}) difieren de layout_bounds.json ({target.min:F2}..{target.max:F2}) en {maxDiff:F2}m.");
            }
        }

        private static IEnumerable<Vector3> Corners(Bounds b)
        {
            var mn = b.min; var mx = b.max;
            yield return new Vector3(mn.x, mn.y, mn.z);
            yield return new Vector3(mx.x, mn.y, mn.z);
            yield return new Vector3(mn.x, mx.y, mn.z);
            yield return new Vector3(mx.x, mx.y, mn.z);
            yield return new Vector3(mn.x, mn.y, mx.z);
            yield return new Vector3(mx.x, mn.y, mx.z);
            yield return new Vector3(mn.x, mx.y, mx.z);
            yield return new Vector3(mx.x, mx.y, mx.z);
        }

        private static float MaxAbsComponent(Vector3 v) => Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        private static void LoadExpectedBounds()
        {
            expected = new Dictionary<string, Bounds>();
            string path = Path.Combine(Application.dataPath, BoundsFile);
            if (!File.Exists(path))
            {
                Debug.LogWarning("[GUADALAJARA BUILDER] No se encontro layout_bounds.json para validar limites.");
                return;
            }

            var list = JsonUtility.FromJson<BoundsList>(File.ReadAllText(path));
            if (list?.items == null) return;
            foreach (var it in list.items)
            {
                var b = new Bounds();
                b.SetMinMax(it.min, it.max);
                expected[it.name] = b;
            }
        }

        private static void CreateMarkerTrigger(Transform parent, string name, Vector3 pos, Vector3 size, AlternativeRouteMarker showMarker, AlternativeRouteMarker hideMarker, bool entersDetourPhase)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;
            var comp = go.AddComponent<RouteMarkerTrigger>();
            var so = new SerializedObject(comp);
            so.FindProperty("marker").objectReferenceValue = showMarker;
            so.FindProperty("markerToHide").objectReferenceValue = hideMarker;
            so.FindProperty("entersDetourPhase").boolValue = entersDetourPhase;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddBlocker(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.AddComponent<BoxCollider>().size = size;
        }

        private static void SetBox(string objName, Vector3 pos, Vector3 size, bool isTrigger)
        {
            var go = GameObject.Find(objName);
            if (go == null) return;
            go.transform.position = pos;
            var col = go.GetComponent<BoxCollider>();
            if (col != null)
            {
                col.center = Vector3.zero;
                col.size = size;
                col.isTrigger = isTrigger;
            }
        }

        private static void SetMarkerText(AlternativeRouteMarker marker, string title, string subtitle)
        {
            if (marker == null) return;
            var so = new SerializedObject(marker);
            var t = so.FindProperty("routeTitle");
            var s = so.FindProperty("routeSubtitle");
            if (t != null) t.stringValue = title;
            if (s != null) s.stringValue = subtitle;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
