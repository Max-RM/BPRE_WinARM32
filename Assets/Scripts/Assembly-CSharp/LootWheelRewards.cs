using System;
using UnityEngine;

public class LootWheelRewards
{
    public enum RewardType
    {
        None = 0,
        Dessert = 1,
        Scrap = 2,
        Powerup = 3,
        Part = 4
    }

    public enum WheelReward
    {
        None = 0,
        Dessert1 = 1,
        Dessert2 = 2,
        Dessert3 = 3,
        Scrap1 = 4,
        Scrap2 = 5,
        Powerup = 6,
        CommonPart = 7,
        RarePart = 8,
        EpicPart = 9
    }

    public struct LootWheelReward
    {
        public int Amount { get; }
        public int SingleValue { get; }
        public int TotalValue => Amount * SingleValue;
        public RewardType Type { get; }
        public LootCrateRewards.Powerup PowerupReward { get; }
        public BasePart PartReward { get; }
        public static LootWheelReward Empty => default(LootWheelReward);

        public LootWheelReward(int amount, int value, RewardType type)
        {
            Amount = amount;
            SingleValue = value;
            Type = type;
            PowerupReward = LootCrateRewards.Powerup.None;
            PartReward = null;
        }

        public LootWheelReward(string key, ConfigData amounts, ConfigData values, RewardType type)
        {
            Amount = int.Parse(amounts[key]);
            SingleValue = int.Parse(values[key]);
            Type = type;
            PowerupReward = LootCrateRewards.Powerup.None;
            PartReward = null;
        }

        public LootWheelReward(string key, ConfigData amounts, ConfigData values, RewardType type, LootCrateRewards.Powerup powerup)
        {
            Amount = int.Parse(amounts[key]);
            SingleValue = int.Parse(values[key]);
            Type = type;
            PowerupReward = powerup;
            PartReward = null;
        }

        public LootWheelReward(string key, ConfigData amounts, ConfigData values, RewardType type, BasePart part)
        {
            Amount = int.Parse(amounts[key]);
            SingleValue = int.Parse(values[key]);
            Type = type;
            PowerupReward = LootCrateRewards.Powerup.None;
            PartReward = part;
        }
    }

    private const string PRIZE_AMOUNTS_CONFIG = "loot_wheel_prize_amounts";
    private const string PRIZE_VALUES_CONFIG = "loot_wheel_prize_values";
    private const string SPIN_PRICE_PARAMS = "loot_wheel_spin_price_params";
    private const string DESSERT_0 = "dessert_0";
    private const string DESSERT_1 = "dessert_1";
    private const string DESSERT_2 = "dessert_2";
    private const string SCRAP_0 = "scrap_0";
    private const string SCRAP_1 = "scrap_1";
    private const string POWERUP = "powerup";
    private const string COMMON = "common";
    private const string RARE = "rare";
    private const string EPIC = "epic";
    private const string VAR_PERCENTAGE = "variation_percentage";
    private const string PRICE_MULTIPLIER = "price_multiplier";
    private ConfigData m_amounts;
    private ConfigData m_values;
    public Action OnInitialized;
    public bool Initialized { get; private set; }
    public int TotalRewardValues { get; private set; }
    public float TotalRewardInverseValues { get; private set; }
    public float RewardValueAvg { get; private set; }
    public float SpinPriceVariation { get; private set; }
    public float SpinPriceMultiplier { get; private set; }

    public LootWheelRewards()
    {
        if (Singleton<GameConfigurationManager>.Instance.HasData)
        {
            Initialize();
            return;
        }

        GameConfigurationManager instance = Singleton<GameConfigurationManager>.Instance;
        instance.OnHasData = (Action)Delegate.Combine(instance.OnHasData, new Action(Initialize));
    }

    private void Initialize()
    {
        GameConfigurationManager instance = Singleton<GameConfigurationManager>.Instance;
        instance.OnHasData = (Action)Delegate.Remove(instance.OnHasData, new Action(Initialize));
        m_amounts = Singleton<GameConfigurationManager>.Instance.GetConfig("loot_wheel_prize_amounts");
        m_values = Singleton<GameConfigurationManager>.Instance.GetConfig("loot_wheel_prize_values");
        TotalRewardValues = 0;
        for (int i = 0; i < m_values.Keys.Length; i++)
        {
            int num = int.Parse(m_amounts[m_amounts.Keys[i]]);
            int num2 = int.Parse(m_values[m_values.Keys[i]]);
            TotalRewardValues += num * num2;
        }

        TotalRewardInverseValues = 0f;
        for (int j = 0; j < m_values.Keys.Length; j++)
        {
            int num3 = int.Parse(m_amounts[m_amounts.Keys[j]]);
            int num4 = int.Parse(m_values[m_values.Keys[j]]);
            TotalRewardInverseValues += (float)TotalRewardValues / ((float)num3 * (float)num4);
        }

        RewardValueAvg = (float)TotalRewardValues / (float)m_values.Count;
        if (Singleton<BuildCustomizationLoader>.Instance.IsOdyssey)
        {
            SpinPriceMultiplier = 0f;
            SpinPriceVariation = 0f;
        }
        else
        {
            ConfigData config = Singleton<GameConfigurationManager>.Instance.GetConfig("loot_wheel_spin_price_params");
            SpinPriceVariation = float.Parse(config["variation_percentage"]);
            SpinPriceMultiplier = float.Parse(config["price_multiplier"]);
        }

        Initialized = true;
        OnInitialized?.Invoke();
    }

    public LootWheelReward GetReward(WheelReward slot)
    {
        return ((slot == WheelReward.Dessert1) ? new LootWheelReward("dessert_0", m_amounts, m_values, RewardType.Dessert) : (slot == WheelReward.Dessert2) ? new LootWheelReward("dessert_1", m_amounts, m_values, RewardType.Dessert) : (slot == WheelReward.Dessert3) ? new LootWheelReward("dessert_2", m_amounts, m_values, RewardType.Dessert) : (slot == WheelReward.Scrap1) ? new LootWheelReward("scrap_0", m_amounts, m_values, RewardType.Scrap) : (slot == WheelReward.Scrap2) ? new LootWheelReward("scrap_1", m_amounts, m_values, RewardType.Scrap) : (slot == WheelReward.Powerup) ? new LootWheelReward("powerup", m_amounts, m_values, RewardType.Powerup, GetRandomPowerup()) : (slot == WheelReward.CommonPart) ? new LootWheelReward("common", m_amounts, m_values, RewardType.Part, GetRandomPart(BasePart.PartTier.Common)) : (slot == WheelReward.RarePart) ? new LootWheelReward("rare", m_amounts, m_values, RewardType.Part, GetRandomPart(BasePart.PartTier.Rare)) : (slot == WheelReward.EpicPart) ? new LootWheelReward("epic", m_amounts, m_values, RewardType.Part, GetRandomPart(BasePart.PartTier.Epic)) : throw new ArgumentException("Not a valid argument!"));
    }

    private BasePart GetRandomPart(BasePart.PartTier tier)
    {
        BasePart randomCraftablePartFromTier = CustomizationManager.GetRandomCraftablePartFromTier(tier, onlyLocked: true);
        if (randomCraftablePartFromTier == null)
        {
            randomCraftablePartFromTier = CustomizationManager.GetRandomCraftablePartFromTier(tier);
        }

        return randomCraftablePartFromTier;
    }

    private LootCrateRewards.Powerup GetRandomPowerup()
    {
        return ((UnityEngine.Random.Range(0, 4) == 0) ? LootCrateRewards.Powerup.Magnet : (UnityEngine.Random.Range(0, 4) == 1) ? LootCrateRewards.Powerup.NightVision : (UnityEngine.Random.Range(0, 4) == 2) ? LootCrateRewards.Powerup.Superglue : (UnityEngine.Random.Range(0, 4) == 3) ? LootCrateRewards.Powerup.Turbo : LootCrateRewards.Powerup.None);
    }
}