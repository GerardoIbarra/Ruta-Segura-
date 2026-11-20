using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RutaSegura.Runtime.Core;
using RutaSegura.Runtime.Player;
using RutaSegura.Runtime.UI;

namespace RutaSegura.Runtime.Events
{
    /// <summary>
    /// Pobla toda la ruta con vida urbana activa:
    /// - 23 peatones y manifestantes animados a lo largo de Av. Juarez, callejones y Calle Hospital.
    /// - Dependiente de Floreria El Jazmin que saluda al aproximarse el jugador.
    /// - Luces intermitentes de emergencia en vehiculos detenidos y luz roja en semaforos.
    /// - Frases breves de transeuntes al caminar por cada tramo.
    /// </summary>
    [AddComponentMenu("Ruta Segura/Events/Urban Life Controller")]
    public class UrbanLifeController : MonoBehaviour
    {
        private class NpcAgent
        {
            public Transform Root;
            public Transform LeftArm;
            public Transform RightArm;
            public Transform LeftLeg;
            public Transform RightLeg;
            public Vector3[] Waypoints;
            public int TargetWp;
            public float Speed;
            public float BaseY;
            public float Phase;
            public float SidestepOffset;
            public bool LoopWrap;
            public bool PingPong;
            public int Direction;
            public bool IsStatic;
            public bool IsPhoneUser;
            public bool IsFlorist;
        }

        private struct HazardBeacon
        {
            public Light Lamp;
            public float PhaseOffset;
        }

        private readonly List<NpcAgent> npcs = new List<NpcAgent>();
        private readonly List<HazardBeacon> hazards = new List<HazardBeacon>();
        private readonly List<Material> runtimeMats = new List<Material>();

        private Material[] jacketMats;
        private Material[] pantsMats;
        private Material[] skinMats;
        private Material hairMat;
        private Material signMat;
        private Material phoneGlowMat;
        private Material apronMat;

        private PlayerController player;

        // Subtitulos breves diegeticos
        private GameObject barkGroup;
        private Text barkText;
        private float barkTimer;
        private bool bark1Shown;
        private bool bark2Shown;
        private bool bark3Shown;
        private bool bark4Shown;

        private void Awake()
        {
            BuildSharedMaterials();
        }

        private void Start()
        {
            player = FindAnyObjectByType<PlayerController>();
            SpawnCityNpcs();
            AttachVehicleHazardsAndTrafficSignals();
            BuildBarkUI();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < runtimeMats.Count; i++)
            {
                if (runtimeMats[i] != null) Destroy(runtimeMats[i]);
            }
            runtimeMats.Clear();
        }

        private void Update()
        {
            if (GameLoopManager.Instance != null && GameLoopManager.Instance.IsPaused)
            {
                return;
            }

            if (player == null) player = FindAnyObjectByType<PlayerController>();
            Vector3 playerPos = player != null ? player.transform.position : new Vector3(0f, 1f, 22f);

            UpdateNpcs(playerPos);
            UpdateHazards();
            UpdateBarks(playerPos);
        }

        private void BuildSharedMaterials()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            jacketMats = new[]
            {
                CreateMat(urpLit, new Color(0.20f, 0.28f, 0.44f), 0.25f),
                CreateMat(urpLit, new Color(0.55f, 0.18f, 0.18f), 0.25f),
                CreateMat(urpLit, new Color(0.24f, 0.36f, 0.28f), 0.25f),
                CreateMat(urpLit, new Color(0.66f, 0.56f, 0.38f), 0.25f),
                CreateMat(urpLit, new Color(0.26f, 0.26f, 0.29f), 0.25f),
                CreateMat(urpLit, new Color(0.52f, 0.34f, 0.42f), 0.25f)
            };

            pantsMats = new[]
            {
                CreateMat(urpLit, new Color(0.14f, 0.18f, 0.28f), 0.20f),
                CreateMat(urpLit, new Color(0.16f, 0.16f, 0.18f), 0.20f)
            };

            skinMats = new[]
            {
                CreateMat(urpLit, new Color(0.64f, 0.45f, 0.34f), 0.35f),
                CreateMat(urpLit, new Color(0.76f, 0.58f, 0.46f), 0.35f)
            };

            hairMat = CreateMat(urpLit, new Color(0.09f, 0.08f, 0.08f), 0.30f);
            signMat = CreateMat(urpLit, new Color(0.92f, 0.89f, 0.80f), 0.25f);
            phoneGlowMat = CreateMat(urpLit, new Color(0.30f, 0.85f, 1.0f), 0.90f, new Color(0.25f, 0.80f, 1.0f));
            apronMat = CreateMat(urpLit, new Color(0.15f, 0.52f, 0.30f), 0.30f);
        }

        private Material CreateMat(Shader shader, Color col, float smoothness, Color? emission = null)
        {
            var m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            else m.color = col;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
            }
            m.enableInstancing = true;
            runtimeMats.Add(m);
            return m;
        }

        private void SpawnCityNpcs()
        {
            var container = new GameObject("Poblacion_Urbana_Activa");
            container.transform.SetParent(transform, false);

            // 1. 6 peatones apresurados bajando por banquetas de Av. Juarez (hacia el sur)
            float[] fleeingJuarezZ = { 64f, 48f, 29f, 71f, 53f, 34f };
            float[] fleeingJuarezX = { -5.65f, -5.55f, -5.70f, 5.60f, 5.70f, 5.55f };
            float[] fleeingSpeeds = { 2.4f, 2.7f, 2.2f, 2.6f, 2.1f, 2.5f };
            for (int i = 0; i < 6; i++)
            {
                var wp = new[] { new Vector3(fleeingJuarezX[i], 0.18f, 75f), new Vector3(fleeingJuarezX[i], 0.18f, 10f) };
                SpawnWalker(container.transform, $"Peaton_Juarez_Sur_{i + 1}", i + 1,
                    new Vector3(fleeingJuarezX[i], 0.18f, fleeingJuarezZ[i]),
                    wp, 1, fleeingSpeeds[i], 0.18f, loopWrap: true, pingPong: false,
                    hasPlacard: false, hasBackpack: (i == 1 || i == 4));
            }

            // 2. 3 manifestantes rezagados caminando al norte con pancartas
            var protesterCoords = new[]
            {
                new Vector3(-4.45f, 0f, 18f),
                new Vector3(4.45f, 0f, 40f),
                new Vector3(-5.30f, 0.18f, 56f)
            };
            float[] protesterY = { 0f, 0f, 0.18f };
            float[] protesterSpeeds = { 1.9f, 2.1f, 1.85f };
            for (int i = 0; i < 3; i++)
            {
                float x = protesterCoords[i].x;
                float y = protesterY[i];
                var wp = new[] { new Vector3(x, y, 14f), new Vector3(x, y, 75f) };
                SpawnWalker(container.transform, $"Manifestante_Rezagado_{i + 1}", 20 + i,
                    protesterCoords[i], wp, 1, protesterSpeeds[i], y, loopWrap: true, pingPong: false,
                    hasPlacard: true, hasBackpack: true);
            }

            // 3. 4 transeuntes locales en el desvio de Callejones
            var alleyLoopWps = new[]
            {
                new Vector3(8.5f, 0f, 72.4f),
                new Vector3(29.5f, 0f, 72.4f),
                new Vector3(29.5f, 0f, 23.0f)
            };
            SpawnWalker(container.transform, "Peaton_Desvio_1", 31,
                new Vector3(16.0f, 0f, 72.4f), alleyLoopWps, 1, 1.95f, 0f, loopWrap: true, pingPong: false,
                hasPlacard: false, hasBackpack: false);
            SpawnWalker(container.transform, "Peaton_Desvio_2", 32,
                new Vector3(29.5f, 0f, 52.0f), alleyLoopWps, 2, 2.25f, 0f, loopWrap: true, pingPong: false,
                hasPlacard: false, hasBackpack: true);

            SpawnWalker(container.transform, "Peaton_Callejon2_A", 33,
                new Vector3(28.45f, 0f, 60.0f),
                new[] { new Vector3(28.45f, 0f, 25f), new Vector3(28.45f, 0f, 67f) },
                1, 1.55f, 0f, loopWrap: false, pingPong: true, hasPlacard: false, hasBackpack: false);
            SpawnWalker(container.transform, "Peaton_Callejon2_B", 34,
                new Vector3(30.55f, 0f, 34.0f),
                new[] { new Vector3(30.55f, 0f, 25f), new Vector3(30.55f, 0f, 67f) },
                0, 1.75f, 0f, loopWrap: false, pingPong: true, hasPlacard: false, hasBackpack: true);

            // 4. 5 peatones por banquetas de Calle Hospital
            var hospDefs = new[]
            {
                (name: "Peaton_Hospital_1", pos: new Vector3(44f, 0.18f, 20.55f), a: new Vector3(34f, 0.18f, 20.55f), b: new Vector3(88f, 0.18f, 20.55f), dir: 1, spd: 1.65f, bag: false),
                (name: "Peaton_Hospital_2", pos: new Vector3(78f, 0.18f, 20.45f), a: new Vector3(34f, 0.18f, 20.45f), b: new Vector3(88f, 0.18f, 20.45f), dir: 0, spd: 1.90f, bag: true),
                (name: "Peaton_Hospital_3", pos: new Vector3(50f, 0.18f, 9.45f), a: new Vector3(36f, 0.18f, 9.45f), b: new Vector3(89f, 0.18f, 9.45f), dir: 1, spd: 1.50f, bag: false),
                (name: "Peaton_Hospital_4", pos: new Vector3(82f, 0.18f, 9.55f), a: new Vector3(36f, 0.18f, 9.55f), b: new Vector3(89f, 0.18f, 9.55f), dir: 0, spd: 1.80f, bag: false),
                (name: "Peaton_Hospital_5", pos: new Vector3(64f, 0.18f, 9.40f), a: new Vector3(52f, 0.18f, 9.40f), b: new Vector3(88f, 0.18f, 9.40f), dir: 1, spd: 1.45f, bag: true)
            };
            for (int i = 0; i < hospDefs.Length; i++)
            {
                var h = hospDefs[i];
                SpawnWalker(container.transform, h.name, 40 + i, h.pos, new[] { h.a, h.b }, h.dir, h.spd, 0.18f,
                    loopWrap: false, pingPong: true, hasPlacard: false, hasBackpack: h.bag);
            }

            // 5. 4 observadores en esquinas + 1 dependiente de Floreria El Jazmin (total 23 NPCs)
            SpawnStatic(container.transform, "Testigo_Neveria", 51, new Vector3(-5.8f, 0.18f, 35.5f), 25f, hasPhone: false, isFlorist: false);
            SpawnStatic(container.transform, "Testigo_Parada", 52, new Vector3(5.8f, 0.18f, 37.5f), -35f, hasPhone: false, isFlorist: false);
            SpawnStatic(container.transform, "Testigo_Telefono_1", 53, new Vector3(-5.7f, 0.18f, 72.0f), 20f, hasPhone: true, isFlorist: false);
            SpawnStatic(container.transform, "Testigo_Telefono_2", 54, new Vector3(5.5f, 0.18f, 67.5f), -25f, hasPhone: true, isFlorist: false);
            SpawnStatic(container.transform, "Dependiente_Floreria", 55, new Vector3(93.4f, 0.18f, 17.8f), -90f, hasPhone: false, isFlorist: true);
        }

        private void SpawnWalker(
            Transform parent,
            string npcName,
            int seed,
            Vector3 startPos,
            Vector3[] waypoints,
            int targetWp,
            float speed,
            float baseY,
            bool loopWrap,
            bool pingPong,
            bool hasPlacard,
            bool hasBackpack)
        {
            var agent = CreateCivilian(parent, npcName, seed, startPos, 0f, hasPlacard, hasBackpack, false, false);
            agent.Waypoints = waypoints;
            agent.TargetWp = targetWp;
            agent.Speed = speed;
            agent.BaseY = baseY;
            agent.LoopWrap = loopWrap;
            agent.PingPong = pingPong;
            agent.Direction = targetWp == 0 ? -1 : 1;
            agent.IsStatic = false;
            npcs.Add(agent);
        }

        private void SpawnStatic(Transform parent, string npcName, int seed, Vector3 pos, float yaw, bool hasPhone, bool isFlorist)
        {
            var agent = CreateCivilian(parent, npcName, seed, pos, yaw, false, false, hasPhone, isFlorist);
            agent.BaseY = pos.y;
            agent.IsStatic = true;
            agent.IsPhoneUser = hasPhone;
            agent.IsFlorist = isFlorist;
            npcs.Add(agent);
        }

        private NpcAgent CreateCivilian(
            Transform parent,
            string npcName,
            int seed,
            Vector3 pos,
            float yaw,
            bool hasPlacard,
            bool hasBackpack,
            bool hasPhone,
            bool isFlorist)
        {
            var root = new GameObject(npcName);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));

            float hScale = 0.94f + (seed % 5) * 0.03f;
            root.transform.localScale = new Vector3(hScale, hScale, hScale);

            Material coat = isFlorist ? apronMat : jacketMats[seed % jacketMats.Length];
            Material pants = pantsMats[seed % pantsMats.Length];
            Material skin = skinMats[seed % skinMats.Length];

            CreatePart(root.transform, "Torso", new Vector3(0f, 1.15f, 0f), new Vector3(0.42f, 0.62f, 0.24f), coat);
            CreatePart(root.transform, "Head", new Vector3(0f, 1.60f, 0f), new Vector3(0.22f, 0.24f, 0.22f), skin);
            CreatePart(root.transform, "Hair", new Vector3(0f, 1.71f, -0.01f), new Vector3(0.23f, 0.10f, 0.24f), hairMat);

            if (hasBackpack)
            {
                CreatePart(root.transform, "Backpack", new Vector3(0f, 1.18f, -0.17f), new Vector3(0.30f, 0.40f, 0.14f), pantsMats[1]);
            }

            Transform leftLeg = CreatePivot(root.transform, "LeftLegPivot", new Vector3(-0.10f, 0.82f, 0f));
            CreatePart(leftLeg, "LegL", new Vector3(0f, -0.41f, 0f), new Vector3(0.14f, 0.82f, 0.16f), pants);

            Transform rightLeg = CreatePivot(root.transform, "RightLegPivot", new Vector3(0.10f, 0.82f, 0f));
            CreatePart(rightLeg, "LegR", new Vector3(0f, -0.41f, 0f), new Vector3(0.14f, 0.82f, 0.16f), pants);

            Transform leftArm = CreatePivot(root.transform, "LeftArmPivot", new Vector3(-0.28f, 1.40f, 0f));
            CreatePart(leftArm, "ArmL", new Vector3(0f, -0.26f, 0f), new Vector3(0.12f, 0.54f, 0.14f), coat);

            Transform rightArm = CreatePivot(root.transform, "RightArmPivot", new Vector3(0.28f, 1.40f, 0f));
            CreatePart(rightArm, "ArmR", new Vector3(0f, -0.26f, 0f), new Vector3(0.12f, 0.54f, 0.14f), coat);

            if (hasPlacard)
            {
                rightArm.localRotation = Quaternion.Euler(-135f, 0f, 0f);
                CreatePart(rightArm, "PlacardStick", new Vector3(0f, -0.42f, 0.30f), new Vector3(0.03f, 0.95f, 0.03f), pantsMats[1]);
                CreatePart(rightArm, "PlacardBoard", new Vector3(0f, -0.78f, 0.30f), new Vector3(0.62f, 0.42f, 0.03f), signMat);
            }
            else if (hasPhone)
            {
                rightArm.localRotation = Quaternion.Euler(-55f, 0f, 0f);
                CreatePart(rightArm, "PhoneGlow", new Vector3(0f, -0.42f, 0.16f), new Vector3(0.08f, 0.14f, 0.02f), phoneGlowMat);
            }

            return new NpcAgent
            {
                Root = root.transform,
                LeftArm = leftArm,
                RightArm = rightArm,
                LeftLeg = leftLeg,
                RightLeg = rightLeg,
                Phase = seed * 0.67f
            };
        }

        private static Transform CreatePivot(Transform parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        private static void CreatePart(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;

            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null && mat != null)
            {
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private void UpdateNpcs(Vector3 playerPos)
        {
            float t = Time.time;

            for (int i = 0; i < npcs.Count; i++)
            {
                var npc = npcs[i];
                if (npc.Root == null) continue;

                if (npc.IsStatic)
                {
                    if (npc.IsFlorist)
                    {
                        float distToPlayer = Vector3.Distance(npc.Root.position, playerPos);
                        if (distToPlayer < 30f && npc.RightArm != null)
                        {
                            float wave = -145f + Mathf.Sin(t * 7.5f) * 22f;
                            npc.RightArm.localRotation = Quaternion.Euler(wave, 0f, 18f);
                        }
                        else if (npc.RightArm != null)
                        {
                            npc.RightArm.localRotation = Quaternion.identity;
                        }
                    }
                    else if (npc.IsPhoneUser)
                    {
                        float nod = Mathf.Sin(t * 1.4f + npc.Phase) * 4f;
                        npc.Root.localRotation *= Quaternion.Euler(0f, nod * Time.deltaTime, 0f);
                    }
                    else
                    {
                        float sway = Mathf.Sin(t * 2.1f + npc.Phase) * 10f;
                        if (npc.LeftArm != null) npc.LeftArm.localRotation = Quaternion.Euler(sway, 0f, 0f);
                    }
                    continue;
                }

                if (npc.Waypoints == null || npc.Waypoints.Length < 2) continue;

                Vector3 target = npc.Waypoints[npc.TargetWp];
                Vector3 pos = npc.Root.position;
                Vector3 toTarget = new Vector3(target.x - pos.x, 0f, target.z - pos.z);
                float dist = toTarget.magnitude;

                if (dist < 0.35f)
                {
                    if (npc.LoopWrap)
                    {
                        if (npc.TargetWp >= npc.Waypoints.Length - 1)
                        {
                            npc.Root.position = npc.Waypoints[0];
                            npc.TargetWp = 1;
                            continue;
                        }
                        npc.TargetWp++;
                    }
                    else if (npc.PingPong)
                    {
                        npc.TargetWp = (npc.TargetWp + 1) % npc.Waypoints.Length;
                    }
                    target = npc.Waypoints[npc.TargetWp];
                    toTarget = new Vector3(target.x - npc.Root.position.x, 0f, target.z - npc.Root.position.z);
                    dist = toTarget.magnitude;
                }

                Vector3 moveDir = dist > 0.001f ? (toTarget / dist) : Vector3.forward;

                // Desvio lateral suave si el jugador se cruza cerca
                Vector3 rightDir = new Vector3(moveDir.z, 0f, -moveDir.x);
                Vector3 diffPlayer = npc.Root.position - playerPos;
                diffPlayer.y = 0f;
                float sideTarget = 0f;
                if (diffPlayer.magnitude < 2.1f)
                {
                    sideTarget = Vector3.Dot(diffPlayer, rightDir) >= 0f ? 0.55f : -0.55f;
                }
                float prevOffset = npc.SidestepOffset;
                npc.SidestepOffset = Mathf.Lerp(npc.SidestepOffset, sideTarget, Time.deltaTime * 5f);
                Vector3 lateralDelta = rightDir * (npc.SidestepOffset - prevOffset);

                Vector3 nextPos = npc.Root.position + moveDir * (npc.Speed * Time.deltaTime) + lateralDelta;
                float cycle = t * npc.Speed * 3.1f + npc.Phase;
                nextPos.y = npc.BaseY + Mathf.Abs(Mathf.Sin(cycle)) * 0.035f;
                npc.Root.position = nextPos;

                if (moveDir.sqrMagnitude > 0.001f)
                {
                    Quaternion desired = Quaternion.LookRotation(moveDir, Vector3.up);
                    npc.Root.rotation = Quaternion.Slerp(npc.Root.rotation, desired, Time.deltaTime * 8f);
                }

                float swing = Mathf.Sin(cycle) * 32f;
                if (npc.LeftLeg != null) npc.LeftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
                if (npc.RightLeg != null) npc.RightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
                if (npc.LeftArm != null) npc.LeftArm.localRotation = Quaternion.Euler(-swing * 0.8f, 0f, 0f);
                if (npc.RightArm != null && npc.RightArm.childCount <= 1)
                {
                    npc.RightArm.localRotation = Quaternion.Euler(swing * 0.8f, 0f, 0f);
                }
            }
        }

        private void AttachVehicleHazardsAndTrafficSignals()
        {
            var entorno = GameObject.Find("Entorno_Guadalajara");
            if (entorno == null) return;

            var trafico = entorno.transform.Find("Trafico_Detenido");
            if (trafico != null)
            {
                string[] hazardVehicles =
                {
                    "Auto_Juarez_Taxi_1",
                    "Auto_Juarez_Rojo_2",
                    "Camion_Urbano_Juarez",
                    "Auto_Juarez_Gris_3",
                    "Auto_Hospital_2"
                };

                for (int i = 0; i < hazardVehicles.Length; i++)
                {
                    var veh = trafico.Find(hazardVehicles[i]);
                    if (veh == null) continue;

                    Vector3 localOffset = hazardVehicles[i].Contains("Camion")
                        ? new Vector3(0f, 1.0f, 4.45f)
                        : new Vector3(0f, 0.90f, 2.25f);

                    var go = new GameObject("Luz_Intermitente_Auto");
                    go.transform.SetParent(veh, false);
                    go.transform.localPosition = localOffset;
                    var light = go.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = new Color(1.0f, 0.58f, 0.08f);
                    light.range = 5.5f;
                    light.intensity = 0f;
                    light.shadows = LightShadows.None;

                    hazards.Add(new HazardBeacon { Lamp = light, PhaseOffset = i * 0.43f });
                }
            }

            var semaforos = entorno.transform.Find("Semaforos");
            if (semaforos != null)
            {
                for (int i = 0; i < semaforos.childCount; i++)
                {
                    Transform sem = semaforos.GetChild(i);
                    var go = new GameObject("Luz_Semaforo_Roja");
                    go.transform.SetParent(sem, false);
                    go.transform.localPosition = new Vector3(-2.55f, 4.4f, 0f);
                    var light = go.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = new Color(1.0f, 0.18f, 0.12f);
                    light.range = 7.5f;
                    light.intensity = 3.2f;
                    light.shadows = LightShadows.None;
                }
            }
        }

        private void UpdateHazards()
        {
            float t = Time.time;
            for (int i = 0; i < hazards.Count; i++)
            {
                var h = hazards[i];
                if (h.Lamp == null) continue;
                float cycle = Mathf.Repeat((t + h.PhaseOffset) * 1.35f, 1f);
                bool on = cycle < 0.42f;
                h.Lamp.intensity = on ? 3.4f : 0f;
            }
        }

        private void BuildBarkUI()
        {
            var hud = TacticalHUDCanvas.GetOrCreate();
            if (hud == null || hud.Root == null) return;

            var panel = hud.CreatePanel("Citizen_BarkPanel", hud.Root, new Color(0.05f, 0.07f, 0.10f, 0.88f));
            var rt = panel.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 88f);
            rt.sizeDelta = new Vector2(680f, 44f);
            panel.raycastTarget = false;

            var accent = hud.CreatePanel("Accent", rt, new Color(0.96f, 0.74f, 0.24f, 0.90f));
            accent.rectTransform.anchorMin = new Vector2(0f, 0f);
            accent.rectTransform.anchorMax = new Vector2(0f, 1f);
            accent.rectTransform.pivot = new Vector2(0f, 0.5f);
            accent.rectTransform.anchoredPosition = Vector2.zero;
            accent.rectTransform.sizeDelta = new Vector2(4f, 0f);
            accent.raycastTarget = false;

            barkText = hud.CreateText("BarkText", rt, "", 14, new Color(0.95f, 0.95f, 0.92f), TextAnchor.MiddleCenter, FontStyle.Bold);
            TacticalHUDCanvas.Stretch(barkText.rectTransform, 10f);

            barkGroup = panel.gameObject;
            barkGroup.SetActive(false);
        }

        private void UpdateBarks(Vector3 pos)
        {
            if (GameLoopManager.Instance != null && GameLoopManager.Instance.CurrentPhase == GamePhase.Outcome)
            {
                if (barkGroup != null && barkGroup.activeSelf) barkGroup.SetActive(false);
                return;
            }

            if (barkTimer > 0f)
            {
                barkTimer -= Time.deltaTime;
                if (barkTimer <= 0f && barkGroup != null)
                {
                    barkGroup.SetActive(false);
                }
            }

            if (!bark1Shown && pos.z >= 28f && pos.x < 10f)
            {
                bark1Shown = true;
                ShowBark("Vecino en puesto: 'Otra vez cerraron Av. Hidalgo... los camiones ya no pasan.'");
            }
            else if (!bark2Shown && pos.z >= 46f && pos.x < 10f)
            {
                bark2Shown = true;
                ShowBark("Chofer de ruta: 'Hasta aqui llegamos, rodeen a la derecha por los callejones!'");
            }
            else if (!bark3Shown && pos.x >= 12f && pos.z >= 67f)
            {
                bark3Shown = true;
                ShowBark("Transeunte: 'En Galeana estan los granaderos, baja por el segundo callejon.'");
            }
            else if (!bark4Shown && pos.x >= 38f && pos.z <= 23f)
            {
                bark4Shown = true;
                ShowBark("Comerciante: 'Por Calle Hospital ya esta despejado, la floreria queda al fondo.'");
            }
        }

        private void ShowBark(string message)
        {
            if (barkText == null || barkGroup == null) return;
            barkText.text = message;
            barkGroup.SetActive(true);
            barkTimer = 4.2f;
        }
    }
}
