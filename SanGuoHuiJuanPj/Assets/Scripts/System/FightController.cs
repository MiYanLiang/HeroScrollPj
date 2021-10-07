using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Assets.System.WarModule;
using CorrelateLib;
using UnityEngine;
using UnityEngine.UI;

public class FightController : MonoBehaviour
{
    public static FightController instance;

    StateOfFight stateOfFight;  //战斗状态

    [HideInInspector]
    public int recordWinner;    //标记胜负 -1：输  1：胜

    private bool isPlayerRound;
    private int roundNums;    //回合数

    [HideInInspector]
    public bool isRoundBegin; //回合是否开始

    public int ChessPosIndex { get; private set; } //行动单位索引

    public  float attackShakeTimeToGo;  //移动时间
    public float attackShakeTimeToBack;  //移动时间
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

    bool isNormalAttack;    //记录特殊远程兵种这次攻击是否是普攻

    [SerializeField]
    GameObject fightBackForShake;
    [SerializeField]
    float doShakeIntensity;

    public AudioSource audioSource;

    private List<FightCardData> gunMuCards; //滚木列表
    private List<FightCardData> gunShiCards;//滚石列表

    int totalGold = 0;    //本场战斗获得金币数
    List<int> chests = new List<int>();    //本场战斗获得宝箱

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
        ChessPosIndex = 0;
        isRoundBegin = false;
        isPlayerRound = true;
        timerForFight = 0;
        targetIndex = -1;
    }

    //普攻
    IEnumerator OnAttackStart(float damageBonus, FightCardData attackUnit, FightCardData target, bool counterAble)
    {
        isNormalAttack = true; //*远程兵种普攻
        var armed = MilitaryInfo.GetInfo(attackUnit.cardId).Id;
        var attackUnitIsNonGeneralDamage = armed == 28 || armed == 29 || armed == 32 || armed == 33;
        if (attackUnitIsNonGeneralDamage)//如果是无普通攻击单位
        {
            //return rounds;
            yield return new WaitForSeconds(attackIntervalTime);

            if (counterAble)
                //SpecialHeroSkill1(damageBonus, attackUnit, target);
                yield return SpecialHeroSkill1(damageBonus, attackUnit, target);

            yield return null;
        }

        var damage = HeroCardMakeSomeDamages(counterAble, attackUnit); //基础，暴击、会心等
        damage += (int)damageBonus;
        damage = MilitarySkillDamage(damage, attackUnit, target, counterAble); //计算技能

        switch (target.cardType)
        {
            //攻击老巢
            case 522:
            {
                if (isNormalAttack)
                {
                    target.Hp.Add(-damage);
                    TargetAnimShow(target, damage);
                    //判定胜负
                    if (target.Hp <= 0) recordWinner = target.isPlayerCard ? -1 : 1;
                }
                break;
            }
            //攻击的是陷阱单位
            case 3:
            {
                if (isNormalAttack)
                {
                    AttackTrapUnit(damage, attackUnit, target, counterAble);
                }
                break;
            }
            //攻击的是塔单位
            case 2:
            {
                if (isNormalAttack)
                {
                    target.Hp.Add(-damage);
                    TargetAnimShow(target, damage);
                }
                break;
            }
            default://攻击的是武将单位
            {
                damage = DefDamageProcessFun(attackUnit, target, damage); //计算防御
                damage = TieQiFenTan(damage, target); //铁骑伤害分摊
                if (isNormalAttack)
                {
                    damage = AddOrCutShieldValue(damage, target, false); //计算防护盾
                    target.Hp.Add(-damage);
                    TargetAnimShow(target, damage);
                }

                yield return SpecialHeroSkill0(damage, attackUnit, target, counterAble); //禁卫反击
                break;
            }
        }

        yield return new WaitForSeconds(attackIntervalTime);

        if (counterAble)
        {
            yield return SpecialHeroSkill1(damageBonus, attackUnit, target);
        }
    }


    //IEnumerator NewOnAttackStart(GameCardOperation op, float damageBonus, bool allowCounterAttack)
    //{
    //    isNormalAttack = true; //*远程兵种普攻

    //    var attackUnitIsNonGeneralDamage = op.Unit.IsNonGeneralDamage;
    //    var target = op.Target.Unit;
    //    if (attackUnitIsNonGeneralDamage)//如果是无普通攻击单位
    //    {
    //        yield return new WaitForSeconds(attackIntervalTime);

    //        if (allowCounterAttack)
    //        {
    //            op.MainOperation = SpecialHeroSkill1(damageBonus, op.Unit, target);
    //        }

    //        yield return null;
    //    }

    //    int damage = 0;
    //    damage = (int) (HeroCardMakeSomeDamages(allowCounterAttack, op.Unit) * damageBonus); //基础，暴击、会心等
    //    damage = MilitarySkillDamage(damage, op.Unit, target, allowCounterAttack); //计算技能

    //    switch (target.cardType)
    //    {
    //        //攻击老巢
    //        case 522:
    //        {
    //            if (isNormalAttack)
    //            {
    //                target.Hp.Add(-damage);
    //                TargetAnimShow(target, damage);
    //                //判定胜负
    //                if (target.Hp <= 0) recordWinner = target.isPlayerCard ? -1 : 1;
    //            }
    //            break;
    //        }
    //        //攻击的是陷阱单位
    //        case 3:
    //        {
    //            if (isNormalAttack)
    //            {
    //                AttackTrapUnit(damage, op.Unit, target, allowCounterAttack);
    //            }
    //            break;
    //        }
    //        //攻击的是塔单位
    //        case 2:
    //        {
    //            if (isNormalAttack)
    //            {
    //                target.Hp.Add(-damage);
    //                TargetAnimShow(target, damage);
    //            }
    //            break;
    //        }
    //        default://攻击的是武将单位
    //        {
    //            damage = DefDamageProcessFun(op.Unit, target, damage); //计算防御
    //            damage = TieQiFenTan(damage, target); //铁骑伤害分摊
    //            if (isNormalAttack)
    //            {
    //                damage = AddOrCutShieldValue(damage, target, false); //计算防护盾
    //                target.Hp.Add(-damage);
    //                TargetAnimShow(target, damage);
    //            }

    //            op.MainOperation = SpecialHeroSkill0(damage, op.Unit, target, allowCounterAttack); //禁卫反击
    //            break;
    //        }
    //    }

    //    yield return new WaitForSeconds(attackIntervalTime);

    //    if (allowCounterAttack)
    //    {
    //        op.MainOperation = SpecialHeroSkill1(damageBonus, op.Unit, target);
    //    }
    //}

    /// <summary>
    /// 计算战鼓台、暴击会心、羁绊等伤害加成
    /// </summary>
    /// <param name="counterable"></param>
    /// <param name="fightCardData"></param>
    /// <returns></returns>
    private int HeroCardMakeSomeDamages(bool counterable, FightCardData fightCardData)
    {
        var combat = HeroCombatInfo.GetInfo(fightCardData.cardId);
        var military = MilitaryInfo.GetInfo(fightCardData.cardId);
        var info = GameCardInfo.GetInfo((GameCardType)fightCardData.cardType,fightCardData.cardId);
        var dmg = 0;
        dmg = (int)(fightCardData.damage * (fightCardData.CardState.StrengthUp + 100) / 100f);//战鼓台伤害加成
        if (counterable)
        {
            switch (indexAttackType)
            {
                case 1:
                    //dmg.Rouse = combat.GetRouseDamage();
                    dmg = (int)combat.GetRouseDamage(dmg);
                    break;
                case 2:
                    //dmg.Critical = combat.GetCriticalDamage();
                    dmg= (int)combat.GetCriticalDamage(dmg);
                    break;
                default:
                    break;
            }
        }
        //羁绊相关伤害
        Dictionary<int, JiBanActivedClass> jiBanAllTypes = fightCardData.isPlayerCard ? playerJiBanAllTypes : enemyJiBanAllTypes;//判断敌方还是我方
        //判断势力
        switch (info.ForceId)
        {
            case 0://刘备军团
                if (jiBanAllTypes[(int)JiBanSkillName.TaoYuanJieYi].IsActive)
                {
                    //【桃园结义】激活时【刘备军团】武将伤害提升
                    dmg=JiBanAddOn(dmg,148);
                }
                break;
            case 1://曹操军团
                if (jiBanAllTypes[(int)JiBanSkillName.HuChiELai].IsActive)
                {
                    //【虎痴恶来】激活时【曹操军团】武将伤害加成
                    dmg = JiBanAddOn(dmg, 152);
                }
                break;
            case 2://孙权军团
                if (jiBanAllTypes[(int)JiBanSkillName.HuJuJiangDong].IsActive)
                {
                    //【虎踞江东】激活时【孙权军团】武将伤害加成
                    dmg = JiBanAddOn(dmg, 157);
                }
                break;
            case 3://袁绍军团
                if (jiBanAllTypes[(int)JiBanSkillName.HeBeiSiTingZhu].IsActive)
                {
                    //【河北四庭柱】激活时【袁绍军团】武将伤害加成
                    dmg = JiBanAddOn(dmg, 163);
                }
                break;
            case 4://吕布军团
                break;
            default:
                break;
        }
        //判断兵系
        switch (military.Id)
        {
            case 5: //骑兵系
                if (jiBanAllTypes[(int) JiBanSkillName.JueShiWuShuang].IsActive)
                {
                    //【绝世无双】激活时【骑兵系】武将伤害加成50%
                    dmg = JiBanAddOn(dmg, 164);
                }

                break;
            case 8: //战船系
                if (jiBanAllTypes[(int) JiBanSkillName.ShuiShiDuDu].IsActive)
                {
                    //水师都督激活时战船系武将伤害加成50%
                    dmg = JiBanAddOn(dmg, 160);
                }

                break;
            case 11: //统御系
                break;
            default:
                break;
        }

        //判断近战远程
        switch (fightCardData.combatType)
        {
            case 0://近战
                if (jiBanAllTypes[(int)JiBanSkillName.WuZiLiangJiang].IsActive)
                {
                    //【五子良将】激活时【近战】武将伤害加成
                    dmg = JiBanAddOn(dmg, 153);
                }
                break;
            case 1://远程
                if (jiBanAllTypes[(int)JiBanSkillName.WoLongFengChu].IsActive)
                {
                    //【卧龙凤雏】激活时【远程】武将伤害加成
                    dmg = JiBanAddOn(dmg, 151);
                }
                break;
        }
        //判断物理法术
        if (fightCardData.cardDamageType == 1 && jiBanAllTypes[(int) JiBanSkillName.HanMoSanXian].IsActive)
        {
            //汉末三仙激活时法术武将伤害加成30%
            dmg = JiBanAddOn(dmg, 161);
        }

        if (fightCardData.cardType == 0 && fightCardData.CardState.Unarmed > 0)
        {
            //若攻击者有卸甲状态伤害降低30%
            dmg = (int)(dmg * (100 - DataTable.GetGameValue(7)) / 100f);
        }
        return dmg;

        int JiBanAddOn(int d,int id) => d = (int) (d* (DataTable.GetGameValue(id) + 100) * 0.01f);
    }

    ///////攻击trap单位////////////
    private void AttackTrapUnit(int damage, FightCardData attackUnit, FightCardData defUnit, bool isCanFightBack)
    {
        var isGongChengChe = ((GameCardType)attackUnit.cardType) == GameCardType.Hero &&
                             DataTable.Hero[attackUnit.cardId].MilitaryUnitTableId == 23;//攻城车

        switch (defUnit.cardId)
        {
            case 0://拒马
                defUnit.Hp.Add(-damage);
                TargetAnimShow(defUnit, damage);
                if (isCanFightBack && attackUnit.combatType == 0 && !isGongChengChe)//攻城车不被拒马反伤
                {
                    damage = DefDamageProcessFun(defUnit, attackUnit, damage);
                    attackUnit.Hp.Add(-(int)(damage * (DataTable.GetGameValue(8) / 100f)));
                    GameObject effectObj = AttackToEffectShow(attackUnit, false, Effect.Blademail7A);
                    effectObj.transform.localScale = new Vector3(1, defUnit.isPlayerCard ? 1 : -1, 1);
                    TargetAnimShow(attackUnit, damage);
                    PlayAudioForSecondClip(89, 0.2f);
                }
                break;
            case 1://地雷
                defUnit.Hp.Add(-damage);
                TargetAnimShow(defUnit, damage);
                if (isCanFightBack && attackUnit.combatType == 0)  //踩地雷的是近战
                {
                    var info = GameCardInfo.GetInfo((GameCardType) defUnit.cardType, defUnit.cardId);
                    var dileiDamage = info.GetDamage(defUnit.level) * DataTable.GetGameValue(9) / 100;
                        //;
                    dileiDamage = DefDamageProcessFun(defUnit, attackUnit, dileiDamage);
                    attackUnit.Hp.Add(-dileiDamage);
                    AttackToEffectShow(attackUnit, false, Effect.Explode);
                    TargetAnimShow(attackUnit, dileiDamage);
                    PlayAudioForSecondClip(88, 0.2f);
                }
                break;
            case 2://石墙
                defUnit.Hp.Add(-damage);
                TargetAnimShow(defUnit, damage);
                break;
            case 3://八阵图
                defUnit.Hp.Add(-damage);
                TargetAnimShow(defUnit, damage);
                if (attackUnit.combatType == 0 && !isGongChengChe)//攻城车免疫
                {
                    TakeOneUnitDizzed(attackUnit, DataTable.GetGameValue(133));
                }
                break;
            case 4://金锁阵
                defUnit.Hp.Add(-damage);
                TargetAnimShow(defUnit, damage);
                if (attackUnit.combatType == 0 && !isGongChengChe)//攻城车免疫
                {
                    TakeToImprisoned(attackUnit, DataTable.GetGameValue(10));
                }
                break;
            case 5://鬼兵阵
                defUnit.Hp.Add(-damage);
                TargetAnimShow(defUnit, damage);
                if (attackUnit.combatType == 0 && !isGongChengChe)//攻城车免疫
                {
                    TakeToCowardly(attackUnit, DataTable.GetGameValue(11));
                }
                break;
            case 6://火墙
                defUnit.Hp.Add(-damage);
                TargetAnimShow(defUnit, damage);
                if (isCanFightBack && attackUnit.combatType == 0 && !isGongChengChe)//攻城车免疫
                {
                    TakeToBurn(attackUnit, DataTable.GetGameValue(12));
                }
                break;
            case 7://毒泉
                defUnit.Hp.Add(-damage);
                TargetAnimShow(defUnit, damage);
                if (isCanFightBack && attackUnit.combatType == 0 && !isGongChengChe)//攻城车免疫
                {
                    TakeToPoisoned(attackUnit, DataTable.GetGameValue(13));
                }
                break;
            case 8://刀墙
                defUnit.Hp.Add(-damage);
                TargetAnimShow(defUnit, damage);
                if (isCanFightBack && attackUnit.combatType == 0 && !isGongChengChe)//攻城车免疫
                {
                    TakeToBleed(attackUnit, DataTable.GetGameValue(14));
                }
                break;
            case 9://滚石
                defUnit.Hp.Add(-damage);
                TargetAnimShow(defUnit, damage);
                break;
            case 10://滚木
                defUnit.Hp.Add(-damage);
                TargetAnimShow(defUnit, damage);
                break;
            case 11://金币宝箱
                if (defUnit.Hp > 0)
                {
                    defUnit.Hp.Add(-damage);
                    GetGoldBoxFun(defUnit);
                }
                TargetAnimShow(defUnit, damage);
                break;
            case 12://宝箱
                if (defUnit.Hp > 0)
                {
                    defUnit.Hp.Add(-damage);
                    if (defUnit.Hp <= 0)
                    {
                        //GameObject obj = EffectsPoolingControl.instance.GetEffectToFight("GetGold", 1.5f, attackedUnit.cardObj.transform);
                        //obj.GetComponentInChildren<Text>().text = string.Format(LoadJsonFile.GetStringText(8), LoadJsonFile.enemyUnitTableDatas[attackedUnit.unitId][4]);
                        PlayAudioForSecondClip(98, 0);
                    }
                }
                TargetAnimShow(defUnit, damage);
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
            GameObject obj = EffectsPoolingControl.instance.GetVTextEffect(Effect.GetGold, 1.5f, attackedUnit.cardObj);
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
                yield return GunMuTrapAttack(gunMuList[i], 1f, DataTable.GetGameValue(15));
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

        var opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);
        PlayAudioForSecondClip(95, 0);
        //yield return CardAnimator.RangePreAction(attackUnit);//, attackShakeTimeToGo / 2);
        //yield return new WaitForSeconds(attackShakeTimeToGo);

        List<FightCardData> newGunMuList = new List<FightCardData>();
        List<FightCardData> newGunShiList = new List<FightCardData>();

        List<int> fightColumns = new List<int>() { 0, 1, 2, 3, 4 };  //记录会攻击到的列
        int cutHpNum = (int)(attackUnit.damage * damageRate);
        for (int i = 0; i < fightColumns.Count; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                int cardIndex = fightColumns[i] + j * 5;
                if (opponents[cardIndex] != null && opponents[cardIndex].Hp > 0)
                {
                    var nowDamage = DefDamageProcessFun(attackUnit, opponents[cardIndex], cutHpNum);
                    if (opponents[cardIndex].cardType == 3)    //滚石和滚木，对陷阱造成2倍伤害
                    {
                        nowDamage = nowDamage * 2;
                    }
                    if (cardIndex != 17)//？
                    {
                        opponents[cardIndex].Hp.Add(-nowDamage);
                        TakeOneUnitDizzed(opponents[cardIndex], dizzedRate);
                    }
                    else
                    {
                        opponents[cardIndex].Hp.Add(-nowDamage);
                        if (opponents[cardIndex].Hp <= 0)
                        {
                            recordWinner = attackUnit.isPlayerCard ? 1 : -1;
                        }
                    }
                    AttackToEffectShow(opponents[cardIndex], false, Effect.Dropping);
                    ShowSpellTextObj(opponents[cardIndex].cardObj, DataTable.GetStringText(9), true, true);
                    TargetAnimShow(opponents[cardIndex], nowDamage);

                    if (opponents[cardIndex].Hp <= 0 &&
                        opponents[cardIndex].cardType == 3 &&
                        (opponents[cardIndex].cardId == 9 || opponents[cardIndex].cardId == 10))
                    {
                        if (opponents[cardIndex].cardId == 9)
                        {
                            newGunShiList.Add(opponents[cardIndex]);
                        }
                        else
                        {
                            newGunMuList.Add(opponents[cardIndex]);
                        }
                    }
                    break;
                }
            }
        }

        if (newGunMuList.Count > 0 || newGunShiList.Count > 0)
        {
            yield return GunMuGunShiSkill(newGunMuList, newGunShiList);
        }

        yield return new WaitForSeconds(attackShakeTimeToGo);
    }

    //滚石反击
    IEnumerator GunShiTrapAttack(FightCardData attackUnit, float damageRate, int dizzedRate)
    {
        attackUnit.isActionDone = true;

        yield return new WaitForSeconds(attackShakeTimeToGo / 2);

        var opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);
        PlayAudioForSecondClip(94, 0);
        //yield return CardAnimator.RangePreAction(attackUnit);//, attackShakeTimeToGo / 2);
        //yield return new WaitForSeconds(attackShakeTimeToGo);

        List<FightCardData> newGunMuList = new List<FightCardData>();
        List<FightCardData> newGunShiList = new List<FightCardData>();

        int startIndex = attackUnit.posIndex % 5;
        int cutHpNum = (int)(attackUnit.damage * damageRate);
        for (int i = 0; i < 4; i++)
        {
            int cardIndex = startIndex + i * 5;
            if (opponents[cardIndex] != null && opponents[cardIndex].Hp > 0)
            {
                int nowDamage = DefDamageProcessFun(attackUnit, opponents[cardIndex], cutHpNum);
                if (opponents[cardIndex].cardType == 3)    //滚石和滚木，对陷阱造成2倍伤害
                {
                    nowDamage = nowDamage * 2;
                }
                if (cardIndex != 17)
                {
                    opponents[cardIndex].Hp.Add(-nowDamage);
                    TakeOneUnitDizzed(opponents[cardIndex], dizzedRate);
                }
                else
                {
                    opponents[cardIndex].Hp.Add(-nowDamage);
                    if (opponents[cardIndex].Hp <= 0)
                    {
                        recordWinner = attackUnit.isPlayerCard ? 1 : -1;
                    }
                }
                AttackToEffectShow(opponents[cardIndex], false, Effect.Dropping);
                ShowSpellTextObj(opponents[cardIndex].cardObj, DataTable.GetStringText(10), true, true);
                TargetAnimShow(opponents[cardIndex], nowDamage);

                if (opponents[cardIndex].Hp <= 0 &&
                        opponents[cardIndex].cardType == 3 &&
                        (opponents[cardIndex].cardId == 9 || opponents[cardIndex].cardId == 10))
                {
                    if (opponents[cardIndex].cardId == 9)
                    {
                        newGunShiList.Add(opponents[cardIndex]);
                    }
                    else
                    {
                        newGunMuList.Add(opponents[cardIndex]);
                    }
                }
            }
        }

        if (newGunMuList.Count > 0 || newGunShiList.Count > 0)
        {
            yield return GunMuGunShiSkill(newGunMuList, newGunShiList);
        }

        yield return new WaitForSeconds(attackShakeTimeToGo);
    }

    //箭楼远射技能(塔)
    public void JianLouYuanSheSkill(FightCardData attackUnit, int finalDamage)
    {
        int damage = (int)(DataTable.GetGameValue(17) / 100f * finalDamage);
        PlayAudioForSecondClip(20, 0);

        var opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);
        List<int> canFightUnits = new List<int>();
        for (int i = 0; i < opponents.Count; i++)
        {
            //if (fightCardDatas[i] != null && fightCardDatas[i].cardType == 0 && fightCardDatas[i].Hp > 0)
            if (opponents[i] != null && opponents[i].Hp > 0)//箭楼可攻击任何单位
            {
                canFightUnits.Add(i);
            }
        }
        List<int> attackedIndexList = BackRandsList(canFightUnits, DataTable.GetGameValue(18));
        for (int i = 0; i < attackedIndexList.Count; i++)
        {
            FightCardData attackedUnit = opponents[attackedIndexList[i]];
            AttackToEffectShow(attackedUnit, false, Effect.Bow20A);

            int nowDamage = DefDamageProcessFun(attackUnit, attackedUnit, damage);
            attackedUnit.Hp.Add(-nowDamage);
            TargetAnimShow(attackedUnit, nowDamage);
            if (attackedUnit.cardType == 522 && attackedUnit.Hp<=0)

            { 

                recordWinner = attackUnit.isPlayerCard ? 1 : -1;

            }

        }
    }

    #endregion

    ///////造成伤害时触发的特殊技能///////////

    private int MilitarySkillDamage(int finalDamage, FightCardData attackUnit, FightCardData target, bool isCanFightBack)
    {
        if (isCanFightBack)
        {
            if (attackUnit.CardState.Imprisoned <= 0) //攻击者没有禁锢状态
            {
                switch (DataTable.Hero[attackUnit.cardId].MilitaryUnitTableId)
                {
                    case 3://飞甲
                        PlayAudioForSecondClip(3, 0);
                        AttackToEffectShow(target, false, Effect.FeiJia3A);
                        break;
                    case 4://大盾
                        ShowSpellTextObj(attackUnit.cardObj, "4", false);
                        AttackToEffectShow(attackUnit, false, Effect.Shield4A);
                        if (attackUnit.CardState.Shield <= 0)
                        {
                            FightForManager.instance.CreateSateIcon(attackUnit.cardObj.War.StateContent,
                                StringNameStatic.StateIconPath_withStand, true);
                        }

                        attackUnit.CardState.Shield++;
                        PlayAudioForSecondClip(4, 0);
                        break;
                    case 6://虎卫
                        AttackToEffectShow(target, false, Effect.SuckBlood6A);
                        PlayAudioForSecondClip(6, 0);
                        break;
                    case 8://象兵
                        XiangBingTrampleAttAck(target, attackUnit);
                        break;
                    case 9://先锋
                        AttackToEffectShow(target, false, Effect.Cavalry9A);
                        PlayAudioForSecondClip(9, 0);
                        break;
                    case 60:
                        AttackToEffectShow(target, false, Effect.Cavalry60A);
                        PlayAudioForSecondClip(9, 0);
                        break;
                    case 10:
                        finalDamage = SiShiSheMingAttack(finalDamage, attackUnit, target);
                        break;
                    case 11:
                        finalDamage = TieQiWuWeiAttack(finalDamage, attackUnit, target);
                        break;
                    case 12:
                        finalDamage = ShenWuZhanYiAttack(finalDamage, attackUnit, target);
                        break;
                    case 59:
                        QiangBingChuanCiAttack(finalDamage, attackUnit, target, 59);
                        break;
                    case 14:
                        QiangBingChuanCiAttack(finalDamage, attackUnit, target, 14);
                        break;
                    case 15:
                        JianBingHengSaoAttack(finalDamage, attackUnit, target);
                        break;
                    case 16:
                        AttackToEffectShow(target, false, Effect.Cavalry16A);
                        PlayAudioForSecondClip(16, 0);
                        break;
                    case 17:
                        AttackToEffectShow(target, false, Effect.Blade17A);
                        PlayAudioForSecondClip(17, 0);
                        break;
                    case 18:
                        finalDamage = FuBingTuLuAttack(finalDamage, target, attackUnit);
                        break;
                    case 19:
                        AttackToEffectShow(target, false, Effect.CrossBow19A);
                        PlayAudioForSecondClip(19, 0);
                        break;
                    case 51:
                        AttackToEffectShow(target, false, Effect.CrossBow19A);
                        PlayAudioForSecondClip(19, 0);
                        break;
                    case 20:
                        GongBingYuanSheSkill(finalDamage, attackUnit, target, 20);
                        break;
                    case 52:
                        GongBingYuanSheSkill(finalDamage, attackUnit, target, 52);
                        break;
                    case 21:
                        finalDamage = ZhanChuanChongJiAttack(finalDamage, target, attackUnit);
                        break;
                    case 22:
                        finalDamage = ZhanCheZhuangYaAttack(finalDamage, target, attackUnit);
                        break;
                    case 23:
                        finalDamage = GongChengChePoCheng(finalDamage, attackUnit, target);
                        break;
                    case 24:
                        TouShiCheSkill(finalDamage, attackUnit);
                        break;
                    case 25:
                        CiKePoJiaAttack(target, attackUnit);
                        break;
                    case 26:
                        JunShiSkill(attackUnit, 26, finalDamage);
                        SpecilSkillNeedPuGongFun(target);
                        break;
                    case 27:
                        JunShiSkill(attackUnit, 27, finalDamage);
                        SpecilSkillNeedPuGongFun(target);
                        break;
                    case 28:
                    case 29:
                    case 32:
                    case 33:
                        isNormalAttack = false;
                        break;
                    case 30:
                        DuShiSkill(DataTable.GetGameValue(19), attackUnit, 30, finalDamage);
                        SpecilSkillNeedPuGongFun(target);
                        break;
                    case 31:
                        DuShiSkill(DataTable.GetGameValue(20), attackUnit, 31, finalDamage);
                        SpecilSkillNeedPuGongFun(target);
                        break;
                    case 34:
                        BianShiSkill(DataTable.GetGameValue(21), attackUnit, 34);
                        SpecilSkillNeedPuGongFun(target);
                        break;
                    case 35:
                        BianShiSkill(DataTable.GetGameValue(22), attackUnit, 35);
                        SpecilSkillNeedPuGongFun(target);
                        break;
                    case 36:
                        MouShiSkill(DataTable.GetGameValue(23), attackUnit, 36);
                        SpecilSkillNeedPuGongFun(target);
                        break;
                    case 37:
                        MouShiSkill(DataTable.GetGameValue(24), attackUnit, 37);
                        SpecilSkillNeedPuGongFun(target);
                        break;
                    case 38:
                        NeiZhengSkill(attackUnit);
                        SpecilSkillNeedPuGongFun(target);
                        break;
                    case 39:
                        FuZuoBiHuSkill(finalDamage, attackUnit);
                        SpecilSkillNeedPuGongFun(target);
                        break;
                    case 40:
                        QiXieXiuFu(attackUnit);
                        SpecilSkillNeedPuGongFun(target);
                        break;
                    case 42:
                        YiShengSkill(DataTable.GetGameValue(25), attackUnit, 42);
                        SpecilSkillNeedPuGongFun(target);
                        break;
                    case 43:
                        YiShengSkill(DataTable.GetGameValue(26), attackUnit, 43);
                        SpecilSkillNeedPuGongFun(target);
                        break;
                    case 44:
                        ShuiBingXieJia(attackUnit, target);
                        break;
                    case 45:
                        MeiRenJiNeng(attackUnit, 45);
                        SpecilSkillNeedPuGongFun(target);
                        break;
                    case 46:
                        MeiRenJiNeng(attackUnit, 46);
                        SpecilSkillNeedPuGongFun(target);
                        break;
                    case 47:
                        ShuiKeSkill(DataTable.GetGameValue(27), attackUnit, 47);
                        SpecilSkillNeedPuGongFun(target);
                        break;
                    case 48:
                        ShuiKeSkill(DataTable.GetGameValue(28), attackUnit, 48);
                        SpecilSkillNeedPuGongFun(target);
                        break;
                    case 49:
                        PlayAudioForSecondClip(49, 0);
                        AttackToEffectShow(target, false, Effect.Crossbow49A);
                        break;
                    case 50:
                        PlayAudioForSecondClip(50, 0);
                        AttackToEffectShow(target, false, Effect.Scribe50A);
                        break;
                    case 53:
                        YinShiSkill(DataTable.GetGameValue(29), attackUnit, 53, finalDamage);
                        SpecilSkillNeedPuGongFun(target);
                        break;
                    case 54:
                        YinShiSkill(DataTable.GetGameValue(30), attackUnit, 54, finalDamage);
                        SpecilSkillNeedPuGongFun(target);
                        break;
                    case 55:
                        HuoChuanSkill(finalDamage, attackUnit, target);
                        break;
                    case 56:
                        ManZuSkill(attackUnit, target);
                        break;
                    case 57:
                        PlayAudioForSecondClip(57, 0);
                        AttackToEffectShow(target, true);
                        break;
                    case 58:
                        finalDamage = TieQiSkill(finalDamage, attackUnit, target);
                        break;
                    case 65:
                        finalDamage = HuangJinSkill(finalDamage, attackUnit, target);
                        break;
                    default:
                        //isNeedToAttack = false;
                        PlayAudioForSecondClip(0, 0);
                        AttackToEffectShow(target, true);
                        break;
                }
            }
            else
            {
                PlayAudioForSecondClip(0, 0);
                ShowSpellTextObj(attackUnit.cardObj, DataTable.GetStringText(11), true, true);
            }
        }

        if (target.cardType == 0)//如果受击单位是武将
        {
            switch (DataTable.Hero[target.cardId].MilitaryUnitTableId)
            {
                case 7:
                    if (isCanFightBack)
                    {
                        CiJiaFanShangAttack(finalDamage, attackUnit, target);
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
        if (!isCanFightBack) yield break;
        if (attackedUnit.CardState.Stunned > 0 || attackedUnit.CardState.Imprisoned > 0) yield break;
        //禁卫
        if (DataTable.Hero[attackedUnit.cardId].MilitaryUnitTableId == 13 && attackUnit.combatType == 0)
        {
            yield return JinWeiFanJiAttack(attackedUnit, attackUnit);
        }
    }

    //特殊单位的技能行动1   连击系列
    IEnumerator SpecialHeroSkill1(float damageBonus, FightCardData attackUnit, FightCardData target)
    {
        yield return GunMuGunShiSkill(gunMuCards, gunShiCards);

        if (attackUnit.CardState.Imprisoned > 0) //禁锢，不进行技能攻击
        {
            attackUnit.CardState.Imprisoned--;
            ShowSpellTextObj(attackUnit.cardObj, DataTable.GetStringText(11), true, true);
            if (attackUnit.CardState.Imprisoned <= 0)
            {
                attackUnit.CardState.Imprisoned = 0;
                FightForManager.instance.DestroySateIcon(attackUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_imprisoned, true);
            }
        }
        else
        {
            switch (DataTable.Hero[attackUnit.cardId].MilitaryUnitTableId)
            {
                case 9:
                    yield return XianFengYongWu(attackUnit, target, 9);
                    break;
                case 60:
                    yield return XianFengYongWu(attackUnit, target, 60);
                    break;
                case 16:
                    yield return QiBingChiCheng(attackUnit, target);
                    break;
                case 17:
                    yield return DaoBingLianZhan(damageBonus, attackUnit, target);
                    break;
                case 19:
                    yield return NuBingLianShe(attackUnit, target, 19);
                    break;
                case 51:
                    yield return NuBingLianShe(attackUnit, target, 51);
                    break;
                case 28:
                    yield return ShuShiLuoLei(DataTable.GetGameValue(31), attackUnit, 28);
                    break;
                case 29:
                    yield return ShuShiLuoLei(DataTable.GetGameValue(32), attackUnit, 29);
                    break;
                case 32:
                    yield return TongShuaiSkill(attackUnit, 32);
                    break;
                case 33:
                    yield return TongShuaiSkill(attackUnit, 33);
                    break;
                case 12:
                    yield return ShenWuZhanYi(damageBonus, attackUnit, target);
                    break;
                default:
                    break;
            }
        }

        yield return GunMuGunShiSkill(gunMuCards, gunShiCards);
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
        IReadOnlyList<FightCardData> opponents;
        List<GameObject> posListToSetBurn;

        int burnRoundIndex = 0; //记录烧到第几圈了
        opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);
        if (attackUnit.isPlayerCard)
        {
            burnRoundIndex = tongShuaiBurnRoundPy;
            posListToSetBurn = FightForManager.instance.enemyCardsPos;
        }
        else
        {
            burnRoundIndex = tongShuaiBurnRoundEm;
            posListToSetBurn = FightForManager.instance.playerCardsPos;
        }

        //尝试消除上一圈火焰,灼烧之前回合烧过的地方
        if (burnRoundIndex != -1)
        {
            for (int i = 0; i < GoalGfSetFireRound[burnRoundIndex].Length; i++)
            {
                if (opponents[GoalGfSetFireRound[burnRoundIndex][i]] != null && opponents[GoalGfSetFireRound[burnRoundIndex][i]].cardType == 0 && opponents[GoalGfSetFireRound[burnRoundIndex][i]].Hp > 0)
                {
                    TakeToBurn(opponents[GoalGfSetFireRound[burnRoundIndex][i]], DataTable.GetGameValue(33), attackUnit);
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

            if (opponents[targets[i]] != null && opponents[targets[i]].Hp > 0)
            {
                int nowDamage = DefDamageProcessFun(attackUnit, opponents[targets[i]], damage);
                opponents[targets[i]].Hp.Add(-nowDamage);
                TargetAnimShow(opponents[targets[i]], nowDamage);
                if (opponents[targets[i]].cardType == 522)
                {
                    if (opponents[targets[i]].Hp <= 0)
                    {
                        recordWinner = opponents[targets[i]].isPlayerCard ? -1 : 1;
                    }
                }
                else
                {
                    if (opponents[targets[i]].cardType == 0)
                    {
                        TakeToBurn(opponents[targets[i]], classType == 32 ? DataTable.GetGameValue(35) : DataTable.GetGameValue(36), attackUnit);
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
                float waitTime = CountAttackTimeSpan(attackUnit);
                yield return new WaitForSeconds(waitTime);
            }

            yield return GunMuGunShiSkill(gunMuCards, gunShiCards);
        }
        thunderCloudObj.GetComponent<Image>().DOColor(new Color(0, 0, 0, 0), 0.5f);
        thunderCloudObj.SetActive(false);
    }
    //召唤落雷
    private bool ShuShiTakeThunder(FightCardData attackUnit, int classType)
    {
        string effectStr = classType + "A";
        var opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);
        List<GameObject> posListToThunder = attackUnit.isPlayerCard ? FightForManager.instance.enemyCardsPos : FightForManager.instance.playerCardsPos;
        List<int> canFightUnits = new List<int>();
        for (int i = 0; i < opponents.Count; i++)
        {
            canFightUnits.Add(i);
        }
        int damage = (int)(HeroCardMakeSomeDamages(true, attackUnit) * DataTable.GetGameValue(37) / 100f);
        List<int> attackedIndexList = BackRandsList(canFightUnits, classType == 28 ? Random.Range(1, DataTable.GetGameValue(38)) : Random.Range(1, DataTable.GetGameValue(39)));

        thunderCloudObj.transform.GetChild(0).gameObject.SetActive(true);

        for (int i = 0; i < attackedIndexList.Count; i++)
        {
            EffectsPoolingControl.instance.GetEffectToFight1(effectStr, 1f, posListToThunder[attackedIndexList[i]].transform);
            if (opponents[attackedIndexList[i]] != null && opponents[attackedIndexList[i]].Hp > 0)
            {
                int nowDamage = DefDamageProcessFun(attackUnit, opponents[attackedIndexList[i]], damage);
                opponents[attackedIndexList[i]].Hp.Add(-nowDamage);
                TargetAnimShow(opponents[attackedIndexList[i]], nowDamage);
                if (opponents[attackedIndexList[i]].cardType == 522)
                {
                    if (opponents[attackedIndexList[i]].Hp <= 0)
                    {
                        recordWinner = opponents[attackedIndexList[i]].isPlayerCard ? -1 : 1;
                        return true;
                    }
                }
                else
                {
                    TakeOneUnitDizzed(opponents[attackedIndexList[i]], DataTable.GetGameValue(40), attackUnit);
                }
            }
        }
        return false;
    }

    //刀兵连斩技能
    IEnumerator DaoBingLianZhan(float damageBonus, FightCardData attackUnit, FightCardData target)
    {
        if (attackUnit.Hp <= 0 || attackUnit.CardState.Stunned > 0 ||
            attackUnit.CardState.Imprisoned > 0) yield break;
        if (target.Hp > 0 || target.cardType == 522) yield break;
        //Debug.Log("-----刀兵连斩");
        float waitTime = CountAttackTimeSpan(attackUnit);
        yield return new WaitForSeconds(waitTime);
        ShowSpellTextObj(attackUnit.cardObj, "17", false);

        targetIndex = FightForManager.instance.FindOpponentIndex(attackUnit); //锁定目标卡牌
        //if(ChessPosIndex != -1) yield return CardAnimator.MeleeMoving(attackUnit, target);
        yield return new WaitForSeconds(attackShakeTimeToGo);

        var nextAttackedUnit = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard)[targetIndex];
        AttackToEffectShow(nextAttackedUnit, false, Effect.Blade17A);
        PlayAudioForSecondClip(17, 0);

        yield return OnAttackStart(damageBonus + DataTable.GetGameValue(41) / 100f, attackUnit,
            nextAttackedUnit, true);
    }

    //神武战意技能
    IEnumerator ShenWuZhanYi(float damageBonus, FightCardData attackUnit, FightCardData target)
    {
        if (attackUnit.Hp <= 0 || attackUnit.CardState.Stunned > 0 ||
            attackUnit.CardState.Imprisoned > 0) yield break;
        if (target.Hp <= 0 || target.cardType != 0) yield break;
        if (target.attackedBehavior != 2 && target.attackedBehavior != 3) yield break;
        var waitTime = CountAttackTimeSpan(attackUnit);
        yield return new WaitForSeconds(waitTime);
        ShowSpellTextObj(attackUnit.cardObj, "12", false);
        //if (ChessPosIndex != -1) yield return CardAnimator.MeleeMoving(attackUnit, target);
        yield return new WaitForSeconds(attackShakeTimeToGo);
        AttackToEffectShow(target, false, Effect.Stimulate12A);
        PlayAudioForSecondClip(12, 0);

        float propAttack = 1 + DataTable.GetGameValue(97) / 100f * attackUnit.CardState.Stimulate;

        yield return OnAttackStart(propAttack, attackUnit, target, true);
    }

    //先锋勇武技能
    IEnumerator XianFengYongWu(FightCardData attackUnit, FightCardData target, int classType)
    {
        if (attackUnit.Hp <= 0 || attackUnit.CardState.Stunned > 0 ||
            attackUnit.CardState.Imprisoned > 0) yield break;
        int triggerRatio = 0; //特殊技能触发概率
        int attackNums = 0; //攻击次数
        if (classType == 9)
        {
            triggerRatio = DataTable.GetGameValue(42);
            attackNums = DataTable.GetGameValue(43);
        }
        else
        {
            triggerRatio = DataTable.GetGameValue(44);
            attackNums = DataTable.GetGameValue(45);
        }

        if (!isSuccessFromHalfRandom(triggerRatio)) yield break;
        for (var i = 0; i < attackNums; i++)
        {
            float waitTime = CountAttackTimeSpan(attackUnit);
            yield return new WaitForSeconds(waitTime);
            ShowSpellTextObj(attackUnit.cardObj, "9", false);
            //if(ChessPosIndex!=-1) yield return CardAnimator.MeleeMoving(attackUnit, target);
            yield return new WaitForSeconds(attackShakeTimeToGo);
            AttackToEffectShow(target, false, classType + "A");
            PlayAudioForSecondClip(9, 0);
            //TakeToCowardly(attackedUnit, LoadJsonFile.GetGameValue(46));
            yield return OnAttackStart(1f, attackUnit, target, false);
        }
    }

    //骑兵驰骋技能
    IEnumerator QiBingChiCheng(FightCardData attackUnit, FightCardData target)
    {
        if (attackUnit.Hp <= 0 || attackUnit.CardState.Stunned > 0 ||
            attackUnit.CardState.Imprisoned > 0) yield break;
        if (target.Hp <= 0 ||
            (indexAttackType == 0 && !isSuccessFromHalfRandom(DataTable.GetGameValue(47)))) yield break;
        //Debug.Log("-----骑兵驰骋");
        float waitTime = CountAttackTimeSpan(attackUnit);
        yield return new WaitForSeconds(waitTime);
        ShowSpellTextObj(attackUnit.cardObj, "16", false);
        //if(ChessPosIndex!=-1) yield return CardAnimator.MeleeMoving(attackUnit, target);
        yield return new WaitForSeconds(attackShakeTimeToGo);
        AttackToEffectShow(target, false, Effect.Cavalry16A);
        PlayAudioForSecondClip(16, 0);

        yield return OnAttackStart(1f, attackUnit, target, true);
    }

    //弩兵连射技能
    IEnumerator NuBingLianShe(FightCardData attackUnit, FightCardData attackedUnit, int classIndex)
    {
        if (attackedUnit.Hp <= 0 ||
            !isSuccessFromHalfRandom(classIndex == 51 ? DataTable.GetGameValue(48) : DataTable.GetGameValue(49))) yield break;
        //Debug.Log("-----弩兵连射");
        var waitTime = CountAttackTimeSpan(attackUnit);
        yield return new WaitForSeconds(waitTime);
        //yield return CardAnimator.RangePreAction(attackUnit);//, yuanChengShakeTimeToGo);
        //yield return new WaitForSeconds(yuanChengShakeTimeToGo / 2);

        ShowSpellTextObj(attackUnit.cardObj, "19", false);
        AttackToEffectShow(attackedUnit, false, Effect.CrossBow19A);
        PlayAudioForSecondClip(19, 0);
        //连射
        yield return OnAttackStart(1, attackUnit, attackedUnit, false);

        if (classIndex != 51) yield break;
        yield return new WaitForSeconds(waitTime);
        //yield return CardAnimator.RangePreAction(attackUnit);//, yuanChengShakeTimeToGo);
        //yield return new WaitForSeconds(yuanChengShakeTimeToGo / 2);
        AttackToEffectShow(attackedUnit, false, Effect.CrossBow19A);
        PlayAudioForSecondClip(19, 0);
        //连射
        yield return OnAttackStart(1, attackUnit, attackedUnit, false);
    }

    //禁卫反击技能
    IEnumerator JinWeiFanJiAttack(FightCardData attackUnit, FightCardData attackedUnit)
    {
        if (attackUnit.Hp <= 0) yield break;
        yield return new WaitForSeconds(0.3f);
        ShowSpellTextObj(attackUnit.cardObj, "13", false);
        yield return new WaitForSeconds(0.2f);
        PlayAudioForSecondClip(13, 0);
        AttackToEffectShow(attackedUnit, false, Effect.Guard13A);

        int damage = DefDamageProcessFun(attackUnit, attackedUnit, attackUnit.damage);
        attackedUnit.Hp.Add(-damage);
        TargetAnimShow(attackedUnit, damage);
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
        var opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);

        PlayAudioForSecondClip(65, 0);
        ShowSpellTextObj(attackUnit.cardObj, "65", false);
        AttackToEffectShow(attackedUnit, false, Effect.YellowBand65A);

        int sameTypeHeroNums = 0;
        for (int i = 0; i < opponents.Count; i++)
        {
            if (opponents[i] != null && opponents[i].Hp > 0 && opponents[i].cardType == 0 && DataTable.Hero[opponents[i].cardId].MilitaryUnitTableId == 65)
            {
                AttackToEffectShow(opponents[i], false, Effect.YellowBand65B);

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
                                        TargetAnimShow(tieQiCardsList[i], backDamage);
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
            FightForManager.instance.DestroySateIcon(tieQiCard.cardObj.War.StateContent, StringNameStatic.StateIconPath_lianHuan, true);
            tieQiCards.Remove(tieQiCard);
        }

        if (tieQiCards.Count > 1)
        {
            //Debug.Log(tieQiCards.Count - 1);
            if (tieQiCards.Count == 2)
            {
                FightForManager.instance.CreateSateIcon(tieQiCards[0].cardObj.War.StateContent, StringNameStatic.StateIconPath_lianHuan, true);
            }
            FightForManager.instance.CreateSateIcon(tieQiCards[tieQiCards.Count - 1].cardObj.War.StateContent, StringNameStatic.StateIconPath_lianHuan, true);
            //for (int i = 0; i < tieQiCards.Count; i++)
            //{
            //    Debug.Log(i);
            //    FightForManager.instance.CreateSateIcon(tieQiCards[i].cardObj.War.StateContent, StringNameStatic.StateIconPath_lianHuan, true);
            //}
        }
        else
        {
            for (int i = 0; i < tieQiCards.Count; i++)
            {
                FightForManager.instance.DestroySateIcon(tieQiCards[i].cardObj.War.StateContent, StringNameStatic.StateIconPath_lianHuan, true);
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
            if (isSuccessFromHalfRandom(DataTable.GetGameValue(51)))
            {
                if (attackedUnit.CardState.Poison <= 0)
                {
                    FightForManager.instance.CreateSateIcon(attackedUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_poisoned, true);
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
            attackUnit.UpdateHpUi();

            finalDamage = (int)(finalDamage * DataTable.GetGameValue(55) / 100f);

            FightForManager.instance.NeighborsLoop(targetIndex, pos =>
            {
                FightCardData attackedUnits = FightForManager.instance.GetCard(pos, !attackUnit.isPlayerCard);
                if (attackedUnits == null || attackedUnits.Hp <= 0) return;
                AttackToEffectShow(attackedUnits, false, Effect.FireShip55A);
                var backDamage = DefDamageProcessFun(attackUnit, attackedUnits, finalDamage);
                attackedUnits.Hp.Add(-backDamage);
                TargetAnimShow(attackedUnits, backDamage);
                if (attackedUnits.cardType == 522 && 
                    attackedUnits.Hp <= 0)
                    recordWinner = attackedUnits.isPlayerCard ? -1 : 1;

                if (attackedUnits.cardType != 0 || attackedUnits.Hp <= 0) return;
                TakeToBurn(attackedUnits, takeBurnPro, attackUnit);
            });
        }
        else
        {
            PlayAudioForSecondClip(55, 0);
            ShowSpellTextObj(attackUnit.cardObj, "55", false);
            AttackToEffectShow(attackedUnit, false, Effect.FireShip55A);
        }
        TakeToBurn(attackedUnit, takeBurnPro, attackUnit);
    }

    //军师技能
    private void JunShiSkill(FightCardData attackUnit, int classType, int finalDamage)
    {
        var opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);
        var canFightUnits = new List<int>();
        for (int i = 0; i < opponents.Count; i++)
        {
            if (opponents[i] != null && opponents[i].cardType == 0 && opponents[i].Hp > 0)
            {
                canFightUnits.Add(i);
            }
        }
        //按血量从少到多排序
        canFightUnits.Sort((int a, int b) => opponents[a].Hp.Rate().CompareTo(opponents[a].Hp / opponents[b].Hp.Max));
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
            isNormalAttack = false;
            ShowSpellTextObj(attackUnit.cardObj, classType.ToString(), false);
            string effectStr = classType + "A";
            PlayAudioForSecondClip(classType, 0);

            finalDamage = (int)(DataTable.GetGameValue(63) / 100f * finalDamage);

            for (int i = 0; i < fightNums; i++)
            {
                int nowDamage = 0;
                //造成伤害
                FightCardData attackedUnit = opponents[canFightUnits[i]];
                if (attackedUnit.Hp < zhanShaXian && isSuccessFromHalfRandom(killPos))
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
                TargetAnimShow(attackedUnit, nowDamage);
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
            TakeToRemoveArmor(attackedUnit, DataTable.GetGameValue(64), attackUnit);
        }
    }

    //器械修复技能
    private void QiXieXiuFu(FightCardData attackUnit)
    {
        int fightNums = DataTable.GetGameValue(132);
        var opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);
        var canHuiFuUnits = new List<int>();
        for (int i = 0; i < opponents.Count; i++)
        {
            if (opponents[i] != null && opponents[i].cardType != 0 && opponents[i].Hp > 0 && opponents[i].Hp != opponents[i].Hp.Max)
            {
                canHuiFuUnits.Add(i);
            }
        }
        if (canHuiFuUnits.Count > 0)
        {
            isNormalAttack = false;
            if (fightNums > canHuiFuUnits.Count)
            {
                fightNums = canHuiFuUnits.Count;
            }
            canHuiFuUnits.Sort((int a, int b) =>
            {
                return opponents[a].Hp.CompareTo(opponents[b].Hp);
            });

            ShowSpellTextObj(attackUnit.cardObj, "40", false);

            int addtionNums = (int)(attackUnit.damage * (DataTable.GetGameValue(65) / 100f) / fightNums);
            for (int i = 0; i < fightNums; i++)
            {
                AttackToEffectShow(opponents[canHuiFuUnits[i]], false, Effect.Mechanical40A);
                ShowSpellTextObj(opponents[canHuiFuUnits[i]].cardObj, DataTable.GetStringText(15), true, false);
                opponents[canHuiFuUnits[i]].Hp.Add(addtionNums);
                TargetAnimShow(opponents[canHuiFuUnits[i]], addtionNums);
            }
        }
    }

    //辩士技能
    private void BianShiSkill(int fightNums, FightCardData attackUnit, int classType)
    {
        var opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);
        var canFightUnits = new List<int>();
        for (int i = 0; i < opponents.Count; i++)
        {
            if (opponents[i] != null && opponents[i].Hp > 0 && opponents[i].cardType == 0)
            {
                canFightUnits.Add(i);
            }
        }
        if (canFightUnits.Count > 0)
        {
            isNormalAttack = false;
            List<int> targets = BackRandsList(canFightUnits, fightNums);
            ShowSpellTextObj(attackUnit.cardObj, classType.ToString(), false);
            string effectStr = Effect.Debate34A;
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
            for (int i = 0; i < targets.Count; i++)
            {
                AttackToEffectShow(opponents[targets[i]], false, effectStr);
                TakeToImprisoned(opponents[targets[i]],
                    indexAttackType == 1 ?
                    DataTable.GetGameValue(66) :
                    (DataTable.GetGameValue(68) * attackUnit.level + DataTable.GetGameValue(67)),
                    attackUnit);
            }
        }
    }

    //说客技能
    private void ShuiKeSkill(int fightNums, FightCardData attackUnit, int classType)
    {
        var opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);
        List<int> canFightUnits = new List<int>();
        for (int i = 0; i < opponents.Count; i++)
        {
            if (opponents[i] != null && opponents[i].Hp > 0 && opponents[i].cardType == 0)
            {
                canFightUnits.Add(i);
            }
        }
        if (canFightUnits.Count > 0)
        {
            isNormalAttack = false;
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
                AttackToEffectShow(opponents[attackedIndexList[i]], false, effectStr);
                TakeToCowardly(opponents[attackedIndexList[i]],
                    indexAttackType == 1 ?
                    DataTable.GetGameValue(69) :
                    (DataTable.GetGameValue(71) * attackUnit.level + DataTable.GetGameValue(70)),
                    attackUnit);
            }
        }
    }

    //投石车精准技能
    private void TouShiCheSkill(int finalDamage, FightCardData attackUnit)
    {
        isNormalAttack = false;
        ShowSpellTextObj(attackUnit.cardObj, "24", false);
        PlayAudioForSecondClip(24, 0);
        var opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);
        List<int> canFightUnits = new List<int>();
        if (opponents[12] != null && opponents[12].Hp > 0)
        {
            canFightUnits.Add(12);
        }
        if (opponents[15] != null && opponents[15].Hp > 0)
        {
            canFightUnits.Add(15);
        }
        if (opponents[16] != null && opponents[16].Hp > 0)
        {
            canFightUnits.Add(16);
        }
        if (opponents[17] != null && opponents[17].Hp > 0)
        {
            canFightUnits.Add(17);
        }
        int randTarget = canFightUnits[Random.Range(0, canFightUnits.Count)];
        FightCardData attackedUnit = opponents[randTarget];
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
            TargetAnimShow(attackedUnit, finalDamage);
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
        var mgr = FightForManager.instance;
        var opponents = mgr.GetCardList(!attackUnit.isPlayerCard);
        var canFightUnits = new List<int>();
        for (int i = 0; i < opponents.Count; i++)
        {
            if (opponents[i] != null && opponents[i].Hp > 0 && opponents[i].cardType == 0)
            {
                canFightUnits.Add(i);
            }
        }
        if (canFightUnits.Count > 0)
        {
            isNormalAttack = false;
            ShowSpellTextObj(attackUnit.cardObj, classType.ToString(), false);
            string effectStr = classType + "A";
            PlayAudioForSecondClip(classType, 0);

            finalDamage = (int)(DataTable.GetGameValue(75) / 100f * finalDamage);
            int attackedNums = 0;
            for (int i = 0; i < canFightUnits.Count; i++)
            {
                var attackedUnit = opponents[canFightUnits[i]];
                AttackToEffectShow(attackedUnit, false, effectStr);
                //造成伤害
                int nowDamage = DefDamageProcessFun(attackUnit, attackedUnit, finalDamage);
                attackedUnit.Hp.Add(-nowDamage);
                TargetAnimShow(attackedUnit, nowDamage);
                //击退
                int nextPos = attackedUnit.posIndex + 5;
                if (nextPos <= 19 && opponents[nextPos] == null)
                    mgr.StartPushCardBackward(attackedUnit, nextPos, 0.2f);

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

        var opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);
        List<int> canFightUnits = new List<int>();
        for (int i = 0; i < opponents.Count; i++)
        {
            if (i != attackedUnitForIndex.posIndex && opponents[i] != null && opponents[i].Hp > 0)
            {
                canFightUnits.Add(i);
            }
        }
        isNormalAttack = false;

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
            FightCardData attackedUnit = opponents[attackedIndexList[i]];
            AttackToEffectShow(attackedUnit, false, Effect.Bow20A);
            int nowDamage = DefDamageProcessFun(attackUnit, attackedUnit, damage);
            attackedUnit.Hp.Add(-nowDamage);
            TargetAnimShow(attackedUnit, nowDamage);
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
        var opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);
        int addIndex = -1;
        float minFlo = 2;
        for (int i = 0; i < opponents.Count; i++)
        {
            if (opponents[i] != null && opponents[i].cardType == 0 && opponents[i].Hp > 0 && opponents[i].CardState.ExtendedHp < 1000)
            {
                float nowFlo = (opponents[i].Hp + opponents[i].CardState.ExtendedHp) / (float)opponents[i].Hp.Max;
                if (nowFlo < minFlo)
                {
                    minFlo = nowFlo;
                    addIndex = i;
                }
            }
        }
        if (addIndex != -1)
        {
            isNormalAttack = false;
            AddOrCutShieldValue(finalDamage, opponents[addIndex], true);
            //Debug.Log("---辅佐技能");
            ShowSpellTextObj(attackUnit.cardObj, "39", false);
            AttackToEffectShow(opponents[addIndex], false, Effect.Support39A);
            PlayAudioForSecondClip(39, 0);
        }
    }

    //内政技能
    private void NeiZhengSkill(FightCardData attackUnit)
    {
        var opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);

        int prop = DataTable.GetGameValue(127) * attackUnit.level + DataTable.GetGameValue(126);

        int cardIndex = -1;
        int cardIndex2 = -1;    //非普通攻击
        int cardIndex3 = -1;    //会心一击
        int maxBadNums = 0;
        for (int i = 0; i < opponents.Count; i++)
        {
            int nowBadNums = 0;
            if (opponents[i] != null && opponents[i].Hp > 0 && opponents[i].cardType == 0)
            {
                if (opponents[i].CardState.Stunned > 0)
                {
                    nowBadNums += opponents[i].CardState.Stunned;
                }
                if (opponents[i].CardState.Imprisoned > 0)
                {
                    nowBadNums += opponents[i].CardState.Imprisoned;
                }
                if (opponents[i].CardState.Bleed > 0)
                {
                    nowBadNums += opponents[i].CardState.Bleed;
                }
                if (opponents[i].CardState.Poison > 0)
                {
                    nowBadNums += opponents[i].CardState.Poison;
                }
                if (opponents[i].CardState.Burn > 0)
                {
                    nowBadNums += opponents[i].CardState.Burn;
                }
                if (opponents[i].CardState.Unarmed > 0)
                {
                    nowBadNums += opponents[i].CardState.Unarmed;
                }
                if (opponents[i].CardState.Cowardly > 0)
                {
                    nowBadNums += opponents[i].CardState.Cowardly;
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
            isNormalAttack = false;
            //Debug.Log("---内政技能");
            ShowSpellTextObj(attackUnit.cardObj, "38", false);
            PlayAudioForSecondClip(38, 0);
            AttackToEffectShow(opponents[cardIndex], false, Effect.StateAffairs38A);
            ShowSpellTextObj(opponents[cardIndex].cardObj, DataTable.GetStringText(14), true, false);

            if (isSuccessFromHalfRandom(prop))
            {
                ClearOneUnitBadState(opponents[cardIndex]);
            }
        }
        if (indexAttackType != 0 && cardIndex2 != -1)
        {
            cardIndex = cardIndex2;
            isNormalAttack = false;
            ShowSpellTextObj(attackUnit.cardObj, "38", false);
            PlayAudioForSecondClip(38, 0);
            AttackToEffectShow(opponents[cardIndex], false, Effect.StateAffairs38A);
            ShowSpellTextObj(opponents[cardIndex].cardObj, DataTable.GetStringText(14), true, false);
            if (isSuccessFromHalfRandom(prop))
            {
                ClearOneUnitBadState(opponents[cardIndex]);
            }

            if (indexAttackType == 1 && cardIndex3 != -1)
            {
                cardIndex = cardIndex3;
                isNormalAttack = false;
                ShowSpellTextObj(attackUnit.cardObj, "38", false);
                PlayAudioForSecondClip(38, 0);
                AttackToEffectShow(opponents[cardIndex], false, Effect.StateAffairs38A);
                ShowSpellTextObj(opponents[cardIndex].cardObj, DataTable.GetStringText(14), true, false);
                if (isSuccessFromHalfRandom(prop))
                {
                    ClearOneUnitBadState(opponents[cardIndex]);
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
            FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_dizzy, true);
        }
        if (cardData.CardState.Imprisoned > 0)
        {
            cardData.CardState.Imprisoned = 0;
            FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_imprisoned, true);
        }
        if (cardData.CardState.Bleed > 0)
        {
            cardData.CardState.Bleed = 0;
            FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_bleed, true);
        }
        if (cardData.CardState.Poison > 0)
        {
            cardData.CardState.Poison = 0;
            FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_poisoned, true);
        }
        if (cardData.CardState.Burn > 0)
        {
            cardData.CardState.Burn = 0;
            FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_burned, true);
        }
        if (cardData.CardState.Unarmed > 0)
        {
            cardData.CardState.Unarmed = 0;
            FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_removeArmor, true);
        }
        if (cardData.CardState.Cowardly > 0)
        {
            cardData.CardState.Cowardly = 0;
            FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_cowardly, true);
        }
    }

    //医生技能
    private void YiShengSkill(int fightNums, FightCardData attackUnit, int classType)
    {
        var opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);
        List<int> canHuiFuUnits = new List<int>();
        for (int i = 0; i < opponents.Count; i++)
        {
            if (opponents[i] != null && opponents[i].cardType == 0 && opponents[i].Hp > 0 && opponents[i].Hp != opponents[i].Hp.Max)
            {
                canHuiFuUnits.Add(i);
            }
        }
        if (canHuiFuUnits.Count > 0)
        {
            isNormalAttack = false;

            if (fightNums > canHuiFuUnits.Count)
            {
                fightNums = canHuiFuUnits.Count;
            }
            canHuiFuUnits.Sort((int a, int b) =>
            {
                return opponents[a].Hp.CompareTo(opponents[b].Hp);
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
                AttackToEffectShow(opponents[canHuiFuUnits[i]], false, effectStr);
                opponents[canHuiFuUnits[i]].Hp.Add(addtionNums);
                ShowSpellTextObj(opponents[canHuiFuUnits[i]].cardObj, DataTable.GetStringText(15), true, false);
                TargetAnimShow(opponents[canHuiFuUnits[i]], addtionNums);
            }
        }
    }

    //美人技能
    private void MeiRenJiNeng(FightCardData attackUnit, int classType)
    {
        var opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);
        int index = -1;
        bool isHadFirst = false;

        PlayAudioForSecondClip(classType, 0);

        int prop = DataTable.GetGameValue(129) * attackUnit.level + DataTable.GetGameValue(128);
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
            for (int i = 0; i < opponents.Count; i++)
            {
                if (opponents[i] != null && opponents[i].cardType == 0 && opponents[i].Hp > 0)
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
                            if (opponents[i].damage > opponents[index].damage)
                            {
                                if (opponents[i].CardState.Neizhu <= opponents[index].CardState.Neizhu)
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
                                if (opponents[i].CardState.Neizhu < opponents[index].CardState.Neizhu)
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
                            if (opponents[i].damage > opponents[index].damage)
                            {
                                if (opponents[i].CardState.ShenZhu <= opponents[index].CardState.ShenZhu)
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
                                if (opponents[i].CardState.ShenZhu < opponents[index].CardState.ShenZhu)
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
                isNormalAttack = false;
                ShowSpellTextObj(attackUnit.cardObj, classType.ToString(), false);
                if (isSuccessFromHalfRandom(prop))
                {
                    AttackToEffectShow(opponents[index], false, classType + "A");
                    if (classType == 45)  //美人
                    {
                        if (opponents[index].CardState.Neizhu <= 0)
                        {
                            FightForManager.instance.CreateSateIcon(opponents[index].cardObj.War.StateContent, StringNameStatic.StateIconPath_neizhu, false);
                        }
                        opponents[index].CardState.Neizhu++;
                    }
                    else
                    {//大美人
                        if (opponents[index].CardState.ShenZhu <= 0)
                        {
                            FightForManager.instance.CreateSateIcon(opponents[index].cardObj.War.StateContent, StringNameStatic.StateIconPath_shenzhu, false);
                        }
                        opponents[index].CardState.ShenZhu++;
                    }
                }
            }
        }
    }

    //谋士技能
    private void MouShiSkill(int fightNums, FightCardData attackUnit, int classType)
    {
        var opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);
        List<int> canFightUnits = new List<int>();
        for (int i = 0; i < opponents.Count; i++)
        {
            if (opponents[i] != null && opponents[i].Hp > 0 && opponents[i].cardType == 0)
            {
                canFightUnits.Add(i);
            }
        }
        if (canFightUnits.Count > 0)
        {
            isNormalAttack = false;
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
            int propNums = DataTable.GetGameValue(83) * attackUnit.level + DataTable.GetGameValue(82);
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
                AttackToEffectShow(opponents[attackedIndexList[i]], false, effectStr);
                TakeOneUnitDizzed(opponents[attackedIndexList[i]], propNums, attackUnit);
            }
        }
    }

    //毒士技能
    private void DuShiSkill(int fightNums, FightCardData attackUnit, int classType, int finalDamage)
    {
        var opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);
        List<int> canFightUnits = new List<int>();
        for (int i = 0; i < opponents.Count; i++)
        {
            if (opponents[i] != null && opponents[i].Hp > 0 && opponents[i].cardType == 0)
            {
                canFightUnits.Add(i);
            }
        }
        if (canFightUnits.Count > 0)
        {
            isNormalAttack = false;
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

            int prop = DataTable.GetGameValue(89) * attackUnit.level + DataTable.GetGameValue(88);
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
                AttackToEffectShow(opponents[attackedIndexList[i]], false, effectStr);
                TakeToPoisoned(opponents[attackedIndexList[i]], prop, attackUnit);

                int backDamage = DefDamageProcessFun(attackUnit, opponents[attackedIndexList[i]], finalDamage);
                opponents[attackedIndexList[i]].Hp.Add(-backDamage);
                TargetAnimShow(opponents[attackedIndexList[i]], backDamage);
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
            TakeToBleed(attackedUnit, DataTable.GetGameValue(147), attackUnit);
            ShowSpellTextObj(attackUnit.cardObj, DataTable.GetStringText(16), true, true);
        }
    }


    //战船冲击技能
    private int ZhanChuanChongJiAttack(int finalDamage, FightCardData attackedUnit, FightCardData attackUnit)
    {
        //使敌方武将和士兵单位往后退一格。当敌方单位无法再后退时，造成2.5倍伤害
        PlayAudioForSecondClip(21, 0);
        AttackToEffectShow(attackedUnit, false, Effect.Warship21A);
        ShowSpellTextObj(attackUnit.cardObj, "21", false);
        var mgr = FightForManager.instance;
        if (attackedUnit.cardType != 0) return finalDamage;
        var opponents = mgr.GetCardList(!attackUnit.isPlayerCard);
        var nextPos = attackedUnit.posIndex + 5;
        if (nextPos <= 19 && opponents[nextPos] == null)
            mgr.StartPushCardBackward(attackedUnit, nextPos, 0.2f);
        else
            finalDamage = (int) (finalDamage * DataTable.GetGameValue(90) / 100f);
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
        TakeOneUnitDizzed(attackedUnit, DataTable.GetGameValue(91), attackUnit);
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
            FightForManager.instance.DestroySateIcon(attackedUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_withStand, true);
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
        FightForManager.instance.NeighborsLoop(targetIndex, pos =>
        {
            var attackedUnits = FightForManager.instance.GetCard(pos, !attackUnit.isPlayerCard);
            if (attackedUnits == null || attackedUnits.Hp <= 0) return;
            int backDamage = DefDamageProcessFun(attackUnit, attackedUnits, finalDamage);
            attackedUnits.Hp.Add(-backDamage);
            TargetAnimShow(attackedUnits, backDamage);
            if (attackedUnits.cardType != 522) return;
            if (attackedUnits.Hp > 0) return;
            recordWinner = attackedUnits.isPlayerCard ? -1 : 1;
        });
    }

    //枪兵穿刺技能
    private void QiangBingChuanCiAttack(int finalDamage, FightCardData attackUnit, FightCardData attackedUnit, int classType)
    {
        var opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);
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
            if (chuanCiUnitId < 20 && opponents[chuanCiUnitId] != null && opponents[chuanCiUnitId].Hp > 0)
            {
                int backDamage = DefDamageProcessFun(attackUnit, opponents[chuanCiUnitId], finalDamage);
                opponents[chuanCiUnitId].Hp.Add(-backDamage);
                TargetAnimShow(opponents[chuanCiUnitId], backDamage);
                if (opponents[chuanCiUnitId].cardType == 522)
                {
                    if (opponents[chuanCiUnitId].Hp <= 0)
                    {
                        recordWinner = opponents[chuanCiUnitId].isPlayerCard ? -1 : 1;
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
            FightForManager.instance.CreateSateIcon(attackUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_willFight, true);
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
            attackUnit.UpdateHpUi();
            ShowSpellTextObj(attackUnit.cardObj, "10", false);
            PlayAudioForSecondClip(10, 0);
            finalDamage = (int)(finalDamage * DataTable.GetGameValue(101) / 100f);

            var opponents = FightForManager.instance.GetCardList(!attackUnit.isPlayerCard);
            for (int i = 0; i < opponents.Count; i++)
            {
                FightCardData attackedUnits = opponents[i];
                if (attackedUnits != null && attackedUnits.cardType == 0 && attackedUnits.Hp > 0)
                {
                    int backDamage = DefDamageProcessFun(attackUnit, attackedUnits, finalDamage);
                    attackedUnits.Hp.Add(-backDamage);
                    TargetAnimShow(attackedUnits, backDamage);
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
        TakeOneUnitDizzed(attackedUnit, DataTable.GetGameValue(102), attackUnit);
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
            TargetAnimShow(attackedUnit, finalDamage);
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
                FightForManager.instance.CreateSateIcon(attackedUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_deathFight, true);
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
                            FightForManager.instance.CreateSateIcon(fightCardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_invincible, true);
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
    /// <param name="defendUnit">被攻击者</param>
    /// <param name="damage">伤害值</param>
    public int DefDamageProcessFun(FightCardData attackUnit, FightCardData defendUnit, int damage)
    {
        defendUnit.attackedBehavior = 0;
        int finalDamage = damage;
        if (defendUnit.cardType == 0)
        {
            var defUnit = HeroCombatInfo.GetInfo(defendUnit.cardId);

            //判断闪避
            int dodgeRateNums = defUnit.DodgeRatio + defendUnit.CardState.DodgeUp;
            if (DataTable.Hero[defendUnit.cardId].MilitaryUnitTableId == 3 || DataTable.Hero[defendUnit.cardId].MilitaryUnitTableId == 10)   //飞甲，死士 自身血量每降低10 %，提高5%闪避
            {
                if (DataTable.Hero[defendUnit.cardId].MilitaryUnitTableId == 3)
                {
                    dodgeRateNums = dodgeRateNums + (int)((1f - defendUnit.Hp.Rate()) / (DataTable.GetGameValue(106) / 100f) * DataTable.GetGameValue(107));
                }
                else
                {
                    dodgeRateNums = dodgeRateNums + (int)((1f - defendUnit.Hp.Rate()) / (DataTable.GetGameValue(108) / 100f) * DataTable.GetGameValue(109));
                }
            }
            if (isSuccessFromHalfRandom(dodgeRateNums))
            {
                finalDamage = 0;
                ShowSpellTextObj(defendUnit.cardObj, DataTable.GetStringText(19), false);
                PlayAudioForSecondClip(97, 0);
                defendUnit.attackedBehavior = 2;
            }
            else
            {
                //远程攻击者，判断远程闪避
                if (attackUnit != null && attackUnit.cardType == 0 && attackUnit.combatType == 1 && isSuccessFromHalfRandom(defendUnit.CardState.MiWuZhenAddOn))
                {
                    finalDamage = 0;
                    ShowSpellTextObj(defendUnit.cardObj, DataTable.GetStringText(19), false);
                    PlayAudioForSecondClip(97, 0);
                    defendUnit.attackedBehavior = 2;
                }
                else
                {
                    //判断无敌
                    if (defendUnit.CardState.Invincible > 0)
                    {
                        finalDamage = 0;
                        ShowSpellTextObj(defendUnit.cardObj, DataTable.GetStringText(18), false);
                        PlayAudioForSecondClip(96, 0);
                        defendUnit.attackedBehavior = 4;
                    }
                    else
                    {
                        //判断护盾//不可抵挡法术
                        if (attackUnit != null && (attackUnit.cardType != 0 || attackUnit.cardDamageType == 0) && OffsetWithStand(defendUnit))
                        {
                            finalDamage = 0;
                            ShowSpellTextObj(defendUnit.cardObj, DataTable.GetStringText(18), false);
                            PlayAudioForSecondClip(96, 0);
                            defendUnit.attackedBehavior = 3;
                        }
                        else
                        {
                            int defPropNums = 0;
                            //免伤计算
                            if (defendUnit.CardState.Unarmed <= 0)   //是否有卸甲
                            {
                                defPropNums = defUnit.Armor + defendUnit.CardState.FengHuoTaiAddOn;
                                //白马/重甲，自身血量每降低10%，提高5%免伤
                                switch (DataTable.Hero[defendUnit.cardId].MilitaryUnitTableId)
                                {
                                    case 2:
                                        //重甲，自身血量每降低10%，提高5%免伤
                                        defPropNums = defPropNums + (int)((1f - defendUnit.Hp.Rate()) / (DataTable.GetGameValue(110) / 100f) * DataTable.GetGameValue(111));
                                        break;
                                    case 11:
                                        //白马，自身血量每降低10%，提高5%免伤
                                        defPropNums = defPropNums + (int)((1f - defendUnit.Hp.Rate()) / (DataTable.GetGameValue(112) / 100f) * DataTable.GetGameValue(113));
                                        break;
                                    case 58:
                                        //铁骑，单位越多免伤越高
                                        List<FightCardData> tieQiList = defendUnit.isPlayerCard ? tieQiCardsPy : tieQiCardsEm;
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
                                //羁绊相关免伤
                                Dictionary<int, JiBanActivedClass> jiBanAllTypes = defendUnit.isPlayerCard ? playerJiBanAllTypes : enemyJiBanAllTypes;
                                //五子良将激活时所受伤害减免30%
                                //if (jiBanAllTypes[(int)JiBanSkillName.WuZiLiangJiang].isActived)
                                //{
                                //    defPropNums = defPropNums + LoadJsonFile.GetGameValue(153);
                                //}
                                //if (LoadJsonFile.heroTableDatas[attackedUnit.cardId][6] == "3") //吴势力
                                //{
                                //    //虎踞江东激活时吴国所受伤害减免30%
                                //    if (jiBanAllTypes[(int)JiBanSkillName.HuJuJiangDong].isActived)
                                //    {
                                //        defPropNums = defPropNums + LoadJsonFile.GetGameValue(157);
                                //    }
                                //    //天作之合激活时吴国所受伤害减免30%
                                //    if (jiBanAllTypes[(int)JiBanSkillName.TianZuoZhiHe].isActived)
                                //    {
                                //        defPropNums = defPropNums + LoadJsonFile.GetGameValue(158);
                                //    }
                                //}

                                defPropNums = defPropNums > DataTable.GetGameValue(116) ? DataTable.GetGameValue(116) : defPropNums;

                                //判断攻击者的伤害类型，获得被攻击者的物理或法术免伤百分比
                                if (attackUnit?.cardDamageType == 0)
                                {
                                    defPropNums = defPropNums + defUnit.PhysicalResist;
                                }
                                else
                                {
                                    defPropNums = defPropNums + defUnit.MagicResist;
                                }
                                defPropNums = Mathf.Min(defPropNums, 100);
                                finalDamage = (int)((100f - defPropNums) / 100f * finalDamage);
                            }

                            //流血状态加成
                            if (defendUnit.CardState.Bleed > 0)
                            {
                                finalDamage = (int)(finalDamage * DataTable.GetGameValue(117) / 100f);
                            }
                            //抵扣防护盾
                            finalDamage = AddOrCutShieldValue(finalDamage, defendUnit, false);
                        }
                    }
                }
            }
            if (defendUnit.cardType == 0)
            {
                switch (DataTable.Hero[defendUnit.cardId].MilitaryUnitTableId)
                {
                    case 1:
                        //近战兵种受到暴击和会心加盾
                        if (defendUnit.CardState.Stunned <= 0 && defendUnit.CardState.Imprisoned <= 0)
                        {
                            if (indexAttackType != 0)
                            {
                                if (defendUnit.CardState.Shield <= 0)
                                {
                                    FightForManager.instance.CreateSateIcon(defendUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_withStand, true);
                                }
                                defendUnit.CardState.Shield++;
                            }
                        }
                        break;
                    case 12:
                        //神武战意技能
                        if (defendUnit.CardState.Stimulate <= 0)
                        {
                            FightForManager.instance.CreateSateIcon(defendUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_willFight, true);
                        }
                        if (defendUnit.CardState.Stimulate < 10)
                        {
                            defendUnit.CardState.Stimulate++;
                            defendUnit.cardObj.transform.Find(StringNameStatic.StateIconPath_willFight + "Din").GetComponent<Image>().color = new Color(1, 1, 1, 0.4f + 0.6f * (defendUnit.CardState.Stimulate / 10f));
                        }
                        ShowSpellTextObj(defendUnit.cardObj, "12", false);
                        break;
                    case 41:
                        //敢死死战技能
                        finalDamage = GanSiSiZhanAttack(finalDamage, defendUnit);
                        break;
                    default:
                        break;
                }
                if (attackUnit?.CardState.Stunned <= 0 && attackUnit.CardState.Imprisoned <= 0)
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
                                TargetAnimShow(attackUnit, addHp);
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
                FightForManager.instance.DestroySateIcon(fightCardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_withStand, true);
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
                FightForManager.instance.DestroySateIcon(dizzyUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_dizzy, true);
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
                FightForManager.instance.DestroySateIcon(attackUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_neizhu, false);
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
                FightForManager.instance.DestroySateIcon(attackUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_shenzhu, false);
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// 获得武将免疫减益效果成功几率
    /// </summary>
    /// <param name="cardData"></param>
    /// <returns></returns>
    private int GetCardDebuffSuccessRate(FightCardData cardData)
    {
        return HeroCombatInfo.GetInfo(cardData.cardId).ConditionResist;
        ////羁绊相关减益
        //Dictionary<int, JiBanActivedClass> jiBanAllTypes = cardData.isPlayerCard ? playerJiBanAllTypes : enemyJiBanAllTypes;
        ////魏五奇谋激活时武将减益成功率提升10%
        //if (jiBanAllTypes[(int)JiBanSkillName.WuZiLiangJiang].isActived)
        //{
        //    defPropNums = defPropNums + LoadJsonFile.GetGameValue(153);
        //}
    }

    /// <summary>
    /// 获得武将减益效果成功几率
    /// </summary>
    /// <param name="cardData"></param>
    /// <returns></returns>
    private int GetCardTakeDebuffSuccessRate(FightCardData cardData, int prop)
    {
        int props = prop;
        //羁绊相关减益
        Dictionary<int, JiBanActivedClass> jiBanAllTypes = cardData.isPlayerCard ? playerJiBanAllTypes : enemyJiBanAllTypes;
        //魏五奇谋激活时武将减益成功率提升10%
        if (jiBanAllTypes[(int)JiBanSkillName.WeiWuMouShi].IsActive)
        {
            props += DataTable.GetGameValue(156);
        }
        return props;
    }

    //触发眩晕
    private void TakeOneUnitDizzed(FightCardData attackedUnit, int prob, FightCardData attackUnit = null)
    {
        if (attackUnit != null)
            prob = GetCardTakeDebuffSuccessRate(attackUnit, prob);
        if (attackedUnit.cardType == 0 && isSuccessFromHalfRandom(prob))
        {
            //判断免疫负面状效果触发
            if (!isSuccessFromHalfRandom(GetCardDebuffSuccessRate(attackedUnit)))
            {
                if (attackedUnit.CardState.Stunned <= 0)
                {
                    FightForManager.instance.CreateSateIcon(attackedUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_dizzy, true);
                }
                attackedUnit.CardState.Stunned++;
            }
        }
    }

    //触发禁锢
    private void TakeToImprisoned(FightCardData attackedUnit, int prob, FightCardData attackUnit = null)
    {
        if (attackUnit != null)
            prob = GetCardTakeDebuffSuccessRate(attackUnit, prob);
        if (attackedUnit.cardType == 0 && isSuccessFromHalfRandom(prob))
        {
            if (!isSuccessFromHalfRandom(GetCardDebuffSuccessRate(attackedUnit)))
            {
                if (attackedUnit.CardState.Imprisoned <= 0)
                {
                    FightForManager.instance.CreateSateIcon(attackedUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_imprisoned, true);
                }
                attackedUnit.CardState.Imprisoned += 1;
                ShowSpellTextObj(attackedUnit.cardObj, DataTable.GetStringText(11), true, true);
            }
        }
    }

    //触发卸甲
    private void TakeToRemoveArmor(FightCardData attackedUnit, int prob, FightCardData attackUnit = null)
    {
        if (attackUnit != null)
            prob = GetCardTakeDebuffSuccessRate(attackUnit, prob);
        if (attackedUnit.cardType == 0 && isSuccessFromHalfRandom(prob))
        {
            if (!isSuccessFromHalfRandom(GetCardDebuffSuccessRate(attackedUnit)))
            {
                if (attackedUnit.CardState.Unarmed <= 0)
                {
                    FightForManager.instance.CreateSateIcon(attackedUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_removeArmor, true);
                }
                attackedUnit.CardState.Unarmed += 1;
            }
        }
    }

    //触发流血
    private void TakeToBleed(FightCardData attackedUnit, int prob, FightCardData attackUnit = null)
    {
        if (attackUnit != null)
            prob = GetCardTakeDebuffSuccessRate(attackUnit, prob);
        if (attackedUnit.cardType == 0 && isSuccessFromHalfRandom(prob))
        {
            if (!isSuccessFromHalfRandom(GetCardDebuffSuccessRate(attackedUnit)))
            {
                if (attackedUnit.CardState.Bleed <= 0)
                {
                    FightForManager.instance.CreateSateIcon(attackedUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_bleed, true);
                }
                attackedUnit.CardState.Bleed++;
                ShowSpellTextObj(attackedUnit.cardObj, DataTable.GetStringText(16), true, true);
            }
        }
    }

    //触发中毒
    private void TakeToPoisoned(FightCardData attackedUnit, int prob, FightCardData attackUnit = null)
    {
        if (attackUnit != null)
            prob = GetCardTakeDebuffSuccessRate(attackUnit, prob);
        if (attackedUnit.cardType == 0 && isSuccessFromHalfRandom(prob))
        {
            if (!isSuccessFromHalfRandom(GetCardDebuffSuccessRate(attackedUnit)))
            {
                if (attackedUnit.CardState.Poison <= 0)
                {
                    FightForManager.instance.CreateSateIcon(attackedUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_poisoned, true);
                }
                attackedUnit.CardState.Poison++;
                ShowSpellTextObj(attackedUnit.cardObj, DataTable.GetStringText(12), true, true);
            }
        }
    }

    //触发灼烧
    private void TakeToBurn(FightCardData attackedUnit, int prob, FightCardData attackUnit = null)
    {
        if (attackUnit != null)
            prob = GetCardTakeDebuffSuccessRate(attackUnit, prob);
        if (attackedUnit.cardType == 0 && isSuccessFromHalfRandom(prob))
        {
            if (!isSuccessFromHalfRandom(GetCardDebuffSuccessRate(attackedUnit)))
            {
                if (attackedUnit.CardState.Burn <= 0)
                {
                    FightForManager.instance.CreateSateIcon(attackedUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_burned, true);
                }
                attackedUnit.CardState.Burn++;
                ShowSpellTextObj(attackedUnit.cardObj, DataTable.GetStringText(20), true, true);
            }
        }
    }

    //触发怯战
    private void TakeToCowardly(FightCardData attackedUnit, int prob, FightCardData attackUnit = null)
    {
        if (attackUnit != null)
            prob = GetCardTakeDebuffSuccessRate(attackUnit, prob);
        if (attackedUnit.cardType == 0 && isSuccessFromHalfRandom(prob))
        {
            if (!isSuccessFromHalfRandom(GetCardDebuffSuccessRate(attackedUnit)))
            {
                if (attackedUnit.CardState.Cowardly <= 0)
                {
                    FightForManager.instance.CreateSateIcon(attackedUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_cowardly, true);
                }
                attackedUnit.CardState.Cowardly += 1;
                ShowSpellTextObj(attackedUnit.cardObj, DataTable.GetStringText(21), true, true);
            }
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
            TargetAnimShow(cardData, cutHpNum);
        }

        cardData.CardState.Burn--;
        PlayAudioForSecondClip(87, 0);
        if (cardData.CardState.Burn <= 0)
        {
            cardData.CardState.Burn = 0;
            FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_burned, true);
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
                    FightForManager.instance.CreateSateIcon(attackUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_shield, true);
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
                            FightForManager.instance.DestroySateIcon(attackUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_shield, true);
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
    /// <param name="target"></param>
    /// <param name="isPuGong"></param>
    /// <param name="effectName"></param>
    /// <returns></returns>
    public GameObject AttackToEffectShow(FightCardData target, bool isPuGong, string effectName = "")
    {
        GameObject effectObj = new GameObject();
        PlayerDataForGame.garbageStationObjs.Add(effectObj);

        if (isPuGong)
        {
            effectObj = EffectsPoolingControl.instance.GetVTextEffect(Effect.Basic0A, 0.5f, target.cardObj);
            effectObj.transform.localEulerAngles = new Vector3(0, 0, Random.Range(0, 360));
        }
        else
        {
            effectObj = EffectsPoolingControl.instance.GetVTextEffect(effectName, 1f, target.cardObj);
        }

        effectObj.transform.localScale = indexAttackType != 0 ? new Vector3(effectObj.transform.localScale.x * 1.5f, effectObj.transform.localScale.y * 1.5f, effectObj.transform.localScale.z * 1.5f) 
            : Vector3.one;

        return effectObj;
    }

    /// <summary>
    /// 被击表现
    /// </summary>
    /// <param name="target"></param>
    /// <param name="hpSubtract"></param>
    /// <param name="isAdd"></param>
    public void TargetAnimShow(FightCardData target, int hpSubtract)
    {
        var isAdd = hpSubtract < 0;
        GameObject effectObj = new GameObject();
        PlayerDataForGame.garbageStationObjs.Add(effectObj);

        effectObj = EffectsPoolingControl.instance.GetVTextEffect(Effect.DropBlood, 1.5f, target.cardObj);
        var effectText = (isAdd ? "+" : "-") + Mathf.Abs(hpSubtract);
        effectObj.GetComponentInChildren<Text>().text = effectText;
        effectObj.GetComponentInChildren<Text>().color = isAdd ? ColorDataStatic.huiFu_green : Color.red;
        if (isAdd)
        {
            UpdateUiStates();
            return;
        }

        if (hpSubtract > 0) target.cardObj.transform.DOShakePosition(0.3f, new Vector3(10, 20, 10));

        if (indexAttackType == 0)
        {
            effectObj.transform.localScale = Vector3.one;
            UpdateUiStates();
            return;
        }

        effectObj.transform.localScale = new Vector3(effectObj.transform.localScale.x * 1.3f,
            effectObj.transform.localScale.y * 1.3f, effectObj.transform.localScale.z * 1.3f);
        var vec3 = fightBackForShake.transform.position;
        if (indexAttackType == 1) //会心
        {
            ShowSpellTextObj(target.cardObj, DataTable.GetStringText(22), true);
            fightBackForShake.transform.DOShakePosition(0.25f, doShakeIntensity).OnComplete(() => fightBackForShake.transform.position = vec3);
        }
        else //暴击
        {
            ShowSpellTextObj(target.cardObj, DataTable.GetStringText(23), true);
            fightBackForShake.transform.DOShakePosition(0.25f, doShakeIntensity).OnComplete(() => fightBackForShake.transform.position = vec3);
        }

        UpdateUiStates();
        void UpdateUiStates()
        {
            //更新血条
            target.UpdateHpUi();
            InitCardAfterFightedState(target);
        }
    }

    //特殊单位普攻特效
    private void SpecilSkillNeedPuGongFun(FightCardData attackedUnit)
    {
        if (isNormalAttack)
        {
            PlayAudioForSecondClip(0, 0);
            AttackToEffectShow(attackedUnit, true);
        }
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
            effectObj = EffectsPoolingControl.instance.GetVTextEffect(Effect.SpellTextH, 1.5f, ui);
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
            effectObj = EffectsPoolingControl.instance.GetVTextEffect(Effect.SpellTextV, 1.5f, ui);
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
                Transform obj = FightForManager.instance.enemyCardsPos[GoalGfSetFireRound[tongShuaiBurnRoundPy][i]].transform.Find(StringNameStatic.StateIconPath_burned);
                if (obj != null)
                    Destroy(obj.gameObject);
            }
            tongShuaiBurnRoundPy = -1;
        }
        if (tongShuaiBurnRoundEm != -1)
        {
            for (int i = 0; i < GoalGfSetFireRound[tongShuaiBurnRoundEm].Length; i++)
            {
                Transform obj = FightForManager.instance.playerCardsPos[GoalGfSetFireRound[tongShuaiBurnRoundEm][i]].transform.Find(StringNameStatic.StateIconPath_burned);
                if (obj != null)
                    Destroy(obj.gameObject);
            }
            tongShuaiBurnRoundEm = -1;
        }

        FightCardData card;
        //我方
        var playerCards = FightForManager.instance.GetCardList(true);
        for (int i = 0; i < playerCards.Count; i++)
        {
            card = playerCards[i];
            if (i != 17 && card != null && card.Hp > 0)
            {
                int addtionNums = (int)(card.Hp.Max * card.hpr / 100f);
                card.Hp.Add(addtionNums);
                ShowSpellTextObj(card.cardObj, DataTable.GetStringText(15), true, false);
                TargetAnimShow(card, addtionNums);
                if (card.cardType == 0)
                {
                    OnClearCardStateUpdate(card);  //消除所有状态
                }
            }
        }
    }

    //播放特殊音效
    public void PlayAudioForSecondClip(int clipIndex, float delayedTime)
    {
        var clip = WarsUIManager.instance.audioClipsFightEffect[clipIndex];
        var volume = WarsUIManager.instance.audioVolumeFightEffect[clipIndex];
        if (AudioController0.instance.ChangeAudioClip(clip, volume))
        {
            AudioController0.instance.PlayAudioSource(0);
            return;
        }

        AudioController0.instance.audioSource.volume *= 0.75f;
        audioSource.clip = WarsUIManager.instance.audioClipsFightEffect[clipIndex];
        audioSource.volume = WarsUIManager.instance.audioVolumeFightEffect[clipIndex];
        if (!GamePref.PrefMusicPlay)
            return;
        audioSource.PlayDelayed(delayedTime);
    }


    //计算是否触发特殊攻击状态
    private bool isSuccessFromHalfRandom(int odds) => Random.Range(1, 101) <= odds;

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
    [SerializeField]
    Transform[] jBEffectShowPos;    //0敌方1我方位置

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
        var list = FightForManager.instance.GetCardList(true);
        for (int i = 0; i < list.Count; i++)
        {
            fightCardData = list[i];
            if (i != 17 && fightCardData != null && fightCardData.cardType == 0 && fightCardData.Hp > 0)
            {
                switch (DataTable.Hero[fightCardData.cardId].MilitaryUnitTableId)
                {
                    case 4://盾兵
                        if (fightCardData.CardState.Shield <= 0)
                        {
                            FightForManager.instance.CreateSateIcon(fightCardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_withStand, true);
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
            PlayAudioForSecondClip(93, 0);

            isFirstRound = false;
            StartBattle();
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
                StartBattle();
                timer = 0;
            }
            roundTimeSlider.value = 1 - timer / roundTime;
        }
    }

    //锁定玩家上场卡牌
    private void LockFightCardState()
    {
        var list = FightForManager.instance.GetCardList(true);
        for (int i = 0; i < list.Count; i++)
        {
            if (i != 17 && list[i] != null)
            {
                if (!list[i].cardObj.DragComponent.IsLocked)
                {
                    list[i].cardObj.DragComponent.IsLocked = true;
                    list[i].cardObj.SetSelected(false);
                }
            }
        }
    }

    /// <summary>
    /// 回合开始方法
    /// </summary>
    private void StartBattle()
    {
        timer = 0;
        isRoundBegin = true;
        roundTimeSlider.fillRect.GetComponent<Image>().color = Color.white;
        roundTimeSlider.gameObject.SetActive(false);
        startFightBtn.GetComponent<Button>().interactable = false;
        startFightBtn.GetComponent<Animator>().SetBool("isShow", false);
        LockFightCardState();
        StartCoroutine(RoundStart());
    }

    //等待战鼓消失动画结束后,回合开始数据重置
    IEnumerator RoundStart()
    {
        yield return new WaitForSeconds(1f);
        roundNums++;
        targetIndex = -1;
        ChessPosIndex = 0;
        isPlayerRound = true;
        timerForFight = 0;
        stateOfFight = StateOfFight.ReadyForFight;

        float updateStateTime = 0;  //刷新状态需等待的时间
        updateStateTime = UpdateCardDataBeforeRound();
        yield return new WaitForSeconds(updateStateTime);

        //羁绊部分的战前附加
        yield return StartCoroutine(InitJiBanForStartFight());

        ChessPosAction();
    }

    /// <summary>
    /// 战斗开始羁绊数据附加到卡牌上
    /// </summary>
    IEnumerator InitJiBanForStartFight()
    {
        float waitTime = 0;
        //玩家部分
        foreach (var item in playerJiBanAllTypes)
        {
            if (item.Value.IsActive)
            {
                //Debug.Log("玩家触发羁绊： " + item.Value.jiBanIndex);
                ShowAllScreenFightEffect(FullScreenEffectName.JiBanEffect, item.Value.JiBanId);
                yield return new WaitForSeconds(1f);//羁绊名动图和羁绊全屏特效等待时长
                waitTime = JiBanAddStateForCard(item.Value, true);//羁绊加状态前等待
                yield return new WaitForSeconds(waitTime);
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
                waitTime = JiBanAddStateForCard(item.Value, false);
                yield return new WaitForSeconds(waitTime);
            }
        }
    }

    //给卡牌上附加羁绊属性
    private float JiBanAddStateForCard(JiBanActivedClass jiBanActivedClass, bool isPlayer)
    {
        Debug.Log(isPlayer);
        FightCardData fightCardData;
        var enemyCards = FightForManager.instance.GetCardList(!isPlayer);
        var waitTime = 0f;
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
                            if (isSuccessFromHalfRandom(DataTable.GetGameValue(134)))
                            {
                                if (fightCardData.CardState.ShenZhu <= 0)
                                {
                                    FightForManager.instance.CreateSateIcon(fightCardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_shenzhu, false);
                                }
                                fightCardData.CardState.ShenZhu++;
                            }
                        }
                    }
                }
                waitTime = 1f;
                break;
            case JiBanSkillName.WuHuShangJiang:
                //对敌方全体武将造成一次物理攻击（平均*0.5），并有75%概率造成【怯战】
                PlayAudioForSecondClip(101, 0);
                fullScreenEffectObjs[2].SetActive(false);
                fullScreenEffectObjs[2].transform.position = jBEffectShowPos[isPlayer ? 0 : 1].position;
                fullScreenEffectObjs[2].SetActive(true);

                int damage = 0; //记录总伤害
                int heroNums = 0;
                for (int i = 0; i < jiBanActivedClass.List.Count; i++)
                {
                    for (int j = 0; j < jiBanActivedClass.List[i].Cards.Count; j++)
                    {
                        fightCardData = jiBanActivedClass.List[i].Cards[j];
                        if (fightCardData != null && fightCardData.Hp > 0)
                        {
                            damage += fightCardData.damage;
                            heroNums++;
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                        }
                    }
                }
                damage = (int)(damage * DataTable.GetGameValue(149) / heroNums / 100f);
                for (int i = 0; i < enemyCards.Count; i++)
                {
                    if (enemyCards[i] != null && enemyCards[i].cardType == 0 && enemyCards[i].Hp > 0)
                    {
                        int nowDamage = DefDamageProcessFun(null, enemyCards[i], damage);
                        enemyCards[i].Hp.Add(-nowDamage);
                        AttackToEffectShow(enemyCards[i], true);
                        TargetAnimShow(enemyCards[i], nowDamage);
                        TakeToCowardly(enemyCards[i], DataTable.GetGameValue(150));
                    }
                }
                waitTime = 2f;
                break;
            case JiBanSkillName.WoLongFengChu:
                //概率分别为羁绊武将增加1层【神助】
                for (int i = 0; i < jiBanActivedClass.List.Count; i++)
                {
                    for (int j = 0; j < jiBanActivedClass.List[i].Cards.Count; j++)
                    {
                        fightCardData = jiBanActivedClass.List[i].Cards[j];
                        if (fightCardData != null && fightCardData.Hp > 0)
                        {
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                            if (isSuccessFromHalfRandom(DataTable.GetGameValue(136)))
                            {
                                if (fightCardData.CardState.ShenZhu <= 0)
                                {
                                    FightForManager.instance.CreateSateIcon(fightCardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_shenzhu, false);
                                }
                                fightCardData.CardState.ShenZhu++;
                            }
                        }
                    }
                }
                waitTime = 1f;
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
                            if (isSuccessFromHalfRandom(DataTable.GetGameValue(137)))
                            {
                                if (fightCardData.CardState.Shield <= 0)
                                {
                                    FightForManager.instance.CreateSateIcon(fightCardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_withStand, true);
                                }
                                fightCardData.CardState.Shield++;
                            }
                        }
                    }
                }
                waitTime = 1f;
                break;
            case JiBanSkillName.WuZiLiangJiang:
                PlayAudioForSecondClip(104, 0);
                fullScreenEffectObjs[5].SetActive(false);
                fullScreenEffectObjs[5].transform.position = jBEffectShowPos[isPlayer ? 1 : 0].position;
                fullScreenEffectObjs[5].SetActive(true);
                //30 % 概率分别为羁绊武将增加1层【护盾】
                for (int i = 0; i < jiBanActivedClass.List.Count; i++)
                {
                    for (int j = 0; j < jiBanActivedClass.List[i].Cards.Count; j++)
                    {
                        fightCardData = jiBanActivedClass.List[i].Cards[j];
                        if (fightCardData != null && fightCardData.Hp > 0)
                        {
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                            if (isSuccessFromHalfRandom(DataTable.GetGameValue(138)))
                            {
                                if (fightCardData.CardState.Shield <= 0)
                                {
                                    FightForManager.instance.CreateSateIcon(fightCardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_withStand, true);
                                }
                                fightCardData.CardState.Shield++;
                            }
                        }
                    }
                }
                waitTime = 1f;
                break;
            case JiBanSkillName.WeiWuMouShi:
                //对敌方全体武将造成一次物理攻击（平均*0.5），并有20%概率造成【眩晕】
                PlayAudioForSecondClip(103, 0);
                fullScreenEffectObjs[4].SetActive(false);
                fullScreenEffectObjs[4].transform.position = jBEffectShowPos[isPlayer ? 0 : 1].position;
                fullScreenEffectObjs[4].SetActive(true);

                int damage1 = 0; //记录总伤害
                int heroNums1 = 0;
                for (int i = 0; i < jiBanActivedClass.List.Count; i++)
                {
                    for (int j = 0; j < jiBanActivedClass.List[i].Cards.Count; j++)
                    {
                        fightCardData = jiBanActivedClass.List[i].Cards[j];
                        if (fightCardData != null && fightCardData.Hp > 0)
                        {
                            damage1 += fightCardData.damage;
                            heroNums1++;
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                        }
                    }
                }
                damage1 = (int)(damage1 * DataTable.GetGameValue(154) / heroNums1 / 100f);
                for (int i = 0; i < enemyCards.Count; i++)
                {
                    if (enemyCards[i] != null && enemyCards[i].cardType == 0 && enemyCards[i].Hp > 0)
                    {
                        int nowDamage = DefDamageProcessFun(null, enemyCards[i], damage1);
                        enemyCards[i].Hp.Add(-nowDamage);
                        AttackToEffectShow(enemyCards[i], true);
                        TargetAnimShow(enemyCards[i], nowDamage);
                        TakeOneUnitDizzed(enemyCards[i], DataTable.GetGameValue(155));
                    }
                }
                waitTime = 2f;
                break;
            case JiBanSkillName.HuJuJiangDong:
                //自身30%概率获得【神助】
                for (int i = 0; i < jiBanActivedClass.List.Count; i++)
                {
                    for (int j = 0; j < jiBanActivedClass.List[i].Cards.Count; j++)
                    {
                        fightCardData = jiBanActivedClass.List[i].Cards[j];
                        if (fightCardData != null && fightCardData.Hp > 0)
                        {
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                            if (isSuccessFromHalfRandom(DataTable.GetGameValue(140)))
                            {
                                if (fightCardData.CardState.ShenZhu <= 0)
                                {
                                    FightForManager.instance.CreateSateIcon(fightCardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_shenzhu, false);
                                }
                                fightCardData.CardState.ShenZhu++;
                            }
                        }
                    }
                }
                waitTime = 1f;
                break;
            case JiBanSkillName.ShuiShiDuDu:
                //对敌方全体武将造成一次隐士攻击（平均*0.25）
                PlayAudioForSecondClip(102, 0);
                fullScreenEffectObjs[3].SetActive(false);
                fullScreenEffectObjs[3].transform.position = jBEffectShowPos[isPlayer ? 0 : 1].position;
                fullScreenEffectObjs[3].SetActive(true);

                int damage2 = 0; //记录总伤害
                int heroNums2 = 0;
                for (int i = 0; i < jiBanActivedClass.List.Count; i++)
                {
                    for (int j = 0; j < jiBanActivedClass.List[i].Cards.Count; j++)
                    {
                        fightCardData = jiBanActivedClass.List[i].Cards[j];
                        if (fightCardData != null && fightCardData.Hp > 0)
                        {
                            damage2 += fightCardData.damage;
                            heroNums2++;
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                        }
                    }
                }
                damage2 = (int)(damage2 * DataTable.GetGameValue(159) / heroNums2 / 100f);
                for (int i = 0; i < enemyCards.Count; i++)
                {
                    if (enemyCards[i] != null && enemyCards[i].cardType == 0 && enemyCards[i].Hp > 0)
                    {
                        int nowDamage = DefDamageProcessFun(null, enemyCards[i], damage2);
                        enemyCards[i].Hp.Add(-nowDamage);
                        AttackToEffectShow(enemyCards[i], true);
                        TargetAnimShow(enemyCards[i], nowDamage);
                        //击退
                        int nextPos = enemyCards[i].posIndex + 5;
                        if (nextPos <= 19 && enemyCards[nextPos] == null)
                            FightForManager.instance.StartPushCardBackward(enemyCards[i], nextPos, 0.2f);
                    }
                }
                waitTime = 2f;
                break;
            case JiBanSkillName.TianZuoZhiHe:
                //40%概率分别为羁绊武将增加1层【神助】
                for (int i = 0; i < jiBanActivedClass.List.Count; i++)
                {
                    for (int j = 0; j < jiBanActivedClass.List[i].Cards.Count; j++)
                    {
                        fightCardData = jiBanActivedClass.List[i].Cards[j];
                        if (fightCardData != null && fightCardData.Hp > 0)
                        {
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                            if (isSuccessFromHalfRandom(DataTable.GetGameValue(142)))
                            {
                                if (fightCardData.CardState.ShenZhu <= 0)
                                {
                                    FightForManager.instance.CreateSateIcon(fightCardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_shenzhu, false);
                                }
                                fightCardData.CardState.ShenZhu++;
                            }
                        }
                    }
                }
                waitTime = 1f;
                break;
            case JiBanSkillName.HeBeiSiTingZhu:

                PlayAudioForSecondClip(104, 0);
                fullScreenEffectObjs[6].SetActive(false);
                fullScreenEffectObjs[6].transform.position = jBEffectShowPos[isPlayer ? 1 : 0].position;
                fullScreenEffectObjs[6].SetActive(true);
                //30%概率分别为羁绊武将增加1层【神助】
                for (int i = 0; i < jiBanActivedClass.List.Count; i++)
                {
                    for (int j = 0; j < jiBanActivedClass.List[i].Cards.Count; j++)
                    {
                        fightCardData = jiBanActivedClass.List[i].Cards[j];
                        if (fightCardData != null && fightCardData.Hp > 0)
                        {
                            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.JiBanId);
                            if (isSuccessFromHalfRandom(DataTable.GetGameValue(143)))
                            {
                                if (fightCardData.CardState.ShenZhu <= 0)
                                {
                                    FightForManager.instance.CreateSateIcon(fightCardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_shenzhu, false);
                                }
                                fightCardData.CardState.ShenZhu++;
                            }
                        }
                    }
                }
                waitTime = 1f;
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
                            if (isSuccessFromHalfRandom(DataTable.GetGameValue(144)))
                            {
                                if (fightCardData.CardState.ShenZhu <= 0)
                                {
                                    FightForManager.instance.CreateSateIcon(fightCardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_shenzhu, false);
                                }
                                fightCardData.CardState.ShenZhu++;
                            }
                        }
                    }
                }
                waitTime = 1f;
                break;
            case JiBanSkillName.HanMoSanXian:
                ////30%概率分别为羁绊武将增加1层【神助】
                //for (int i = 0; i < jiBanActivedClass.cardTypeLists.Count; i++)
                //{
                //    for (int j = 0; j < jiBanActivedClass.cardTypeLists[i].cardLists.Count; j++)
                //    {
                //        fightCardData = jiBanActivedClass.cardTypeLists[i].cardLists[j];
                //        if (fightCardData != null && fightCardData.Hp > 0)
                //        {
                //            AttackToEffectShow(fightCardData, false, "JB" + jiBanActivedClass.jiBanIndex);
                //            if (TakeSpecialAttack(LoadJsonFile.GetGameValue(145)))
                //            {
                //                if (fightCardData.fightState.shenzhuNums <= 0)
                //                {
                //                    FightForManager.instance.CreateSateIcon(fightCardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_shenzhu, false);
                //                }
                //                fightCardData.fightState.shenzhuNums++;
                //            }
                //        }
                //    }
                //}
                waitTime = 0f;
                break;
            default:
                break;
        }
        return waitTime;
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
        var playerCards = FightForManager.instance.GetCardList(true);
        var enemyCards = FightForManager.instance.GetCardList(false);
        for (int i = 0; i < playerCards.Count; i++)
        {
            if (playerCards[i] != null && playerCards[i].Hp > 0)
            {
                playerCards[i].isActionDone = false;
                if (playerCards[i].cardType == 0)
                {
                    nowRounds = UpdateOneCardBeforeRound(playerCards[i]);
                    var military = DataTable.Hero[playerCards[i].cardId].MilitaryUnitTableId;
                    //是否有统帅
                    isHadTongShuai = playerCards[i].Hp > 0 &&
                                     (military == 32 || military == 33);
                }
                else
                {
                    if (playerCards[i].cardType == 2)
                    {
                        switch (playerCards[i].cardId)
                        {
                            case 0:
                                //营寨回合开始回血
                                int addHp = (int)(DataTable.GetGameValue(120) / 100f * playerCards[i].Hp.Max);
                                playerCards[i].Hp.Add(addHp);
                                TargetAnimShow(playerCards[i], addHp);
                                break;
                            //case 17:
                            //    //迷雾阵迷雾动画开启
                            //    for (int j = 0; j < playerFightCardsDatas[i].cardObj.transform.childCount; j++)
                            //    {
                            //        if (playerFightCardsDatas[i].cardObj.transform.GetChild(j).name == StringNameStatic.StateIconPath_miWuZhenAddtion + "Din")
                            //        {
                            //            if (!playerFightCardsDatas[i].cardObj.transform.GetChild(j).GetComponent<Animator>().enabled)
                            //            {
                            //                playerFightCardsDatas[i].cardObj.transform.GetChild(j).GetComponent<Animator>().enabled = true;
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
                if (playerCards[i].cardType == 3)
                {
                    if (playerCards[i].cardId == 9)
                    {
                        gunShiCards.Add(playerCards[i]);
                    }
                    if (playerCards[i].cardId == 10)
                    {
                        gunMuCards.Add(playerCards[i]);
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
                    Transform obj = FightForManager.instance.enemyCardsPos[GoalGfSetFireRound[tongShuaiBurnRoundPy][i]].transform.Find(StringNameStatic.StateIconPath_burned);
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

        for (int i = 0; i < enemyCards.Count; i++)
        {
            if (enemyCards[i] != null && enemyCards[i].Hp > 0)
            {
                enemyCards[i].isActionDone = false;
                if (enemyCards[i].cardType == 0)
                {
                    nowRounds = UpdateOneCardBeforeRound(enemyCards[i]);
                    var military = DataTable.Hero[enemyCards[i].cardId]
                        .MilitaryUnitTableId;
                    //是否有统帅
                    isHadTongShuai = enemyCards[i].Hp > 0 &&
                                     (military == 32 || military == 33);
                }
                else
                {
                    if (enemyCards[i].cardType == 2)
                    {
                        switch (enemyCards[i].cardId)
                        {
                            case 0:
                                //营寨回合开始回血
                                int addHp = (int)(DataTable.GetGameValue(120) / 100f * enemyCards[i].Hp.Max);
                                enemyCards[i].Hp.Add(addHp);
                                TargetAnimShow(enemyCards[i], addHp);
                                break;
                            case 17:
                                //迷雾阵迷雾动画开启
                                Transform tran = enemyCards[i].cardObj.transform;
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
                                Transform tran0 = enemyCards[i].cardObj.transform;
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
                if (enemyCards[i].cardType == 3)
                {
                    if (enemyCards[i].cardId == 9)
                    {
                        gunShiCards.Add(enemyCards[i]);
                    }
                    if (enemyCards[i].cardId == 10)
                    {
                        gunMuCards.Add(enemyCards[i]);
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
                    Transform obj = FightForManager.instance.playerCardsPos[GoalGfSetFireRound[tongShuaiBurnRoundEm][i]].transform.Find(StringNameStatic.StateIconPath_burned);
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
                TargetAnimShow(cardData, cutHpNum);
            }

            cardData.CardState.Poison--;
            if (cardData.CardState.Poison <= 0)
            {
                cardData.CardState.Poison = 0;
                FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_poisoned, true);
            }
        }
        return showRounds;
    }


    //回合结束更新卡牌特殊状态
    private void UpdateRoundState()
    {
        var playerCards = FightForManager.instance.GetCardList(true);
        var mgr = FightForManager.instance;
        //我方
        for (int i = 0; i < playerCards.Count; i++)
        {
            if (i == 17 || playerCards[i] == null) continue;
            if (playerCards[i].Hp > 0)
            {
                OnCardStateUpdate(playerCards[i]);
                continue;
            }

            switch (playerCards[i].cardType)
            {
                case 0:
                    //羁绊消除
                    mgr.UpdateActiveBond(playerCards[i], false);

                    if (DataTable.Hero[playerCards[i].cardId].MilitaryUnitTableId == 58)//铁骑
                        UpdateTieQiStateIconShow(playerCards[i], false);

                    break;
                //塔倒了
                case 2:
                    mgr.RemoveCardFromBoard(playerCards[i], false);
                    break;
            }
            mgr.DestroyCard(playerCards[i]);
            mgr.UpdateChessmanAmount(WarsUIManager.instance.maxHeroNums);
        }

        var enemyCards = FightForManager.instance.GetCardList(false);
        //敌方
        for (int i = 0; i < enemyCards.Count; i++)
        {
            if (i != 17 && enemyCards[i] != null)
            {
                if (enemyCards[i].Hp > 0)
                {
                    OnCardStateUpdate(enemyCards[i]);
                }
                if (enemyCards[i].Hp <= 0)
                {
                    if (enemyCards[i].cardType == 0)
                    {
                        //羁绊消除
                        FightForManager.instance.UpdateActiveBond(enemyCards[i], false);

                        switch (DataTable.Hero[enemyCards[i].cardId].MilitaryUnitTableId)
                        {
                            case 58: //铁骑阵亡
                                UpdateTieQiStateIconShow(enemyCards[i], false);
                                break;

                            default:
                                break;
                        }
                        OnClearCardStateUpdate(enemyCards[i]);
                    }
                    else
                    {
                        if (enemyCards[i].cardType == 2) //塔倒了
                        {
                            FightForManager.instance.RemoveCardFromBoard(enemyCards[i], false);
                        }
                    }
                    //杀死敌将获得金币
                    totalGold += DataTable.EnemyUnit[enemyCards[i].unitId].GoldReward;
                    var chests = DataTable.EnemyUnit[enemyCards[i].unitId].WarChest;
                    if (chests!=null && chests.Length>0)
                    {
                        for (int j = 0; j < chests.Length; j++)
                        {
                            this.chests.Add(chests[j]);
                        }
                    }
                    FightForManager.instance.DestroyCard(enemyCards[i]);
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
                FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_dizzy, true);
            }
            if (cardData.CardState.Imprisoned > 0)     //禁锢状态
            {
                cardData.CardState.Imprisoned = 0;
                FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_imprisoned, true);
            }
            if (cardData.CardState.Bleed > 0)          //流血状态
            {
                cardData.CardState.Bleed = 0;
                FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_bleed, true);
            }
            if (cardData.CardState.Poison > 0)       //中毒状态
            {
                cardData.CardState.Poison = 0;
                FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_poisoned, true);
            }
            if (cardData.CardState.Burn > 0)         //灼烧触发
            {
                cardData.CardState.Burn = 0;
                FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_burned, true);
            }
            if (cardData.CardState.Unarmed > 0)    //卸甲状态
            {
                cardData.CardState.Unarmed = 0;
                FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_removeArmor, true);
            }
            if (cardData.CardState.Shield > 0)      //护盾状态
            {
                cardData.CardState.Shield = 0;
                FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_withStand, true);
            }
            if (cardData.CardState.Invincible > 0)     //无敌消减
            {
                cardData.CardState.Invincible = 0;
                FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_invincible, true);
            }
            if (cardData.CardState.DeathFight > 0)     //死战状态
            {
                cardData.CardState.DeathFight = 0;
                FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_deathFight, true);
            }
            if (cardData.CardState.Stimulate > 0)      //战意状态
            {
                cardData.CardState.Stimulate = 0;
                FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_willFight, true);
            }
            if (cardData.CardState.Neizhu > 0)         //内助状态
            {
                cardData.CardState.Neizhu = 0;
                FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_neizhu, false);
            }
            if (cardData.CardState.ShenZhu > 0)        //神助状态
            {
                cardData.CardState.ShenZhu = 0;
                FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_shenzhu, false);
            }
            if (cardData.CardState.Cowardly > 0)       //怯战状态
            {
                cardData.CardState.Cowardly = 0;
                FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_cowardly, true);
            }
            if (cardData.CardState.MiWuZhenAddOn > 0)    //隐蔽状态
            {
                cardData.CardState.MiWuZhenAddOn = 0;
                FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_miWuZhenAddtion, false);
            }
            if (cardData.CardState.ExtendedHp > 0)        //防护盾状态
            {
                cardData.CardState.ExtendedHp = 0;
                FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_shield, true);
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
                    FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_invincible, true);
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
                    FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_bleed, true);
                }
            }
            if (cardData.CardState.Unarmed > 0) //卸甲状态
            {
                cardData.CardState.Unarmed--;
                if (cardData.CardState.Unarmed <= 0)
                {
                    cardData.CardState.Unarmed = 0;
                    FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_removeArmor, true);
                }
            }
            if (cardData.CardState.DeathFight > 0)    //死战状态
            {
                cardData.CardState.DeathFight--;
                if (cardData.CardState.DeathFight <= 0)
                {
                    cardData.CardState.DeathFight = 0;
                    FightForManager.instance.DestroySateIcon(cardData.cardObj.War.StateContent, StringNameStatic.StateIconPath_deathFight, true);
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

    private List<ChessProcess> rounds;
    //武将行动
    IEnumerator InitiativeHeroAction(FightCardData attackUnit)
    {
        float waitTime = 0;
        waitTime = CountAttackTimeSpan(attackUnit);
        yield return new WaitForSeconds(waitTime);
        rounds = new List<ChessProcess>();
        /////////前摇//////////
        targetIndex = FightForManager.instance.FindOpponentIndex(attackUnit); //锁定目标卡牌
        var target = FightForManager.instance.GetCard(targetIndex, !isPlayerRound);
        //近战跟远程选择不同的进攻方式
        if (attackUnit.combatType == 0)
        {
            //if (ChessPosIndex != -1) yield return CardAnimator.MeleeMoving(attackUnit, target);
            yield return new WaitForSeconds(attackShakeTimeToGo);
            //var tg = new MeleeOperation();
            //var melee = new MeleeOperation();
            //tg.SetUnit(target);
            //tg.Target = melee;
            //melee.SetUnit(attackUnit);
            //melee.Target = tg;
            ////melee.MainOperation = PuTongGongji(1f, attackUnit, target, true);

            yield return OnAttackStart(1f, attackUnit, target, true);
            //yield return ChessmanOperator.Instance.MainOperation(melee);
            CardBackToSelfPosFun(attackUnit);
            yield return new WaitForSeconds(attackShakeTimeToBack);
            Next();
            yield return null;
        }

        //yield return CardAnimator.RangePreAction(attackUnit);//, yuanChengShakeTimeToGo);
        yield return OnAttackStart(1f, attackUnit, target, true);
        //yield return new WaitForSeconds(yuanChengShakeTime);

        /////////后摇//////////
        yield return new WaitForSeconds(yuanChengShakeTimeToBack);
        ////////////////////
        Next();
    }

    //卡牌进入战斗
    private void ChessPosAction()
    {
        var attackUnit = FightForManager.instance.GetCard(ChessPosIndex, isPlayerRound);
        //主动单位并且可行动
        if (attackUnit == null || !attackUnit.activeUnit || attackUnit.isActionDone || OffsetDizzyState(attackUnit) ||
            attackUnit.Hp <= 0)
        {
            Next();
            return;
        }

        attackUnit.isActionDone = true;
        if (attackUnit.cardType == 0)//武将攻击方法
        {
            StartCoroutine(InitiativeHeroAction(attackUnit));
            return;
        }

        if (attackUnit.cardType != 2) return;
        //塔进攻方法
        StartCoroutine(InitiativeTowerAction(attackUnit, isPlayerRound));
    }

    //消除统帅火焰
    private void ClearTongShuaiBurnState()
    {
        if (tongShuaiBurnRoundPy != -1)
        {
            for (int i = 0; i < GoalGfSetFireRound[tongShuaiBurnRoundPy].Length; i++)
            {
                Transform obj = FightForManager.instance.enemyCardsPos[GoalGfSetFireRound[tongShuaiBurnRoundPy][i]].transform.Find(StringNameStatic.StateIconPath_burned);
                if (obj != null)
                    Destroy(obj.gameObject);
            }
            tongShuaiBurnRoundPy = -1;
        }
        if (tongShuaiBurnRoundEm != -1)
        {
            for (int i = 0; i < GoalGfSetFireRound[tongShuaiBurnRoundEm].Length; i++)
            {
                Transform obj = FightForManager.instance.playerCardsPos[GoalGfSetFireRound[tongShuaiBurnRoundEm][i]].transform.Find(StringNameStatic.StateIconPath_burned);
                if (obj != null)
                    Destroy(obj.gameObject);
            }
            tongShuaiBurnRoundEm = -1;
        }
    }

    //下一卡牌单位行动
    private void Next()
    {
        if (recordWinner != 0)
        {
            ClearTongShuaiBurnState();
            if (recordWinner == 1)
            {
                //Debug.Log("---击败敌方");
                UpdateRoundState();

                CollectiveRecoveryHp(); //战斗结束上阵卡牌回复血量

                StartCoroutine(BattleFinalize());

                PlayerDataForGame.instance.ClearGarbageStationObj();
                return;
            }

            //Debug.Log("---被敌方击败");
            WarsUIManager.instance.ExpeditionFinalize(false);
            PlayerDataForGame.instance.ClearGarbageStationObj();
            //Debug.Log("------战斗结束------");
            return;
        }

        if (!isPlayerRound) //敌方单位索引已进行过行动
        {
            ChessPosIndex++;
            if (ChessPosIndex == 17)
            {
                ChessPosIndex++;
            }
            if (ChessPosIndex > 19)    //回合结束
            {
                //Debug.Log("----回合结束");
                stateOfFight = StateOfFight.ReadyForFight;
                UpdateRoundState();
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
        ChessPosAction();
    }

    //战前行动准备
    private float CountAttackTimeSpan(FightCardData attackUnit)
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
                FightForManager.instance.DestroySateIcon(attackUnit.cardObj.War.StateContent, StringNameStatic.StateIconPath_cowardly, true);
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
            if (isSuccessFromHalfRandom(huixinPropNums))
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
                if (isSuccessFromHalfRandom(criPropNums))
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
    IEnumerator InitiativeTowerAction(FightCardData attackUnit, bool playerRound)
    {
        //yield return CardAnimator.RangePreAction(attackUnit);//, towerFightTime0);
        //yield return new WaitForSeconds(towerFightTime0 / 2);
        FightForManager.instance.ActiveTowerFight(attackUnit, playerRound);
        yield return new WaitForSeconds(towerFightTime1);
        //滚石滚木行动
        yield return GunMuGunShiSkill(gunMuCards, gunShiCards);
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
        Next();
    }

    [SerializeField]
    GameObject fireUIObj;
    [SerializeField]
    GameObject boomUIObj;
    [SerializeField]
    GameObject gongKeUIObj;

    //战斗结算
    IEnumerator BattleFinalize()
    {
        boomUIObj.transform.position = FightForManager.instance.enemyCardsPos[17].transform.position;
        fireUIObj.transform.position = gongKeUIObj.transform.position = FightForManager.instance.enemyCardsPos[7].transform.position;
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
        yield return new WaitForSeconds(0.1f);

        Time.timeScale = 1;
        var enemyCards = FightForManager.instance.GetCardList(false);
        for (int i = 0; i < enemyCards.Count; i++)
        {
            FightCardData cardData = enemyCards[i];
            if (i != 17 && cardData != null && cardData.Hp <= 0)
            {
                totalGold += DataTable.EnemyUnit[cardData.unitId].GoldReward;
                var chests = DataTable.EnemyUnit[cardData.unitId].WarChest;
                //暂时关闭打死单位获得的战役宝箱
                if (chests!=null && chests.Length > 0)
                {
                    for (int j = 0; j < chests.Length; j++)
                    {
                        this.chests.Add(chests[j]);
                    }
                }
            }
        }
        totalGold += DataTable.BattleEvent[FightForManager.instance.battleIdIndex].GoldReward;
        var warChests = DataTable.BattleEvent[FightForManager.instance.battleIdIndex].WarChestTableIds;
        for (int k = 0; k < warChests.Length; k++)
        {
            chests.Add(warChests[k]);
        }

        //WarsUIManager.instance.FinalizeWar(totalGold, chests);
        totalGold = 0;
        chests.Clear();
    }

    ////攻击行动方式1-适用于-近战
    //private bool PreAction(FightCardData attackUnit) => ChessPosIndex == -1 || FightForManager.instance.MeleeMoveToTarget(attackUnit, targetIndex);

    //返回原始位置
    private void CardBackToSelfPosFun(FightCardData card) => FightForManager.instance.MeleeReturnOrigin(card);
}

/// <summary>
/// 战斗机状态
/// </summary>
public enum StateOfFight   
{
    ReadyForFight,  //0备战
    MoveNow,       //1攻击前摇
    FightInterval,  //2攻击间
    FightOver       //3攻击后摇
}

/// <summary>
/// 全屏特技名索引
/// </summary>
public enum FullScreenEffectName
{
    /// <summary>
    /// 会心一击特效
    /// </summary>
    HuiXinEffect,
    /// <summary>
    /// 羁绊激活特效
    /// </summary>
    JiBanEffect,
    /// <summary>
    /// 桃园结义主动技能特效
    /// </summary>
    JBTaoYuanJieYi,
    /// <summary>
    /// 五虎上将主动技能特效
    /// </summary>
    JBWuHuShangJiang,
    /// <summary>
    /// 卧龙凤雏主动技能特效
    /// </summary>
    JBWoLongFengChu,
    /// <summary>
    /// 虎痴恶来主动技能特效
    /// </summary>
    JBHuChiELai,
    /// <summary>
    /// 五子良将主动技能特效
    /// </summary>
    JBWuZiLiangJiang,
    /// <summary>
    /// 魏五奇谋主动技能特效
    /// </summary>
    JBWeiWuQiMou,
    /// <summary>
    /// 虎踞江东主动技能特效
    /// </summary>
    JBHuJuJiangDong,
    /// <summary>
    /// 水师都督主动技能特效
    /// </summary>
    JBShuiShiDouDu,
    /// <summary>
    /// 天作之合主动技能特效
    /// </summary>
    JBTianZuoZhiHe,
    /// <summary>
    /// 河北四庭柱主动技能特效
    /// </summary>
    JBHeBeiSiTingZhu,
    /// <summary>
    /// 绝世无双主动技能特效
    /// </summary>
    JBJueShiWuShuang,
    ///汉末三仙主动技能特效
    JBHanMoSanXian
}

/// <summary>
/// 羁绊名索引
/// </summary>
public enum JiBanSkillName
{
    /// <summary>
    /// 桃园结义
    /// </summary>
    TaoYuanJieYi = 0,
    /// <summary>
    /// 五虎上将
    /// </summary>
    WuHuShangJiang = 1,
    /// <summary>
    /// 卧龙凤雏
    /// </summary>
    WoLongFengChu = 2,
    /// <summary>
    /// 虎痴恶来
    /// </summary>
    HuChiELai = 3,
    /// <summary>
    /// 五子良将
    /// </summary>
    WuZiLiangJiang = 4,
    /// <summary>
    /// 魏五谋士
    /// </summary>
    WeiWuMouShi = 5,
    /// <summary>
    /// 虎踞江东
    /// </summary>
    HuJuJiangDong = 6,
    /// <summary>
    /// 水师都督
    /// </summary>
    ShuiShiDuDu = 7,
    /// <summary>
    /// 天作之合
    /// </summary>
    TianZuoZhiHe = 8,
    /// <summary>
    /// 河北四庭柱
    /// </summary>
    HeBeiSiTingZhu = 9,
    /// <summary>
    /// 绝世无双
    /// </summary>
    JueShiWuShuang = 10,
    /// <summary>
    /// 汉末三仙
    /// </summary>
    HanMoSanXian = 11
}