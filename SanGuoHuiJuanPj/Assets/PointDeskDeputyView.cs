using System.Linq;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;

public class PointDeskDeputyView : MonoBehaviour
{
    [SerializeField] private DeputyBtnUi[] deputies;
    [SerializeField] private EnlistBtnUi enlistBtn;

    public void Init(UnityAction<int> onClickDeputyAction,UnityAction onEnlistAction)
    {
        for (var i = 0; i < deputies.Length; i++)
        {
            var ui = deputies[i];
            var index = i;
            ui.Init(() => onClickDeputyAction(index));
        }
        enlistBtn.Init(onEnlistAction);
    }

    public void UpdateCardUi(GameCard card)
    {
        var isEnlisted = card.IsFight != 0;
        enlistBtn.Set(!isEnlisted);
        var isHero = card.Type == (int)GameCardType.Hero;
        var isArousable = false;
        if (isHero)
            isArousable = DataTable.Hero[card.CardId].Arousable > 0;
        if (!isArousable)
        {
            SetNotArousable();
            return;
        }
        var arouseLevel = card.Arouse;
        var list = new[]
        {
            GetCard(card.Deputy1Id),
            GetCard(card.Deputy2Id),
            GetCard(card.Deputy3Id),
            GetCard(card.Deputy4Id)
        };

        for (int i = 0; i < arouseLevel; i++) //如果已经觉醒
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


    private void SetNotArousable()
    {
        foreach (var ui in deputies) ui.Display(false);
    }
}