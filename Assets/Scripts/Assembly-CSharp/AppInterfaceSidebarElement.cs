using UnityEngine;
using UnityEngine.UI;

public class AppInterfaceSidebarElement : MonoBehaviour
{
    private AppInterfaceSidebar m_sidebar;
    private Toggle m_toggle;
    private Text m_text;
    private bool m_enabled;
    private GameObject m_relatedObject;
    public void Initialize(string id, GameObject relatedObject)
    {
        m_sidebar = base.transform.parent.GetComponent<AppInterfaceSidebar>();
        m_text = base.transform.Find("Text").GetComponent<Text>();
        m_toggle = GetComponent<Toggle>();
        m_toggle.onValueChanged.AddListener(OnValueChanged);
        m_toggle.group = m_sidebar.GetComponent<ToggleGroup>();
        UITextLocale component = m_text.GetComponent<UITextLocale>();
        component.ID = id;
        component.UpdateText();
        if (relatedObject != null)
        {
            m_relatedObject = INAppInterface.Instance.FindElement(relatedObject.name);
        }
    }

    private void OnValueChanged(bool enabled)
    {
        m_sidebar.SetElementEnabled(this, enabled);
    }

    public void SetEnabled(bool enabled)
    {
        if (m_enabled != enabled)
        {
            m_enabled = enabled;
            if (enabled)
            {
                string text = INLocalization.Instance.GetText("AppInterface_AppName");
                INAppInterface.Instance.SetTitle(text + " " + m_text.text);
            }

            if (m_relatedObject != null)
            {
                m_relatedObject.SetActive(enabled);
            }
        }
    }
}