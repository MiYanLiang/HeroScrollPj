using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

[Serializable]public class FightCardData : ICardDrag
{
    //单位id,为0表示null
    public int unitId;

    [JsonIgnore]public WarGameCardUi cardObj;
    //卡牌obj
    //public GameObject cardObj;
    //卡牌类型
    public int cardType;
    //卡牌id
    public int cardId;
    //等级
    public int cardGrade;
    //伤害
    public int damage;
    //满血
    public int fullHp;
    //当前血量
    public int nowHp;
    //战斗状态
    [JsonIgnore]public FightState fightState;
    //摆放位置记录
    public int posIndex = -1;
    //生命值回复
    public int hpr;
    //主被动单位
    public bool activeUnit;
    //此回合是否行动
    public bool isActionDone;
    //是否是玩家卡牌
    public bool isPlayerCard;
    /// <summary>
    /// 单位伤害类型0物理，1法术
    /// </summary>
    public int cardDamageType;
    /// <summary>
    /// 单位行动类型0近战，1远程
    /// </summary>
    public int cardMoveType;
    /// <summary>
    /// 被攻击者的行为，0受击，1防护盾，2闪避，3护盾，4无敌
    /// </summary>
    public int attackedBehavior;

    public FightCardData()
    {
        
    }
    [JsonIgnore]public bool IsNonGeneralDamage => cardObj.CardInfo.IsNonGeneralDamage;
    public int PosIndex => posIndex;
    public void UpdatePos(int pos) => posIndex = pos;
    //更新血条显示
    public void UpdateHpUi()
    {
        var isLose = nowHp <= 0;

        if (isLose)
        {
            nowHp = 0;
            UpdateHp();
            cardObj.SetLose(true);
            return;
        }
        if (nowHp > fullHp) nowHp = fullHp;
        UpdateHp();
        void UpdateHp() => cardObj.War.SetHp(1f * nowHp / fullHp);
    }
}

[Serializable]public class CardRound
{
    public int Hp { get; set; }
    [JsonProperty("H")]public CardHit[] Hits { get; set; }
    [JsonProperty("S")]public int[,] States { get; set; }
}

[Serializable]public class CardHit
{
    [JsonProperty("P")]public int Pos { get; set; }
    [JsonProperty("D")]public Damage Damage { get; set; }
    [JsonProperty("C")]public Damage Counter { get; set; }
}

[Serializable]public class Damage
{
    [JsonProperty("M")]public int IsMagic { get; set; }
    [JsonProperty("T")]public int Type { get; set; }
    [JsonProperty("D")]public int[] Damages { get; set; }
}