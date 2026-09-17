using NUnit.Framework;
using Triki.Gameplay;
using UnityEngine;

namespace Triki.Tests
{
    /// <summary>
    /// El tablero está quieto casi todo el tiempo: sin límite de fotogramas el equipo trabaja
    /// para nada. Estos tests fijan la intención para que no se pierda en un refactor.
    /// </summary>
    public class FrameRateLimiterTests
    {
        private int _targetFrameRate;
        private int _vSyncCount;

        [SetUp]
        public void SetUp()
        {
            _targetFrameRate = Application.targetFrameRate;
            _vSyncCount = QualitySettings.vSyncCount;
        }

        [TearDown]
        public void TearDown()
        {
            Application.targetFrameRate = _targetFrameRate;
            QualitySettings.vSyncCount = _vSyncCount;
        }

        [Test]
        public void Apply_CapsTheFrameRate()
        {
            Application.targetFrameRate = -1;

            FrameRateLimiter.Apply();

            Assert.AreEqual(FrameRateLimiter.TargetFrameRate, Application.targetFrameRate);
        }

        [Test]
        public void Apply_TurnsOffVSync_OrTheCapIsIgnored()
        {
            QualitySettings.vSyncCount = 1;

            FrameRateLimiter.Apply();

            Assert.AreEqual(0, QualitySettings.vSyncCount, "Con vSync activo Unity ignora targetFrameRate.");
        }

        [Test]
        public void TargetFrameRate_IsEnoughForTheAnimations()
        {
            // Las animaciones más cortas duran 0,18 s: a 60 fps son unos 11 fotogramas.
            Assert.GreaterOrEqual(FrameRateLimiter.TargetFrameRate, 30);
            Assert.LessOrEqual(FrameRateLimiter.TargetFrameRate, 120);
        }
    }
}
