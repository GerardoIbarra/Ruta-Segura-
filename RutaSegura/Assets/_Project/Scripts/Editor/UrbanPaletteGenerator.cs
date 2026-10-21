using UnityEngine;
using UnityEditor;
using System.IO;

namespace RutaSegura.Editor
{
    /// <summary>
    /// Herramienta de Editor para generar la paleta de materiales urbanos
    /// inspirada en la arquitectura y estética de Guadalajara (GDD Sección 7.1).
    /// </summary>
    public static class UrbanPaletteGenerator
    {
        private const string MaterialsFolder = "Assets/_Project/Materials";

        [MenuItem("Tools/Ruta Segura/Generar Paleta de Materiales Urbanos")]
        public static void GenerateUrbanMaterials()
        {
            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project"))
                {
                    AssetDatabase.CreateFolder("Assets", "_Project");
                }
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            }

            var urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null)
            {
                urpShader = Shader.Find("Standard");
            }

            // 1. Asfalto / Calzada (Gris carbón oscuro mate)
            CreateOrUpdateMaterial("M_Asfalto_Calle", new Color(0.18f, 0.18f, 0.20f), smoothness: 0.15f, urpShader);

            // 2. Banqueta / Acera (Concreto claro)
            CreateOrUpdateMaterial("M_Concreto_Banqueta", new Color(0.60f, 0.60f, 0.62f), smoothness: 0.25f, urpShader);

            // 3. Muro Cantera (Tono cálido tradicional de Guadalajara)
            CreateOrUpdateMaterial("M_Muro_Cantera", new Color(0.82f, 0.74f, 0.60f), smoothness: 0.1f, urpShader);

            // 4. Muro Terracota (Ladrillo y fachada urbana)
            CreateOrUpdateMaterial("M_Muro_Terracota", new Color(0.68f, 0.32f, 0.22f), smoothness: 0.1f, urpShader);

            // 5. Muro Amarillo Colonial (Fachada desgastada)
            CreateOrUpdateMaterial("M_Muro_AmarilloColonial", new Color(0.85f, 0.68f, 0.28f), smoothness: 0.15f, urpShader);

            // 6. Valla de Bloqueo (Color advertencia / seguridad urbana)
            CreateOrUpdateMaterial("M_Valla_Bloqueo", new Color(0.95f, 0.45f, 0.05f), smoothness: 0.4f, urpShader);

            // 7. Florería 'El Jazmín' (Tono verde fresco acogedor)
            CreateOrUpdateMaterial("M_Floreria_Destino", new Color(0.18f, 0.58f, 0.32f), smoothness: 0.3f, urpShader);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Ruta Segura", 
                "¡Paleta de materiales urbanos generada con éxito en Assets/_Project/Materials!", "Aceptar");
        }

        private static void CreateOrUpdateMaterial(string name, Color color, float smoothness, Shader shader)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            // Asignar color según el shader (URP Lit usa _BaseColor, Standard usa _Color)
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            else
            {
                mat.color = color;
            }

            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", smoothness);
            }

            EditorUtility.SetDirty(mat);
        }
    }
}
