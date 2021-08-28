using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beebyte.Obfuscator;
using CorrelateLib;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class FightForManager : MonoBehaviour
{
    public static FightForManager instance;

    [SerializeField]
    Transform enemyChechBoard; //敌方棋盘
    [SerializeField]
    Transform playerChechBoard; //我方棋盘
    [SerializeField]
    GameObject enemyCardPosPre;    //敌方卡牌位置预制件
    [SerializeField]
    GameObject playerCardPosPre;    //我方卡牌位置预制件
    public Transform enemyCardsBox;    //敌方卡牌父级
    public Transform playerCardsBox;    //我方卡牌父级
    [SerializeField]
    GameObject fightCardPre;    //战斗卡牌预制件
    [SerializeField]
    Text heroNumText;
    [SerializeField]
    GameObject homeCardObj;

    public GameObject stateIconPre;

    public List<GameObject> enemyCardsPos; //敌方卡牌位置列表
    public List<GameObject> playerCardsPos; //我方卡牌位置列表

    public FightCardData[] enemyFightCardsDatas; //敌方战斗卡牌信息集合
    public FightCardData[] playerFightCardsDatas; //我方战斗卡牌信息集合

    [HideInInspector]
    public float floDisY;  //加成比
    [HideInInspector]
    public float oneDisY;  //半格高

    [SerializeField]
    GridLayoutGroup gridLayoutGroup;

    public int battleIdIndex;   //战役编号索引

    public int nowHeroNums; //记录上战卡牌数量

    //卡牌附近单位遍历次序
    public int[][] CardNearbyAdditionForeach = new int[20][] {
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
        enemyFightCardsDatas = new FightCardData[20];
        playerFightCardsDatas = new FightCardData[20];
        nowHeroNums = 0;
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
        FightCardData playerHomeData = new FightCardData();
        playerHomeData.cardObj = Instantiate(homeCardObj, playerCardsBox);
        playerHomeData.cardObj.transform.position = playerCardsPos[17].transform.position;
        playerHomeData.fullHp = playerHomeData.nowHp = DataTable.BaseLevel[WarsUIManager.instance.cityLevel].BaseHp + DataTable.PlayerLevelConfig[PlayerDataForGame.instance.pyData.Level].BaseHpAddOn;
        playerHomeData.hpr = 0;
        playerHomeData.cardType = 522;
        playerHomeData.posIndex = 17;
        playerHomeData.isPlayerCard = true;
        playerHomeData.activeUnit = false;
        playerHomeData.fightState = new FightState();
        playerFightCardsDatas[17] = playerHomeData;
        UpdateFightNumTextShow(WarsUIManager.instance.maxHeroNums);
    }

    /// <summary>
    /// 初始化敌方卡牌到战斗位上
    /// </summary>

    public void InitChessboard(GameStage stage)
    {
        //初始化敌方羁绊原始集合
        CardManager.ResetJiBan(FightController.instance.enemyJiBanAllTypes);

        battleIdIndex = stage.BattleEvent.Id;
        var baseConfig = DataTable.BaseLevel[WarsUIManager.instance.cityLevel];
        var playerLvlCfg = DataTable.PlayerLevelConfig[PlayerDataForGame.instance.pyData.Level];
        playerFightCardsDatas[17].fullHp = playerFightCardsDatas[17].nowHp = baseConfig.BaseHp + playerLvlCfg.BaseHpAddOn;
        FightController.instance.UpdateUnitHpShow(playerFightCardsDatas[17]);
        FightController.instance.ClearEmTieQiCardList();

        for (int i = 0; i < enemyFightCardsDatas.Length; i++)
        {
            if (enemyFightCardsDatas[i] != null)
            {
                Destroy(enemyFightCardsDatas[i].cardObj);
                enemyFightCardsDatas[i] = null;
            }
        }

        var battle = stage.BattleEvent;
        var enemyRandId = stage.RandomId;
        //随机敌人卡牌
        if (battle.IsStaticEnemies == 0)
        {
            var enemies = DataTable.Enemy[enemyRandId].Poses();
            for (int i = 0; i < 20; i++)
            {
                var enemyId = enemies[i];
                if (enemyId == 0) continue;
                FightCardData data = CreateEnemyFightUnit(enemyId, false);
                data.posIndex = i;
                data.isPlayerCard = false;
                data.cardObj.transform.position = enemyCardsPos[i].transform.position;
                enemyFightCardsDatas[i] = data;
                CardGoIntoBattleProcess(enemyFightCardsDatas[i], i, enemyFightCardsDatas, true);
            }
        }
        else
        {
            var enemies = DataTable.StaticArrangement[enemyRandId].Poses();
            //固定敌人卡牌
            for (int i = 0; i < 20; i++)
            {
                var chessman = enemies[i];
                if (chessman == null) continue;
                if (chessman.Star <= 0)
                    throw new InvalidOperationException(
                        $"卡牌初始化异常，[{chessman.CardId}.{chessman.CardType}]等级为{chessman.Star}!");
                FightCardData data = CreateEnemyFightUnit(1, true, chessman);
                data.posIndex = i;
                data.isPlayerCard = false;
                data.cardObj.transform.position = enemyCardsPos[i].transform.position;
                enemyFightCardsDatas[i] = data;
                CardGoIntoBattleProcess(enemyFightCardsDatas[i], i, enemyFightCardsDatas, true);
            }
        }


        FightCardData enemyHomeData = new FightCardData();
        enemyHomeData.cardObj = Instantiate(homeCardObj, enemyCardsBox);
        enemyHomeData.cardObj.transform.position = enemyCardsPos[17].transform.position;
        enemyHomeData.fullHp = enemyHomeData.nowHp = battle.BaseHp;
        enemyHomeData.hpr = 0;
        enemyHomeData.cardType = 522;
        enemyHomeData.posIndex = 17;
        enemyHomeData.isPlayerCard = false;
        enemyHomeData.activeUnit = false;
        enemyHomeData.fightState = new FightState();
        enemyFightCardsDatas[17] = enemyHomeData;

        FightController.instance.InitStartFight();

        //调整游戏速度
        var speed = GamePref.PrefWarSpeed;
        Time.timeScale = speed;
        speedBtnText.text = Multiply + speed;
    }

    //主动塔行动
    public void ActiveTowerFight(FightCardData cardData, FightCardData[] cardsDatas)
    {
        switch (cardData.cardId)
        {
            case 0://营寨
                YingZhaiFun(cardData, cardsDatas);
                break;
            case 1://投石台
                TouShiTaiAttackFun(cardData);
                break;
            case 2://奏乐台
                ZouYueTaiAddtionFun(cardData, cardsDatas);
                break;
            case 3://箭楼
                FightController.instance.JianLouYuanSheSkill(cardData, GetTowerAddValue(cardData.cardId, cardData.cardGrade));
                break;
            case 6://轩辕台
                XuanYuanTaiAddtionFun(cardData, cardsDatas);
                break;
            default:
                break;
        }
    }

    //营寨行动
    private void YingZhaiFun(FightCardData cardData, FightCardData[] cardsDatas)
    {
        if (cardData.nowHp > 1)
        {
            FightController.instance.indexAttackType = 0;
            FightController.instance.PlayAudioForSecondClip(42, 0);

            int indexCutMaxHp = 0;  //最多扣除血量
            FightCardData needAddHpCard = new FightCardData();

            for (int i = 0; i < CardNearbyAdditionForeach[cardData.posIndex].Length; i++)
            {
                FightCardData addedFightCard = cardsDatas[CardNearbyAdditionForeach[cardData.posIndex][i]];
                if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
                {
                    if (addedFightCard.fullHp - addedFightCard.nowHp > indexCutMaxHp)
                    {
                        indexCutMaxHp = addedFightCard.fullHp - addedFightCard.nowHp;
                        needAddHpCard = addedFightCard;
                    }
                }
            }

            if (indexCutMaxHp > 0)
            {
                int canAddHpNum = cardData.nowHp - 1;
                if (canAddHpNum > indexCutMaxHp)
                {
                    canAddHpNum = indexCutMaxHp;
                }
                //自身减血
                cardData.nowHp -= canAddHpNum;
                FightController.instance.AttackedAnimShow(cardData, canAddHpNum, false);
                //单位加血
                FightController.instance.AttackToEffectShow(needAddHpCard, false, "42A");
                needAddHpCard.nowHp += canAddHpNum;
                FightController.instance.ShowSpellTextObj(needAddHpCard.cardObj, DataTable.GetStringText(15), true, false);
                FightController.instance.AttackedAnimShow(needAddHpCard, canAddHpNum, true);
            }
        }
    }

    //投石台攻击
    private void TouShiTaiAttackFun(FightCardData cardData)
    {
        FightCardData[] attackedUnits = cardData.isPlayerCard ? enemyFightCardsDatas : playerFightCardsDatas;
        int damage = (int)(DataTable.GetGameValue(122) / 100f * GetTowerAddValue(cardData.cardId, cardData.cardGrade)); //造成的伤害
        FightController.instance.indexAttackType = 0;

        FightController.instance.PlayAudioForSecondClip(24, 0);

        List<GameObject> posListToThunder = cardData.isPlayerCard ? enemyCardsPos : playerCardsPos;
        EffectsPoolingControl.instance.GetEffectToFight1("101A", 1f, posListToThunder[cardData.posIndex].transform);

        if (attackedUnits[cardData.posIndex] != null && attackedUnits[cardData.posIndex].nowHp > 0)
        {
            int finalDamage = FightController.instance.DefDamageProcessFun(cardData, attackedUnits[cardData.posIndex], damage);
            attackedUnits[cardData.posIndex].nowHp -= finalDamage;
            FightController.instance.AttackedAnimShow(attackedUnits[cardData.posIndex], finalDamage, false);
            if (attackedUnits[cardData.posIndex].cardType == 522)
            {
                if (attackedUnits[cardData.posIndex].nowHp <= 0)
                {
                    FightController.instance.recordWinner = attackedUnits[cardData.posIndex].isPlayerCard ? -1 : 1;
                }
            }
        }
        for (int i = 0; i < CardNearbyAdditionForeach[cardData.posIndex].Length; i++)
        {
            FightCardData attackedUnit = attackedUnits[CardNearbyAdditionForeach[cardData.posIndex][i]];
            if (attackedUnit != null && attackedUnit.nowHp > 0)
            {
                int finalDamage = FightController.instance.DefDamageProcessFun(cardData, attackedUnit, damage);
                attackedUnit.nowHp -= finalDamage;
                FightController.instance.AttackedAnimShow(attackedUnit, finalDamage, false);
                if (attackedUnit.cardType == 522)
                {
                    if (attackedUnit.nowHp <= 0)
                    {
                        FightController.instance.recordWinner = attackedUnit.isPlayerCard ? -1 : 1;
                    }
                }
            }
        }
    }

    //奏乐台血量回复
    private void ZouYueTaiAddtionFun(FightCardData cardData, FightCardData[] cardsDatas)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);    //回复血量基值
        addtionNums = (int)(addtionNums * DataTable.GetGameValue(123) / 100f);
        FightController.instance.indexAttackType = 0;
        FightController.instance.PlayAudioForSecondClip(42, 0);
        for (int i = 0; i < CardNearbyAdditionForeach[cardData.posIndex].Length; i++)
        {
            FightCardData addedFightCard = cardsDatas[CardNearbyAdditionForeach[cardData.posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
            {
                FightController.instance.AttackToEffectShow(addedFightCard, false, "42A");
                addedFightCard.nowHp += addtionNums;
                FightController.instance.ShowSpellTextObj(addedFightCard.cardObj, DataTable.GetStringText(15), true, false);
                FightController.instance.AttackedAnimShow(addedFightCard, addtionNums, true);
            }
        }
    }

    //轩辕台护盾加成
    private void XuanYuanTaiAddtionFun(FightCardData cardData, FightCardData[] cardsDatas)
    {
        FightController.instance.PlayAudioForSecondClip(4, 0);
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);    //添加护盾的单位最大数
        for (int i = 0; i < CardNearbyAdditionForeach[cardData.posIndex].Length; i++)
        {
            FightCardData addedFightCard = cardsDatas[CardNearbyAdditionForeach[cardData.posIndex][i]];
            if (addedFightCard != null && addedFightCard.nowHp > 0)
            {
                if (addedFightCard.cardType == 0 && addedFightCard.fightState.withStandNums <= 0)
                {
                    FightController.instance.AttackToEffectShow(addedFightCard, false, "4A");
                    addedFightCard.fightState.withStandNums = 1;
                    CreateSateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_withStand, true);
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
            Transform tranZyt = cardData.cardObj.transform.GetChild(7).Find(StringNameStatic.StateIconPath_zouyuetaiAddtion);
            if (tranZyt != null)
                DestroyImmediate(tranZyt.gameObject);
        }
        //战斗力图标
        if (cardData.fightState.zhangutaiAddtion > 0)
        {
            cardData.fightState.zhangutaiAddtion = 0;
            DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
        }
        //风神台图标
        if (cardData.fightState.fengShenTaiAddtion > 0)
        {
            cardData.fightState.fengShenTaiAddtion = 0;
            DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_fengShenTaiAddtion, false);
        }
        //霹雳台图标
        if (cardData.fightState.pilitaiAddtion > 0)
        {
            cardData.fightState.pilitaiAddtion = 0;
            DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_pilitaiAddtion, false);
        }
        //霹雳台图标
        if (cardData.fightState.pilitaiAddtion > 0)
        {
            cardData.fightState.pilitaiAddtion = 0;
            DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_pilitaiAddtion, false);
        }
        //狼牙台图标
        if (cardData.fightState.langyataiAddtion > 0)
        {
            cardData.fightState.langyataiAddtion = 0;
            DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_langyataiAddtion, false);
        }
        //烽火台图标
        if (cardData.fightState.fenghuotaiAddtion > 0)
        {
            cardData.fightState.fenghuotaiAddtion = 0;
            DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_fenghuotaiAddtion, false);
        }
        //迷雾阵图标
        if (cardData.fightState.miWuZhenAddtion > 0)
        {
            cardData.fightState.miWuZhenAddtion = 0;
            DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_miWuZhenAddtion, false);
        }
    }

    /// <summary>
    /// 尝试激活或取消羁绊
    /// </summary>
    /// <param name="cardData">行动卡牌</param>
    /// <param name="isAdd">是否上阵</param>
    public void TryToActivatedBond(FightCardData cardData, bool isAdd)
    {
        var jiBanIds = DataTable.Hero[cardData.cardId].JiBanIds;
        if (jiBanIds == null || jiBanIds.Length == 0)return;
        Dictionary<int, JiBanActivedClass> jiBanActivedClasses = cardData.isPlayerCard
            ? FightController.instance.playerJiBanAllTypes
            : FightController.instance.enemyJiBanAllTypes;

        //遍历所属羁绊
        for (int i = 0; i < jiBanIds.Length; i++)
        {
            var jiBan = DataTable.JiBan[jiBanIds[i]];
            if(jiBan.IsOpen == 0)continue;
            JiBanActivedClass jiBanActivedClass = jiBanActivedClasses[jiBan.Id];
            bool isActived = true;
            for (int j = 0; j < jiBanActivedClass.cardTypeLists.Count; j++)
            {
                if (jiBanActivedClass.cardTypeLists[j].cardType == cardData.cardType
                    && (jiBanActivedClass.cardTypeLists[j].cardId == cardData.cardId
                        || (jiBanActivedClass.isHadBossId &&
                            jiBanActivedClass.cardTypeLists[j].bossId == cardData.cardId)))
                {
                    if (isAdd)
                    {
                        if (!jiBanActivedClass.cardTypeLists[j].cardLists.Exists(t => t == cardData))
                        {
                            jiBanActivedClass.cardTypeLists[j].cardLists.Add(cardData);
                        }
                    }
                    else
                    {
                        jiBanActivedClass.cardTypeLists[j].cardLists.Remove(cardData);
                    }
                }

                if (jiBanActivedClass.cardTypeLists[j].cardLists.Count <= 0)
                {
                    isActived = false;
                }
            }

            if (cardData.isPlayerCard)
            {
                if (!jiBanActivedClass.isActived && isActived)
                {
                    for (int k = 0; k < jiBanActivedClass.cardTypeLists.Count; k++)
                    {
                        for (int s = 0; s < jiBanActivedClass.cardTypeLists[k].cardLists.Count; s++)
                        {
                            jiBanActivedClass.cardTypeLists[k].cardLists[s].cardObj.transform.GetChild(10)
                                .gameObject.SetActive(false);
                            jiBanActivedClass.cardTypeLists[k].cardLists[s].cardObj.transform.GetChild(10)
                                .gameObject.SetActive(true);
                        }
                    }
                }

                if (!isActived)
                {
                    for (int k = 0; k < jiBanActivedClass.cardTypeLists.Count; k++)
                    {
                        for (int s = 0; s < jiBanActivedClass.cardTypeLists[k].cardLists.Count; s++)
                        {
                            jiBanActivedClass.cardTypeLists[k].cardLists[s].cardObj.transform.GetChild(10)
                                .gameObject.SetActive(false);
                        }
                    }
                }
            }

            jiBanActivedClass.isActived = isActived;
            //Debug.Log("羁绊: " + jiBanActivedClass.jiBanIndex+ ", isActived: " + jiBanActivedClass.isActived);
        }
    }

    //上阵卡牌特殊相关处理
    public void CardGoIntoBattleProcess(FightCardData cardData, int posIndex, FightCardData[] cardDatas, bool isAdd, bool isNeedRefresh = true)
    {
        switch (cardData.cardType)
        {
            case 0:
                if (isNeedRefresh)
                {
                    TryToActivatedBond(cardData, isAdd);
                }

                var military = MilitaryInfo.GetInfo(cardData.cardId).Id;
                switch (military)
                {
                    case 4://盾兵
                        if (isAdd)
                        {
                            if (cardData.fightState.withStandNums <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_withStand, true);
                            }
                            cardData.fightState.withStandNums = 1;
                        }
                        else
                        {
                            cardData.fightState.withStandNums = 0;
                            DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_withStand, true);
                        }
                        break;
                    case 58://铁骑
                        FightController.instance.UpdateTieQiStateIconShow(cardData, isAdd);
                        break;
                    default:
                        break;
                }
                if (!isAdd)
                {
                    HeroCardRemoveStatus(cardData);
                }
                else
                {
                    HeroSoldierAddtionFun(cardData, posIndex, cardDatas);
                }
                break;
            case 1:
                if (!isAdd)
                {
                    HeroCardRemoveStatus(cardData);
                }
                else
                {
                    HeroSoldierAddtionFun(cardData, posIndex, cardDatas);
                }
                break;
            case 2:
                switch (cardData.cardId)
                {
                    case 2://奏乐台
                        ZouYueTaiAddIcon(posIndex, cardDatas, isAdd);
                        break;
                    case 4://战鼓台
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 5://风神台
                        FengShenTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 7://霹雳台
                        PiLiTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 8://狼牙台
                        LangYaTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 9://烽火台
                        FengHuoTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 10://号角台
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 11://瞭望台
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 12://七星坛
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 13://斗神台
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 14://曹魏旗
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 15://蜀汉旗
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 16://东吴旗
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 17://迷雾阵
                        miWuZhenAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 18://迷雾阵
                        miWuZhenAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 19://骑兵营
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 20://弓弩营
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 21://步兵营
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 22://长持营
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 23://战船营
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
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
    private void HeroSoldierAddtionFun(FightCardData cardData, int posIndex, FightCardData[] cardDatas)
    {
        var armed = MilitaryInfo.GetInfo(cardData.cardId).ArmedType;
        for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
        {
            FightCardData addtionCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
            if (addtionCard != null && addtionCard.cardType == 2 && addtionCard.nowHp > 0)
            {
                int addtionNums = GetTowerAddValue(addtionCard.cardId, addtionCard.cardGrade);
                switch (addtionCard.cardId)
                {
                    case 2://奏乐台
                        if (cardData.cardObj.transform.GetChild(7).Find(StringNameStatic.StateIconPath_zouyuetaiAddtion) == null)
                        {
                            CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zouyuetaiAddtion, false);
                        }
                        break;
                    case 4://战鼓台
                        if (cardData.fightState.zhangutaiAddtion <= 0)
                        {
                            CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                        }
                        cardData.fightState.zhangutaiAddtion += addtionNums;
                        break;
                    case 5://风神台
                        if (cardData.fightState.fengShenTaiAddtion <= 0)
                        {
                            CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_fengShenTaiAddtion, false);
                        }
                        cardData.fightState.fengShenTaiAddtion += addtionNums;
                        break;
                    case 7://霹雳台
                        if (cardData.fightState.pilitaiAddtion <= 0)
                        {
                            CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_pilitaiAddtion, false);
                        }
                        cardData.fightState.pilitaiAddtion += addtionNums;
                        break;
                    case 8://狼牙台
                        if (cardData.fightState.langyataiAddtion <= 0)
                        {
                            CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_langyataiAddtion, false);
                        }
                        cardData.fightState.langyataiAddtion += addtionNums;
                        break;
                    case 9://烽火台
                        if (cardData.fightState.fenghuotaiAddtion <= 0)
                        {
                            CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_fenghuotaiAddtion, false);
                        }
                        cardData.fightState.fenghuotaiAddtion += addtionNums;
                        break;
                    case 10://号角台
                        if (cardData.cardMoveType == 0)   //近战
                        {
                            if (cardData.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 11://瞭望台
                        if (cardData.cardMoveType == 1)   //远程
                        {
                            if (cardData.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 12://七星坛
                        if (cardData.cardDamageType == 1) //法术
                        {
                            if (cardData.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 13://斗神台
                        if (cardData.cardDamageType == 0) //物理
                        {
                            if (cardData.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 14://曹魏旗
                        if (DataTable.Hero[cardData.cardId].ForceTableId == 1)
                        {
                            if (cardData.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 15://蜀汉旗
                        if (DataTable.Hero[cardData.cardId].ForceTableId == 0)
                        {
                            if (cardData.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 16://东吴旗
                        if (DataTable.Hero[cardData.cardId].ForceTableId == 2)
                        {
                            if (cardData.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 17://迷雾阵
                        if (cardData.fightState.miWuZhenAddtion <= 0)
                        {
                            CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_miWuZhenAddtion, false);
                        }
                        cardData.fightState.miWuZhenAddtion += addtionNums;
                        break;
                    case 18://迷雾阵
                        if (cardData.fightState.miWuZhenAddtion <= 0)
                        {
                            CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_miWuZhenAddtion, false);
                        }
                        cardData.fightState.miWuZhenAddtion += addtionNums;
                        break;
                    case 19://骑兵营
                        if (armed == 5)
                        {
                            if (cardData.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 20://弓弩营
                        if (armed == 9)
                        {
                            if (cardData.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 21://步兵营
                        if (armed == 2)
                        {
                            if (cardData.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 22://长持营
                        if (armed == 3)
                        {
                            if (cardData.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    case 23://战船营
                        if (armed == 8)
                        {
                            if (cardData.fightState.zhangutaiAddtion <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.fightState.zhangutaiAddtion += addtionNums;
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }

    //奏乐台状态栏添加
    private void ZouYueTaiAddIcon(int posIndex, FightCardData[] cardDatas, bool isAdd)
    {
        for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
        {
            FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
            {
                if (isAdd)
                {
                    if (addedFightCard.cardObj.transform.GetChild(7).Find(StringNameStatic.StateIconPath_zouyuetaiAddtion) == null)
                    {
                        CreateSateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zouyuetaiAddtion, false);
                    }
                }
                else
                {
                    if (addedFightCard.cardObj.transform.GetChild(7).Find(StringNameStatic.StateIconPath_zouyuetaiAddtion) != null)
                    {
                        DestroyImmediate(addedFightCard.cardObj.transform.GetChild(7).Find(StringNameStatic.StateIconPath_zouyuetaiAddtion).gameObject);
                    }
                }
            }
        }
    }

    //烽火台加成技能
    private void FengHuoTaiAddtionFun(FightCardData cardData, int posIndex, FightCardData[] cardDatas, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);

        for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
        {
            FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
            {
                if (isAdd)
                {
                    if (addedFightCard.fightState.fenghuotaiAddtion <= 0)
                    {
                        CreateSateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_fenghuotaiAddtion, false);
                    }
                    addedFightCard.fightState.fenghuotaiAddtion += addtionNums;
                }
                else
                {
                    addedFightCard.fightState.fenghuotaiAddtion -= addtionNums;
                    if (addedFightCard.fightState.fenghuotaiAddtion <= 0)
                    {
                        addedFightCard.fightState.fenghuotaiAddtion = 0;
                        DestroySateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_fenghuotaiAddtion, false);
                    }
                }
            }
        }
    }

    //狼牙台加成技能
    private void LangYaTaiAddtionFun(FightCardData cardData, int posIndex, FightCardData[] cardDatas, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);

        for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
        {
            FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
            {
                if (isAdd)
                {
                    if (addedFightCard.fightState.langyataiAddtion <= 0)
                    {
                        CreateSateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_langyataiAddtion, false);
                    }
                    addedFightCard.fightState.langyataiAddtion += addtionNums;
                }
                else
                {
                    addedFightCard.fightState.langyataiAddtion -= addtionNums;
                    if (addedFightCard.fightState.langyataiAddtion <= 0)
                    {
                        addedFightCard.fightState.langyataiAddtion = 0;
                        DestroySateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_langyataiAddtion, false);
                    }
                }
            }
        }
    }

    //霹雳台加成技能
    private void PiLiTaiAddtionFun(FightCardData cardData, int posIndex, FightCardData[] cardDatas, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);

        for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
        {
            FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
            {
                if (isAdd)
                {
                    if (addedFightCard.fightState.pilitaiAddtion <= 0)
                    {
                        CreateSateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_pilitaiAddtion, false);
                    }
                    addedFightCard.fightState.pilitaiAddtion += addtionNums;
                }
                else
                {
                    addedFightCard.fightState.pilitaiAddtion -= addtionNums;
                    if (addedFightCard.fightState.pilitaiAddtion <= 0)
                    {
                        addedFightCard.fightState.pilitaiAddtion = 0;
                        DestroySateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_pilitaiAddtion, false);
                    }
                }
            }
        }
    }

    //迷雾阵加成技能
    private void miWuZhenAddtionFun(FightCardData cardData, int posIndex, FightCardData[] cardDatas, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);

        List<GameObject> posListToSetMiWu = cardData.isPlayerCard ? playerCardsPos : enemyCardsPos;

        for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
        {
            if (!cardData.isPlayerCard)
            {
                //迷雾动画
                GameObject stateDinObj = Instantiate(Resources.Load("Prefabs/stateDin/" + StringNameStatic.StateIconPath_miWuZhenAddtion, typeof(GameObject)) as GameObject, cardData.cardObj.transform);
                stateDinObj.name = StringNameStatic.StateIconPath_miWuZhenAddtion + "Din";
                stateDinObj.transform.position = posListToSetMiWu[CardNearbyAdditionForeach[posIndex][i]].transform.position;
                stateDinObj.GetComponent<Animator>().enabled = false;
            }
            FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
            {
                if (isAdd)
                {
                    if (addedFightCard.fightState.miWuZhenAddtion <= 0)
                    {
                        CreateSateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_miWuZhenAddtion, false);
                    }
                    addedFightCard.fightState.miWuZhenAddtion += addtionNums;
                }
                else
                {
                    addedFightCard.fightState.miWuZhenAddtion -= addtionNums;
                    if (addedFightCard.fightState.miWuZhenAddtion <= 0)
                    {
                        addedFightCard.fightState.miWuZhenAddtion = 0;
                        DestroySateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_miWuZhenAddtion, false);
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
    private void FengShenTaiAddtionFun(FightCardData cardData, int posIndex, FightCardData[] cardDatas, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);

        for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
        {
            FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
            {
                if (isAdd)
                {
                    if (addedFightCard.fightState.fengShenTaiAddtion <= 0)
                    {
                        CreateSateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_fengShenTaiAddtion, false);
                    }
                    addedFightCard.fightState.fengShenTaiAddtion += addtionNums;
                }
                else
                {
                    addedFightCard.fightState.fengShenTaiAddtion -= addtionNums;
                    if (addedFightCard.fightState.fengShenTaiAddtion <= 0)
                    {
                        addedFightCard.fightState.fengShenTaiAddtion = 0;
                        DestroySateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_fengShenTaiAddtion, false);
                    }
                }
            }
        }
    }

    //战鼓台(10-13塔)加成技能
    private void ZhanGuTaiAddtionFun(FightCardData cardData, int posIndex, FightCardData[] cardDatas, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.cardGrade);

        switch (cardData.cardId)
        {
            case 4://战鼓台
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
                        DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                }
                break;
            case 10://号角台
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
                    {
                        if (addedFightCard.cardMoveType == 0)   //近战
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 11://瞭望台
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
                    {
                        if (addedFightCard.cardMoveType == 1)   //远程
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 12://七星坛
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
                    {
                        if (addedFightCard.cardDamageType == 1) //法术
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 13://斗神台
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
                    {
                        if (addedFightCard.cardDamageType == 0) //物理
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 14://曹魏旗
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
                    {
                        if (DataTable.Hero[addedFightCard.cardId].ForceTableId == 1) //魏势力
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 15://蜀汉旗
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
                    {
                        if (DataTable.Hero[addedFightCard.cardId].ForceTableId == 0) //蜀势力
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 16://东吴旗
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
                    {
                        if (DataTable.Hero[addedFightCard.cardId].ForceTableId == 2) //吴势力
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 19://骑兵营
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
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
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
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
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
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
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
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
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.nowHp > 0)
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
                CreateSateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
            }
            addedFightCard.fightState.zhangutaiAddtion += addtionNums;
        }
        else
        {
            addedFightCard.fightState.zhangutaiAddtion -= addtionNums;
            if (addedFightCard.fightState.zhangutaiAddtion <= 0)
            {
                addedFightCard.fightState.zhangutaiAddtion = 0;
                DestroySateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
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

    //创建敌方卡牌战斗单位数据
    private FightCardData CreateEnemyFightUnit(int unitId, bool isFixed, Chessman chessman = null)
    {
        FightCardData data = new FightCardData();
        data.cardObj = Instantiate(fightCardPre, enemyCardsBox);
        data.fightState = new FightState();
        data.unitId = unitId;
        var enemy = DataTable.EnemyUnit[unitId];
        //是否是固定阵容
        if (isFixed)
        {
            data.cardType = chessman.CardType;
            data.cardId = chessman.CardId;
            data.cardGrade = chessman.Star;
        }
        else
        {
            data.cardType = DataTable.EnemyUnit[unitId].CardType;
            data.cardId = GameCardInfo.RandomPick((GameCardType) enemy.CardType, enemy.Rarity).Id;
            data.cardGrade = enemy.Star;
        }

        var info = GameCardInfo.GetInfo((GameCardType) data.cardType, data.cardId);
        var resources = GameSystem.GameResources;
        //兵种
        data.cardObj.transform.GetChild(1).GetComponent<Image>().sprite = info.Type == GameCardType.Hero
            ? resources.HeroImg[data.cardId]
            : resources.FuZhuImg[info.ImageId];
        //血量
        data.cardObj.transform.GetChild(2).GetComponent<Image>().fillAmount = 0;
        //名字
        WarsUIManager.instance.ShowNameTextRules(data.cardObj.transform.GetChild(3).GetComponent<Text>(),info.Name);
        //名字颜色
        data.cardObj.transform.GetChild(3).GetComponent<Text>().color = NameColorChoose(info.Rare);
        //星级
        data.cardObj.transform.GetChild(4).GetComponent<Image>().sprite = resources.GradeImg[data.cardGrade];
        //兵种
        data.cardObj.transform.GetChild(5).GetComponentInChildren<Text>().text = info.Short;
        //边框
        data.cardObj.transform.GetChild(5).GetComponent<Image>().sprite = resources.ClassImg[0];
        WarsUIManager.instance.FrameChoose(info.Rare, data.cardObj.transform.GetChild(6).GetComponent<Image>());
        data.damage = info.GetDamage(data.cardGrade);
        data.hpr = info.GameSetRecovery;
        data.fullHp = data.nowHp = info.GetHp(data.cardGrade);
        data.activeUnit = info.Type == GameCardType.Hero || ((info.Type == GameCardType.Tower) &&
                                                            (info.Id == 0 || info.Id == 1 || info.Id == 2 ||
                                                             info.Id == 3 || info.Id == 6));
        data.cardMoveType = info.CombatType;
        data.cardDamageType = info.DamageType;
        GiveGameObjEventForHoldOn(data.cardObj, info.About);
        return data;
    }
    /// <summary>
    /// 匹配稀有度的颜色
    /// </summary>
    /// <returns></returns>
    private Color NameColorChoose(int rarity)
    {
        Color color = new Color();
        switch (rarity)
        {
            case 1:
                color = ColorDataStatic.name_gray;
                break;
            case 2:
                color = ColorDataStatic.name_green;
                break;
            case 3:
                color = ColorDataStatic.name_blue;
                break;
            case 4:
                color = ColorDataStatic.name_purple;
                break;
            case 5:
                color = ColorDataStatic.name_orange;
                break;
            case 6:
                color = ColorDataStatic.name_red;
                break;
            case 7:
                color = ColorDataStatic.name_black;
                break;
            default:
                color = ColorDataStatic.name_gray;
                break;
        }
        return color;
    }

    /// <summary>
    /// 更新上阵数量显示
    /// </summary>
    public void UpdateFightNumTextShow(int maxCards)
    {
        heroNumText.text = string.Format(DataTable.GetStringText(24), nowHeroNums,maxCards);
    }

    //是否能成功上阵
    public bool ChangeNumsSuccess(bool isAdd)
    {
        var maxCard = WarsUIManager.instance.maxHeroNums;
        if (isAdd)
        {
            if (nowHeroNums < maxCard)
            {
                nowHeroNums++;
                UpdateFightNumTextShow(maxCard);
                return true;
            }
            else
            {
                nowHeroNums = maxCard;
                return false;
            }
        }
        else
        {
            if (nowHeroNums > 0)
            {
                nowHeroNums--;
                UpdateFightNumTextShow(maxCard);
            }
            else
            {
                nowHeroNums = 0;
            }
            return true;
        }
    }

    [SerializeField]
    Text speedBtnText;

    private const string Multiply = "×";
    //改变游戏速度
    public void ChangeTimeScale()
    {
        var warScale = GamePref.PrefWarSpeed;
        warScale *= 2;
        if (warScale> 2)
            warScale = 1;
        GamePref.SetPrefWarSpeed(warScale);
        Time.timeScale = warScale;
        speedBtnText.text = Multiply + warScale;
    }

    [SerializeField]
    GameObject bZCardInfoObj;   //阵上卡牌详情展示位

    [HideInInspector]
    public bool isNeedAutoHideInfo;  //是否需要自动隐藏卡牌详情

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

    IEnumerator ShowOrHideCardInfoWin()
    {
        yield return new WaitForSeconds(4f);
        if (isNeedAutoHideInfo)
        {
            HideInfoOfCardOrArms();
        }
    }

    //卡牌附加按下抬起方法
    public void GiveGameObjEventForHoldOn(GameObject obj, string str)
    {
        EventTriggerListener.Get(obj).onDown += (go) =>
        {
            ShowInfoOfCardOrArms(str);
        };
        EventTriggerListener.Get(obj).onUp += (go) =>
        {
            HideInfoOfCardOrArms();
        };
    }
}