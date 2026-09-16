using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Triki.Tests
{
    /// <summary>
    /// Los scripts de UI buscan elementos por nombre; estos tests fallan si un UXML
    /// se renombra sin actualizar el código (en runtime sería un NullReferenceException).
    /// </summary>
    public class UiContractTests
    {
        private const string UiFolder = "Assets/_Project/UI/";

        [TestCase("main-panel"), TestCase("history-panel")]
        [TestCase("play-button"), TestCase("history-button"), TestCase("quit-button"), TestCase("back-button")]
        [TestCase("games-played"), TestCase("draws")]
        [TestCase("player-one-name"), TestCase("player-one-wins"), TestCase("player-one-losses")]
        [TestCase("player-two-name"), TestCase("player-two-wins"), TestCase("player-two-losses")]
        public void MainMenu_HasElement(string elementName) => AssertHasElement("MainMenu.uxml", elementName);

        [TestCase("status-label"), TestCase("restart-button"), TestCase("menu-button")]
        public void GameHud_HasElement(string elementName) => AssertHasElement("GameHud.uxml", elementName);

        [Test]
        public void MainMenu_StartsOnMainPanel()
        {
            var root = Clone("MainMenu.uxml");

            Assert.IsFalse(root.Q("main-panel").ClassListContains("hidden"));
            Assert.IsTrue(root.Q("history-panel").ClassListContains("hidden"));
        }

        [Test]
        public void PanelSettings_HasThemeAndScalesWithScreen()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiFolder + "TrikiPanelSettings.asset");

            Assert.IsNotNull(settings);
            Assert.IsNotNull(settings.themeStyleSheet, "Sin tema no se dibuja texto.");
            Assert.AreEqual(PanelScaleMode.ScaleWithScreenSize, settings.scaleMode);
        }

        private static void AssertHasElement(string uxml, string elementName)
        {
            Assert.IsNotNull(Clone(uxml).Q(elementName), $"{uxml} no tiene '{elementName}'.");
        }

        private static VisualElement Clone(string uxml)
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiFolder + uxml);
            Assert.IsNotNull(tree, $"No se encontró {uxml}.");
            return tree.CloneTree();
        }
    }
}
