using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class BgmPlayer : MonoBehaviour
{
    public static BgmPlayer Instance { get; private set; }

    [SerializeField] private AudioSource _audioSource;

    private AudioClip _currentClip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;

        GameObject go = new GameObject("[BgmPlayer]");
        DontDestroyOnLoad(go);
        go.AddComponent<BgmPlayer>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();

        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.playOnAwake = false;
        _audioSource.loop = true;
        _audioSource.spatialBlend = 0f;
    }

    public void Play(AudioClip clip, float volume = 1f, bool loop = true)
    {
        if (clip == null)
        {
            Stop();
            return;
        }

        if (_currentClip == clip && _audioSource.isPlaying)
        {
            _audioSource.volume = volume;
            _audioSource.loop = loop;
            return;
        }

        _currentClip = clip;
        _audioSource.clip = clip;
        _audioSource.volume = volume;
        _audioSource.loop = loop;
        _audioSource.Play();
    }

    public void Stop()
    {
        _currentClip = null;
        _audioSource.Stop();
        _audioSource.clip = null;
    }
}
