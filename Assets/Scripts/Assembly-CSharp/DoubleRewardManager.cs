using System;
using UnityEngine;

public class DoubleRewardManager : Singleton<DoubleRewardManager>
{
    public enum Status
    {
        Uninitialized = 0,
        Initialized = 1,
        Error = 2
    }

    public Action OnInitialize;
    public Action OnAdWatched;
    public Action OnAdFailed;
    public Action<bool> OnAdLoaded;
    public Action OnConfirmationFailed;
    private AdReward adReward;
    private ServerTime serverTime;
    private int rewardTime;
    private float lastCheckTime;
    private float doubleRewardEndTime;
    public bool HasAd { get; private set; }
    public bool LoadingAd { get; private set; }
    public Status CurrentStatus { get; private set; }
    public float DoubleRewardTimeRemaining => doubleRewardEndTime - Time.realtimeSinceStartup;
    public bool HasDoubleReward => DoubleRewardTimeRemaining > 0f;
    public int RewardCoins { get; private set; }

    public string FormattedRewardTime
    {
        get
        {
            TimeSpan time = TimeSpan.FromSeconds(Mathf.Clamp(rewardTime, 0f, float.MaxValue));
            if (time.TotalMinutes > 0.0)
            {
                return TimeFormatter.Format2Minutes(time);
            }

            return TimeFormatter.Format2Seconds(time);
        }
    }

    public string FormattedDoubleRewardTimeRemaining
    {
        get
        {
            TimeSpan time = TimeSpan.FromSeconds(Mathf.Clamp(DoubleRewardTimeRemaining, 0f, float.MaxValue));
            if (time.TotalMinutes > 0.0)
            {
                return TimeFormatter.Format2Minutes(time);
            }

            return TimeFormatter.Format2Seconds(time);
        }
    }

    private float TimeSinceLastCheck
    {
        get
        {
            if (lastCheckTime < 0f)
            {
                return float.MaxValue;
            }

            return Time.realtimeSinceStartup - lastCheckTime;
        }
    }

    private void Awake()
    {
        SetAsPersistant();
        CurrentStatus = Status.Uninitialized;
        lastCheckTime = -1f;
        if (Singleton<GameConfigurationManager>.Instance.HasData)
        {
            rewardTime = Singleton<GameConfigurationManager>.Instance.GetValue<int>("double_reward_duration", "seconds");
            RewardCoins = Singleton<GameConfigurationManager>.Instance.GetValue<int>("double_reward_coin_reward", "coin_reward");
            Initialize();
            return;
        }

        GameConfigurationManager gameConfigurationManager = Singleton<GameConfigurationManager>.Instance;
        gameConfigurationManager.OnHasData = (Action)Delegate.Combine(gameConfigurationManager.OnHasData, (Action)delegate
        {
            rewardTime = Singleton<GameConfigurationManager>.Instance.GetValue<int>("double_reward_duration", "seconds");
            RewardCoins = Singleton<GameConfigurationManager>.Instance.GetValue<int>("double_reward_coin_reward", "coin_reward");
            Initialize();
        });
    }

    private void Initialize()
    {
        adReward = new AdReward(AdvertisementHandler.DoubleRewardPlacement);
        adReward.OnReady = (Action)Delegate.Combine(adReward.OnReady, new Action(OnAdReady));
        adReward.OnFailed = (Action)Delegate.Combine(adReward.OnFailed, new Action(OnAdFailure));
        adReward.OnAdFinished = (Action)Delegate.Combine(adReward.OnAdFinished, new Action(OnAdFinished));
        adReward.OnLoading = (Action)Delegate.Combine(adReward.OnLoading, new Action(OnAdLoading));
        adReward.OnCancel = (Action)Delegate.Combine(adReward.OnCancel, new Action(OnAdCancel));
        adReward.OnConfirmationFailed = (Action)Delegate.Combine(adReward.OnConfirmationFailed, new Action(OnAdConfirmationFailed));
        adReward.Load();
        serverTime = new ServerTime();
        serverTime.StatusChanged += OnServerTimeStatusChanged;
        serverTime.RefreshServerTime();
    }

    private void OnDestroy()
    {
        adReward?.Dispose();
        serverTime?.Destroy();
    }

    private void OnAdReady()
    {
        HasAd = true;
        LoadingAd = false;
        if (OnAdLoaded != null)
        {
            OnAdLoaded(obj: true);
            OnAdLoaded = null;
        }
    }

    private void OnAdFailure()
    {
        HasAd = false;
        LoadingAd = false;
        if (OnAdFailed != null)
        {
            OnAdFailed();
            OnAdLoaded = null;
        }
    }

    private void OnAdFinished()
    {
        HasAd = false;
        if (OnAdWatched != null)
        {
            OnAdWatched();
            OnAdWatched = null;
        }
    }

    private void OnAdLoading()
    {
        HasAd = false;
        LoadingAd = true;
    }

    private void OnAdCancel()
    {
    }

    private void OnAdConfirmationFailed()
    {
        adReward.Load();
        if (OnConfirmationFailed != null)
        {
            OnConfirmationFailed();
            OnConfirmationFailed = null;
        }
    }

    private void OnServerTimeStatusChanged(int serverTime)
    {
        lastCheckTime = Time.realtimeSinceStartup;
        int doubleRewardStartTime = GameProgress.GetDoubleRewardStartTime();
        if (doubleRewardStartTime > 0)
        {
            int num = doubleRewardStartTime + rewardTime - serverTime;
            if (num > 0)
            {
                doubleRewardEndTime = Time.realtimeSinceStartup + (float)num;
            }
            else
            {
                doubleRewardEndTime = -1f;
            }
        }
        else
        {
            doubleRewardEndTime = -1f;
        }

        if (CurrentStatus == Status.Uninitialized)
        {
            CurrentStatus = Status.Initialized;
            OnInitialize?.Invoke();
        }
    }

    private void SetDoubleRewardStartTime(bool success, int time)
    {
        if (success)
        {
            GameProgress.SetDoubleRewardStartTime(time);
            doubleRewardEndTime = Time.realtimeSinceStartup + (float)rewardTime;
            if (DoubleRewardIcon.Instance != null)
            {
                DoubleRewardIcon.Instance.gameObject.SetActive(value: true);
            }
        }
    }

    public void RefreshServerTime()
    {
        serverTime.RefreshServerTime();
    }

    public void RefreshAd()
    {
        if (!LoadingAd)
        {
            adReward.Load();
        }
    }

    public void PlayAd()
    {
        if (HasAd)
        {
            adReward.Play();
        }
    }
}