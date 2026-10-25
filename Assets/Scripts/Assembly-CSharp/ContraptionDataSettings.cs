using System;

[Serializable]
public class ContraptionDataSettings : SettingsBase
{
    public enum SerializationFormat
    {
        CSV = 0,
        JSON = 1,
        ALL = -1
    }

    public bool Enabled { get; set; }
    public SerializationFormat LoadFormat { get; set; }
    public SerializationFormat SaveFormat { get; set; }
    public bool BackupData { get; set; }
    public bool BackupOriginalData { get; set; }
    public bool SaveAsOriginalData { get; set; }

    public ContraptionDataSettings()
    {
        Enabled = true;
        LoadFormat = SerializationFormat.ALL;
        SaveFormat = SerializationFormat.CSV;
        BackupData = true;
        BackupOriginalData = true;
        SaveAsOriginalData = false;
    }

    public ContraptionDataSettings(ContraptionDataSettings settings)
    {
        Enabled = settings.Enabled;
        LoadFormat = settings.LoadFormat;
        SaveFormat = settings.SaveFormat;
        BackupData = settings.BackupData;
        BackupOriginalData = settings.BackupOriginalData;
        SaveAsOriginalData = settings.SaveAsOriginalData;
    }
}