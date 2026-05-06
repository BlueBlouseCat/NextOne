using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance {get; private set;}

    public Transform CurrentPlayer {get; private set;}
    public string LastSpawnPointId { get; private set; }

    // 场景切换相关
    private bool _isSceneLoading;
    private string _nextSpawnPointId;
    [SerializeField] private SceneFader _sceneFader;
    private bool _isFadingSceneLoad;

    private readonly HashSet<string> _visitedScenes = new HashSet<string>(); // 是否第一次来到此场景
    private readonly HashSet<string> _flags = new HashSet<string>(); // 通用标记表

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_sceneFader == null)
            _sceneFader = FindObjectOfType<SceneFader>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void RegisterPlayer(Transform player)
    {
        CurrentPlayer = player;
    }

    /// <summary>
    /// 加载场景相关
    /// </summary>
    /// <returns></returns>
    public bool IsLoadingScene()
    {
        return _isSceneLoading;
    }

    public void LoadScene(string sceneName, string spawnPointId)
    {
        if(_isSceneLoading) return;
        if(string.IsNullOrEmpty(sceneName)) return;

        _isSceneLoading = true;
        _nextSpawnPointId = spawnPointId;
        SceneManager.LoadScene(sceneName);
    }

    public void LoadScene(string sceneName)
    {
        if (_isSceneLoading) return;
        if (string.IsNullOrEmpty(sceneName)) return;

        _isSceneLoading = true;
        _nextSpawnPointId = null;
        SceneManager.LoadScene(sceneName);
    }

    public void ReloadCurrentScene()
    {
        if(_isSceneLoading) return;

        _isSceneLoading = true;
        _nextSpawnPointId = null;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void LoadNextScene()
    {
        if (_isSceneLoading) return;

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex >= SceneManager.sceneCountInBuildSettings) return;

        _isSceneLoading = true;
        _nextSpawnPointId = null;
        SceneManager.LoadScene(nextIndex);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        CurrentPlayer = playerObject != null ? playerObject.transform : null;

        LastSpawnPointId = null;

        if (CurrentPlayer != null && !string.IsNullOrEmpty(_nextSpawnPointId))
        {
            SceneSpawnPoint[] spawnPoints = FindObjectsOfType<SceneSpawnPoint>();

            foreach (SceneSpawnPoint point in spawnPoints)
            {
                if (point.SpawnPointId == _nextSpawnPointId)
                {
                    CurrentPlayer.position = point.transform.position;
                    LastSpawnPointId = _nextSpawnPointId;
                    break;
                }
            }
        }

        _nextSpawnPointId = null;

        if (!_isFadingSceneLoad)
            _isSceneLoading = false;
    }

    /// <summary>
    /// 淡出场景
    /// </summary>
    /// <param name="sceneName"></param>
    /// <returns></returns>
    public void LoadSceneWithFade(string sceneName)
    {
        LoadSceneWithFade(sceneName, null);
    }

    public void LoadSceneWithFade(string sceneName, string spawnPointId)
    {
        if(_isSceneLoading) return;
        if(string.IsNullOrEmpty(sceneName)) return;

        if(_sceneFader == null)
        {
            if(string.IsNullOrEmpty(spawnPointId))
                LoadScene(sceneName);
            else
                LoadScene(sceneName, spawnPointId);
            return;
        }

        StartCoroutine(LoadSceneWithFadeRoutine(sceneName, spawnPointId));
    }

    private IEnumerator LoadSceneWithFadeRoutine(string sceneName, string spawnPointId)
    {
        _isSceneLoading = true;
        _isFadingSceneLoad = true;
        _nextSpawnPointId = spawnPointId;

        yield return _sceneFader.FadeOut();

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName);
        while(!loadOp.isDone)
            yield return null;

        yield return null;

        yield return _sceneFader.FadeIn();

        _isFadingSceneLoad = false;
        _isSceneLoading = false;
    }

    /// <summary>
    /// 标记是否第一次来到场景
    /// </summary>
    /// <param name="sceneName"></param>
    /// <returns></returns>
    public bool ConsumeFirstVisit(string sceneName)
    {
        if(string.IsNullOrEmpty(sceneName)) return false;

        if(_visitedScenes.Contains(sceneName)) return false;

        _visitedScenes.Add(sceneName);
        return true;
    }

    /// <summary>
    /// 设置标识相关
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool GetFlag(string key)
    {
        if(string.IsNullOrEmpty(key)) return false;
        return _flags.Contains(key);
    }

    public void SetFlag(string key, bool value)
    {
        if (string.IsNullOrEmpty(key)) return;

        if (value)
            _flags.Add(key);
        else
            _flags.Remove(key);
    }

    public bool HasFlag(string key)
    {
        return GetFlag(key);
    }
}
