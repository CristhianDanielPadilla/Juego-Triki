using UnityEngine;

namespace Triki.Gameplay
{
    /// <summary>
    /// Sprites de respaldo generados en runtime para poder jugar sin arte.
    /// Quien crea un sprite es dueño de él y debe liberarlo con <see cref="Release"/>.
    /// </summary>
    internal static class SpriteFactory
    {
        /// <summary>Círculo blanco con borde suavizado; mide 1 unidad de diámetro.</summary>
        public static Sprite CreateCircle(int resolution)
        {
            var texture = CreateTexture(resolution, "Triki Circle");
            var pixels = new Color32[resolution * resolution];
            var radius = resolution * 0.5f;

            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var dx = x + 0.5f - radius;
                    var dy = y + 0.5f - radius;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = Mathf.Clamp01(radius - distance);
                    pixels[y * resolution + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return CreateSprite(texture, resolution);
        }

        /// <summary>Cuadrado blanco de 1 unidad; se estira por escala para dibujar aristas.</summary>
        public static Sprite CreateSquare()
        {
            const int resolution = 4;
            var texture = CreateTexture(resolution, "Triki Square");
            var pixels = new Color32[resolution * resolution];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return CreateSprite(texture, resolution);
        }

        public static void Release(Sprite sprite)
        {
            if (sprite == null)
                return;

            var texture = sprite.texture;
            Object.Destroy(sprite);
            if (texture != null)
                Object.Destroy(texture);
        }

        private static Texture2D CreateTexture(int resolution, string name)
        {
            return new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
        }

        private static Sprite CreateSprite(Texture2D texture, int pixelsPerUnit)
        {
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}
