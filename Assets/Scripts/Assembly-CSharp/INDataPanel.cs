using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class INDataPanel : MonoBehaviour
{
    private class DataGroup
    {
        public GameObject GameObject { get; private set; }
        public Text GroupName { get; private set; }
        public UITextLocale GroupNameLocale { get; private set; }
        public List<DataElement> Elements { get; private set; }

        public DataGroup(GameObject gameObject) : this(gameObject, new List<DataElement>())
        {
        }

        public DataGroup(GameObject gameObject, List<DataElement> elements)
        {
            GameObject = gameObject;
            GroupName = gameObject.transform.Find("GroupName").GetComponent<Text>();
            GroupNameLocale = GroupName.GetComponent<UITextLocale>();
            Elements = elements;
        }
    }

    private class DataElement
    {
        public GameObject GameObject { get; private set; }
        public Text Name { get; private set; }
        public UITextLocale NameLocale { get; private set; }
        public Text Value { get; private set; }
        public Func<string> Function { get; private set; }

        public DataElement(GameObject gameObject, Func<string> function)
        {
            GameObject = gameObject;
            Name = gameObject.transform.Find("Name").GetComponent<Text>();
            NameLocale = Name.GetComponent<UITextLocale>();
            Value = gameObject.transform.Find("Value").GetComponent<Text>();
            Function = function;
        }

        public void UpdateValue()
        {
            if (Function != null)
            {
                Value.text = Function();
            }
        }
    }

    [SerializeField]
    private GameObject m_content;
    [SerializeField]
    private GameObject m_dataElementPrefab;
    [SerializeField]
    private GameObject m_dataGroupPrefab;
    private List<DataGroup> m_dataGroups;
    private INDataDetector m_detector;
    public static INDataPanel Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        m_dataGroups = new List<DataGroup>();
        m_detector = UnityEngine.Object.Instantiate(INUnity.LoadGameObject("INDataDetector")).GetComponent<INDataDetector>();
        DataGroup dataGroup = GenerateDataGroup(0, "DataPanel_TimeData");
        GenerateDataElement(dataGroup, 0, "DataPanel_Time", () => DateTime.Now.ToString("s"));
        GenerateDataElement(dataGroup, 1, "DataPanel_RunningTime", () => Time.realtimeSinceStartup.ToString("F2"));
        DataGroup dataGroup2 = GenerateDataGroup(1, "DataPanel_PerformanceData");
        GenerateDataElement(dataGroup2, 0, "DataPanel_FPS", () => m_detector.FPS.ToString("F2"));
        GenerateDataElement(dataGroup2, 1, "DataPanel_FixedFPS", () => m_detector.FixedFPS.ToString("F2"));
        GenerateDataElement(dataGroup2, 2, "DataPanel_AllocatedManagedHeapSize", () => m_detector.AllocatedManagedHeapSize.ToString("F2") + " MB");
        GenerateDataElement(dataGroup2, 3, "DataPanel_ReservedManagedHeapSize", () => m_detector.ReservedManagedHeapSize.ToString("F2") + " MB");
        GenerateDataElement(dataGroup2, 4, "DataPanel_TotalAllocatedMemorySize", () => m_detector.TotalAllocatedMemorySize.ToString("F2") + " MB");
        GenerateDataElement(dataGroup2, 5, "DataPanel_TotalReservedMemorySize", () => m_detector.TotalReservedMemorySize.ToString("F2") + " MB");
        DataGroup dataGroup3 = GenerateDataGroup(2, "DataPanel_DeviceData");
        GenerateDataElement(dataGroup3, 0, "DataPanel_DeviceModel", () => SystemInfo.deviceModel);
        GenerateDataElement(dataGroup3, 1, "DataPanel_DeviceName", () => SystemInfo.deviceName);
        GenerateDataElement(dataGroup3, 2, "DataPanel_DeviceType", () => SystemInfo.deviceType.ToString());
        GenerateDataElement(dataGroup3, 3, "DataPanel_OperatingSystem", () => SystemInfo.operatingSystem);
        GenerateDataElement(dataGroup3, 4, "DataPanel_ProcessorType", () => SystemInfo.processorType);
        GenerateDataElement(dataGroup3, 5, "DataPanel_ProcessorFrequency", () => SystemInfo.processorFrequency + " MHz");
        GenerateDataElement(dataGroup3, 6, "DataPanel_ProcessorCount", () => SystemInfo.processorCount.ToString());
        GenerateDataElement(dataGroup3, 7, "DataPanel_GraphicsDeviceName", () => SystemInfo.graphicsDeviceName);
        GenerateDataElement(dataGroup3, 8, "DataPanel_GraphicsDeviceType", () => SystemInfo.graphicsDeviceType.ToString());
        GenerateDataElement(dataGroup3, 9, "DataPanel_GraphicsMemorySize", () => SystemInfo.graphicsMemorySize + " MB");
        GenerateDataElement(dataGroup3, 10, "DataPanel_SystemMemorySize", () => SystemInfo.systemMemorySize + " MB");
        GenerateDataElement(dataGroup3, 11, "DataPanel_BatteryLevel", () => SystemInfo.batteryLevel.ToString("P"));
        GenerateDataElement(dataGroup3, 12, "DataPanel_BatteryStatus", () => SystemInfo.batteryStatus.ToString());
        SetLayout();
    }

    private DataGroup GenerateDataGroup(int index, string groupName)
    {
        GameObject obj = UnityEngine.Object.Instantiate(m_dataGroupPrefab, m_content.transform, false);
        obj.SetActive(value: true);
        obj.name = "DataGroup_" + index;
        DataGroup dataGroup = new DataGroup(obj);
        dataGroup.GroupNameLocale.ID = groupName;
        dataGroup.GroupNameLocale.UpdateText();
        m_dataGroups.Add(dataGroup);
        return dataGroup;
    }

    private DataElement GenerateDataElement(DataGroup dataGroup, int index, string name, Func<string> function)
    {
        GameObject obj = UnityEngine.Object.Instantiate(m_dataElementPrefab, dataGroup.GameObject.transform, false);
        obj.SetActive(value: true);
        obj.name = "DataElement_" + index;
        DataElement dataElement = new DataElement(obj, function);
        dataElement.NameLocale.ID = name;
        dataElement.NameLocale.UpdateText();
        dataElement.Value.text = function();
        dataGroup.Elements.Add(dataElement);
        return dataElement;
    }

    private void SetLayout()
    {
        float num = 100f;
        foreach (DataGroup dataGroup in m_dataGroups)
        {
            float num2 = 0f;
            RectTransform obj = (RectTransform)dataGroup.GameObject.transform;
            obj.anchoredPosition = new Vector2(obj.anchoredPosition.x, 0f - num);
            num += 100f;
            num2 += 100f;
            foreach (DataElement element in dataGroup.Elements)
            {
                RectTransform obj2 = (RectTransform)element.GameObject.transform;
                obj2.anchoredPosition = new Vector2(obj2.anchoredPosition.x, 0f - num2);
                num += 100f;
                num2 += 100f;
            }
        }

        RectTransform rectTransform = (RectTransform)m_content.transform;
        Vector2 sizeDelta = new Vector2(rectTransform.sizeDelta.x, num);
        rectTransform.sizeDelta = sizeDelta;
    }

    private void Update()
    {
        foreach (DataGroup dataGroup in m_dataGroups)
        {
            foreach (DataElement element in dataGroup.Elements)
            {
                element.UpdateValue();
            }
        }
    }
}