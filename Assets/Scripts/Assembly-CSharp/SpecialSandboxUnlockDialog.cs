using UnityEngine;

public class SpecialSandboxUnlockDialog : TextDialog
{
    public enum UnlockType
    {
        Statue = 0,
        Skull = 1
    }

    [SerializeField]
    private LocalizeSprite[] statueEpisodeIcon;
    [SerializeField]
    private LocalizeSprite[] skullEpisodeIcon;
    [SerializeField]
    private Sprite sandboxLogo;
    [SerializeField]
    private TextMesh costTextEnabled;
    [SerializeField]
    private TextMesh costTextDisabled;
    [SerializeField]
    private TextMesh[] collectedTexts;
    public UnlockType Type { get; set; }
    public int Collected { get; set; }
    public int Required { get; set; }
    public int Cost { get; set; }

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        if (!isOpened)
        {
            RebuildIcons();
            RebuildTexts();
        }
    }

    public new void Open()
    {
        RebuildIcons();
        RebuildTexts();
        base.Open();
    }

    public new void Close()
    {
        base.Close();
    }

    public new void Confirm()
    {
        base.Confirm();
    }

    public new void OpenShop()
    {
        base.OpenShop();
    }

    public void RebuildIcons()
    {
        if (Singleton<RuntimeSpriteDatabase>.Instance != null)
        {
            RuntimeSpriteDatabase instance = Singleton<RuntimeSpriteDatabase>.Instance;
            switch (Type)
            {
                case UnlockType.Statue:
                    sandboxLogo.SelectSprite(instance.Find(LocalizeSprite.GetLocalizedSprite(statueEpisodeIcon, Singleton<Localizer>.Instance.CurrentLocale)), forceResetMesh: true);
                    break;
                case UnlockType.Skull:
                    sandboxLogo.SelectSprite(instance.Find(LocalizeSprite.GetLocalizedSprite(skullEpisodeIcon, Singleton<Localizer>.Instance.CurrentLocale)), forceResetMesh: true);
                    break;
            }
        }
    }

    public void RebuildTexts()
    {
        string arg = string.Empty;
        string text = $"[snout] {Cost}";
        arg = ((Type == UnlockType.Statue) ? "[statue]" : (Type == UnlockType.Skull) ? "[skull]" : arg);
        string text2 = $"{arg} {Collected}/{Required}";
        for (int i = 0; i < collectedTexts.Length; i++)
        {
            collectedTexts[i].text = text2;
            TextMeshSpriteIcons.EnsureSpriteIcon(collectedTexts[i]);
        }

        costTextDisabled.text = text;
        TextMeshSpriteIcons.EnsureSpriteIcon(costTextDisabled);
        costTextEnabled.text = text;
        TextMeshSpriteIcons.EnsureSpriteIcon(costTextEnabled);
    }
}