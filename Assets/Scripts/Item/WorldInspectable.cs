using UnityEngine;

public class WorldInspectable : MonoBehaviour
{
    [SerializeField] private string _title;
    [TextArea(3, 6)]
    [SerializeField] private string _description;

    public string Title => _title;
    public string Description => _description;
}
