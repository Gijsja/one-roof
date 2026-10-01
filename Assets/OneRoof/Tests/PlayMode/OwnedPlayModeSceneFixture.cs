using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace OneRoof.Tests.PlayMode
{
    // Each fixture owns its worlds and cameras. Existing scene roots are restored,
    // never destroyed, even when an assertion interrupts the test coroutine.
    public abstract class OwnedPlayModeSceneFixture
    {
        private readonly List<GameObject> _suspendedRoots = new List<GameObject>();
        private readonly List<Scene> _ownedScenes = new List<Scene>();
        private Scene _previousActiveScene;

        [UnitySetUp]
        public IEnumerator CreateOwnedWorld()
        {
            _previousActiveScene = SceneManager.GetActiveScene();
            for (var i = 0; i < SceneManager.sceneCount; i++)
                foreach (var root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                    if (root.activeSelf && !HostsTestRunner(root))
                    {
                        _suspendedRoots.Add(root);
                        root.SetActive(false);
                    }
            var scene = SceneManager.CreateScene("OneRoof test " + System.Guid.NewGuid().ToString("N"));
            _ownedScenes.Add(scene);
            SceneManager.SetActiveScene(scene);
            yield return null;
        }

        protected IEnumerator LoadTestScene(string path)
        {
#if UNITY_EDITOR
            var priorHandles = new HashSet<int>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
                priorHandles.Add(SceneManager.GetSceneAt(i).handle);
            Scene loaded = default;
            void RegisterScene(Scene scene, LoadSceneMode mode)
            {
                if (scene.path != path || priorHandles.Contains(scene.handle)) return;
                loaded = scene;
                _ownedScenes.Add(scene); // Own it before any post-load assertion can fail.
                SceneManager.SetActiveScene(scene);
            }
            SceneManager.sceneLoaded += RegisterScene;
            try
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
                Assert.That(loaded.IsValid() && loaded.isLoaded, Is.True, "Authored test scene must load.");
            }
            finally { SceneManager.sceneLoaded -= RegisterScene; }
#else
            Assert.Ignore("Authored scene fixtures require the Editor.");
            yield break;
#endif
        }

        private static bool HostsTestRunner(GameObject root)
        {
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (component != null && component.GetType().Assembly.GetName().Name == "UnityEngine.TestRunner")
                    return true;
            return false;
        }

        [UnityTearDown]
        public IEnumerator RestoreWorld()
        {
            try
            {
                // Let deferred Destroy finish before unloading their containing scenes.
                yield return null;
                if (_previousActiveScene.IsValid() && _previousActiveScene.isLoaded)
                    SceneManager.SetActiveScene(_previousActiveScene);
                for (var i = _ownedScenes.Count - 1; i >= 0; i--)
                    if (_ownedScenes[i].IsValid() && _ownedScenes[i].isLoaded)
                        yield return SceneManager.UnloadSceneAsync(_ownedScenes[i]);
            }
            finally
            {
                _ownedScenes.Clear();
                foreach (var root in _suspendedRoots)
                    if (root != null) root.SetActive(true);
                _suspendedRoots.Clear();
            }
            yield return null;
        }
    }
}
