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
        public static void SetupSunsetAtmosphere()
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

                // Ángulo de sol poniente en el oeste
                directionalLight.transform.rotation = Quaternion.Euler(26f, -38f, 0f);

                // Luz dorada de Guadalajara
                directionalLight.color = new Color(1.0f, 0.90f, 0.74f); // #FFE6BD
                directionalLight.intensity = 1.15f;
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

            EditorUtility.DisplayDialog("Ruta Segura", 
                "¡Atmósfera y luz de atardecer en Guadalajara (17:42) aplicadas con éxito!", "Aceptar");
        }
    }
}
