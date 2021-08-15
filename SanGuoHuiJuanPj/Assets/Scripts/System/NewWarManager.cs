using System;
using Assets.System.WarModule;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class NewWarManager : MonoBehaviour
{
    private const string ButtonTrigger = "isShow";
    #region UnityFields

    public Button StartButton;
    public ChessPos[] PlayerPoses;
    public ChessPos[] EnemyPoses;
    public ChessCard[] Player;
    public ChessCard[] Enemy;
    public UnityAction OnButtonClick;
    #endregion

    public ChessGrid Grid;
    public ChessOperatorManager ChessOperator;
    void Start()
    {
        Grid = new ChessGrid(PlayerPoses, EnemyPoses);
        ChessOperator = new ChessOperatorManager(true, Grid);
    }

    public void StartButtonShow(bool show)
    {
        StartButton.GetComponent<Animator>().SetBool(ButtonTrigger, show);
        StartButton.interactable = show;
    }

    [Serializable]
    public class ChessCard
    {
        public int Id;
        public int Pos;
        public GameCardType Type;
        public int Level = 1;
    }

}