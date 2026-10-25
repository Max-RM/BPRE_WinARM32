using System;
using UnityEngine;

public class RefreshLocalizer : IDisposable
{
    private string originalText = string.Empty;
    private string localizedText = string.Empty;
    private float originalCharacterSize;
    private float originalLineSpacing;
    private bool disposed;
    public Func<string> Update { get; set; }
    public TextMesh Target { get; private set; }

    public RefreshLocalizer(TextMesh target)
    {
        this.Target = target;
        originalText = target.text;
        originalCharacterSize = target.characterSize;
        originalLineSpacing = target.lineSpacing;
        ReloadLocalization(default(LocalizationReloaded));
        EventManager.Connect<LocalizationReloaded>(ReloadLocalization);
    }

    ~RefreshLocalizer()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (!disposed)
        {
            EventManager.Disconnect<LocalizationReloaded>(ReloadLocalization);
            Target = null;
            originalText = null;
            localizedText = null;
            Update = null;
            GC.SuppressFinalize(this);
        }
    }

    private void ReloadLocalization(LocalizationReloaded localizationReloaded)
    {
        ApplyLocale();
        Refresh();
    }

    private void ApplyLocale(string localeName = null)
    {
        Localizer.LocaleParameters localeParameters = Singleton<Localizer>.Instance.Resolve(originalText, localeName);
        Font font = Singleton<Localizer>.Instance.GetFont(localeName);
        if ((bool)font)
        {
            Color color = Target.GetComponent<Renderer>().material.color;
            Target.font = font;
            Target.GetComponent<Renderer>().material = font.material;
            Target.GetComponent<Renderer>().material.color = color;
        }

        localizedText = localeParameters.translation;
        Target.characterSize = originalCharacterSize * localeParameters.characterSizeFactor;
        Target.lineSpacing = originalLineSpacing * localeParameters.lineSpacingFactor;
    }

    public void Refresh()
    {
        if (Update != null)
        {
            Target.text = string.Format(localizedText, Update());
        }
    }
}