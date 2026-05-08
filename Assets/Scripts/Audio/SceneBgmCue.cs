using UnityEngine;

public class SceneBgmCue : MonoBehaviour
{
    [SerializeField] private AudioClip _clip;
    [SerializeField, Range(0f, 1f)] private float _volume = 1f;
    [SerializeField] private bool _loop = true;

    private void Start()
    {
        if (BgmPlayer.Instance == null)
        {
            Debug.LogWarning("BgmPlayer not found in scene.");
            return;
        }

        BgmPlayer.Instance.Play(_clip, _volume, _loop);
    }
}
