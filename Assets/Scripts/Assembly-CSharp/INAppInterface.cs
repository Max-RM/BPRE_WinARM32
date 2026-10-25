using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class INAppInterface : MonoBehaviour
{
    [SerializeField]
    private Canvas m_canvas;
    [SerializeField]
    private GameObject m_panel;
    [SerializeField]
    private GameObject m_main;
    [SerializeField]
    private GameObject m_actionBar;
    [SerializeField]
    private GameObject m_sidebar;
    [SerializeField]
    private UnityEngine.UI.Button m_enterButton;
    [SerializeField]
    private UnityEngine.UI.Button m_backButton;
    [SerializeField]
    private UnityEngine.UI.Button m_menuButton;
    [SerializeField]
    private UnityEngine.UI.Button m_hidePropertyPanelButton;
    [SerializeField]
    public GameObject ExecutorDummy;
    [SerializeField]
    private List<GameObject> m_elements;
    private bool m_enabled;
    private bool m_paused;
    public GameObject Main => m_main;
    public GameObject ActionBar => m_actionBar;
    public GameObject Sidebar => m_sidebar.gameObject;
    public static INAppInterface Instance { get; private set; }

    public event Action AppInterfaceEnabled;
    public event Action AppInterfaceDisabled;
    private void Awake()
    {
        Instance = this;
        UnityEngine.Object.DontDestroyOnLoad(this);
        Initialize();
    }

    private void Initialize()
    {
        m_enabled = false;
        m_enterButton.onClick.AddListener(OnEnterButtonClick);
        m_backButton.onClick.AddListener(OnBackButtonClick);
        m_menuButton.onClick.AddListener(OnMenuButtonClick);
        m_hidePropertyPanelButton.onClick.AddListener(HidePropertyPanel);
        string text = INLocalization.Instance.GetText("AppInterface_AppName");
        string text2 = INLocalization.Instance.GetText("AppInterface_Name");
        SetTitle(text + " " + text2);
        foreach (GameObject element in m_elements)
        {
            GameObject obj = UnityEngine.Object.Instantiate(element, m_main.transform, false);
            obj.name = element.name;
            obj.SetActive(value: false);
            RectTransform obj2 = (RectTransform)obj.transform;
            obj2.anchorMin = new Vector2(0f, 0f);
            obj2.anchorMax = new Vector2(1f, 1f);
            obj2.anchoredPosition = Vector2.zero;
            obj2.sizeDelta = Vector2.zero;
        }
    }

    private void HidePropertyPanel()
    {
        GameRules.ShowPropertyPanel = !GameRules.ShowPropertyPanel;
    }

    public GameObject FindElement(string name)
    {
        try
        {
            return m_main.transform.Find(name).gameObject;
        }
        catch (Exception e)
        {
            throw;
        }
    }

    public void SetTitle(string text)
    {
        m_actionBar.transform.Find("Text").GetComponent<Text>().text = text;
    }

    private void OnEnterButtonClick()
    {
        SetEnabled(isEnabled: true);
    }

    private void OnBackButtonClick()
    {
        SetEnabled(isEnabled: false);
    }

    private void OnMenuButtonClick()
    {
        AppInterfaceSidebar component = m_sidebar.GetComponent<AppInterfaceSidebar>();
        component.SetEnabled(!component.Enabled);
    }

    private void SetEnabled(bool isEnabled)
    {
        if (isEnabled == m_enabled)
        {
            return;
        }

        m_enabled = isEnabled;
        m_enterButton.gameObject.SetActive(!isEnabled);
        m_hidePropertyPanelButton.gameObject.SetActive(!isEnabled);
        m_panel.gameObject.SetActive(isEnabled);
        Singleton<GuiManager>.Instance.IsEnabled = !m_enabled;
        Singleton<GuiManager>.Instance.gameObject.SetActive(!m_enabled);
        if (m_enabled)
        {
            INContraption.Instance?.OnInterfaceEnabled();
            this.AppInterfaceEnabled?.Invoke();
            m_paused = GameTime.IsPaused();
            if (!m_paused)
            {
                GameTime.Pause(pause: true);
            }
        }
        else
        {
            INContraption.Instance?.OnInterfaceDisabled();
            this.AppInterfaceDisabled?.Invoke();
            if (!m_paused)
            {
                GameTime.Pause(pause: false);
            }

            m_paused = false;
        }
    }
}