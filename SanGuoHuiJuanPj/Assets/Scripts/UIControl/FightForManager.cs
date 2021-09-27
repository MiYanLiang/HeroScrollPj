using Beebyte.Obfuscator;
using CorrelateLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.System.WarModule;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class FightForManager : MonoBehaviour
{
    public static FightForManager instance;
    [SerializeField] private Chessboard chessboard;//棋盘
    GameObject enemyCardPosPre;    //敌方卡牌位置预制件
    [SerializeField]
    GameObject playerCardPosPre;    //我方卡牌位置预制件
    //public Transform enemyCardsBox;    //敌方卡牌父级
    //public Transform playerCardsBox;    //我方卡牌父级
    [SerializeField]
    GameObject fightCardPre;    //战斗卡牌预制件
    [SerializeField]
    Text heroNumText;
    [SerializeField]
    WarGameCardUi homeCardObj;

    public GameObject stateIconPre;

    public List<GameObject> enemyCardsPos; //敌方卡牌位置列表
    public List<GameObject> playerCardsPos; //我方卡牌位置列表

    //public IReadOnlyList<FightCardData> EnemyCards => enemyCards; //敌方战斗卡牌信息集合
    //public IReadOnlyList<FightCardData> PlayerCards => playerCards; //我方战斗卡牌信息集合

    [HideInInspector]
    public float floDisY;  //加成比
    [HideInInspector]
    public float oneDisY;  //半格高

    [SerializeField]
    GridLayoutGroup gridLayoutGroup;

    public int battleIdIndex;   //战役编号索引

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        //enemyCards = new FightCardData[20];
        //playerCards = new FightCardData[20];
        //nowHeroNums = 0;
        isNeedAutoHideInfo = true;
        Input.multiTouchEnabled = false;    //限制多指拖拽
    }

    public void Init()
    {
        oneDisY = Screen.height / (1920 / gridLayoutGroup.cellSize.y) / 9;
        float xFlo = (1920f / 1080f) / ((float)Screen.height / Screen.width);
        floDisY = 2 * oneDisY * xFlo;

        //玩家羁绊集合初始化
        CardManager.ResetJiBan(FightController.instance.playerJiBanAllTypes);

        CreatePlayerHomeCard();
    }
    [SkipRename]
    private void CreatePlayerHomeCard()
    {
        var playerBase = new FightCardData();
        playerBase.cardObj = Instantiate(homeCardObj);
        playerBase.ResetHp(DataTable.BaseLevel[WarsUIManager.instance.cityLevel].BaseHp + DataTable.PlayerLevelConfig[PlayerDataForGame.instance.pyData.Level].BaseHpAddOn);
        playerBase.hpr = 0;
        playerBase.cardType = 522;
        playerBase.isPlayerCard = true;
        playerBase.activeUnit = false;
        playerBase.CardState = new CardState();
        chessboard.PlaceCard(17, playerBase);
        UpdateFightNumTextShow(WarsUIManager.instance.maxHeroNums);
    }

    /// <summary>
    /// 初始化敌方卡牌到战斗位上
    /// </summary>

    //主动塔行动
    public void ActiveTowerFight(FightCardData cardData, bool isPlayer)
    {
        switch (cardData.cardId)
        {
            case 0://营寨
                YingZhaiFun(cardData, isPlayer);
                break;
            case 1://投石台
                TouShiTaiAttackFun(cardData);
                break;
            case 2://奏乐台
                ZouYueTaiAddtionFun(cardData, isPlayer);
                break;
            case 3://箭楼
                FightController.instance.JianLouYuanSheSkill(cardData, GetTowerAddValue(cardData.cardId, cardData.cardGrade));
                break;
            case 6://轩辕台
                XuanYuanTaiAddtionFun(cardData, isPlayer);
                break;
            default:
                break;
        }
    }

    public void NeighborsLoop(int pos,UnityAction<int> function)
    {
        var neighbors = chessboard.GetNeighborIndexes(pos);
        for (var i = 0; i < neighbors.Length; i++) function(neighbors[i]);
    }

    public FightCardData[] GetNeighbors(int pos, bool isPlayer, Func<FightCardData, bool> function = null)
    {
        if(function==null)return chessboard.GetNeighborIndexes(pos).Select(i => GetCard(i, isPlayer)).ToArray();
        return chessboard.GetNeighborIndexes(pos).Where(i => function(GetCard(i, isPlayer)))
            .Select(i => GetCard(i, isPlayer)).ToArray();
    }

    public FightCardData[] GetFriendlyNeighbors(int pos,FightCardData card, Func<FightCardData, bool> func = null) => GetNeighbors(pos, card.isPlayerCard, func);

    //营寨行动
    private void YingZhaiFun(FightCardData card, bool isPlayer)
    {
        if (card.Hp <= 1) return;
        FightController.instance.indexAttackType = 0;
        FightController.instance.PlayAudioForSecondClip(42, 0);

        var mostHpRequest = 0;  //最多扣除血量
        var needAddHpCard = new FightCardData();
        //var neighbors = chessboard.GetNeighborIndexes(card.posIndex);
        NeighborsLoop(card.PosIndex, i =>
        {
            var target = GetCardList(isPlayer)[i];
            if (target == null || target.cardType != 0 || target.Hp.Value <= 0) return;
            if (target.Hp.Max - target.Hp.Value <= mostHpRequest) return;//找出最缺血的单位
            mostHpRequest = target.Hp.Max - target.Hp.Value;
            needAddHpCard = target;
        });

        if (mostHpRequest <= 0) return;//没有单位缺血
        var maxAddingHp = card.Hp - 1;
        if (maxAddingHp > mostHpRequest) maxAddingHp = mostHpRequest;
        //自身减血
        card.Hp.Add(-maxAddingHp);
        FightController.instance.TargetAnimShow(card, maxAddingHp);
        //单位加血
        FightController.instance.AttackToEffectShow(needAddHpCard, false, Effect.Heal42A);
        needAddHpCard.Hp.Add(maxAddingHp);
        FightController.instance.ShowSpellTextObj(needAddHpCard.cardObj, DataTable.GetStringText(15), true, false);
        FightController.instance.TargetAnimShow(needAddHpCard, maxAddingHp);
    }

    //投石台攻击
    private void TouShiTaiAttackFun(FightCardData cardData)
    {
        var targets = GetCardList(!cardData.isPlayerCard);
        var damage = (int)(DataTable.GetGameValue(122) / 100f * GetTowerAddValue(cardData.cardId, cardData.cardGrade)); //造成的伤害
        FightController.instance.indexAttackType = 0;

        FightController.instance.PlayAudioForSecondClip(24, 0);

        var posListToThunder = cardData.isPlayerCard ? enemyCardsPos : playerCardsPos;
        EffectsPoolingControl.instance.GetEffectToFight1("101A", 1f, posListToThunder[cardData.posIndex].transform);

        if (targets[cardData.posIndex] != null && targets[cardData.posIndex].Hp > 0)
        {
            var finalDamage = FightController.instance.DefDamageProcessFun(cardData, targets[cardData.posIndex], damage);
            targets[cardData.posIndex].Hp.Add(-finalDamage);
            FightController.instance.TargetAnimShow(targets[cardData.posIndex], finalDamage);
            if (targets[cardData.posIndex].cardType == 522 && targets[cardData.posIndex].Hp <= 0)
            {
                FightController.instance.recordWinner = targets[cardData.posIndex].isPlayerCard ? -1 : 1;
            }
        }
        NeighborsLoop(cardData.PosIndex, i =>
        {
            var target = targets[i];
            if (target == null || target.Hp <= 0) return;
            var finalDamage = FightController.instance.DefDamageProcessFun(cardData, target, damage);
            target.Hp.Add(-finalDamage);
            FightController.instance.TargetAnimShow(target, finalDamage);
            if (target.cardType != 522 || target.Hp > 0) return;
            FightController.instance.recordWinner = target.isPlayerCard ? -1 : 1;
        });
    }

    //奏乐台血量回复
    private void ZouYueTaiAddtionFun(FightCardData cardData, bool isPlayer)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);    //回复血量基值
        addtionNums = (int)(addtionNums * DataTable.GetGameValue(123) / 100f);
        FightController.instance.indexAttackType = 0;
        FightController.instance.PlayAudioForSecondClip(42, 0);
        NeighborsLoop(cardData.PosIndex, i =>
        {
            FightCardData addedFightCard = GetCardList(isPlayer)[i];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
            {
                FightController.instance.AttackToEffectShow(addedFightCard, false, Effect.Heal42A);
                addedFightCard.Hp.Add(addtionNums);
                FightController.instance.ShowSpellTextObj(addedFightCard.cardObj, DataTable.GetStringText(15), true, false);
                FightController.instance.TargetAnimShow(addedFightCard, addtionNums);
            }
        });

    }

    //轩辕台护盾加成
    private void XuanYuanTaiAddtionFun(FightCardData cardData, bool isPlayer)
    {
        FightController.instance.PlayAudioForSecondClip(4, 0);
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);    //添加护盾的单位最大数
        NeighborsLoop(cardData.PosIndex, i =>
        {
            var addedFightCard = GetCardList(isPlayer)[i];
            if (addedFightCard == null ||
                addedFightCard.Hp <= 0 ||
                addedFightCard.cardType != 0 ||
                addedFightCard.CardState.Shield > 0 ||
                addtionNums <= 0) return;
            FightController.instance.AttackToEffectShow(addedFightCard, false, Effect.Shield4A);
            addedFightCard.CardState.Shield = 1;
            CreateSateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_withStand, true);
            addtionNums--;
        });
    }

    //武将卡牌移动消除附加状态
    private void HeroCardRemoveStatus(FightCardData cardData)
    {
        //奏乐台图标
        {
            Transform tranZyt = cardData.cardObj.War.StateContent.Find(StringNameStatic.StateIconPath_zouyuetaiAddtion);
            if (tranZyt != null)
                DestroyImmediate(tranZyt.gameObject);
        }
        //战斗力图标
        if (cardData.CardState.StrengthUp > 0)
        {
            cardData.CardState.StrengthUp = 0;
            DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
        }
        //风神台图标
        if (cardData.CardState.DodgeUp > 0)
        {
            cardData.CardState.DodgeUp = 0;
            DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_fengShenTaiAddtion, false);
        }
        //霹雳台图标
        if (cardData.CardState.CriticalUp > 0)
        {
            cardData.CardState.CriticalUp = 0;
            DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_pilitaiAddtion, false);
        }
        //霹雳台图标
        if (cardData.CardState.CriticalUp > 0)
        {
            cardData.CardState.CriticalUp = 0;
            DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_pilitaiAddtion, false);
        }
        //狼牙台图标
        if (cardData.CardState.RouseUp > 0)
        {
            cardData.CardState.RouseUp = 0;
            DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_langyataiAddtion, false);
        }
        //烽火台图标
        if (cardData.CardState.FengHuoTaiAddOn > 0)
        {
            cardData.CardState.FengHuoTaiAddOn = 0;
            DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_fenghuotaiAddtion, false);
        }
        //迷雾阵图标
        if (cardData.CardState.MiWuZhenAddOn > 0)
        {
            cardData.CardState.MiWuZhenAddOn = 0;
            DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_miWuZhenAddtion, false);
        }
    }

    /// <summary>
    /// 尝试激活或取消羁绊
    /// </summary>
    /// <param name="cardData">行动卡牌</param>
    /// <param name="isAdd">是否上阵</param>
    public void UpdateActiveBond(FightCardData cardData, bool isAdd)
    {
        var jiBanIds = DataTable.Hero[cardData.cardId].JiBanIds;
        if (jiBanIds == null || jiBanIds.Length == 0) return;
        Dictionary<int, JiBanActivedClass> jiBanActivedClasses = cardData.isPlayerCard
            ? FightController.instance.playerJiBanAllTypes
            : FightController.instance.enemyJiBanAllTypes;

        //遍历所属羁绊
        for (int i = 0; i < jiBanIds.Length; i++)
        {
            var jiBan = DataTable.JiBan[jiBanIds[i]];
            if (jiBan.IsOpen == 0) continue;
            JiBanActivedClass jiban = jiBanActivedClasses[jiBan.Id];
            bool isActived = true;
            for (int j = 0; j < jiban.List.Count; j++)
            {
                if (jiban.List[j].CardType == cardData.cardType
                    && (jiban.List[j].CardId == cardData.cardId
                        || (jiban.IsHadBossId &&
                            jiban.List[j].BossId == cardData.cardId)))
                {
                    if (isAdd)
                    {
                        if (!jiban.List[j].Cards.Exists(t => t == cardData))
                        {
                            jiban.List[j].Cards.Add(cardData);
                        }
                    }
                    else
                    {
                        jiban.List[j].Cards.Remove(cardData);
                    }
                }

                if (jiban.List[j].Cards.Count <= 0)
                {
                    isActived = false;
                }
            }

            if (cardData.isPlayerCard)
            {
                if (!jiban.IsActive && isActived)
                {
                    for (int k = 0; k < jiban.List.Count; k++)
                    {
                        for (int s = 0; s < jiban.List[k].Cards.Count; s++)
                            jiban.List[k].Cards[s].cardObj.SetHighLight(true);
                    }
                }

                if (!isActived)
                {
                    for (int k = 0; k < jiban.List.Count; k++)
                    {
                        for (int s = 0; s < jiban.List[k].Cards.Count; s++)
                            jiban.List[k].Cards[s].cardObj.SetHighLight(true);
                    }
                }
            }

            jiban.IsActive = isActived;
            //Debug.Log("羁绊: " + jiBanActivedClass.jiBanIndex+ ", isActived: " + jiBanActivedClass.isActived);
        }
    }

    public void RemoveCardFromBoard(FightCardData card, bool refresh = true)
    {
        chessboard.RemoveCard(card.PosIndex, card.isPlayerCard);
        OnBoardReactSet(card, card.posIndex, false, refresh);
        UpdateFightNumTextShow(WarsUIManager.instance.maxHeroNums);
    }

    public void PlaceCardOnBoard(FightCardData card, int posIndex, bool refresh = true)
    {
        OnBoardReactSet(card, posIndex, true, refresh);
        chessboard.PlaceCard(posIndex, card);
        UpdateFightNumTextShow(WarsUIManager.instance.maxHeroNums);
    }

    //上阵卡牌特殊相关处理
    public void OnBoardReactSet(FightCardData card, int posIndex, bool isAdd, bool refresh = true)
    {
        switch (card.cardType)
        {
            case 0:
                if (refresh) UpdateActiveBond(card, isAdd);

                var military = MilitaryInfo.GetInfo(card.cardId).Id;
                switch (military)
                {
                    case 4://盾兵
                        if (isAdd)
                        {
                            if (card.CardState.Shield <= 0)
                            {
                                CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_withStand, true);
                            }
                            card.CardState.Shield = 1;
                        }
                        else
                        {
                            card.CardState.Shield = 0;
                            DestroySateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_withStand, true);
                        }
                        break;
                    case 58://铁骑
                        FightController.instance.UpdateTieQiStateIconShow(card, isAdd);
                        break;
                    default:
                        break;
                }
                if (!isAdd)
                {
                    HeroCardRemoveStatus(card);
                }
                else
                {
                    HeroSoldierAddtionFun(card, posIndex);
                }
                break;
            case 1:
                if (!isAdd)
                {
                    HeroCardRemoveStatus(card);
                }
                else
                {
                    HeroSoldierAddtionFun(card, posIndex);
                }
                break;
            case 2:
                switch (card.cardId)
                {
                    case 2://奏乐台
                        ZouYueTaiAddIcon(posIndex, card.isPlayerCard, isAdd);
                        break;
                    case 4://战鼓台
                        ZhanGuTaiAddtionFun(card, posIndex, card.isPlayerCard, isAdd);
                        break;
                    case 5://风神台
                        FengShenTaiAddtionFun(card, posIndex, card.isPlayerCard, isAdd);
                        break;
                    case 7://霹雳台
                        PiLiTaiAddtionFun(card, posIndex, card.isPlayerCard, isAdd);
                        break;
                    case 8://狼牙台
                        LangYaTaiAddtionFun(card, posIndex, card.isPlayerCard, isAdd);
                        break;
                    case 9://烽火台
                        FengHuoTaiAddtionFun(card, posIndex, card.isPlayerCard, isAdd);
                        break;
                    case 10://号角台
                        ZhanGuTaiAddtionFun(card, posIndex, card.isPlayerCard, isAdd);
                        break;
                    case 11://瞭望台
                        ZhanGuTaiAddtionFun(card, posIndex, card.isPlayerCard, isAdd);
                        break;
                    case 12://七星坛
                        ZhanGuTaiAddtionFun(card, posIndex, card.isPlayerCard, isAdd);
                        break;
                    case 13://斗神台
                        ZhanGuTaiAddtionFun(card, posIndex, card.isPlayerCard, isAdd);
                        break;
                    case 14://曹魏旗
                        ZhanGuTaiAddtionFun(card, posIndex, card.isPlayerCard, isAdd);
                        break;
                    case 15://蜀汉旗
                        ZhanGuTaiAddtionFun(card, posIndex, card.isPlayerCard, isAdd);
                        break;
                    case 16://东吴旗
                        ZhanGuTaiAddtionFun(card, posIndex, card.isPlayerCard, isAdd);
                        break;
                    case 17://迷雾阵
                        miWuZhenAddtionFun(card, posIndex, card.isPlayerCard, isAdd);
                        break;
                    case 18://迷雾阵
                        miWuZhenAddtionFun(card, posIndex, card.isPlayerCard, isAdd);
                        break;
                    case 19://骑兵营
                        ZhanGuTaiAddtionFun(card, posIndex, card.isPlayerCard, isAdd);
                        break;
                    case 20://弓弩营
                        ZhanGuTaiAddtionFun(card, posIndex, card.isPlayerCard, isAdd);
                        break;
                    case 21://步兵营
                        ZhanGuTaiAddtionFun(card, posIndex, card.isPlayerCard, isAdd);
                        break;
                    case 22://长持营
                        ZhanGuTaiAddtionFun(card, posIndex, card.isPlayerCard, isAdd);
                        break;
                    case 23://战船营
                        ZhanGuTaiAddtionFun(card, posIndex, card.isPlayerCard, isAdd);
                        break;
                    default:
                        break;
                }

                break;
            default:
                break;
        }
    }

    //武将士兵卡牌加成效果
    private void HeroSoldierAddtionFun(FightCardData card, int posIndex)
    {
        var armed = MilitaryInfo.GetInfo(card.cardId).ArmedType;
        NeighborsLoop(card.PosIndex, i =>
        {
            var addtionCard = GetCardList(card.isPlayerCard)[i];
            if (addtionCard == null || addtionCard.cardType != 2 || addtionCard.Hp <= 0) return;
            int addtionNums = GetTowerAddValue(addtionCard.cardId, addtionCard.cardGrade);
            switch (addtionCard.cardId)
            {
                case 2://奏乐台
                    if (card.cardObj.War.StateContent.Find(StringNameStatic.StateIconPath_zouyuetaiAddtion) == null)
                    {
                        CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zouyuetaiAddtion, false);
                    }
                    break;
                case 4://战鼓台
                    if (card.CardState.StrengthUp <= 0)
                    {
                        CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                    }
                    card.CardState.StrengthUp += addtionNums;
                    break;
                case 5://风神台
                    if (card.CardState.DodgeUp <= 0)
                    {
                        CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_fengShenTaiAddtion, false);
                    }
                    card.CardState.DodgeUp += addtionNums;
                    break;
                case 7://霹雳台
                    if (card.CardState.CriticalUp <= 0)
                    {
                        CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_pilitaiAddtion, false);
                    }
                    card.CardState.CriticalUp += addtionNums;
                    break;
                case 8://狼牙台
                    if (card.CardState.RouseUp <= 0)
                    {
                        CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_langyataiAddtion, false);
                    }
                    card.CardState.RouseUp += addtionNums;
                    break;
                case 9://烽火台
                    if (card.CardState.FengHuoTaiAddOn <= 0)
                    {
                        CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_fenghuotaiAddtion, false);
                    }
                    card.CardState.FengHuoTaiAddOn += addtionNums;
                    break;
                case 10://号角台
                    if (card.combatType == 0)   //近战
                    {
                        if (card.CardState.StrengthUp <= 0)
                        {
                            CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                        }
                        card.CardState.StrengthUp += addtionNums;
                    }
                    break;
                case 11://瞭望台
                    if (card.combatType == 1)   //远程
                    {
                        if (card.CardState.StrengthUp <= 0)
                        {
                            CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                        }
                        card.CardState.StrengthUp += addtionNums;
                    }
                    break;
                case 12://七星坛
                    if (card.cardDamageType == 1) //法术
                    {
                        if (card.CardState.StrengthUp <= 0)
                        {
                            CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                        }
                        card.CardState.StrengthUp += addtionNums;
                    }
                    break;
                case 13://斗神台
                    if (card.cardDamageType == 0) //物理
                    {
                        if (card.CardState.StrengthUp <= 0)
                        {
                            CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                        }
                        card.CardState.StrengthUp += addtionNums;
                    }
                    break;
                case 14://曹魏旗
                    if (DataTable.Hero[card.cardId].ForceTableId == 1)
                    {
                        if (card.CardState.StrengthUp <= 0)
                        {
                            CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                        }
                        card.CardState.StrengthUp += addtionNums;
                    }
                    break;
                case 15://蜀汉旗
                    if (DataTable.Hero[card.cardId].ForceTableId == 0)
                    {
                        if (card.CardState.StrengthUp <= 0)
                        {
                            CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                        }
                        card.CardState.StrengthUp += addtionNums;
                    }
                    break;
                case 16://东吴旗
                    if (DataTable.Hero[card.cardId].ForceTableId == 2)
                    {
                        if (card.CardState.StrengthUp <= 0)
                        {
                            CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                        }
                        card.CardState.StrengthUp += addtionNums;
                    }
                    break;
                case 17://迷雾阵
                    if (card.CardState.MiWuZhenAddOn <= 0)
                    {
                        CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_miWuZhenAddtion, false);
                    }
                    card.CardState.MiWuZhenAddOn += addtionNums;
                    break;
                case 18://迷雾阵
                    if (card.CardState.MiWuZhenAddOn <= 0)
                    {
                        CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_miWuZhenAddtion, false);
                    }
                    card.CardState.MiWuZhenAddOn += addtionNums;
                    break;
                case 19://骑兵营
                    if (armed == 5)
                    {
                        if (card.CardState.StrengthUp <= 0)
                        {
                            CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                        }
                        card.CardState.StrengthUp += addtionNums;
                    }
                    break;
                case 20://弓弩营
                    if (armed == 9)
                    {
                        if (card.CardState.StrengthUp <= 0)
                        {
                            CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                        }
                        card.CardState.StrengthUp += addtionNums;
                    }
                    break;
                case 21://步兵营
                    if (armed == 2)
                    {
                        if (card.CardState.StrengthUp <= 0)
                        {
                            CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                        }
                        card.CardState.StrengthUp += addtionNums;
                    }
                    break;
                case 22://长持营
                    if (armed == 3)
                    {
                        if (card.CardState.StrengthUp <= 0)
                        {
                            CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                        }
                        card.CardState.StrengthUp += addtionNums;
                    }
                    break;
                case 23://战船营
                    if (armed == 8)
                    {
                        if (card.CardState.StrengthUp <= 0)
                        {
                            CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                        }
                        card.CardState.StrengthUp += addtionNums;
                    }
                    break;
                default:
                    break;
            }
        });
    }

    //奏乐台状态栏添加
    private void ZouYueTaiAddIcon(int posIndex, bool isPlayer, bool isAdd)
    {
        NeighborsLoop(posIndex, i =>
        {
            var addedFightCard = GetCardList(isPlayer)[i];
            if (addedFightCard == null || addedFightCard.cardType != 0 || addedFightCard.Hp <= 0) return;
            if (isAdd)
            {
                if (addedFightCard.cardObj.War.StateContent.Find(StringNameStatic.StateIconPath_zouyuetaiAddtion) !=
                    null) return;
                CreateSateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_zouyuetaiAddtion, false);
                return;
            }

            if (addedFightCard.cardObj.War.StateContent.Find(StringNameStatic.StateIconPath_zouyuetaiAddtion) ==
                null) return;
            DestroyImmediate(addedFightCard.cardObj.War.StateContent.Find(StringNameStatic.StateIconPath_zouyuetaiAddtion).gameObject);

        });
    }

    //烽火台加成技能
    private void FengHuoTaiAddtionFun(FightCardData cardData, int posIndex, bool isPlayer, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);
        NeighborsLoop(posIndex, i =>
        {
            var addedFightCard = GetCardList(isPlayer)[i];
            if (addedFightCard == null || addedFightCard.cardType != 0 || addedFightCard.Hp <= 0) return;
            if (isAdd)
            {
                if (addedFightCard.CardState.FengHuoTaiAddOn <= 0)
                {
                    CreateSateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_fenghuotaiAddtion, false);
                }
                addedFightCard.CardState.FengHuoTaiAddOn += addtionNums;
                return;
            }

            addedFightCard.CardState.FengHuoTaiAddOn -= addtionNums;
            if (addedFightCard.CardState.FengHuoTaiAddOn > 0) return;
            addedFightCard.CardState.FengHuoTaiAddOn = 0;
            DestroySateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_fenghuotaiAddtion, false);
        });
    }

    //狼牙台加成技能
    private void LangYaTaiAddtionFun(FightCardData cardData, int posIndex, bool isPlayer, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);
        NeighborsLoop(posIndex, i =>
        {
            var addedFightCard = GetCardList(isPlayer)[i];
            if (addedFightCard == null || addedFightCard.cardType != 0 || addedFightCard.Hp <= 0) return;
            if (isAdd)
            {
                if (addedFightCard.CardState.RouseUp <= 0)
                {
                    CreateSateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_langyataiAddtion, false);
                }
                addedFightCard.CardState.RouseUp += addtionNums;
                return;
            }

            addedFightCard.CardState.RouseUp -= addtionNums;
            if (addedFightCard.CardState.RouseUp > 0) return;
            addedFightCard.CardState.RouseUp = 0;
            DestroySateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_langyataiAddtion, false);

        });
    }

    //霹雳台加成技能
    private void PiLiTaiAddtionFun(FightCardData cardData, int posIndex, bool isPlayer, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);

        NeighborsLoop(posIndex, i =>
        {
            var addedFightCard = GetCardList(isPlayer)[i];
            if (addedFightCard == null || addedFightCard.cardType != 0 || addedFightCard.Hp <= 0) return;
            if (isAdd)
            {
                if (addedFightCard.CardState.CriticalUp <= 0)
                {
                    CreateSateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_pilitaiAddtion, false);
                }
                addedFightCard.CardState.CriticalUp += addtionNums;
                return;
            }

            addedFightCard.CardState.CriticalUp -= addtionNums;
            if (addedFightCard.CardState.CriticalUp > 0) return;
            addedFightCard.CardState.CriticalUp = 0;
            DestroySateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_pilitaiAddtion, false);

        });
    }

    //迷雾阵加成技能
    private void miWuZhenAddtionFun(FightCardData cardData, int posIndex, bool isPlayer, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);

        var posListToSetMiWu = cardData.isPlayerCard ? playerCardsPos : enemyCardsPos;

        NeighborsLoop(posIndex, i =>
        {
            var obj = posListToSetMiWu[i];
            if (!cardData.isPlayerCard)
            {
                //迷雾动画
                GameObject stateDinObj = Instantiate(Resources.Load("Prefabs/stateDin/" + StringNameStatic.StateIconPath_miWuZhenAddtion, typeof(GameObject)) as GameObject, cardData.cardObj.transform);
                stateDinObj.name = StringNameStatic.StateIconPath_miWuZhenAddtion + "Din";
                stateDinObj.transform.position = obj.transform.position;
                stateDinObj.GetComponent<Animator>().enabled = false;
            }
            NeighborsLoop(posIndex, pos =>
            {
                var addedFightCard = GetCardList(isPlayer)[pos];
                if (addedFightCard == null || addedFightCard.cardType != 0 || addedFightCard.Hp <= 0) return;
                if (isAdd)
                {
                    if (addedFightCard.CardState.MiWuZhenAddOn <= 0)
                    {
                        CreateSateIcon(addedFightCard.cardObj.War.StateContent,
                            StringNameStatic.StateIconPath_miWuZhenAddtion, false);
                    }

                    addedFightCard.CardState.MiWuZhenAddOn += addtionNums;
                    return;
                }

                addedFightCard.CardState.MiWuZhenAddOn -= addtionNums;
                if (addedFightCard.CardState.MiWuZhenAddOn > 0) return;
                addedFightCard.CardState.MiWuZhenAddOn = 0;
                DestroySateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_miWuZhenAddtion,
                    false);
            });
        });

        if (isAdd || cardData.isPlayerCard) return;
        for (int i = 0; i < cardData.cardObj.transform.childCount; i++)
        {
            Transform tran = cardData.cardObj.transform.GetChild(i);
            if (tran.name == StringNameStatic.StateIconPath_miWuZhenAddtion + "Din")
            {
                tran.gameObject.SetActive(false);
            }
        }
    }

    //风神台加成技能
    private void FengShenTaiAddtionFun(FightCardData card, int posIndex, bool isPlayer, bool isAdd)
    {
        var towerAddValue = GetTowerAddValue(card.cardId, card.cardGrade);
        NeighborsLoop(posIndex, i =>
        {
            var addedFightCard = GetCardList(isPlayer)[i];
            if (addedFightCard == null || addedFightCard.cardType != 0 || addedFightCard.Hp <= 0) return;
            if (isAdd)
            {
                if (addedFightCard.CardState.DodgeUp <= 0)
                {
                    CreateSateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_fengShenTaiAddtion, false);
                }
                addedFightCard.CardState.DodgeUp += towerAddValue;
                return;
            }

            addedFightCard.CardState.DodgeUp -= towerAddValue;
            if (addedFightCard.CardState.DodgeUp > 0) return;
            addedFightCard.CardState.DodgeUp = 0;
            DestroySateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_fengShenTaiAddtion, false);
        });
    }

    //战鼓台(10-13塔)加成技能
    private void ZhanGuTaiAddtionFun(FightCardData cardData, int targetPos, bool isPlayer, bool isAdd)
    {
        var addOn = GetTowerAddValue(cardData.cardId, cardData.cardGrade);
        void WhileNeighborsTrueAddOn(Func<FightCardData,bool> condition)
        {
            NeighborsLoop(targetPos, pos =>
            {
                var card = GetCard(pos, isPlayer);
                if (card == null ||
                    card.cardType != 0 ||
                    card.Hp <= 0) return;
                if(condition(card)) DamageTowerAdditionFun(card, isAdd, addOn);
            });
        }

        switch (cardData.cardId)
        {
            case 4://战鼓台
                WhileNeighborsTrueAddOn(c => true);
                break;
            case 10://号角台
                WhileNeighborsTrueAddOn(c=>c.combatType == 0);
                break;
            case 11://瞭望台
                WhileNeighborsTrueAddOn(c=>c.combatType == 1);
                break;
            case 12://七星坛
                WhileNeighborsTrueAddOn(c=>c.cardDamageType == 1);
                break;
            case 13://斗神台
                WhileNeighborsTrueAddOn(c=>c.cardDamageType == 0);
                break;
            case 14://曹魏旗
                WhileNeighborsTrueAddOn(c => DataTable.Hero[c.cardId].ForceTableId == 1);
                break;
            case 15://蜀汉旗
                WhileNeighborsTrueAddOn(c => DataTable.Hero[c.cardId].ForceTableId == 0);
                break;
            case 16://东吴旗
                WhileNeighborsTrueAddOn(c => DataTable.Hero[c.cardId].ForceTableId == 2);
                break;
            case 19://骑兵营
                WhileNeighborsTrueAddOn(c => MilitaryInfo.GetInfo(c.cardId).ArmedType == 5);
                break;
            case 20://弓弩营
                WhileNeighborsTrueAddOn(c => MilitaryInfo.GetInfo(c.cardId).ArmedType == 9);
                break;
            case 21://步兵营
                WhileNeighborsTrueAddOn(c => MilitaryInfo.GetInfo(c.cardId).ArmedType == 2);
                break;
            case 22://长持营
                WhileNeighborsTrueAddOn(c => MilitaryInfo.GetInfo(c.cardId).ArmedType == 3);
                break;
            case 23://战船营
                WhileNeighborsTrueAddOn(c => MilitaryInfo.GetInfo(c.cardId).ArmedType == 8);
                break;
        }

    }

    //伤害加成塔的上阵下阵对周为单位的处理
    private void DamageTowerAdditionFun(FightCardData addedFightCard, bool isAdd, int addtionNums)
    {
        if (isAdd)
        {
            if (addedFightCard.CardState.StrengthUp <= 0)
            {
                CreateSateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
            }
            addedFightCard.CardState.StrengthUp += addtionNums;
        }
        else
        {
            addedFightCard.CardState.StrengthUp -= addtionNums;
            if (addedFightCard.CardState.StrengthUp <= 0)
            {
                addedFightCard.CardState.StrengthUp = 0;
                DestroySateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
            }
        }
    }

    //获取塔的加成值
    private int GetTowerAddValue(int towerId, int towerGrade) =>
        GameCardInfo.GetInfo(GameCardType.Tower, towerId).GetDamage(towerGrade);

    /// <summary>
    /// 创建状态图标
    /// </summary>
    /// <param name="tran"></param>
    /// <param name="stateName"></param>
    public void CreateSateIcon(Transform tran, string stateName, bool isShowEffect)
    {
        if (tran.Find(stateName) == null)
        {
            GameObject stateIconObj = EffectsPoolingControl.instance.GetBuffEffect("stateIcon", tran).gameObject;
            stateIconObj.name = stateName;
            stateIconObj.GetComponent<Image>().sprite = Resources.Load("Image/fightStateIcon/" + stateName, typeof(Sprite)) as Sprite;
        }
        if (isShowEffect && tran.parent.Find(stateName + "Din") == null)
        {
            GameObject stateDinObj = EffectsPoolingControl.instance.GetBuffEffect(stateName, tran.parent).gameObject;
            stateDinObj.name = stateName + "Din";
        }
    }

    //删除状态图标
    public void DestroySateIcon(Transform tran, string stateName, bool isShowEffect)
    {
        Transform tran0 = tran.Find(stateName);
        if (tran0 != null)
            EffectsPoolingControl.instance.RecycleEffect(tran0.gameObject.GetComponent<EffectStateUi>());
        if (isShowEffect)
        {
            Transform tran1 = tran.parent.Find(stateName + "Din");
            if (tran1 != null)
                EffectsPoolingControl.instance.RecycleEffect(tran1.gameObject.GetComponent<EffectStateUi>());
        }
    }

    /// <summary>
    /// 更新上阵数量显示
    /// </summary>
    public void UpdateFightNumTextShow(int maxCards) => heroNumText.text = string.Format(DataTable.GetStringText(24), chessboard.PlayerCardsOnBoard, maxCards);

    public bool IsPlayerAvailableToPlace(int index) => chessboard.PlayerCardsOnBoard < WarsUIManager.instance.maxHeroNums && chessboard.IsPlayerScopeAvailable(index);


    [HideInInspector]
    public bool isNeedAutoHideInfo;  //是否需要自动隐藏卡牌详情

    //private FightCardData[] enemyCards;
    //private FightCardData[] playerCards;


    public IReadOnlyList<FightCardData> GetCardList(bool isPlayer) =>
        chessboard.GetScope(isPlayer).Where(o => o.Card != null).Select(o => o.Card).ToArray();

    public FightCardData GetCard(int index, bool isPlayer) => chessboard.GetChessPos(index, isPlayer).Card;

    /// <summary>
    /// ////////////////战斗主线逻辑//////////////////////////////////////////////////
    /// </summary>
    //位置列攻击目标选择次序
    private int[][] AttackSelectionOrder = new int[5][]
    {
        new int[11]{ 0, 2, 3, 5, 7, 8, 10,12,13,15,17},     //0列
        new int[11]{ 1, 2, 4, 6, 7, 9, 11,12,14,16,17},     //1列
        new int[12]{ 0, 1, 2, 5, 6, 7, 10,11,12,15,16,17},  //2列
        new int[8] { 0, 3, 5, 8, 10,13,15,17},              //3列
        new int[8] { 1, 4, 6, 9, 11,14,16,17},              //4列
    };

    //锁定目标卡牌
    public int FindOpponentIndex(FightCardData attackUnit)
    {
        if (attackUnit.CardState.Imprisoned <= 0)//攻击者没有禁锢状态
        {
            switch (DataTable.Hero[attackUnit.cardId].MilitaryUnitTableId)
            {
                //刺客
                case 25:
                    int chooseIndex = CiKeOpponentChoose(attackUnit);
                    if (chooseIndex != -1)
                        return chooseIndex;
                    break;
                default:
                    break;
            }
        }

        var enemyCards = GetCardList(false);
        var playerCards = GetCardList(true);
        int index = 0;
        int arrIndex = FightController.instance.ChessPosIndex % 5;
        if (attackUnit.isPlayerCard)
        {
            for (; index < AttackSelectionOrder[arrIndex].Length; index++)
            {
                if (enemyCards[AttackSelectionOrder[arrIndex][index]] != null
                    && enemyCards[AttackSelectionOrder[arrIndex][index]].Hp > 0)
                {
                    break;
                }
            }
            if (index >= AttackSelectionOrder[arrIndex].Length)
            {
                //Debug.Log("敌方无存活单位");
                return -1;
            }
        }
        else
        {
            for (; index < AttackSelectionOrder[arrIndex].Length; index++)
            {
                if (playerCards[AttackSelectionOrder[arrIndex][index]] != null
                    && playerCards[AttackSelectionOrder[arrIndex][index]].Hp > 0)
                {
                    break;
                }
            }
            if (index >= AttackSelectionOrder[arrIndex].Length)
            {
                //Debug.Log("我方无存活单位");
                return -1;
            }
        }
        return AttackSelectionOrder[arrIndex][index];
    }

    //刺客目标选择
    private int CiKeOpponentChoose(FightCardData attackUnit)
    {
        int index = -1;
        int maxDamage = 0;
        var cardList = GetCardList(!attackUnit.isPlayerCard);
        for (int i = 0; i < cardList.Count; i++)
        {
            if (cardList[i] != null && cardList[i].cardType == 0 && cardList[i].Hp > 0)
            {
                if (cardList[i].combatType == 1)
                {
                    if (cardList[i].damage > maxDamage)
                    {
                        index = i;
                        maxDamage = cardList[i].damage;
                    }
                }
            }
        }
        return index;
    }

    public void StartPushCardBackward(FightCardData card, int nextPos, float sec)
    {
        StartCoroutine(PushCardBackward());
        IEnumerator PushCardBackward()
        {
            yield return new WaitForSeconds(sec);
            RemoveCardFromBoard(card, false);
            var nextChessPos = chessboard.GetScope(card.isPlayerCard)[nextPos];
            FightController.instance.ShowSpellTextObj(card.cardObj, DataTable.GetStringText(17), true);
            card.cardObj.transform.DOMove(nextChessPos.transform.position, sec).SetEase(Ease.Unset).OnComplete(() =>
                PlaceCardOnBoard(card, nextPos));
        }
    }

    public bool MeleeReturnOrigin(FightCardData card)
    {
        card.cardObj.transform.DOLocalMove(Vector3.zero, FightController.instance.attackShakeTimeToBack).SetEase(Ease.Unset);
        return true;
    }

    public float forward = 0.2f;   //去
    public float back = 0.3f;   //回
    public float wait = 0.4f; //等
    public float go = 0.1f;  //去
    public float posFloat;
    //近战移动方式
    //public bool MeleeMoveToTarget(FightCardData card, int targetIndex)
    //{
    //    var ui = card.cardObj;
    //    var targetPos = GetCard(targetIndex, !card.isPlayerCard).cardObj.transform.position;
    //    var targetHalfPos = new Vector3(targetPos.x, targetPos.y - floDisY, targetPos.z);
    //    var vec = new Vector3(
    //        targetHalfPos.x,
    //        targetHalfPos.y + (card.isPlayerCard ? (-1 * posFloat) : posFloat) * oneDisY,
    //        targetHalfPos.z
    //    );
    //    ui.transform.DOMove(targetHalfPos, FightController.instance.attackShakeTimeToGo * forward).SetEase(Ease.Unset)
    //        .OnComplete(() => ui.transform.DOMove(ui.transform.position, FightController.instance.attackShakeTimeToGo * back)
    //            .SetEase(Ease.Unset)
    //            .OnComplete(() => ui.transform.DOMove(vec, FightController.instance.attackShakeTimeToGo * wait).SetEase(Ease.Unset)
    //                .OnComplete(() =>
    //                    ui.transform.DOMove(targetHalfPos, FightController.instance.attackShakeTimeToGo * go).SetEase(Ease.Unset))));
    //    return true;
    //}

    public Transform GetChessPos(int posIndex, bool isPlayer) => chessboard.GetChessPos(posIndex, isPlayer).transform;

    public void DestroyCard(FightCardData playerCard) => chessboard.DestroyCard(playerCard);

}