using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    #region Singleton
    public static SceneController Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    #endregion

    [SerializeField] private TransitionEffect transitionEffect;

    private Dictionary<string, string> loadedScenesBySlot = new Dictionary<string, string>();
    private bool isBusy = false;

    public SceneTransitionPlan NewTransition()
    {
        return new SceneTransitionPlan();
    }

    private Coroutine ExecutePlan(SceneTransitionPlan sceneTransitionPlan)
    {
        if(isBusy)
        {
            Debug.LogWarning("SceneController is busy executing another plan.");
            return null;
        }
        isBusy = true;
        return StartCoroutine(ChangeSceneRoutine(sceneTransitionPlan));
    }

    private IEnumerator ChangeSceneRoutine(SceneTransitionPlan sceneTransitionPlan)
    {
       if(sceneTransitionPlan.Overlay && transitionEffect != null)
       {
            yield return transitionEffect.FadeIn();
            yield return new WaitForSeconds(0.5f);
       }

        foreach (var keySlot in sceneTransitionPlan.ScenesToUnload)
        {
            yield return UnloadSceneRoutine(keySlot);
        }

        if(sceneTransitionPlan.ClearUnusedAssets)
        {
            yield return CleanUpUnusedAssetsRoutine();
        }

        foreach (var kvp in sceneTransitionPlan.ScenesToLoad)
        {
            if(loadedScenesBySlot.ContainsKey(kvp.Key))
            {
                yield return UnloadSceneRoutine(kvp.Key);
            }
            yield return LoadAdditiveRoutine(kvp.Key, kvp.Value, sceneTransitionPlan.ActiveSceneName == kvp.Value);
        }

        if(sceneTransitionPlan.Overlay && transitionEffect != null)
        {
            yield return transitionEffect.FadeOut();
        }

        isBusy = false;
    }

    private IEnumerator LoadAdditiveRoutine(string slotKey, string sceneName, bool setActive)
    {
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (loadOp == null)
        {
            yield break;
        }

        loadOp.allowSceneActivation = false;
        while (loadOp.progress < 0.9f)
        {
            yield return null;
        }

        loadOp.allowSceneActivation = true;
        while (!loadOp.isDone)
        {
            yield return null;
        }

        if(setActive)
        {
            Scene newScene = SceneManager.GetSceneByName(sceneName);
            if (newScene.IsValid() && newScene.isLoaded)
            {
                SceneManager.SetActiveScene(newScene);
            }
        }

        loadedScenesBySlot[slotKey] = sceneName;
    }

    private IEnumerator CleanUpUnusedAssetsRoutine()
    {
        AsyncOperation cleanUpOp = Resources.UnloadUnusedAssets();
        while(!cleanUpOp.isDone)
        {
            yield return null;
        }
    }

    private IEnumerator UnloadSceneRoutine(string keySlot)
    {
        if(!loadedScenesBySlot.TryGetValue(keySlot, out string sceneName))
        {
            yield break;
        }

        if(string.IsNullOrEmpty(sceneName))
        {
            yield break;
        }

        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(sceneName);
        if (unloadOp != null)
        {
            while (!unloadOp.isDone)
            {
                yield return null;
            }
        }
        loadedScenesBySlot.Remove(keySlot);
    }

    /// <summary>
    /// Gives a fluent interface to build a scene transition plan.
    /// </summary>
    public class SceneTransitionPlan
    {
        /// <summary>
        /// All scenes to load, mapped by their slot keys.
        /// </summary>
        public Dictionary<string, string> ScenesToLoad { get; private set; } = new Dictionary<string, string>();

        /// <summary>
        /// All scenes to unload, identified by their slot keys.
        /// </summary>
        public List<string> ScenesToUnload { get; } = new List<string>();

        /// <summary>
        /// The name of the scene to set as active after the transition.
        /// </summary>
        public string ActiveSceneName { get; private set; }

        /// <summary>
        /// Gets a value indicating whether unused assets should be cleared.
        /// </summary>
        public bool ClearUnusedAssets { get; private set; } = false;

        /// <summary>
        /// Indicates whether the scenes should be loaded as overlays.
        /// </summary>
        public bool Overlay { get; private set; } = false;

        /// <summary>
        /// Adds a scene to the loading plan and optionally sets it as the active scene.
        /// </summary>
        /// <param name="slotKey">The key identifying the slot where the scene should be loaded.</param>
        /// <param name="sceneName">The name of the scene to load.</param>
        /// <param name="setActive">A value indicating whether the specified scene should be set as the active scene.  <see langword="true"/> to set
        /// the scene as active; otherwise, <see langword="false"/>.</param>
        /// <returns>The current <see cref="SceneTransitionPlan"/> instance, allowing for method chaining.</returns>
        public SceneTransitionPlan Load(string slotKey, string sceneName, bool setActive = false)
        {
            ScenesToLoad[slotKey] = sceneName;
            if (setActive)
            {
                ActiveSceneName = sceneName;
            }
            return this;
        }

        /// <summary>
        /// Marks a scene identified by the specified slot key for unloading.
        /// </summary>
        /// <param name="slotKey">The unique key identifying the scene to unload. Cannot be null or empty.</param>
        /// <returns>The current <see cref="SceneTransitionPlan"/> instance, allowing for method chaining.</returns>
        public SceneTransitionPlan Unload(string slotKey)
        {
            ScenesToUnload.Add(slotKey);
            return this;
        }

        /// <summary>
        /// Enables the overlay option for the scene transition plan.
        /// </summary>
        /// <returns>The current <see cref="SceneTransitionPlan"/> instance with the overlay option enabled.</returns>
        public SceneTransitionPlan WithOverlay()
        {
            Overlay = true;
            return this;
        }

        /// <summary>
        /// Configures the transition plan to clear unused assets during the scene transition.
        /// </summary>
        /// <returns>The current <see cref="SceneTransitionPlan"/> instance with the clear unused assets option enabled.</returns>
        public SceneTransitionPlan WithClearUnusedAssets()
        {
            ClearUnusedAssets = true;
            return this;
        }

        /// <summary>
        /// Executes the current plan and returns a coroutine representing its execution.
        /// </summary>
        /// <remarks>This method delegates the execution of the plan to the <see cref="SceneController"/>
        /// singleton instance. Ensure that the <see cref="SceneController.Instance"/> is properly initialized before
        /// calling this method.</remarks>
        /// <returns>A <see cref="Coroutine"/> that represents the asynchronous execution of the plan.</returns>
        public Coroutine Perform()
        {
            return SceneController.Instance.ExecutePlan(this);
        }
    }
}
