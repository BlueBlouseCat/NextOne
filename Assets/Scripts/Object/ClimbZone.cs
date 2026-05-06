using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ClimbZone : MonoBehaviour
{
    [Header("Optional")]
    [SerializeField] private bool _forceTrigger = true;
    [SerializeField] private string _requiredTag = "Climbable";

    private void Reset()
    {
        ApplySetup();
    }

    private void Awake()
    {
        ApplySetup();
    }

    private void ApplySetup()
    {
        gameObject.tag = _requiredTag;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null && _forceTrigger)
            col.isTrigger = true;
    }
}
