using NUnit.Framework;
using Triki.Gameplay;
using Triki.UI;
using UnityEngine;

namespace Triki.Tests
{
    public class AdaptiveLayoutTests
    {
        private const float Tolerance = 0.001f;
        private static readonly Vector2 BoardHalf = new Vector2(3.8f, 3.8f);

        [Test]
        public void Landscape_WithoutReserves_FitsHeight()
        {
            var fit = CameraFit.Compute(BoardHalf, 1920, 1080, new Rect(0, 0, 1920, 1080), 0f, 0f);

            Assert.AreEqual(3.8f, fit.OrthographicSize, Tolerance);
            Assert.AreEqual(Vector2.zero, fit.RegionCenterOffset);
        }

        [Test]
        public void Portrait_FitsWidth()
        {
            var fit = CameraFit.Compute(BoardHalf, 1080, 1920, new Rect(0, 0, 1080, 1920), 0f, 0f);

            // Ancho visible = 2 · size · (1080/1920) debe cubrir 2 · 3.8.
            Assert.AreEqual(3.8f / (1080f / 1920f), fit.OrthographicSize, Tolerance);
        }

        [Test]
        public void SymmetricReserves_GrowSize_WithoutMovingCenter()
        {
            var fit = CameraFit.Compute(BoardHalf, 1920, 1080, new Rect(0, 0, 1920, 1080), 0.12f, 0.12f);

            Assert.AreEqual(3.8f / 0.76f, fit.OrthographicSize, Tolerance);
            Assert.AreEqual(0f, fit.RegionCenterOffset.y, Tolerance);
        }

        [Test]
        public void TopReserveOnly_ShiftsRegionDown()
        {
            var fit = CameraFit.Compute(BoardHalf, 1920, 1080, new Rect(0, 0, 1920, 1080), 0.2f, 0f);

            // Zona útil: 0..0.8 del alto -> su centro queda 0.1 por debajo del centro de pantalla.
            Assert.AreEqual(3.8f / 0.8f, fit.OrthographicSize, Tolerance);
            Assert.AreEqual(-0.1f * 2f * fit.OrthographicSize, fit.RegionCenterOffset.y, Tolerance);
            Assert.AreEqual(0f, fit.RegionCenterOffset.x, Tolerance);
        }

        [TestCase(1920, 1080, 0, 0, 1920, 1080, 0.12f, 0.12f, TestName = "PC 16:9")]
        [TestCase(1080, 2400, 0, 90, 1080, 2230, 0.12f, 0.12f, TestName = "Móvil vertical con muesca")]
        [TestCase(2400, 1080, 100, 0, 2200, 1080, 0.12f, 0.12f, TestName = "Móvil horizontal con muesca")]
        [TestCase(1024, 768, 0, 0, 1024, 768, 0.2f, 0.05f, TestName = "4:3 con reservas asimétricas")]
        [TestCase(800, 3000, 0, 0, 800, 3000, 0.12f, 0.12f, TestName = "Muy estrecha")]
        public void Board_AlwaysFitsInsideUsableRegion(int width, int height, int safeX, int safeY, int safeW, int safeH, float top, float bottom)
        {
            var safe = new Rect(safeX, safeY, safeW, safeH);
            var fit = CameraFit.Compute(BoardHalf, width, height, safe, top, bottom);
            var size = fit.OrthographicSize;
            var aspect = (float)width / height;

            // Bordes del tablero (centrado en el offset) en coordenadas de viewport.
            var cx = 0.5f + fit.RegionCenterOffset.x / (2f * size * aspect);
            var cy = 0.5f + fit.RegionCenterOffset.y / (2f * size);
            var hx = BoardHalf.x / (2f * size * aspect);
            var hy = BoardHalf.y / (2f * size);

            Assert.GreaterOrEqual(cx - hx, safe.xMin / width - Tolerance, "se sale por la izquierda");
            Assert.LessOrEqual(cx + hx, safe.xMax / width + Tolerance, "se sale por la derecha");
            Assert.GreaterOrEqual(cy - hy, safe.yMin / height + bottom - Tolerance, "invade la franja inferior");
            Assert.LessOrEqual(cy + hy, safe.yMax / height - top + Tolerance, "invade la franja superior");

            // Y lo aprovecha: toca al menos uno de los dos ejes.
            var usedX = 2f * hx / ((safe.xMax - safe.xMin) / width);
            var usedY = 2f * hy / ((safe.yMax - safe.yMin) / height - top - bottom);
            Assert.AreEqual(1f, Mathf.Max(usedX, usedY), Tolerance, "el tablero no usa todo el espacio disponible");
        }

        [Test]
        public void DegenerateInputs_DoNotProduceNaN()
        {
            var zeroScreen = CameraFit.Compute(BoardHalf, 0, 0, Rect.zero, 0.12f, 0.12f);
            var hugeReserves = CameraFit.Compute(BoardHalf, 1920, 1080, new Rect(0, 0, 1920, 1080), 0.4f, 0.4f);

            Assert.IsFalse(float.IsNaN(zeroScreen.OrthographicSize) || float.IsInfinity(zeroScreen.OrthographicSize));
            Assert.IsFalse(float.IsNaN(hugeReserves.OrthographicSize) || float.IsInfinity(hugeReserves.OrthographicSize));
            Assert.Greater(hugeReserves.OrthographicSize, 0f);
        }

        [Test]
        public void SafeAreaInsets_ScaleToPanelUnits()
        {
            // Pantalla 1080x2400 con muesca arriba (80 px) y barra abajo (40 px); panel de 540 unidades de ancho.
            var insets = SafeAreaInsets.Compute(1080, 2400, new Rect(0, 40, 1080, 2280), 540);

            Assert.AreEqual(0f, insets.Left, Tolerance);
            Assert.AreEqual(0f, insets.Right, Tolerance);
            Assert.AreEqual(40f, insets.Top, Tolerance);
            Assert.AreEqual(20f, insets.Bottom, Tolerance);
        }

        [Test]
        public void SafeAreaInsets_FullScreen_AreZero_AndInvalidInputIsSafe()
        {
            var full = SafeAreaInsets.Compute(1920, 1080, new Rect(0, 0, 1920, 1080), 1920);
            var invalid = SafeAreaInsets.Compute(0, 0, Rect.zero, 0);

            Assert.AreEqual(0f, full.Left + full.Right + full.Top + full.Bottom, Tolerance);
            Assert.AreEqual(0f, invalid.Left + invalid.Right + invalid.Top + invalid.Bottom, Tolerance);
        }
    }
}
