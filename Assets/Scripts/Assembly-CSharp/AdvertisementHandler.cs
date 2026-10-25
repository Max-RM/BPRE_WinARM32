using System;
using System.Collections.Generic;
using UnityEngine;

public class AdvertisementHandler
{
    public class RenderableHandler
    {
        public string m_placementName;
        public Texture2D m_texture;
        public Action<bool> onRenderableReady;
        public RenderableHandler(string placement)
        {
            m_placementName = placement;
            m_texture = null;
        }

        public bool OnRenderableReady(string placement, string contentType, List<byte> content)
        {
            if (!placement.Equals(m_placementName) || content == null)
            {
                onRenderableReady?.Invoke(obj: false);
                return false;
            }

            if (contentType.StartsWith("image/"))
            {
                Texture2D texture2D = new Texture2D(1, 1);
                if (texture2D.LoadImage(content.ToArray()))
                {
                    m_texture = texture2D;
                    onRenderableReady?.Invoke(obj: true);
                    return true;
                }
            }

            onRenderableReady?.Invoke(obj: false);
            return false;
        }
    }

    private static RenderableHandler rewardNativeRenderable;
    private static string timeRewardVideoPlacement = "RewardVideo";
    private static string rewardNativePlacement = "RewardNative";
    private static string mainMenuPopupPlacement = "MainMenuPopup";
    private static string interstitialPlacement = "LevelStartInterstitial";
    private static string pauseMenuPromoPlacement = "NewsFeed.pause";
    private static string crossPromoMainPlacement = "InGameNative.MainMenu";
    private static string crossPromoEpisodePlacement = "InGameNative.EpisodeMenu";
    public static string LevelRewardVideoPlacement { get; } = "RewardVideo.LevelUnlock";
    public static string SnoutCoinRewardVideoPlacement { get; } = "RewardVideo.SnoutReward";
    public static string DoubleRewardPlacement { get; } = "RewardVideo.DoubleReward";
    public static string ExtraCoinsRewardPlacement { get; } = "RewardVideo.ExtraCoins";
    public static string DailyChallengeRevealPlacement { get; } = "RewardVideo.DailyChallengeReveal";
    public static string FreeLootCratePlacement { get; } = "RewardVideo.FreeLootCrate";
    public static RenderableHandler MainMenuPromoRenderable { get; }
    public static RenderableHandler CrossPromoMainRenderable { get; }
    public static RenderableHandler CrossPromoEpisodeRenderable { get; }

    public static Texture2D GetRewardNativeTexture()
    {
        return rewardNativeRenderable?.m_texture;
    }

    public static Texture2D GetMainMenuPopupTexture()
    {
        return MainMenuPromoRenderable?.m_texture;
    }

    public static Texture2D GetCrossPromoMainTexture()
    {
        return CrossPromoMainRenderable?.m_texture;
    }

    public static Texture2D GetCrossPromoEpisodeTexture()
    {
        return CrossPromoEpisodeRenderable?.m_texture;
    }

    public static bool IsAdvertisementReady(string placement)
    {
        return false;
    }
}