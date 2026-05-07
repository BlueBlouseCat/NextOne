using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Scene0MainTouch : MonoBehaviour
{
    [SerializeField] Transform unopened;
    [SerializeField] Transform opened;
    [SerializeField] Transform text;
    [SerializeField] Transform photo;
    [SerializeField] private Transform detail;
    [SerializeField] private Transform detailText;
    [SerializeField] private Transform detailPhoto;

    [SerializeField] float detailDisplayDuration = 3f;

    private bool isBlocking;

    private void Start()
    {
        unopened.gameObject.SetActive(true);
        opened.gameObject.SetActive(false);
        text.gameObject.SetActive(false);
        photo.gameObject.SetActive(false);
        detailText.gameObject.SetActive(false);
        detailPhoto.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isBlocking)
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        if (EventSystem.current == null) return;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            Transform hit = result.gameObject.transform;

            if (hit.IsChildOf(unopened) || hit == unopened)
            {
                OnClickUnopened();
                return;
            }

            if (hit.IsChildOf(photo) || hit == photo)
            {
                OnClickPhoto();
                return;
            }

            if (hit.IsChildOf(text) || hit == text)
            {
                OnClickText();
                return;
            }
        }
    }

    private void OnClickUnopened()
    {
        unopened.gameObject.SetActive(false);
        opened.gameObject.SetActive(true);
        text.gameObject.SetActive(true);
        photo.gameObject.SetActive(true);
    }

    private void OnClickText()
    {
        text.gameObject.SetActive(false);
        StartCoroutine(ShowDetailAndWait(detailText, () => { SceneManager.LoadScene(SceneName.Scene0); }));
    }

    private void OnClickPhoto()
    {
        photo.gameObject.SetActive(false);
        StartCoroutine(ShowDetailAndWait(detailPhoto));
    }

    private IEnumerator ShowDetailAndWait(Transform detailObj, Action callback = null)
    {
        isBlocking = true;
        detailObj.gameObject.SetActive(true);

        yield return new WaitForSeconds(detailDisplayDuration);

        detailObj.gameObject.SetActive(false);
        isBlocking = false;
        callback?.Invoke();
    }
}