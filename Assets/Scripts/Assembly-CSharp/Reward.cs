using System.Collections.Generic;
using UnityEngine;

public class Reward : MonoBehaviour
{
    private const string GLUE_REWARD_PREFAB = "UI/Amazon/RewardSuperGlue";
    private const string MAGNET_REWARD_PREFAB = "UI/Amazon/RewardSuperMagnet";
    private const string MECHANIC_REWARD_PREFAB = "UI/Amazon/RewardSuperMechanic";
    private const string TURBO_REWARD_PREFAB = "UI/Amazon/RewardTurboCharger";
    private const string NIGHT_VISION_REWARD_PREFAB = "UI/Amazon/RewardNightVision";
    private const string BUNDLE_REWARD_PREFAB = "UI/Amazon/RewardBundle";
    [SerializeField]
    private Transform rewardPosition;
    [SerializeField]
    private GameObject rewardCount;
    [SerializeField]
    private GameObject claimedIcon;
    [SerializeField]
    private GameObject rewardBG;
    [SerializeField]
    private GameObject rewardBGLit;
    private Dictionary<PrizeType, GameObject> rewardPrefabs;
    public RewardIcon RewardIcon { get; private set; }
    public TextMesh RewardCount { get; private set; }

    private void Awake()
    {
        RewardCount = rewardCount.GetComponent<TextMesh>();
        UpdateBackground();
    }

    private void UpdateBackground(bool lit = false)
    {
        rewardBG.SetActive(!lit);
        rewardBGLit.SetActive(lit);
    }

    public void SetRewards(List<DailyReward> rewards)
    {
        if (RewardIcon != null)
        {
            Object.Destroy(RewardIcon.gameObject);
        }

        if (rewards.Count > 1)
        {
            RewardIcon = ((GameObject)Object.Instantiate(Resources.Load("UI/Amazon/RewardBundle"))).GetComponent<RewardIcon>();
            SetRewardCount(0);
        }
        else
        {
            if (rewards.Count <= 0)
            {
                return;
            }

            RewardIcon = Object.Instantiate(GetRewardPrefab(rewards[0].prize)).GetComponent<RewardIcon>();
            SetRewardCount(rewards[0].prizeCount);
        }

        RewardIcon.transform.parent = base.transform;
        RewardIcon.transform.localPosition = rewardPosition.localPosition;
        RewardIcon.SetButtonState(RewardIcon.State.NotAvailable);
    }

    public GameObject GetRewardPrefab(PrizeType prizeType)
    {
        if (rewardPrefabs == null)
        {
            rewardPrefabs = new Dictionary<PrizeType, GameObject>();
        }

        if (rewardPrefabs.ContainsKey(prizeType))
        {
            return rewardPrefabs[prizeType];
        }

        GameObject gameObject = ((prizeType == PrizeType.SuperGlue) ? Resources.Load("UI/Amazon/RewardSuperGlue") as GameObject : (prizeType == PrizeType.SuperMagnet) ? Resources.Load("UI/Amazon/RewardSuperMagnet") as GameObject : (prizeType == PrizeType.TurboCharge) ? Resources.Load("UI/Amazon/RewardTurboCharger") as GameObject : (prizeType == PrizeType.SuperMechanic) ? Resources.Load("UI/Amazon/RewardSuperMechanic") as GameObject : (prizeType == PrizeType.NightVision) ? Resources.Load("UI/Amazon/RewardNightVision") as GameObject : null);
        rewardPrefabs.Add(prizeType, gameObject);
        return gameObject;
    }

    public void SetState(RewardIcon.State newState)
    {
        if (!(RewardIcon == null))
        {
            RewardIcon.SetButtonState(newState);
            claimedIcon.SetActive(newState == RewardIcon.State.Claimed);
            RewardCount.gameObject.SetActive(newState != RewardIcon.State.Claimed);
            UpdateBackground(newState == RewardIcon.State.ClaimNow);
        }
    }

    public void SetDayText(string textKey, int day)
    {
    }

    public void SetRewardCount(int count, string prefix = "x")
    {
        if (!(RewardCount == null))
        {
            if (count > 0)
            {
                RewardCount.GetComponent<Renderer>().enabled = true;
                RewardCount.text = $"{prefix}{count}";
            }
            else
            {
                RewardCount.text = string.Empty;
                RewardCount.GetComponent<Renderer>().enabled = false;
            }
        }
    }
}