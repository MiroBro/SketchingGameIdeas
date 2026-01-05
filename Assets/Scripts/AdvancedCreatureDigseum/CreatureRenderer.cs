using UnityEngine;

namespace AdvancedCreatureDigseum
{
    public class CreatureRenderer : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;

        public void RenderAnimal(AnimalData animal)
        {
            RenderTraits(animal.Traits);
        }

        public void RenderHybrid(HybridData hybrid)
        {
            RenderTraits(hybrid.MixedTraits);
        }

        public void RenderTraits(AnimalTraits traits)
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            int texSize = 64;
            Texture2D texture = new Texture2D(texSize, texSize);
            Color[] pixels = new Color[texSize * texSize];

            // Clear
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            int centerX = texSize / 2;
            int baseY = 10;

            // Calculate dimensions from traits
            int bodyW = (int)(8 + traits.BodyLength * 20);
            int bodyH = (int)(6 + traits.BodyHeight * 14);
            int neckH = (int)(traits.NeckLength * 18);
            int legH = (int)(traits.LegLength * 12);
            int numLegs = (int)(traits.LegCount * 4);
            int tailLen = (int)(traits.TailLength * 16);
            int headR = (int)(4 + traits.HeadSize * 8);
            int earH = (int)(traits.EarSize * 8);

            // Draw legs first (behind body)
            if (numLegs > 0 && legH > 0)
            {
                int legSpacing = bodyW * 2 / (numLegs + 1);
                for (int i = 0; i < numLegs; i++)
                {
                    int legX = centerX - bodyW + legSpacing * (i + 1);
                    for (int ly = 0; ly < legH; ly++)
                    {
                        int py = baseY - ly - 1;
                        if (py >= 0 && py < texSize)
                        {
                            SetPixelSafe(pixels, texSize, legX, py, traits.BodyColor * 0.8f);
                            SetPixelSafe(pixels, texSize, legX + 1, py, traits.BodyColor * 0.8f);
                        }
                    }
                }
            }

            // Draw tail
            if (tailLen > 0)
            {
                int tailStartX = centerX - bodyW + 2;
                int tailY = baseY + bodyH / 2;
                for (int t = 0; t < tailLen; t++)
                {
                    int tx = tailStartX - t;
                    int ty = tailY + (int)(Mathf.Sin(t * 0.4f) * 3);
                    SetPixelSafe(pixels, texSize, tx, ty, traits.AccentColor);
                    SetPixelSafe(pixels, texSize, tx, ty + 1, traits.AccentColor);
                }
            }

            // Draw body (ellipse)
            int bodyCenterY = baseY + bodyH / 2;
            for (int y = -bodyH; y <= bodyH; y++)
            {
                for (int x = -bodyW; x <= bodyW; x++)
                {
                    float dx = (float)x / bodyW;
                    float dy = (float)y / bodyH;
                    if (dx * dx + dy * dy <= 1f)
                    {
                        int px = centerX + x;
                        int py = bodyCenterY + y;
                        SetPixelSafe(pixels, texSize, px, py, traits.BodyColor);
                    }
                }
            }

            // Draw neck
            int neckTopY = bodyCenterY + bodyH;
            if (neckH > 0)
            {
                neckTopY = bodyCenterY + bodyH + neckH;
                int neckW = 3;
                for (int ny = 0; ny < neckH; ny++)
                {
                    int py = bodyCenterY + bodyH + ny;
                    for (int nx = -neckW; nx <= neckW; nx++)
                    {
                        SetPixelSafe(pixels, texSize, centerX + bodyW / 2 + nx, py, traits.BodyColor);
                    }
                }
            }

            // Draw head
            int headX = centerX + bodyW / 2;
            int headY = neckTopY + headR / 2;
            for (int y = -headR; y <= headR; y++)
            {
                for (int x = -headR; x <= headR; x++)
                {
                    if (x * x + y * y <= headR * headR)
                    {
                        SetPixelSafe(pixels, texSize, headX + x, headY + y, traits.BodyColor);
                    }
                }
            }

            // Draw eyes
            int eyeX = headX + headR / 2;
            int eyeY = headY + headR / 4;
            SetPixelSafe(pixels, texSize, eyeX, eyeY, Color.white);
            SetPixelSafe(pixels, texSize, eyeX + 1, eyeY, Color.white);
            SetPixelSafe(pixels, texSize, eyeX, eyeY + 1, Color.black);

            // Draw ears
            if (earH > 1)
            {
                for (int e = 0; e < earH; e++)
                {
                    int ew = earH - e;
                    int ey = headY + headR + e;
                    for (int ex = -ew / 2; ex <= ew / 2; ex++)
                    {
                        SetPixelSafe(pixels, texSize, headX - headR / 2 + ex, ey, traits.AccentColor);
                        SetPixelSafe(pixels, texSize, headX + headR / 2 + ex, ey, traits.AccentColor);
                    }
                }
            }

            // Draw fins
            if (traits.HasFins > 0.3f)
            {
                int finH = (int)(traits.HasFins * 10);
                int finY = bodyCenterY + bodyH / 2;
                for (int f = 0; f < finH; f++)
                {
                    int fw = finH - f;
                    for (int fx = 0; fx < fw; fx++)
                    {
                        SetPixelSafe(pixels, texSize, centerX + fx, finY + f, traits.AccentColor);
                    }
                }
                // Side fins
                int sideFinX = centerX + bodyW - 2;
                int sideFinY = bodyCenterY;
                for (int sf = 0; sf < finH / 2; sf++)
                {
                    SetPixelSafe(pixels, texSize, sideFinX + sf, sideFinY - sf, traits.AccentColor);
                    SetPixelSafe(pixels, texSize, sideFinX + sf, sideFinY + sf, traits.AccentColor);
                }
            }

            // Draw wings
            if (traits.HasWings > 0.3f)
            {
                int wingW = (int)(traits.HasWings * 15);
                int wingH = (int)(traits.HasWings * 8);
                int wingY = bodyCenterY + bodyH / 2;

                for (int wy = 0; wy < wingH; wy++)
                {
                    int rowW = wingW - wy;
                    for (int wx = 0; wx < rowW; wx++)
                    {
                        SetPixelSafe(pixels, texSize, centerX - bodyW / 2 - wx, wingY + wy, traits.AccentColor * 0.9f);
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.2f), texSize);
            spriteRenderer.sortingOrder = 5;
        }

        void SetPixelSafe(Color[] pixels, int size, int x, int y, Color color)
        {
            if (x >= 0 && x < size && y >= 0 && y < size)
            {
                pixels[y * size + x] = color;
            }
        }
    }
}
