using UnityEngine;
using RutaSegura.Runtime.Player;

namespace RutaSegura.Runtime.Events
{
    /// <summary>
    /// Sistema ligero de particulas URP que emite motas de polvo y ceniza suave
    /// con textura radial difuminada (sin cuadros duros) alrededor del jugador.
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
        private Texture2D dustTex;

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
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.gravityModifier = 0.02f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.86f, 0.82f, 0.72f, 0.34f),
                new Color(0.68f, 0.65f, 0.60f, 0.20f));

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
            vel.x = new ParticleSystem.MinMaxCurve(0.7f, 1.5f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.12f, 0.22f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.35f);

            var colLife = ps.colorOverLifetime;
            colLife.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.18f),
                    new GradientAlphaKey(0.85f, 0.70f),
                    new GradientAlphaKey(0f, 1f)
                });
            colLife.color = new ParticleSystem.MinMaxGradient(grad);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.45f;
            noise.frequency = 0.35f;

            dustTex = CreateSoftDustTexture(64);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                particleMat = new Material(shader);
                if (particleMat.HasProperty("_BaseColor"))
                    particleMat.SetColor("_BaseColor", Color.white);
                else
                    particleMat.color = Color.white;

                if (particleMat.HasProperty("_BaseMap"))
                    particleMat.SetTexture("_BaseMap", dustTex);
                else
                    particleMat.mainTexture = dustTex;

                // Configurar mezcla transparente suave en URP Particles/Unlit
                if (particleMat.HasProperty("_Surface")) particleMat.SetFloat("_Surface", 1f);
                if (particleMat.HasProperty("_Blend")) particleMat.SetFloat("_Blend", 0f);
                if (particleMat.HasProperty("_ZWrite")) particleMat.SetFloat("_ZWrite", 0f);
                if (particleMat.HasProperty("_SrcBlend")) particleMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (particleMat.HasProperty("_DstBlend")) particleMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                particleMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                particleMat.SetOverrideTag("RenderType", "Transparent");
                particleMat.renderQueue = 3000;

                renderer.sharedMaterial = particleMat;
            }
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
        }

        private static Texture2D CreateSoftDustTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            float inv = 2f / (size - 1);
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float dy = y * inv - 1f;
                for (int x = 0; x < size; x++)
                {
                    float dx = x * inv - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - r);
                    a = a * a * (3f - 2f * a);
                    a *= a;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
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
            if (particleMat != null) Destroy(particleMat);
            if (dustTex != null) Destroy(dustTex);
        }
    }
}
