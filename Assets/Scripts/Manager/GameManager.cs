using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public Transform CurrentPlayer { get; private set; }
    public string LastSpawnPointId { get; private set; }

    private bool _isSceneLoading;
    private string _nextSpawnPointId;
    [SerializeField] private SceneFader _sceneFader;
    private bool _isFadingSceneLoad;

    private readonly HashSet<string> _visitedScenes = new HashSet<string>();
    private readonly HashSet<string> _flags = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
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

    public bool IsLoadingScene()
    {
        return _isSceneLoading;
    }

    public void LoadScene(string sceneName, string spawnPointId)
    {
        if (_isSceneLoading) return;
        if (string.IsNullOrEmpty(sceneName)) return;

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
        if (_isSceneLoading) return;

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
        SceneSpawnPoint matchedSpawnPoint = null;

        if (CurrentPlayer != null && !string.IsNullOrEmpty(_nextSpawnPointId))
        {
            SceneSpawnPoint[] spawnPoints = FindObjectsOfType<SceneSpawnPoint>();

            foreach (SceneSpawnPoint point in spawnPoints)
            {
                if (point == null) continue;
                if (point.SpawnPointId != _nextSpawnPointId) continue;

                CurrentPlayer.position = point.transform.position;
                LastSpawnPointId = _nextSpawnPointId;
                matchedSpawnPoint = point;
                break;
            }
        }

        ApplyPlayerFacingForSceneEntry(matchedSpawnPoint);

        _nextSpawnPointId = null;

        if (!_isFadingSceneLoad)
            _isSceneLoading = false;
    }

    private void ApplyPlayerFacingForSceneEntry(SceneSpawnPoint spawnPoint)
    {
        if (CurrentPlayer == null) return;

        PlayerMovement playerMovement = CurrentPlayer.GetComponent<PlayerMovement>();
        if (playerMovement == null)
            playerMovement = CurrentPlayer.GetComponentInChildren<PlayerMovement>();
        if (playerMovement == null)
            playerMovement = CurrentPlayer.GetComponentInParent<PlayerMovement>();
        if (playerMovement == null)
            return;

        if (spawnPoint != null)
            playerMovement.ApplyFacingFromSceneSpawnPoint(spawnPoint);
        else
            playerMovement.ApplyDefaultFacingForSceneEntry();
    }

    public void LoadSceneWithFade(string sceneName)
    {
        LoadSceneWithFade(sceneName, null);
    }

    public void LoadSceneWithFade(string sceneName, string spawnPointId)
    {
        if (_isSceneLoading) return;
        if (string.IsNullOrEmpty(sceneName)) return;

        if (_sceneFader == null)
        {
            if (string.IsNullOrEmpty(spawnPointId))
                LoadScene(sceneName);
            else
                LoadScene(sceneName, spawnPointId);
            return;
        }

        StartCoroutine(LoadSceneWithFadeRoutine(sceneName, spawnPointId, string.Empty));
    }

    public void LoadSceneWithFadeMessage(string sceneName, string transitionMessage)
    {
        LoadSceneWithFadeMessage(sceneName, null, transitionMessage);
    }

    public void LoadSceneWithFadeMessage(string sceneName, string spawnPointId, string transitionMessage)
    {
        if (_isSceneLoading) return;
        if (string.IsNullOrEmpty(sceneName)) return;

        if (_sceneFader == null)
        {
            if (string.IsNullOrEmpty(spawnPointId))
                LoadScene(sceneName);
            else
                LoadScene(sceneName, spawnPointId);
            return;
        }

        StartCoroutine(LoadSceneWithFadeRoutine(sceneName, spawnPointId, transitionMessage));
    }

    private IEnumerator LoadSceneWithFadeRoutine(string sceneName, string spawnPointId, string transitionMessage)
    {
        _isSceneLoading = true;
        _isFadingSceneLoad = true;
        _nextSpawnPointId = spawnPointId;

        yield return _sceneFader.FadeOut(transitionMessage);
        yield return _sceneFader.HoldBeforeSceneLoad();

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName);
        while (!loadOp.isDone)
            yield return null;

        yield return null;
        yield return _sceneFader.HoldAfterSceneLoad();
        yield return _sceneFader.FadeIn(true);

        _isFadingSceneLoad = false;
        _isSceneLoading = false;
    }

    public bool ConsumeFirstVisit(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;

        if (_visitedScenes.Contains(sceneName)) return false;

        _visitedScenes.Add(sceneName);
        return true;
    }

    public bool GetFlag(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
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
