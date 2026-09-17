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

        [TestCase("main-panel"), TestCase("history-panel"), TestCase("setup-panel"), TestCase("ai-options")]
        [TestCase("play-button"), TestCase("history-button"), TestCase("quit-button"), TestCase("back-button")]
        [TestCase("start-button"), TestCase("setup-back-button"), TestCase("sound-button")]
        [TestCase("mode-ai"), TestCase("mode-local")]
        [TestCase("difficulty-easy"), TestCase("difficulty-normal"), TestCase("difficulty-hard")]
        [TestCase("color-one"), TestCase("color-two")]
        [TestCase("tab-ai"), TestCase("tab-local"), TestCase("tab-legacy")]
        [TestCase("history-ai"), TestCase("history-local"), TestCase("history-legacy"), TestCase("legacy-stats")]
        [TestCase("ai-games"), TestCase("ai-easy"), TestCase("ai-normal"), TestCase("ai-hard")]
        public void MainMenu_HasElement(string elementName) => AssertHasElement("MainMenu.uxml", elementName);

        // HistoryView busca estos nombres dentro de cada fila de dificultad.
        [TestCase("ai-easy"), TestCase("ai-normal"), TestCase("ai-hard")]
        public void AiRow_HasResultLabels(string row)
        {
            var element = Clone("MainMenu.uxml").Q(row);
            foreach (var name in new[] { "wins", "losses", "draws" })
                Assert.IsNotNull(element.Q<Label>(name), $"{row} no tiene '{name}'.");
        }

        // Y estos dentro de cada instancia de ColorStatsTable.uxml.
        [TestCase("history-local"), TestCase("legacy-stats")]
        public void ColorTable_HasStatLabels(string table)
        {
            var element = Clone("MainMenu.uxml").Q(table);
            foreach (var name in new[] { "games", "draws", "player-one-name", "player-one-wins", "player-one-losses",
                         "player-two-name", "player-two-wins", "player-two-losses" })
                Assert.IsNotNull(element.Q<Label>(name), $"{table} no tiene '{name}'.");
        }

        [TestCase("status-label"), TestCase("restart-button"), TestCase("menu-button")]
        public void GameHud_HasElement(string elementName) => AssertHasElement("GameHud.uxml", elementName);

        [Test]
        public void MainMenu_StartsOnMainPanel()
        {
            var root = Clone("MainMenu.uxml");

            Assert.IsFalse(root.Q("main-panel").ClassListContains("hidden"));
            Assert.IsTrue(root.Q("history-panel").ClassListContains("hidden"));
            Assert.IsTrue(root.Q("setup-panel").ClassListContains("hidden"));
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
