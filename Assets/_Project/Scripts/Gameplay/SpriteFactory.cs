using UnityEngine;

namespace Triki.Gameplay
{
    /// <summary>
    /// Sprites de respaldo generados en runtime para poder jugar sin arte.
    /// Son blancos o grises: el color final lo da el tinte del SpriteRenderer.
    /// Quien crea un sprite es dueño de él y debe liberarlo con <see cref="Release"/>.
    /// </summary>
    internal static class SpriteFactory
    {
        /// <summary>Círculo blanco con borde suavizado; mide 1 unidad de diámetro.</summary>
        public static Sprite CreateCircle(int resolution)
        {
            return CreateRadial(resolution, "Triki Circle", (distance, dx, dy) => 1f);
        }

        /// <summary>
        /// Ficha con relieve: centro claro, borde más oscuro, un surco interior y un brillo arriba a la
        /// izquierda. Con tinte, el brillo es el color puro del jugador y el resto, versiones más oscuras.
        /// </summary>
        public static Sprite CreatePiece(int resolution)
        {
            return CreateRadial(resolution, "Triki Piece", (distance, dx, dy) =>
            {
                var shade = 0.86f - 0.14f * distance * distance;

                if (distance > 0.86f)
                    shade *= 0.7f;
                else if (Mathf.Abs(distance - 0.6f) < 0.035f)
                    shade *= 0.88f;

                var hx = dx + 0.32f;
                var hy = dy - 0.32f;
                var highlight = Mathf.Clamp01(1f - Mathf.Sqrt(hx * hx + hy * hy) / 0.42f);
                shade += 0.14f * highlight * highlight;

                return Mathf.Min(shade, 1f);
            });
        }

        /// <summary>Círculo blanco que se desvanece hacia el borde, para sombras.</summary>
        public static Sprite CreateSoftCircle(int resolution)
        {
            return CreateRadial(resolution, "Triki Soft Circle", (distance, dx, dy) => 1f, softEdge: true);
        }

        /// <summary>
        /// Rectángulo de esquinas redondeadas con bordes 9-slice: se estira con
        /// <see cref="SpriteDrawMode.Sliced"/> sin deformar las esquinas.
        /// </summary>
        public static Sprite CreateRoundedPanel(int resolution, int cornerRadius)
        {
            var texture = CreateTexture(resolution, "Triki Panel");
            var pixels = new Color32[resolution * resolution];
            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    // Distancia a la esquina redondeada más cercana (0 en el interior recto).
                    var cx = Mathf.Clamp(x + 0.5f, cornerRadius, resolution - cornerRadius);
                    var cy = Mathf.Clamp(y + 0.5f, cornerRadius, resolution - cornerRadius);
                    var dx = x + 0.5f - cx;
                    var dy = y + 0.5f - cy;
                    var alpha = Mathf.Clamp01(cornerRadius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    pixels[y * resolution + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var border = cornerRadius + 1f;
            return CreateSprite(texture, resolution, new Vector4(border, border, border, border));
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
            return CreateSprite(texture, resolution, Vector4.zero);
        }

        public static void Release(Sprite sprite)
        {
            if (sprite == null)
                return;

            var texture = sprite.texture;
            SafeDestroy(sprite);
            SafeDestroy(texture);
        }

        /// <summary>Destroy en Play; DestroyImmediate en el editor (tests, herramientas).</summary>
        public static void SafeDestroy(Object target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }

        private delegate float ShadeFunction(float normalizedDistance, float dx, float dy);

        /// <summary>
        /// Disco de 1 unidad de diámetro con borde suavizado. <paramref name="shade"/> recibe la distancia
        /// al centro normalizada (0..1) y la posición (-1..1, y hacia arriba) y devuelve el gris.
        /// Con <paramref name="softEdge"/> la opacidad cae gradualmente desde el 55 % del radio.
        /// </summary>
        private static Sprite CreateRadial(int resolution, string name, ShadeFunction shade, bool softEdge = false)
        {
            var texture = CreateTexture(resolution, name);
            var pixels = new Color32[resolution * resolution];
            var radius = resolution * 0.5f;

            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var px = x + 0.5f - radius;
                    var py = y + 0.5f - radius;
                    var distance = Mathf.Sqrt(px * px + py * py);
                    var normalized = distance / radius;
                    var alpha = Mathf.Clamp01(radius - distance);
                    if (softEdge)
                        alpha *= 1f - Mathf.SmoothStep(0.55f, 1f, normalized);
                    var grey = (byte)(Mathf.Clamp01(shade(normalized, px / radius, py / radius)) * 255f);
                    pixels[y * resolution + x] = new Color32(grey, grey, grey, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return CreateSprite(texture, resolution, Vector4.zero);
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

        private static Sprite CreateSprite(Texture2D texture, int pixelsPerUnit, Vector4 border)
        {
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                border);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}
