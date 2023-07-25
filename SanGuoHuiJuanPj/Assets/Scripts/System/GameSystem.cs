using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.HttpUnitScripts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameSystem : MonoBehaviour
{
    public const string UnTagged = "Untagged";
    public const string PyCard = "PyCard";
    public const string CardPos = "CardPos";

    public enum GameScene
    {
        PreloadScene,
        StartScene,
        MainScene,
        WarScene
    }

    public static GameScene CurrentScene { get; private set; }

    public static GameSystem Instance { get; private set; }
    public static LoginUiController LoginUi { get; private set; }
    public static TimeSystemControl TimeSystemControl { get; private set; }
    public static Configuration Configuration { get; private set; }
    public static GameResources GameResources { get; private set; }
    public static bool IsInit { get; private set; }
    public static MapService MapService { get; private set; }
    [Header("强制播放过场")]public bool ForcePlayStory = false;

    #region Reference Fields
    public LoginUiController loginUiController;
    public TimeSystemControl timeSystemControl;
    public PlayerDataForGame playerDataForGame;
    public Configuration configuration;
    public DataTable dataTable;
    public PrefabManager prefabManager;
    public AdManager adManager;
    public Canvas systemCanvas;
    public ServerRequestExceptionWindow ServerException;
    #endregion
    private static ServerRequestExceptionWindow _serverException;
    //method properties/fields
    private List<UnityAction> SceneLoadActions { get; } = new List<UnityAction>();
    public static UnityEvent OnWarSceneInit = new UnityEvent();
    public static UnityEvent OnMainSceneInit = new UnityEvent();
    private Queue<Func<bool>> InitQueue;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public virtual void Init()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Configuration = configuration;
        LoginUi = loginUiController;
        TimeSystemControl = timeSystemControl;
        SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;

        //InitEnqueue(dataTable.Init);
        Parallel.Invoke(dataTable.Init);

        InitQueue = new Queue<Func<bool>>();

        InitEnqueue(Configuration.Init);
        InitEnqueue(() =>
        {
            GameResources = new GameResources();
            GameResources.Init();
        });
        InitEnqueue(prefabManager.Init);
        //InitEnqueue(adManager.Init); // 广告初始化
        InitEnqueue(() => AudioController0.instance.MusicSwitch(GamePref.PrefMusicPlay));
        InitEnqueue(() => AudioController1.instance.MusicSwitch(GamePref.PrefMusicPlay));
        InitEnqueue(() =>
        {
            MapService = new MapService();
            playerDataForGame.Init(() => IsInit = true);
            _serverException = ServerException;
            ServerException.Init();
        });
        StartCoroutine(InitCo());
    }

    IEnumerator InitCo()
    {
        while (InitQueue.Count > 0)
        {
            var action = InitQueue.Dequeue();
            yield return new WaitUntil(action.Invoke);
        }
    }

    // 当登录成功后，初始化游戏依赖组件
    public static void OnLoggedInInitGameDependencyComponents()
    {
        Instance.adManager.Init(); // 广告初始化换到这里
        TimeSystemControl.Init();
    }
#if UNITY_EDITOR
    public void SetScene(GameScene scene) => CurrentScene = scene;
#endif
    private void InitScene(GameScene scene)
    {
        CurrentScene = scene;
        switch (CurrentScene)
        {
            case GameScene.StartScene:
                OnStartSceneLoaded();
                break;
            case GameScene.MainScene:
                OnMainSceneLoaded();
                break;
            case GameScene.WarScene:
                OnWarSceneLoaded();
                break;
            case GameScene.PreloadScene:
                //OnBeginSceneLoaded();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void SceneManagerOnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitScene((GameScene) scene.buildIndex);
        if(SceneLoadActions.Count == 0)return;
        SceneLoadActions.ForEach(a=>a?.Invoke());
        SceneLoadActions.Clear();
    }

    private void OnWarSceneLoaded()
    {
        WarsUIManager.instance.Init();
        OnWarSceneInit.Invoke();
        OnWarSceneInit?.RemoveAllListeners();
    }

    public bool DisplayStaminaUiChangeEffect { get; set; }


    private void OnMainSceneLoaded()
    {
        UIManager.instance.Init(true);
        if (DisplayStaminaUiChangeEffect)
        {
            UIManager.instance.DelayInvokeReturnStaminaUi();
            DisplayStaminaUiChangeEffect = false;
        }
        OnMainSceneInit.Invoke();
        OnMainSceneInit?.RemoveAllListeners();
    }

    private bool isFirstStartScene = true;
    private void OnStartSceneLoaded()
    {
        if(isFirstStartScene)
        {
            AudioController0.instance.ForcePlayAudio(12);
            isFirstStartScene = false;
        }
        StartSceneUIManager.instance.Init();
        StartSceneToServerCS.instance.Init();
        loginUiController.Init();
    }

    protected void InitEnqueue(Action action)
    {
        InitQueue.Enqueue(Func);
        bool Func()
        {
            action.Invoke();
            return true;
        }
    }

    public void RegNextSceneLoadAction(UnityAction action) => SceneLoadActions.Add(action);

    public void BeginAllOnlineServices()
    {
        MapService.Init();
    }

    public static void ServerRequestException(string title, string detail) => _serverException.ShowError(title, detail);
}
