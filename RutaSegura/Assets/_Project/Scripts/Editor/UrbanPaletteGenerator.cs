using System.IO;
using UnityEditor;
using UnityEngine;

namespace RutaSegura.Editor
{
    /// <summary>
    /// Generador de materiales PBR y mapas tileables procedurales (Albedo + Normal)
    /// inspirados en el Centro Historico de Guadalajara. Activa GPU Instancing en todos los materiales.
    /// </summary>
    public static class UrbanPaletteGenerator
    {
        private const string MaterialsFolder = "Assets/_Project/Materials";
        private const string PbrTexturesFolder = "Assets/_Project/Textures/PBR";
        private const int TexSize = 512;

        private struct TextureSet
        {
            public Texture2D Albedo;
            public Texture2D Normal;
        }

        [MenuItem("Tools/Ruta Segura/Generar Paleta de Materiales Urbanos")]
        public static void GenerateUrbanMaterialsMenu()
        {
            GenerateUrbanMaterials(forceRegenerate: true);
        }

        public static void GenerateUrbanMaterials(bool forceRegenerate = false)
        {
            EnsureFolder("Assets", "_Project");
            EnsureFolder("Assets/_Project", "Materials");
            EnsureFolder("Assets/_Project", "Textures");
            EnsureFolder("Assets/_Project/Textures", "PBR");

            bool anyGenerated = EnsurePbrTextures(forceRegenerate);
            if (anyGenerated)
            {
                AssetDatabase.Refresh();
            }

            ConfigurePbrImporters();

            var texAsfalto = LoadTextureSet("Asfalto");
            var texCallejon = LoadTextureSet("ConcretoCallejon");
            var texBanqueta = LoadTextureSet("Banqueta");
            var texCantera = LoadTextureSet("Cantera");
            var texLadrillo = LoadTextureSet("Ladrillo");
            var texEstuco = LoadTextureSet("Estuco");
            var texCortina = LoadTextureSet("CortinaMetalica");

            var urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // Superficies viales y piso (asfalto ligeramente humedo para reflejar farolas y torretas)
            SetMat("M_Asfalto_Calle", Color.white, 0.42f, 0.05f, texAsfalto, 1.45f, urpShader);
            SetMat("M_Concreto_Callejon", Color.white, 0.16f, 0f, texCallejon, 1.35f, urpShader);
            SetMat("M_Concreto_Banqueta", Color.white, 0.24f, 0f, texBanqueta, 1.05f, urpShader);
            SetMat("M_Machuelo_Guarnicion", new Color(0.92f, 0.91f, 0.85f), 0.22f, 0f, texBanqueta, 0.85f, urpShader);
            SetMat("M_Paso_Cebra_Blanco", new Color(0.86f, 0.86f, 0.82f), 0.24f, 0f, texAsfalto, 0.70f, urpShader);
            SetMat("M_Linea_Amarilla", new Color(0.86f, 0.66f, 0.14f), 0.24f, 0f, texAsfalto, 0.65f, urpShader);
            SetMat("M_Coladera_Hierro", new Color(0.14f, 0.14f, 0.15f), 0.48f, 0.78f, texBanqueta, 0.95f, urpShader);

            // Arquitectura y fachadas de Guadalajara (tonos desgastados menos pastel para evitar aspecto plastilina)
            SetMat("M_Muro_Cantera", Color.white, 0.14f, 0f, texCantera, 1.35f, urpShader);
            SetMat("M_Muro_Ladrillo", Color.white, 0.15f, 0f, texLadrillo, 1.55f, urpShader);
            SetMat("M_Muro_LadrilloOscuro", new Color(0.62f, 0.56f, 0.54f), 0.14f, 0f, texLadrillo, 1.55f, urpShader);
            SetMat("M_Muro_Terracota", new Color(0.62f, 0.30f, 0.22f), 0.14f, 0f, texEstuco, 1.05f, urpShader);
            SetMat("M_Muro_AmarilloColonial", new Color(0.75f, 0.60f, 0.28f), 0.16f, 0f, texEstuco, 1.05f, urpShader);
            SetMat("M_Muro_BlancoColonial", new Color(0.80f, 0.78f, 0.73f), 0.18f, 0f, texEstuco, 1.00f, urpShader);
            SetMat("M_Muro_Ocre", new Color(0.66f, 0.48f, 0.30f), 0.15f, 0f, texEstuco, 1.05f, urpShader);
            SetMat("M_Muro_RosaDeslavado", new Color(0.70f, 0.48f, 0.48f), 0.15f, 0f, texEstuco, 1.05f, urpShader);
            SetMat("M_Muro_VerdeAgua", new Color(0.47f, 0.59f, 0.54f), 0.15f, 0f, texEstuco, 1.05f, urpShader);
            SetMat("M_Muro_AzulAnil", new Color(0.37f, 0.46f, 0.56f), 0.16f, 0f, texEstuco, 1.05f, urpShader);
            SetMat("M_Muro_GrisCemento", new Color(0.68f, 0.67f, 0.64f), 0.18f, 0f, texCallejon, 1.15f, urpShader);

            // Herreria, cortinas y detalles
            SetMat("M_Cortina_Metalica", new Color(0.80f, 0.83f, 0.87f), 0.50f, 0.70f, texCortina, 1.50f, urpShader);
            SetMat("M_Herreria_Negra", new Color(0.08f, 0.08f, 0.10f), 0.55f, 0.80f, default, 0f, urpShader);
            SetMat("M_Madera_Puerta", new Color(0.26f, 0.15f, 0.08f), 0.38f, 0f, texEstuco, 0.65f, urpShader);
            SetMat("M_Vidrio_Ventana", new Color(0.08f, 0.12f, 0.16f), 0.90f, 0.65f, default, 0f, urpShader);
            SetMat("M_Cristal_Aparador", new Color(0.16f, 0.28f, 0.34f), 0.92f, 0.85f, default, 0f, urpShader, new Color(0.08f, 0.12f, 0.10f));
            SetMat("M_Tinaco_Negro", new Color(0.10f, 0.10f, 0.11f), 0.35f, 0f, default, 0f, urpShader);
            SetMat("M_Minisplit", new Color(0.76f, 0.76f, 0.74f), 0.45f, 0.1f, default, 0f, urpShader);
            SetMat("M_Toldo_Rojo", new Color(0.62f, 0.16f, 0.14f), 0.18f, 0f, default, 0f, urpShader);
            SetMat("M_Toldo_Verde", new Color(0.14f, 0.36f, 0.22f), 0.18f, 0f, default, 0f, urpShader);
            SetMat("M_Toldo_Azul", new Color(0.14f, 0.22f, 0.42f), 0.18f, 0f, default, 0f, urpShader);
            SetMat("M_Toldo_Floreria", new Color(0.76f, 0.20f, 0.18f), 0.22f, 0f, default, 0f, urpShader);
            SetMat("M_Floreria_Destino", new Color(0.16f, 0.50f, 0.28f), 0.32f, 0f, default, 0f, urpShader);
            SetMat("M_Flores_Rojas", new Color(0.82f, 0.14f, 0.18f), 0.25f, 0f, texEstuco, 0.4f, urpShader);
            SetMat("M_Flores_Moradas", new Color(0.55f, 0.22f, 0.72f), 0.25f, 0f, texEstuco, 0.4f, urpShader);
            SetMat("M_Pinta_Grafiti_1", new Color(0.20f, 0.22f, 0.30f), 0.15f, 0f, texEstuco, 0.6f, urpShader);
            SetMat("M_Pinta_Grafiti_2", new Color(0.56f, 0.56f, 0.58f), 0.15f, 0f, texEstuco, 0.6f, urpShader);
            SetMat("M_Pinta_Grafiti_3", new Color(0.32f, 0.12f, 0.14f), 0.15f, 0f, texEstuco, 0.6f, urpShader);

            // Props urbanos, vehiculos (con emision en focos y torretas) y personajes
            SetMat("M_Valla_Bloqueo", new Color(0.95f, 0.45f, 0.05f), 0.40f, 0.2f, default, 0f, urpShader);
            SetMat("M_Metal_Valla", new Color(0.72f, 0.72f, 0.75f), 0.60f, 0.68f, default, 0f, urpShader);
            SetMat("M_Foco_Luz", new Color(1.0f, 0.92f, 0.70f), 0.80f, 0f, default, 0f, urpShader, new Color(1.5f, 1.25f, 0.75f));
            SetMat("M_Contenedor_Verde", new Color(0.14f, 0.26f, 0.18f), 0.35f, 0.3f, texEstuco, 0.5f, urpShader);
            SetMat("M_Bolsa_Basura", new Color(0.05f, 0.05f, 0.06f), 0.58f, 0f, default, 0f, urpShader);
            SetMat("M_Cinta_Precaucion", new Color(0.95f, 0.80f, 0.08f), 0.45f, 0f, default, 0f, urpShader);
            SetMat("M_Poste_Gris", new Color(0.40f, 0.40f, 0.42f), 0.45f, 0.6f, default, 0f, urpShader);
            SetMat("M_Cartulina_Pancarta", new Color(0.86f, 0.84f, 0.78f), 0.20f, 0f, default, 0f, urpShader);
            SetMat("M_Uniforme_Policia", new Color(0.08f, 0.10f, 0.16f), 0.28f, 0f, default, 0f, urpShader);
            SetMat("M_Escudo_Policia", new Color(0.30f, 0.34f, 0.38f), 0.78f, 0.35f, default, 0f, urpShader);
            SetMat("M_Casco_Policia", new Color(0.06f, 0.06f, 0.08f), 0.65f, 0.4f, default, 0f, urpShader);
            SetMat("M_Patrulla_Blanco", new Color(0.82f, 0.82f, 0.84f), 0.72f, 0.4f, default, 0f, urpShader);
            SetMat("M_Patrulla_Negro", new Color(0.06f, 0.07f, 0.10f), 0.72f, 0.4f, default, 0f, urpShader);
            SetMat("M_Torreta_Roja", new Color(0.90f, 0.10f, 0.10f), 0.85f, 0f, default, 0f, urpShader, new Color(2.0f, 0.15f, 0.15f));
            SetMat("M_Torreta_Azul", new Color(0.10f, 0.25f, 0.95f), 0.85f, 0f, default, 0f, urpShader, new Color(0.15f, 0.45f, 2.2f));
            SetMat("M_Llanta", new Color(0.05f, 0.05f, 0.05f), 0.15f, 0f, default, 0f, urpShader);
            SetMat("M_Ropa_Gris", new Color(0.32f, 0.32f, 0.34f), 0.20f, 0f, default, 0f, urpShader);
            SetMat("M_Ropa_Azul", new Color(0.18f, 0.24f, 0.38f), 0.20f, 0f, default, 0f, urpShader);
            SetMat("M_Ropa_Vino", new Color(0.40f, 0.14f, 0.16f), 0.20f, 0f, default, 0f, urpShader);
            SetMat("M_Ropa_Beige", new Color(0.62f, 0.56f, 0.44f), 0.20f, 0f, default, 0f, urpShader);
            SetMat("M_Ropa_Negra", new Color(0.10f, 0.10f, 0.11f), 0.20f, 0f, default, 0f, urpShader);
            SetMat("M_Piel_Morena", new Color(0.52f, 0.36f, 0.26f), 0.30f, 0f, default, 0f, urpShader);
            SetMat("M_Piel_Clara", new Color(0.72f, 0.56f, 0.44f), 0.30f, 0f, default, 0f, urpShader);
            SetMat("M_Pantalon_Mezclilla", new Color(0.16f, 0.20f, 0.30f), 0.20f, 0f, default, 0f, urpShader);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static bool EnsurePbrTextures(bool force)
        {
            string[] keys = { "Asfalto", "ConcretoCallejon", "Banqueta", "Cantera", "Ladrillo", "Estuco", "CortinaMetalica" };
            bool generated = false;

            foreach (var key in keys)
            {
                string albedoPath = $"{PbrTexturesFolder}/T_{key}_Albedo.png";
                string normalPath = $"{PbrTexturesFolder}/T_{key}_Normal.png";
                if (!force && File.Exists(albedoPath) && File.Exists(normalPath))
                {
                    continue;
                }

                BuildSurfaceMaps(key, albedoPath, normalPath);
                generated = true;
            }
            return generated;
        }

        private static void BuildSurfaceMaps(string kind, string albedoPath, string normalPath)
        {
            int n = TexSize;
            float[] h = new float[n * n];
            Color[] col = new Color[n * n];

            for (int y = 0; y < n; y++)
            {
                float v = y / (float)n;
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n;
                    int idx = y * n + x;
                    float f1 = PeriodicFbm(u, v, 8, 13);
                    float f2 = PeriodicFbm(u, v, 24, 37);
                    float fine = PeriodicFbm(u, v, 64, 71);

                    switch (kind)
                    {
                        case "Asfalto":
                        {
                            float crack = Mathf.Abs(f1 - 0.5f) < 0.025f ? -0.35f : 0f;
                            float height = fine * 0.65f + f2 * 0.25f + crack;
                            float shade = 0.14f + fine * 0.07f + (f1 - 0.5f) * 0.04f + crack * 0.10f;
                            h[idx] = height;
                            col[idx] = new Color(shade, shade, shade * 1.06f, 1f);
                            break;
                        }
                        case "ConcretoCallejon":
                        {
                            float stain = (f1 - 0.5f) * 0.10f;
                            float height = f2 * 0.45f + fine * 0.45f;
                            float shade = Mathf.Clamp01(0.32f + stain + (fine - 0.5f) * 0.08f);
                            h[idx] = height;
                            col[idx] = new Color(shade, shade * 0.97f, shade * 0.93f, 1f);
                            break;
                        }
                        case "Banqueta":
                        {
                            int gx = (x % 256);
                            int gy = (y % 256);
                            bool joint = gx < 4 || gy < 4;
                            float groove = joint ? 0.05f : 0.70f;
                            float height = groove + fine * 0.20f + f2 * 0.10f;
                            float shade = joint ? 0.38f : 0.58f + (f1 - 0.5f) * 0.06f + (fine - 0.5f) * 0.05f;
                            h[idx] = height;
                            col[idx] = new Color(shade, shade, shade * 1.02f, 1f);
                            break;
                        }
                        case "Cantera":
                        {
                            int row = (y / 128);
                            int ry = y % 128;
                            int rx = (x + row * 128) % 256;
                            bool mortar = ry < 5 || rx < 5;
                            float height = mortar ? 0.12f : 0.65f + fine * 0.22f + f2 * 0.13f;
                            float tint = mortar ? 0.64f : 0.80f + (f1 - 0.5f) * 0.08f + (fine - 0.5f) * 0.06f;
                            h[idx] = height;
                            col[idx] = new Color(tint, tint * 0.89f, tint * 0.72f, 1f);
                            break;
                        }
                        case "Ladrillo":
                        {
                            int brickH = 64;
                            int brickW = 128;
                            int row = y / brickH;
                            int ry = y % brickH;
                            int rx = (x + (row % 2) * (brickW / 2)) % brickW;
                            bool mortar = ry < 6 || rx < 6;
                            float brickVar = Hash2(x / brickW, row, 91) * 0.12f;
                            float height = mortar ? 0.08f : 0.72f + fine * 0.20f;
                            if (mortar)
                            {
                                col[idx] = new Color(0.56f, 0.53f, 0.49f, 1f);
                            }
                            else
                            {
                                float r = 0.46f + brickVar + (fine - 0.5f) * 0.06f;
                                col[idx] = new Color(r, r * 0.50f, r * 0.36f, 1f);
                            }
                            h[idx] = height;
                            break;
                        }
                        case "CortinaMetalica":
                        {
                            float wave = Mathf.Sin(v * Mathf.PI * 32f);
                            float slat = Mathf.Abs(Mathf.Sin(v * Mathf.PI * 16f));
                            float height = 0.5f + wave * 0.38f + fine * 0.08f;
                            float shade = 0.24f + slat * 0.10f + (f1 - 0.5f) * 0.04f;
                            h[idx] = height;
                            col[idx] = new Color(shade, shade * 1.03f, shade * 1.08f, 1f);
                            break;
                        }
                        default: // Estuco
                        {
                            float height = f1 * 0.35f + f2 * 0.35f + fine * 0.30f;
                            float shade = 0.90f + (f1 - 0.5f) * 0.10f + (fine - 0.5f) * 0.06f;
                            h[idx] = height;
                            col[idx] = new Color(shade, shade, shade, 1f);
                            break;
                        }
                    }
                }
            }

            WritePng(albedoPath, n, n, col);

            // Mapa de normales en espacio tangente por diferencias finitas periodicas
            Color[] norms = new Color[n * n];
            float strength = kind == "CortinaMetalica" ? 4.5f : (kind == "Ladrillo" || kind == "Cantera" ? 3.5f : 2.2f);
            for (int y = 0; y < n; y++)
            {
                int ym = (y - 1 + n) % n;
                int yp = (y + 1) % n;
                for (int x = 0; x < n; x++)
                {
                    int xm = (x - 1 + n) % n;
                    int xp = (x + 1) % n;
                    float dx = (h[y * n + xp] - h[y * n + xm]) * strength;
                    float dy = (h[yp * n + x] - h[ym * n + x]) * strength;
                    Vector3 norm = new Vector3(-dx, -dy, 1f).normalized;
                    norms[y * n + x] = new Color(norm.x * 0.5f + 0.5f, norm.y * 0.5f + 0.5f, norm.z * 0.5f + 0.5f, 1f);
                }
            }
            WritePng(normalPath, n, n, norms);
        }

        private static void WritePng(string path, int w, int h, Color[] pixels)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void ConfigurePbrImporters()
        {
            string[] keys = { "Asfalto", "ConcretoCallejon", "Banqueta", "Cantera", "Ladrillo", "Estuco", "CortinaMetalica" };
            foreach (var key in keys)
            {
                ConfigureImporter($"{PbrTexturesFolder}/T_{key}_Albedo.png", isNormal: false);
                ConfigureImporter($"{PbrTexturesFolder}/T_{key}_Normal.png", isNormal: true);
            }
        }

        private static void ConfigureImporter(string assetPath, bool isNormal)
        {
            var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (ti == null) return;

            TextureImporterType targetType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            bool dirty = ti.textureType != targetType ||
                         ti.wrapMode != TextureWrapMode.Repeat ||
                         ti.anisoLevel != 4 ||
                         (!isNormal && !ti.sRGBTexture);

            if (!dirty) return;

            ti.textureType = targetType;
            ti.sRGBTexture = !isNormal;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Trilinear;
            ti.anisoLevel = 4;
            ti.mipmapEnabled = true;
            ti.SaveAndReimport();
        }

        private static TextureSet LoadTextureSet(string key)
        {
            return new TextureSet
            {
                Albedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{PbrTexturesFolder}/T_{key}_Albedo.png"),
                Normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{PbrTexturesFolder}/T_{key}_Normal.png")
            };
        }

        private static void SetMat(string name, Color color, float smoothness, float metallic, TextureSet tex, float bumpScale, Shader shader, Color? emission = null)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            if (tex.Albedo != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex.Albedo);
                else mat.mainTexture = tex.Albedo;
            }

            if (tex.Normal != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", tex.Normal);
                if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", bumpScale);
                mat.EnableKeyword("_NORMALMAP");
            }
            else if (tex.Normal == null)
            {
                mat.DisableKeyword("_NORMALMAP");
            }

            if (emission.HasValue && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }

            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
        }

        private static float PeriodicFbm(float u, float v, int baseFreq, int seed)
        {
            float sum = 0f;
            float amp = 0.55f;
            float norm = 0f;
            int freq = baseFreq;
            for (int o = 0; o < 3; o++)
            {
                sum += PeriodicValueNoise(u, v, freq, seed + o * 17) * amp;
                norm += amp;
                amp *= 0.5f;
                freq *= 2;
            }
            return sum / norm;
        }

        private static float PeriodicValueNoise(float u, float v, int period, int seed)
        {
            float fx = u * period;
            float fy = v * period;
            int x0 = Mathf.FloorToInt(fx) % period;
            int y0 = Mathf.FloorToInt(fy) % period;
            if (x0 < 0) x0 += period;
            if (y0 < 0) y0 += period;
            int x1 = (x0 + 1) % period;
            int y1 = (y0 + 1) % period;

            float tx = fx - Mathf.Floor(fx);
            float ty = fy - Mathf.Floor(fy);
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);

            float c00 = Hash2(x0, y0, seed);
            float c10 = Hash2(x1, y0, seed);
            float c01 = Hash2(x0, y1, seed);
            float c11 = Hash2(x1, y1, seed);

            float a = Mathf.Lerp(c00, c10, tx);
            float b = Mathf.Lerp(c01, c11, tx);
            return Mathf.Lerp(a, b, ty);
        }

        private static float Hash2(int x, int y, int seed)
        {
            unchecked
            {
                int n = x * 374761393 + y * 668265263 + seed * 1442695041;
                n = (n ^ (n >> 13)) * 1274126177;
                return ((n ^ (n >> 16)) & 0x7fffffff) / (float)0x7fffffff;
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            string full = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(full))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
