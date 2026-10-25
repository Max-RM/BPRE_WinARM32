using UnityEngine;

public class CurrencyParticleBurst : WPFMonoBehaviour
{
    [SerializeField]
    private IapManager.CurrencyType currencyType = IapManager.CurrencyType.SnoutCoin;
    [SerializeField]
    private int burstAmount = 1;
    [SerializeField]
    private float burstRate = 20f;
    [SerializeField]
    private bool selfDestruct = true;
    private SoftCurrencyButton parentButton;
    private void Start()
    {
        parentButton = ((currencyType == IapManager.CurrencyType.Scrap) ? ScrapButton.Instance : (currencyType == IapManager.CurrencyType.SnoutCoin) ? SnoutButton.Instance : parentButton);
        if (burstAmount <= 0 || parentButton == null || parentButton.CurrencyEffect == null)
        {
            CheckDestroy();
        }
    }

    private void CheckDestroy()
    {
        if (selfDestruct)
        {
            Object.Destroy(base.gameObject);
        }
    }

    public void Burst()
    {
        if (!(parentButton == null) && !(parentButton.CurrencyEffect == null))
        {
            burstRate = Mathf.Clamp(burstRate, 1f, float.PositiveInfinity);
            float num = 0f;
            for (int i = 0; i < burstAmount; i++)
            {
                parentButton.CurrencyEffect.AddParticle(base.transform.position, Random.insideUnitCircle.normalized * Random.Range(20f, 25f), num);
                num += 1f / burstRate;
            }

            CheckDestroy();
        }
    }

    public void SetBurst(int newAmount, float newRate, bool burstNow = true)
    {
        burstAmount = newAmount;
        burstRate = newRate;
        if (burstNow)
        {
            Burst();
        }
    }
}