using System.Collections.Generic;
using UnityEngine;

namespace RutaSegura.Runtime.Events
{
    /// <summary>
    /// Aporta vida visual a la protesta y cordon policial:
    /// - Torretas rojas y azules parpadeantes sobre cada patrulla iluminando el asfalto.
    /// - Movimiento sutil de consigna/vaiven en los grupos de manifestantes.
    /// </summary>
    [AddComponentMenu("Ruta Segura/Events/Protest Atmosphere Controller")]
    public class ProtestAtmosphereController : MonoBehaviour
    {
        private struct BeaconPair
        {
            public Light RedLight;
            public Light BlueLight;
            public float PhaseOffset;
        }

        private struct CrowdMotion
        {
            public Transform Target;
            public Vector3 BasePos;
            public Quaternion BaseRot;
            public float Phase;
        }

        [Header("Parametros Visuales")]
        [SerializeField] private float beaconRange = 18f;
        [SerializeField] private float maxBeaconIntensity = 14f;
        [SerializeField] private float strobeFrequencyHz = 3.5f;

        private readonly List<BeaconPair> beacons = new List<BeaconPair>();
        private readonly List<CrowdMotion> crowds = new List<CrowdMotion>();

        private void Start()
        {
            GameObject entorno = GameObject.Find("Entorno_Guadalajara");
            Transform root = entorno != null ? entorno.transform : null;
            if (root == null) return;

            Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
            int idx = 0;
            foreach (Transform t in allTransforms)
            {
                if (t.name.StartsWith("Patrulla_"))
                {
                    AttachBeaconLights(t, idx * 0.37f);
                    idx++;
                }
                else if (t.name.StartsWith("Multitud_"))
                {
                    crowds.Add(new CrowdMotion
                    {
                        Target = t,
                        BasePos = t.localPosition,
                        BaseRot = t.localRotation,
                        Phase = idx * 0.71f
                    });
                    idx++;
                }
            }
        }

        private void AttachBeaconLights(Transform patrolHolder, float phaseOffset)
        {
            var red = CreateStrobeLight(patrolHolder, "Torreta_Luz_Roja", new Vector3(-0.42f, 2.35f, 0f), new Color(1.0f, 0.12f, 0.10f));
            var blue = CreateStrobeLight(patrolHolder, "Torreta_Luz_Azul", new Vector3(0.42f, 2.35f, 0f), new Color(0.12f, 0.35f, 1.0f));
            beacons.Add(new BeaconPair
            {
                RedLight = red,
                BlueLight = blue,
                PhaseOffset = phaseOffset
            });
        }

        private Light CreateStrobeLight(Transform parent, string lightName, Vector3 localOffset, Color color)
        {
            var go = new GameObject(lightName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localOffset;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = beaconRange;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            return light;
        }

        private void Update()
        {
            float t = Time.time;

            // 1. Torretas alternantes roja / azul con doble destello
            for (int i = 0; i < beacons.Count; i++)
            {
                var b = beacons[i];
                float cycle = Mathf.Repeat((t + b.PhaseOffset) * strobeFrequencyHz, 2f);
                bool redWindow = cycle < 1f;
                float pulse = redWindow ? cycle : (cycle - 1f);
                float flash = Mathf.Pow(Mathf.Abs(Mathf.Sin(pulse * Mathf.PI * 2f)), 1.6f);

                if (b.RedLight != null) b.RedLight.intensity = redWindow ? flash * maxBeaconIntensity : 0f;
                if (b.BlueLight != null) b.BlueLight.intensity = !redWindow ? flash * maxBeaconIntensity : 0f;
            }

            // 2. Movimiento organico de consignas (~0.85 Hz sincronizado con audio de multitud)
            for (int i = 0; i < crowds.Count; i++)
            {
                var c = crowds[i];
                if (c.Target == null) continue;
                float bounce = Mathf.Abs(Mathf.Sin((t + c.Phase) * Mathf.PI * 0.85f)) * 0.05f;
                float swayDeg = Mathf.Sin((t + c.Phase) * 1.6f) * 1.2f;
                c.Target.localPosition = c.BasePos + new Vector3(0f, bounce, 0f);
                c.Target.localRotation = c.BaseRot * Quaternion.Euler(0f, swayDeg * 0.5f, swayDeg);
            }
        }
    }
}
