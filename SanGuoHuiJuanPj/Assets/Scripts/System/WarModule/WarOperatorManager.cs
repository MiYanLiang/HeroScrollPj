using System;
using CorrelateLib;

public class ChessOperatorManager
{
    //public WarCard GetWarCard(FightCardData card)
    //{
    //    switch (DataTable.Hero[card.cardId].MilitaryUnitTableId)
    //    {
            
    //    }
    //}
    private const int TowerArmedType = -1;
    private const int TrapArmedType = -2;
    private const int RangeCombatStyle = 1;
    private const int MeleeCombatStyle = 0;
    private const int SpecialCombatStyle = 2;
    private const int NoCounter = 0;
    private const int BasicCounterStyle = 1;
    private static Random random = new Random();

    private static AttackStyle TowerRangeNoCounter =
        AttackStyle.Instance(TowerArmedType, TowerArmedType, RangeCombatStyle, NoCounter, 0);
    private static AttackStyle TrapSpecialNoCounter =
        AttackStyle.Instance(TrapArmedType, TrapArmedType, SpecialCombatStyle, NoCounter, 0);

    public static PieceOperator GetWarCard(FightCardData card)
    {

        switch ((GameCardType)card.cardType)
        {
            case GameCardType.Hero:
                return InstanceHero(card);
            case GameCardType.Tower:
                return InstanceTower(card);
            case GameCardType.Trap:
                return InstanceTrap(card);
            case GameCardType.Spell:
            case GameCardType.Base:
            case GameCardType.Soldier:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static PieceOperator InstanceHero(FightCardData card)
    {
        HeroOperator op = null;
        switch (card.cardId)
        {
            case 1 : op = new JinZhanOperator();
                break;//1   近战
            case 2: op = new TeiWeiOperator();break; //2   铁卫
            case 3: op = new FeiJiaOperator();break;//3   飞甲
            case 4: op = new DaDunOperator();break;//4   大盾
            case 5: op = new XianZhenOperator();break;//5   陷阵
            case 6: op = new HuWeiOperator();break;//6   虎卫
            case 7: op = new ChiJiaOperator();break;//7   刺甲
            case 8: op = new XiangBingOperator();break;//8   象兵
            case 9: op = new XianFengOperator();break;//9   先锋
            case 10: op = new XianDengOperator();break;//10  先登
            case 11: op = new BaiMaOperator();break;//11  白马
            case 12: op = new ShenWuOperator();break;//12  神武
            case 13: op = new JinWeiOperator();break;//13  禁卫
            case 14: op = new ChangQiangOperator();break;//14  长枪
            case 15: op = new DaJiOperator();break;//15  大戟
            case 16: op = new BiaoQiOperator();break;//16  骠骑
            case 17: op = new DaDaoOperator();break;//17  大刀
            case 18: op = new DaFuOperator();break;//18  大斧
            case 19: op = new LianNuOperator();break;//19  连弩
            case 20: op = new GongBingOperator();break;//20  弓兵
            case 21: op = new ZhanChuanOperator();break;//21  战船
            case 22: op = new ZhanCheOperator();break;//22  战车
            case 23: op = new GongChengCheOperator();break;//23  攻城车
            case 24: op = new TouShiCheOperator();break;//24  投石车
            case 25: op = new ChiKeOperator();break;//25  刺客
            case 26: op = new JunShiOperator();break;//26  军师
            case 27: op = new DaJunShiOperator();break;//27  大军师
            case 28: op = new ShuShiOperator();break;//28  术士
            case 29: op = new DaShuShiOperator();break;//29  大术士
            case 30: op = new DuShiOperator();break;//30  毒士
            case 31: op = new DaDuShiOperator();break;//31  大毒士
            case 32: op = new TongShuaiOperator();break;//32  统帅
            case 33: op = new DaTongShuaiOperator();break;//33  大统帅
            case 34: op = new BianShiOperator();break;//34  辩士
            case 35: op = new DaBianShiOperator();break;//35  大辩士
            case 36: op = new MouShiOperator();break;//36  谋士
            case 37: op = new DaMouShiOperator();break;//37  大谋士
            case 38: op = new NeiZhengOperator();break;//38  内政
            case 39: op = new FuZuoOperator();break;//39  辅佐
            case 40: op = new QiXieOperator();break;//40  器械
            case 41: op = new GanSiOperator();break;//41  敢死
            case 42: op = new YiShiOperator();break;//42  医师
            case 43: op = new DaYiShiOperator();break;//43  大医师
            case 44: op = new JingGuoOperator();break;//44  巾帼
            case 45: op = new MeiRenOperator();break;//45  美人
            case 46: op = new DaMeiRenOperator();break;//46  大美人
            case 47: op = new ShuiKeOperator();break;//47  说客
            case 48: op = new DaShuiKeOperator();break;//48  大说客
            case 49: op = new NuBingOperator();break;//49  弩兵
            case 50: op = new WenShiOperator();break;//50  文士
            case 51: op = new QiangNuOperator();break;//51  强弩
            case 52: op = new DaGongOperator();break;//52  大弓
            case 53: op = new YinShiOperator();break;//53  隐士
            case 54: op = new DaYinShiOperator();break;//54  大隐士
            case 55: op = new HuoChuanOperator();break;//55  火船
            case 56: op = new ManZuOperator();break;//56  蛮族
            case 57: op = new TengJiaOperator();break;//57  藤甲
            case 58: op = new TieQiOperator();break;//58  铁骑
            case 59: op = new DuanQiangOperator();break;//59  短枪
            case 60: op = new JiXianFengOperator();break;//60  急先锋
            //61  红颜
            //62  妖师
            //63  大妖师
            //64  锦帆
            case 65: op = new HuangJinOperator();break;//65  黄巾
            default: op = new GeneralHeroOperator(); break;//0   普通
        }

        op.Init(card,GetStyle(card));
        return op;
    }

    private static AttackStyle GetStyle(FightCardData card)
    {
        var m = MilitaryInfo.GetInfo(card.cardId);
        return AttackStyle.Instance(m.Id, m.ArmedType, card.cardMoveType, card.cardMoveType == 1 ? 1 : 0,
            card.cardDamageType);
    }

    private static PieceOperator InstanceTrap(FightCardData card)
    {
        TrapOperator op = null;
        switch (card.cardId)
        {
            case 0: op = new JuMaOperator(); break;
            case 1: op = new DiLeiOperator(); break;
            case 2: op = new ShiQiangOperator(); break;
            case 3: op = new BaZhenTuOperator(); break;
            case 4: op = new JinSuoZhenOperator(); break;
            case 5: op = new GuiBingZhenOperator(); break;
            case 6: op = new FireWallOperator(); break;
            case 7: op = new PoisonSpringOperator(); break;
            case 8: op = new BladeWallOperator(); break;
            case 9: op = new GunShiOperator(); break;
            case 10: op = new GunMuOperator(); break;
            case 11: op = new TreasureOperator(); break;
            case 12: op = new JuMaOperator(); break;
            default: op = new BlankTrapOperator(); break;
        }

        op.Init(card, TrapSpecialNoCounter);
        return op;
    }

    private static PieceOperator InstanceTower(FightCardData card)
    {
        TowerOperator op = null;
        switch (card.cardId)
        {
            //营寨
            case 0: op = new YingZhaiOperator(); break;
            //投石台
            case 1: op = new TouShiTaiOperator(); break;
            //奏乐台
            case 2: op = new ZouYueTaiOperator(); break;
            //箭楼
            case 3: op = new JianLouOperator(); break;
            //轩辕台
            case 6: op = new XuanYuanTaiOperator(); break;
            default: op = new BlankTowerOperator(); break;
        }
        op.Init(card, TowerRangeNoCounter);
        return op;
    }


    // Random Range
    private static bool IsRandomPass(int ratio, int range = 100) => random.Next(0, range) <= ratio;
    public static bool RandomFromConfigTable(int id) => IsRandomPass(DataTable.GetGameValue(id));
    public static CombatFactor GetDamage(PieceOperator op)
    {

    }
    public static CombatFactor GetDamage(HeroOperator hero)
    {
        var criticalRatio = GetCriticalRatio(hero);
        if (IsRandomPass(criticalRatio))
            return GetCriticalDamage(hero);
        var rouseRatio = GetRouseRatio(hero);
        if (IsRandomPass(rouseRatio))
            return GetRouseDamage(hero);
        return CombatFactor.InstanceDamage(hero.Chessman.damage, hero.Style.Element);
    }

    private static int GetRouseRatio(HeroOperator hero)
    {
        throw new NotImplementedException();
    }
    private static int GetCriticalRatio(HeroOperator hero)
    {
        throw new NotImplementedException();
    }

    private static CombatFactor GetRouseDamage(HeroOperator hero)
    {
        var dmg = hero.Chessman.damage;
        var rouse = hero.CombatInfo.RouseDamage * 0.01f * dmg - dmg;
        return CombatFactor.Instance(dmg, 0, rouse, hero.Style.Element);
    }

    private static CombatFactor GetCriticalDamage(HeroOperator hero)
    {
        var dmg = hero.Chessman.damage;
        var cri = hero.CombatInfo.CriticalDamage * 0.01f * dmg - dmg;
        return CombatFactor.InstanceDamage(dmg, cri, hero.Style.Element);
    }


    public static PieceOperator GetSequenceTarget(PieceOperator op)
    {

    }
}