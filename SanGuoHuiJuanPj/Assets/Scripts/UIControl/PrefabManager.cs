﻿using UnityEngine;

public class PrefabManager : MonoBehaviour
{
    public static PrefabManager Instance { get; private set; }

    #region Props
    [SerializeField] GameCardUi gameCardUi;
    [SerializeField] WarGameCardUi warGameCardUi;
    public static GameCardUi GameCardUi { get; private set; }
    public static WarGameCardUi WarGameCardUi { get; private set; }
    #endregion

    public void Init()
    {
        Instance = this;
        GameCardUi = gameCardUi;
        WarGameCardUi = warGameCardUi;
    }

    public static GameCardUi NewGameCardUi(Transform parent) => Instantiate(GameCardUi, parent);
    public static WarGameCardUi NewWarGameCardUi(Transform parent) => Instantiate(WarGameCardUi, parent);

}