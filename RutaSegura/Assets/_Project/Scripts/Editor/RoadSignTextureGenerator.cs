using System.IO;
using UnityEditor;
using UnityEngine;

namespace RutaSegura.Editor
{
    /// <summary>
    /// Generador de texturas y materiales para la senaletica vial de la barricada
    /// y la rotulacion urbana tapatia (Florería El Jazmin y placas de nomenclatura).
    /// </summary>
    public static class RoadSignTextureGenerator
    {
        private const string TexturesFolder = "Assets/_Project/Textures";
        private const string MaterialsFolder = "Assets/_Project/Materials";

        [MenuItem("Tools/Ruta Segura/Generar Senaletica Vial (Calle Cerrada y Desvio)")]
        public static void GenerateRoadSignsMenu()
        {
            GenerateRoadSigns(showDialog: true);
        }

        public static void GenerateRoadSigns(bool showDialog = true)
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
            {
                AssetDatabase.CreateFolder("Assets", "_Project");
            }
            if (!AssetDatabase.IsValidFolder(TexturesFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Textures");
            }
            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            }

            // 1. Senaletica de barricada
            string pathCerrada = $"{TexturesFolder}/Tex_CalleCerrada.png";
            GenerateSignTexture(pathCerrada, "CALLE", "CERRADA", drawArrow: false);

            string pathDesvio = $"{TexturesFolder}/Tex_Desvio.png";
            GenerateSignTexture(pathDesvio, "DESVIO", "", drawArrow: true);

            // 2. Rotulacion tapatia (Florería y placas coloniales de calle)
            string pathFloreria = $"{TexturesFolder}/Tex_Letrero_Floreria.png";
            GenerateBannerSignTexture(
                pathFloreria,
                width: 1024,
                height: 256,
                bgCol: new Color(0.11f, 0.36f, 0.21f),
                fgCol: new Color(0.95f, 0.92f, 0.80f),
                text: "FLORERIA EL JAZMIN",
                charWidth: 38,
                charHeight: 136,
                spacing: 8);

            string pathJuarez = $"{TexturesFolder}/Tex_Placa_Juarez.png";
            GenerateBannerSignTexture(
                pathJuarez,
                width: 512,
                height: 256,
                bgCol: new Color(0.09f, 0.20f, 0.44f),
                fgCol: new Color(0.94f, 0.94f, 0.92f),
                text: "AV. JUAREZ",
                charWidth: 34,
                charHeight: 96,
                spacing: 8);

            string pathHospital = $"{TexturesFolder}/Tex_Placa_Hospital.png";
            GenerateBannerSignTexture(
                pathHospital,
                width: 512,
                height: 256,
                bgCol: new Color(0.09f, 0.20f, 0.44f),
                fgCol: new Color(0.94f, 0.94f, 0.92f),
                text: "C. HOSPITAL",
                charWidth: 30,
                charHeight: 90,
                spacing: 7);

            string pathManta = $"{TexturesFolder}/Tex_Manta_Protesta.png";
            GenerateProtestBannerTexture(pathManta, "EXIGIMOS", "CALLES SEGURAS");

            AssetDatabase.Refresh();

            var urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            CreateSignMaterial("M_Placa_CalleCerrada", pathCerrada, urpShader, smoothness: 0.45f);
            CreateSignMaterial("M_Placa_Desvio", pathDesvio, urpShader, smoothness: 0.45f);
            CreateSignMaterial("M_Letrero_Floreria", pathFloreria, urpShader, smoothness: 0.38f, emission: new Color(0.15f, 0.42f, 0.24f));
            CreateSignMaterial("M_Placa_Calle_Juarez", pathJuarez, urpShader, smoothness: 0.55f);
            CreateSignMaterial("M_Placa_Calle_Hospital", pathHospital, urpShader, smoothness: 0.55f);
            CreateSignMaterial("M_Manta_Protesta", pathManta, urpShader, smoothness: 0.15f);
            CreateConeMaterial("M_Cono_Trafico", urpShader);

            AssetDatabase.SaveAssets();

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Ruta Segura",
                    "Senaletica vial y rotulacion tapatia generadas con exito.\n\nMateriales listos en:\n- M_Placa_CalleCerrada\n- M_Placa_Desvio\n- M_Letrero_Floreria\n- M_Placa_Calle_Juarez\n- M_Placa_Calle_Hospital\n- M_Manta_Protesta\n- M_Cono_Trafico",
                    "Aceptar");
            }
        }

        private static void GenerateSignTexture(string filePath, string line1, string line2, bool drawArrow)
        {
            int w = 512;
            int h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color yellowBg = new Color(0.96f, 0.73f, 0.08f);
            Color borderBlack = new Color(0.10f, 0.10f, 0.12f);
            Color boltColor = new Color(0.35f, 0.35f, 0.38f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float noise = (Mathf.PerlinNoise(x * 0.05f, y * 0.05f) - 0.5f) * 0.04f;
                    Color baseCol = new Color(yellowBg.r + noise, yellowBg.g + noise, yellowBg.b + noise, 1f);

                    if (x < 12 || x > w - 12 || y < 12 || y > h - 12)
                    {
                        tex.SetPixel(x, y, borderBlack);
                    }
                    else if ((x >= 20 && x <= 26) || (x >= w - 26 && x <= w - 20) ||
                             (y >= 20 && y <= 26) || (y >= h - 26 && y <= h - 20))
                    {
                        tex.SetPixel(x, y, borderBlack);
                    }
                    else
                    {
                        tex.SetPixel(x, y, baseCol);
                    }

                    if (IsNearPoint(x, y, 36, 36, 6) || IsNearPoint(x, y, w - 36, 36, 6) ||
                        IsNearPoint(x, y, 36, h - 36, 6) || IsNearPoint(x, y, w - 36, h - 36, 6))
                    {
                        tex.SetPixel(x, y, boltColor);
                    }
                }
            }

            if (!drawArrow)
            {
                DrawCenteredBlockText(tex, line1, yCenter: 160, charHeight: 52, charWidth: 32, spacing: 8, stroke: 8, borderBlack);
                DrawCenteredBlockText(tex, line2, yCenter: 90, charHeight: 52, charWidth: 28, spacing: 6, stroke: 8, borderBlack);
            }
            else
            {
                DrawCenteredBlockText(tex, line1, yCenter: 160, charHeight: 52, charWidth: 32, spacing: 8, stroke: 8, borderBlack);
                DrawThickArrow(tex, xCenter: w / 2, yCenter: 88, arrowLength: 120, borderBlack);
            }

            tex.Apply();
            File.WriteAllBytes(filePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateBannerSignTexture(
            string filePath,
            int width,
            int height,
            Color bgCol,
            Color fgCol,
            string text,
            int charWidth,
            int charHeight,
            int spacing)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color darkTrim = Color.Lerp(bgCol, Color.black, 0.35f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float noise = (Mathf.PerlinNoise(x * 0.04f, y * 0.04f) - 0.5f) * 0.025f;
                    Color baseCol = new Color(
                        Mathf.Clamp01(bgCol.r + noise),
                        Mathf.Clamp01(bgCol.g + noise),
                        Mathf.Clamp01(bgCol.b + noise),
                        1f);

                    if (x < 10 || x >= width - 10 || y < 10 || y >= height - 10)
                    {
                        tex.SetPixel(x, y, darkTrim);
                    }
                    else if ((x >= 18 && x <= 24) || (x >= width - 25 && x <= width - 19) ||
                             (y >= 18 && y <= 24) || (y >= height - 25 && y <= height - 19))
                    {
                        tex.SetPixel(x, y, fgCol);
                    }
                    else
                    {
                        tex.SetPixel(x, y, baseCol);
                    }
                }
            }

            DrawCenteredBlockText(tex, text, yCenter: height / 2, charHeight: charHeight, charWidth: charWidth, spacing: spacing, stroke: 8, fgCol);

            tex.Apply();
            File.WriteAllBytes(filePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static bool IsNearPoint(int x, int y, int px, int py, int r)
        {
            int dx = x - px;
            int dy = y - py;
            return (dx * dx + dy * dy) <= (r * r);
        }

        private static void DrawThickArrow(Texture2D tex, int xCenter, int yCenter, int arrowLength, Color col)
        {
            int halfL = arrowLength / 2;
            int startX = xCenter - halfL;
            int endX = xCenter + halfL;
            int barThickness = 14;

            for (int y = yCenter - barThickness; y <= yCenter + barThickness; y++)
            {
                for (int x = startX; x <= endX - 20; x++)
                {
                    tex.SetPixel(x, y, col);
                }
            }

            int headSize = 36;
            for (int dx = 0; dx <= headSize; dx++)
            {
                int curX = endX - headSize + dx;
                int currentHalfH = headSize - dx;
                for (int y = yCenter - currentHalfH; y <= yCenter + currentHalfH; y++)
                {
                    if (curX >= 0 && curX < tex.width && y >= 0 && y < tex.height)
                    {
                        tex.SetPixel(curX, y, col);
                    }
                }
            }
        }

        private static void DrawCenteredBlockText(Texture2D tex, string text, int yCenter, int charHeight, int charWidth, int spacing, int stroke, Color col)
        {
            int totalWidth = text.Length * charWidth + (text.Length - 1) * spacing;
            int startX = (tex.width - totalWidth) / 2;
            int halfH = charHeight / 2;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                int cx = startX + i * (charWidth + spacing);
                int cy = yCenter - halfH;
                DrawBlockChar(tex, c, cx, cy, charWidth, charHeight, stroke, col);
            }
        }

        private static void DrawBlockChar(Texture2D tex, char c, int x, int y, int w, int h, int t, Color col)
        {
            int midY = y + h / 2 - t / 2;
            int midX = x + w / 2 - t / 2;

            switch (c)
            {
                case ' ':
                    break;
                case '.':
                    FillRect(tex, midX, y, t, t, col);
                    break;
                case 'A':
                    FillRect(tex, x, y, t, h, col);
                    FillRect(tex, x + w - t, y, t, h, col);
                    FillRect(tex, x, y + h - t, w, t, col);
                    FillRect(tex, x, midY, w, t, col);
                    break;
                case 'B':
                    FillRect(tex, x, y, t, h, col);
                    FillRect(tex, x, y + h - t, w - 4, t, col);
                    FillRect(tex, x, midY, w - 4, t, col);
                    FillRect(tex, x, y, w - 4, t, col);
                    FillRect(tex, x + w - t, y + 4, t, h / 2 - 4, col);
                    FillRect(tex, x + w - t, y + h / 2, t, h / 2 - 4, col);
                    break;
                case 'C':
                    FillRect(tex, x, y, t, h, col);
                    FillRect(tex, x, y + h - t, w, t, col);
                    FillRect(tex, x, y, w, t, col);
                    break;
                case 'D':
                    FillRect(tex, x, y, t, h, col);
                    FillRect(tex, x, y + h - t, w - 4, t, col);
                    FillRect(tex, x, y, w - 4, t, col);
                    FillRect(tex, x + w - t, y + 4, t, h - 8, col);
                    break;
                case 'E':
                    FillRect(tex, x, y, t, h, col);
                    FillRect(tex, x, y + h - t, w, t, col);
                    FillRect(tex, x, midY, w - 4, t, col);
                    FillRect(tex, x, y, w, t, col);
                    break;
                case 'F':
                    FillRect(tex, x, y, t, h, col);
                    FillRect(tex, x, y + h - t, w, t, col);
                    FillRect(tex, x, midY, w - 4, t, col);
                    break;
                case 'G':
                    FillRect(tex, x, y, t, h, col);
                    FillRect(tex, x, y + h - t, w, t, col);
                    FillRect(tex, x, y, w, t, col);
                    FillRect(tex, x + w - t, y, t, h / 2, col);
                    FillRect(tex, midX, midY, w / 2 + t / 2, t, col);
                    break;
                case 'H':
                    FillRect(tex, x, y, t, h, col);
                    FillRect(tex, x + w - t, y, t, h, col);
                    FillRect(tex, x, midY, w, t, col);
                    break;
                case 'I':
                    FillRect(tex, midX, y, t, h, col);
                    FillRect(tex, x + 4, y + h - t, w - 8, t, col);
                    FillRect(tex, x + 4, y, w - 8, t, col);
                    break;
                case 'J':
                    FillRect(tex, x + w - t, y, t, h, col);
                    FillRect(tex, x, y, w, t, col);
                    FillRect(tex, x, y, t, h / 3, col);
                    break;
                case 'K':
                    FillRect(tex, x, y, t, h, col);
                    FillRect(tex, x, midY, w / 2, t, col);
                    FillRect(tex, x + w - t, y, t, h / 2 - 2, col);
                    FillRect(tex, x + w - t, y + h / 2 + 2, t, h / 2 - 2, col);
                    break;
                case 'L':
                    FillRect(tex, x, y, t, h, col);
                    FillRect(tex, x, y, w, t, col);
                    break;
                case 'M':
                    FillRect(tex, x, y, t, h, col);
                    FillRect(tex, x + w - t, y, t, h, col);
                    FillRect(tex, x, y + h - t, w, t, col);
                    FillRect(tex, midX, y + h / 3, t, 2 * h / 3, col);
                    break;
                case 'N':
                    FillRect(tex, x, y, t, h, col);
                    FillRect(tex, x + w - t, y, t, h, col);
                    FillRect(tex, x, y + h - t, w / 2 + 2, t, col);
                    FillRect(tex, x + w / 2 - 2, y + t, t, h - t, col);
                    FillRect(tex, x + w / 2 - 2, y, w / 2 + 2, t, col);
                    break;
                case 'O':
                    FillRect(tex, x, y, t, h, col);
                    FillRect(tex, x + w - t, y, t, h, col);
                    FillRect(tex, x, y + h - t, w, t, col);
                    FillRect(tex, x, y, w, t, col);
                    break;
                case 'P':
                    FillRect(tex, x, y, t, h, col);
                    FillRect(tex, x, y + h - t, w, t, col);
                    FillRect(tex, x, midY, w, t, col);
                    FillRect(tex, x + w - t, midY, t, h / 2, col);
                    break;
                case 'Q':
                    FillRect(tex, x, y + t, t, h - t, col);
                    FillRect(tex, x + w - t, y, t, h, col);
                    FillRect(tex, x, y + h - t, w, t, col);
                    FillRect(tex, x, y + t, w, t, col);
                    FillRect(tex, x + w - t, y, w / 3, t, col);
                    break;
                case 'R':
                    FillRect(tex, x, y, t, h, col);
                    FillRect(tex, x, y + h - t, w, t, col);
                    FillRect(tex, x + w - t, y + h / 2, t, h / 2, col);
                    FillRect(tex, x, y + h / 2, w, t, col);
                    FillRect(tex, x + w - t, y, t, h / 2, col);
                    break;
                case 'S':
                    FillRect(tex, x, y + h - t, w, t, col);
                    FillRect(tex, x, y + h / 2, t, h / 2, col);
                    FillRect(tex, x, midY, w, t, col);
                    FillRect(tex, x + w - t, y, t, h / 2, col);
                    FillRect(tex, x, y, w, t, col);
                    break;
                case 'T':
                    FillRect(tex, x, y + h - t, w, t, col);
                    FillRect(tex, midX, y, t, h, col);
                    break;
                case 'U':
                    FillRect(tex, x, y, t, h, col);
                    FillRect(tex, x + w - t, y, t, h, col);
                    FillRect(tex, x, y, w, t, col);
                    break;
                case 'V':
                    FillRect(tex, x, y + 8, t, h - 8, col);
                    FillRect(tex, x + w - t, y + 8, t, h - 8, col);
                    FillRect(tex, x + 4, y, w - 8, t, col);
                    break;
                case 'W':
                    FillRect(tex, x, y, t, h, col);
                    FillRect(tex, x + w - t, y, t, h, col);
                    FillRect(tex, x, y, w, t, col);
                    FillRect(tex, midX, y, t, 2 * h / 3, col);
                    break;
                case 'X':
                    FillRect(tex, x, y, t, h / 2 - 2, col);
                    FillRect(tex, x, y + h / 2 + 2, t, h / 2 - 2, col);
                    FillRect(tex, x + w - t, y, t, h / 2 - 2, col);
                    FillRect(tex, x + w - t, y + h / 2 + 2, t, h / 2 - 2, col);
                    FillRect(tex, x, midY, w, t, col);
                    break;
                case 'Y':
                    FillRect(tex, x, y + h / 2, t, h / 2, col);
                    FillRect(tex, x + w - t, y + h / 2, t, h / 2, col);
                    FillRect(tex, x, midY, w, t, col);
                    FillRect(tex, midX, y, t, h / 2, col);
                    break;
                case 'Z':
                    FillRect(tex, x, y + h - t, w, t, col);
                    FillRect(tex, x, y, w, t, col);
                    FillRect(tex, x + w - t, y + h / 2, t, h / 2, col);
                    FillRect(tex, x, midY, w, t, col);
                    FillRect(tex, x, y, t, h / 2, col);
                    break;
            }
        }

        private static void FillRect(Texture2D tex, int rx, int ry, int rw, int rh, Color col)
        {
            for (int y = ry; y < ry + rh; y++)
            {
                for (int x = rx; x < rx + rw; x++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
        }

        private static void GenerateProtestBannerTexture(string filePath, string line1, string line2)
        {
            int width = 1024;
            int height = 256;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color cloth = new Color(0.89f, 0.87f, 0.81f);
            Color borderRed = new Color(0.55f, 0.12f, 0.12f);
            Color inkDark = new Color(0.10f, 0.10f, 0.12f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float wrinkle = Mathf.Sin(x * 0.03f) * 0.015f + (Mathf.PerlinNoise(x * 0.05f, y * 0.05f) - 0.5f) * 0.05f;
                    if (x < 14 || x >= width - 14 || y < 14 || y >= height - 14)
                    {
                        tex.SetPixel(x, y, borderRed);
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color(
                            Mathf.Clamp01(cloth.r + wrinkle),
                            Mathf.Clamp01(cloth.g + wrinkle),
                            Mathf.Clamp01(cloth.b + wrinkle),
                            1f));
                    }
                }
            }

            DrawCenteredBlockText(tex, line1, yCenter: 170, charHeight: 74, charWidth: 44, spacing: 12, stroke: 9, borderRed);
            DrawCenteredBlockText(tex, line2, yCenter: 80, charHeight: 74, charWidth: 40, spacing: 10, stroke: 9, inkDark);

            tex.Apply();
            File.WriteAllBytes(filePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateSignMaterial(string matName, string texturePath, Shader shader, float smoothness, Color? emission = null)
        {
            string matPath = $"{MaterialsFolder}/{matName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            else mat.color = Color.white;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                else mat.mainTexture = tex;
            }

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
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

        private static void CreateConeMaterial(string matName, Shader shader)
        {
            string matPath = $"{MaterialsFolder}/{matName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            Color orange = new Color(0.98f, 0.42f, 0.05f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", orange);
            else mat.color = orange;

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.35f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
        }
    }
}
