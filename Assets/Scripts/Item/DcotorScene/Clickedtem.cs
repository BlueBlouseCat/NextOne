using UnityEngine;
using UnityEngine.EventSystems;

public class Clickedtem : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        gameObject.SetActive(false);
    }
}
