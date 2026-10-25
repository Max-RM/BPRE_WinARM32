using UnityEngine;

public class InGameGUI : MonoBehaviour
{
    public GameObject buildMenuPrefab;
    public GameObject flightMenuPrefab;
    public GameObject previewMenuPrefab;
    public GameObject pauseMenuPrefab;
    public GameObject levelCompleteMenuPrefab;
    public GameObject tutorialBookMenuPrefab;
    public GameObject mechanicGiftScreenPrefab;
    public GameObject cakeRaceCompleteMenuPrefab;
    private GameObject buildMenuGo;
    private GameObject flightMenuGo;
    private GameObject previewMenuGo;
    private GameObject pauseMenuGo;
    private GameObject levelCompleteMenuGo;
    private GameObject tutorialBookMenuGo;
    private GameObject mechanicGiftScreenGo;
    private GameObject cakeRaceCompleteMenuGo;
    private CakeRaceComplete cakeRaceCompleteMenu;
    private GameObject currentMenu;
    public InGameBuildMenu BuildMenu { get; private set; }
    public InGameFlightMenu FlightMenu { get; private set; }
    public PreviewMenu PreviewMenu { get; private set; }
    public PausePage PauseMenu { get; private set; }
    public LevelComplete LevelCompleteMenu { get; private set; }
    public TutorialBook TutorialBook { get; private set; }
    public InGameMechanicGift MechanicGiftScreen { get; private set; }
    public CakeRaceHUD CakeRaceHUD { get; private set; }

    private void Awake()
    {
        buildMenuGo = InstantiateMenu(buildMenuPrefab);
        flightMenuGo = InstantiateMenu(flightMenuPrefab);
        previewMenuGo = InstantiateMenu(previewMenuPrefab);
        pauseMenuGo = InstantiateMenu(pauseMenuPrefab);
        levelCompleteMenuGo = InstantiateMenu(levelCompleteMenuPrefab);
        tutorialBookMenuGo = InstantiateMenu(tutorialBookMenuPrefab);
        mechanicGiftScreenGo = InstantiateMenu(mechanicGiftScreenPrefab);
        cakeRaceCompleteMenuGo = InstantiateMenu(cakeRaceCompleteMenuPrefab);
        BuildMenu = buildMenuGo.GetComponent<InGameBuildMenu>();
        FlightMenu = flightMenuGo.GetComponent<InGameFlightMenu>();
        PreviewMenu = previewMenuGo.GetComponent<PreviewMenu>();
        PauseMenu = pauseMenuGo.GetComponent<PausePage>();
        LevelCompleteMenu = levelCompleteMenuGo.GetComponent<LevelComplete>();
        TutorialBook = tutorialBookMenuGo.GetComponent<TutorialBook>();
        MechanicGiftScreen = mechanicGiftScreenGo.GetComponent<InGameMechanicGift>();
        cakeRaceCompleteMenu = cakeRaceCompleteMenuGo.GetComponent<CakeRaceComplete>();
        CakeRaceHUD = flightMenuGo.GetComponentInChildren<CakeRaceHUD>();
    }

    private GameObject InstantiateMenu(GameObject prefab)
    {
        GameObject obj = Object.Instantiate(prefab, base.transform, true);
        obj.name = prefab.name;
        obj.transform.position = base.transform.position;
        obj.SetActive(value: false);
        return obj;
    }

    private void OnEnable()
    {
        EventManager.Connect<GameStateChanged>(ReceiveGameStateChangedEvent);
    }

    private void OnDisable()
    {
        EventManager.Disconnect<GameStateChanged>(ReceiveGameStateChangedEvent);
    }

    private void ReceiveGameStateChangedEvent(GameStateChanged data)
    {
        switch (data.state)
        {
            case LevelManager.GameState.Building:
                SetMenu(buildMenuGo);
                break;
            case LevelManager.GameState.PreviewWhileBuilding:
                SetMenu(previewMenuGo);
                break;
            case LevelManager.GameState.PreviewWhileRunning:
                SetMenu(previewMenuGo);
                break;
            case LevelManager.GameState.Running:
            case LevelManager.GameState.Continue:
                SetMenu(flightMenuGo);
                break;
            case LevelManager.GameState.Completed:
                SetMenu(levelCompleteMenuGo);
                break;
            case LevelManager.GameState.PausedWhileRunning:
                SetMenu(pauseMenuGo);
                break;
            case LevelManager.GameState.PausedWhileBuilding:
                SetMenu(pauseMenuGo);
                break;
            case LevelManager.GameState.TutorialBook:
                SetMenu(tutorialBookMenuGo);
                break;
            case LevelManager.GameState.ShowingUnlockedParts:
                SetMenu(buildMenuGo);
                break;
            case LevelManager.GameState.MechanicGiftScreen:
                SetMenu(mechanicGiftScreenGo);
                break;
            case LevelManager.GameState.LootCrateOpening:
                SetMenu(null);
                break;
            case LevelManager.GameState.CakeRaceCompleted:
                SetMenu(cakeRaceCompleteMenuGo);
                break;
            case LevelManager.GameState.Preview:
            case LevelManager.GameState.PreviewMoving:
            case LevelManager.GameState.IngamePurchase:
            case LevelManager.GameState.AutoBuilding:
            case LevelManager.GameState.Purchasing:
            case LevelManager.GameState.Snapshot:
            case LevelManager.GameState.MechanicInfoScreen:
            case LevelManager.GameState.SuperAutoBuilding:
            case LevelManager.GameState.CustomizingPart:
            case LevelManager.GameState.CakeRaceExploding:
                break;
        }
    }

    private void SetMenu(GameObject menu)
    {
        if (!(menu == currentMenu))
        {
            ShowCurrentMenu(show: false);
            currentMenu = menu;
            if ((bool)currentMenu)
            {
                currentMenu.SetActive(value: true);
            }
        }
    }

    public void ShowCurrentMenu(bool show = true)
    {
        if ((bool)currentMenu)
        {
            currentMenu.SetActive(show);
        }
    }

    public void Hide()
    {
        base.gameObject.SetActive(value: false);
    }

    public void Show()
    {
        base.gameObject.SetActive(value: true);
    }
}