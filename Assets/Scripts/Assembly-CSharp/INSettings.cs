using System;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class INSettings
{
    private enum SettingScope
    {
        None = 0,
        Level = 1,
        Sandbox = 2,
        Global = 3
    }

    private enum SettingTypeCode
    {
        Empty = 0,
        Boolean = 1,
        Int32 = 2,
        Single = 3,
        String = 4,
        Array = 5,
        RandomElementForDifferentHash = 87654649
    }

    private class SettingType
    {
        public SettingTypeCode MainType { get; }
        public SettingTypeCode[] GenericArguments { get; }
        public bool IsGeneric => GenericArguments != null;

        public SettingType(SettingTypeCode mainType, SettingTypeCode[] genericArguments)
        {
            MainType = mainType;
            GenericArguments = genericArguments;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(MainType.ToString());
            if (GenericArguments != null && GenericArguments.Length != 0)
            {
                stringBuilder.Append('<');
                stringBuilder.Append(GenericArguments[0].ToString());
                for (int i = 1; i < GenericArguments.Length; i++)
                {
                    stringBuilder.Append(", ");
                    stringBuilder.Append(GenericArguments[i].ToString());
                }

                stringBuilder.Append('>');
            }

            return stringBuilder.ToString();
        }

        public static SettingType Parse(string s)
        {
            SettingTypeCode[] array = null;
            int num = s.IndexOf('<');
            int num2 = s.IndexOf('>');
            SettingTypeCode mainType;
            if (num != -1 && num2 != -1)
            {
                mainType = s.Substring(0, num).ToEnum<SettingTypeCode>();
                string[] array2 = s.Substring(num + 1, num2 - num - 1).Split(',');
                array = new SettingTypeCode[array2.Length];
                for (int i = 0; i < array2.Length; i++)
                {
                    array[i] = array2[i].ToEnum<SettingTypeCode>();
                }
            }
            else
            {
                mainType = s.ToEnum<SettingTypeCode>();
            }

            return new SettingType(mainType, array);
        }
    }

    private class SettingDeclaration
    {
        public SettingType Type { get; private set; }
        public Variant InitialValue { get; private set; }

        public SettingDeclaration(SettingType type, Variant initialValue)
        {
            Type = type;
            InitialValue = initialValue;
        }
    }

    private class SettingDeclarationContainer
    {
        private SettingDeclaration[] m_items;
        public int Count => m_items.Length;

        public SettingDeclarationContainer(int count)
        {
            m_items = new SettingDeclaration[count];
        }

        public SettingDeclaration GetDeclaration(INFeature name)
        {
            return m_items[(int)name];
        }

        public void SetDeclaration(INFeature name, SettingDeclaration data)
        {
            m_items[(int)name] = data;
        }
    }

    private class SettingData
    {
        public SettingScope Scope { get; set; }
        public Variant Value { get; set; }

        public SettingData()
        {
            Scope = SettingScope.None;
            Value = null;
        }

        public SettingData(SettingScope scope, Variant value)
        {
            Scope = scope;
            Value = value;
        }
    }

    private class SettingDataContainer
    {
        private SettingData[] m_items;
        public int Count => m_items.Length;

        public SettingDataContainer(int count)
        {
            m_items = new SettingData[count];
        }

        public SettingDataContainer(SettingDataContainer settings)
        {
            int num = settings.m_items.Length;
            m_items = new SettingData[num];
            Array.Copy(settings.m_items, m_items, num);
        }

        public SettingData GetData(INFeature name)
        {
            return m_items[(int)name];
        }

        public void SetData(INFeature name, SettingData data)
        {
            m_items[(int)name] = data;
        }
    }

    public class WrappedSettingData
    {
        private readonly SettingDeclaration m_declaration;
        private readonly SettingData m_data;
        private WrappedSettingData(SettingDeclaration declaration, SettingData data)
        {
            m_declaration = declaration;
            m_data = data;
        }

        public static WrappedSettingData Create(INFeature name)
        {
            return new WrappedSettingData(s_declarations.GetDeclaration(name), s_runtimeSettings.GetData(name));
        }

        public void AlterValue(INFeature inFeature, object newValue)
        {
            Variant variant = GetVariant();
        }

        public Variant GetVariant()
        {
            if (!IsEnabled(m_data.Scope))
            {
                return m_declaration.InitialValue;
            }

            return m_data.Value;
        }

        public bool GetBool()
        {
            return (bool)GetVariant().Value;
        }

        public int GetInt()
        {
            return (int)GetVariant().Value;
        }

        public float GetFloat()
        {
            return (float)GetVariant().Value;
        }

        public string GetString()
        {
            return (string)GetVariant().Reference.ToObject();
        }

        public object GetObject()
        {
            return GetVariant().Reference.ToObject();
        }
    }

    [Serializable]
    private class SerializedDeclaration
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public object Value { get; set; }

        public SerializedDeclaration()
        {
        }

        public SerializedDeclaration(string name, string type, object value)
        {
            Name = name;
            Type = type;
            Value = value;
        }
    }

    [Serializable]
    private class SerializedDeclarations
    {
        public SerializedDeclaration[] Items { get; set; }

        public SerializedDeclarations()
        {
        }

        public SerializedDeclarations(SerializedDeclaration[] items)
        {
            Items = items;
        }

        public SettingDeclarationContainer Convert()
        {
            SettingDeclarationContainer settingDeclarationContainer = new SettingDeclarationContainer(s_count);
            SerializedDeclaration[] items = Items;
            foreach (SerializedDeclaration serializedDeclaration in items)
            {
                INFeature name = serializedDeclaration.Name.ToEnum<INFeature>();
                SettingType type = SettingType.Parse(serializedDeclaration.Type);
                Variant initialValue = ToVariant(type, serializedDeclaration.Value);
                settingDeclarationContainer.SetDeclaration(name, new SettingDeclaration(type, initialValue));
            }

            return settingDeclarationContainer;
        }
    }

    [Serializable]
    private class SerializedSetting
    {
        public string Name { get; set; }
        public string Scope { get; set; }
        public object Value { get; set; }

        public SerializedSetting()
        {
        }

        public SerializedSetting(string name, string scope, object value)
        {
            Name = name;
            Scope = scope;
            Value = value;
        }
    }

    [Serializable]
    private class SerializedSettings
    {
        public SerializedSetting[] Items { get; set; }

        public SerializedSettings()
        {
        }

        public SerializedSettings(SerializedSetting[] items)
        {
            Items = items;
        }

        public SettingDataContainer Convert(SettingDeclarationContainer declarations)
        {
            int s_count = INSettings.s_count;
            SettingDataContainer settingDataContainer = new SettingDataContainer(s_count);
            for (int i = 0; i < s_count; i++)
            {
                INFeature name = (INFeature)i;
                settingDataContainer.SetData(name, new SettingData(SettingScope.Global, declarations.GetDeclaration(name).InitialValue));
            }

            SerializedSetting[] items = Items;
            foreach (SerializedSetting serializedSetting in items)
            {
                INFeature name2 = serializedSetting.Name.ToEnum<INFeature>();
                SettingScope scope = serializedSetting.Scope.ToEnum<SettingScope>();
                Variant value = ToVariant(declarations.GetDeclaration(name2).Type, serializedSetting.Value);
                settingDataContainer.SetData(name2, new SettingData(scope, value));
            }

            return settingDataContainer;
        }
    }

    private static readonly int s_count = Enum.GetNames(typeof(INFeature)).Length;
    private static SettingDeclarationContainer s_declarations;
    private static SettingDataContainer s_defaultSettings;
    private static SettingDataContainer s_runtimeSettings;
    private static Action[] s_settingEditedEvents = new Action[s_count];
    public static bool VersionSelected { get; private set; }
    public static int VersionType { get; private set; }

    public static void Initialize(int version)
    {
        Load(version);
        InitializeSettings();
        VersionSelected = true;
    //GameRules.SetPartHPMode(2);
    }

    private static void Load(int version)
    {
        string text = ((version == 2) ? "A" : (version == 1) ? "O" : (version == 0) ? string.Empty : "B");
        VersionType = version;
        s_declarations = DeserializeDeclarations(INUnity.LoadTextAsset("INDeclarationSettings").text);
        if (version != 0)
        {
            s_defaultSettings = DeserializeSettings(INUnity.LoadTextAsset("INSettings" + text).text, s_declarations);
        }
        else
        {
            s_defaultSettings = new SettingDataContainer(s_count);
            for (int i = 0; i < s_count; i++)
            {
                INFeature name = (INFeature)i;
                s_defaultSettings.SetData(name, new SettingData());
            }
        }

        s_runtimeSettings = new SettingDataContainer(s_defaultSettings);
    }

    private static SettingDeclarationContainer DeserializeDeclarations(string json)
    {
        SettingDeclarationContainer settingDeclarationContainer = new SettingDeclarationContainer(s_count);
        JArray items = (JArray)JObject.Parse(json)["items"];
        foreach (JObject item in items)
        {
            INFeature name = ((string)item["name"]).ToEnum<INFeature>();
            SettingType type = SettingType.Parse((string)item["type"]);
            Variant initialValue = ToVariant(type, ToSettingValue(item["value"]));
            settingDeclarationContainer.SetDeclaration(name, new SettingDeclaration(type, initialValue));
        }

        return settingDeclarationContainer;
    }

    private static SettingDataContainer DeserializeSettings(string json, SettingDeclarationContainer declarations)
    {
        SettingDataContainer settingDataContainer = new SettingDataContainer(s_count);
        for (int i = 0; i < s_count; i++)
        {
            INFeature name = (INFeature)i;
            settingDataContainer.SetData(name, new SettingData(SettingScope.Global, declarations.GetDeclaration(name).InitialValue));
        }

        JArray items = (JArray)JObject.Parse(json)["items"];
        foreach (JObject item in items)
        {
            INFeature name = ((string)item["name"]).ToEnum<INFeature>();
            SettingScope scope = ((string)item["scope"]).ToEnum<SettingScope>();
            Variant value = ToVariant(declarations.GetDeclaration(name).Type, ToSettingValue(item["value"]));
            settingDataContainer.SetData(name, new SettingData(scope, value));
        }

        return settingDataContainer;
    }

    private static object ToSettingValue(JToken token)
    {
        if (token is JArray)
        {
            return token;
        }

        return ((JValue)token).Value;
    }

    public static bool IsEnabled(INFeature name)
    {
        return IsEnabled(GetScope(name));
    }

    private static bool IsEnabled(SettingScope scope)
    {
        return ((scope == SettingScope.None) ? false : (scope == SettingScope.Level) ? !IsSandboxMode() : (scope == SettingScope.Sandbox) ? IsSandboxMode() : (scope == SettingScope.Global) ? true : false);
    }

    public static bool GetBool(INFeature name)
    {
        return (bool)GetVariant(name).Value;
    }

    public static int GetInt(INFeature name)
    {
        return (int)GetVariant(name).Value;
    }

    public static float GetFloat(INFeature name)
    {
        return (float)GetVariant(name).Value;
    }

    public static string GetString(INFeature name)
    {
        return (string)GetVariant(name).Reference.ToObject();
    }

    public static object GetObject(INFeature name)
    {
        return GetVariant(name).Reference.ToObject();
    }

    public static bool GetInitialBool(INFeature name)
    {
        return (bool)GetInitialVariant(name).Value;
    }

    public static int GetInitialInt(INFeature name)
    {
        return (int)GetInitialVariant(name).Value;
    }

    public static float GetInitialFloat(INFeature name)
    {
        return (float)GetInitialVariant(name).Value;
    }

    public static string GetInitialString(INFeature name)
    {
        return (string)GetInitialVariant(name).Reference.ToObject();
    }

    public static object GetInitialObject(INFeature name)
    {
        return GetInitialVariant(name).Reference.ToObject();
    }

    public static WrappedSettingData GetSettingsData(INFeature name)
    {
        return WrappedSettingData.Create(name);
    }

    private static SettingScope GetScope(INFeature name)
    {
        return s_runtimeSettings.GetData(name).Scope;
    }

    private static Variant GetVariant(INFeature name)
    {
        if (!IsEnabled(name))
        {
            return GetInitialVariant(name);
        }

        return s_runtimeSettings.GetData(name).Value;
    }

    private static Variant GetInitialVariant(INFeature name)
    {
        return s_declarations.GetDeclaration(name).InitialValue;
    }

    public static void SetValue(INFeature name, Variant value)
    {
        Variant variant = GetVariant(name);
        s_runtimeSettings.GetData(name).Value = value;
        Action listener = GetListener(name);
        if (listener != null && variant != GetVariant(name))
        {
            listener();
        }
    }

    private static void SetScope(INFeature name, SettingScope scope)
    {
        Variant variant = GetVariant(name);
        s_runtimeSettings.GetData(name).Scope = scope;
        Action listener = GetListener(name);
        if (listener != null && variant != GetVariant(name))
        {
            listener();
        }
    }

    private static bool IsSandboxMode()
    {
        GameManager instance = Singleton<GameManager>.Instance;
        LevelManager levelManager = WPFMonoBehaviour.levelManager;
        if (instance != null && levelManager != null && instance.CurrentEpisodeType == GameManager.EpisodeType.Sandbox)
        {
            return levelManager.CurrentGameMode is BaseGameMode;
        }

        return false;
    }

    public static void AddListener(INFeature name, Action action)
    {
        ref Action reference = ref s_settingEditedEvents[(int)name];
        reference = (Action)Delegate.Combine(reference, action);
    }

    public static void AddListenerAndInvoke(INFeature name, Action action)
    {
        action();
        ref Action reference = ref s_settingEditedEvents[(int)name];
        reference = (Action)Delegate.Combine(reference, action);
    }

    public static void RemoveListener(INFeature name, Action action)
    {
        ref Action reference = ref s_settingEditedEvents[(int)name];
        reference = (Action)Delegate.Remove(reference, action);
    }

    private static Action GetListener(INFeature name)
    {
        return s_settingEditedEvents[(int)name];
    }

    private static void InitializeSettings()
    {
        UnityEngine.Object.DontDestroyOnLoad(UnityEngine.Object.Instantiate(INUnity.LoadGameObject("EventSystem")));
        INLocalization.Create();
        INUserSettings.Load();
        AddListenerAndInvoke(INFeature.TimeScale, SetTimeScale);
        AddListenerAndInvoke(INFeature.NewContraptionData, INContraptionDataManager.SetContraptionData);
    }

    private static void SetTimeScale()
    {
        if (!GameTime.IsPaused())
        {
            Time.timeScale = GetFloat(INFeature.TimeScale);
        }
    }

    private static Variant ToVariant(SettingType type, object obj)
    {
        if (type.MainType.IsValueType() && (obj is IConvertible value))
        {
            ValueVariant value2 = new ValueVariant(type.MainType.ToTypeCode(), value);
            return new Variant(value2);
        }

        if (type.MainType == SettingTypeCode.Array && (obj is JArray jArray))
        {
            int count = jArray.Count;
            SettingTypeCode elementType = type.GenericArguments[0];
            if (elementType == SettingTypeCode.Boolean)
            {
                bool[] boolArray = new bool[count];
                for (int i = 0; i < count; i++)
                {
                    boolArray[i] = (bool)jArray[i];
                }
                obj = boolArray;
            }
            else if (elementType == SettingTypeCode.Int32)
            {
                int[] intArray = new int[count];
                for (int i = 0; i < count; i++)
                {
                    intArray[i] = (int)jArray[i];
                }
                obj = intArray;
            }
            else if (elementType == SettingTypeCode.Single)
            {
                float[] floatArray = new float[count];
                for (int i = 0; i < count; i++)
                {
                    floatArray[i] = (float)jArray[i];
                }
                obj = floatArray;
            }
            else if (elementType == SettingTypeCode.String)
            {
                string[] stringArray = new string[count];
                for (int i = 0; i < count; i++)
                {
                    stringArray[i] = (string)jArray[i];
                }
                obj = stringArray;
            }
            else
            {
                object[] objectArray = new object[count];
                for (int i = 0; i < count; i++)
                {
                    objectArray[i] = ((JValue)jArray[i]).Value;
                }
                obj = objectArray;
            }
        }

        return new Variant(new RefVariant(obj));
    }

    private static bool IsValueType(this SettingTypeCode settingsType)
    {
        if (settingsType != SettingTypeCode.Boolean && settingsType != SettingTypeCode.Int32)
        {
            return settingsType == SettingTypeCode.Single;
        }

        return true;
    }

    private static TypeCode ToTypeCode(this SettingTypeCode settingsType)
    {
        return ((settingsType == SettingTypeCode.Boolean) ? TypeCode.Boolean : (settingsType == SettingTypeCode.Int32) ? TypeCode.Int32 : (settingsType == SettingTypeCode.Single) ? TypeCode.Single : (settingsType == SettingTypeCode.String) ? TypeCode.String : TypeCode.Empty);
    }
}
