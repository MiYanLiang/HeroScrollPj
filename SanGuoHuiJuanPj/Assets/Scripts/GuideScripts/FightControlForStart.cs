using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using CorrelateLib;
using UnityEngine;
using UnityEngine.UI;

public class FightControlForStart : MonoBehaviour
{
    public static FightControlForStart instance;

    StateOfFight stateOfFight;  //战斗状态

    //跳过战斗
    public void TiaoGuoZD()
    {
        recordWinner = 1;
    }

    [HideInInspector]
    public int recordWinner;    //标记胜负-1.0.1

    private int roundNums;

    [HideInInspector]
    public bool isRoundBegin;

    [HideInInspector]
    public bool isPlayerRound;

    private int fightUnitIndex; //行动单位索引

    [SerializeField]
    private float attackShakeTimeToGo;  //移动时间，近战单位
    [SerializeField]
    private float attackShakeTimeToBack;  //移动时间
    [SerializeField]
    private float attackIntervalTime;   //间隔时间

    private float timerForFight;

    /// <summary>
    /// 记录攻击种类，0普通，1会心，2暴击
    /// </summary>
    public int indexAttackType;

    int targetIndex;    //目标卡牌id

    [SerializeField]
    GameObject startFightBtn;   //开战按钮

    [SerializeField]
    Transform transferStation;  //卡牌中转站

    [SerializeField]
    Toggle autoFightTog;    //自动战斗勾选控件

    bool isNeedToAttack;    //记录特殊远程兵种这次攻击是否是普攻

    [SerializeField]
    GameObject fightBackForShake;
    [SerializeField]
    float doShakeIntensity;

    [SerializeField]
    AudioSource audioSource;

    private List<FightCardData> gunMuCards; //滚木列表
    private List<FightCardData> gunShiCards;//滚石列表

    /// <summary>
    /// 玩家所有羁绊激活情况
    /// </summary>
    public Dictionary<int, JiBanActivedClass> playerJiBanAllTypes;
    /// <summary>
    /// 敌方所有羁绊激活情况
    /// </summary>
    public Dictionary<int, JiBanActivedClass> enemyJiBanAllTypes;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void Init()
    {
        gunMuCards = new List<FightCardData>();
        gunShiCards = new List<FightCardData>();

        playerJiBanAllTypes = new Dictionary<int, JiBanActivedClass>();
        enemyJiBanAllTypes = new Dictionary<int, JiBanActivedClass>();

        stateOfFight = StateOfFight.ReadyForFight;

        roundNums = 0;
        fightUnitIndex = 0;
        isRoundBegin = false;
        isPlayerRound = true;
        timerForFight = 0;
        targetIndex = -1;
    }

    //普攻
    IEnumerator PuTongGongji(float damageBonus, FightCardData attackUnit, FightCardData attackedUnit, bool isCanFightBack)
    {
        isNeedToAttack = true;
        //攻击的是老家
        var armed = MilitaryInfo.GetInfo(attackUnit.cardId).ArmedType;
        if (attackedUnit.cardType == 522)
        {
            if (armed == 28 ||
                armed == 29 ||
                armed == 32 ||
                armed == 33) { }
            else
            {
                int cutHpNum = (int)(HeroCardMakeSomeDamages(isCanFightBack, attackUnit) * damageBonus);
                cutHpNum = FightDamageForSpecialSkill(cutHpNum, attackUnit, attackedUnit, isCanFightBack);
                if (isNeedToAttack)
                {
                    attackedUnit.Hp.Add(-cutHpNum);
                    AttackedAnimShow(attackedUnit, cutHpNum, false);
                    if (attackedUnit.Hp <= 0)
                    {
                        recordWinner = attackedUnit.isPlayerCard ? -1 : 1;
                    }
                }
            }
        }
        else
        {
            //攻击的是陷阱单位
            if (attackedUnit.cardType == 3)
            {
                if (armed == 28 ||
                    armed == 29 ||
                    armed == 32 ||
                    armed == 33) { }
                else
                {
                    int finalDamage = (int)(HeroCardMakeSomeDamages(isCanFightBack, attackUnit) * damageBonus);
                    finalDamage = FightDamageForSpecialSkill(finalDamage, attackUnit, attackedUnit, isCanFightBack);
                    if (isNeedToAttack)
                    {
                        AttackTrapUnit(finalDamage, attackUnit, attackedUnit, isCanFightBack);
                    }
                }
            }
            else
            {
                //攻击的是塔单位
                if (attackedUnit.cardType == 2)
                {
                    if (armed == 28 ||
                        armed == 29 ||
                        armed == 32 ||
                        armed == 33) { }
                    else
                    {
                        int finalDamage = (int)(HeroCardMakeSomeDamages(isCanFightBack, attackUnit) * damageBonus);
                        finalDamage = FightDamageForSpecialSkill(finalDamage, attackUnit, attackedUnit, isCanFightBack);
                        if (isNeedToAttack)
                        {
                            attackedUnit.Hp.Add(-finalDamage);
                            AttackedAnimShow(attackedUnit, finalDamage, false);
                        }
                    }
                }
                else
                {
                    //攻击的是武将单位
                    int finalDamage = (int)(HeroCardMakeSomeDamages(isCanFightBack, attackUnit) * damageBonus);
                    finalDamage = FightDamageForSpecialSkill(finalDamage, attackUnit, attackedUnit, isCanFightBack);
                    finalDamage = DefDamageProcessFun(attackUnit, attackedUnit, finalDamage);
                    finalDamage = TieQiFenTan(finalDamage, attackedUnit);
                    if (isNeedToAttack)
                    {
                        finalDamage = AddOrCutShieldValue(finalDamage, attackedUnit, false);
                        attackedUnit.Hp.Add(-finalDamage);
                        AttackedAnimShow(attackedUnit, finalDamage, false);
                    }
                    yield return StartCoroutine(SpecialHeroSkill0(finalDamage, attackUnit, attackedUnit, isCanFightBack));
                }
            }
        }
        yield return new WaitForSeconds(attackIntervalTime);

        if (isCanFightBack)
        {
            yield return StartCoroutine(SpecialHeroSkill1(damageBonus, attackUnit, attackedUnit));
        }
    }

    /// <summary>
    /// 计算暴击会心等伤害加成
    /// </summary>
    /// <param name="isCanAdd"></param>
    /// <param name="fightCardData"></param>
    /// <returns></returns>
    private int HeroCardMakeSomeDamages(bool isCanAdd, FightCardData fightCardData)
    {
        var combat = HeroCombatInfo.GetInfo(fightCardData.cardId);
         int damage = (int)(fightCardData.damage * (fightCardData.CardState.StrengthUp + 100) / 100f);
        if (isCanAdd)
        {
            switch (indexAttackType)
            {
                case 1:
                    damage = (int)combat.GetRouseDamage(damage);
                    break;
                case 2:
                    damage = (int)combat.GetCriticalDamage(damage);
                    break;
                default:
                    break;
            }
        }
        if (fightCardData.cardType == 0 && fightCardData.CardState.Unarmed > 0)
        {
            //若攻击者有卸甲状态伤害降低30%
            damage = (int)(damage * (100 - DataTable.GetGameValue(7)) / 100f);
        }
        return damage;
    }

    ///////攻击trap单位////////////
    private void AttackTrapUnit(int damage, FightCardData attackUnit, FightCardData attackedUnit, bool isCanFightBack)
    {
        var isGongChengChe = ((GameCardType)attackUnit.cardType) == GameCardType.Hero &&
                             DataTable.Hero[attackUnit.cardId].MilitaryUnitTableId == 23;//攻城车
        switch (attackedUnit.cardId)
        {
            case 0://拒马
                attackedUnit.Hp.Add(-damage);
                AttackedAnimShow(attackedUnit, damage, false);
                if (isCanFightBack && attackUnit.combatType == 0 && !isGongChengChe)
                {
                    damage = DefDamageProcessFun(attackedUnit, attackUnit, damage);
                    attackUnit.Hp.Add(-(int)(damage * (DataTable.GetGameValue(8) / 100f)));
                    GameObject effectObj = AttackToEffectShow(attackUnit, false, Effect.Blademail7A);
                    effectObj.transform.localScale = new Vector3(1, attackedUnit.isPlayerCard ? 1 : -1, 1);
                    AttackedAnimShow(attackUnit, damage, false);
                    PlayAudioForSecondClip(89, 0.2f);
                }
                break;
            case 1://地雷
                attackedUnit.Hp.Add(-damage);
                AttackedAnimShow(attackUnit, damage, false);
                if (isCanFightBack && attackUnit.combatType == 0)  //踩地雷的是近战
                {
                    var dmg = GameCardInfo.GetInfo(GameCardType.Trap, attackUnit.cardId)
                        .GetDamage(attackUnit.cardGrade);
                    //todo : 这里的代码与FightController(351)不同
                    int dileiDamage = (int)(dmg * DataTable.GetGameValue(9) / 100f);
                    dileiDamage = DefDamageProcessFun(attackedUnit, attackUnit, dileiDamage);
                    attackUnit.Hp.Add(-dileiDamage);
                    AttackToEffectShow(attackUnit, false, Effect.Explode);
                    AttackedAnimShow(attackUnit, dileiDamage, false);
                    PlayAudioForSecondClip(88, 0.2f);
                }
                break;
            case 2://石墙
                attackedUnit.Hp.Add(-damage);
                AttackedAnimShow(attackedUnit, damage, false);
                break;
            case 3://八阵图
                attackedUnit.Hp.Add(-damage);
                AttackedAnimShow(attackedUnit, damage, false);
                if (attackUnit.combatType == 0 && !isGongChengChe)
                {
                    TakeOneUnitDizzed(attackUnit, DataTable.GetGameValue(133));
                }
                break;
            case 4://金锁阵
                attackedUnit.Hp.Add(-damage);
                AttackedAnimShow(attackedUnit, damage, false);
                if (attackUnit.combatType == 0 && !isGongChengChe)
                {
                    TakeToImprisoned(attackUnit, DataTable.GetGameValue(10));
                }
                break;
            case 5://鬼兵阵
                attackedUnit.Hp.Add(-damage);
                AttackedAnimShow(attackedUnit, damage, false);
                if (attackUnit.combatType == 0 && !isGongChengChe)
                {
                    TakeToCowardly(attackUnit, DataTable.GetGameValue(11));
                }
                break;
            case 6://火墙
                attackedUnit.Hp.Add(-damage);
                AttackedAnimShow(attackedUnit, damage, false);
                if (isCanFightBack && attackUnit.combatType == 0 && !isGongChengChe)
                {
                    TakeToBurn(attackUnit, DataTable.GetGameValue(12));
                }
                break;
            case 7://毒泉
                attackedUnit.Hp.Add(-damage);
                AttackedAnimShow(attackedUnit, damage, false);
                if (isCanFightBack && attackUnit.combatType == 0 && !isGongChengChe)
                {
                    TakeToPoisoned(attackUnit, DataTable.GetGameValue(13));
                }
                break;
            case 8://刀墙
                attackedUnit.Hp.Add(-damage);
                AttackedAnimShow(attackedUnit, damage, false);
                if (isCanFightBack && attackUnit.combatType == 0 && !isGongChengChe)
                {
                    TakeToBleed(attackUnit, DataTable.GetGameValue(14));
                }
                break;
            case 9://滚石
                attackedUnit.Hp.Add(-damage);
                AttackedAnimShow(attackedUnit, damage, false);
                break;
            case 10://滚木
                attackedUnit.Hp.Add(-damage);
                AttackedAnimShow(attackedUnit, damage, false);
                break;
            case 11://金币宝箱
                if (attackedUnit.Hp > 0)
                {
                    attackedUnit.Hp.Add(-damage);
                    GetGoldBoxFun(attackedUnit);
                }
                AttackedAnimShow(attackedUnit, damage, false);
                break;
            default:
                break;
        }
    }

    #region 陷阱单位特殊方法
    //获得金币宝箱
    private void GetGoldBoxFun(FightCardData attackedUnit)
    {
        if (attackedUnit.Hp <= 0)
        {
            GameObject obj = EffectsPoolingControl.instance.GetEffectToFight(Effect.GetGold, 1.5f, attackedUnit.cardObj);
            obj.GetComponentInChildren<Text>().text = string.Format(DataTable.GetStringText(8), DataTable.EnemyUnit[attackedUnit.unitId].GoldReward);
            PlayAudioForSecondClip(98, 0);
        }
    }

    IEnumerator GunMuGunShiSkill(List<FightCardData> gunMuList, List<FightCardData> gunShiList)
    {
        for (int i = 0; i < gunMuList.Count; i++)
        {
            if (gunMuList[i].Hp <= 0 && !gunMuList[i].isActionDone)
            {
                yield return StartCoroutine(GunMuTrapAttack(gunMuList[i], 1f, DataTable.GetGameValue(15)));
            }
        }

        for (int i = 0; i < gunShiList.Count; i++)
        {
            if (gunShiList[i].Hp <= 0 && !gunShiList[i].isActionDone)
            {
                yield return StartCoroutine(GunShiTrapAttack(gunShiList[i], 1f, DataTable.GetGameValue(16)));
            }
        }
    }

    //滚木反击
    IEnumerator GunMuTrapAttack(FightCardData attackUnit, float damageRate, int dizzedRate)
    {
        attackUnit.isActionDone = true;

        yield return new WaitForSeconds(attackShakeTimeToGo / 2);

        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.enemyFightCardsDatas : FightForManagerForStart.instance.playerFightCardsDatas;
        PlayAudioForSecondClip(95, 0);
        MoveToFightWay0(attackUnit, attackShakeTimeToGo / 2);
        yield return new WaitForSeconds(attackShakeTimeToGo);

        List<FightCardData> newGunMuList = new List<FightCardData>();
        List<FightCardData> newGunShiList = new List<FightCardData>();

        List<int> fightColumns = new List<int>() { 0, 1, 2, 3, 4 };  //记录会攻击到的列
        int cutHpNum = (int)(attackUnit.damage * damageRate);
        for (int i = 0; i < fightColumns.Count; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                int cardIndex = fightColumns[i] + j * 5;
                if (fightCardDatas[cardIndex] != null && fightCardDatas[cardIndex].Hp > 0)
                {
                    int nowDamage = DefDamageProcessFun(attackUnit, fightCardDatas[cardIndex], cutHpNum);
                    if (fightCardDatas[cardIndex].cardType == 3)    //滚石和滚木，对陷阱造成2倍伤害
                    {
                        nowDamage = nowDamage * 2;
                    }
                    if (cardIndex != 17)
                    {
                        fightCardDatas[cardIndex].Hp.Add(-nowDamage);
                        TakeOneUnitDizzed(fightCardDatas[cardIndex], dizzedRate);
                    }
                    else
                    {
                        fightCardDatas[cardIndex].Hp.Add(-nowDamage);
                        if (fightCardDatas[cardIndex].Hp <= 0)
                        {
                            recordWinner = attackUnit.isPlayerCard ? 1 : -1;
                        }
                    }
                    AttackToEffectShow(fightCardDatas[cardIndex], false, Effect.Dropping);
                    ShowSpellTextObj(fightCardDatas[cardIndex].cardObj, DataTable.GetStringText(9), true, true);
                    AttackedAnimShow(fightCardDatas[cardIndex], nowDamage, false);

                    if (fightCardDatas[cardIndex].Hp <= 0 &&
                        fightCardDatas[cardIndex].cardType == 3 &&
                        (fightCardDatas[cardIndex].cardId == 9 || fightCardDatas[cardIndex].cardId == 10))
                    {
                        if (fightCardDatas[cardIndex].cardId == 9)
                        {
                            newGunShiList.Add(fightCardDatas[cardIndex]);
                        }
                        else
                        {
                            newGunMuList.Add(fightCardDatas[cardIndex]);
                        }
                    }
                    break;
                }
            }
        }

        if (newGunMuList.Count > 0 || newGunShiList.Count > 0)
        {
            yield return StartCoroutine(GunMuGunShiSkill(newGunMuList, newGunShiList));
        }

        yield return new WaitForSeconds(attackShakeTimeToGo);
    }

    //滚石反击
    IEnumerator GunShiTrapAttack(FightCardData attackUnit, float damageRate, int dizzedRate)
    {
        attackUnit.isActionDone = true;

        yield return new WaitForSeconds(attackShakeTimeToGo / 2);

        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.enemyFightCardsDatas : FightForManagerForStart.instance.playerFightCardsDatas;
        PlayAudioForSecondClip(94, 0);
        MoveToFightWay0(attackUnit, attackShakeTimeToGo / 2);
        yield return new WaitForSeconds(attackShakeTimeToGo);

        List<FightCardData> newGunMuList = new List<FightCardData>();
        List<FightCardData> newGunShiList = new List<FightCardData>();

        int startIndex = attackUnit.posIndex % 5;
        int cutHpNum = (int)(attackUnit.damage * damageRate);
        for (int i = 0; i < 4; i++)
        {
            int cardIndex = startIndex + i * 5;
            if (fightCardDatas[cardIndex] != null && fightCardDatas[cardIndex].Hp > 0)
            {
                int nowDamage = DefDamageProcessFun(attackUnit, fightCardDatas[cardIndex], cutHpNum);
                if (fightCardDatas[cardIndex].cardType == 3)    //滚石和滚木，对陷阱造成2倍伤害
                {
                    nowDamage = nowDamage * 2;
                }
                if (cardIndex != 17)
                {
                    fightCardDatas[cardIndex].Hp.Add(-nowDamage);
                    TakeOneUnitDizzed(fightCardDatas[cardIndex], dizzedRate);
                }
                else
                {
                    fightCardDatas[cardIndex].Hp.Add(-nowDamage);
                    if (fightCardDatas[cardIndex].Hp <= 0)
                    {
                        recordWinner = attackUnit.isPlayerCard ? 1 : -1;
                    }
                }
                AttackToEffectShow(fightCardDatas[cardIndex], false, Effect.Dropping);
                ShowSpellTextObj(fightCardDatas[cardIndex].cardObj, DataTable.GetStringText(10), true, true);
                AttackedAnimShow(fightCardDatas[cardIndex], nowDamage, false);

                if (fightCardDatas[cardIndex].Hp <= 0 &&
                        fightCardDatas[cardIndex].cardType == 3 &&
                        (fightCardDatas[cardIndex].cardId == 9 || fightCardDatas[cardIndex].cardId == 10))
                {
                    if (fightCardDatas[cardIndex].cardId == 9)
                    {
                        newGunShiList.Add(fightCardDatas[cardIndex]);
                    }
                    else
                    {
                        newGunMuList.Add(fightCardDatas[cardIndex]);
                    }
                }
            }
        }

        if (newGunMuList.Count > 0 || newGunShiList.Count > 0)
        {
            yield return StartCoroutine(GunMuGunShiSkill(newGunMuList, newGunShiList));
        }

        yield return new WaitForSeconds(attackShakeTimeToGo);
    }

    //箭楼远射技能(塔)
    public void JianLouYuanSheSkill(FightCardData attackUnit, int finalDamage)
    {
        int damage = (int)(DataTable.GetGameValue(17) / 100f * finalDamage);
        PlayAudioForSecondClip(20, 0);

        FightCardData[] fightCardDatas = isPlayerRound ? FightForManagerForStart.instance.enemyFightCardsDatas : FightForManagerForStart.instance.playerFightCardsDatas;
        List<int> canFightUnits = new List<int>();
        for (int i = 0; i < fightCardDatas.Length; i++)
        {
            if (fightCardDatas[i] != null && fightCardDatas[i].cardType == 0 && fightCardDatas[i].Hp > 0)
            {
                canFightUnits.Add(i);
            }
        }
        List<int> attackedIndexList = BackRandsList(canFightUnits, DataTable.GetGameValue(18));
        for (int i = 0; i < attackedIndexList.Count; i++)
        {
            FightCardData attackedUnit = fightCardDatas[attackedIndexList[i]];
            AttackToEffectShow(attackedUnit, false, Effect.Bow20A);

            int nowDamage = DefDamageProcessFun(attackUnit, attackedUnit, damage);
            attackedUnit.Hp.Add(-nowDamage);
            AttackedAnimShow(attackedUnit, nowDamage, false);
            if (attackedUnit.cardType == 522 && attackedUnit.Hp <= 0)
            {
                recordWinner = attackUnit.isPlayerCard ? 1 : -1;
            }
        }
    }

    #endregion

    ///////造成伤害时触发的特殊技能///////////

    private int FightDamageForSpecialSkill(int finalDamage, FightCardData attackUnit, FightCardData attackedUnit, bool isCanFightBack)
    {
        if (isCanFightBack)
        {
            if (attackUnit.CardState.Imprisoned <= 0) //攻击者没有禁锢状态
            {
                switch (DataTable.Hero[attackUnit.cardId].MilitaryUnitTableId)
                {
                    case 3:
                        PlayAudioForSecondClip(3, 0);
                        AttackToEffectShow(attackedUnit, false, Effect.FeiJia3A);
                        break;
                    case 4:
                        ShowSpellTextObj(attackUnit.cardObj, "4", false);
                        AttackToEffectShow(attackUnit, false, Effect.Shield4A);
                        if (attackUnit.CardState.Shield <= 0)
                        {
                            FightForManagerForStart.instance.CreateSateIcon(attackUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_withStand, true);
                        }
                        attackUnit.CardState.Shield++;
                        PlayAudioForSecondClip(4, 0);
                        break;
                    case 6:
                        AttackToEffectShow(attackedUnit, false, Effect.SuckBlood6A);
                        PlayAudioForSecondClip(6, 0);
                        break;
                    case 8:
                        XiangBingTrampleAttAck(attackedUnit, attackUnit);
                        break;
                    case 9:
                        AttackToEffectShow(attackedUnit, false, Effect.Cavalry9A);
                        PlayAudioForSecondClip(9, 0);
                        break;
                    case 60:
                        AttackToEffectShow(attackedUnit, false, Effect.Cavalry60A);
                        PlayAudioForSecondClip(9, 0);
                        break;
                    case 10:
                        finalDamage = SiShiSheMingAttack(finalDamage, attackUnit, attackedUnit);
                        break;
                    case 11:
                        finalDamage = TieQiWuWeiAttack(finalDamage, attackUnit, attackedUnit);
                        break;
                    case 12:
                        finalDamage = ShenWuZhanYiAttack(finalDamage, attackUnit, attackedUnit);
                        break;
                    case 59:
                        QiangBingChuanCiAttack(finalDamage, attackUnit, attackedUnit, 59);
                        break;
                    case 14:
                        QiangBingChuanCiAttack(finalDamage, attackUnit, attackedUnit, 14);
                        break;
                    case 15:
                        JianBingHengSaoAttack(finalDamage, attackUnit, attackedUnit);
                        break;
                    case 16:
                        AttackToEffectShow(attackedUnit, false, Effect.Cavalry16A);
                        PlayAudioForSecondClip(16, 0);
                        break;
                    case 17:
                        AttackToEffectShow(attackedUnit, false, Effect.Blade17A);
                        PlayAudioForSecondClip(17, 0);
                        break;
                    case 18:
                        finalDamage = FuBingTuLuAttack(finalDamage, attackedUnit, attackUnit);
                        break;
                    case 19:
                        AttackToEffectShow(attackedUnit, false, Effect.CrossBow19A);
                        PlayAudioForSecondClip(19, 0);
                        break;
                    case 51:
                        AttackToEffectShow(attackedUnit, false, Effect.CrossBow19A);
                        PlayAudioForSecondClip(19, 0);
                        break;
                    case 20:
                        GongBingYuanSheSkill(finalDamage, attackUnit, attackedUnit, 20);
                        break;
                    case 52:
                        GongBingYuanSheSkill(finalDamage, attackUnit, attackedUnit, 52);
                        break;
                    case 21:
                        finalDamage = ZhanChuanChongJiAttack(finalDamage, attackedUnit, attackUnit);
                        break;
                    case 22:
                        finalDamage = ZhanCheZhuangYaAttack(finalDamage, attackedUnit, attackUnit);
                        break;
                    case 23:
                        finalDamage = GongChengChePoCheng(finalDamage, attackUnit, attackedUnit);
                        break;
                    case 24:
                        TouShiCheSkill(finalDamage, attackUnit);
                        break;
                    case 25:
                        CiKePoJiaAttack(attackedUnit, attackUnit);
                        break;
                    case 26:
                        JunShiSkill(attackUnit, 26, finalDamage);
                        SpecilSkillNeedPuGongFun(attackedUnit);
                        break;
                    case 27:
                        JunShiSkill(attackUnit, 27, finalDamage);
                        SpecilSkillNeedPuGongFun(attackedUnit);
                        break;
                    case 28:
                        isNeedToAttack = false;
                        break;
                    case 29:
                        isNeedToAttack = false;
                        break;
                    case 30:
                        DuShiSkill(DataTable.GetGameValue(19), attackUnit, 30, finalDamage);
                        SpecilSkillNeedPuGongFun(attackedUnit);
                        break;
                    case 31:
                        DuShiSkill(DataTable.GetGameValue(20), attackUnit, 31, finalDamage);
                        SpecilSkillNeedPuGongFun(attackedUnit);
                        break;
                    case 32:
                        isNeedToAttack = false;
                        break;
                    case 33:
                        isNeedToAttack = false;
                        break;
                    case 34:
                        BianShiSkill(DataTable.GetGameValue(21), attackUnit, 34);
                        SpecilSkillNeedPuGongFun(attackedUnit);
                        break;
                    case 35:
                        BianShiSkill(DataTable.GetGameValue(22), attackUnit, 35);
                        SpecilSkillNeedPuGongFun(attackedUnit);
                        break;
                    case 36:
                        MouShiSkill(DataTable.GetGameValue(23), attackUnit, 36);
                        SpecilSkillNeedPuGongFun(attackedUnit);
                        break;
                    case 37:
                        MouShiSkill(DataTable.GetGameValue(24), attackUnit, 37);
                        SpecilSkillNeedPuGongFun(attackedUnit);
                        break;
                    case 38:
                        NeiZhengSkill(attackUnit);
                        SpecilSkillNeedPuGongFun(attackedUnit);
                        break;
                    case 39:
                        FuZuoBiHuSkill(finalDamage, attackUnit);
                        SpecilSkillNeedPuGongFun(attackedUnit);
                        break;
                    case 40:
                        QiXieXiuFu(attackUnit);
                        SpecilSkillNeedPuGongFun(attackedUnit);
                        break;
                    case 42:
                        YiShengSkill(DataTable.GetGameValue(25), attackUnit, 42);
                        SpecilSkillNeedPuGongFun(attackedUnit);
                        break;
                    case 43:
                        YiShengSkill(DataTable.GetGameValue(26), attackUnit, 43);
                        SpecilSkillNeedPuGongFun(attackedUnit);
                        break;
                    case 44:
                        ShuiBingXieJia(attackUnit, attackedUnit);
                        break;
                    case 45:
                        MeiRenJiNeng(attackUnit, 45);
                        SpecilSkillNeedPuGongFun(attackedUnit);
                        break;
                    case 46:
                        MeiRenJiNeng(attackUnit, 46);
                        SpecilSkillNeedPuGongFun(attackedUnit);
                        break;
                    case 47:
                        ShuiKeSkill(DataTable.GetGameValue(27), attackUnit, 47);
                        SpecilSkillNeedPuGongFun(attackedUnit);
                        break;
                    case 48:
                        ShuiKeSkill(DataTable.GetGameValue(28), attackUnit, 48);
                        SpecilSkillNeedPuGongFun(attackedUnit);
                        break;
                    case 49:
                        PlayAudioForSecondClip(49, 0);
                        AttackToEffectShow(attackedUnit, false, Effect.Crossbow49A);
                        break;
                    case 50:
                        PlayAudioForSecondClip(50, 0);
                        AttackToEffectShow(attackedUnit, false, Effect.Scribe50A);
                        break;
                    case 53:
                        YinShiSkill(DataTable.GetGameValue(29), attackUnit, 53, finalDamage);
                        SpecilSkillNeedPuGongFun(attackedUnit);
                        break;
                    case 54:
                        YinShiSkill(DataTable.GetGameValue(30), attackUnit, 54, finalDamage);
                        SpecilSkillNeedPuGongFun(attackedUnit);
                        break;
                    case 55:
                        HuoChuanSkill(finalDamage, attackUnit, attackedUnit);
                        break;
                    case 56:
                        ManZuSkill(attackUnit, attackedUnit);
                        break;
                    case 57:
                        PlayAudioForSecondClip(57, 0);
                        AttackToEffectShow(attackedUnit, false, Effect.TengJia57A);
                        break;
                    case 58:
                        finalDamage = TieQiSkill(finalDamage, attackUnit, attackedUnit);
                        break;
                    case 65:
                        finalDamage = HuangJinSkill(finalDamage, attackUnit, attackedUnit);
                        break;
                    default:
                        //isNeedToAttack = false;
                        PlayAudioForSecondClip(0, 0);
                        AttackToEffectShow(attackedUnit, true);
                        break;
                }
            }
            else
            {
                PlayAudioForSecondClip(0, 0);
                ShowSpellTextObj(attackUnit.cardObj, DataTable.GetStringText(11), true, true);
            }
        }

        if (attackedUnit.cardType == 0)
        {
            switch (DataTable.Hero[attackedUnit.cardId].MilitaryUnitTableId)
            {
                case 7:
                    if (isCanFightBack)
                    {
                        CiJiaFanShangAttack(finalDamage, attackUnit, attackedUnit);
                    }
                    break;
                default:
                    break;
            }
        }
        return finalDamage;
    }

    //特殊单位的技能行动0
    IEnumerator SpecialHeroSkill0(int finalDamage, FightCardData attackUnit, FightCardData attackedUnit, bool isCanFightBack)
    {
        if (isCanFightBack)
        {
            if (attackedUnit.CardState.Stunned <= 0 && attackedUnit.CardState.Imprisoned <= 0)
            {
                //禁卫
                if (DataTable.Hero[attackedUnit.cardId].MilitaryUnitTableId == 13 && attackUnit.combatType == 0)
                {
                    yield return StartCoroutine(JinWeiFanJiAttack(attackedUnit, attackUnit));
                }
            }
        }
    }

    //特殊单位的技能行动1
    IEnumerator SpecialHeroSkill1(float damageBonus, FightCardData attackUnit, FightCardData attackedUnit)
    {
        yield return StartCoroutine(GunMuGunShiSkill(gunMuCards, gunShiCards));

        if (attackUnit.CardState.Imprisoned > 0) //禁锢，不进行技能攻击
        {
            attackUnit.CardState.Imprisoned--;
            ShowSpellTextObj(attackUnit.cardObj, DataTable.GetStringText(11), true, true);
            if (attackUnit.CardState.Imprisoned <= 0)
            {
                attackUnit.CardState.Imprisoned = 0;
                FightForManagerForStart.instance.DestroySateIcon(attackUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_imprisoned, true);
            }
        }
        else
        {
            switch (DataTable.Hero[attackUnit.cardId].MilitaryUnitTableId)
            {
                case 9:
                    yield return StartCoroutine(XianFengYongWu(attackUnit, attackedUnit, 9));
                    break;
                case 60:
                    yield return StartCoroutine(XianFengYongWu(attackUnit, attackedUnit, 60));
                    break;
                case 16:
                    yield return StartCoroutine(QiBingChiCheng(attackUnit, attackedUnit));
                    break;
                case 17:
                    yield return StartCoroutine(DaoBingLianZhan(damageBonus, attackUnit, attackedUnit));
                    break;
                case 19:
                    yield return StartCoroutine(NuBingLianShe(attackUnit, attackedUnit, 19));
                    break;
                case 51:
                    yield return StartCoroutine(NuBingLianShe(attackUnit, attackedUnit, 51));
                    break;
                case 28:
                    yield return StartCoroutine(ShuShiLuoLei(DataTable.GetGameValue(31), attackUnit, 28));
                    break;
                case 29:
                    yield return StartCoroutine(ShuShiLuoLei(DataTable.GetGameValue(32), attackUnit, 29));
                    break;
                case 32:
                    yield return StartCoroutine(TongShuaiSkill(attackUnit, 32));
                    break;
                case 33:
                    yield return StartCoroutine(TongShuaiSkill(attackUnit, 33));
                    break;
                case 12:
                    yield return StartCoroutine(ShenWuZhanYi(damageBonus, attackUnit, attackedUnit));
                    break;
                default:
                    break;
            }
        }

        yield return StartCoroutine(GunMuGunShiSkill(gunMuCards, gunShiCards));
        //消除滚石滚木
        for (int i = 0; i < gunMuCards.Count; i++)
        {
            if (gunMuCards[i].Hp <= 0)
            {
                gunMuCards.Remove(gunMuCards[i]);
            }
        }
        for (int i = 0; i < gunShiCards.Count; i++)
        {
            if (gunShiCards[i].Hp <= 0)
            {
                gunShiCards.Remove(gunShiCards[i]);
            }
        }
    }

    #region 英雄特殊技能方法

    private int tongShuaiBurnRoundPy = -1;
    private int tongShuaiBurnRoundEm = -1;

    //统帅回合攻击目标
    int[][] GoalGfSetFireRound = new int[3][] {
        new int[1] { 7},
        new int[6] { 2, 5, 6,10,11,12},
        new int[11]{ 0, 1, 3, 4, 8, 9,13,14,15,16,17},
    };
    //统帅野火技能
    IEnumerator TongShuaiSkill(FightCardData attackUnit, int classType)
    {
        FightCardData[] fightCardDatas;
        List<GameObject> posListToSetBurn;

        int burnRoundIndex = 0; //记录烧到第几圈了
        if (attackUnit.isPlayerCard)
        {
            burnRoundIndex = tongShuaiBurnRoundPy;
            fightCardDatas = FightForManagerForStart.instance.enemyFightCardsDatas;
            posListToSetBurn = FightForManagerForStart.instance.enemyCardsPos;
        }
        else
        {
            burnRoundIndex = tongShuaiBurnRoundEm;
            fightCardDatas = FightForManagerForStart.instance.playerFightCardsDatas;
            posListToSetBurn = FightForManagerForStart.instance.playerCardsPos;
        }

        //尝试消除上一圈火焰,灼烧之前回合烧过的地方
        if (burnRoundIndex != -1)
        {
            for (int i = 0; i < GoalGfSetFireRound[burnRoundIndex].Length; i++)
            {
                if (fightCardDatas[GoalGfSetFireRound[burnRoundIndex][i]] != null && fightCardDatas[GoalGfSetFireRound[burnRoundIndex][i]].cardType == 0 && fightCardDatas[GoalGfSetFireRound[burnRoundIndex][i]].Hp > 0)
                {
                    TakeToBurn(fightCardDatas[GoalGfSetFireRound[burnRoundIndex][i]], DataTable.GetGameValue(33));
                }
                Transform obj = posListToSetBurn[GoalGfSetFireRound[burnRoundIndex][i]].transform.Find(StringNameStatic.StateIconPath_burned);
                if (obj != null)
                    Destroy(obj.gameObject);
            }
        }

        burnRoundIndex++;
        if (burnRoundIndex == 3)
            burnRoundIndex = 0;
        if (attackUnit.isPlayerCard)
        {
            tongShuaiBurnRoundPy = burnRoundIndex;
        }
        else
        {
            tongShuaiBurnRoundEm = burnRoundIndex;
        }

        ShowSpellTextObj(attackUnit.cardObj, classType.ToString(), false);
        PlayAudioForSecondClip(32, 0);

        int[] targets = GoalGfSetFireRound[burnRoundIndex];

        string effectStr = classType + "A";

        int damage = HeroCardMakeSomeDamages(true, attackUnit);
        damage = (int)(damage * (1 - burnRoundIndex * DataTable.GetGameValue(34) / 100f));  //伤害递减

        for (int i = 0; i < targets.Length; i++)
        {
            EffectsPoolingControl.instance.GetEffectToFight1(effectStr, 1f, posListToSetBurn[targets[i]].transform);

            GameObject stateDinObj = Instantiate(Resources.Load("Prefabs/stateDin/" + StringNameStatic.StateIconPath_burned, typeof(GameObject)) as GameObject, posListToSetBurn[targets[i]].transform);
            stateDinObj.name = StringNameStatic.StateIconPath_burned;

            if (fightCardDatas[targets[i]] != null && fightCardDatas[targets[i]].Hp > 0)
            {
                int nowDamage = DefDamageProcessFun(attackUnit, fightCardDatas[targets[i]], damage);
                fightCardDatas[targets[i]].Hp.Add(-nowDamage);
                AttackedAnimShow(fightCardDatas[targets[i]], nowDamage, false);
                if (fightCardDatas[targets[i]].cardType == 522)
                {
                    if (fightCardDatas[targets[i]].Hp <= 0)
                    {
                        recordWinner = fightCardDatas[targets[i]].isPlayerCard ? -1 : 1;
                    }
                }
                else
                {
                    if (fightCardDatas[targets[i]].cardType == 0)
                    {
                        TakeToBurn(fightCardDatas[targets[i]], classType == 32 ? DataTable.GetGameValue(35) : DataTable.GetGameValue(36));
                    }
                }
            }
        }
        yield return new WaitForSeconds(attackShakeTimeToGo);
    }

    [SerializeField]
    GameObject thunderCloudObj;

    //术士落雷技能
    IEnumerator ShuShiLuoLei(int fightNums, FightCardData attackUnit, int classType)
    {
        ShowSpellTextObj(attackUnit.cardObj, classType.ToString(), false);
        PlayAudioForSecondClip(29, 0);

        thunderCloudObj.SetActive(true);
        thunderCloudObj.GetComponent<Image>().DOColor(new Color(0, 0, 0, 175f / 255f), 1f);
        yield return new WaitForSeconds(yuanChengShakeTimeToGo);

        for (int i = 0; i < fightNums; i++)
        {
            PlayAudioForSecondClip(28, 0);

            yield return new WaitForSeconds(attackShakeTimeToBack);
            bool isGameOver = ShuShiTakeThunder(attackUnit, classType);
            yield return new WaitForSeconds(attackShakeTimeToGo);
            thunderCloudObj.transform.GetChild(0).gameObject.SetActive(false);

            if (isGameOver)
            {
                break;
            }
            if (i < fightNums - 1)
            {
                float waitTime = BeforeFightDoThingFun(attackUnit);
                yield return new WaitForSeconds(waitTime);
            }

            yield return StartCoroutine(GunMuGunShiSkill(gunMuCards, gunShiCards));
        }
        thunderCloudObj.GetComponent<Image>().DOColor(new Color(0, 0, 0, 0), 0.5f);
        thunderCloudObj.SetActive(false);
    }
    //召唤落雷
    private bool ShuShiTakeThunder(FightCardData attackUnit, int classType)
    {
        string effectStr = classType + "A";
        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.enemyFightCardsDatas : FightForManagerForStart.instance.playerFightCardsDatas;
        List<GameObject> posListToThunder = attackUnit.isPlayerCard ? FightForManagerForStart.instance.enemyCardsPos : FightForManagerForStart.instance.playerCardsPos;
        List<int> canFightUnits = new List<int>();
        for (int i = 0; i < fightCardDatas.Length; i++)
        {
            canFightUnits.Add(i);
        }
        int damage = (int)(HeroCardMakeSomeDamages(true, attackUnit) * DataTable.GetGameValue(37) / 100f);
        List<int> attackedIndexList = BackRandsList(canFightUnits, classType == 28 ? Random.Range(1, DataTable.GetGameValue(38)) : Random.Range(1, DataTable.GetGameValue(39)));

        thunderCloudObj.transform.GetChild(0).gameObject.SetActive(true);

        for (int i = 0; i < attackedIndexList.Count; i++)
        {
            EffectsPoolingControl.instance.GetEffectToFight1(effectStr, 1f, posListToThunder[attackedIndexList[i]].transform);
            if (fightCardDatas[attackedIndexList[i]] != null && fightCardDatas[attackedIndexList[i]].Hp > 0)
            {
                int nowDamage = DefDamageProcessFun(attackUnit, fightCardDatas[attackedIndexList[i]], damage);
                fightCardDatas[attackedIndexList[i]].Hp.Add(-nowDamage);
                AttackedAnimShow(fightCardDatas[attackedIndexList[i]], nowDamage, false);
                if (fightCardDatas[attackedIndexList[i]].cardType == 522)
                {
                    if (fightCardDatas[attackedIndexList[i]].Hp <= 0)
                    {
                        recordWinner = fightCardDatas[attackedIndexList[i]].isPlayerCard ? -1 : 1;
                        return true;
                    }
                }
                else
                {
                    TakeOneUnitDizzed(fightCardDatas[attackedIndexList[i]], DataTable.GetGameValue(40));
                }
            }
        }
        return false;
    }

    //刀兵连斩技能
    IEnumerator DaoBingLianZhan(float damageBonus, FightCardData attackUnit, FightCardData attackedUnit)
    {
        if (attackUnit.Hp <= 0 || attackUnit.CardState.Stunned > 0 || attackUnit.CardState.Imprisoned > 0)
        {

        }
        else
        {
            if (attackedUnit.Hp <= 0 && attackedUnit.cardType != 522)
            {
                //Debug.Log("-----刀兵连斩");
                float waitTime = BeforeFightDoThingFun(attackUnit);
                yield return new WaitForSeconds(waitTime);
                ShowSpellTextObj(attackUnit.cardObj, "17", false);

                targetIndex = FindOpponentIndex(attackUnit);  //锁定目标卡牌
                MoveToFightWay1(attackUnit);
                yield return new WaitForSeconds(attackShakeTimeToGo);

                FightCardData nextAttackedUnit = attackUnit.isPlayerCard ? FightForManagerForStart.instance.enemyFightCardsDatas[targetIndex] : FightForManagerForStart.instance.playerFightCardsDatas[targetIndex];
                AttackToEffectShow(nextAttackedUnit, false, Effect.Blade17A);
                PlayAudioForSecondClip(17, 0);

                yield return StartCoroutine(PuTongGongji(damageBonus + DataTable.GetGameValue(41) / 100f, attackUnit, nextAttackedUnit, true));
            }
        }
    }

    //神武战意技能
    IEnumerator ShenWuZhanYi(float damageBonus, FightCardData attackUnit, FightCardData attackedUnit)
    {
        if (attackUnit.Hp <= 0 || attackUnit.CardState.Stunned > 0 || attackUnit.CardState.Imprisoned > 0)
        {

        }
        else
        {
            if (attackedUnit.Hp > 0 && attackedUnit.cardType == 0)
            {
                if (attackedUnit.attackedBehavior == 2 || attackedUnit.attackedBehavior == 3)
                {
                    float waitTime = BeforeFightDoThingFun(attackUnit);
                    yield return new WaitForSeconds(waitTime);
                    ShowSpellTextObj(attackUnit.cardObj, "12", false);
                    MoveToFightWay1(attackUnit);
                    yield return new WaitForSeconds(attackShakeTimeToGo);
                    AttackToEffectShow(attackedUnit, false, Effect.Stimulate12A);
                    PlayAudioForSecondClip(12, 0);

                    float propAttack = 1 + DataTable.GetGameValue(97) / 100f * attackUnit.CardState.Stimulate;

                    yield return StartCoroutine(PuTongGongji(propAttack, attackUnit, attackedUnit, true));
                }
            }
        }
    }


    //先锋勇武技能
    IEnumerator XianFengYongWu(FightCardData attackUnit, FightCardData attackedUnit, int classType)
    {
        if (attackUnit.Hp <= 0 || attackUnit.CardState.Stunned > 0 || attackUnit.CardState.Imprisoned > 0)
        {

        }
        else
        {
            int propNums = 0;   //特殊技能触发概率
            int attackNums = 0; //攻击次数
            if (classType == 9)
            {
                propNums = DataTable.GetGameValue(42);
                attackNums = DataTable.GetGameValue(43);
            }
            else
            {
                propNums = DataTable.GetGameValue(44);
                attackNums = DataTable.GetGameValue(45);
            }

            if (TakeSpecialAttack(propNums))
            {
                for (int i = 0; i < attackNums; i++)
                {
                    float waitTime = BeforeFightDoThingFun(attackUnit);
                    yield return new WaitForSeconds(waitTime);
                    ShowSpellTextObj(attackUnit.cardObj, "9", false);
                    MoveToFightWay1(attackUnit);
                    yield return new WaitForSeconds(attackShakeTimeToGo);
                    AttackToEffectShow(attackedUnit, false, classType + "A");
                    PlayAudioForSecondClip(9, 0);
                    //TakeToCowardly(attackedUnit, LoadJsonFile.GetGameValue(46));
                    yield return StartCoroutine(PuTongGongji(1f, attackUnit, attackedUnit, false));
                }
            }
        }
    }

    //骑兵驰骋技能
    IEnumerator QiBingChiCheng(FightCardData attackUnit, FightCardData attackedUnit)
    {
        if (attackUnit.Hp <= 0 || attackUnit.CardState.Stunned > 0 || attackUnit.CardState.Imprisoned > 0)
        {

        }
        else
        {
            if (attackedUnit.Hp > 0 && (indexAttackType != 0 || TakeSpecialAttack(DataTable.GetGameValue(47))))
            {
                //Debug.Log("-----骑兵驰骋");
                float waitTime = BeforeFightDoThingFun(attackUnit);
                yield return new WaitForSeconds(waitTime);
                ShowSpellTextObj(attackUnit.cardObj, "16", false);
                MoveToFightWay1(attackUnit);
                yield return new WaitForSeconds(attackShakeTimeToGo);
                AttackToEffectShow(attackedUnit, false, Effect.Cavalry16A);
                PlayAudioForSecondClip(16, 0);

                yield return StartCoroutine(PuTongGongji(1f, attackUnit, attackedUnit, true));
            }
        }
    }

    //弩兵连射技能
    IEnumerator NuBingLianShe(FightCardData attackUnit, FightCardData attackedUnit, int classIndex)
    {
        if (attackedUnit.Hp > 0 && TakeSpecialAttack(classIndex == 51 ? DataTable.GetGameValue(48) : DataTable.GetGameValue(49)))
        {
            //Debug.Log("-----弩兵连射");
            float waitTime = BeforeFightDoThingFun(attackUnit);
            yield return new WaitForSeconds(waitTime);
            MoveToFightWay0(attackUnit, yuanChengShakeTimeToGo);
            yield return new WaitForSeconds(yuanChengShakeTimeToGo / 2);

            ShowSpellTextObj(attackUnit.cardObj, "19", false);
            AttackToEffectShow(attackedUnit, false, Effect.CrossBow19A);
            PlayAudioForSecondClip(19, 0);
            //连射
            yield return StartCoroutine(PuTongGongji(1, attackUnit, attackedUnit, false));

            if (classIndex == 51)//强弩攻击第三次
            {
                yield return new WaitForSeconds(waitTime);
                MoveToFightWay0(attackUnit, yuanChengShakeTimeToGo);
                yield return new WaitForSeconds(yuanChengShakeTimeToGo / 2);
                AttackToEffectShow(attackedUnit, false, Effect.CrossBow19A);
                PlayAudioForSecondClip(19, 0);
                //连射
                yield return StartCoroutine(PuTongGongji(1, attackUnit, attackedUnit, false));
            }
        }
    }

    //禁卫反击技能
    IEnumerator JinWeiFanJiAttack(FightCardData attackUnit, FightCardData attackedUnit)
    {
        if (attackUnit.Hp > 0)
        {
            yield return new WaitForSeconds(0.3f);
            ShowSpellTextObj(attackUnit.cardObj, "13", false);
            yield return new WaitForSeconds(0.2f);
            PlayAudioForSecondClip(13, 0);
            AttackToEffectShow(attackedUnit, false, Effect.Guard13A);

            int damage = DefDamageProcessFun(attackUnit, attackedUnit, attackUnit.damage);
            attackedUnit.Hp.Add(-damage);
            AttackedAnimShow(attackedUnit, damage, false);
        }
    }

    /// <summary>
    /// 黄巾群起技能
    /// </summary>
    /// <param name="finalDamage"></param>
    /// <param name="attackUnit"></param>
    /// <param name="attackedUnit"></param>
    /// <returns></returns>
    private int HuangJinSkill(int finalDamage, FightCardData attackUnit, FightCardData attackedUnit)
    {
        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.playerFightCardsDatas : FightForManagerForStart.instance.enemyFightCardsDatas;

        PlayAudioForSecondClip(65, 0);
        ShowSpellTextObj(attackUnit.cardObj, "65", false);
        AttackToEffectShow(attackedUnit, false, Effect.YellowBand65A);

        int sameTypeHeroNums = 0;
        for (int i = 0; i < fightCardDatas.Length; i++)
        {
            if (fightCardDatas[i] != null && fightCardDatas[i].Hp > 0 && fightCardDatas[i].cardType == 0 && DataTable.Hero[fightCardDatas[i].cardId].MilitaryUnitTableId == 65)
            {
                AttackToEffectShow(fightCardDatas[i], false, Effect.YellowBand65B);
                sameTypeHeroNums++;
            }
        }
        finalDamage = (int)(sameTypeHeroNums * finalDamage * DataTable.GetGameValue(146) / 100f);
        return finalDamage;
    }

    /// <summary>
    /// 铁骑分摊伤害
    /// </summary>
    /// <param name="finalDamage"></param>
    /// <param name="attackedUnit"></param>
    /// <returns></returns>
    private int TieQiFenTan(int finalDamage, FightCardData attackedUnit)
    {
        if (finalDamage <= 0)
            return finalDamage;

        int damage = finalDamage;
        if (attackedUnit.cardType == 0)
        {
            switch (DataTable.Hero[attackedUnit.cardId].MilitaryUnitTableId)
            {
                case 58:
                    List<FightCardData> tieQiCardsList = attackedUnit.isPlayerCard ? tieQiCardsPy : tieQiCardsEm;
                    int survivalUnit = 0;
                    for (int i = 0; i < tieQiCardsList.Count; i++)
                    {
                        if (tieQiCardsList[i].Hp > 0)
                        {
                            survivalUnit++;
                        }
                    }

                    if (tieQiCardsList.Count > 1)
                    {
                        damage = (int)((float)damage / (survivalUnit > 0 ? survivalUnit : 1));
                        for (int i = 0; i < tieQiCardsList.Count; i++)
                        {
                            if (tieQiCardsList[i].Hp > 0)
                            {
                                ShowSpellTextObj(tieQiCardsList[i].cardObj, "58_0", false);
                                if (tieQiCardsList[i] != attackedUnit)
                                {
                                    if (!(tieQiCardsList[i].CardState.Invincible > 0))
                                    {
                                        int backDamage = AddOrCutShieldValue(damage, tieQiCardsList[i], false);
                                        tieQiCardsList[i].Hp.Add(-backDamage);
                                        AttackedAnimShow(tieQiCardsList[i], backDamage, false);
                                    }
                                }
                            }
                        }
                    }

                    break;
                default:
                    break;
            }
        }
        return damage;
    }

    private List<FightCardData> tieQiCardsPy = new List<FightCardData>();   //玩家铁骑索引
    private List<FightCardData> tieQiCardsEm = new List<FightCardData>();   //敌人铁骑索引
    //铁骑连环技能
    private int TieQiSkill(int finalDamage, FightCardData attackUnit, FightCardData attackedUnit)
    {
        List<FightCardData> tieQiCardsList = attackUnit.isPlayerCard ? tieQiCardsPy : tieQiCardsEm;

        PlayAudioForSecondClip(58, 0);
        ShowSpellTextObj(attackUnit.cardObj, "58", false);
        AttackToEffectShow(attackedUnit, false, Effect.HeavyCavalry58A);

        float damageBonus = 0;
        if (tieQiCardsList.Count > 1)
        {
            damageBonus = DataTable.GetGameValue(50) / 100f * tieQiCardsList.Count;
        }
        finalDamage = (int)(finalDamage * (1 + damageBonus));
        return finalDamage;
    }
    //刷新铁骑连环状态图标
    public void UpdateTieQiStateIconShow(FightCardData tieQiCard, bool isAdd)
    {
        List<FightCardData> tieQiCards = tieQiCard.isPlayerCard ? tieQiCardsPy : tieQiCardsEm;

        if (isAdd)
        {
            tieQiCards.Add(tieQiCard);
        }
        else
        {
            FightForManagerForStart.instance.DestroySateIcon(tieQiCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_lianHuan, true);
            tieQiCards.Remove(tieQiCard);
        }

        if (tieQiCards.Count > 1)
        {
            if (tieQiCards.Count == 2)
            {
                FightForManagerForStart.instance.CreateSateIcon(tieQiCards[0].cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_lianHuan, true);
            }
            FightForManagerForStart.instance.CreateSateIcon(tieQiCards[tieQiCards.Count - 1].cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_lianHuan, true);
        }
        else
        {
            for (int i = 0; i < tieQiCards.Count; i++)
            {
                FightForManagerForStart.instance.DestroySateIcon(tieQiCards[i].cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_lianHuan, true);
            }
        }
    }

    /// <summary>
    /// 清空敌方铁骑列表
    /// </summary>
    public void ClearEmTieQiCardList()
    {
        tieQiCardsEm.Clear();   //敌方铁骑列表清空
    }

    //蛮族剧毒技能
    private void ManZuSkill(FightCardData attackUnit, FightCardData attackedUnit)
    {
        PlayAudioForSecondClip(56, 0);
        ShowSpellTextObj(attackUnit.cardObj, "56", false);
        AttackToEffectShow(attackedUnit, false, Effect.Barbarians56A);
        if (attackedUnit.cardType == 0 && attackedUnit.Hp > 0)
        {
            if (TakeSpecialAttack(DataTable.GetGameValue(51)))
            {
                if (attackedUnit.CardState.Poison <= 0)
                {
                    FightForManagerForStart.instance.CreateSateIcon(attackedUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_poisoned, true);
                }
                ShowSpellTextObj(attackedUnit.cardObj, DataTable.GetStringText(12), true, true);
                attackedUnit.CardState.Poison++;
            }
        }
    }

    //火船引燃技能
    private void HuoChuanSkill(int finalDamage, FightCardData attackUnit, FightCardData attackedUnit)
    {
        int takeBurnPro = DataTable.GetGameValue(52);   //附加灼烧概率

        //血量小于50%，发起自杀攻击
        if (attackUnit.Hp.Rate() <= DataTable.GetGameValue(54) / 100f)
        {
            PlayAudioForSecondClip(84, 0);
            ShowSpellTextObj(attackUnit.cardObj, "55_0", false);
            AttackToEffectShow(attackedUnit, false, Effect.FireShip55A0);

            takeBurnPro = DataTable.GetGameValue(53);

            attackUnit.Hp.Set(0);
            UpdateUnitHpShow(attackUnit);

            finalDamage = (int)(finalDamage * DataTable.GetGameValue(55) / 100f);

            FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.enemyFightCardsDatas : FightForManagerForStart.instance.playerFightCardsDatas;
            for (int i = 0; i < FightForManagerForStart.instance.CardNearbyAdditionForeach[targetIndex].Length; i++)
            {
                FightCardData attackedUnits = fightCardDatas[FightForManagerForStart.instance.CardNearbyAdditionForeach[targetIndex][i]];
                if (attackedUnits != null && attackedUnits.Hp > 0)
                {
                    AttackToEffectShow(attackedUnits, false, Effect.FireShip55A);
                    int backDamage = DefDamageProcessFun(attackUnit, attackedUnits, finalDamage);
                    attackedUnits.Hp.Add(-backDamage);
                    AttackedAnimShow(attackedUnits, backDamage, false);
                    if (attackedUnits.cardType == 522)
                    {
                        if (attackedUnits.Hp <= 0)
                        {
                            recordWinner = attackedUnits.isPlayerCard ? -1 : 1;
                        }
                    }
                    if (attackedUnits.cardType == 0 && attackedUnits.Hp > 0)
                    {
                        TakeToBurn(attackedUnits, takeBurnPro);
                    }
                }
            }

        }
        else
        {
            PlayAudioForSecondClip(55, 0);
            ShowSpellTextObj(attackUnit.cardObj, "55", false);
            AttackToEffectShow(attackedUnit, false, Effect.FireShip55A);
        }
        TakeToBurn(attackedUnit, takeBurnPro);
    }

    //军师技能
    private void JunShiSkill(FightCardData attackUnit, int classType, int finalDamage)
    {
        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.enemyFightCardsDatas : FightForManagerForStart.instance.playerFightCardsDatas;
        List<int> canFightUnits = new List<int>();
        for (int i = 0; i < fightCardDatas.Length; i++)
        {
            if (fightCardDatas[i] != null && fightCardDatas[i].cardType == 0 && fightCardDatas[i].Hp > 0)
            {
                canFightUnits.Add(i);
            }
        }
        //按血量从少到多排序
        canFightUnits.Sort((a, b) => fightCardDatas[a].Hp.Rate().CompareTo(fightCardDatas[b].Hp.Rate()));
        int fightNums = 0;
        int killPos = 0;
        int zhanShaXian = attackUnit.damage;    //斩杀线
        switch (indexAttackType)
        {
            case 0:
                zhanShaXian = (int)(zhanShaXian * DataTable.GetGameValue(56) / 100f);
                break;
            case 1: //会心
                zhanShaXian = (int)(zhanShaXian * DataTable.GetGameValue(58) / 100f);
                break;
            case 2: //暴击
                zhanShaXian = (int)(zhanShaXian * DataTable.GetGameValue(57) / 100f);
                break;
            default:
                break;
        }

        if (classType == 26)
        {
            fightNums = DataTable.GetGameValue(59);
            killPos = DataTable.GetGameValue(60);
        }
        else
        {
            fightNums = DataTable.GetGameValue(61);
            killPos = DataTable.GetGameValue(62);
        }
        fightNums = canFightUnits.Count > fightNums ? fightNums : canFightUnits.Count;

        if (fightNums > 0)
        {
            isNeedToAttack = false;
            ShowSpellTextObj(attackUnit.cardObj, classType.ToString(), false);
            string effectStr = classType + "A";
            PlayAudioForSecondClip(classType, 0);

            finalDamage = (int)(DataTable.GetGameValue(63) / 100f * finalDamage);

            for (int i = 0; i < fightNums; i++)
            {
                int nowDamage = 0;
                //造成伤害
                FightCardData attackedUnit = fightCardDatas[canFightUnits[i]];
                if (attackedUnit.Hp < zhanShaXian && TakeSpecialAttack(killPos))
                {
                    ShowSpellTextObj(attackedUnit.cardObj, DataTable.GetStringText(13), true, true);

                    nowDamage = attackedUnit.CardState.ExtendedHp + attackedUnit.Hp;
                    attackedUnit.Hp.Set(0);
                }
                else
                {
                    nowDamage = DefDamageProcessFun(attackUnit, attackedUnit, finalDamage);
                    attackedUnit.Hp.Add(-nowDamage);
                }
                AttackedAnimShow(attackedUnit, nowDamage, false);
                AttackToEffectShow(attackedUnit, false, effectStr);
            }
        }
    }

    //水兵卸甲技能
    private void ShuiBingXieJia(FightCardData attackUnit, FightCardData attackedUnit)
    {
        AttackToEffectShow(attackedUnit, false, Effect.FemaleRider44A);
        PlayAudioForSecondClip(44, 0);
        if (attackedUnit.cardType == 0)
        {
            //Debug.Log("-----水兵卸甲技能");
            ShowSpellTextObj(attackUnit.cardObj, "44", false);
            TakeToRemoveArmor(attackedUnit, DataTable.GetGameValue(64));
        }
    }

    //器械修复技能
    private void QiXieXiuFu(FightCardData attackUnit)
    {
        int fightNums = DataTable.GetGameValue(132);
        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.playerFightCardsDatas : FightForManagerForStart.instance.enemyFightCardsDatas;
        List<int> canHuiFuUnits = new List<int>();
        for (int i = 0; i < fightCardDatas.Length; i++)
        {
            if (fightCardDatas[i] != null && fightCardDatas[i].cardType != 0 && fightCardDatas[i].Hp > 0 && fightCardDatas[i].Hp != fightCardDatas[i].Hp.Max)
            {
                canHuiFuUnits.Add(i);
            }
        }
        if (canHuiFuUnits.Count > 0)
        {
            isNeedToAttack = false;
            if (fightNums > canHuiFuUnits.Count)
            {
                fightNums = canHuiFuUnits.Count;
            }
            canHuiFuUnits.Sort((int a, int b) =>
            {
                return fightCardDatas[a].Hp.CompareTo(fightCardDatas[b].Hp);
            });

            ShowSpellTextObj(attackUnit.cardObj, "40", false);

            int addtionNums = (int)(attackUnit.damage * (DataTable.GetGameValue(65) / 100f) / fightNums);
            for (int i = 0; i < fightNums; i++)
            {
                AttackToEffectShow(fightCardDatas[canHuiFuUnits[i]], false, Effect.Mechanical40A);
                ShowSpellTextObj(fightCardDatas[canHuiFuUnits[i]].cardObj, DataTable.GetStringText(15), true, false);
                fightCardDatas[canHuiFuUnits[i]].Hp.Add(+addtionNums);
                AttackedAnimShow(fightCardDatas[canHuiFuUnits[i]], addtionNums, true);
            }
        }
    }

    //辩士技能
    private void BianShiSkill(int fightNums, FightCardData attackUnit, int classType)
    {
        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.enemyFightCardsDatas : FightForManagerForStart.instance.playerFightCardsDatas;
        List<int> canFightUnits = new List<int>();
        for (int i = 0; i < fightCardDatas.Length; i++)
        {
            if (fightCardDatas[i] != null && fightCardDatas[i].Hp > 0 && fightCardDatas[i].cardType == 0)
            {
                canFightUnits.Add(i);
            }
        }
        if (canFightUnits.Count > 0)
        {
            isNeedToAttack = false;
            List<int> attackedIndexList = BackRandsList(canFightUnits, fightNums);
            ShowSpellTextObj(attackUnit.cardObj, classType.ToString(), false);
            string effectStr = "";
            if (classType == 34)  //辩士
            {
                effectStr = Effect.Debate34A;
                PlayAudioForSecondClip(34, 0);
            }
            else
            {//大辩士
                effectStr = Effect.Controversy35A;
                PlayAudioForSecondClip(35, 0);
            }
            for (int i = 0; i < attackedIndexList.Count; i++)
            {
                AttackToEffectShow(fightCardDatas[attackedIndexList[i]], false, effectStr);
                TakeToImprisoned(fightCardDatas[attackedIndexList[i]],
                    indexAttackType == 1 ?
                    DataTable.GetGameValue(66) :
                    (DataTable.GetGameValue(68) * attackUnit.cardGrade + DataTable.GetGameValue(67)));
            }
        }
    }

    //说客技能
    private void ShuiKeSkill(int fightNums, FightCardData attackUnit, int classType)
    {
        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.enemyFightCardsDatas : FightForManagerForStart.instance.playerFightCardsDatas;
        List<int> canFightUnits = new List<int>();
        for (int i = 0; i < fightCardDatas.Length; i++)
        {
            if (fightCardDatas[i] != null && fightCardDatas[i].Hp > 0 && fightCardDatas[i].cardType == 0)
            {
                canFightUnits.Add(i);
            }
        }
        if (canFightUnits.Count > 0)
        {
            isNeedToAttack = false;
            List<int> attackedIndexList = BackRandsList(canFightUnits, fightNums);
            ShowSpellTextObj(attackUnit.cardObj, classType.ToString(), false);
            string effectStr = "";
            if (classType == 47)  //说客
            {
                effectStr = Effect.Persuade47A;
                PlayAudioForSecondClip(47, 0);
            }
            else
            {//大说客
                effectStr = Effect.Convince48A;
                PlayAudioForSecondClip(48, 0);
            }
            for (int i = 0; i < attackedIndexList.Count; i++)
            {
                AttackToEffectShow(fightCardDatas[attackedIndexList[i]], false, effectStr);
                TakeToCowardly(fightCardDatas[attackedIndexList[i]],
                    indexAttackType == 1 ?
                    DataTable.GetGameValue(69) :
                    (DataTable.GetGameValue(71) * attackUnit.cardGrade + DataTable.GetGameValue(70)));
            }
        }
    }

    //投石车精准技能
    private void TouShiCheSkill(int finalDamage, FightCardData attackUnit)
    {
        isNeedToAttack = false;
        ShowSpellTextObj(attackUnit.cardObj, "24", false);
        PlayAudioForSecondClip(24, 0);
        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.enemyFightCardsDatas : FightForManagerForStart.instance.playerFightCardsDatas;
        List<int> canFightUnits = new List<int>();
        if (fightCardDatas[12] != null && fightCardDatas[12].Hp > 0)
        {
            canFightUnits.Add(12);
        }
        if (fightCardDatas[15] != null && fightCardDatas[15].Hp > 0)
        {
            canFightUnits.Add(15);
        }
        if (fightCardDatas[16] != null && fightCardDatas[16].Hp > 0)
        {
            canFightUnits.Add(16);
        }
        if (fightCardDatas[17] != null && fightCardDatas[17].Hp > 0)
        {
            canFightUnits.Add(17);
        }
        int randTarget = canFightUnits[Random.Range(0, canFightUnits.Count)];
        FightCardData attackedUnit = fightCardDatas[randTarget];
        if (attackedUnit != null && (!(attackedUnit.CardState.Invincible > 0 || OffsetWithStand(attackedUnit))))
        {
            AttackToEffectShow(attackedUnit, false, Effect.ThrowRocks24A);
            if (attackedUnit.cardType == 522)   //如果目标是老巢，造成1.5倍伤害
            {
                finalDamage = (int)(finalDamage * DataTable.GetGameValue(72) / 100f);
                finalDamage = DefDamageProcessFun(attackUnit, attackedUnit, finalDamage);
                attackedUnit.Hp.Add(-finalDamage);
                if (attackedUnit.Hp <= 0)
                {
                    recordWinner = attackedUnit.isPlayerCard ? -1 : 1;
                }
            }
            else
            {
                finalDamage = DefDamageProcessFun(attackUnit, attackedUnit, finalDamage);
                attackedUnit.Hp.Add(-finalDamage);
            }
            AttackedAnimShow(attackedUnit, finalDamage, false);
        }
    }

    //攻城车破城技能
    private int GongChengChePoCheng(int damage, FightCardData attackUnit, FightCardData attackedUnit)
    {
        if (attackedUnit.cardType != 0)
        {
            //Debug.Log("----攻城车破城");
            ShowSpellTextObj(attackUnit.cardObj, "23", false);
            AttackToEffectShow(attackedUnit, false, Effect.SiegeMachine23A);
            PlayAudioForSecondClip(23, 0);
            return (int)(damage * DataTable.GetGameValue(73) / 100f);
        }
        else
        {
            AttackToEffectShow(attackedUnit, true);
            return (int)(damage * DataTable.GetGameValue(74) / 100f);
        }
    }

    //隐士技能
    private void YinShiSkill(int fightNums, FightCardData attackUnit, int classType, int finalDamage)
    {
        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.enemyFightCardsDatas : FightForManagerForStart.instance.playerFightCardsDatas;
        List<int> canFightUnits = new List<int>();
        for (int i = 0; i < fightCardDatas.Length; i++)
        {
            if (fightCardDatas[i] != null && fightCardDatas[i].Hp > 0 && fightCardDatas[i].cardType == 0)
            {
                canFightUnits.Add(i);
            }
        }
        if (canFightUnits.Count > 0)
        {
            isNeedToAttack = false;
            ShowSpellTextObj(attackUnit.cardObj, classType.ToString(), false);
            string effectStr = classType + "A";
            PlayAudioForSecondClip(classType, 0);

            finalDamage = (int)(DataTable.GetGameValue(75) / 100f * finalDamage);
            int attackedNums = 0;
            for (int i = 0; i < canFightUnits.Count; i++)
            {
                FightCardData attackedUnit = fightCardDatas[canFightUnits[i]];
                AttackToEffectShow(attackedUnit, false, effectStr);
                //造成伤害
                int nowDamage = DefDamageProcessFun(attackUnit, attackedUnit, finalDamage);
                attackedUnit.Hp.Add(-nowDamage);
                AttackedAnimShow(attackedUnit, nowDamage, false);
                //击退
                int nextPos = attackedUnit.posIndex + 5;
                if (nextPos <= 19 && fightCardDatas[nextPos] == null)
                {
                    StartCoroutine(TakeCardPosBack(fightCardDatas, attackedUnit, nextPos, 0.2f, isPlayerRound));
                }

                attackedNums++;
                if (attackedNums >= fightNums)
                    break;
            }
        }
    }

    //弓兵远射技能
    private void GongBingYuanSheSkill(int finalDamage, FightCardData attackUnit, FightCardData attackedUnitForIndex, int classIndex)
    {
        //Debug.Log("---弓兵远射");
        ShowSpellTextObj(attackUnit.cardObj, "20", false);
        int damage = finalDamage;
        PlayAudioForSecondClip(20, 0);

        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.enemyFightCardsDatas : FightForManagerForStart.instance.playerFightCardsDatas;
        List<int> canFightUnits = new List<int>();
        for (int i = 0; i < fightCardDatas.Length; i++)
        {
            if (i != attackedUnitForIndex.posIndex && fightCardDatas[i] != null && fightCardDatas[i].Hp > 0)
            {
                canFightUnits.Add(i);
            }
        }
        isNeedToAttack = false;

        int fightNums = 0;
        if (classIndex == 20)   //弓兵和大弓
        {
            fightNums = Mathf.Min(DataTable.GetGameValue(76), canFightUnits.Count);
            damage = (int)(damage * (DataTable.GetGameValue(77) / 100f) / (fightNums + 1));
        }
        else
        {
            fightNums = Mathf.Min(DataTable.GetGameValue(78), canFightUnits.Count);
            damage = (int)(damage * (DataTable.GetGameValue(79) / 100f) / (fightNums + 1));
        }

        List<int> attackedIndexList = BackRandsList(canFightUnits, fightNums);
        attackedIndexList.Add(attackedUnitForIndex.posIndex);

        for (int i = 0; i < attackedIndexList.Count; i++)
        {
            FightCardData attackedUnit = fightCardDatas[attackedIndexList[i]];
            AttackToEffectShow(attackedUnit, false, Effect.Bow20A);
            int nowDamage = DefDamageProcessFun(attackUnit, attackedUnit, damage);
            attackedUnit.Hp.Add(-nowDamage);
            AttackedAnimShow(attackedUnit, nowDamage, false);
            if (attackedUnit.cardType == 522)
            {
                if (attackedUnit.Hp <= 0)
                {
                    recordWinner = attackedUnit.isPlayerCard ? -1 : 1;
                }
            }
        }
    }

    //辅佐庇护技能
    private void FuZuoBiHuSkill(int finalDamage, FightCardData attackUnit)
    {
        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.playerFightCardsDatas : FightForManagerForStart.instance.enemyFightCardsDatas;
        int addIndex = -1;
        float minFlo = 2;
        for (int i = 0; i < fightCardDatas.Length; i++)
        {
            if (fightCardDatas[i] != null && fightCardDatas[i].cardType == 0 && fightCardDatas[i].Hp > 0 && fightCardDatas[i].CardState.ExtendedHp < 1000)
            {
                float nowFlo = (fightCardDatas[i].Hp + fightCardDatas[i].CardState.ExtendedHp) / (float)fightCardDatas[i].Hp.Max;
                if (nowFlo < minFlo)
                {
                    minFlo = nowFlo;
                    addIndex = i;
                }
            }
        }
        if (addIndex != -1)
        {
            isNeedToAttack = false;
            AddOrCutShieldValue(finalDamage, fightCardDatas[addIndex], true);
            //Debug.Log("---辅佐技能");
            ShowSpellTextObj(attackUnit.cardObj, "39", false);
            AttackToEffectShow(fightCardDatas[addIndex], false, Effect.Support39A);
            PlayAudioForSecondClip(39, 0);
        }
    }

    //内政技能
    private void NeiZhengSkill(FightCardData attackUnit)
    {
        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.playerFightCardsDatas : FightForManagerForStart.instance.enemyFightCardsDatas;

        int prop = DataTable.GetGameValue(127) * attackUnit.cardGrade + DataTable.GetGameValue(126);

        int cardIndex = -1;
        int cardIndex2 = -1;    //非普通攻击
        int cardIndex3 = -1;    //会心一击
        int maxBadNums = 0;
        for (int i = 0; i < fightCardDatas.Length; i++)
        {
            int nowBadNums = 0;
            if (fightCardDatas[i] != null && fightCardDatas[i].Hp > 0 && fightCardDatas[i].cardType == 0)
            {
                if (fightCardDatas[i].CardState.Stunned > 0)
                {
                    nowBadNums += fightCardDatas[i].CardState.Stunned;
                }
                if (fightCardDatas[i].CardState.Imprisoned > 0)
                {
                    nowBadNums += fightCardDatas[i].CardState.Imprisoned;
                }
                if (fightCardDatas[i].CardState.Bleed > 0)
                {
                    nowBadNums += fightCardDatas[i].CardState.Bleed;
                }
                if (fightCardDatas[i].CardState.Poison > 0)
                {
                    nowBadNums += fightCardDatas[i].CardState.Poison;
                }
                if (fightCardDatas[i].CardState.Burn > 0)
                {
                    nowBadNums += fightCardDatas[i].CardState.Burn;
                }
                if (fightCardDatas[i].CardState.Unarmed > 0)
                {
                    nowBadNums += fightCardDatas[i].CardState.Unarmed;
                }
                if (fightCardDatas[i].CardState.Cowardly > 0)
                {
                    nowBadNums += fightCardDatas[i].CardState.Cowardly;
                }
                if (nowBadNums > maxBadNums)  //记录负面效果最多的单位
                {
                    maxBadNums = nowBadNums;
                    cardIndex3 = cardIndex2;
                    cardIndex2 = cardIndex;
                    cardIndex = i;
                }
            }
        }
        if (cardIndex != -1)
        {
            isNeedToAttack = false;
            //Debug.Log("---内政技能");
            ShowSpellTextObj(attackUnit.cardObj, "38", false);
            PlayAudioForSecondClip(38, 0);
            AttackToEffectShow(fightCardDatas[cardIndex], false, Effect.StateAffairs38A);
            ShowSpellTextObj(fightCardDatas[cardIndex].cardObj, DataTable.GetStringText(14), true, false);

            if (TakeSpecialAttack(prop))
            {
                ClearOneUnitBadState(fightCardDatas[cardIndex]);
            }
        }
        if (indexAttackType != 0 && cardIndex2 != -1)
        {
            cardIndex = cardIndex2;
            isNeedToAttack = false;
            ShowSpellTextObj(attackUnit.cardObj, "38", false);
            PlayAudioForSecondClip(38, 0);
            AttackToEffectShow(fightCardDatas[cardIndex], false, Effect.StateAffairs38A);
            ShowSpellTextObj(fightCardDatas[cardIndex].cardObj, DataTable.GetStringText(14), true, false);
            if (TakeSpecialAttack(prop))
            {
                ClearOneUnitBadState(fightCardDatas[cardIndex]);
            }

            if (indexAttackType == 1 && cardIndex3 != -1)
            {
                cardIndex = cardIndex3;
                isNeedToAttack = false;
                ShowSpellTextObj(attackUnit.cardObj, "38", false);
                PlayAudioForSecondClip(38, 0);
                AttackToEffectShow(fightCardDatas[cardIndex], false, Effect.StateAffairs38A);
                ShowSpellTextObj(fightCardDatas[cardIndex].cardObj, DataTable.GetStringText(14), true, false);
                if (TakeSpecialAttack(prop))
                {
                    ClearOneUnitBadState(fightCardDatas[cardIndex]);
                }
            }
        }
    }

    //清除单个单位的负面状态
    private void ClearOneUnitBadState(FightCardData cardData)
    {
        if (cardData.CardState.Stunned > 0)
        {
            cardData.CardState.Stunned = 0;
            FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_dizzy, true);
        }
        if (cardData.CardState.Imprisoned > 0)
        {
            cardData.CardState.Imprisoned = 0;
            FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_imprisoned, true);
        }
        if (cardData.CardState.Bleed > 0)
        {
            cardData.CardState.Bleed = 0;
            FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_bleed, true);
        }
        if (cardData.CardState.Poison > 0)
        {
            cardData.CardState.Poison = 0;
            FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_poisoned, true);
        }
        if (cardData.CardState.Burn > 0)
        {
            cardData.CardState.Burn = 0;
            FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_burned, true);
        }
        if (cardData.CardState.Unarmed > 0)
        {
            cardData.CardState.Unarmed = 0;
            FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_removeArmor, true);
        }
        if (cardData.CardState.Cowardly > 0)
        {
            cardData.CardState.Cowardly = 0;
            FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_cowardly, true);
        }
    }

    //医生技能
    private void YiShengSkill(int fightNums, FightCardData attackUnit, int classType)
    {
        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.playerFightCardsDatas : FightForManagerForStart.instance.enemyFightCardsDatas;
        List<int> canHuiFuUnits = new List<int>();
        for (int i = 0; i < fightCardDatas.Length; i++)
        {
            if (fightCardDatas[i] != null && fightCardDatas[i].cardType == 0 && fightCardDatas[i].Hp > 0 && fightCardDatas[i].Hp != fightCardDatas[i].Hp.Max)
            {
                canHuiFuUnits.Add(i);
            }
        }
        if (canHuiFuUnits.Count > 0)
        {
            isNeedToAttack = false;

            if (fightNums > canHuiFuUnits.Count)
            {
                fightNums = canHuiFuUnits.Count;
            }
            canHuiFuUnits.Sort((int a, int b) =>
            {
                return fightCardDatas[a].Hp.CompareTo(fightCardDatas[b].Hp);
            });

            ShowSpellTextObj(attackUnit.cardObj, classType.ToString(), false);

            int addtionNums = 0;
            string effectStr = "";
            if (classType == 42)  //医士
            {
                effectStr = Effect.Heal42A;
                PlayAudioForSecondClip(42, 0);
                addtionNums = (int)(attackUnit.damage * (DataTable.GetGameValue(80) / 100f) / fightNums);
            }
            else
            {//大医士
                effectStr = Effect.Heal43A;
                PlayAudioForSecondClip(43, 0);
                addtionNums = (int)(attackUnit.damage * (DataTable.GetGameValue(81) / 100f) / fightNums);
            }
            for (int i = 0; i < fightNums; i++)
            {
                AttackToEffectShow(fightCardDatas[canHuiFuUnits[i]], false, effectStr);
                fightCardDatas[canHuiFuUnits[i]].Hp.Add(+addtionNums);
                ShowSpellTextObj(fightCardDatas[canHuiFuUnits[i]].cardObj, DataTable.GetStringText(15), true, false);
                AttackedAnimShow(fightCardDatas[canHuiFuUnits[i]], addtionNums, true);
            }
        }
    }

    //美人技能
    private void MeiRenJiNeng(FightCardData attackUnit, int classType)
    {
        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.playerFightCardsDatas : FightForManagerForStart.instance.enemyFightCardsDatas;
        int index = -1;
        bool isHadFirst = false;

        PlayAudioForSecondClip(classType, 0);

        int prop = DataTable.GetGameValue(129) * attackUnit.cardGrade + DataTable.GetGameValue(128);
        int fightNums = 1;  //添加单位数量
        if (indexAttackType != 0)
        {
            if (indexAttackType == 1)
            {
                fightNums = DataTable.GetGameValue(131);
            }
            else
            {
                fightNums = DataTable.GetGameValue(130);
            }
        }
        for (int m = 0; m < fightNums; m++)
        {
            for (int i = 0; i < fightCardDatas.Length; i++)
            {
                if (fightCardDatas[i] != null && fightCardDatas[i].cardType == 0 && fightCardDatas[i].Hp > 0)
                {
                    if (!isHadFirst)
                    {
                        index = i;
                        isHadFirst = true;
                    }
                    else
                    {
                        if (classType == 45)
                        {
                            if (fightCardDatas[i].damage > fightCardDatas[index].damage)
                            {
                                if (fightCardDatas[i].CardState.Neizhu <= fightCardDatas[index].CardState.Neizhu)
                                {
                                    index = i;
                                }
                                else
                                {
                                    //新单位伤害高，但内助层数也高
                                }
                            }
                            else
                            {
                                if (fightCardDatas[i].CardState.Neizhu < fightCardDatas[index].CardState.Neizhu)
                                {
                                    index = i;
                                }
                                else
                                {
                                    //新单位伤害低，而且内助层数也高
                                }
                            }
                        }
                        else
                        {
                            if (fightCardDatas[i].damage > fightCardDatas[index].damage)
                            {
                                if (fightCardDatas[i].CardState.ShenZhu <= fightCardDatas[index].CardState.ShenZhu)
                                {
                                    index = i;
                                }
                                else
                                {
                                    //新单位伤害高，但内助层数也高
                                }
                            }
                            else
                            {
                                if (fightCardDatas[i].CardState.ShenZhu < fightCardDatas[index].CardState.ShenZhu)
                                {
                                    index = i;
                                }
                                else
                                {
                                    //新单位伤害低，而且内助层数也高
                                }
                            }
                        }
                    }
                }
            }
            if (index != -1)
            {
                isNeedToAttack = false;
                ShowSpellTextObj(attackUnit.cardObj, classType.ToString(), false);
                if (TakeSpecialAttack(prop))
                {
                    AttackToEffectShow(fightCardDatas[index], false, classType + "A");
                    if (classType == 45)  //美人
                    {
                        if (fightCardDatas[index].CardState.Neizhu <= 0)
                        {
                            FightForManagerForStart.instance.CreateSateIcon(fightCardDatas[index].cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_neizhu, false);
                        }
                        fightCardDatas[index].CardState.Neizhu++;
                    }
                    else
                    {//大美人
                        if (fightCardDatas[index].CardState.ShenZhu <= 0)
                        {
                            FightForManagerForStart.instance.CreateSateIcon(fightCardDatas[index].cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_shenzhu, false);
                        }
                        fightCardDatas[index].CardState.ShenZhu++;
                    }
                }
            }
        }
    }

    //谋士技能
    private void MouShiSkill(int fightNums, FightCardData attackUnit, int classType)
    {
        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.enemyFightCardsDatas : FightForManagerForStart.instance.playerFightCardsDatas;
        List<int> canFightUnits = new List<int>();
        for (int i = 0; i < fightCardDatas.Length; i++)
        {
            if (fightCardDatas[i] != null && fightCardDatas[i].Hp > 0 && fightCardDatas[i].cardType == 0)
            {
                canFightUnits.Add(i);
            }
        }
        if (canFightUnits.Count > 0)
        {
            isNeedToAttack = false;
            List<int> attackedIndexList = BackRandsList(canFightUnits, fightNums);
            //Debug.Log("---谋士技能");
            ShowSpellTextObj(attackUnit.cardObj, classType.ToString(), false);
            string effectStr = "";
            if (classType == 36)  //谋士
            {
                effectStr = "36A";
                PlayAudioForSecondClip(36, 0);
            }
            else
            {//大谋士
                effectStr = "37A";
                PlayAudioForSecondClip(37, 0);
            }

            //眩晕概率
            int propNums = DataTable.GetGameValue(83) * attackUnit.cardGrade + DataTable.GetGameValue(82);
            if (indexAttackType == 2)
            {
                propNums += DataTable.GetGameValue(84);
            }
            else
            {
                if (indexAttackType == 1)
                {
                    propNums += DataTable.GetGameValue(85);
                }
            }

            for (int i = 0; i < attackedIndexList.Count; i++)
            {
                AttackToEffectShow(fightCardDatas[attackedIndexList[i]], false, effectStr);
                TakeOneUnitDizzed(fightCardDatas[attackedIndexList[i]], propNums);
            }
        }
    }

    //毒士技能
    private void DuShiSkill(int fightNums, FightCardData attackUnit, int classType, int finalDamage)
    {
        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.enemyFightCardsDatas : FightForManagerForStart.instance.playerFightCardsDatas;
        List<int> canFightUnits = new List<int>();
        for (int i = 0; i < fightCardDatas.Length; i++)
        {
            if (fightCardDatas[i] != null && fightCardDatas[i].Hp > 0 && fightCardDatas[i].cardType == 0)
            {
                canFightUnits.Add(i);
            }
        }
        if (canFightUnits.Count > 0)
        {
            isNeedToAttack = false;
            List<int> attackedIndexList = BackRandsList(canFightUnits, fightNums);
            //Debug.Log("---毒士技能");
            ShowSpellTextObj(attackUnit.cardObj, classType.ToString(), false);

            finalDamage = (int)(finalDamage * DataTable.GetGameValue(86) / 100f);

            string effectStr = "";
            if (classType == 30)  //毒士
            {
                effectStr = "30A";
                PlayAudioForSecondClip(30, 0);
            }
            else
            {//大毒士
                effectStr = "31A";
                PlayAudioForSecondClip(31, 0);
            }

            int prop = DataTable.GetGameValue(89) * attackUnit.cardGrade + DataTable.GetGameValue(88);
            if (indexAttackType != 0)
            {
                if (indexAttackType == 1)
                {
                    prop += DataTable.GetGameValue(124);
                }
                else
                {
                    prop += DataTable.GetGameValue(125);
                }
            }
            prop = Mathf.Min(DataTable.GetGameValue(87), prop);

            for (int i = 0; i < attackedIndexList.Count; i++)
            {
                AttackToEffectShow(fightCardDatas[attackedIndexList[i]], false, effectStr);
                TakeToPoisoned(fightCardDatas[attackedIndexList[i]], prop);

                int backDamage = DefDamageProcessFun(attackUnit, fightCardDatas[attackedIndexList[i]], finalDamage);
                fightCardDatas[attackedIndexList[i]].Hp.Add(-backDamage);
                AttackedAnimShow(fightCardDatas[attackedIndexList[i]], backDamage, false);
            }
        }
    }

    //敢死死战技能
    private int GanSiSiZhanAttack(int finalDamage, FightCardData attackedUnit)
    {
        if (attackedUnit.CardState.DeathFight >= 1)
        {
            //Debug.Log("---敢死死战");
            ShowSpellTextObj(attackedUnit.cardObj, "41", false);
            finalDamage = -1 * finalDamage;
        }
        return finalDamage;
    }

    //刺客破甲技能
    private void CiKePoJiaAttack(FightCardData attackedUnit, FightCardData attackUnit)
    {
        //Debug.Log("---刺客破甲");
        PlayAudioForSecondClip(25, 0);
        AttackToEffectShow(attackedUnit, false, Effect.Assassin25A);
        if (attackedUnit.cardType == 0)
        {
            ShowSpellTextObj(attackUnit.cardObj, "25", false);
            if (TakeSpecialAttack(DataTable.GetGameValue(147)))
            {
                if (attackedUnit.CardState.Bleed <= 0)
                {
                    FightForManagerForStart.instance.CreateSateIcon(attackedUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_bleed, true);
                }
            }
            attackedUnit.CardState.Bleed++;
            ShowSpellTextObj(attackUnit.cardObj, DataTable.GetStringText(16), true, true);
        }
    }

    IEnumerator TakeCardPosBack(FightCardData[] fightCardDatas, FightCardData attackedUnit, int nextPos, float waitTime, bool isPlayer)
    {
        yield return new WaitForSeconds(waitTime);

        FightForManagerForStart.instance.CardGoIntoBattleProcess(attackedUnit, attackedUnit.posIndex, fightCardDatas, false);

        ShowSpellTextObj(attackedUnit.cardObj, DataTable.GetStringText(17), true, true);
        attackedUnit.cardObj.transform.DOMove(
            isPlayer ? FightForManagerForStart.instance.enemyCardsPos[nextPos].transform.position : FightForManagerForStart.instance.playerCardsPos[nextPos].transform.position,
            waitTime
            ).SetEase(Ease.Unset).OnComplete(delegate ()
            {
                FightForManagerForStart.instance.CardGoIntoBattleProcess(attackedUnit, nextPos, fightCardDatas, true);
            });

        fightCardDatas[nextPos] = attackedUnit;
        fightCardDatas[attackedUnit.posIndex] = null;
        attackedUnit.posIndex = nextPos;
    }

    //战船冲击技能
    private int ZhanChuanChongJiAttack(int finalDamage, FightCardData attackedUnit, FightCardData attackUnit)
    {
        //使敌方武将和士兵单位往后退一格。当敌方单位无法再后退时，造成2.5倍伤害
        PlayAudioForSecondClip(21, 0);
        AttackToEffectShow(attackedUnit, false, Effect.Warship21A);
        ShowSpellTextObj(attackUnit.cardObj, "21", false);
        if (attackedUnit.cardType == 0)
        {
            FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.enemyFightCardsDatas : FightForManagerForStart.instance.playerFightCardsDatas;
            int nextPos = attackedUnit.posIndex + 5;
            if (nextPos <= 19 && fightCardDatas[nextPos] == null)
            {
                StartCoroutine(TakeCardPosBack(fightCardDatas, attackedUnit, nextPos, 0.2f, isPlayerRound));
            }
            else
            {
                finalDamage = (int)(finalDamage * DataTable.GetGameValue(90) / 100f);
            }
        }
        return finalDamage;
    }

    //战车撞压技能
    private int ZhanCheZhuangYaAttack(int finalDamage, FightCardData attackedUnit, FightCardData attackUnit)
    {
        //50%概率使敌方武将和士兵单位【眩晕】。对已经眩晕单位，造成2.5倍伤害
        PlayAudioForSecondClip(22, 0);
        ShowSpellTextObj(attackUnit.cardObj, "22", false);
        AttackToEffectShow(attackedUnit, false, Effect.Chariot22A);
        if (attackedUnit.CardState.Stunned > 0)
        {
            finalDamage = (int)(finalDamage * DataTable.GetGameValue(92) / 100f);
        }
        TakeOneUnitDizzed(attackedUnit, DataTable.GetGameValue(91));
        return finalDamage;
    }

    //斧兵屠戮技能
    private int FuBingTuLuAttack(int finalDamage, FightCardData attackedUnit, FightCardData attackUnit)
    {
        PlayAudioForSecondClip(18, 0);
        AttackToEffectShow(attackedUnit, false, Effect.Axe18A);

        //破护盾
        if (attackedUnit.CardState.Shield > 0)
        {
            attackedUnit.CardState.Shield = 0;
            FightForManagerForStart.instance.DestroySateIcon(attackedUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_withStand, true);
        }

        float damageProp = (1f - attackedUnit.Hp.Rate()) / (DataTable.GetGameValue(93) / 100f) * (DataTable.GetGameValue(94) / 100f);
        if (damageProp > 0)
        {
            ShowSpellTextObj(attackUnit.cardObj, "18", false);
            finalDamage = (int)(finalDamage * (1f + damageProp));
        }
        return finalDamage;
    }

    //戟兵横扫技能
    private void JianBingHengSaoAttack(int finalDamage, FightCardData attackUnit, FightCardData attackedUnit)
    {
        PlayAudioForSecondClip(15, 0);
        AttackToEffectShow(attackedUnit, false, Effect.Halberd15A);
        ShowSpellTextObj(attackUnit.cardObj, "15", false);

        //对目标周围的其他单位造成50%伤害
        finalDamage = (int)(finalDamage * DataTable.GetGameValue(95) / 100f);
        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.enemyFightCardsDatas : FightForManagerForStart.instance.playerFightCardsDatas;
        for (int i = 0; i < FightForManagerForStart.instance.CardNearbyAdditionForeach[targetIndex].Length; i++)
        {
            FightCardData attackedUnits = fightCardDatas[FightForManagerForStart.instance.CardNearbyAdditionForeach[targetIndex][i]];
            if (attackedUnits != null && attackedUnits.Hp > 0)
            {
                int backDamage = DefDamageProcessFun(attackUnit, attackedUnits, finalDamage);
                attackedUnits.Hp.Add(-backDamage);
                AttackedAnimShow(attackedUnits, backDamage, false);
                if (attackedUnits.cardType == 522)
                {
                    if (attackedUnits.Hp <= 0)
                    {
                        recordWinner = attackedUnits.isPlayerCard ? -1 : 1;
                    }
                }
            }
        }
    }

    //枪兵穿刺技能
    private void QiangBingChuanCiAttack(int finalDamage, FightCardData attackUnit, FightCardData attackedUnit, int classType)
    {
        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.enemyFightCardsDatas : FightForManagerForStart.instance.playerFightCardsDatas;
        PlayAudioForSecondClip(14, 0);
        ShowSpellTextObj(attackUnit.cardObj, "14", false);
        GameObject effectObj = AttackToEffectShow(attackedUnit, false, classType + "A");
        effectObj.transform.localScale = new Vector3(1, attackUnit.isPlayerCard ? 1 : -1, 1);

        //对目标身后单位造成100 % 伤害
        finalDamage = (int)(finalDamage * DataTable.GetGameValue(96) / 100f);
        int fightNums = classType == 14 ? 2 : 1;    //穿刺目标个数

        int chuanCiUnitId = targetIndex;
        for (int n = 0; n < fightNums; n++)
        {
            chuanCiUnitId = chuanCiUnitId + 5;
            if (chuanCiUnitId < 20 && fightCardDatas[chuanCiUnitId] != null && fightCardDatas[chuanCiUnitId].Hp > 0)
            {
                int backDamage = DefDamageProcessFun(attackUnit, fightCardDatas[chuanCiUnitId], finalDamage);
                fightCardDatas[chuanCiUnitId].Hp.Add(-backDamage);
                AttackedAnimShow(fightCardDatas[chuanCiUnitId], backDamage, false);
                if (fightCardDatas[chuanCiUnitId].cardType == 522)
                {
                    if (fightCardDatas[chuanCiUnitId].Hp <= 0)
                    {
                        recordWinner = fightCardDatas[chuanCiUnitId].isPlayerCard ? -1 : 1;
                    }
                }
            }
        }
    }

    //神武战意技能
    private int ShenWuZhanYiAttack(int finalDamage, FightCardData attackUnit, FightCardData attackedUnit)
    {
        //每次攻击时，获得【战意】状态，提升伤害，可叠加10次
        if (attackUnit.CardState.Stimulate <= 0)
        {
            FightForManagerForStart.instance.CreateSateIcon(attackUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_willFight, true);
        }
        if (attackUnit.CardState.Stimulate < 10)
        {
            attackUnit.CardState.Stimulate++;
            attackUnit.cardObj.transform.Find(StringNameStatic.StateIconPath_willFight + "Din").GetComponent<Image>().color = new Color(1, 1, 1, 0.4f + 0.6f * (attackUnit.CardState.Stimulate / 10f));
        }
        PlayAudioForSecondClip(12, 0);
        AttackToEffectShow(attackedUnit, false, Effect.Stimulate12A);
        ShowSpellTextObj(attackUnit.cardObj, "12", false);
        finalDamage = (int)(finalDamage * (1 + DataTable.GetGameValue(97) / 100f * attackUnit.CardState.Stimulate));
        return finalDamage;
    }

    //白马无畏技能
    private int TieQiWuWeiAttack(int finalDamage, FightCardData attackUnit, FightCardData attackedUnit)
    {
        PlayAudioForSecondClip(11, 0);
        AttackToEffectShow(attackedUnit, false, Effect.Knight11A);
        //自身血量每降低10%，提高15%伤害
        float damageProp = (1f - attackUnit.Hp.Rate()) / (DataTable.GetGameValue(98) / 100f) * (DataTable.GetGameValue(99) / 100f);
        if (damageProp > 0)
        {
            ShowSpellTextObj(attackUnit.cardObj, "11", false);
            finalDamage = (int)(finalDamage * (1f + damageProp));
        }
        return finalDamage;
    }

    //死士舍命技能
    private int SiShiSheMingAttack(int finalDamage, FightCardData attackUnit, FightCardData attackedUnit)
    {
        //血量低于25%时，获得【舍命】。下次攻击时，发起自杀式攻击，对敌方全体武将造成一次100%伤害
        if (attackUnit.Hp.Rate() <= (DataTable.GetGameValue(100) / 100f))
        {
            attackUnit.Hp.Set(0);
            UpdateUnitHpShow(attackUnit);
            ShowSpellTextObj(attackUnit.cardObj, "10", false);
            PlayAudioForSecondClip(10, 0);
            finalDamage = (int)(finalDamage * DataTable.GetGameValue(101) / 100f);

            FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.enemyFightCardsDatas : FightForManagerForStart.instance.playerFightCardsDatas;
            for (int i = 0; i < fightCardDatas.Length; i++)
            {
                FightCardData attackedUnits = fightCardDatas[i];
                if (attackedUnits != null && attackedUnits.cardType == 0 && attackedUnits.Hp > 0)
                {
                    int backDamage = DefDamageProcessFun(attackUnit, attackedUnits, finalDamage);
                    attackedUnits.Hp.Add(-backDamage);
                    AttackedAnimShow(attackedUnits, backDamage, false);
                    AttackToEffectShow(attackedUnits, false, Effect.Daredevil10A);
                }
            }
            AttackToEffectShow(attackedUnit, false, Effect.Daredevil10A);

            return finalDamage;
        }
        else
        {
            PlayAudioForSecondClip(0, 0);
            AttackToEffectShow(attackedUnit, true);
            return finalDamage;
        }
    }

    //象兵践踏技能
    private void XiangBingTrampleAttAck(FightCardData attackedUnit, FightCardData attackUnit)
    {
        //Debug.Log("---象兵践踏");
        ShowSpellTextObj(attackUnit.cardObj, "8", false);

        PlayAudioForSecondClip(8, 0);
        AttackToEffectShow(attackedUnit, false, Effect.Elephant8A);
        TakeOneUnitDizzed(attackedUnit, DataTable.GetGameValue(102));
    }

    //刺甲兵种反伤-护盾闪避无效
    private void CiJiaFanShangAttack(int finalDamage, FightCardData attackedUnit, FightCardData attackUnit)
    {
        if (attackedUnit.combatType == 0)    //近战
        {
            PlayAudioForSecondClip(7, 0.2f);
            ShowSpellTextObj(attackUnit.cardObj, "7", false);
            finalDamage = DefDamageProcessFun(attackUnit, attackedUnit, finalDamage);
            attackedUnit.Hp.Add(-finalDamage);
            AttackedAnimShow(attackedUnit, finalDamage, false);
            GameObject effectObj = AttackToEffectShow(attackedUnit, false, Effect.Blademail7A);
            effectObj.transform.localScale = new Vector3(1, attackUnit.isPlayerCard ? 1 : -1, 1);
        }
    }
    #endregion

    //敢死兵种添加死战状态
    private void SiZhanStateCreate(FightCardData attackedUnit)
    {
        if (attackedUnit.CardState.DeathFight == 0 && attackedUnit.Hp > 0 && attackedUnit.Hp.Rate() < (DataTable.GetGameValue(103) / 100f))
        {
            //Debug.Log("---附加死战状态");
            ShowSpellTextObj(attackedUnit.cardObj, "41", false);
            if (attackedUnit.Hp <= 0)
            {
                attackedUnit.Hp.Set(1);
            }
            if (attackedUnit.CardState.DeathFight <= 0)
            {
                FightForManagerForStart.instance.CreateSateIcon(attackedUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_deathFight, true);
            }
            attackedUnit.CardState.DeathFight = 1;
        }
    }

    /// <summary>
    /// 初始化无敌状态或死战状态
    /// </summary>
    /// <param name="fightCardData"></param>
    private void InitCardAfterFightedState(FightCardData fightCardData)
    {
        if (fightCardData.CardState.Stunned > 0 || fightCardData.CardState.Imprisoned > 0)
            return;
        if (fightCardData.cardType == 0)
        {
            switch (DataTable.Hero[fightCardData.cardId].MilitaryUnitTableId)
            {
                //武将的陷阵兵种
                case 5:
                    if (fightCardData.Hp.Rate() <= (DataTable.GetGameValue(104) / 100f))
                    {
                        if (fightCardData.CardState.Invincible <= 0)
                        {
                            fightCardData.CardState.Invincible = DataTable.GetGameValue(105);
                            FightForManagerForStart.instance.CreateSateIcon(fightCardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_invincible, true);
                        }
                    }
                    break;
                //敢死兵种添加死战
                case 41:
                    SiZhanStateCreate(fightCardData);
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// 防御伤害计算流程
    /// </summary>
    /// <param name="attackUnit">攻击者</param>
    /// <param name="attackedUnit">被攻击者</param>
    /// <param name="damage">伤害值</param>
    public int DefDamageProcessFun(FightCardData attackUnit, FightCardData attackedUnit, int damage)
    {
        attackedUnit.attackedBehavior = 0;
        int finalDamage = damage;
        if (attackedUnit.cardType == 0)
        {
            var attUnit = HeroCombatInfo.GetInfo(attackUnit.cardId);
            var defUnit = HeroCombatInfo.GetInfo(attackedUnit.cardId);
            //判断闪避
            int dodgeRate = defUnit.DodgeRatio + attackedUnit.CardState.DodgeUp;
            if (DataTable.Hero[attackedUnit.cardId].MilitaryUnitTableId == 3 || DataTable.Hero[attackedUnit.cardId].MilitaryUnitTableId == 10)   //飞甲，死士 自身血量每降低10 %，提高5%闪避
            {
                if (DataTable.Hero[attackedUnit.cardId].MilitaryUnitTableId == 3)
                {
                    dodgeRate = dodgeRate + (int)((1f - attackedUnit.Hp.Rate()) / (DataTable.GetGameValue(106) / 100f) * DataTable.GetGameValue(107));
                }
                else
                {
                    dodgeRate = dodgeRate + (int)((1f - attackedUnit.Hp.Rate()) / (DataTable.GetGameValue(108) / 100f) * DataTable.GetGameValue(109));
                }
            }
            if (TakeSpecialAttack(dodgeRate))
            {
                finalDamage = 0;
                ShowSpellTextObj(attackedUnit.cardObj, DataTable.GetStringText(19), false);
                PlayAudioForSecondClip(97, 0);
                attackedUnit.attackedBehavior = 2;
            }
            else
            {
                //远程攻击者，判断远程闪避
                if (attackUnit.cardType == 0 && attackUnit.combatType == 1 && TakeSpecialAttack(attackedUnit.CardState.MiWuZhenAddOn))
                {
                    finalDamage = 0;
                    ShowSpellTextObj(attackedUnit.cardObj, DataTable.GetStringText(19), false);
                    PlayAudioForSecondClip(97, 0);
                    attackedUnit.attackedBehavior = 2;
                }
                else
                {
                    //判断无敌
                    if (attackedUnit.CardState.Invincible > 0)
                    {
                        finalDamage = 0;
                        ShowSpellTextObj(attackedUnit.cardObj, DataTable.GetStringText(18), false);
                        PlayAudioForSecondClip(96, 0);
                        attackedUnit.attackedBehavior = 4;
                    }
                    else
                    {
                        //判断护盾//不可抵挡法术
                        if ((attackUnit.cardType != 0 || attackUnit.cardDamageType == 0) && OffsetWithStand(attackedUnit))
                        {
                            finalDamage = 0;
                            ShowSpellTextObj(attackedUnit.cardObj, DataTable.GetStringText(18), false);
                            PlayAudioForSecondClip(96, 0);
                            attackedUnit.attackedBehavior = 3;
                        }
                        else
                        {
                            //免伤计算
                            if (attackedUnit.CardState.Unarmed <= 0)   //是否有卸甲
                            {
                                int defPropNums = defUnit.Armor + attackedUnit.CardState.FengHuoTaiAddOn;
                                //白马/重甲，自身血量每降低10%，提高5%免伤
                                switch (DataTable.Hero[attackedUnit.cardId].MilitaryUnitTableId)
                                {
                                    case 2:
                                        //重甲，自身血量每降低10%，提高5%免伤
                                        defPropNums = defPropNums + (int)((1f - attackedUnit.Hp.Rate()) / (DataTable.GetGameValue(110) / 100f) * DataTable.GetGameValue(111));
                                        break;
                                    case 11:
                                        //白马，自身血量每降低10%，提高5%免伤
                                        defPropNums = defPropNums + (int)((1f - attackedUnit.Hp.Rate()) / (DataTable.GetGameValue(112) / 100f) * DataTable.GetGameValue(113));
                                        break;
                                    case 58:
                                        //铁骑，单位越多免伤越高
                                        List<FightCardData> tieQiList = attackedUnit.isPlayerCard ? tieQiCardsPy : tieQiCardsEm;
                                        int nowTieQiNums = 0;
                                        for (int i = 0; i < tieQiList.Count; i++)
                                        {
                                            if (tieQiList[i].Hp > 0)
                                            {
                                                nowTieQiNums++;
                                            }
                                        }
                                        if (nowTieQiNums > 1)
                                        {
                                            defPropNums = defPropNums + Mathf.Min(DataTable.GetGameValue(114), nowTieQiNums * DataTable.GetGameValue(115));
                                        }
                                        break;
                                    default:
                                        break;
                                }
                                defPropNums = defPropNums > DataTable.GetGameValue(116) ? DataTable.GetGameValue(116) : defPropNums;
                                finalDamage = (int)((100f - defPropNums) / 100f * finalDamage);
                                //判断攻击者的伤害类型，获得被攻击者的物理或法术免伤百分比
                                defPropNums = GameCardInfo.GetInfo((GameCardType)attackUnit.cardType, attackUnit.cardId).DamageType == 0 ? defUnit.PhysicalResist : defUnit.MagicResist;
                                finalDamage = (int)((100f - defPropNums) / 100f * finalDamage);
                            }

                            //藤甲免疫物理伤害
                            if (DataTable.Hero[attackedUnit.cardId].MilitaryUnitTableId == 57 && attackUnit.cardDamageType == 0)
                            {
                                finalDamage = 0;
                            }

                            //流血状态加成
                            if (attackedUnit.CardState.Bleed > 0)
                            {
                                finalDamage = (int)(finalDamage * DataTable.GetGameValue(117) / 100f);
                            }
                            //抵扣防护盾
                            finalDamage = AddOrCutShieldValue(finalDamage, attackedUnit, false);
                        }
                    }
                }
            }
            if (attackedUnit.cardType == 0)
            {
                var defInfo = GameCardInfo.GetInfo((GameCardType) attackedUnit.cardType, attackedUnit.cardId);
                switch (DataTable.Hero[defInfo.Id].MilitaryUnitTableId)
                {
                    case 1:
                        //近战兵种受到暴击和会心加盾
                        if (attackedUnit.CardState.Stunned <= 0 && attackedUnit.CardState.Imprisoned <= 0)
                        {
                            if (indexAttackType != 0)
                            {
                                if (attackedUnit.CardState.Shield <= 0)
                                {
                                    FightForManagerForStart.instance.CreateSateIcon(attackedUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_withStand, true);
                                }
                                attackedUnit.CardState.Shield++;
                            }
                        }
                        break;
                    case 12:
                        //神武战意技能
                        if (attackedUnit.CardState.Stimulate <= 0)
                        {
                            FightForManagerForStart.instance.CreateSateIcon(attackedUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_willFight, true);
                        }
                        if (attackedUnit.CardState.Stimulate < 10)
                        {
                            attackedUnit.CardState.Stimulate++;
                            attackedUnit.cardObj.transform.Find(StringNameStatic.StateIconPath_willFight + "Din").GetComponent<Image>().color = new Color(1, 1, 1, 0.4f + 0.6f * (attackedUnit.CardState.Stimulate / 10f));
                        }
                        ShowSpellTextObj(attackedUnit.cardObj, "12", false);
                        break;
                    case 41:
                        //敢死死战技能
                        finalDamage = GanSiSiZhanAttack(finalDamage, attackedUnit);
                        break;
                    default:
                        break;
                }
                if (attackUnit.CardState.Stunned <= 0 && attackUnit.CardState.Imprisoned <= 0)
                {
                    switch (DataTable.Hero[attackUnit.cardId].MilitaryUnitTableId)
                    {
                        case 6:
                            //虎卫吸血
                            if (attackUnit.CardState.Stunned <= 0 && attackUnit.CardState.Imprisoned <= 0)
                            {
                                ShowSpellTextObj(attackUnit.cardObj, "6", false);
                                int addHp = finalDamage;
                                attackUnit.Hp.Add(addHp);
                                AttackedAnimShow(attackUnit, addHp, true);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        return finalDamage;
    }

    //抵消护盾
    private bool OffsetWithStand(FightCardData fightCardData)
    {
        if (fightCardData.CardState.Shield > 0)
        {
            fightCardData.CardState.Shield--;
            if (fightCardData.CardState.Shield <= 0)
            {
                fightCardData.CardState.Shield = 0;
                FightForManagerForStart.instance.DestroySateIcon(fightCardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_withStand, true);
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    //判断行动单位是否有眩晕
    private bool OffsetDizzyState(FightCardData dizzyUnit)
    {
        if (dizzyUnit.CardState.Stunned > 0)
        {
            dizzyUnit.isActionDone = true;
            //Debug.Log("---处于眩晕");
            dizzyUnit.CardState.Stunned--;
            if (dizzyUnit.CardState.Stunned <= 0)
            {
                dizzyUnit.CardState.Stunned = 0;
                FightForManagerForStart.instance.DestroySateIcon(dizzyUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_dizzy, true);
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    //判断行动单位是否有内助
    private bool OffsetNeiZhuState(FightCardData attackUnit)
    {
        if (attackUnit.CardState.Neizhu > 0)
        {
            attackUnit.CardState.Neizhu--;
            if (attackUnit.CardState.Neizhu <= 0)
            {
                attackUnit.CardState.Neizhu = 0;
                FightForManagerForStart.instance.DestroySateIcon(attackUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_neizhu, false);
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    //判断行动单位是否有神助
    private bool OffsetShenZhuState(FightCardData attackUnit)
    {
        if (attackUnit.CardState.ShenZhu > 0)
        {
            attackUnit.CardState.ShenZhu--;
            if (attackUnit.CardState.ShenZhu <= 0)
            {
                attackUnit.CardState.ShenZhu = 0;
                FightForManagerForStart.instance.DestroySateIcon(attackUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_shenzhu, false);
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    //触发眩晕
    private void TakeOneUnitDizzed(FightCardData attackedUnit, int prob)
    {
        if (attackedUnit.cardType == 0 && TakeSpecialAttack(prob))
        {
            if (attackedUnit.CardState.Stunned <= 0)
            {
                FightForManagerForStart.instance.CreateSateIcon(attackedUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_dizzy, true);
            }
            attackedUnit.CardState.Stunned++;
        }
    }

    //触发禁锢
    private void TakeToImprisoned(FightCardData attackedUnit, int prob)
    {
        if (attackedUnit.cardType == 0 && TakeSpecialAttack(prob))
        {
            if (attackedUnit.CardState.Imprisoned <= 0)
            {
                FightForManagerForStart.instance.CreateSateIcon(attackedUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_imprisoned, true);
            }
            attackedUnit.CardState.Imprisoned += 1;
            ShowSpellTextObj(attackedUnit.cardObj, DataTable.GetStringText(11), true, true);
        }
    }

    //触发卸甲
    private void TakeToRemoveArmor(FightCardData attackedUnit, int prob)
    {
        if (attackedUnit.cardType == 0 && TakeSpecialAttack(prob))
        {
            if (attackedUnit.CardState.Unarmed <= 0)
            {
                FightForManagerForStart.instance.CreateSateIcon(attackedUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_removeArmor, true);
            }
            attackedUnit.CardState.Unarmed += 1;
        }
    }

    //触发流血
    private void TakeToBleed(FightCardData attackedUnit, int prob)
    {
        if (attackedUnit.cardType == 0 && TakeSpecialAttack(prob))
        {
            if (attackedUnit.CardState.Bleed <= 0)
            {
                FightForManagerForStart.instance.CreateSateIcon(attackedUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_bleed, true);
            }
            attackedUnit.CardState.Bleed++;
            ShowSpellTextObj(attackedUnit.cardObj, DataTable.GetStringText(16), true, true);
        }
    }

    //触发中毒
    private void TakeToPoisoned(FightCardData attackedUnit, int prob)
    {
        if (attackedUnit.cardType == 0 && TakeSpecialAttack(prob))
        {
            if (attackedUnit.CardState.Poison <= 0)
            {
                FightForManagerForStart.instance.CreateSateIcon(attackedUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_poisoned, true);
            }
            attackedUnit.CardState.Poison++;
            ShowSpellTextObj(attackedUnit.cardObj, DataTable.GetStringText(12), true, true);
        }
    }

    //触发灼烧
    private void TakeToBurn(FightCardData attackedUnit, int prob)
    {
        if (attackedUnit.cardType == 0 && TakeSpecialAttack(prob))
        {
            if (attackedUnit.CardState.Burn <= 0)
            {
                FightForManagerForStart.instance.CreateSateIcon(attackedUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_burned, true);
            }
            attackedUnit.CardState.Burn++;
            ShowSpellTextObj(attackedUnit.cardObj, DataTable.GetStringText(20), true, true);
        }
    }

    //触发怯战
    private void TakeToCowardly(FightCardData attackedUnit, int prob)
    {
        if (attackedUnit.cardType == 0 && TakeSpecialAttack(prob))
        {
            if (attackedUnit.CardState.Cowardly <= 0)
            {
                FightForManagerForStart.instance.CreateSateIcon(attackedUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_cowardly, true);
            }
            attackedUnit.CardState.Cowardly += 1;
            ShowSpellTextObj(attackedUnit.cardObj, DataTable.GetStringText(21), true, true);
        }
    }

    //灼烧触发(暂无上限数值)
    private void BurningFightUnit(FightCardData cardData)
    {
        ShowSpellTextObj(cardData.cardObj, DataTable.GetStringText(20), true, true);

        if (cardData.CardState.Invincible <= 0)
        {
            int cutHpNum = (int)(DataTable.GetGameValue(118) / 100f * cardData.Hp.Max);
            cutHpNum = AddOrCutShieldValue(cutHpNum, cardData, false);
            cardData.Hp.Add(-cutHpNum);
            AttackedAnimShow(cardData, cutHpNum, false);
        }

        cardData.CardState.Burn--;
        PlayAudioForSecondClip(87, 0);
        if (cardData.CardState.Burn <= 0)
        {
            cardData.CardState.Burn = 0;
            FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_burned, true);
        }
    }

    /// <summary>
    /// 辅佐添加的防护盾增减值
    /// </summary>
    /// <param name="damage"></param>
    /// <param name="attackUnit"></param>
    /// <param name="isAdd"></param>
    /// <returns></returns>
    private int AddOrCutShieldValue(int damage, FightCardData attackUnit, bool isAdd)
    {
        int finalDamage = damage;
        if (attackUnit.cardType == 0)
        {
            if (isAdd)
            {
                if (attackUnit.CardState.ExtendedHp <= 0)
                {
                    FightForManagerForStart.instance.CreateSateIcon(attackUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_shield, true);
                }
                attackUnit.CardState.ExtendedHp += damage;
                attackUnit.CardState.ExtendedHp = Mathf.Min(attackUnit.CardState.ExtendedHp, DataTable.GetGameValue(119));
                float fadeFlo = Mathf.Max(0.3f, attackUnit.CardState.ExtendedHp / (float)DataTable.GetGameValue(119));
                attackUnit.cardObj.transform.Find(StringNameStatic.StateIconPath_shield + "Din").GetComponent<Image>().color = new Color(1, 1, 1, fadeFlo);
            }
            else
            {
                if (damage > 0)
                {
                    if (damage < attackUnit.CardState.ExtendedHp)
                    {
                        attackUnit.CardState.ExtendedHp -= damage;
                        finalDamage = 0;
                        float fadeFlo = Mathf.Max(0.3f, attackUnit.CardState.ExtendedHp / (float)DataTable.GetGameValue(119));
                        attackUnit.cardObj.transform.Find(StringNameStatic.StateIconPath_shield + "Din").GetComponent<Image>().color = new Color(1, 1, 1, fadeFlo);
                    }
                    else
                    {
                        if (attackUnit.CardState.ExtendedHp > 0)
                        {
                            finalDamage = damage - attackUnit.CardState.ExtendedHp;
                            attackUnit.CardState.ExtendedHp = 0;
                            FightForManagerForStart.instance.DestroySateIcon(attackUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_shield, true);
                        }
                    }
                }
            }
        }
        return finalDamage;
    }

    /// <summary>
    /// 技能特效表现
    /// </summary>
    /// <param name="attackedUnit"></param>
    /// <param name="isPuGong"></param>
    /// <param name="effectName"></param>
    /// <returns></returns>
    public GameObject AttackToEffectShow(FightCardData attackedUnit, bool isPuGong, string effectName = "")
    {
        GameObject effectObj = new GameObject();
        PlayerDataForGame.garbageStationObjs.Add(effectObj);

        if (isPuGong)
        {
            effectObj = EffectsPoolingControl.instance.GetEffectToFight(Effect.Basic0A, 0.5f, attackedUnit.cardObj);
            effectObj.transform.localEulerAngles = new Vector3(0, 0, Random.Range(0, 360));
        }
        else
        {
            effectObj = EffectsPoolingControl.instance.GetEffectToFight(effectName, 1f, attackedUnit.cardObj);
        }

        if (indexAttackType != 0) //非普通攻击
        {
            effectObj.transform.localScale = new Vector3(effectObj.transform.localScale.x * 1.5f, effectObj.transform.localScale.y * 1.5f, effectObj.transform.localScale.z * 1.5f);
        }
        else
        {
            if (effectObj.transform.localScale.x != 1f)
            {
                effectObj.transform.localScale = new Vector3(1, 1, 1);
            }
        }
        return effectObj;
    }

    /// <summary>
    /// 被击表现
    /// </summary>
    /// <param name="attackedUnit"></param>
    /// <param name="cutHpNum"></param>
    /// <param name="isAdd"></param>
    public void AttackedAnimShow(FightCardData attackedUnit, int cutHpNum, bool isAdd)
    {
        if (cutHpNum < 0)
        {
            isAdd = true;
        }
        GameObject effectObj = new GameObject();
        PlayerDataForGame.garbageStationObjs.Add(effectObj);

        effectObj = EffectsPoolingControl.instance.GetEffectToFight(Effect.DropBlood, 1.5f, attackedUnit.cardObj);
        if (isAdd)
        {
            effectObj.GetComponentInChildren<Text>().text = "+" + Mathf.Abs(cutHpNum);
            effectObj.GetComponentInChildren<Text>().color = ColorDataStatic.huiFu_green;
        }
        else
        {
            effectObj.GetComponentInChildren<Text>().text = "-" + Mathf.Abs(cutHpNum);
            effectObj.GetComponentInChildren<Text>().color = Color.red;
            if (cutHpNum > 0)
            {
                attackedUnit.cardObj.transform.DOShakePosition(0.3f, new Vector3(10, 20, 10));
            }
            if (indexAttackType != 0)
            {
                effectObj.transform.localScale = new Vector3(effectObj.transform.localScale.x * 1.3f, effectObj.transform.localScale.y * 1.3f, effectObj.transform.localScale.z * 1.3f);
                Vector3 vec3 = fightBackForShake.transform.position;
                if (indexAttackType == 1)//会心
                {
                    ShowSpellTextObj(attackedUnit.cardObj, DataTable.GetStringText(22), true, true);
                    fightBackForShake.transform.DOShakePosition(0.25f, doShakeIntensity).OnComplete(delegate ()
                    {
                        fightBackForShake.transform.position = vec3;
                    });
                }
                else //暴击
                {
                    ShowSpellTextObj(attackedUnit.cardObj, DataTable.GetStringText(23), true, true);
                    fightBackForShake.transform.DOShakePosition(0.25f, doShakeIntensity).OnComplete(delegate ()
                    {
                        fightBackForShake.transform.position = vec3;
                    });
                }
            }
            else
            {
                if (effectObj.transform.localScale.x != 1f)
                {
                    effectObj.transform.localScale = new Vector3(1, 1, 1);
                }
            }
        }

        //更新血条
        UpdateUnitHpShow(attackedUnit);

        InitCardAfterFightedState(attackedUnit);
    }

    //特殊单位普攻特效
    private void SpecilSkillNeedPuGongFun(FightCardData attackedUnit)
    {
        if (isNeedToAttack)
        {
            PlayAudioForSecondClip(0, 0);
            AttackToEffectShow(attackedUnit, true);
        }
    }

    //更新血条显示
    public void UpdateUnitHpShow(FightCardData updateUnit)
    {
        if (updateUnit.Hp <= 0)
        {
            updateUnit.Hp.Set(0);
            if (updateUnit.cardType != 522)
            {
                updateUnit.cardObj.transform.GetChild(9).gameObject.SetActive(true);
            }
            else
            {
                updateUnit.cardObj.transform.GetChild(4).gameObject.SetActive(true);
            }
        }
        updateUnit.cardObj.transform.GetChild(2).GetComponent<Image>().fillAmount = 1f - updateUnit.Hp.Rate();
    }

    /// <summary>
    /// 技能文字表现
    /// </summary>
    /// <param name="ui"></param>
    /// <param name="showTextName"></param>
    /// <param name="isHorizontal"></param>
    /// <param name="isRed"></param>
    public void ShowSpellTextObj(WarGameCardUi ui, string showTextName, bool isHorizontal, bool isRed = true)
    {
        GameObject effectObj = new GameObject();
        PlayerDataForGame.garbageStationObjs.Add(effectObj);

        if (isHorizontal)
        {
            Transform go = ui.transform.Find("spellTextH(Clone)");
            if (go != null)
            {
                go.gameObject.SetActive(false);
            }
            effectObj = EffectsPoolingControl.instance.GetEffectToFight(Effect.SpellTextH, 1.5f, ui);
            effectObj.GetComponentInChildren<Text>().text = showTextName;
            effectObj.GetComponentInChildren<Text>().color = isRed ? Color.red : ColorDataStatic.huiFu_green;
        }
        else
        {
            Transform go = ui.transform.Find("spellTextV(Clone)");
            if (go != null)
            {
                go.gameObject.SetActive(false);
            }
            effectObj = EffectsPoolingControl.instance.GetEffectToFight(Effect.SpellTextV, 1.5f, ui);
            effectObj.GetComponentsInChildren<Image>()[1].sprite = Resources.Load("Image/battle/" + showTextName, typeof(Sprite)) as Sprite;
        }
    }

    /// <summary>
    /// 战斗结束上阵卡牌回复血量消除相关状态
    /// </summary>
    private void CollectiveRecoveryHp()
    {
        //尝试消除统帅火焰
        if (tongShuaiBurnRoundPy != -1)
        {
            for (int i = 0; i < GoalGfSetFireRound[tongShuaiBurnRoundPy].Length; i++)
            {
                Transform obj = FightForManagerForStart.instance.enemyCardsPos[GoalGfSetFireRound[tongShuaiBurnRoundPy][i]].transform.Find(StringNameStatic.StateIconPath_burned);
                if (obj != null)
                    Destroy(obj.gameObject);
            }
            tongShuaiBurnRoundPy = -1;
        }
        if (tongShuaiBurnRoundEm != -1)
        {
            for (int i = 0; i < GoalGfSetFireRound[tongShuaiBurnRoundEm].Length; i++)
            {
                Transform obj = FightForManagerForStart.instance.playerCardsPos[GoalGfSetFireRound[tongShuaiBurnRoundEm][i]].transform.Find(StringNameStatic.StateIconPath_burned);
                if (obj != null)
                    Destroy(obj.gameObject);
            }
            tongShuaiBurnRoundEm = -1;
        }

        FightCardData fightCardData;
        //我方
        for (int i = 0; i < FightForManagerForStart.instance.playerFightCardsDatas.Length; i++)
        {
            fightCardData = FightForManagerForStart.instance.playerFightCardsDatas[i];
            if (i != 17 && fightCardData != null && fightCardData.Hp > 0)
            {
                int addtionNums = (int)(fightCardData.Hp.Max * fightCardData.hpr / 100f);
                fightCardData.Hp.Add(addtionNums);
                ShowSpellTextObj(fightCardData.cardObj, DataTable.GetStringText(15), true, false);
                AttackedAnimShow(fightCardData, addtionNums, true);
                if (fightCardData.cardType == 0)
                {
                    OnClearCardStateUpdate(fightCardData);  //消除所有状态
                }
            }
        }
    }

    //播放特殊音效
    public void PlayAudioForSecondClip(int clipIndex, float delayedTime)
    {
        audioSource.clip = FightForManagerForStart.instance.audioClipsFightEffect[clipIndex];
        audioSource.volume = FightForManagerForStart.instance.audioVolumeFightEffect[clipIndex];
        if (AudioController0.instance.ChangeAudioClip(audioSource.clip,audioSource.volume))
        {
            AudioController0.instance.PlayAudioSource(0);
        }
        else
        {
            AudioController0.instance.audioSource.volume = AudioController0.instance.audioSource.volume * 0.75f;
            if (!GamePref.PrefMusicPlay)
                return;
            audioSource.PlayDelayed(delayedTime);
        }
    }


    //计算是否触发特殊攻击状态
    private bool TakeSpecialAttack(int odds)
    {
        int num = Random.Range(1, 101);
        if (num <= odds)
            return true;
        else
            return false;
    }

    //从list中随机抽取对应数量的元素
    private List<int> BackRandsList(List<int> parentList, int childsCount)
    {
        List<int> randsList = new List<int>();

        if (parentList.Count <= childsCount)
        {
            randsList = parentList;//LoadJsonFile.DeepClone<int>(parentList);
        }
        else
        {
            for (int i = 0; i < childsCount; i++)
            {
                int rand = parentList[Random.Range(0, parentList.Count)];
                while (randsList.IndexOf(rand) != -1)
                {
                    rand = parentList[Random.Range(0, parentList.Count)];
                }
                randsList.Add(rand);
            }
        }
        return randsList;
    }

    /// <summary>
    /// 全屏技能特效展示，0会心一击
    /// </summary>
    [SerializeField]
    GameObject[] fullScreenEffectObjs;

    //关闭所有开启的全屏特技
    private void CloseAllFullScreenEffect()
    {
        for (int i = 0; i < fullScreenEffectObjs.Length; i++)
        {
            if (fullScreenEffectObjs[i].activeSelf)
            {
                fullScreenEffectObjs[i].SetActive(false);
            }
        }
    }

    //全屏技能特效展示,会心一击,羁绊
    private void ShowAllScreenFightEffect(FullScreenEffectName fullScreenEffectName, int indexResPic = 0)
    {
        GameObject effectObj = fullScreenEffectObjs[(int)fullScreenEffectName];
        if (effectObj.activeSelf)
        {
            effectObj.SetActive(false);
        }
        switch (fullScreenEffectName)
        {
            case FullScreenEffectName.HuiXinEffect:
                PlayAudioForSecondClip(92, 0);
                break;
            case FullScreenEffectName.JiBanEffect:
                effectObj.transform.GetChild(0).GetComponent<Image>().sprite = Resources.Load("Image/JiBan/art/" + indexResPic, typeof(Sprite)) as Sprite;
                effectObj.transform.GetChild(0).GetChild(1).GetComponent<Image>().sprite = Resources.Load("Image/JiBan/name_h/" + indexResPic, typeof(Sprite)) as Sprite;
                PlayAudioForSecondClip(100, 0);
                break;
            default:
                break;
        }
        effectObj.SetActive(true);
    }

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

    float roundTime = 1.5f;   //回合开始倒计时
    float timer = 0;

    [SerializeField]
    Slider roundTimeSlider; //回合时间条
    bool isFirstRound;

    //一场战斗开始前数据初始化
    public void InitStartFight()
    {
        isRoundBegin = false;
        recordWinner = 0;
        roundNums = 0;
        CloseAllFullScreenEffect();
        timer = 0;
        startFightBtn.GetComponent<Button>().interactable = true;
        startFightBtn.GetComponent<Animator>().SetBool("isShow", true);
        isFirstRound = true;
        roundTimeSlider.gameObject.SetActive(false);
        StartAddSomeState();
    }

    //战斗开始前的属性附加
    private void StartAddSomeState()
    {
        FightCardData fightCardData;
        for (int i = 0; i < FightForManagerForStart.instance.playerFightCardsDatas.Length; i++)
        {
            fightCardData = FightForManagerForStart.instance.playerFightCardsDatas[i];
            if (i != 17 && fightCardData != null && fightCardData.cardType == 0 && fightCardData.Hp > 0)
            {
                switch (DataTable.Hero[fightCardData.cardId].MilitaryUnitTableId)
                {
                    case 4://盾兵
                        if (fightCardData.CardState.Shield <= 0)
                        {
                            FightForManagerForStart.instance.CreateSateIcon(fightCardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_withStand, true);
                        }
                        fightCardData.CardState.Shield = 1;
                        break;
                    default:
                        break;
                }
            }
        }
    }

    //按钮方法-开始战斗
    public void OnClickForRoundBegin()
    {
        if (!isRoundBegin)
        {
            //隐藏第二个提示
            FightForManagerForStart.instance.ChangeGuideForFight(1);
            //播放战中弹幕
            FightForManagerForStart.instance.PlayZhanZhongBarrage();

            PlayAudioForSecondClip(93, 0);

            isFirstRound = false;
            RoundBeginFun();
        }
    }

    //实时更新等待条
    private void LateUpdate()
    {
        if (!isRoundBegin && !isFirstRound && autoFightTog.isOn)
        {
            timer += Time.deltaTime;
            if (timer >= roundTime)
            {
                RoundBeginFun();
                timer = 0;
            }
            roundTimeSlider.value = 1 - timer / roundTime;
        }
    }

    //锁定玩家上场卡牌
    private void LockFightCardState()
    {
        for (int i = 0; i < FightForManagerForStart.instance.playerFightCardsDatas.Length; i++)
        {
            if (i != 17 && FightForManagerForStart.instance.playerFightCardsDatas[i] != null)
            {
                if (!FightForManagerForStart.instance.playerFightCardsDatas[i].cardObj.GetComponent<CardDragForStart>().isFightCard)
                {
                    FightForManagerForStart.instance.playerFightCardsDatas[i].cardObj.GetComponent<CardDragForStart>().isFightCard = true;
                    FightForManagerForStart.instance.playerFightCardsDatas[i].cardObj.transform.GetChild(8).gameObject.SetActive(false);
                }
            }
        }
    }

    //回合开始方法
    private void RoundBeginFun()
    {
        timer = 0;
        isRoundBegin = true;
        roundTimeSlider.fillRect.GetComponent<Image>().color = Color.white;
        roundTimeSlider.gameObject.SetActive(false);
        startFightBtn.GetComponent<Button>().interactable = false;
        startFightBtn.GetComponent<Animator>().SetBool("isShow", false);
        LockFightCardState();
        StartCoroutine(LiteForStartRound());
    }

    //等待战鼓消失动画结束后,回合开始数据重置
    IEnumerator LiteForStartRound()
    {
        yield return new WaitForSeconds(1f);
        //FightForManagerForStart.instance.UpdateCardDataBeforeRound();
        roundNums++;
        targetIndex = -1;
        fightUnitIndex = 0;
        isPlayerRound = true;
        timerForFight = 0;
        stateOfFight = StateOfFight.ReadyForFight;
        float updateStateTime = 0;  //刷新状态需等待的时间
        updateStateTime = UpdateCardDataBeforeRound();
        yield return new WaitForSeconds(updateStateTime);

        //羁绊部分的战前附加
        yield return StartCoroutine(InitJiBanForStartFight());

        CardMoveToFight();
    }

    /// <summary>
    /// 战斗开始羁绊数据附加到卡牌上
    /// </summary>
    IEnumerator InitJiBanForStartFight()
    {
        //玩家部分
        foreach (var item in playerJiBanAllTypes)
        {
            if (item.Value.IsActive)
            {
                //Debug.Log("触发羁绊： " + item.Value.jiBanIndex);
                ShowAllScreenFightEffect(FullScreenEffectName.JiBanEffect, item.Value.JiBanId);
                yield return new WaitForSeconds(1f);
                JiBanAddStateForCard(item.Value, true);
                yield return new WaitForSeconds(1f);
            }
        }
        //敌人部分
        foreach (var item in enemyJiBanAllTypes)
        {
            if (item.Value.IsActive)
            {
                //Debug.Log("敌方触发羁绊： " + item.Value.jiBanIndex);
                ShowAllScreenFightEffect(FullScreenEffectName.JiBanEffect, item.Value.JiBanId);
                yield return new WaitForSeconds(1f);
                JiBanAddStateForCard(item.Value, false);
                yield return new WaitForSeconds(1f);
            }
        }
    }

    //给卡牌上附加羁绊属性
    private void JiBanAddStateForCard(JiBanActivedClass jiBanActivedClass, bool isPlayer)
    {
        FightCardData fightCardData;
        FightCardData[] cardDatas = isPlayer ? FightForManagerForStart.instance.playerFightCardsDatas : FightForManagerForStart.instance.enemyFightCardsDatas;

        switch ((JiBanSkillName)jiBanActivedClass.JiBanId)
        {
            case JiBanSkillName.TaoYuanJieYi:
                //30%概率分别为羁绊武将增加1层【神助】
                for (int i = 0; i < jiBanActivedClass.List.Count; i++)
                {
                    for (int j = 0; j < jiBanActivedClass.List[i].Cards.Count; j++)
                    {
                        fightCardData = jiBanActivedClass.List[i].Cards[j];
                        if (fightCardData != null && fightCardData.Hp > 0)
                        {
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                            if (TakeSpecialAttack(DataTable.GetGameValue(134)))
                            {
                                if (fightCardData.CardState.ShenZhu <= 0)
                                {
                                    FightForManagerForStart.instance.CreateSateIcon(fightCardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_shenzhu, false);
                                }
                                fightCardData.CardState.ShenZhu++;
                            }
                        }
                    }
                }
                break;
            case JiBanSkillName.WuHuShangJiang:
                //50%概率分别为羁绊武将增加1层【内助】
                for (int i = 0; i < jiBanActivedClass.List.Count; i++)
                {
                    for (int j = 0; j < jiBanActivedClass.List[i].Cards.Count; j++)
                    {
                        fightCardData = jiBanActivedClass.List[i].Cards[j];
                        if (fightCardData != null && fightCardData.Hp > 0)
                        {
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                            if (TakeSpecialAttack(DataTable.GetGameValue(135)))
                            {
                                if (fightCardData.CardState.Neizhu <= 0)
                                {
                                    FightForManagerForStart.instance.CreateSateIcon(fightCardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_neizhu, false);
                                }
                                fightCardData.CardState.Neizhu++;
                            }
                        }
                    }
                }
                break;
            case JiBanSkillName.WoLongFengChu:
                //20%概率分别为统御（兵种系）武将增加1层【内助】  统御11
                for (int i = 0; i < cardDatas.Length; i++)
                {
                    fightCardData = cardDatas[i];
                    if (fightCardData != null && fightCardData.cardType == 0 && fightCardData.Hp > 0)
                    {
                        var armed = MilitaryInfo.GetInfo(fightCardData.cardId).ArmedType;
                        if (armed == 11)
                        {
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                            if (TakeSpecialAttack(DataTable.GetGameValue(136)))
                            {
                                if (fightCardData.CardState.Neizhu <= 0)
                                {
                                    FightForManagerForStart.instance.CreateSateIcon(fightCardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_neizhu, false);
                                }
                                fightCardData.CardState.Neizhu++;
                            }
                        }
                    }
                }
                break;
            case JiBanSkillName.HuChiELai:
                //30 % 概率分别为羁绊武将增加1层【护盾】
                for (int i = 0; i < jiBanActivedClass.List.Count; i++)
                {
                    for (int j = 0; j < jiBanActivedClass.List[i].Cards.Count; j++)
                    {
                        fightCardData = jiBanActivedClass.List[i].Cards[j];
                        if (fightCardData != null && fightCardData.Hp > 0)
                        {
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                            if (TakeSpecialAttack(DataTable.GetGameValue(137)))
                            {
                                if (fightCardData.CardState.Shield <= 0)
                                {
                                    FightForManagerForStart.instance.CreateSateIcon(fightCardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_withStand, true);
                                }
                                fightCardData.CardState.Shield++;
                            }
                        }
                    }
                }
                break;
            case JiBanSkillName.WuZiLiangJiang:
                //40%概率分别为羁绊武将增加1层【内助】
                for (int i = 0; i < jiBanActivedClass.List.Count; i++)
                {
                    for (int j = 0; j < jiBanActivedClass.List[i].Cards.Count; j++)
                    {
                        fightCardData = jiBanActivedClass.List[i].Cards[j];
                        if (fightCardData != null && fightCardData.Hp > 0)
                        {
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                            if (TakeSpecialAttack(DataTable.GetGameValue(138)))
                            {
                                if (fightCardData.CardState.Neizhu <= 0)
                                {
                                    FightForManagerForStart.instance.CreateSateIcon(fightCardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_neizhu, false);
                                }
                                fightCardData.CardState.Neizhu++;
                            }
                        }
                    }
                }
                break;
            case JiBanSkillName.WeiWuMouShi:
                //50%概率分别为干扰（兵种系）武将增加1层【内助】 干扰系12
                for (int i = 0; i < cardDatas.Length; i++)
                {
                    fightCardData = cardDatas[i];
                    if (fightCardData != null && fightCardData.cardType == 0 && fightCardData.Hp > 0)
                    {
                        var armed = MilitaryInfo.GetInfo(fightCardData.cardId).ArmedType;
                        if (armed == 12)
                        {
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                            if (TakeSpecialAttack(DataTable.GetGameValue(139)))
                            {
                                if (fightCardData.CardState.Neizhu <= 0)
                                {
                                    FightForManagerForStart.instance.CreateSateIcon(fightCardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_neizhu, false);
                                }
                                fightCardData.CardState.Neizhu++;
                            }
                        }
                    }
                }
                break;
            case JiBanSkillName.HuJuJiangDong:
                //20%概率分别为吴国（阵营）武将增加1层【内助】 吴国3
                for (int i = 0; i < cardDatas.Length; i++)
                {
                    fightCardData = cardDatas[i];
                    if (fightCardData != null && fightCardData.cardType == 0 && fightCardData.Hp > 0)
                    {
                        var armed = MilitaryInfo.GetInfo(fightCardData.cardId).ArmedType;
                        if (armed == 3)
                        {
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                            if (TakeSpecialAttack(DataTable.GetGameValue(140)))
                            {
                                if (fightCardData.CardState.Neizhu <= 0)
                                {
                                    FightForManagerForStart.instance.CreateSateIcon(fightCardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_neizhu, false);
                                }
                                fightCardData.CardState.Neizhu++;
                            }
                        }
                    }
                }
                break;
            case JiBanSkillName.ShuiShiDuDu:
                //40%概率分别为战船（兵种系）武将增加1层【内助】 战船8
                for (int i = 0; i < cardDatas.Length; i++)
                {
                    fightCardData = cardDatas[i];
                    if (fightCardData != null && fightCardData.cardType == 0 && fightCardData.Hp > 0)
                    {
                        if (DataTable.Hero[fightCardData.cardId].MilitaryUnitTableId == 8)
                        {
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                            if (TakeSpecialAttack(DataTable.GetGameValue(141)))
                            {
                                if (fightCardData.CardState.Neizhu <= 0)
                                {
                                    FightForManagerForStart.instance.CreateSateIcon(fightCardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_neizhu, false);
                                }
                                fightCardData.CardState.Neizhu++;
                            }
                        }
                    }
                }
                break;
            case JiBanSkillName.TianZuoZhiHe:
                //40%概率分别为羁绊武将增加1层【内助】
                for (int i = 0; i < jiBanActivedClass.List.Count; i++)
                {
                    for (int j = 0; j < jiBanActivedClass.List[i].Cards.Count; j++)
                    {
                        fightCardData = jiBanActivedClass.List[i].Cards[j];
                        if (fightCardData != null && fightCardData.Hp > 0)
                        {
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                            if (TakeSpecialAttack(DataTable.GetGameValue(142)))
                            {
                                if (fightCardData.CardState.Neizhu <= 0)
                                {
                                    FightForManagerForStart.instance.CreateSateIcon(fightCardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_neizhu, false);
                                }
                                fightCardData.CardState.Neizhu++;
                            }
                        }
                    }
                }
                break;
            case JiBanSkillName.HeBeiSiTingZhu:
                //30%概率分别为羁绊武将增加1层【护盾】
                for (int i = 0; i < jiBanActivedClass.List.Count; i++)
                {
                    for (int j = 0; j < jiBanActivedClass.List[i].Cards.Count; j++)
                    {
                        fightCardData = jiBanActivedClass.List[i].Cards[j];
                        if (fightCardData != null && fightCardData.Hp > 0)
                        {
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                            if (TakeSpecialAttack(DataTable.GetGameValue(143)))
                            {
                                if (fightCardData.CardState.Shield <= 0)
                                {
                                    FightForManagerForStart.instance.CreateSateIcon(fightCardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_withStand, true);
                                }
                                fightCardData.CardState.Shield++;
                            }
                        }
                    }
                }
                break;
            case JiBanSkillName.JueShiWuShuang:
                //50%概率分别为羁绊武将增加1层【神助】
                for (int i = 0; i < jiBanActivedClass.List.Count; i++)
                {
                    for (int j = 0; j < jiBanActivedClass.List[i].Cards.Count; j++)
                    {
                        fightCardData = jiBanActivedClass.List[i].Cards[j];
                        if (fightCardData != null && fightCardData.Hp > 0)
                        {
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                            if (TakeSpecialAttack(DataTable.GetGameValue(144)))
                            {
                                if (fightCardData.CardState.ShenZhu <= 0)
                                {
                                    FightForManagerForStart.instance.CreateSateIcon(fightCardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_shenzhu, false);
                                }
                                fightCardData.CardState.ShenZhu++;
                            }
                        }
                    }
                }
                break;
            case JiBanSkillName.HanMoSanXian:
                //30%概率分别为羁绊武将增加1层【神助】
                for (int i = 0; i < jiBanActivedClass.List.Count; i++)
                {
                    for (int j = 0; j < jiBanActivedClass.List[i].Cards.Count; j++)
                    {
                        fightCardData = jiBanActivedClass.List[i].Cards[j];
                        if (fightCardData != null && fightCardData.Hp > 0)
                        {
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                            if (TakeSpecialAttack(DataTable.GetGameValue(145)))
                            {
                                if (fightCardData.CardState.ShenZhu <= 0)
                                {
                                    FightForManagerForStart.instance.CreateSateIcon(fightCardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_shenzhu, false);
                                }
                                fightCardData.CardState.ShenZhu++;
                            }
                        }
                    }
                }
                break;
            default:
                break;
        }
    }

    //回合开始状态等统一处理
    private float UpdateCardDataBeforeRound()
    {
        float waitTime = 1f;
        int maxRounds = 0;
        int nowRounds = 0;
        indexAttackType = 0;

        gunMuCards.Clear();
        gunShiCards.Clear();

        bool isHadTongShuai = false;    //是否有统帅

        for (int i = 0; i < FightForManagerForStart.instance.playerFightCardsDatas.Length; i++)
        {
            if (FightForManagerForStart.instance.playerFightCardsDatas[i] != null && FightForManagerForStart.instance.playerFightCardsDatas[i].Hp > 0)
            {
                FightForManagerForStart.instance.playerFightCardsDatas[i].isActionDone = false;
                if (FightForManagerForStart.instance.playerFightCardsDatas[i].cardType == 0)
                {
                    nowRounds = UpdateOneCardBeforeRound(FightForManagerForStart.instance.playerFightCardsDatas[i]);
                    //是否有统帅
                    if (!isHadTongShuai && FightForManagerForStart.instance.playerFightCardsDatas[i].Hp > 0 &&
                        (DataTable.Hero[FightForManagerForStart.instance.playerFightCardsDatas[i].cardId].MilitaryUnitTableId == 32 ||
                        DataTable.Hero[FightForManagerForStart.instance.playerFightCardsDatas[i].cardId].MilitaryUnitTableId == 33))
                    {
                        isHadTongShuai = true;
                    }
                }
                else
                {
                    if (FightForManagerForStart.instance.playerFightCardsDatas[i].cardType == 2)
                    {
                        switch (FightForManagerForStart.instance.playerFightCardsDatas[i].cardId)
                        {
                            case 0:
                                //营寨回合开始回血
                                int addHp = (int)(DataTable.GetGameValue(120) / 100f * FightForManagerForStart.instance.playerFightCardsDatas[i].Hp.Max);
                                FightForManagerForStart.instance.playerFightCardsDatas[i].Hp.Add(addHp);
                                AttackedAnimShow(FightForManagerForStart.instance.playerFightCardsDatas[i], addHp, true);
                                break;
                            //case 17:
                            //    //迷雾阵迷雾动画开启
                            //    for (int j = 0; j < FightForManagerForStart.instance.playerFightCardsDatas[i].cardObj.transform.childCount; j++)
                            //    {
                            //        if (FightForManagerForStart.instance.playerFightCardsDatas[i].cardObj.transform.GetChild(j).name == StringNameStatic.StateIconPath_miWuZhenAddtion + "Din")
                            //        {
                            //            if (!FightForManagerForStart.instance.playerFightCardsDatas[i].cardObj.transform.GetChild(j).GetComponent<Animator>().enabled)
                            //            {
                            //                FightForManagerForStart.instance.playerFightCardsDatas[i].cardObj.transform.GetChild(j).GetComponent<Animator>().enabled = true;
                            //            }
                            //            else
                            //            {
                            //                break;
                            //            }
                            //        }
                            //    }
                            //    break;
                            default:
                                break;
                        }
                    }
                }
                //滚石滚木列表添加
                if (FightForManagerForStart.instance.playerFightCardsDatas[i].cardType == 3)
                {
                    if (FightForManagerForStart.instance.playerFightCardsDatas[i].cardId == 9)
                    {
                        gunShiCards.Add(FightForManagerForStart.instance.playerFightCardsDatas[i]);
                    }
                    if (FightForManagerForStart.instance.playerFightCardsDatas[i].cardId == 10)
                    {
                        gunMuCards.Add(FightForManagerForStart.instance.playerFightCardsDatas[i]);
                    }
                }
            }
            if (maxRounds < nowRounds)
                maxRounds = nowRounds;
        }

        if (!isHadTongShuai)
        {
            //尝试消除统帅火焰
            if (tongShuaiBurnRoundPy != -1)
            {
                for (int i = 0; i < GoalGfSetFireRound[tongShuaiBurnRoundPy].Length; i++)
                {
                    Transform obj = FightForManagerForStart.instance.enemyCardsPos[GoalGfSetFireRound[tongShuaiBurnRoundPy][i]].transform.Find(StringNameStatic.StateIconPath_burned);
                    if (obj != null)
                        Destroy(obj.gameObject);
                }
                tongShuaiBurnRoundPy = -1;
            }
        }
        else
        {
            isHadTongShuai = false;
        }

        for (int i = 0; i < FightForManagerForStart.instance.enemyFightCardsDatas.Length; i++)
        {
            if (FightForManagerForStart.instance.enemyFightCardsDatas[i] != null && FightForManagerForStart.instance.enemyFightCardsDatas[i].Hp > 0)
            {
                FightForManagerForStart.instance.enemyFightCardsDatas[i].isActionDone = false;
                if (FightForManagerForStart.instance.enemyFightCardsDatas[i].cardType == 0)
                {
                    nowRounds = UpdateOneCardBeforeRound(FightForManagerForStart.instance.enemyFightCardsDatas[i]);
                    //是否有统帅
                    if (!isHadTongShuai && FightForManagerForStart.instance.enemyFightCardsDatas[i].Hp > 0 &&
                        (DataTable.Hero[FightForManagerForStart.instance.enemyFightCardsDatas[i].cardId].MilitaryUnitTableId == 32 ||
                        DataTable.Hero[FightForManagerForStart.instance.enemyFightCardsDatas[i].cardId].MilitaryUnitTableId == 33))
                    {
                        isHadTongShuai = true;
                    }
                }
                else
                {
                    if (FightForManagerForStart.instance.enemyFightCardsDatas[i].cardType == 2)
                    {
                        switch (FightForManagerForStart.instance.enemyFightCardsDatas[i].cardId)
                        {
                            case 0:
                                //营寨回合开始回血
                                int addHp = (int)(DataTable.GetGameValue(120) / 100f * FightForManagerForStart.instance.enemyFightCardsDatas[i].Hp.Max);
                                FightForManagerForStart.instance.enemyFightCardsDatas[i].Hp.Add(addHp);
                                AttackedAnimShow(FightForManagerForStart.instance.enemyFightCardsDatas[i], addHp, true);
                                break;
                            case 17:
                                //迷雾阵迷雾动画开启
                                Transform tran = FightForManagerForStart.instance.enemyFightCardsDatas[i].cardObj.transform;
                                for (int j = 0; j < tran.childCount; j++)
                                {
                                    Transform tranChild = tran.GetChild(j);
                                    if (tranChild.name == StringNameStatic.StateIconPath_miWuZhenAddtion + "Din")
                                    {
                                        if (!tranChild.GetComponent<Animator>().enabled)
                                        {
                                            tranChild.GetComponent<Animator>().enabled = true;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                }
                                break;
                            case 18:
                                //迷雾阵迷雾动画开启
                                Transform tran0 = FightForManagerForStart.instance.enemyFightCardsDatas[i].cardObj.transform;
                                for (int j = 0; j < tran0.childCount; j++)
                                {
                                    Transform tranChild = tran0.GetChild(j);
                                    if (tranChild.name == StringNameStatic.StateIconPath_miWuZhenAddtion + "Din")
                                    {
                                        if (!tranChild.GetComponent<Animator>().enabled)
                                        {
                                            tranChild.GetComponent<Animator>().enabled = true;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
                //滚石滚木列表添加
                if (FightForManagerForStart.instance.enemyFightCardsDatas[i].cardType == 3)
                {
                    if (FightForManagerForStart.instance.enemyFightCardsDatas[i].cardId == 9)
                    {
                        gunShiCards.Add(FightForManagerForStart.instance.enemyFightCardsDatas[i]);
                    }
                    if (FightForManagerForStart.instance.enemyFightCardsDatas[i].cardId == 10)
                    {
                        gunMuCards.Add(FightForManagerForStart.instance.enemyFightCardsDatas[i]);
                    }
                }
            }
            if (maxRounds < nowRounds)
                maxRounds = nowRounds;
        }

        if (!isHadTongShuai)
        {
            //尝试消除统帅火焰
            if (tongShuaiBurnRoundEm != -1)
            {
                for (int i = 0; i < GoalGfSetFireRound[tongShuaiBurnRoundEm].Length; i++)
                {
                    Transform obj = FightForManagerForStart.instance.playerCardsPos[GoalGfSetFireRound[tongShuaiBurnRoundEm][i]].transform.Find(StringNameStatic.StateIconPath_burned);
                    if (obj != null)
                        Destroy(obj.gameObject);
                }
                tongShuaiBurnRoundEm = -1;
            }
        }

        return maxRounds * waitTime;
    }
    private int UpdateOneCardBeforeRound(FightCardData cardData)
    {
        int showRounds = 0;
        if (cardData.CardState.Poison > 0) //中毒触发
        {
            showRounds++;
            ShowSpellTextObj(cardData.cardObj, DataTable.GetStringText(12), true, true);
            PlayAudioForSecondClip(86, 0);

            if (cardData.CardState.Invincible <= 0)
            {
                int cutHpNum = (int)(DataTable.GetGameValue(121) / 100f * cardData.Hp.Max);
                cutHpNum = AddOrCutShieldValue(cutHpNum, cardData, false);
                cardData.Hp.Add(-cutHpNum);
                AttackedAnimShow(cardData, cutHpNum, false);
            }

            cardData.CardState.Poison--;
            if (cardData.CardState.Poison <= 0)
            {
                cardData.CardState.Poison = 0;
                FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_poisoned, true);
            }
        }
        return showRounds;
    }

    //回合结束更新卡牌特殊状态
    private void UpdateCardStateAfterRound()
    {
        //我方
        for (int i = 0; i < FightForManagerForStart.instance.playerFightCardsDatas.Length; i++)
        {
            if (i != 17 && FightForManagerForStart.instance.playerFightCardsDatas[i] != null)
            {
                if (FightForManagerForStart.instance.playerFightCardsDatas[i].Hp > 0)
                {
                    OnCardStateUpdate(FightForManagerForStart.instance.playerFightCardsDatas[i]);
                }
                if (FightForManagerForStart.instance.playerFightCardsDatas[i].Hp <= 0)
                {
                    if (FightForManagerForStart.instance.playerFightCardsDatas[i].cardType == 0)
                    {
                        //羁绊消除
                        FightForManagerForStart.instance.TryToActivatedBond(FightForManagerForStart.instance.playerFightCardsDatas[i], false);

                        switch (DataTable.Hero[FightForManagerForStart.instance.playerFightCardsDatas[i].cardId].MilitaryUnitTableId)
                        {
                            case 58: //铁骑阵亡
                                UpdateTieQiStateIconShow(FightForManagerForStart.instance.playerFightCardsDatas[i], false);
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        if (FightForManagerForStart.instance.playerFightCardsDatas[i].cardType == 2) //塔倒了
                        {
                            FightForManagerForStart.instance.CardGoIntoBattleProcess(FightForManagerForStart.instance.playerFightCardsDatas[i], i, FightForManagerForStart.instance.playerFightCardsDatas, false);
                        }
                    }
                    Destroy(FightForManagerForStart.instance.playerFightCardsDatas[i].cardObj);
                    FightForManagerForStart.instance.playerFightCardsDatas[i] = null;
                }
            }
        }
        //敌方
        for (int i = 0; i < FightForManagerForStart.instance.enemyFightCardsDatas.Length; i++)
        {
            if (i != 17 && FightForManagerForStart.instance.enemyFightCardsDatas[i] != null)
            {
                if (FightForManagerForStart.instance.enemyFightCardsDatas[i].Hp > 0)
                {
                    OnCardStateUpdate(FightForManagerForStart.instance.enemyFightCardsDatas[i]);
                }
                if (FightForManagerForStart.instance.enemyFightCardsDatas[i].Hp <= 0)
                {
                    if (FightForManagerForStart.instance.enemyFightCardsDatas[i].cardType == 0)
                    {
                        //羁绊消除
                        FightForManagerForStart.instance.TryToActivatedBond(FightForManagerForStart.instance.enemyFightCardsDatas[i], false);

                        switch (DataTable.Hero[FightForManagerForStart.instance.enemyFightCardsDatas[i].cardId].MilitaryUnitTableId)
                        {
                            case 58: //铁骑阵亡
                                UpdateTieQiStateIconShow(FightForManagerForStart.instance.enemyFightCardsDatas[i], false);
                                break;

                            default:
                                break;
                        }
                        OnClearCardStateUpdate(FightForManagerForStart.instance.enemyFightCardsDatas[i]);
                    }
                    else
                    {
                        if (FightForManagerForStart.instance.enemyFightCardsDatas[i].cardType == 2) //塔倒了
                        {
                            FightForManagerForStart.instance.CardGoIntoBattleProcess(FightForManagerForStart.instance.enemyFightCardsDatas[i], i, FightForManagerForStart.instance.enemyFightCardsDatas, false);
                        }
                    }
                    Destroy(FightForManagerForStart.instance.enemyFightCardsDatas[i].cardObj);
                    FightForManagerForStart.instance.enemyFightCardsDatas[i] = null;
                }
            }
        }
    }

    //消除卡牌所有状态图标特效等
    private void OnClearCardStateUpdate(FightCardData cardData)
    {
        try
        {
            if (cardData.CardState.Stunned > 0)          //眩晕状态
            {
                cardData.CardState.Stunned = 0;
                FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_dizzy, true);
            }
            if (cardData.CardState.Imprisoned > 0)     //禁锢状态
            {
                cardData.CardState.Imprisoned = 0;
                FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_imprisoned, true);
            }
            if (cardData.CardState.Bleed > 0)          //流血状态
            {
                cardData.CardState.Bleed = 0;
                FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_bleed, true);
            }
            if (cardData.CardState.Poison > 0)       //中毒状态
            {
                cardData.CardState.Poison = 0;
                FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_poisoned, true);
            }
            if (cardData.CardState.Burn > 0)         //灼烧触发
            {
                cardData.CardState.Burn = 0;
                FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_burned, true);
            }
            if (cardData.CardState.Unarmed > 0)    //卸甲状态
            {
                cardData.CardState.Unarmed = 0;
                FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_removeArmor, true);
            }
            if (cardData.CardState.Shield > 0)      //护盾状态
            {
                cardData.CardState.Shield = 0;
                FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_withStand, true);
            }
            if (cardData.CardState.Invincible > 0)     //无敌消减
            {
                cardData.CardState.Invincible = 0;
                FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_invincible, true);
            }
            if (cardData.CardState.DeathFight > 0)     //死战状态
            {
                cardData.CardState.DeathFight = 0;
                FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_deathFight, true);
            }
            if (cardData.CardState.Stimulate > 0)      //战意状态
            {
                cardData.CardState.Stimulate = 0;
                FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_willFight, true);
            }
            if (cardData.CardState.Neizhu > 0)         //内助状态
            {
                cardData.CardState.Neizhu = 0;
                FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_neizhu, false);
            }
            if (cardData.CardState.ShenZhu > 0)        //神助状态
            {
                cardData.CardState.ShenZhu = 0;
                FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_shenzhu, false);
            }
            if (cardData.CardState.Cowardly > 0)       //怯战状态
            {
                cardData.CardState.Cowardly = 0;
                FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_cowardly, true);
            }
            if (cardData.CardState.MiWuZhenAddOn > 0)    //隐蔽状态
            {
                cardData.CardState.MiWuZhenAddOn = 0;
                FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_miWuZhenAddtion, false);
            }
            if (cardData.CardState.ExtendedHp > 0)        //防护盾状态
            {
                cardData.CardState.ExtendedHp = 0;
                FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_shield, true);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }

    //回合结束单个卡牌状态刷新
    private void OnCardStateUpdate(FightCardData cardData)
    {
        try
        {
            if (cardData.CardState.Invincible > 0) //无敌消减
            {
                cardData.CardState.Invincible--;
                if (cardData.CardState.Invincible <= 0)
                {
                    cardData.CardState.Invincible = 0;
                    FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_invincible, true);
                }
            }
            if (cardData.CardState.Burn > 0) //灼烧触发
            {
                BurningFightUnit(cardData);
            }
            if (cardData.CardState.Bleed > 0) //流血状态
            {
                cardData.CardState.Bleed--;
                if (cardData.CardState.Bleed <= 0)
                {
                    cardData.CardState.Bleed = 0;
                    FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_bleed, true);
                }
            }
            if (cardData.CardState.Unarmed > 0) //卸甲状态
            {
                cardData.CardState.Unarmed--;
                if (cardData.CardState.Unarmed <= 0)
                {
                    cardData.CardState.Unarmed = 0;
                    FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_removeArmor, true);
                }
            }
            if (cardData.CardState.DeathFight > 0)    //死战状态
            {
                cardData.CardState.DeathFight--;
                if (cardData.CardState.DeathFight <= 0)
                {
                    cardData.CardState.DeathFight = 0;
                    FightForManagerForStart.instance.DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_deathFight, true);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }

    [SerializeField]
    float yuanChengShakeTimeToGo = 0.5f;
    [SerializeField]
    float yuanChengShakeTimeToBack = 0.5f;
    [SerializeField]
    float yuanChengShakeTime = 0.1f;

    //武将行动
    IEnumerator InitiativeHeroAction(FightCardData attackUnit, FightCardData[] cardsDatas)
    {
        float waitTime = 0;
        waitTime = BeforeFightDoThingFun(attackUnit);
        yield return new WaitForSeconds(waitTime);

        /////////前摇//////////
        targetIndex = FindOpponentIndex(attackUnit);  //锁定目标卡牌
        //近战跟远程选择不同的进攻方式
        if (attackUnit.combatType == 0)
        {
            MoveToFightWay1(attackUnit);
            yield return new WaitForSeconds(attackShakeTimeToGo);
        }
        else
        {
            MoveToFightWay0(attackUnit, yuanChengShakeTimeToGo);
            yield return new WaitForSeconds(yuanChengShakeTime);
        }
        /////////攻击//////////
        FightCardData attackedUnit = isPlayerRound ? FightForManagerForStart.instance.enemyFightCardsDatas[targetIndex] : FightForManagerForStart.instance.playerFightCardsDatas[targetIndex];

        yield return StartCoroutine(PuTongGongji(1f, attackUnit, attackedUnit, true));

        /////////后摇//////////
        if (attackUnit.combatType == 0)
        {
            CardBackToSelfPosFun();
            yield return new WaitForSeconds(attackShakeTimeToBack);
        }
        else
        {
            yield return new WaitForSeconds(yuanChengShakeTimeToBack);
        }
        ////////////////////
        NextCardDoThingFun();
    }

    //卡牌进入战斗
    private void CardMoveToFight()
    {
        FightCardData[] cardDatas;
        FightCardData attackUnit = new FightCardData();
        if (isPlayerRound)
        {
            attackUnit = FightForManagerForStart.instance.playerFightCardsDatas[fightUnitIndex];
            cardDatas = FightForManagerForStart.instance.playerFightCardsDatas;
        }
        else
        {
            attackUnit = FightForManagerForStart.instance.enemyFightCardsDatas[fightUnitIndex];
            cardDatas = FightForManagerForStart.instance.enemyFightCardsDatas;
        }
        //主动单位并且可行动
        if (attackUnit != null && attackUnit.activeUnit && !attackUnit.isActionDone && !OffsetDizzyState(attackUnit) && attackUnit.Hp > 0)
        {
            attackUnit.isActionDone = true;
            switch (attackUnit.cardType)
            {
                case 0:
                    StartCoroutine(InitiativeHeroAction(attackUnit, cardDatas));
                    break;
                case 2:
                    StartCoroutine(InitiativeTowerAction(attackUnit, cardDatas));
                    break;
                default:
                    break;
            }
        }
        else
        {
            NextCardDoThingFun();
        }
    }

    //刺客目标选择
    private int CiKeOpponentChoose(FightCardData attackUnit)
    {
        int index = -1;
        int maxDamage = 0;
        FightCardData[] fightCardDatas = attackUnit.isPlayerCard ? FightForManagerForStart.instance.enemyFightCardsDatas : FightForManagerForStart.instance.playerFightCardsDatas;
        for (int i = 0; i < fightCardDatas.Length; i++)
        {
            if (fightCardDatas[i] != null && fightCardDatas[i].cardType == 0 && fightCardDatas[i].Hp > 0)
            {
                if (fightCardDatas[i].combatType == 1)
                {
                    if (fightCardDatas[i].damage > maxDamage)
                    {
                        index = i;
                        maxDamage = fightCardDatas[i].damage;
                    }
                }
            }
        }
        return index;
    }

    //锁定目标卡牌
    private int FindOpponentIndex(FightCardData attackUnit)
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

        int index = 0;
        int arrIndex = fightUnitIndex % 5;
        if (attackUnit.isPlayerCard)
        {
            for (; index < AttackSelectionOrder[arrIndex].Length; index++)
            {
                if (FightForManagerForStart.instance.enemyFightCardsDatas[AttackSelectionOrder[arrIndex][index]] != null
                    && FightForManagerForStart.instance.enemyFightCardsDatas[AttackSelectionOrder[arrIndex][index]].Hp > 0)
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
                if (FightForManagerForStart.instance.playerFightCardsDatas[AttackSelectionOrder[arrIndex][index]] != null
                    && FightForManagerForStart.instance.playerFightCardsDatas[AttackSelectionOrder[arrIndex][index]].Hp > 0)
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

    //消除统帅火焰
    private void ClearTongShuaiBurnState()
    {
        if (tongShuaiBurnRoundPy != -1)
        {
            for (int i = 0; i < GoalGfSetFireRound[tongShuaiBurnRoundPy].Length; i++)
            {
                Transform obj = FightForManagerForStart.instance.enemyCardsPos[GoalGfSetFireRound[tongShuaiBurnRoundPy][i]].transform.Find(StringNameStatic.StateIconPath_burned);
                if (obj != null)
                    Destroy(obj.gameObject);
            }
            tongShuaiBurnRoundPy = -1;
        }
        if (tongShuaiBurnRoundEm != -1)
        {
            for (int i = 0; i < GoalGfSetFireRound[tongShuaiBurnRoundEm].Length; i++)
            {
                Transform obj = FightForManagerForStart.instance.playerCardsPos[GoalGfSetFireRound[tongShuaiBurnRoundEm][i]].transform.Find(StringNameStatic.StateIconPath_burned);
                if (obj != null)
                    Destroy(obj.gameObject);
            }
            tongShuaiBurnRoundEm = -1;
        }
    }

    //下一卡牌单位行动
    private void NextCardDoThingFun()
    {
        if (recordWinner != 0)
        {
            ClearTongShuaiBurnState();
            if (recordWinner == 1)
            {
                //Debug.Log("---击败敌方");
                UpdateCardStateAfterRound();

                StartCoroutine(BattleSettlementFun());
            }
            else
            {
                FightForManagerForStart.instance.InitEnemyCardForFight();
            }
        }
        else
        {
            if (!isPlayerRound) //敌方单位索引已进行过行动
            {
                fightUnitIndex++;
                if (fightUnitIndex == 17)
                {
                    fightUnitIndex++;
                }
                if (fightUnitIndex > 19)    //回合结束
                {
                    //Debug.Log("----回合结束");
                    stateOfFight = StateOfFight.ReadyForFight;
                    UpdateCardStateAfterRound();
                    isRoundBegin = false;
                    startFightBtn.GetComponent<Button>().interactable = true;
                    startFightBtn.GetComponent<Animator>().SetBool("isShow", true);
                    roundTimeSlider.gameObject.SetActive(autoFightTog.isOn);
                    roundTimeSlider.fillRect.GetComponent<Image>().color = Color.white;
                    roundTimeSlider.fillRect.GetComponent<Image>().DOColor(Color.red, roundTime);
                    return;
                }
            }
            isPlayerRound = !isPlayerRound;
            CardMoveToFight();
        }
    }

    //战前行动准备
    private float BeforeFightDoThingFun(FightCardData attackUnit)
    {
        float needAllTime = 0;
        needAllTime += ConfirmAttackStatus(attackUnit);
        return needAllTime;
    }

    //确认此次攻击状态
    private float ConfirmAttackStatus(FightCardData attackUnit)
    {
        float needTime = 0;
        indexAttackType = 0;
        if (attackUnit.CardState.ShenZhu <= 0 && attackUnit.CardState.Neizhu <= 0 && attackUnit.CardState.Cowardly > 0) //怯战无法使用暴击和会心一击
        {
            attackUnit.CardState.Cowardly--;
            ShowSpellTextObj(attackUnit.cardObj, DataTable.GetStringText(21), true, true);
            if (attackUnit.CardState.Cowardly <= 0)
            {
                attackUnit.CardState.Cowardly = 0;
                FightForManagerForStart.instance.DestroySateIcon(attackUnit.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_cowardly, true);
            }
        }
        else
        {
            var combat = HeroCombatInfo.GetInfo(attackUnit.cardId);
            int huixinPropNums = combat.RouseRatio + attackUnit.CardState.RouseUp;
            //是否有神助
            if (OffsetShenZhuState(attackUnit))
                huixinPropNums = 100;
            //是否触发会心一击
            if (TakeSpecialAttack(huixinPropNums))
            {
                indexAttackType = 1;
                needTime = 1.2f;
                ShowAllScreenFightEffect(FullScreenEffectName.HuiXinEffect);
            }
            else
            {
                int criPropNums = combat.CriticalRatio + attackUnit.CardState.CriticalUp;
                //是否有内助
                if (OffsetNeiZhuState(attackUnit))
                    criPropNums = 100;
                //是否触发暴击
                if (TakeSpecialAttack(criPropNums))
                {
                    indexAttackType = 2;
                }
            }
        }
        return needTime;
    }

    /// <summary>
    /// ///////////////////////////////////////////////////////
    /// </summary>
    [SerializeField]
    float towerFightTime0 = 0.3f;   //塔行动前摇时间
    [SerializeField]
    float towerFightTime1 = 0.5f;   //塔行动后摇时间
    //主动塔行动
    IEnumerator InitiativeTowerAction(FightCardData attackUnit, FightCardData[] cardsDatas)
    {
        MoveToFightWay0(attackUnit, towerFightTime0);
        yield return new WaitForSeconds(towerFightTime0 / 2);
        FightForManagerForStart.instance.ActiveTowerFight(attackUnit, cardsDatas);
        yield return new WaitForSeconds(towerFightTime1);
        //滚石滚木行动
        yield return StartCoroutine(GunMuGunShiSkill(gunMuCards, gunShiCards));
        //消除滚石滚木
        for (int i = 0; i < gunMuCards.Count; i++)
        {
            if (gunMuCards[i].Hp <= 0)
            {
                gunMuCards.Remove(gunMuCards[i]);
            }
        }
        for (int i = 0; i < gunShiCards.Count; i++)
        {
            if (gunShiCards[i].Hp <= 0)
            {
                gunShiCards.Remove(gunShiCards[i]);
            }
        }
        NextCardDoThingFun();
    }

    [SerializeField]
    GameObject fireUIObj;
    [SerializeField]
    GameObject boomUIObj;
    [SerializeField]
    GameObject gongKeUIObj;

    //战斗结算
    IEnumerator BattleSettlementFun()
    {
        boomUIObj.transform.position = FightForManagerForStart.instance.enemyCardsPos[17].transform.position;
        fireUIObj.transform.position = gongKeUIObj.transform.position = FightForManagerForStart.instance.enemyCardsPos[7].transform.position;
        yield return new WaitForSeconds(0.5f);
        PlayAudioForSecondClip(91, 0);
        boomUIObj.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        boomUIObj.SetActive(false);
        //欢呼声
        PlayAudioForSecondClip(90, 0);

        //火焰
        fireUIObj.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        gongKeUIObj.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        fireUIObj.SetActive(false);
        gongKeUIObj.SetActive(false);
        yield return new WaitForSeconds(1f);

        FightForManagerForStart.instance.InitEnemyCardForFight();
    }

    //攻击行动方式0-适用于-主动塔,远程兵
    private void MoveToFightWay0(FightCardData actionUnit, float readyTime)
    {
        actionUnit.cardObj.transform.DOScale(new Vector3(1.15f, 1.15f, 1), readyTime).SetAutoKill(false).OnComplete(delegate ()
        {
            actionUnit.cardObj.transform.DOPlayBackwards();
        });
    }

    //攻击行动方式1-适用于-近战
    private void MoveToFightWay1(FightCardData attackUnit)
    {
        if (fightUnitIndex != -1)
        {
            //stateOfFight = StateOfFight.MoveNow;
            if (isPlayerRound)
            {
                CardMoveToTargetPos(
                    attackUnit.cardObj,
                    new Vector3(
                        FightForManagerForStart.instance.enemyCardsPos[targetIndex].transform.position.x,
                        FightForManagerForStart.instance.enemyCardsPos[targetIndex].transform.position.y - FightForManagerForStart.instance.floDisY,
                        FightForManagerForStart.instance.enemyCardsPos[targetIndex].transform.position.z),
                    false,
                    true
                    );
            }
            else
            {
                CardMoveToTargetPos(
                    attackUnit.cardObj,
                    new Vector3(
                        FightForManagerForStart.instance.playerCardsPos[targetIndex].transform.position.x,
                        FightForManagerForStart.instance.playerCardsPos[targetIndex].transform.position.y + FightForManagerForStart.instance.floDisY,
                        FightForManagerForStart.instance.playerCardsPos[targetIndex].transform.position.z),
                    false,
                    false
                    );
            }
        }
    }

    //返回原始位置
    private void CardBackToSelfPosFun()
    {
        if (isPlayerRound)
        {
            CardMoveToTargetPos(FightForManagerForStart.instance.playerFightCardsDatas[fightUnitIndex].cardObj, FightForManagerForStart.instance.playerCardsPos[fightUnitIndex].transform.position, true, true);
        }
        else
        {
            CardMoveToTargetPos(FightForManagerForStart.instance.enemyFightCardsDatas[fightUnitIndex].cardObj, FightForManagerForStart.instance.enemyCardsPos[fightUnitIndex].transform.position, true, false);
        }
    }

    public float one;   //去
    public float two;   //回
    public float three; //等
    public float four;  //去
    public float posFloat;
    //近战移动方式
    private void CardMoveToTargetPos(WarGameCardUi cardObj, Vector3 targetPos, bool isBack, bool isPlayerCard)
    {
        if (isBack)
        {
            var cardBox = isPlayerCard
                ? FightForManagerForStart.instance.playerCardsBox
                : FightForManagerForStart.instance.enemyCardsBox;
            cardObj.transform.DOMove(targetPos, attackShakeTimeToBack).SetEase(Ease.Unset).OnComplete(() =>
                cardObj.transform.SetParent(cardBox));
            return;
        }

        cardObj.transform.SetParent(transferStation);

        Vector3 vec = new Vector3(
            targetPos.x,
            targetPos.y + (isPlayerRound ? (-1 * posFloat) : posFloat) * FightForManagerForStart.instance.oneDisY,
            targetPos.z
        );
        cardObj.transform.DOMove(targetPos, attackShakeTimeToGo * one).SetEase(Ease.Unset).OnComplete(delegate ()
        {
            cardObj.transform.DOMove(cardObj.transform.position, attackShakeTimeToGo * two).SetEase(Ease.Unset).OnComplete(delegate ()
            {
                cardObj.transform.DOMove(vec, attackShakeTimeToGo * three).SetEase(Ease.Unset).OnComplete(delegate ()
                {
                    cardObj.transform.DOMove(targetPos, attackShakeTimeToGo * four).SetEase(Ease.Unset);
                });
            });
        });
    }
}