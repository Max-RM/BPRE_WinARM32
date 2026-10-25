using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class INInitializer : MonoBehaviour
{
    private const string PortSplashResource = "innovation/Innovation_Background_WinARM32";

    [SerializeField]
    private List<GameObject> m_splashes;
    [SerializeField]
    private List<GameObject> m_prefabs;
    [SerializeField]
    private ResourceData m_resourceData;
    private bool m_useAlphaAnimation;
    private float m_time;
    public bool Initialized { get; private set; }

    private void Awake()
    {
        m_useAlphaAnimation = true;
        m_time = 3f;
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        for (int i = 0; i < m_splashes.Count; i++)
        {
            GameObject splash = Object.Instantiate(m_splashes[i], Vector3.zero, Quaternion.identity);
            ApplyPortSplash(splash);
            yield return PlayAnimation(splash);
            Object.Destroy(splash);
        }

        INUnity.Initialize(m_resourceData);
        foreach (GameObject prefab in m_prefabs)
        {
            Object.Instantiate(prefab);
        }

        while (!INSettings.VersionSelected)
        {
            yield return null;
        }

        Initialized = true;
        yield return LoadMainMenu();
    }

    private IEnumerator LoadMainMenu()
    {
        while (!SingletonSpawner.SpawnDone)
        {
            yield return null;
        }

        while (!Bundle.initialized || Bundle.checkingBundles || !Singleton<GameConfigurationManager>.Instance.HasData)
        {
            yield return null;
        }

        PostInitialize();
        Singleton<GameManager>.Instance.LoadMainMenu(showLoadingScreen: true);
    }

    private void PostInitialize()
    {
        if (INSettings.GetBool(INFeature.RuntimeGameData))
        {
            Object.Instantiate(INUnity.LoadGameObject("INRuntimeGameData"));
        }

        if (INSettings.GetBool(INFeature.ApplicationInterface))
        {
            Object.Instantiate(INUnity.LoadGameObject("INApplicationInterface"));
        }
    }

    // The Windows ARM32 build is the only 32-bit target of this port, so it shows a
    // splash screen that credits the port instead of the original artwork.
    private static void ApplyPortSplash(GameObject splash)
    {
        if (System.IntPtr.Size != 4)
        {
            return;
        }

        RawImage image = splash.GetComponentInChildren<RawImage>();
        if (image == null || image.texture == null || !image.texture.name.StartsWith("Innovation_Background"))
        {
            return;
        }

        Texture2D portSplash = Resources.Load<Texture2D>(PortSplashResource);
        if (portSplash != null)
        {
            image.texture = portSplash;
        }
    }

    private IEnumerator PlayAnimation(GameObject gameObject)
    {
        if (!m_useAlphaAnimation)
        {
            yield return new WaitForSeconds(m_time);
            yield break;
        }

        CanvasRenderer canvasRenderer = gameObject.GetComponentInChildren<CanvasRenderer>();
        if (canvasRenderer != null)
        {
            yield return canvasRenderer.PlayFadeInAnimation(m_time / 3f);
            yield return new WaitForSeconds(m_time / 3f);
            yield return canvasRenderer.PlayFadeOutAnimation(m_time / 3f);
        }
    }
}