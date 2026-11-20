using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RutaSegura.Editor
{
    /// <summary>
    /// Configura la iluminacion, skybox crepuscular con nubes, Reflection Probes
    /// y post-procesado cinematografico ACES de atardecer en Guadalajara (17:42).
    /// </summary>
    public static class UrbanLightingSetup
    {
        [MenuItem("Tools/Ruta Segura/Configurar Iluminacion de Atardecer (17:42)")]
        public static void SetupSunsetAtmosphereMenu()
        {
            SetupSunsetAtmosphere(showDialog: true);
        }

        public static void SetupSunsetAtmosphere(bool showDialog = true)
        {
            // 1. Configurar Sol / Directional Light
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            Light directionalLight = null;
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    directionalLight = l;
                    break;
                }
            }

            if (directionalLight != null)
            {
                Undo.RecordObject(directionalLight.transform, "Ajustar Luz Atardecer");
                Undo.RecordObject(directionalLight, "Ajustar Luz Atardecer");

                directionalLight.transform.rotation = Quaternion.Euler(20f, -35f, 0f);
                directionalLight.color = new Color(1.0f, 0.82f, 0.62f);
                directionalLight.intensity = 1.15f;
                directionalLight.shadows = LightShadows.Soft;

                EditorUtility.SetDirty(directionalLight);
                EditorUtility.SetDirty(directionalLight.transform);
            }

            // 2. Post-Procesado cinematografico URP en Global Volume
            var volumes = Object.FindObjectsByType<Volume>(FindObjectsInactive.Exclude);
            Volume globalVolume = null;
            foreach (var v in volumes)
            {
                if (v.isGlobal)
                {
                    globalVolume = v;
                    break;
                }
            }

            if (globalVolume != null && globalVolume.sharedProfile != null)
            {
                var profile = globalVolume.sharedProfile;
                Undo.RecordObject(profile, "Ajustar Post-Procesado Realista");

                var tonemapping = GetOrAdd<Tonemapping>(profile);
                tonemapping.mode.overrideState = true;
                tonemapping.mode.value = TonemappingMode.ACES;

                var bloom = GetOrAdd<Bloom>(profile);
                bloom.threshold.overrideState = true;
                bloom.threshold.value = 0.92f;
                bloom.intensity.overrideState = true;
                bloom.intensity.value = 0.55f;
                bloom.scatter.overrideState = true;
                bloom.scatter.value = 0.68f;
                bloom.highQualityFiltering.overrideState = true;
                bloom.highQualityFiltering.value = true;

                var colorAdj = GetOrAdd<ColorAdjustments>(profile);
                colorAdj.postExposure.overrideState = true;
                colorAdj.postExposure.value = 0.22f;
                colorAdj.contrast.overrideState = true;
                colorAdj.contrast.value = 12f;
                colorAdj.saturation.overrideState = true;
                colorAdj.saturation.value = 6f;
                colorAdj.colorFilter.overrideState = true;
                colorAdj.colorFilter.value = new Color(1f, 0.97f, 0.93f);

                var split = GetOrAdd<SplitToning>(profile);
                split.shadows.overrideState = true;
                split.shadows.value = new Color(0.42f, 0.49f, 0.58f);
                split.highlights.overrideState = true;
                split.highlights.value = new Color(0.62f, 0.54f, 0.42f);
                split.balance.overrideState = true;
                split.balance.value = -10f;

                var lgg = GetOrAdd<LiftGammaGain>(profile);
                lgg.lift.overrideState = true;
                lgg.lift.value = new Vector4(1f, 1.01f, 1.03f, 0.03f);
                lgg.gamma.overrideState = true;
                lgg.gamma.value = new Vector4(1f, 1f, 1f, 0.02f);
                lgg.gain.overrideState = true;
                lgg.gain.value = new Vector4(1.02f, 1f, 0.97f, 0f);

                var vignette = GetOrAdd<Vignette>(profile);
                vignette.intensity.overrideState = true;
                vignette.intensity.value = 0.24f;
                vignette.smoothness.overrideState = true;
                vignette.smoothness.value = 0.45f;

                var grain = GetOrAdd<FilmGrain>(profile);
                grain.type.overrideState = true;
                grain.type.value = FilmGrainLookup.Thin1;
                grain.intensity.overrideState = true;
                grain.intensity.value = 0.12f;
                grain.response.overrideState = true;
                grain.response.value = 0.75f;

                var flare = GetOrAdd<ScreenSpaceLensFlare>(profile);
                flare.intensity.overrideState = true;
                flare.intensity.value = 0.20f;
                flare.streaksIntensity.overrideState = true;
                flare.streaksIntensity.value = 0.26f;
                flare.resolution.overrideState = true;
                flare.resolution.value = ScreenSpaceLensFlareResolution.Quarter;

                EditorUtility.SetDirty(profile);
            }

            // 3. Skybox procedural con nubes de atardecer, rebote ambiental elevado y neblina atmosferica
            var skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Skybox_Guadalajara.mat");
            if (skyMat != null)
            {
                RenderSettings.skybox = skyMat;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.36f, 0.42f, 0.54f);
            RenderSettings.ambientEquatorColor = new Color(0.34f, 0.30f, 0.28f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.16f, 0.16f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.26f, 0.27f, 0.33f);
            RenderSettings.fogDensity = 0.0085f;

            DynamicGI.UpdateEnvironment();

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Ruta Segura",
                    "Pack Visual Realista v1 (Skybox, ACES + Bloom + Split Toning + Film Grain) aplicado con exito.", "Aceptar");
            }
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet<T>(out var comp))
            {
                comp = profile.Add<T>(true);
            }
            comp.active = true;
            return comp;
        }
    }
}
