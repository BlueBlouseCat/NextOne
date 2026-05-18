using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public struct RestartDestination
{
    public string TargetSceneName;
    public string TargetSpawnPointId;

    public RestartDestination(string targetSceneName, string targetSpawnPointId)
    {
        TargetSceneName = targetSceneName;
        TargetSpawnPointId = targetSpawnPointId;
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(TargetSceneName);
}

[Serializable]
public class RestartRouteEntry
{
    [Header("Debug")]
    public string routeId = "new_route";

    [Header("Match Current Scene")]
    public string[] sourceSceneNames;

    [Header("Optional Flag Conditions")]
    public string[] requiredFlagsAllTrue;
    public string[] requiredFlagsAllFalse;

    [Header("Restart Destination")]
    public string targetSceneName = "BirthPlace";
    public string targetSpawnPointId = "";
}

public class SettingsCanvas : MonoBehaviour
{
    [Header("Fallback Restart")]
    [SerializeField] private string _fallbackRestartSceneName = "BirthPlace";
    [SerializeField] private string _fallbackRestartSpawnPointId = "restart_birthplace";

    [Header("Restart Routes")]
    [SerializeField] private RestartRouteEntry[] _restartRoutes;

    private bool _isRestarting;
    private static bool _isRestartingGlobally;

    public void OpenCanvas()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    public bool isOpened()
    {
        return gameObject.activeSelf;
    }

    public void ClosedCanvas()
    {
        gameObject.SetActive(false);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
#if !UNITY_WEBGL
        Application.Quit();
#endif
#endif
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainStart");
        ClosedCanvas();
    }

    public void RestartGameFromBeginning()
    {
        if (_isRestarting || _isRestartingGlobally)
            return;

        string currentSceneName = SceneManager.GetActiveScene().name;
        RestartDestination destination = ResolveRestartDestination(currentSceneName);

        if (!destination.IsValid)
            destination = new RestartDestination(_fallbackRestartSceneName, _fallbackRestartSpawnPointId);

        _isRestarting = true;
        _isRestartingGlobally = true;

        ClosedCanvas();

        RestartFromBeginningRunner.Begin(destination);
    }

    internal static void NotifyRestartFinished()
    {
        _isRestartingGlobally = false;

        SettingsCanvas[] canvases = Resources.FindObjectsOfTypeAll<SettingsCanvas>();
        for (int i = 0; i < canvases.Length; i++)
        {
            SettingsCanvas canvas = canvases[i];
            if (canvas == null) continue;
            if (canvas.gameObject == null) continue;
            if (!canvas.gameObject.scene.IsValid()) continue;

            canvas._isRestarting = false;
        }
    }

    private RestartDestination ResolveRestartDestination(string currentSceneName)
    {
        if (_restartRoutes != null)
        {
            for (int i = 0; i < _restartRoutes.Length; i++)
            {
                RestartRouteEntry route = _restartRoutes[i];
                if (route == null) continue;
                if (string.IsNullOrWhiteSpace(route.targetSceneName)) continue;
                if (!MatchesScene(route.sourceSceneNames, currentSceneName)) continue;
                if (!AreAllFlagsTrue(route.requiredFlagsAllTrue)) continue;
                if (!AreAllFlagsFalse(route.requiredFlagsAllFalse)) continue;

                return new RestartDestination(route.targetSceneName, route.targetSpawnPointId);
            }
        }

        return new RestartDestination(_fallbackRestartSceneName, _fallbackRestartSpawnPointId);
    }

    private bool MatchesScene(string[] sourceSceneNames, string currentSceneName)
    {
        if (sourceSceneNames == null || sourceSceneNames.Length == 0)
            return true;

        for (int i = 0; i < sourceSceneNames.Length; i++)
        {
            string sceneName = sourceSceneNames[i];
            if (string.IsNullOrWhiteSpace(sceneName)) continue;
            if (sceneName == currentSceneName) return true;
        }

        return false;
    }

    private bool AreAllFlagsTrue(string[] flags)
    {
        if (flags == null || flags.Length == 0)
            return true;

        if (GameManager.Instance == null)
            return false;

        for (int i = 0; i < flags.Length; i++)
        {
            string flag = flags[i];
            if (string.IsNullOrWhiteSpace(flag)) continue;
            if (!GameManager.Instance.GetFlag(flag)) return false;
        }

        return true;
    }

    private bool AreAllFlagsFalse(string[] flags)
    {
        if (flags == null || flags.Length == 0)
            return true;

        if (GameManager.Instance == null)
            return false;

        for (int i = 0; i < flags.Length; i++)
        {
            string flag = flags[i];
            if (string.IsNullOrWhiteSpace(flag)) continue;
            if (GameManager.Instance.GetFlag(flag)) return false;
        }

        return true;
    }

    [ContextMenu("Fill Default Restart Routes")]
    private void FillDefaultRestartRoutes()
    {
        _fallbackRestartSceneName = "BirthPlace";
        _fallbackRestartSpawnPointId = "restart_birthplace";

        _restartRoutes = new[]
        {
            new RestartRouteEntry
            {
                routeId = "restart_from_birthplace",
                sourceSceneNames = new[] { "BirthPlace" },
                targetSceneName = "BirthPlace",
                targetSpawnPointId = "restart_birthplace"
            },
            new RestartRouteEntry
            {
                routeId = "restart_from_outside_or_brush",
                sourceSceneNames = new[] { "OutsideOfHouse", "Brush", "Brush1" },
                targetSceneName = "OutsideOfHouse",
                targetSpawnPointId = "restart_outside"
            },
            new RestartRouteEntry
            {
                routeId = "restart_from_house_or_mouseholes",
                sourceSceneNames = new[]
                {
                    "House",
                    "MouseHole1",
                    "MouseHole2",
                    "MouseHole3",
                    "MouseHole4",
                    "MouseHole5",
                    "MouseHole6",
                    "MouseHole7"
                },
                targetSceneName = "House",
                targetSpawnPointId = "restart_house"
            }
        };
    }
}

public class RestartFromBeginningRunner : MonoBehaviour
{
    private RestartDestination _destination;

    public static void Begin(RestartDestination destination)
    {
        GameObject runnerObject = new GameObject("[RestartFromBeginningRunner]");
        DontDestroyOnLoad(runnerObject);

        RestartFromBeginningRunner runner = runnerObject.AddComponent<RestartFromBeginningRunner>();
        runner._destination = destination;
        runner.StartCoroutine(runner.RestartGameRoutine());
    }

    private IEnumerator RestartGameRoutine()
    {
        Time.timeScale = 1f;
        GlobalInteractionLock.Reset();

        if (ItemPreviewViewerUI.Instance != null)
            ItemPreviewViewerUI.Instance.CloseImmediate();

        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.ClosePickupPopup();
            InventoryUI.Instance.StopSlotHint();
            InventoryUI.Instance.ShowInteractHint(false);
        }

        if (BgmPlayer.Instance != null)
            BgmPlayer.Instance.Stop();

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.ClearAll();

        if (GameManager.Instance != null)
            GameManager.Instance.ResetRuntimeState();

        DestroyAllRuntimeObjects<SceneFader>();
        DestroyAllRuntimeObjects<ItemPreviewViewerUI>();

        yield return null;

        if (GameManager.Instance != null)
        {
            if (string.IsNullOrWhiteSpace(_destination.TargetSpawnPointId))
                GameManager.Instance.LoadScene(_destination.TargetSceneName);
            else
                GameManager.Instance.LoadScene(_destination.TargetSceneName, _destination.TargetSpawnPointId);
        }
        else
        {
            SceneManager.LoadScene(_destination.TargetSceneName, LoadSceneMode.Single);
        }

        SettingsCanvas.NotifyRestartFinished();
        Destroy(gameObject);
    }

    private static void DestroyAllRuntimeObjects<T>() where T : Component
    {
        T[] instances = Resources.FindObjectsOfTypeAll<T>();

        for (int i = 0; i < instances.Length; i++)
        {
            T instance = instances[i];
            if (instance == null) continue;

            GameObject go = instance.gameObject;
            if (go == null) continue;

            if (!go.scene.IsValid())
                continue;

            Destroy(go);
        }
    }
}
