using UnityEngine;
using System;

public class INVersionSelector : MonoBehaviour
{
    private int m_version;
    private Canvas m_canvas;
    private INVersionButton[] m_buttons;
    private bool m_entering;

    private void Awake()
    {
        m_version = -1;
        m_canvas = base.transform.Find("Canvas").GetComponent<Canvas>();
        m_buttons = GetComponentsInChildren<INVersionButton>(includeInactive: true);
        SetEnterButtonsActive(active: false);
        m_canvas.planeDistance = 9f;
        m_canvas.worldCamera = UnityEngine.Object.FindObjectOfType<Camera>();
        CanvasGroup component = base.transform.Find("Canvas").GetComponent<CanvasGroup>();
        StartCoroutine(component.PlayFadeInAnimation(0.5f));
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            if (Input.GetKeyDown((KeyCode)(49 + i)))
            {
                SelectVersion(i);
                EnterVersion();
                return;
            }
        }

        if (m_version != -1 && Input.GetKeyDown(KeyCode.Return))
        {
            EnterVersion();
            return;
        }
    }

    public void SelectVersion(int version)
    {
        m_version = version;
        SetEnterButtonsActive(active: true);
    }

    public void EnterVersion()
    {
        if (m_entering || m_version == -1)
        {
            return;
        }

        m_entering = true;
        SetButtonsInteractable(interactable: false);
        try
        {
            INSettings.Initialize(m_version);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            m_entering = false;
            SetButtonsInteractable(interactable: true);
        }
    }

    private void SetEnterButtonsActive(bool active)
    {
        foreach (INVersionButton button in m_buttons)
        {
            if (button.Type == -1 && button.gameObject.activeSelf != active)
            {
                button.gameObject.SetActive(active);
            }
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        foreach (INVersionButton button in m_buttons)
        {
            button.interactable = interactable;
        }
    }
}
