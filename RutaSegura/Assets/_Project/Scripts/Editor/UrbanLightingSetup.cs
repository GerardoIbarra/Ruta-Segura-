using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RutaSegura.Editor
{
    /// <summary>
    /// Configura la iluminación y atmósfera de la hora dorada (17:42 Guadalajara)
    /// según la Sección 7.1 y 7.3 del GDD.
    /// </summary>
    public static class UrbanLightingSetup
    {
        [MenuItem("Tools/Ruta Segura/Configurar Iluminación de Atardecer (17:42)")]
        public static void SetupSunsetAtmosphereMenu()
        {
            SetupSunsetAtmosphere(showDialog: true);
        }

        public static void SetupSunsetAtmosphere(bool showDialog = true)
        {
            // 1. Configurar Sol / Directional Light
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
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

                // Sol bajo de tarde nublada tapatia (contraste con sombras largas y farolas)
                directionalLight.transform.rotation = Quaternion.Euler(22f, -35f, 0f);

                directionalLight.color = new Color(1.0f, 0.84f, 0.66f);
                directionalLight.intensity = 0.95f;
                directionalLight.shadows = LightShadows.Soft;

                EditorUtility.SetDirty(directionalLight);
                EditorUtility.SetDirty(directionalLight.transform);
            }

            // 2. Ajustar Post-Processing en el Global Volume
            var volumes = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
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
                Undo.RecordObject(profile, "Ajustar Volumen Atardecer");

                // Tonemapping ACES (cinematográfico)
                if (!profile.TryGet<Tonemapping>(out var tonemapping))
                {
                    tonemapping = profile.Add<Tonemapping>(true);
                }
                tonemapping.mode.overrideState = true;
                tonemapping.mode.value = TonemappingMode.ACES;

                // Ajuste de color cálido
                if (!profile.TryGet<ColorAdjustments>(out var colorAdjustments))
                {
                    colorAdjustments = profile.Add<ColorAdjustments>(true);
                }
                colorAdjustments.postExposure.overrideState = true;
                colorAdjustments.postExposure.value = 0.1f;
                colorAdjustments.contrast.overrideState = true;
                colorAdjustments.contrast.value = 15f;
                colorAdjustments.colorFilter.overrideState = true;
                colorAdjustments.colorFilter.value = new Color(1f, 0.96f, 0.92f);

                // Viñeta suave
                if (!profile.TryGet<Vignette>(out var vignette))
                {
                    vignette = profile.Add<Vignette>(true);
                }
                vignette.intensity.overrideState = true;
                vignette.intensity.value = 0.22f;
                vignette.smoothness.overrideState = true;
                vignette.smoothness.value = 0.4f;

                EditorUtility.SetDirty(profile);
            }

            // 3. Iluminacion ambiental trilight y neblina atmosferica de atardecer nublado
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.28f, 0.33f, 0.42f);
            RenderSettings.ambientEquatorColor = new Color(0.24f, 0.23f, 0.22f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.09f, 0.09f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.22f, 0.25f, 0.30f);
            RenderSettings.fogDensity = 0.011f;

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Ruta Segura",
                    "Atmosfera y luz de atardecer en Guadalajara (17:42) aplicadas con exito.", "Aceptar");
            }
        }
    }
}
