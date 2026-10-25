using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AppInterfaceSidebar : MonoBehaviour
{
    [Serializable]
    public class SidebarElementData
    {
        public string Name;
        public GameObject RelatedObject;
    }

    [SerializeField]
    private List<SidebarElementData> m_sidebarElements;
    [SerializeField]
    private GameObject m_sidebarElementPrefab;
    public bool Enabled { get; private set; }

    private void Awake()
    {
        Enabled = true;
        GenerateSidebarElements();
    }

    private void GenerateSidebarElements()
    {
        GameObject sidebarElementPrefab = m_sidebarElementPrefab;
        int num = 0;
        foreach (SidebarElementData sidebarElement in m_sidebarElements)
        {
            GameObject obj = UnityEngine.Object.Instantiate(sidebarElementPrefab, base.transform, false);
            obj.SetActive(value: true);
            obj.name = "SidebarElement_" + (num + 1);
            RectTransform obj2 = (RectTransform)obj.transform;
            obj2.anchoredPosition = new Vector2(0f, -50f * (float)(2 * num + 1));
            obj2.sizeDelta = new Vector2(0f, 100f);
            obj.GetComponent<AppInterfaceSidebarElement>().Initialize(sidebarElement.Name, sidebarElement.RelatedObject);
            num++;
        }
    }

    public void SetEnabled(bool enabled)
    {
        if (Enabled ^ enabled)
        {
            Enabled = enabled;
            CanvasGroup component = GetComponent<CanvasGroup>();
            component.alpha = (enabled ? 1f : 0f);
            component.blocksRaycasts = enabled;
        }
    }

    public void SetElementEnabled(AppInterfaceSidebarElement element, bool enabled)
    {
        if (!GetComponent<ToggleGroup>().AnyTogglesOn())
        {
            string text = INLocalization.Instance.GetText("AppInterface_AppName");
            string text2 = INLocalization.Instance.GetText("AppInterface_Name");
            INAppInterface.Instance.SetTitle(text + " " + text2);
        }

        element.SetEnabled(enabled);
    }
}