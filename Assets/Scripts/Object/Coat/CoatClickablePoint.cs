using UnityEngine;

public class CoatClickablePoint : MonoBehaviour
{
    [SerializeField] private string _title;

    [TextArea(3, 8)]
    [SerializeField] private string _description;

    private bool _hasBeenViewed;

    public string Title => _title;
    public string Description => _description;
    public bool HasBeenViewed => _hasBeenViewed;

    public void MarkViewed()
    {
        _hasBeenViewed = true;
    }

    public void ResetViewedState()
    {
        _hasBeenViewed = false;
    }
}
