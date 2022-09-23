using System.Linq;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;

public class PointDeskDeputyView : MonoBehaviour
{
    [SerializeField] private DeputyBtnUi[] deputies;
    [SerializeField] private EnlistBtnUi enlistBtn;
    private event UnityAction OnEnlistAction;
    private event UnityAction<GameCard> OnRecallFromDeputy;
    public void Init(UnityAction<int, GameCard> onClickDeputyAction, 
        UnityAction onEnlistAction,
        UnityAction<GameCard> onRecallFromDeputy)
    {
        for (var i = 0; i < deputies.Length; i++)
        {
            var ui = deputies[i];
            var index = i;
            ui.Init(() => onClickDeputyAction(index, ui.DeputyCard));
        }
        OnEnlistAction = onEnlistAction;
        OnRecallFromDeputy = onRecallFromDeputy;
    }

    public void UpdateCardUi(GameCard card)
    {
        if (card == null) return;
        foreach (var ui in deputies) ui.SetMode(DeputyBtnUi.Modes.Locked);
        var deputyList = PlayerDataForGame.instance.hstData.heroSaveData.GetDeputyIds();
        var isHero = card.Type == (int)GameCardType.Hero;
        var isDeputy = isHero && deputyList.Contains(card.CardId);
        if (isDeputy)
        {
            enlistBtn.SetOnClick(()=>OnRecallFromDeputy?.Invoke(card));
            enlistBtn.SetRecall();
            return;
        }

        enlistBtn.SetOnClick(OnEnlistAction);
        enlistBtn.SetEnlist(card);
        var acceptDeputy = false;
        if (isHero) acceptDeputy = DataTable.Hero[card.CardId].Arousable > 0;

        if (!acceptDeputy || card.Arouse <= 1)
        {
            SetNoDeputy();
            return;
        }

        var deputyIndexes = card.Arouse - 1;
        var list = new[]
        {
            GetCard(card.Deputy1Id),//0
            GetCard(card.Deputy2Id),//1
            GetCard(card.Deputy3Id),//2
            GetCard(card.Deputy4Id)//3
        };
        for (int i = 0; i < deputyIndexes; i++) //如果已经觉醒
        {
            var deputyUi = deputies[i]; //副将ui
            var deputyCard = list[i];
            if (deputyCard == null) //如果没有副将
                deputyUi.SetMode(DeputyBtnUi.Modes.Ready);
            else deputyUi.SetMode(DeputyBtnUi.Modes.Assigned, list[i]);
        }

        GameCard GetCard(int cardId) =>
            PlayerDataForGame.instance.hstData.heroSaveData.FirstOrDefault(h => h.CardId == cardId);
    }


    private void SetNoDeputy()
    {
        foreach (var ui in deputies) ui.SetMode(DeputyBtnUi.Modes.Locked);
    }
}