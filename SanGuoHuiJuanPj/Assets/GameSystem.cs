﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameSystem : MonoBehaviour
{
    public enum GameScene
    {
        StartScene,
        MainScene,
        WarScene
    }

    public static GameScene CurrentScene { get; private set; }

    public static GameSystem instance;
    public static LoginUiController LoginUi { get; private set; }
    public LoginUiController loginUiController;
    public static TimeSystemControl TimeSystemControl { get; private set; }
    public TimeSystemControl timeSystemControl;
    public PlayerDataForGame playerDataForGame;
    private List<UnityAction> SceneLoadActions { get; } = new List<UnityAction>();

    public static GameResources GameResources { get; private set; }
    public Canvas systemCanvas;

    void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
        LoginUi = loginUiController;
        TimeSystemControl = timeSystemControl;
    }

    void Start()
    {
        SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;
        GameResources = new GameResources();
        GameResources.Init();
    }

    public static void InitGameDependencyComponents()
    {
        TimeSystemControl.Init();
    }

    private void SceneManagerOnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CurrentScene = (GameScene) scene.buildIndex;
        if(SceneLoadActions.Count == 0)return;
        SceneLoadActions.ForEach(a=>a?.Invoke());
        SceneLoadActions.Clear();
    }

    public void RegNextSceneLoadAction(UnityAction action) => SceneLoadActions.Add(action);

}
