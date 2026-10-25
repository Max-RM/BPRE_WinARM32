using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

[Serializable]
public class INUserSettings
{
    public Version Version { get; set; }
    public PhysicsSettings PhysicsSettings { get; set; }
    public ButtonSettings ButtonSettings { get; set; }
    public ContraptionDataSettings ContraptionDataSettings { get; set; }

    [JsonIgnore]
    public IEnumerable<SettingsBase> SettingsList
    {
        get
        {
            yield return PhysicsSettings;
            yield return ButtonSettings;
            yield return ContraptionDataSettings;
        }
    }

    public static INUserSettings Default { get; private set; }
    public static INUserSettings Instance { get; private set; }

    public INUserSettings()
    {
    }

    public INUserSettings(Version version)
    {
        Version = version;
        PhysicsSettings = new PhysicsSettings();
        ButtonSettings = new ButtonSettings();
        ContraptionDataSettings = new ContraptionDataSettings();
    }

    public INUserSettings(INUserSettings settings)
    {
        Version = settings.Version;
        PhysicsSettings = new PhysicsSettings(settings.PhysicsSettings);
        ButtonSettings = new ButtonSettings(settings.ButtonSettings);
        ContraptionDataSettings = new ContraptionDataSettings(settings.ContraptionDataSettings);
    }

    static INUserSettings()
    {
        Default = new INUserSettings(INUnity.Version);
        Instance = new INUserSettings(Default);
    }

    public void Initialize()
    {
        foreach (SettingsBase settings in SettingsList)
        {
            settings.Initialize();
        }
    }

    public static void Load()
    {
        string path = INUnity.SettingsPath + "/INUserSettings.json";
        if (File.Exists(path))
        {
            try
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    INUserSettings iNUserSettings = INJsonSerializer.Deserialize<INUserSettings>(reader);
                    if (iNUserSettings.Version != null && iNUserSettings.Version >= new Version(2022, 1, 0))
                    {
                        Instance = iNUserSettings;
                    }
                }
            }
            catch
            {
            }
        }

        Instance.Initialize();
    }

    public static void Save()
    {
        string settingsPath = INUnity.SettingsPath;
        string path = INUnity.SettingsPath + "/INUserSettings.json";
        try
        {
            if (!Directory.Exists(settingsPath))
            {
                Directory.CreateDirectory(settingsPath);
            }

            using (StreamWriter writer = new StreamWriter(path))
            {
                INJsonSerializer.Serialize(Instance, writer);
            }
        }
        catch
        {
        }
    }

    public static void Reset()
    {
        Instance = new INUserSettings(Default);
        Instance.Initialize();
    }
}