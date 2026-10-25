using System;
using System.Collections;
using UnityEngine;

public class AdReward : IDisposable
{
    private enum State
    {
        Loading = 0,
        WaitingFail = 1,
        Failed = 2,
        Ready = 3,
        Finished = 4,
        Stalled = 5,
        Cancelled = 6,
        WaitingConfirmation = 7,
        ConfirmationFailed = 8
    }

    public Action OnFailed;
    public Action OnReady;
    public Action OnAdFinished;
    public Action OnLoading;
    public Action OnCancel;
    public Action OnConfirmationFailed;
    public Action OnAdPlayFailed;
    private const float TIMEOUT_LENGTH = 10f;
    private bool wasPaused;
    private bool waitingFailure;
    private string placement;
    private State state;
    public bool Disposed { get; private set; }

    public AdReward(string placement)
    {
        this.placement = placement;
        SetState(State.Stalled, forced: true);
    }

    ~AdReward()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (!Disposed)
        {
            OnFailed = null;
            OnReady = null;
            OnAdFinished = null;
            OnLoading = null;
            OnCancel = null;
            OnConfirmationFailed = null;
            if (Singleton<NetworkManager>.IsInstantiated())
            {
                Singleton<NetworkManager>.Instance.UnsubscribeFromResponse(HasInternet);
                Singleton<NetworkManager>.Instance.UnsubscribeFromResponse(PlayRewardVideo);
            }

            Disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    public void Stall()
    {
        if (Singleton<NetworkManager>.IsInstantiated())
        {
            Singleton<NetworkManager>.Instance.UnsubscribeFromResponse(HasInternet);
            Singleton<NetworkManager>.Instance.UnsubscribeFromResponse(PlayRewardVideo);
        }

        SetState(State.Stalled);
    }

    public void Play()
    {
        Singleton<GuiManager>.Instance.enabled = false;
        Singleton<NetworkManager>.Instance.CheckAccess(PlayRewardVideo);
    }

    public void Load()
    {
        if (AdvertisementHandler.IsAdvertisementReady(placement))
        {
            SetState(State.Ready);
        }
        else
        {
            SetState(State.Loading);
        }
    }

    private void SetState(State state, bool forced = false)
    {
        if (state == this.state && !forced)
        {
            return;
        }

        this.state = state;
        switch (state)
        {
            case State.Loading:
                OnLoading?.Invoke();
                StartLoading();
                break;
            case State.WaitingFail:
                CoroutineRunner.Instance.StartCoroutine(FailureWait());
                break;
            case State.Failed:
                OnFailed?.Invoke();
                break;
            case State.Ready:
                OnReady?.Invoke();
                break;
            case State.Finished:
                OnAdFinished?.Invoke();
                break;
            case State.Stalled:
                OnFailed?.Invoke();
                break;
            case State.Cancelled:
                OnCancel?.Invoke();
                break;
            case State.ConfirmationFailed:
                OnConfirmationFailed?.Invoke();
                break;
            case State.WaitingConfirmation:
                break;
        }
    }

    private void StartLoading()
    {
        if (Application.internetReachability != 0)
        {
            Singleton<NetworkManager>.Instance.CheckAccess(HasInternet);
        }
        else
        {
            SetState(State.Failed);
        }
    }

    private void HasInternet(bool internet)
    {
        if (state != State.Stalled)
        {
            if (!internet)
            {
                SetState(State.Failed);
            }
            else if (internet && state == State.Loading)
            {
                RefreshVideo();
            }
        }
    }

    public void RefreshVideo()
    {
        CoroutineRunner.Instance.StartCoroutine(CheckVideoTimeout());
    }

    private void PlayRewardVideo(bool canPlay)
    {
        if (canPlay)
        {
            OnAdPlayFailed?.Invoke();
        }
        else
        {
            OnAdPlayFailed?.Invoke();
            SetState(State.Failed);
        }

        Singleton<GuiManager>.Instance.enabled = true;
    }

    private IEnumerator DelayedAction(float delay, Action action)
    {
        if (action != null)
        {
            yield return new WaitForRealSeconds(delay);
            if (!Disposed)
            {
                action();
            }
        }
    }

    private IEnumerator CheckVideoTimeout()
    {
        float counter = 0f;
        while (counter <= 10f)
        {
            counter += Time.unscaledDeltaTime;
            if (AdvertisementHandler.IsAdvertisementReady(placement) || state == State.Stalled || Disposed)
            {
                break;
            }

            yield return null;
        }
    }

    private IEnumerator FailureWait()
    {
        float counter = 0f;
        while (counter <= 10f)
        {
            counter += Time.unscaledDeltaTime;
            if (state == State.Stalled || Disposed)
            {
                yield break;
            }

            yield return null;
        }

        if (state == State.WaitingFail)
        {
            SetState(State.Failed);
        }
    }
}