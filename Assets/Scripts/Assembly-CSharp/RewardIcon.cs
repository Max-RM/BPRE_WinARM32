using UnityEngine;

public class RewardIcon : MonoBehaviour
{
    public enum State
    {
        NotAvailable = 0,
        ClaimNow = 1,
        Claimed = 2
    }

    [SerializeField]
    public GameObject disabledSprite;
    [SerializeField]
    public GameObject claimNowSprite;
    public State ButtonState { get; private set; }

    public void Awake()
    {
        RefreshButtonImage();
    }

    public RewardIcon SetButtonState(State state)
    {
        ButtonState = state;
        RefreshButtonImage();
        return this;
    }

    private void RefreshButtonImage()
    {
        switch (ButtonState)
        {
            case State.Claimed:
                disabledSprite.SetActive(value: true);
                claimNowSprite.SetActive(value: false);
                break;
            case State.ClaimNow:
                disabledSprite.SetActive(value: false);
                claimNowSprite.SetActive(value: true);
                break;
            case State.NotAvailable:
                disabledSprite.SetActive(value: true);
                claimNowSprite.SetActive(value: false);
                break;
        }
    }
}