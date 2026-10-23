using UnityEngine;
using UnityEditor;
using System.IO;

namespace RutaSegura.Editor
{
    /// <summary>
    /// Generador de texturas y materiales para la señalética vial de la barricada.
    /// Crea letreros urbanos creíbles estilo Jalisco (CALLE CERRADA y DESVÍO)
    /// inspirados directamente en el prototipo en video del usuario.
    /// </summary>
    public static class RoadSignTextureGenerator
    {
        private const string TexturesFolder = "Assets/_Project/Textures";
        private const string MaterialsFolder = "Assets/_Project/Materials";

        [MenuItem("Tools/Ruta Segura/Generar Señalética Vial (Calle Cerrada y Desvío)")]
        public static void GenerateRoadSigns()
        {
            if (!AssetDatabase.IsValidFolder(TexturesFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Textures");
            }
            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            }

            // 1. Generar Textura CALLE CERRADA
            string pathCerrada = $"{TexturesFolder}/Tex_CalleCerrada.png";
            GenerateSignTexture(pathCerrada, "CALLE", "CERRADA", drawArrow: false);

            // 2. Generar Textura DESVÍO con flecha
            string pathDesvio = $"{TexturesFolder}/Tex_Desvio.png";
            GenerateSignTexture(pathDesvio, "DESVÍO", "➔", drawArrow: true);

            AssetDatabase.Refresh();

            // 3. Crear Materiales URP Lit
            var urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            CreateSignMaterial("M_Placa_CalleCerrada", pathCerrada, urpShader);
            CreateSignMaterial("M_Placa_Desvio", pathDesvio, urpShader);

            // 4. Material para Cono de Tráfico
            CreateConeMaterial("M_Cono_Trafico", urpShader);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Ruta Segura",
                "¡Señalética vial urbana generada con éxito!\n\nMateriales listos en:\n• M_Placa_CalleCerrada\n• M_Placa_Desvio\n• M_Cono_Trafico",
                "Aceptar");
        }

        private static void GenerateSignTexture(string filePath, string line1, string line2, bool drawArrow)
        {
            int w = 512;
            int h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color yellowBg = new Color(0.96f, 0.73f, 0.08f); // Amarillo vial reflectante
            Color borderBlack = new Color(0.10f, 0.10f, 0.12f); // Negro asfalto
            Color boltColor = new Color(0.35f, 0.35f, 0.38f);   // Tornillo metálico

            // Rellenar fondo con textura de vinilo ligeramente texturizado
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Pequeña variación de ruido para que no parezca vectorial plano
                    float noise = (Mathf.PerlinNoise(x * 0.05f, y * 0.05f) - 0.5f) * 0.04f;
                    Color baseCol = new Color(yellowBg.r + noise, yellowBg.g + noise, yellowBg.b + noise, 1f);

                    // Marco perimetral negro (borde exterior)
                    if (x < 12 || x > w - 12 || y < 12 || y > h - 12)
                    {
                        tex.SetPixel(x, y, borderBlack);
                    }
                    // Borde interno estilizado de señalética oficial
                    else if ((x >= 20 && x <= 26) || (x >= w - 26 && x <= w - 20) ||
                             (y >= 20 && y <= 26) || (y >= h - 26 && y <= h - 20))
                    {
                        tex.SetPixel(x, y, borderBlack);
                    }
                    else
                    {
                        tex.SetPixel(x, y, baseCol);
                    }

                    // 4 Pernos en las esquinas
                    if (IsNearPoint(x, y, 36, 36, 6) || IsNearPoint(x, y, w - 36, 36, 6) ||
                        IsNearPoint(x, y, 36, h - 36, 6) || IsNearPoint(x, y, w - 36, h - 36, 6))
                    {
                        tex.SetPixel(x, y, boltColor);
                    }
                }
            }

            // Dibujar tipografía stencil de bloque negra directamente en los píxeles
            if (!drawArrow)
            {
                // Letrero: CALLE CERRADA
                DrawCenteredBlockText(tex, "CALLE", yCenter: 160, charHeight: 52, charWidth: 32, spacing: 8, borderBlack);
                DrawCenteredBlockText(tex, "CERRADA", yCenter: 90, charHeight: 52, charWidth: 28, spacing: 6, borderBlack);
            }
            else
            {
                // Letrero: DESVÍO con flecha
                DrawCenteredBlockText(tex, "DESVIO", yCenter: 160, charHeight: 52, charWidth: 32, spacing: 8, borderBlack);
                DrawThickArrow(tex, xCenter: w / 2, yCenter: 88, arrowLength: 120, borderBlack);
            }

            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);
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

            // Barra horizontal de la flecha
            for (int y = yCenter - barThickness; y <= yCenter + barThickness; y++)
            {
                for (int x = startX; x <= endX - 20; x++)
                {
                    tex.SetPixel(x, y, col);
                }
            }

            // Punta triangular de la flecha hacia la derecha
            int headSize = 36;
            for (int dx = 0; dx <= headSize; dx++)
            {
                int curX = endX - headSize + dx;
                int currentHalfH = (headSize - dx);
                for (int y = yCenter - currentHalfH; y <= yCenter + currentHalfH; y++)
                {
                    if (curX >= 0 && curX < tex.width && y >= 0 && y < tex.height)
                    {
                        tex.SetPixel(curX, y, col);
                    }
                }
            }
        }

        private static void DrawCenteredBlockText(Texture2D tex, string text, int yCenter, int charHeight, int charWidth, int spacing, Color col)
        {
            int totalWidth = text.Length * charWidth + (text.Length - 1) * spacing;
            int startX = (tex.width - totalWidth) / 2;
            int halfH = charHeight / 2;
            int t = 8; // Grosor de trazo

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                int cx = startX + i * (charWidth + spacing);
                int cy = yCenter - halfH;

                DrawBlockChar(tex, c, cx, cy, charWidth, charHeight, t, col);
            }
        }

        private static void DrawBlockChar(Texture2D tex, char c, int x, int y, int w, int h, int t, Color col)
        {
            // Dibuja glifos en bloques de alta legibilidad
            switch (c)
            {
                case 'C':
                    FillRect(tex, x, y, t, h, col); // Izq
                    FillRect(tex, x, y + h - t, w, t, col); // Arriba
                    FillRect(tex, x, y, w, t, col); // Abajo
                    break;
                case 'A':
                    FillRect(tex, x, y, t, h, col); // Izq
                    FillRect(tex, x + w - t, y, t, h, col); // Der
                    FillRect(tex, x, y + h - t, w, t, col); // Arriba
                    FillRect(tex, x, y + h / 2 - t / 2, w, t, col); // Medio
                    break;
                case 'L':
                    FillRect(tex, x, y, t, h, col); // Izq
                    FillRect(tex, x, y, w, t, col); // Abajo
                    break;
                case 'E':
                    FillRect(tex, x, y, t, h, col); // Izq
                    FillRect(tex, x, y + h - t, w, t, col); // Arriba
                    FillRect(tex, x, y + h / 2 - t / 2, w - 4, t, col); // Medio
                    FillRect(tex, x, y, w, t, col); // Abajo
                    break;
                case 'R':
                    FillRect(tex, x, y, t, h, col); // Izq
                    FillRect(tex, x, y + h - t, w, t, col); // Arriba
                    FillRect(tex, x + w - t, y + h / 2, t, h / 2, col); // Der sup
                    FillRect(tex, x, y + h / 2, w, t, col); // Medio
                    FillRect(tex, x + w - t, y, t, h / 2, col); // Pata der
                    break;
                case 'D':
                    FillRect(tex, x, y, t, h, col); // Izq
                    FillRect(tex, x, y + h - t, w - 4, t, col); // Arriba
                    FillRect(tex, x, y, w - 4, t, col); // Abajo
                    FillRect(tex, x + w - t, y + 4, t, h - 8, col); // Der
                    break;
                case 'S':
                    FillRect(tex, x, y + h - t, w, t, col); // Arriba
                    FillRect(tex, x, y + h / 2, t, h / 2, col); // Izq sup
                    FillRect(tex, x, y + h / 2 - t / 2, w, t, col); // Medio
                    FillRect(tex, x + w - t, y, t, h / 2, col); // Der inf
                    FillRect(tex, x, y, w, t, col); // Abajo
                    break;
                case 'V':
                    FillRect(tex, x, y + 8, t, h - 8, col); // Izq
                    FillRect(tex, x + w - t, y + 8, t, h - 8, col); // Der
                    FillRect(tex, x + 4, y, w - 8, t, col); // Abajo
                    break;
                case 'I':
                case 'Í':
                    FillRect(tex, x + w / 2 - t / 2, y, t, h, col);
                    FillRect(tex, x + 4, y + h - t, w - 8, t, col);
                    FillRect(tex, x + 4, y, w - 8, t, col);
                    break;
                case 'O':
                    FillRect(tex, x, y, t, h, col); // Izq
                    FillRect(tex, x + w - t, y, t, h, col); // Der
                    FillRect(tex, x, y + h - t, w, t, col); // Arriba
                    FillRect(tex, x, y, w, t, col); // Abajo
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

        private static void CreateSignMaterial(string matName, string texturePath, Shader shader)
        {
            string matPath = $"{MaterialsFolder}/{matName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                else mat.mainTexture = tex;
            }

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.45f);

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

            // Naranja de cono vial de seguridad
            Color orange = new Color(0.98f, 0.42f, 0.05f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", orange);
            else mat.color = orange;

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.35f);

            EditorUtility.SetDirty(mat);
        }
    }
}
