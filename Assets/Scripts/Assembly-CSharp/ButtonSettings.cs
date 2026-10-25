public class ButtonSettings : SettingsBase
{
    public bool Enabled { get; set; }
    public float ButtonScale { get; set; }
    public float ScrollViewHeightScale { get; set; }
    public int MaxSeparationCount { get; set; }
    public bool DisplayButtonIndex { get; set; }

    public ButtonSettings()
    {
        Enabled = true;
        ButtonScale = 1f;
        ScrollViewHeightScale = 1f;
        MaxSeparationCount = 3;
        DisplayButtonIndex = false;
    }

    public ButtonSettings(ButtonSettings settings)
    {
        Enabled = settings.Enabled;
        ButtonScale = settings.ButtonScale;
        ScrollViewHeightScale = settings.ScrollViewHeightScale;
        MaxSeparationCount = settings.MaxSeparationCount;
        DisplayButtonIndex = settings.DisplayButtonIndex;
    }
}