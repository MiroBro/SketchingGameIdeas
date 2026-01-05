using UnityEngine;

namespace MagicCreatureDigseum
{
    public class CreatureVisual : MonoBehaviour
    {
        private CreatureData data;
        private SpriteRenderer spriteRenderer;

        public void Initialize(CreatureData creatureData)
        {
            data = creatureData;
            CreateVisual();
        }

        void CreateVisual()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            int texSize = 48;
            Texture2D texture = new Texture2D(texSize, texSize);
            Color[] pixels = new Color[texSize * texSize];

            // Clear
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            int centerX = texSize / 2;
            int centerY = texSize / 2;

            // Draw body (ellipse)
            int bodyW = (int)(8 + data.bodyWidth * 12);
            int bodyH = (int)(6 + data.bodyHeight * 10);
            DrawEllipse(pixels, texSize, centerX, centerY - 4, bodyW, bodyH, data.bodyColor);

            // Draw legs based on legCount
            int numLegs = 2 + (int)(data.legCount * 4); // 2-6 legs
            int legSpacing = bodyW * 2 / (numLegs + 1);
            for (int i = 0; i < numLegs; i++)
            {
                int legX = centerX - bodyW + legSpacing * (i + 1);
                int legY = centerY - 4 - bodyH;
                DrawLine(pixels, texSize, legX, legY, legX, legY - 6, data.accentColor);
            }

            // Draw neck
            int neckHeight = (int)(data.neckLength * 12);
            int neckTopY = centerY - 4 + bodyH + neckHeight;
            if (neckHeight > 0)
            {
                DrawLine(pixels, texSize, centerX, centerY - 4 + bodyH, centerX, neckTopY, data.bodyColor);
            }

            // Draw head
            int headRadius = (int)(3 + data.headSize * 5);
            int headY = neckTopY + headRadius;
            DrawEllipse(pixels, texSize, centerX, headY, headRadius, headRadius, data.bodyColor);

            // Draw ears
            int earH = (int)(data.earSize * 8);
            if (earH > 1)
            {
                DrawTriangle(pixels, texSize, centerX - headRadius + 2, headY + headRadius, earH, true, data.accentColor);
                DrawTriangle(pixels, texSize, centerX + headRadius - 2, headY + headRadius, earH, true, data.accentColor);
            }

            // Draw eyes
            int numEyes = 1 + (int)(data.eyeCount * 3); // 1-4 eyes
            int eyeSpacing = headRadius * 2 / (numEyes + 1);
            for (int i = 0; i < numEyes; i++)
            {
                int eyeX = centerX - headRadius + eyeSpacing * (i + 1);
                DrawEllipse(pixels, texSize, eyeX, headY + 1, 2, 2, Color.white);
                DrawEllipse(pixels, texSize, eyeX, headY + 1, 1, 1, Color.black);
            }

            // Draw tail
            int tailLen = (int)(data.tailLength * 10);
            if (tailLen > 0)
            {
                int tailStartX = centerX + bodyW - 2;
                int tailStartY = centerY - 4;
                for (int t = 0; t < tailLen; t++)
                {
                    int tx = tailStartX + t;
                    int ty = tailStartY + (int)(Mathf.Sin(t * 0.5f) * 3);
                    if (tx >= 0 && tx < texSize && ty >= 0 && ty < texSize)
                    {
                        pixels[ty * texSize + tx] = data.accentColor;
                        if (ty + 1 < texSize) pixels[(ty + 1) * texSize + tx] = data.accentColor;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), texSize);
            spriteRenderer.sortingOrder = 5;
        }

        void DrawEllipse(Color[] pixels, int texSize, int cx, int cy, int rx, int ry, Color color)
        {
            for (int y = -ry; y <= ry; y++)
            {
                for (int x = -rx; x <= rx; x++)
                {
                    float dx = (float)x / rx;
                    float dy = (float)y / ry;
                    if (dx * dx + dy * dy <= 1f)
                    {
                        int px = cx + x;
                        int py = cy + y;
                        if (px >= 0 && px < texSize && py >= 0 && py < texSize)
                        {
                            pixels[py * texSize + px] = color;
                        }
                    }
                }
            }
        }

        void DrawLine(Color[] pixels, int texSize, int x1, int y1, int x2, int y2, Color color)
        {
            int steps = Mathf.Max(Mathf.Abs(x2 - x1), Mathf.Abs(y2 - y1));
            if (steps == 0) steps = 1;
            for (int i = 0; i <= steps; i++)
            {
                int x = x1 + (x2 - x1) * i / steps;
                int y = y1 + (y2 - y1) * i / steps;
                if (x >= 0 && x < texSize && y >= 0 && y < texSize)
                {
                    pixels[y * texSize + x] = color;
                    if (x + 1 < texSize) pixels[y * texSize + x + 1] = color;
                }
            }
        }

        void DrawTriangle(Color[] pixels, int texSize, int cx, int baseY, int height, bool pointUp, Color color)
        {
            for (int h = 0; h < height; h++)
            {
                int y = pointUp ? baseY + h : baseY - h;
                int width = (height - h) / 2 + 1;
                for (int x = -width; x <= width; x++)
                {
                    int px = cx + x;
                    if (px >= 0 && px < texSize && y >= 0 && y < texSize)
                    {
                        pixels[y * texSize + px] = color;
                    }
                }
            }
        }

        public CreatureData GetData()
        {
            return data;
        }
    }
}
