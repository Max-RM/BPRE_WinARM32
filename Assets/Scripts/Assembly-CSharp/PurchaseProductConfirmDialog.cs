using UnityEngine;

public class PurchaseProductConfirmDialog : TextDialog
{
    [SerializeField]
    private SpriteScale[] customScales;
    [SerializeField]
    private GameObject description;
    [SerializeField]
    private SpriteText countText;
    [SerializeField]
    private Sprite itemIcon;
    [SerializeField]
    private Transform itemCountTf;
    [SerializeField]
    private GameObject costText;
    [SerializeField]
    private GameObject productName;
    private Sprite buttonBackground;
    private Sprite disabledButtonBackground;
    private Vector2 defaultScale;
    public string ItemSpriteID { get; set; }
    public string EffectSpriteID { get; set; }
    public string ItemLocalizationKey { get; set; }
    public string ItemDescriptionKey { get; set; }
    public int ItemCount { get; set; }
    public int Cost { get; set; }

    protected override void Awake()
    {
        defaultScale = new Vector2(itemIcon.m_scaleX, itemIcon.m_scaleY);
        if (WPFMonoBehaviour.levelManager != null)
        {
            WPFMonoBehaviour.levelManager.ConstructionUI.DisableFunctionality = true;
        }

        base.Awake();
    }

    protected virtual void OnDestroy()
    {
        if (WPFMonoBehaviour.levelManager != null)
        {
            WPFMonoBehaviour.levelManager.ConstructionUI.DisableFunctionality = false;
        }
    }

    protected override void Start()
    {
        RebuildIcons();
        RebuildTexts();
    }

    public new void Open()
    {
        base.Open();
        RebuildIcons();
        RebuildTexts();
        EventManager.Send(new UIEvent(UIEvent.Type.OpenedPurchaseConfirmation));
    }

    public new void Close()
    {
        base.Close();
        EventManager.Send(new UIEvent(UIEvent.Type.ClosedPurchaseConfirmation));
    }

    private new void HandleKeyReleased(KeyCode obj)
    {
        if (obj == KeyCode.Escape)
        {
            Close();
        }
    }

    public new void Confirm()
    {
        base.Confirm();
        Close();
    }

    public void OpenSnoutCoinPopup()
    {
        if (Singleton<IapManager>.Instance != null)
        {
            Singleton<IapManager>.Instance.OpenShopPage(null, "SnoutCoinShop");
        }
    }

    public void RebuildIcons()
    {
        if (Singleton<RuntimeSpriteDatabase>.Instance != null)
        {
            if (SpriteScale.GetCustomScale(customScales, ItemSpriteID, out Vector2 scale))
            {
                itemIcon.m_scaleX = scale.x;
                itemIcon.m_scaleY = scale.y;
            }
            else
            {
                itemIcon.m_scaleX = defaultScale.x;
                itemIcon.m_scaleY = defaultScale.y;
            }

            SpriteData spriteData = Singleton<RuntimeSpriteDatabase>.Instance.Find(ItemSpriteID);
            if (spriteData != null)
            {
                itemIcon.SelectSprite(spriteData, forceResetMesh: true);
            }
        }
    }

    public void RebuildTexts()
    {
        if (itemCountTf != null && countText != null)
        {
            string text = ((ItemCount <= 0) ? string.Empty : $"x{ItemCount}");
            countText.Text = text;
        }

        RefreshTexts(costText, $"[snout] {Cost}", updateLocale: false, updateSprites: true);
        RefreshTexts(productName, ItemLocalizationKey, updateLocale: true, updateSprites: false);
        RefreshTexts(description, ItemDescriptionKey, updateLocale: true, updateSprites: false);
        EnableConfirmButton(Cost > 0);
    }

    private void RefreshTexts(GameObject target, string text, bool updateLocale, bool updateSprites)
    {
        TextMesh[] componentsInChildren = target.GetComponentsInChildren<TextMesh>(includeInactive: true);
        for (int i = 0; i < componentsInChildren.Length; i++)
        {
            componentsInChildren[i].text = text;
            if (updateLocale)
            {
                TextMeshLocale textMeshLocale = componentsInChildren[i].GetComponent<TextMeshLocale>();
                if (textMeshLocale == null)
                {
                    textMeshLocale = componentsInChildren[i].gameObject.AddComponent<TextMeshLocale>();
                }

                textMeshLocale.RefreshTranslation();
                textMeshLocale.enabled = false;
            }

            if (updateSprites)
            {
                TextMeshSpriteIcons.EnsureSpriteIcon(componentsInChildren[i]);
            }
        }
    }
}