using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class INSettingsInterface : MonoBehaviour
{
    private class SettingsGroup
    {
        public GameObject GameObject { get; private set; }
        public Text GroupName { get; private set; }
        public UITextLocale GroupNameLocale { get; private set; }
        public List<SettingsElement> Elements { get; private set; }

        public SettingsGroup(GameObject gameObject) : this(gameObject, new List<SettingsElement>())
        {
        }

        public SettingsGroup(GameObject gameObject, List<SettingsElement> elements)
        {
            GameObject = gameObject;
            GroupName = gameObject.transform.Find("GroupName").GetComponent<Text>();
            GroupNameLocale = GroupName.GetComponent<UITextLocale>();
            Elements = elements;
        }

        public void UpdateValues()
        {
            foreach (SettingsElement element in Elements)
            {
                element.UpdateValue();
            }
        }
    }

    private class SettingsElement
    {
        public GameObject GameObject { get; private set; }
        public Text Name { get; private set; }
        public UITextLocale NameLocale { get; private set; }
        public Toggle Toggle { get; private set; }
        public InputField InputField { get; private set; }

        public SettingsElement(GameObject gameObject)
        {
            GameObject = gameObject;
            Name = gameObject.transform.Find("Name").GetComponent<Text>();
            NameLocale = Name.GetComponent<UITextLocale>();
            Toggle = gameObject.transform.Find("Toggle").GetComponent<Toggle>();
            InputField = gameObject.transform.Find("InputField").GetComponent<InputField>();
        }

        public virtual void UpdateValue()
        {
        }
    }

    private class SettingsElement<T> : SettingsElement
    {
        public Func<T> Getter { get; private set; }
        public Action<T> Setter { get; private set; }

        public SettingsElement(GameObject gameObject, Func<T> getter, Action<T> setter) : base(gameObject)
        {
            Getter = getter;
            Setter = setter;
        }
    }

    private class SettingsElementToggle : SettingsElement<bool>
    {
        public SettingsElementToggle(GameObject gameObject, Func<bool> getter, Action<bool> setter) : base(gameObject, getter, setter)
        {
            base.Toggle.gameObject.SetActive(value: true);
            base.InputField.gameObject.SetActive(value: false);
            base.Toggle.isOn = getter();
            if (base.Setter != null)
            {
                base.Toggle.onValueChanged.AddListener(OnValueChanged);
            }
        }

        public override void UpdateValue()
        {
            base.Toggle.isOn = base.Getter();
        }

        private void OnValueChanged(bool value)
        {
            try
            {
                base.Setter(value);
                Instance.IsChanged = true;
            }
            catch
            {
            }

            UpdateValue();
        }
    }

    private class SettingsElementInputField<T> : SettingsElement<T>
    {
        public Func<string, T> Converter { get; private set; }

        public SettingsElementInputField(GameObject gameObject, Func<T> getter, Action<T> setter, Func<string, T> converter) : base(gameObject, getter, setter)
        {
            base.Toggle.gameObject.SetActive(value: false);
            base.InputField.gameObject.SetActive(value: true);
            base.InputField.text = getter().ToString();
            base.InputField.onEndEdit.AddListener(OnEndEdit);
            Converter = converter;
        }

        public override void UpdateValue()
        {
            base.InputField.text = base.Getter().ToString();
        }

        private void OnEndEdit(string text)
        {
            try
            {
                T val = base.Getter();
                base.Setter(Converter(text));
                if (!val.Equals(base.Getter()))
                {
                    Instance.IsChanged = true;
                }
            }
            catch
            {
            }

            UpdateValue();
        }
    }

    [SerializeField]
    private GameObject m_content;
    [SerializeField]
    private GameObject m_settingsElementPrefab;
    [SerializeField]
    private GameObject m_settingsGroupPrefab;
    [SerializeField]
    private UnityEngine.UI.Button m_saveButton;
    [SerializeField]
    private UnityEngine.UI.Button m_resetButton;
    private List<SettingsGroup> m_settingsGroups;
    public bool IsChanged { get; private set; }
    public static INSettingsInterface Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        m_saveButton.onClick.AddListener(Save);
        m_resetButton.onClick.AddListener(Reset);
        m_settingsGroups = new List<SettingsGroup>();
        Func<string, int> converter = (string text) => int.Parse(text);
        Func<string, float> converter2 = (string text) => float.Parse(text);
        SettingsGroup settingsGroup = GenerateSettingsGroup(0, "PhysicsSettings_Name");
        GenerateSettingsElementInputField(settingsGroup, 0, "PhysicsSettings_GravityX", () => INUserSettings.Instance.PhysicsSettings.GravityX, delegate (float value)
        {
            INUserSettings.Instance.PhysicsSettings.SetGravityX(value);
        }, converter2);
        GenerateSettingsElementInputField(settingsGroup, 1, "PhysicsSettings_GravityY", () => INUserSettings.Instance.PhysicsSettings.GravityY, delegate (float value)
        {
            INUserSettings.Instance.PhysicsSettings.SetGravityY(value);
        }, converter2);
        GenerateSettingsElementToggle(settingsGroup, 2, "PhysicsSettings_NoDrag", () => INUserSettings.Instance.PhysicsSettings.NoDrag, delegate (bool value)
        {
            INUserSettings.Instance.PhysicsSettings.SetNoDrag(value);
        });
        GenerateSettingsElementInputField(settingsGroup, 3, "PhysicsSettings_TimeScale", () => INUserSettings.Instance.PhysicsSettings.TimeScale, delegate (float value)
        {
            INUserSettings.Instance.PhysicsSettings.SetTimeScale(value);
        }, converter2);
        SettingsGroup settingsGroup2 = GenerateSettingsGroup(1, "ButtonSettings_Name");
        GenerateSettingsElementToggle(settingsGroup2, 0, "ButtonSettings_Enabled", () => INUserSettings.Instance.ButtonSettings.Enabled, delegate (bool value)
        {
            INUserSettings.Instance.ButtonSettings.Enabled = value;
        });
        GenerateSettingsElementInputField(settingsGroup2, 1, "ButtonSettings_ButtonScale", () => INUserSettings.Instance.ButtonSettings.ButtonScale, delegate (float value)
        {
            INUserSettings.Instance.ButtonSettings.ButtonScale = value;
        }, converter2);
        GenerateSettingsElementInputField(settingsGroup2, 2, "ButtonSettings_ScrollViewHeightScale", () => INUserSettings.Instance.ButtonSettings.ScrollViewHeightScale, delegate (float value)
        {
            INUserSettings.Instance.ButtonSettings.ScrollViewHeightScale = value;
        }, converter2);
        GenerateSettingsElementInputField(settingsGroup2, 3, "ButtonSettings_MaxSeparationCount", () => INUserSettings.Instance.ButtonSettings.MaxSeparationCount, delegate (int value)
        {
            INUserSettings.Instance.ButtonSettings.MaxSeparationCount = Math.Max(value, 0);
        }, converter);
        GenerateSettingsElementToggle(settingsGroup2, 4, "ButtonSettings_DisplayButtonIndex", () => INUserSettings.Instance.ButtonSettings.DisplayButtonIndex, delegate (bool value)
        {
            INUserSettings.Instance.ButtonSettings.DisplayButtonIndex = value;
        });
        SettingsGroup settingsGroup3 = GenerateSettingsGroup(2, "ContraptionDataSettings_Name");
        GenerateSettingsElementToggle(settingsGroup3, 0, "ContraptionDataSettings_Enabled", () => INUserSettings.Instance.ContraptionDataSettings.Enabled, delegate (bool value)
        {
            INUserSettings.Instance.ContraptionDataSettings.Enabled = value;
        });
        GenerateSettingsElementInputField(settingsGroup3, 1, "ContraptionDataSettings_LoadFormat", () => INUserSettings.Instance.ContraptionDataSettings.LoadFormat, delegate (ContraptionDataSettings.SerializationFormat value)
        {
            INUserSettings.Instance.ContraptionDataSettings.LoadFormat = value;
        }, (string text) => text.ToEnum<ContraptionDataSettings.SerializationFormat>());
        GenerateSettingsElementInputField(settingsGroup3, 2, "ContraptionDataSettings_SaveFormat", () => INUserSettings.Instance.ContraptionDataSettings.SaveFormat, delegate (ContraptionDataSettings.SerializationFormat value)
        {
            INUserSettings.Instance.ContraptionDataSettings.SaveFormat = value;
        }, (string text) => text.ToEnum<ContraptionDataSettings.SerializationFormat>());
        GenerateSettingsElementToggle(settingsGroup3, 3, "ContraptionDataSettings_BackupData", () => INUserSettings.Instance.ContraptionDataSettings.BackupData, delegate (bool value)
        {
            INUserSettings.Instance.ContraptionDataSettings.BackupData = value;
        });
        GenerateSettingsElementToggle(settingsGroup3, 4, "ContraptionDataSettings_BackupOriginalData", () => INUserSettings.Instance.ContraptionDataSettings.BackupOriginalData, delegate (bool value)
        {
            INUserSettings.Instance.ContraptionDataSettings.BackupOriginalData = value;
        });
        GenerateSettingsElementToggle(settingsGroup3, 5, "ContraptionDataSettings_SaveAsOriginalData", () => INUserSettings.Instance.ContraptionDataSettings.SaveAsOriginalData, delegate (bool value)
        {
            INUserSettings.Instance.ContraptionDataSettings.SaveAsOriginalData = value;
        });
        SetLayout();
    }

    private SettingsGroup GenerateSettingsGroup(int index, string groupName)
    {
        GameObject obj = UnityEngine.Object.Instantiate(m_settingsGroupPrefab, m_content.transform, false);
        obj.SetActive(value: true);
        obj.name = "SettingsGroup_" + index;
        SettingsGroup settingsGroup = new SettingsGroup(obj);
        settingsGroup.GroupNameLocale.ID = groupName;
        settingsGroup.GroupNameLocale.UpdateText();
        m_settingsGroups.Add(settingsGroup);
        return settingsGroup;
    }

    private GameObject GenerateSettingsElementGameObject(SettingsGroup dataGroup, int index)
    {
        GameObject obj = UnityEngine.Object.Instantiate(m_settingsElementPrefab, dataGroup.GameObject.transform, false);
        obj.SetActive(value: true);
        obj.name = "SettingsElement_" + index;
        return obj;
    }

    private SettingsElementToggle GenerateSettingsElementToggle(SettingsGroup settingsGroup, int index, string name, Func<bool> getFunction, Action<bool> setFunction)
    {
        SettingsElementToggle settingsElementToggle = new SettingsElementToggle(GenerateSettingsElementGameObject(settingsGroup, index), getFunction, setFunction);
        settingsElementToggle.NameLocale.ID = name;
        settingsElementToggle.NameLocale.UpdateText();
        settingsGroup.Elements.Add(settingsElementToggle);
        return settingsElementToggle;
    }

    private SettingsElementInputField<T> GenerateSettingsElementInputField<T>(SettingsGroup settingsGroup, int index, string name, Func<T> getFunction, Action<T> setFunction, Func<string, T> converter)
    {
        SettingsElementInputField<T> settingsElementInputField = new SettingsElementInputField<T>(GenerateSettingsElementGameObject(settingsGroup, index), getFunction, setFunction, converter);
        settingsElementInputField.NameLocale.ID = name;
        settingsElementInputField.NameLocale.UpdateText();
        settingsGroup.Elements.Add(settingsElementInputField);
        return settingsElementInputField;
    }

    private void SetLayout()
    {
        float num = 100f;
        foreach (SettingsGroup settingsGroup in m_settingsGroups)
        {
            float num2 = 0f;
            RectTransform obj = (RectTransform)settingsGroup.GameObject.transform;
            obj.anchoredPosition = new Vector2(obj.anchoredPosition.x, 0f - num);
            num += 100f;
            num2 += 100f;
            foreach (SettingsElement element in settingsGroup.Elements)
            {
                RectTransform obj2 = (RectTransform)element.GameObject.transform;
                obj2.anchoredPosition = new Vector2(obj2.anchoredPosition.x, 0f - num2);
                num += 100f;
                num2 += 100f;
            }
        }

        RectTransform obj3 = (RectTransform)m_saveButton.transform;
        obj3.anchoredPosition = new Vector2(obj3.anchoredPosition.x, 0f - num);
        RectTransform obj4 = (RectTransform)m_resetButton.transform;
        obj4.anchoredPosition = new Vector2(obj4.anchoredPosition.x, 0f - num);
        num += 100f;
        RectTransform rectTransform = (RectTransform)m_content.transform;
        Vector2 sizeDelta = new Vector2(rectTransform.sizeDelta.x, num);
        rectTransform.sizeDelta = sizeDelta;
    }

    private void Save()
    {
        INUserSettings.Save();
        IsChanged = false;
        UpdateValues();
    }

    private void Reset()
    {
        INUserSettings.Reset();
        IsChanged = true;
        UpdateValues();
    }

    private void Update()
    {
        Text component = m_saveButton.transform.Find("Text").GetComponent<Text>();
        string text = component.GetComponent<UITextLocale>().Text;
        component.text = (IsChanged ? (text + "*") : text);
    }

    private void UpdateValues()
    {
        foreach (SettingsGroup settingsGroup in m_settingsGroups)
        {
            settingsGroup.UpdateValues();
        }
    }
}