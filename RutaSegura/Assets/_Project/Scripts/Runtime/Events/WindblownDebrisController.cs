using UnityEngine;
using RutaSegura.Runtime.Player;

namespace RutaSegura.Runtime.Events
{
    /// <summary>
    /// Sistema ligero de particulas URP que genera volantes de papel y polvo urbano
    /// arrastrados por el viento alrededor del jugador para reforzar la escala e inmersion.
    /// </summary>
    [AddComponentMenu("Ruta Segura/Events/Windblown Debris Controller")]
    public class WindblownDebrisController : MonoBehaviour
    {
        [Header("Ajustes Visuales")]
        [SerializeField] private int maxParticles = 40;
        [SerializeField] private float emissionRate = 6.5f;

        private PlayerController player;
        private Transform emitterTransform;
        private ParticleSystem ps;
        private Material particleMat;

        private void Awake()
        {
            player = FindAnyObjectByType<PlayerController>();

            var child = new GameObject("Viento_Volantes_Polvo");
            child.transform.SetParent(transform, true);
            emitterTransform = child.transform;

            ps = child.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4.2f, 6.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.17f);
            main.gravityModifier = 0.03f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.86f, 0.83f, 0.74f, 0.55f),
                new Color(0.68f, 0.65f, 0.60f, 0.35f));

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = emissionRate;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(16f, 3.2f, 16f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.15f, 0.25f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.4f);

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-2.4f, 2.4f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.55f;
            noise.frequency = 0.35f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                particleMat = new Material(shader);
                if (particleMat.HasProperty("_BaseColor"))
                    particleMat.SetColor("_BaseColor", new Color(0.92f, 0.90f, 0.84f, 0.70f));
                else
                    particleMat.color = new Color(0.92f, 0.90f, 0.84f, 0.70f);
                renderer.sharedMaterial = particleMat;
            }
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
        }

        private void Update()
        {
            if (player == null)
            {
                player = FindAnyObjectByType<PlayerController>();
            }

            if (player != null && emitterTransform != null)
            {
                Vector3 p = player.transform.position;
                emitterTransform.position = new Vector3(p.x, p.y + 2.2f, p.z);
            }
        }

        private void OnDestroy()
        {
            if (particleMat != null)
            {
                Destroy(particleMat);
            }
        }
    }
}
