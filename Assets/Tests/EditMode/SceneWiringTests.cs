using System.Collections.Generic;
using NUnit.Framework;
using Triki.Gameplay;
using Triki.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Triki.Tests
{
    /// <summary>
    /// Abre las escenas como preview (no toca las escenas abiertas en el editor) y comprueba que
    /// no falten scripts y que todas las referencias del inspector estén asignadas.
    /// </summary>
    public class SceneWiringTests
    {
        private const string MenuScene = "Assets/_Project/Scenes/Menu.unity";
        private const string GameScene = "Assets/_Project/Scenes/Game.unity";

        private Scene _scene;

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
        }

        [TestCase(MenuScene), TestCase(GameScene)]
        public void Scene_HasNoMissingScripts(string path)
        {
            _scene = EditorSceneManager.OpenPreviewScene(path);

            foreach (var component in AllComponents<Component>())
                Assert.IsNotNull(component, $"{path} tiene un script faltante.");
        }

        [TestCase(MenuScene), TestCase(GameScene)]
        public void UIDocuments_HavePanelSettingsAndTree(string path)
        {
            _scene = EditorSceneManager.OpenPreviewScene(path);

            var documents = AllComponents<UIDocument>();
            Assert.IsNotEmpty(documents);
            foreach (var document in documents)
            {
                Assert.IsNotNull(document.panelSettings, $"{document.name} sin PanelSettings.");
                Assert.IsNotNull(document.visualTreeAsset, $"{document.name} sin UXML.");
            }
        }

        [Test]
        public void Menu_ComponentsAreWired()
        {
            _scene = EditorSceneManager.OpenPreviewScene(MenuScene);

            AssertSingleWired<MainMenuController>("_document");
            AssertSingleWired<SafeAreaPadding>("_document");
        }

        [Test]
        public void Game_ComponentsAreWired()
        {
            _scene = EditorSceneManager.OpenPreviewScene(GameScene);

            AssertSingleWired<GameController>("_boardView", "_camera");
            AssertSingleWired<GameHud>("_document", "_gameController");
            AssertSingleWired<SafeAreaPadding>("_document");
            AssertSingleWired<AudioFeedback>("_gameController");
            var fitter = AssertSingleWired<CameraFitter>("_boardView");
            Assert.IsNotNull(fitter.GetComponent<Camera>(), "CameraFitter debe estar en la cámara.");
        }

        private T AssertSingleWired<T>(params string[] fields) where T : Component
        {
            var found = AllComponents<T>();
            Assert.AreEqual(1, found.Count, $"Se esperaba un {typeof(T).Name}.");

            var serialized = new SerializedObject(found[0]);
            foreach (var field in fields)
            {
                var property = serialized.FindProperty(field);
                Assert.IsNotNull(property, $"{typeof(T).Name} no tiene el campo {field}.");
                Assert.IsNotNull(property.objectReferenceValue, $"{typeof(T).Name}.{field} sin asignar.");
            }
            return found[0];
        }

        private List<T> AllComponents<T>() where T : Component
        {
            var result = new List<T>();
            foreach (var root in _scene.GetRootGameObjects())
                result.AddRange(root.GetComponentsInChildren<T>(true));
            return result;
        }
    }
}
