using Beebyte.Obfuscator;
using CorrelateLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
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

    //卡牌附近单位遍历次序
    public int[][] NeighbourCards = new int[20][] {
        new int[3]{ 2, 3, 5},           //0
        new int[3]{ 2, 4, 6},           //1
        new int[5]{ 0, 1, 5, 6, 7},     //2
        new int[3]{ 0, 5, 8},           //3
        new int[3]{ 1, 6, 9},           //4
        new int[6]{ 0, 2, 3, 7, 8, 10}, //5
        new int[6]{ 1, 2, 4, 7, 9, 11}, //6
        new int[6]{ 2, 5, 6, 10,11,12}, //7
        new int[4]{ 3, 5, 10,13},       //8
        new int[4]{ 4, 6, 11,14},       //9
        new int[6]{ 5, 7, 8, 12,13,15}, //10
        new int[6]{ 6, 7, 9, 12,14,16}, //11
        new int[6]{ 7, 10,11,15,16,17}, //12
        new int[4]{ 8, 10,15,18},       //13
        new int[4]{ 9, 11,16,19},       //14
        new int[5]{ 10,12,13,17,18},    //15
        new int[5]{ 11,12,14,17,19},    //16
        new int[3]{ 12,15,16},          //17
        new int[2]{ 13,15},             //18
        new int[2]{ 14,16},             //19
    };

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
        chessboard.Init();
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
        playerBase.fightState = new FightState();
        chessboard.PlaceCard(17, playerBase);
        UpdateFightNumTextShow(WarsUIManager.instance.maxHeroNums);
    }

    public FightCardData ResetPlayerBaseHp()
    {
        var baseConfig = DataTable.BaseLevel[WarsUIManager.instance.cityLevel];
        var playerLvlCfg = DataTable.PlayerLevelConfig[PlayerDataForGame.instance.pyData.Level];
        var playerBase = GetCard(17, true);
        playerBase.ResetHp(baseConfig.BaseHp + playerLvlCfg.BaseHpAddOn);
        return playerBase;
    }

    /// <summary>
    /// 初始化敌方卡牌到战斗位上
    /// </summary>
    /// <param name="battleEventId"></param>
    public void InitChessboard(int battleEventId)
    {
        //初始化敌方羁绊原始集合
        CardManager.ResetJiBan(FightController.instance.enemyJiBanAllTypes);

        battleIdIndex = battleEventId;
        var playerBase = ResetPlayerBaseHp();
        playerBase.UpdateHpUi();
        FightController.instance.ClearEmTieQiCardList();
        chessboard.ClearEnemyCards();
        //对战势力名
        var battle = DataTable.BattleEvent[battleEventId];
        var randomIndex = Random.Range(0, battle.EnemyTableIndexes.Length); //敌人随机库抽取一个id
        var enemyRandId = battle.EnemyTableIndexes[randomIndex];

        //随机敌人卡牌
        if (battle.IsStaticEnemies == 0)
        {
            var enemies = DataTable.Enemy[enemyRandId].Poses();
            for (var i = 0; i < 20; i++)
            {
                var enemyId = enemies[i];
                if (enemyId == 0) continue;
                var card = CreateEnemyFightUnit(i, enemyId, false);
                PlaceCardOnBoard(card, i, false);
            }
        }
        else
        {
            var enemies = DataTable.StaticArrangement[enemyRandId].Poses();
            //固定敌人卡牌
            for (var i = 0; i < 20; i++)
            {
                var chessman = enemies[i];
                if (chessman == null) continue;
                if (chessman.Star <= 0)
                    throw new InvalidOperationException(
                        $"卡牌初始化异常，[{chessman.CardId}.{chessman.CardType}]等级为{chessman.Star}!");
                var card = CreateEnemyFightUnit(i, 1, true, chessman);
                PlaceCardOnBoard(card, i, false);
            }
        }

        var enemyBase = new FightCardData();
        enemyBase.cardObj = Instantiate(homeCardObj, transform);
        enemyBase.ResetHp(DataTable.BattleEvent[battleEventId].BaseHp);
        enemyBase.hpr = 0;
        enemyBase.cardType = 522;
        enemyBase.isPlayerCard = false;
        enemyBase.activeUnit = false;
        enemyBase.fightState = new FightState();
        PlaceCardOnBoard(enemyBase, 17, false);

        FightController.instance.InitStartFight();

        //调整游戏速度
        var speed = GamePref.PrefWarSpeed;
        Time.timeScale = speed;
        speedBtnText.text = Multiply + speed;
        //创建敌方卡牌战斗单位数据
        FightCardData CreateEnemyFightUnit(int index, int unitId, bool isFixed, Chessman chessman = null)
        {
            var fCard = new FightCardData();
            fCard.cardObj = PrefabManager.NewWarGameCardUi(transform); //Instantiate(fightCardPre, enemyCardsBox);
            fCard.fightState = new FightState();
            fCard.unitId = unitId;
            var enemy = DataTable.EnemyUnit[unitId];
            //是否是固定阵容
            if (isFixed)
            {
                fCard.cardType = chessman.CardType;
                fCard.cardId = chessman.CardId;
                fCard.cardGrade = chessman.Star;
            }
            else
            {
                fCard.cardType = DataTable.EnemyUnit[unitId].CardType;
                fCard.cardId = GameCardInfo.RandomPick((GameCardType)enemy.CardType, enemy.Rarity).Id;
                fCard.cardGrade = enemy.Star;
            }

            var card = GameCard.Instance(fCard.cardId, fCard.cardType, fCard.cardGrade);
            var info = card.GetInfo();
            fCard.cardObj.Init(card);
            fCard.cardObj.tag = GameSystem.UnTagged;
            fCard.posIndex = index;
            fCard.isPlayerCard = false;
            fCard.damage = info.GetDamage(fCard.cardGrade);
            fCard.hpr = info.GameSetRecovery;
            fCard.ResetHp(info.GetHp(fCard.cardGrade));
            fCard.activeUnit = info.Type == GameCardType.Hero || ((info.Type == GameCardType.Tower) &&
                                                                (info.Id == 0 || info.Id == 1 || info.Id == 2 ||
                                                                 info.Id == 3 || info.Id == 6));
            fCard.cardMoveType = info.CombatType;
            fCard.cardDamageType = info.DamageType;
            GiveGameObjEventForHoldOn(fCard.cardObj, info.About);
            return fCard;
        }
    }

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

    //营寨行动
    private void YingZhaiFun(FightCardData card, bool isPlayer)
    {
        if (card.Hp <= 1) return;
        FightController.instance.indexAttackType = 0;
        FightController.instance.PlayAudioForSecondClip(42, 0);

        var maxHpSubtract = 0;  //最多扣除血量
        var needAddHpCard = new FightCardData();

        for (var i = 0; i < NeighbourCards[card.posIndex].Length; i++)
        {
            var target = GetCardList(isPlayer)[NeighbourCards[card.posIndex][i]];
            if (target != null && target.cardType == 0 && target.Hp > 0)
            {
                if (target.Hp.Max - target.Hp.Value > maxHpSubtract)
                {
                    maxHpSubtract = target.Hp.Max - target.Hp.Value;
                    needAddHpCard = target;
                }
            }
        }

        if (maxHpSubtract <= 0) return;
        var canAddHpNum = card.Hp - 1;
        if (canAddHpNum > maxHpSubtract) canAddHpNum = maxHpSubtract;
        //自身减血
        card.Hp.Add(-canAddHpNum);
        FightController.instance.TargetAnimShow(card, canAddHpNum);
        //单位加血
        FightController.instance.AttackToEffectShow(needAddHpCard, false, "42A");
        needAddHpCard.Hp.Add(canAddHpNum);
        FightController.instance.ShowSpellTextObj(needAddHpCard.cardObj, DataTable.GetStringText(15), true, false);
        FightController.instance.TargetAnimShow(needAddHpCard, canAddHpNum);
    }

    //投石台攻击
    private void TouShiTaiAttackFun(FightCardData cardData)
    {
        var attackedUnits = GetCardList(cardData.isPlayerCard);
        var damage = (int)(DataTable.GetGameValue(122) / 100f * GetTowerAddValue(cardData.cardId, cardData.cardGrade)); //造成的伤害
        FightController.instance.indexAttackType = 0;

        FightController.instance.PlayAudioForSecondClip(24, 0);

        List<GameObject> posListToThunder = cardData.isPlayerCard ? enemyCardsPos : playerCardsPos;
        EffectsPoolingControl.instance.GetEffectToFight1("101A", 1f, posListToThunder[cardData.posIndex].transform);

        if (attackedUnits[cardData.posIndex] != null && attackedUnits[cardData.posIndex].Hp > 0)
        {
            int finalDamage = FightController.instance.DefDamageProcessFun(cardData, attackedUnits[cardData.posIndex], damage);
            attackedUnits[cardData.posIndex].Hp.Add(-finalDamage);
            FightController.instance.TargetAnimShow(attackedUnits[cardData.posIndex], finalDamage);
            if (attackedUnits[cardData.posIndex].cardType == 522)
            {
                if (attackedUnits[cardData.posIndex].Hp <= 0)
                {
                    FightController.instance.recordWinner = attackedUnits[cardData.posIndex].isPlayerCard ? -1 : 1;
                }
            }
        }
        for (int i = 0; i < NeighbourCards[cardData.posIndex].Length; i++)
        {
            FightCardData attackedUnit = attackedUnits[NeighbourCards[cardData.posIndex][i]];
            if (attackedUnit != null && attackedUnit.Hp > 0)
            {
                int finalDamage = FightController.instance.DefDamageProcessFun(cardData, attackedUnit, damage);
                attackedUnit.Hp.Add(-finalDamage);
                FightController.instance.TargetAnimShow(attackedUnit, finalDamage);
                if (attackedUnit.cardType == 522)
                {
                    if (attackedUnit.Hp <= 0)
                    {
                        FightController.instance.recordWinner = attackedUnit.isPlayerCard ? -1 : 1;
                    }
                }
            }
        }
    }

    //奏乐台血量回复
    private void ZouYueTaiAddtionFun(FightCardData cardData, bool isPlayer)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);    //回复血量基值
        addtionNums = (int)(addtionNums * DataTable.GetGameValue(123) / 100f);
        FightController.instance.indexAttackType = 0;
        FightController.instance.PlayAudioForSecondClip(42, 0);
        for (int i = 0; i < NeighbourCards[cardData.posIndex].Length; i++)
        {
            FightCardData addedFightCard = GetCardList(isPlayer)[NeighbourCards[cardData.posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
            {
                FightController.instance.AttackToEffectShow(addedFightCard, false, "42A");
                addedFightCard.Hp.Add(addtionNums);
                FightController.instance.ShowSpellTextObj(addedFightCard.cardObj, DataTable.GetStringText(15), true, false);
                FightController.instance.TargetAnimShow(addedFightCard, addtionNums);
            }
        }
    }

    //轩辕台护盾加成
    private void XuanYuanTaiAddtionFun(FightCardData cardData, bool isPlayer)
    {
        FightController.instance.PlayAudioForSecondClip(4, 0);
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);    //添加护盾的单位最大数
        for (int i = 0; i < NeighbourCards[cardData.posIndex].Length; i++)
        {
            FightCardData addedFightCard = GetCardList(isPlayer)[NeighbourCards[cardData.posIndex][i]];
            if (addedFightCard != null && addedFightCard.Hp > 0)
            {
                if (addedFightCard.cardType == 0 && addedFightCard.fightState.withStandNums <= 0)
                {
                    FightController.instance.AttackToEffectShow(addedFightCard, false, "4A");
                    addedFightCard.fightState.withStandNums = 1;
                    CreateSateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_withStand, true);
                    addtionNums--;
                    if (addtionNums <= 0)
                        break;
                }
            }
        }
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
        if (cardData.fightState.zhangutaiAddtion > 0)
        {
            cardData.fightState.zhangutaiAddtion = 0;
            DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
        }
        //风神台图标
        if (cardData.fightState.fengShenTaiAddtion > 0)
        {
            cardData.fightState.fengShenTaiAddtion = 0;
            DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_fengShenTaiAddtion, false);
        }
        //霹雳台图标
        if (cardData.fightState.pilitaiAddtion > 0)
        {
            cardData.fightState.pilitaiAddtion = 0;
            DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_pilitaiAddtion, false);
        }
        //霹雳台图标
        if (cardData.fightState.pilitaiAddtion > 0)
        {
            cardData.fightState.pilitaiAddtion = 0;
            DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_pilitaiAddtion, false);
        }
        //狼牙台图标
        if (cardData.fightState.langyataiAddtion > 0)
        {
            cardData.fightState.langyataiAddtion = 0;
            DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_langyataiAddtion, false);
        }
        //烽火台图标
        if (cardData.fightState.fenghuotaiAddtion > 0)
        {
            cardData.fightState.fenghuotaiAddtion = 0;
            DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_fenghuotaiAddtion, false);
        }
        //迷雾阵图标
        if (cardData.fightState.miWuZhenAddtion > 0)
        {
            cardData.fightState.miWuZhenAddtion = 0;
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
                            if (card.fightState.withStandNums <= 0)
                            {
                                CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_withStand, true);
                            }
                            card.fightState.withStandNums = 1;
                        }
                        else
                        {
                            card.fightState.withStandNums = 0;
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
        for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
        {
            var addtionCard = GetCardList(card.isPlayerCard)[NeighbourCards[posIndex][i]];
            if (addtionCard != null && addtionCard.cardType == 2 && addtionCard.Hp > 0)
            {
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
                        if (card.fightState.zhangutaiAddtion <= 0)
                        {
                            CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                        }
                        card.fightState.zhangutaiAddtion += addtionNums;
                        break;
                    case 5://风神台
                        if (card.fightState.fengShenTaiAddtion <= 0)
                        {
                            CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_fengShenTaiAddtion, false);
                        }
                        card.fightState.fengShenTaiAddtion += addtionNums;
                        break;
                    case 7://霹雳台
                        if (card.fightState.pilitaiAddtion <= 0)
                        {
                            CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_pilitaiAddtion, false);
                        }
                        card.fightState.pilitaiAddtion += addtionNums;
                        break;
                    case 8://狼牙台
                        if (card.fightState.langyataiAddtion <= 0)
                        {
                            CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_langyataiAddtion, false);
                        }
                        card.fightState.langyataiAddtion += addtionNums;
                        break;
                    case 9://烽火台
                        if (card.fightState.fenghuotaiAddtion <= 0)
                        {
                            CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_fenghuotaiAddtion, false);
                        }
                        card.fightState.fenghuotaiAddtion += addtionNums;
                        break;
                    case 10://号角台
                        if (card.cardMoveType == 0)   //近战
                        {
                            if (card.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            card.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 11://瞭望台
                        if (card.cardMoveType == 1)   //远程
                        {
                            if (card.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            card.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 12://七星坛
                        if (card.cardDamageType == 1) //法术
                        {
                            if (card.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            card.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 13://斗神台
                        if (card.cardDamageType == 0) //物理
                        {
                            if (card.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            card.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 14://曹魏旗
                        if (DataTable.Hero[card.cardId].ForceTableId == 1)
                        {
                            if (card.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            card.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 15://蜀汉旗
                        if (DataTable.Hero[card.cardId].ForceTableId == 0)
                        {
                            if (card.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            card.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 16://东吴旗
                        if (DataTable.Hero[card.cardId].ForceTableId == 2)
                        {
                            if (card.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            card.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 17://迷雾阵
                        if (card.fightState.miWuZhenAddtion <= 0)
                        {
                            CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_miWuZhenAddtion, false);
                        }
                        card.fightState.miWuZhenAddtion += addtionNums;
                        break;
                    case 18://迷雾阵
                        if (card.fightState.miWuZhenAddtion <= 0)
                        {
                            CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_miWuZhenAddtion, false);
                        }
                        card.fightState.miWuZhenAddtion += addtionNums;
                        break;
                    case 19://骑兵营
                        if (armed == 5)
                        {
                            if (card.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            card.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 20://弓弩营
                        if (armed == 9)
                        {
                            if (card.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            card.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 21://步兵营
                        if (armed == 2)
                        {
                            if (card.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            card.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 22://长持营
                        if (armed == 3)
                        {
                            if (card.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            card.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 23://战船营
                        if (armed == 8)
                        {
                            if (card.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(card.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            card.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }

    //奏乐台状态栏添加
    private void ZouYueTaiAddIcon(int posIndex, bool isPlayer, bool isAdd)
    {
        for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
        {
            var addedFightCard = GetCardList(isPlayer)[NeighbourCards[posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
            {
                if (isAdd)
                {
                    if (addedFightCard.cardObj.War.StateContent.Find(StringNameStatic.StateIconPath_zouyuetaiAddtion) == null)
                    {
                        CreateSateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_zouyuetaiAddtion, false);
                    }
                }
                else
                {
                    if (addedFightCard.cardObj.War.StateContent.Find(StringNameStatic.StateIconPath_zouyuetaiAddtion) != null)
                    {
                        DestroyImmediate(addedFightCard.cardObj.War.StateContent.Find(StringNameStatic.StateIconPath_zouyuetaiAddtion).gameObject);
                    }
                }
            }
        }
    }

    //烽火台加成技能
    private void FengHuoTaiAddtionFun(FightCardData cardData, int posIndex, bool isPlayer, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);

        for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
        {
            var addedFightCard = GetCardList(isPlayer)[NeighbourCards[posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
            {
                if (isAdd)
                {
                    if (addedFightCard.fightState.fenghuotaiAddtion <= 0)
                    {
                        CreateSateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_fenghuotaiAddtion, false);
                    }
                    addedFightCard.fightState.fenghuotaiAddtion += addtionNums;
                }
                else
                {
                    addedFightCard.fightState.fenghuotaiAddtion -= addtionNums;
                    if (addedFightCard.fightState.fenghuotaiAddtion <= 0)
                    {
                        addedFightCard.fightState.fenghuotaiAddtion = 0;
                        DestroySateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_fenghuotaiAddtion, false);
                    }
                }
            }
        }
    }

    //狼牙台加成技能
    private void LangYaTaiAddtionFun(FightCardData cardData, int posIndex, bool isPlayer, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);

        for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
        {
            var addedFightCard = GetCardList(isPlayer)[NeighbourCards[posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
            {
                if (isAdd)
                {
                    if (addedFightCard.fightState.langyataiAddtion <= 0)
                    {
                        CreateSateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_langyataiAddtion, false);
                    }
                    addedFightCard.fightState.langyataiAddtion += addtionNums;
                }
                else
                {
                    addedFightCard.fightState.langyataiAddtion -= addtionNums;
                    if (addedFightCard.fightState.langyataiAddtion <= 0)
                    {
                        addedFightCard.fightState.langyataiAddtion = 0;
                        DestroySateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_langyataiAddtion, false);
                    }
                }
            }
        }
    }

    //霹雳台加成技能
    private void PiLiTaiAddtionFun(FightCardData cardData, int posIndex, bool isPlayer, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);

        for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
        {
            var addedFightCard = GetCardList(isPlayer)[NeighbourCards[posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
            {
                if (isAdd)
                {
                    if (addedFightCard.fightState.pilitaiAddtion <= 0)
                    {
                        CreateSateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_pilitaiAddtion, false);
                    }
                    addedFightCard.fightState.pilitaiAddtion += addtionNums;
                }
                else
                {
                    addedFightCard.fightState.pilitaiAddtion -= addtionNums;
                    if (addedFightCard.fightState.pilitaiAddtion <= 0)
                    {
                        addedFightCard.fightState.pilitaiAddtion = 0;
                        DestroySateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_pilitaiAddtion, false);
                    }
                }
            }
        }
    }

    //迷雾阵加成技能
    private void miWuZhenAddtionFun(FightCardData cardData, int posIndex, bool isPlayer, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);

        List<GameObject> posListToSetMiWu = cardData.isPlayerCard ? playerCardsPos : enemyCardsPos;

        for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
        {
            if (!cardData.isPlayerCard)
            {
                //迷雾动画
                GameObject stateDinObj = Instantiate(Resources.Load("Prefabs/stateDin/" + StringNameStatic.StateIconPath_miWuZhenAddtion, typeof(GameObject)) as GameObject, cardData.cardObj.transform);
                stateDinObj.name = StringNameStatic.StateIconPath_miWuZhenAddtion + "Din";
                stateDinObj.transform.position = posListToSetMiWu[NeighbourCards[posIndex][i]].transform.position;
                stateDinObj.GetComponent<Animator>().enabled = false;
            }
            var addedFightCard = GetCardList(isPlayer)[NeighbourCards[posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
            {
                if (isAdd)
                {
                    if (addedFightCard.fightState.miWuZhenAddtion <= 0)
                    {
                        CreateSateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_miWuZhenAddtion, false);
                    }
                    addedFightCard.fightState.miWuZhenAddtion += addtionNums;
                }
                else
                {
                    addedFightCard.fightState.miWuZhenAddtion -= addtionNums;
                    if (addedFightCard.fightState.miWuZhenAddtion <= 0)
                    {
                        addedFightCard.fightState.miWuZhenAddtion = 0;
                        DestroySateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_miWuZhenAddtion, false);
                    }
                }
            }
        }

        if (!isAdd && !cardData.isPlayerCard)
        {
            for (int i = 0; i < cardData.cardObj.transform.childCount; i++)
            {
                Transform tran = cardData.cardObj.transform.GetChild(i);
                if (tran.name == StringNameStatic.StateIconPath_miWuZhenAddtion + "Din")
                {
                    tran.gameObject.SetActive(false);
                }
            }
        }
    }

    //风神台加成技能
    private void FengShenTaiAddtionFun(FightCardData card, int posIndex, bool isPlayer, bool isAdd)
    {
        int towerAddValue = GetTowerAddValue(card.cardId, card.cardGrade);

        for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
        {
            var addedFightCard = GetCardList(isPlayer)[NeighbourCards[posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
            {
                if (isAdd)
                {
                    if (addedFightCard.fightState.fengShenTaiAddtion <= 0)
                    {
                        CreateSateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_fengShenTaiAddtion, false);
                    }
                    addedFightCard.fightState.fengShenTaiAddtion += towerAddValue;
                }
                else
                {
                    addedFightCard.fightState.fengShenTaiAddtion -= towerAddValue;
                    if (addedFightCard.fightState.fengShenTaiAddtion <= 0)
                    {
                        addedFightCard.fightState.fengShenTaiAddtion = 0;
                        DestroySateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_fengShenTaiAddtion, false);
                    }
                }
            }
        }
    }

    //战鼓台(10-13塔)加成技能
    private void ZhanGuTaiAddtionFun(FightCardData cardData, int posIndex, bool isPlayer, bool isAdd)
    {
        var addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);
        var cardList = GetCardList(isPlayer);
        switch (cardData.cardId)
        {
            case 4://战鼓台
                for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardList[NeighbourCards[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                        DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                }
                break;
            case 10://号角台
                for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardList[NeighbourCards[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        if (addedFightCard.cardMoveType == 0)   //近战
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 11://瞭望台
                for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardList[NeighbourCards[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        if (addedFightCard.cardMoveType == 1)   //远程
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 12://七星坛
                for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardList[NeighbourCards[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        if (addedFightCard.cardDamageType == 1) //法术
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 13://斗神台
                for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardList[NeighbourCards[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        if (addedFightCard.cardDamageType == 0) //物理
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 14://曹魏旗
                for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardList[NeighbourCards[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        if (DataTable.Hero[addedFightCard.cardId].ForceTableId == 1) //魏势力
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 15://蜀汉旗
                for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardList[NeighbourCards[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        if (DataTable.Hero[addedFightCard.cardId].ForceTableId == 0) //蜀势力
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 16://东吴旗
                for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardList[NeighbourCards[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        if (DataTable.Hero[addedFightCard.cardId].ForceTableId == 2) //吴势力
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 19://骑兵营
                for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardList[NeighbourCards[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        var armed = MilitaryInfo.GetInfo(addedFightCard.cardId).ArmedType;
                        if (armed == 5)
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 20://弓弩营
                for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardList[NeighbourCards[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        var armed = MilitaryInfo.GetInfo(addedFightCard.cardId).ArmedType;
                        if (armed == 9)
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 21://步兵营
                for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardList[NeighbourCards[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        var armed = MilitaryInfo.GetInfo(addedFightCard.cardId).ArmedType;
                        if (armed == 2)
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 22://长持营
                for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardList[NeighbourCards[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        var armed = MilitaryInfo.GetInfo(addedFightCard.cardId).ArmedType;
                        if (armed == 3)
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 23://战船营
                for (int i = 0; i < NeighbourCards[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardList[NeighbourCards[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        var armed = MilitaryInfo.GetInfo(addedFightCard.cardId).ArmedType;
                        if (armed == 8)
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            default:
                break;
        }

    }

    //伤害加成塔的上阵下阵对周为单位的处理
    private void DamageTowerAdditionFun(FightCardData addedFightCard, bool isAdd, int addtionNums)
    {
        if (isAdd)
        {
            if (addedFightCard.fightState.zhangutaiAddtion <= 0)
            {
                CreateSateIcon(addedFightCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_zhangutaiAddtion, false);
            }
            addedFightCard.fightState.zhangutaiAddtion += addtionNums;
        }
        else
        {
            addedFightCard.fightState.zhangutaiAddtion -= addtionNums;
            if (addedFightCard.fightState.zhangutaiAddtion <= 0)
            {
                addedFightCard.fightState.zhangutaiAddtion = 0;
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
            GameObject stateIconObj = EffectsPoolingControl.instance.GetStateIconToFight("stateIcon", tran);
            stateIconObj.name = stateName;
            stateIconObj.GetComponent<Image>().sprite = Resources.Load("Image/fightStateIcon/" + stateName, typeof(Sprite)) as Sprite;
        }
        if (isShowEffect && tran.parent.Find(stateName + "Din") == null)
        {
            GameObject stateDinObj = EffectsPoolingControl.instance.GetStateIconToFight(stateName, tran.parent);
            stateDinObj.name = stateName + "Din";
        }
    }

    //删除状态图标
    public void DestroySateIcon(Transform tran, string stateName, bool isShowEffect)
    {
        Transform tran0 = tran.Find(stateName);
        if (tran0 != null)
            EffectsPoolingControl.instance.TakeBackStateIcon(tran0.gameObject);
        if (isShowEffect)
        {
            Transform tran1 = tran.parent.Find(stateName + "Din");
            if (tran1 != null)
                EffectsPoolingControl.instance.TakeBackStateIcon(tran1.gameObject);
        }
    }

    /// <summary>
    /// 更新上阵数量显示
    /// </summary>
    public void UpdateFightNumTextShow(int maxCards) => heroNumText.text = string.Format(DataTable.GetStringText(24), chessboard.PlayerCardsOnBoard, maxCards);

    public bool IsPlayerAvailableToPlace(int index) => chessboard.PlayerCardsOnBoard < WarsUIManager.instance.maxHeroNums && chessboard.IsPlayerScopeAvailable(index);

    [SerializeField]
    Text speedBtnText;

    private const string Multiply = "×";
    //改变游戏速度
    public void ChangeTimeScale()
    {
        var warScale = GamePref.PrefWarSpeed;
        warScale *= 2;
        if (warScale > 2)
            warScale = 1;
        GamePref.SetPrefWarSpeed(warScale);
        Time.timeScale = warScale;
        speedBtnText.text = Multiply + warScale;
    }

    [SerializeField]
    GameObject bZCardInfoObj;   //阵上卡牌详情展示位

    [HideInInspector]
    public bool isNeedAutoHideInfo;  //是否需要自动隐藏卡牌详情

    //private FightCardData[] enemyCards;
    //private FightCardData[] playerCards;

    //展示卡牌详细信息
    private void ShowInfoOfCardOrArms(string infoStr)
    {

        bZCardInfoObj.GetComponentInChildren<Text>().text = infoStr;
        bZCardInfoObj.SetActive(true);

        //StartCoroutine(ShowOrHideCardInfoWin());
    }
    //隐藏卡牌详细信息
    private void HideInfoOfCardOrArms()
    {
        //isNeedAutoHideInfo = false;

        bZCardInfoObj.SetActive(false);
    }

    //卡牌附加按下抬起方法
    public void GiveGameObjEventForHoldOn(WarGameCardUi obj, string str)
    {
        EventTriggerListener.Get(obj.gameObject).onDown += _ => ShowInfoOfCardOrArms(str);
        EventTriggerListener.Get(obj.gameObject).onUp += _ => HideInfoOfCardOrArms();
    }

    public IReadOnlyList<FightCardData> GetCardList(bool isPlayer) =>
        chessboard.GetScope(isPlayer).Select(o => o.Card).ToArray();

    public FightCardData GetCard(int index, bool isPlayer) => chessboard.GetCard(index, isPlayer).Card;

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
        if (attackUnit.fightState.imprisonedNums <= 0)//攻击者没有禁锢状态
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
                if (cardList[i].cardMoveType == 1)
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

    public Transform GetChessPos(int posIndex, bool isPlayer) => chessboard.GetCard(posIndex, isPlayer).transform;

    public void DestroyCard(FightCardData playerCard) => chessboard.DestroyCard(playerCard);
}