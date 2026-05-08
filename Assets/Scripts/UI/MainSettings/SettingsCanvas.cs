using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingsCanvas : MonoBehaviour
{
    [Header("Restart")]
    [SerializeField] private string _restartSceneName = "BirthPlace";

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

        _isRestarting = true;
        _isRestartingGlobally = true;

        ClosedCanvas();

        RestartFromBeginningRunner.Begin(_restartSceneName);
    }

    internal static void NotifyRestartFinished()
    {
        _isRestartingGlobally = false;
    }
}

public class RestartFromBeginningRunner : MonoBehaviour
{
    private string _restartSceneName;

    public static void Begin(string restartSceneName)
    {
        GameObject runnerObject = new GameObject("[RestartFromBeginningRunner]");
        DontDestroyOnLoad(runnerObject);

        RestartFromBeginningRunner runner = runnerObject.AddComponent<RestartFromBeginningRunner>();
        runner._restartSceneName = restartSceneName;
        runner.StartCoroutine(runner.RestartGameRoutine());
    }

    private IEnumerator RestartGameRoutine()
    {
        Time.timeScale = 1f;

        // 防止有全局交互锁残留，导致新开局不能操作。
        GlobalInteractionLock.Reset();

        // 关掉预览和交互提示，避免切场景瞬间残留 UI。
        if (ItemPreviewViewerUI.Instance != null)
            ItemPreviewViewerUI.Instance.CloseImmediate();

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.ShowInteractHint(false);

        // 让新开局的 BGM 从头开始。
        if (BgmPlayer.Instance != null)
            BgmPlayer.Instance.Stop();

        // GameManager 挂着 InventoryManager，所以删掉它就会一起清空 flags、visited、背包等运行时状态。
        DestroyAllRuntimeObjects<GameManager>();

        // 这些对象是跨场景常驻的，不删的话会把旧会话状态带进新开局。
        DestroyAllRuntimeObjects<SceneFader>();
        DestroyAllRuntimeObjects<ItemPreviewViewerUI>();

        // 等一帧，让 Destroy 真正完成，再进 BirthPlace，避免旧单例和新场景对象打架。
        yield return null;

        SceneManager.LoadScene(_restartSceneName, LoadSceneMode.Single);

        SettingsCanvas.NotifyRestartFinished();
        Destroy(gameObject);
    }

    private static void DestroyAllRuntimeObjects<T>() where T : Component
    {
        T[] instances = Resources.FindObjectsOfTypeAll<T>();

        for (int i = 0; i < instances.Length; i++)
        {
            T instance = instances[i];
            if (instance == null)
                continue;

            GameObject go = instance.gameObject;
            if (go == null)
                continue;

            // 只处理当前运行中的场景对象和 DontDestroyOnLoad 对象，不碰资源资产。
            if (!go.scene.IsValid())
                continue;

            Object.Destroy(go);
        }
    }
}
