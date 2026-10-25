public class GetMoreScrapDialog : TextDialog
{
    private int buyScrapAmount;
    private BasePart.PartTier partTier;
    protected override void Awake()
    {
        base.Awake();
        base.onOpen += RefreshLocalization;
    }

    protected override void Start()
    {
    }

    private void OnDestroy()
    {
        base.onOpen -= RefreshLocalization;
    }

    public void SetScrapAmount(int scrapAmount, BasePart.PartTier tier)
    {
        buyScrapAmount = scrapAmount;
        partTier = tier;
    }

    private void RefreshLocalization()
    {
        for (int i = 0; i < texts.Length; i++)
        {
            texts[i].textMesh.text = texts[i].localizationKey;
            TextMeshLocale component = texts[i].textMesh.gameObject.GetComponent<TextMeshLocale>();
            if (!(component != null))
            {
                continue;
            }

            component.RefreshTranslation();
            string text = texts[i].textMesh.text;
            if (texts[i].textMesh.name.Equals("ScrapLabel") && text.Contains("{0}") && text.Contains("{1}"))
            {
                string arg = ((partTier == BasePart.PartTier.Common) ? "[common_star]" : (partTier == BasePart.PartTier.Rare) ? "[rare_star][rare_star]" : (partTier == BasePart.PartTier.Epic) ? "[epic_star][epic_star][epic_star]" : (partTier == BasePart.PartTier.Legendary) ? "[legendary_icon]" : string.Empty);
                texts[i].textMesh.text = string.Format(text, buyScrapAmount, arg);
            }

            component.enabled = false;
            TextMeshSpriteIcons.EnsureSpriteIcon(texts[i].textMesh);
            TextMeshHelper.Wrap(texts[i].textMesh, (!TextMeshHelper.UsesKanjiCharacters()) ? maxCharactersInLine : maxKanjiCharacterInLine);
        }
    }
}