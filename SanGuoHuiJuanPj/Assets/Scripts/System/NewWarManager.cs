﻿using System;
using System.Collections.Generic;
using System.Linq;
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
    #endregion

    public ChessGrid Grid;
    public ChessOperatorManager<FightCardData> ChessOperator;
    public Dictionary<int, FightCardData> CardData { get; } = new Dictionary<int, FightCardData>();

    public void Init()
    {
        Grid = new ChessGrid(PlayerPoses, EnemyPoses);
        ChessOperator = new ChessOperatorManager<FightCardData>(Grid);
        RegCards(Player, true);
        RegCards(Enemy, false);

        void RegCards(ChessCard[] list, bool isChallenger)
        {
            foreach (var chessCard in list)
            {
                var card = new FightCardData(GameCard.Instance(chessCard.Id, (int)chessCard.Type, chessCard.Level));
                card.UpdatePos(chessCard.Pos);
                card.isPlayerCard = isChallenger;
                card = ChessOperator.RegOperator(card);
                CardData.Add(card.InstanceId, card);
            }
        }
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