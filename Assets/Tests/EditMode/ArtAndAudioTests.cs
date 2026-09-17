using System;
using NUnit.Framework;
using Triki.Core;
using Triki.Gameplay;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Triki.Tests
{
    public class EasingTests
    {
        [Test]
        public void AllCurves_StartAtZero_EndAtOne_AndClampInput()
        {
            foreach (var curve in new Func<float, float>[] { Easing.OutBack, Easing.OutCubic, Easing.InOutCubic })
            {
                Assert.AreEqual(0f, curve(0f), 1e-4f);
                Assert.AreEqual(1f, curve(1f), 1e-4f);
                Assert.AreEqual(0f, curve(-3f), 1e-4f);
                Assert.AreEqual(1f, curve(5f), 1e-4f);
            }
        }

        [Test]
        public void OutBack_Overshoots()
        {
            var peak = 0f;
            for (var t = 0f; t <= 1f; t += 0.01f)
                peak = Mathf.Max(peak, Easing.OutBack(t));

            Assert.Greater(peak, 1.05f);
        }

        [Test]
        public void InOutCubic_IsSymmetricAroundHalf()
        {
            Assert.AreEqual(0.5f, Easing.InOutCubic(0.5f), 1e-4f);
            Assert.AreEqual(1f - Easing.InOutCubic(0.2f), Easing.InOutCubic(0.8f), 1e-4f);
        }
    }

    public class ToneSynthTests
    {
        [Test]
        public void Sweep_HasExpectedLength_StaysInRange_AndEndsSilent()
        {
            var samples = ToneSynth.Sweep(440f, 880f, 0.1f, 0.8f, 20f);

            Assert.AreEqual(4410, samples.Length);
            AssertInRange(samples, 0.8f);
            Assert.AreEqual(0f, samples[0], 1e-4f, "Sin clic al empezar.");
            Assert.AreEqual(0f, samples[samples.Length - 1], 1e-4f, "Sin clic al terminar.");
            Assert.Greater(Peak(samples), 0.1f, "Debe sonar algo.");
        }

        [Test]
        public void Sequence_ConcatenatesNotes_EachStartingAndEndingSilent()
        {
            var samples = ToneSynth.Sequence(new[] { 523.25f, 659.25f, 783.99f }, 0.1f, 1f, 5f);

            Assert.AreEqual(3 * 4410, samples.Length);
            AssertInRange(samples, 1f);
            Assert.AreEqual(0f, samples[4410], 1e-4f, "La 2.ª nota empieza desde silencio.");
            Assert.AreEqual(0f, samples[4409], 1e-4f, "La 1.ª nota termina en silencio.");
        }

        [Test]
        public void VolumeAboveOne_IsClamped()
        {
            AssertInRange(ToneSynth.Sweep(200f, 200f, 0.05f, 5f, 0f), 1f);
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ToneSynth.Sweep(440f, 440f, 0f, 1f, 1f));
            Assert.Throws<ArgumentException>(() => ToneSynth.Sequence(new float[0], 0.1f, 1f, 1f));
        }

        private static void AssertInRange(float[] samples, float limit)
        {
            foreach (var sample in samples)
            {
                Assert.IsFalse(float.IsNaN(sample));
                Assert.LessOrEqual(Mathf.Abs(sample), limit + 1e-4f);
            }
        }

        private static float Peak(float[] samples)
        {
            var peak = 0f;
            foreach (var sample in samples)
                peak = Mathf.Max(peak, Mathf.Abs(sample));
            return peak;
        }
    }

    public class GeneratedSpriteTests
    {
        [Test]
        public void Piece_IsOneUnitWide_AndNotKeptInCpuMemory()
        {
            var sprite = SpriteFactory.CreatePiece(64);
            try
            {
                Assert.AreEqual(1f, sprite.bounds.size.x, 1e-4f, "Debe medir 1 unidad.");
                Assert.IsFalse(sprite.texture.isReadable, "No debe duplicar la textura en memoria de CPU.");
            }
            finally
            {
                SpriteFactory.Release(sprite);
            }
        }

        [Test]
        public void RoundedPanel_HasNineSliceBorders()
        {
            var sprite = SpriteFactory.CreateRoundedPanel(64, 16);
            try
            {
                Assert.Greater(sprite.border.x, 16f);
                Assert.AreEqual(sprite.border.x, sprite.border.z);
                Assert.AreEqual(sprite.border.y, sprite.border.w);
            }
            finally
            {
                SpriteFactory.Release(sprite);
            }
        }

        [Test]
        public void Release_DestroysSpriteAndTexture_InEditMode()
        {
            var sprite = SpriteFactory.CreateSoftCircle(32);
            var texture = sprite.texture;

            SpriteFactory.Release(sprite);

            Assert.IsTrue(sprite == null, "Sprite no destruido.");
            Assert.IsTrue(texture == null, "Textura no destruida.");
        }
    }

    public class BoardViewAnimationTests
    {
        private GameObject _root;
        private BoardView _view;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("BoardView Test");
            _view = _root.AddComponent<BoardView>();
            _view.Build(BoardGraph.CreateSquare());
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void ShowBlockedCell_DimsOnlyThatNode()
        {
            var free = NodeColor(0);

            _view.ShowBlockedCell(BoardGraph.CenterCell);

            Assert.AreNotEqual(free, NodeColor(BoardGraph.CenterCell), "El centro vetado debe verse distinto.");
            Assert.AreEqual(free, NodeColor(0), "Las demás casillas no cambian.");

            _view.ShowBlockedCell(TrikiGame.NoCell);

            Assert.AreEqual(free, NodeColor(BoardGraph.CenterCell), "Sin veto vuelve a su color.");
        }

        private Color NodeColor(int cell) =>
            _root.transform.Find("Nodes/Node " + cell).GetComponent<SpriteRenderer>().color;

        [Test]
        public void ShowPiece_StartsHidden_AndPopsToFullSize()
        {
            _view.ShowPiece(4, Player.One);
            var piece = FirstActivePiece();

            Assert.AreEqual(0f, piece.localScale.x, 1e-4f, "La ficha aparece desde tamaño 0.");

            _view.CompleteAnimations();

            Assert.AreEqual(1.1f, piece.localScale.x, 1e-4f);
            Assert.AreEqual(_view.GetCellLocalPosition(4), piece.localPosition);
        }

        [Test]
        public void MovePiece_SlidesToTarget()
        {
            _view.ShowPiece(0, Player.Two);
            _view.CompleteAnimations();
            var piece = FirstActivePiece();

            _view.MovePiece(0, 4);
            Assert.AreEqual(_view.GetCellLocalPosition(0), piece.localPosition, "Empieza en el origen.");

            _view.CompleteAnimations();
            Assert.AreEqual(_view.GetCellLocalPosition(4), piece.localPosition);
        }

        [Test]
        public void MovePiece_WhileStillAppearing_EndsAtTargetAndFullSize()
        {
            _view.ShowPiece(0, Player.One);
            _view.MovePiece(0, 4);
            _view.CompleteAnimations();
            var piece = FirstActivePiece();

            Assert.AreEqual(_view.GetCellLocalPosition(4), piece.localPosition);
            Assert.AreEqual(1.1f, piece.localScale.x, 1e-4f, "No debe quedarse a medio aparecer.");
        }

        [Test]
        public void WinningLine_GrowsAndEnlargesWinners()
        {
            _view.ShowPiece(0, Player.One);
            _view.ShowPiece(1, Player.One);
            _view.ShowPiece(2, Player.One);
            _view.ShowPiece(4, Player.Two);

            _view.ShowWinningLine(new BoardLine(0, 1, 2), Player.One);
            var line = _root.transform.Find("Win Line");
            Assert.AreEqual(0f, line.localScale.x, 1e-4f, "La línea empieza sin longitud.");

            _view.CompleteAnimations();

            Assert.AreEqual(6f, line.localScale.x, 1e-4f, "Va de la casilla 0 a la 2 (2 · separación).");
            var pieces = _root.transform.Find("Pieces");
            Assert.AreEqual(1.1f * 1.2f, pieces.GetChild(0).localScale.x, 1e-4f, "Ganadora agrandada.");
            Assert.AreEqual(1.1f, pieces.GetChild(3).localScale.x, 1e-4f, "La rival no cambia.");
        }

        [Test]
        public void ClearPieces_HidesEverything_AndCancelsAnimations()
        {
            _view.ShowPiece(0, Player.One);
            _view.ClearPieces();

            Assert.IsNull(FirstActivePiece());

            _view.ShowPiece(0, Player.Two); // la casilla vuelve a estar libre
            _view.CompleteAnimations();
            Assert.AreEqual(1.1f, FirstActivePiece().localScale.x, 1e-4f);
        }

        [Test]
        public void Build_CreatesPlateSizedToBoard_AndShadowsUnderPieces()
        {
            var plate = _root.transform.Find("Board Plate").GetComponent<SpriteRenderer>();
            var half = _view.GetWorldHalfExtents();

            Assert.AreEqual(SpriteDrawMode.Sliced, plate.drawMode);
            Assert.AreEqual(half * 2f, plate.size);

            var piece = _root.transform.Find("Pieces").GetChild(0);
            var shadow = piece.Find("Shadow").GetComponent<SpriteRenderer>();
            Assert.Less(shadow.sortingOrder, piece.GetComponent<SpriteRenderer>().sortingOrder);
        }

        private Transform FirstActivePiece()
        {
            var pieces = _root.transform.Find("Pieces");
            for (var i = 0; i < pieces.childCount; i++)
            {
                if (pieces.GetChild(i).gameObject.activeSelf)
                    return pieces.GetChild(i);
            }
            return null;
        }
    }
}
