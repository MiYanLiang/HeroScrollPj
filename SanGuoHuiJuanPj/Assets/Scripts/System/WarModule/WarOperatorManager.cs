using System;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using JetBrains.Annotations;

namespace Assets.System.WarModule
{
    public class ChessOperatorManager : ChessboardOperator<FightCardData>
    {
        private const int TowerArmedType = -1;
        private const int TrapArmedType = -2;
        private const int RangeCombatStyle = 1;
        private const int MeleeCombatStyle = 0;
        private const int SpecialCombatStyle = 2;
        private const int NoCounter = 0;
        private const int BasicCounterStyle = 1;

        private static AttackStyle TowerRangeNoCounter =
            AttackStyle.Instance(TowerArmedType, TowerArmedType, RangeCombatStyle, NoCounter, 0);
        private static AttackStyle TrapSpecialNoCounter =
            AttackStyle.Instance(TrapArmedType, TrapArmedType, SpecialCombatStyle, NoCounter, 0);

        private Dictionary<FightCardData, ChessOperator> OpMapper { get; }
        public int ChallengerGold { get; set; }
        public int OpponentGold { get; set; }
        public List<int> ChallengerChests { get; set; }
        public List<int> OpponentChests { get; set; }

        public ChessOperatorManager(bool isChallengerFirst,ChessGrid<FightCardData> grid) : base(isChallengerFirst,grid)
        {
            OpMapper = new Dictionary<FightCardData, ChessOperator>();
        }

        public ChessOperator RegOperator(FightCardData card)
        {
            if (OpMapper.ContainsKey(card))
                throw new InvalidOperationException(
                    $"Duplicated obj = Card.{card.cardId}:Type.{card.CardType} registered!");
            var op = InstanceOperator(card);
            OpMapper.Add(card,op);
            return op;
        }

        public ChessOperator DropOperator(FightCardData card)
        {
            if (!OpMapper.ContainsKey(card))
                throw new NullReferenceException($"Card[{card.cardId}]:Type.{card.CardType}");
            var op = OpMapper[card];
            OpMapper.Remove(card);
            return op;
        }

        private ChessOperator InstanceOperator(FightCardData card)
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

        private ChessOperator InstanceHero(FightCardData card)
        {
            HeroOperator op = null;
            var military = MilitaryInfo.GetInfo(card.cardId);
            switch (military.Id)
            {
                case 1 : op = new JinZhanOperator(); break;//1   近战
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
                case 16: op = new PiaoQiOperator();break;//16  骠骑
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
                ////61  红颜
                ////62  妖师
                ////63  大妖师
                ////64  锦帆
                case 65: op = new HuangJinOperator();break;//65  黄巾
                default: op = new HeroOperator(); break;//0   普通
            }

            op.Init(card, GetStyle(card), this);
            return op;
        }

        private static AttackStyle GetStyle(FightCardData card)
        {
            var m = MilitaryInfo.GetInfo(card.cardId);
            return AttackStyle.Instance(m.Id, m.ArmedType, card.combatType, card.combatType == 1 ? 1 : 0,
                card.cardDamageType);
        }

        private ChessOperator InstanceTrap(FightCardData card)
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

            op.Init(card, TrapSpecialNoCounter,this);
            return op;
        }

        private ChessOperator InstanceTower(FightCardData card)
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

            op.Init(card, TowerRangeNoCounter, this);
            return op;
        }

        public override IChessOperator<FightCardData> GetOperator(IChessPos<FightCardData> chessPos)
        {
            if (chessPos.Chessman == null) return null;
            if (!OpMapper.ContainsKey(chessPos.Chessman))
                throw new InvalidOperationException(
                    $"{nameof(ChessOperatorManager)}:pos[{chessPos.Pos}].Chessman[{chessPos.Chessman.cardId}].Type[{chessPos.Chessman.CardType}] not registered!");
            return OpMapper[chessPos.Chessman];
        }

        #region RoundTrigger
        protected override RoundAction GetRoundEndTriggerByOperators() => ProceedRoundCondition(o => o.OnRoundEnd());

        protected override RoundAction GetPreRoundTriggerByOperators() => ProceedRoundCondition(o => o.OnRoundStart());

        protected override void RoundActionInvocation(int roundKey, IEnumerable<Activity> activities)
        {
            switch (roundKey)
            {
                case RoundAction.PlayerResources:
                    ActionForeach(activities, RoundActionPlayerResources); break;
                case RoundAction.RoundBuffing:
                    ActionForeach(activities, RoundActionBuffering); break;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown RoundKey({roundKey})!");
            }

            void ActionForeach(IEnumerable<Activity> list, Action<Activity> action)
            {
                foreach (var activity in list) action(activity);
            }
        }

        private void RoundActionBuffering(Activity activity) => ActionRespondResult(null, GetTarget(activity), activity.Intent, activity.Conducts);

        private void RoundActionPlayerResources(Activity activity)
        {
            foreach (var conduct in activity.Conducts)
            {
                if (conduct.Element == -1)
                {
                    AddGold(activity.To, conduct);
                    continue;
                }

                AddWarChest(activity.To, conduct);
            }

            void AddGold(int to,CombatConduct c)
            {
                switch (to)
                {
                    case -1: //Challenger
                        ChallengerGold += (int) c.Total;
                        break;
                    case -2: //Opposite
                        OpponentGold += (int) c.Total;
                        break;
                    default: throw new ArgumentOutOfRangeException($"Unknown target({to}) for gold action!");
                }
            }
            void AddWarChest(int to, CombatConduct c)
            {
                switch (to)
                {
                    case -1: //Challenger
                        ChallengerChests.Add(c.Element);
                        break;
                    case -2: //Opposite
                        OpponentChests.Add(c.Element);
                        break;
                    default: throw new ArgumentOutOfRangeException($"Unknown target({to}) for warChest action!");
                }
            }
        }

        protected RoundAction ProceedRoundCondition(
            Func<ChessOperator, IEnumerable<KeyValuePair<int, IEnumerable<Activity>>>> func)
        {
            var round = new RoundAction();
            var roundActions = OpMapper.Values
                .Select(func)
                .Where(o => o != null)
                .SelectMany(o => o)
                .GroupBy(g => g.Key, g => g.Value)
                .ToDictionary(g => g.Key, g => g.SelectMany(s => s));
            round.Activities = roundActions.ToDictionary(a => a.Key, a => a.Value.ToList());
            return round;
        }
        #endregion
    }

    /// <summary>
    /// 38  内政 - 稳定军心，选择有减益的友方武将，有概率为其清除减益状态。
    /// </summary>
    public class NeiZhengOperator : HeroOperator
    {
        private FightState.Cons[] NegativeBuffs { get; } = new[]
        {
            FightState.Cons.Stunned,
            FightState.Cons.Bleed,
            FightState.Cons.Poison,
            FightState.Cons.Burn,
            FightState.Cons.Imprisoned,
            FightState.Cons.Cowardly,
            FightState.Cons.Unarmed
        };

        protected override void MilitaryPerforms()
        {
            var targets = Grid.GetRivalScope(Chessman).
        }
    }

    /// <summary>
    /// 37  大谋士 - 善用诡计，随机选择5个敌方武将，有概率对其造成【眩晕】，无法行动。
    /// </summary>
    public class DaMouShiOperator : MouShiOperator
    {
        protected override int TargetAmount => DataTable.GetGameValue(24);
    }

    /// <summary>
    /// 36  谋士 - 善用诡计，随机选择3个敌方武将，有概率对其造成【眩晕】，无法行动。
    /// </summary>
    public class MouShiOperator : CounselorOperator
    {
        protected override int TargetAmount => DataTable.GetGameValue(23);
        protected override CombatConduct[] Skills() => Singular(CombatConduct.InstanceBuff(FightState.Cons.Stunned));
    }

    /// <summary>
    /// 35  大辩士 - 厉声呵斥，随机选择5个敌方武将，有概率对其造成【禁锢】，无法使用兵种特技。
    /// </summary>
    public class DaBianShiOperator : BianShiOperator
    {
        protected override int TargetAmount => DataTable.GetGameValue(35);
    }

    /// <summary>
    /// 34  辩士 - 厉声呵斥，随机选择3个敌方武将，有概率对其造成【禁锢】，无法使用兵种特技。
    /// </summary>
    public class BianShiOperator : CounselorOperator
    {
        protected override int TargetAmount => DataTable.GetGameValue(21);
        protected override CombatConduct[] Skills() => Singular(CombatConduct.InstanceBuff(FightState.Cons.Imprisoned));
    }
    /// <summary>
    /// 参军/谋士，主要技能是获取一定数量的地方逐一对其发出武将技
    /// </summary>
    public abstract class CounselorOperator : HeroOperator
    {
        protected abstract int TargetAmount { get; }
        protected abstract CombatConduct[] Skills();

        protected override void MilitaryPerforms()
        {
            var targets = Grid.GetRivalScope(Chessman).Values
                .Where(p => p.Chessman != null && p.Chessman.IsAlive)
                .Select(c => new WeightElement<IChessPos<FightCardData>> { Obj = c, Weight = Chessboard.Randomize(3) })
                .Pick(TargetAmount).Select(c => c.Obj).ToArray();
            foreach (var target in targets)
                Chessboard.ActionRespondResult(this, target, Activity.Offensive,
                    Skills());
        }
    }

    /// <summary>
    /// 33  大统帅 - 在敌阵中心纵火，火势每回合向外扩展一圈。多个统帅可接力纵火。
    /// </summary>
    public class DaTongShuaiOperator : TongShuaiOperator
    {
        protected override int BurnRatio => DataTable.GetGameValue(36);
    }

    /// <summary>
    /// 32  统帅 - 在敌阵中心纵火，火势每回合向外扩展一圈。多个统帅可接力纵火。
    /// </summary>
    public class TongShuaiOperator : HeroOperator
    {
        //统帅回合攻击目标
        private int[][] FireRings { get; } = new int[3][] {
            new int[1] { 7},
            new int[6] { 2, 5, 6,10,11,12},
            new int[11]{ 0, 1, 3, 4, 8, 9,13,14,15,16,17},
        };
        //统帅触发灼烧的概率
        protected virtual int BurnRatio => DataTable.GetGameValue(35);
        /// <summary>
        /// 统帅外圈伤害递减
        /// </summary>
        protected virtual int DamageRatio => DataTable.GetGameValue(34);
        
        protected override void MilitaryPerforms()
        {
            var scope = Grid.GetRivalScope(Chessman);
            var ringIndex = -1;
            for (int i = 0; i < FireRings.Length; i++)
            {
                if (scope[FireRings[i][0]].Terrain.GetCondition(ChessTerrain.Fire) <= 0) continue;
                ringIndex = i;
                break;
            }
            ringIndex++;
            if (ringIndex >= FireRings.Length)
                ringIndex = 0;
            var damageDecrease = GetBasicDamage() * DamageRatio * ringIndex * 0.01f;
            var basicDamage = InstanceHeroPerformDamage((int) (GetBasicDamage() - damageDecrease));
            var burnBuff = CombatConduct.InstanceBuff(FightState.Cons.Burn);
            foreach (var chessPos in scope.Values.Join(FireRings[ringIndex],p=>p.Pos,i=>i,(p,_)=>p))
            {
                var combat = new List<CombatConduct> {basicDamage};
                if(Chessboard.IsRandomPass(BurnRatio))
                    combat.Add(burnBuff);
                chessPos.Terrain.AddCondition(ChessTerrain.Fire, 2);
                if (chessPos.Chessman == null || chessPos.Chessman.IsAlive) continue;
                Chessboard.ActionRespondResult(this, chessPos, Activity.Offensive, combat.ToArray());
            }
        }
    }

    /// <summary>
    /// 31  大毒士 - 暗中作祟，随机攻击5个敌方武将，有概率对其造成【中毒】。
    /// </summary>
    public class DaDuShiOperator : DuShiOperator
    {
        protected override int TargetAmount => DataTable.GetGameValue(20);
    }

    /// <summary>
    /// 30  毒士 - 暗中作祟，随机攻击3个敌方武将，有概率对其造成【中毒】。
    /// </summary>
    public class DuShiOperator : HeroOperator
    {
        protected virtual int TargetAmount => DataTable.GetGameValue(19);
        protected virtual int DamageRate => DataTable.GetGameValue(86);
        protected virtual int PoisonRateLimit => DataTable.GetGameValue(87);

        protected override void MilitaryPerforms()
        {
            var targets = Grid.GetRivalScope(Chessman).Values
                .Where(p => p.Chessman != null &&
                            p.Chessman.IsAlive &&
                            p.Chessman.CardType == GameCardType.Hero)
                .Select(p=>new WeightElement<IChessPos<FightCardData>>
                {
                    Obj = p,
                    Weight = WeightElement<IChessPos<FightCardData>>.Random.Next(1,4)
                }).Pick(TargetAmount);
            foreach (var target in targets)
            {
                var basicConduct = InstanceHeroPerformDamage((int) (GetBasicDamage() * DamageRate * 0.01f));
                var poisonRate = (int) (Chessboard.ConfigValue(88) + (Chessboard.ConfigValue(89) * Card.cardGrade - 1));

                if (basicConduct.Rouse > 0)
                    poisonRate += DataTable.GetGameValue(125);
                else if (basicConduct.Critical > 0)
                    poisonRate += DataTable.GetGameValue(124);
                if (poisonRate > PoisonRateLimit) poisonRate = PoisonRateLimit;

                var poison = CombatConduct.InstanceBuff(FightState.Cons.Poison);
                var combats = new List<CombatConduct> {basicConduct};
                if (Chessboard.IsRandomPass(poisonRate))
                    combats.Add(poison);
                Chessboard.ActionRespondResult(this, target.Obj, Activity.Offensive, combats.ToArray());
            }
        }
    }

    /// <summary>
    /// 29  大术士 - 洞察天机，召唤5次天雷，随机落在敌阵中，并有几率造成【眩晕】。
    /// </summary>
    public class DaShuShiOperator : ShuShiOperator
    {
        protected override int AttackTimes => DataTable.GetGameValue(32);
        protected override int TargetLimit => DataTable.GetGameValue(39) - 1;
    }

    /// <summary>
    /// 28  术士 - 洞察天机，召唤3次天雷，随机落在敌阵中，并有几率造成【眩晕】。
    /// </summary>
    public class ShuShiOperator : HeroOperator
    {
        protected virtual int AttackTimes => DataTable.GetGameValue(31);
        protected virtual int TargetLimit => DataTable.GetGameValue(38) - 1;

        protected override void MilitaryPerforms()
        {
            for (var i = 0; i <= AttackTimes; i++)
            {
                var pick = Chessboard.Randomize(TargetLimit) + 1;
                var targets = Grid.GetRivalScope(Chessman).Values
                    .Where(p => p.Chessman != null && p.Chessman.IsAlive)
                    .Select(p => new WeightElement<IChessPos<FightCardData>>
                    {
                        Obj = p,
                        Weight = WeightElement<IChessPos<FightCardData>>.Random.Next(1, 4)
                    }).Pick(pick).ToArray();
                foreach (var target in targets)
                {
                    var combat = new List<CombatConduct> {InstanceHeroPerformDamage()};
                    if (Chessboard.RandomFromConfigTable(40))
                        combat.Add(CombatConduct.InstanceBuff(FightState.Cons.Stunned));
                    Chessboard.ActionRespondResult(this, target.Obj, Activity.Offensive,
                        combat.ToArray());
                }
            }
        }
    }

    /// <summary>
    /// 27  大军师 - 借助东风，攻击血量剩余百分比最少的5个敌方武将，有概率将其绝杀。
    /// </summary>
    public class DaJunShiOperator : JunShiOperator
    {
        protected override int TargetAmount => DataTable.GetGameValue(61);
        protected override int KillingRate => DataTable.GetGameValue(18);
    }

    /// <summary>
    /// 26  军师 - 借助东风，攻击血量剩余百分比最少的3个敌方武将，有小概率将其绝杀。
    /// </summary>
    public class JunShiOperator : HeroOperator
    {
        protected virtual int TargetAmount => DataTable.GetGameValue(59);
        protected virtual int KillingRate => DataTable.GetGameValue(60);
        protected override void MilitaryPerforms()
        {
            var targets = Grid.GetRivalScope(Chessman).Values
                .Where(c => c.Chessman != null && 
                            c.Chessman.IsAlive &&
                            c.Chessman.CardType == GameCardType.Hero)
                .OrderBy(p => Chessboard.GetStatus(p).HpRate)
                .Take(TargetAmount).ToArray();
            var baseDamage = InstanceHeroPerformDamage();
            var killingLineRatio = DataTable.GetGameValue(56);
            if (baseDamage.Critical > 0)
                killingLineRatio = DataTable.GetGameValue(57);
            if (baseDamage.Rouse > 0)
                killingLineRatio = DataTable.GetGameValue(58);
            var killingLine = killingLineRatio * GetBasicDamage();
            foreach (var target in targets)
            {
                var combat = Singular(baseDamage);
                if (Chessboard.GetStatus(target).Hp < killingLine &&
                    Chessboard.IsRandomPass(KillingRate)) 
                    combat = Singular(CombatConduct.InstanceKilling());
                Chessboard.ActionRespondResult(this, target, Activity.Offensive, combat);
            }
        }
    }

    /// <summary>
    /// 25  刺客 - 深入敌方，选择远程单位中攻击最高的目标进行攻击。造成伤害，并为其添加【流血】效果。
    /// </summary>
    public class ChiKeOperator : HeroOperator
    {
        protected override void MilitaryPerforms()
        {
            var target = Grid.GetRivalScope(Chessman).Values
                .Where(p => p.Chessman != null && p.Chessman.IsAlive)
                .OrderByDescending(p => p.Chessman.combatType)
                .ThenByDescending(p => p.Chessman.damage)
                .ThenByDescending(p => p.Chessman.Pos).FirstOrDefault();
            var combats = new List<CombatConduct> {InstanceHeroPerformDamage()};
            if (Chessboard.RandomFromConfigTable(147)) 
                combats.Add(CombatConduct.InstanceBuff(FightState.Cons.Bleed));
            Chessboard.ActionRespondResult(this, target, Activity.Offensive, combats.ToArray());
        }
    }

    /// <summary>
    /// 24  投石车 - 发射巨石，随机以敌方大营及大营周围单位为目标，进行攻击。
    /// </summary>
    public class TouShiCheOperator : HeroOperator
    {
        private int[] TargetPoses = new[] {12, 15, 16, 17};
        protected override void MilitaryPerforms()
        {
            var targets = Grid.GetRivalScope(Chessman).Values.Where(p => p.Chessman != null && p.Chessman.IsAlive)
                .Join(TargetPoses, t => t.Pos, p => p, (t, _) => t).ToArray();
            foreach (var target in targets)
            {
                var damage = GetBasicDamage();
                if (target.Chessman.CardType == GameCardType.Base)
                    damage = (int) (damage * 0.01f * DataTable.GetGameValue(72));
                Chessboard.ActionRespondResult(this, target, Activity.Offensive,
                    Singular(InstanceHeroPerformDamage(damage)));
            }
        }
    }

    /// <summary>
    /// 23  攻城车 - 驱动冲车，对武将造成少量伤害，对塔和陷阱造成高额伤害。
    /// </summary>
    public class GongChengCheOperator : HeroOperator
    {
        protected override CombatConduct[] MilitaryDamages(IChessPos<FightCardData> targetPos)
        {
            var target = Grid.GetContraPositionInSequence(Chessman);
            var damage = GetBasicDamage();
            damage = (int) (damage * 0.01f * 
                            (target.Chessman.CardType != GameCardType.Hero
                                ? DataTable.GetGameValue(73)
                                : DataTable.GetGameValue(74)));
            return Singular(CombatConduct.InstanceDamage(damage, CombatConduct.NonHumanDmg));
        }
    }

    /// <summary>
    /// 22  战车 - 战车攻击时，有概率对目标造成【眩晕】。对已【眩晕】目标造成高额伤害。
    /// </summary>
    public class ZhanCheOperator : HeroOperator
    {
        protected override void MilitaryPerforms()
        {
            var target = Grid.GetContraPositionInSequence(Chessman);
            var status = Chessboard.GetStatus(target);
            var basicDamage = GetBasicDamage();
            if (status.GetBuff(FightState.Cons.Stunned) > 0)
                basicDamage = (int) (DataTable.GetGameValue(92) * 0.01f * basicDamage);
            var combats = new List<CombatConduct> {CombatConduct.InstanceDamage(basicDamage)};
            if (Chessboard.RandomFromConfigTable(91))
                combats.Add(CombatConduct.InstanceBuff(FightState.Cons.Stunned));
            Chessboard.ActionRespondResult(this, target, Activity.Offensive, combats.ToArray());
        }
    }

    /// <summary>
    /// 21  战船 - 驾驭战船，攻击时可击退敌方武将。否则对其造成双倍伤害。
    /// </summary>
    public class ZhanChuanOperator : HeroOperator
    {
        protected override void MilitaryPerforms()
        {
            var target = Grid.GetContraPositionInSequence(Chessman);
            var combatConducts = new List<CombatConduct>();
            var backPos = Grid.BackPos(target);
            if (backPos.Chessman == null)
            {
                combatConducts.Add(InstanceHeroPerformDamage());
                combatConducts.Add(CombatConduct.InstanceRePos(backPos.Pos));
            }
            else
            {
                combatConducts.Add(
                    InstanceHeroPerformDamage((int) (GetBasicDamage() * DataTable.GetGameValue(90) * 0.01f)));
            }

            Chessboard.ActionRespondResult(this, target, Activity.Offensive, combatConducts.ToArray());
        }
    }

    /// <summary>
    /// 52 大弓 - 乱箭齐发，最多攻击5个目标。目标数量越少，造成伤害越高。
    /// </summary>
    public class DaGongOperator : GongBingOperator
    {
        protected override int TargetsPick => DataTable.GetGameValue(78) + 1;
        protected override int DamageRate => DataTable.GetGameValue(79);
    }

    /// <summary>
    /// 20  弓兵 - 乱箭齐发，最多攻击3个目标。目标数量越少，造成伤害越高。
    /// </summary>
    public class GongBingOperator : HeroOperator
    {
        protected virtual int TargetsPick => DataTable.GetGameValue(76) + 1;
        protected virtual int DamageRate => DataTable.GetGameValue(77);
        protected override void MilitaryPerforms()
        {
            var targets = Grid.GetRivalScope(Chessman).Values.Where(p => p.Chessman != null && p.Chessman.IsAlive).Take(TargetsPick).ToArray();
            if(targets.Any())return;
            var damage = DamageRate * GetBasicDamage() * 0.01f / targets.Length;
            var perform = InstanceHeroPerformDamage((int)damage);
            foreach (var target in targets)
            {
                Chessboard.ActionRespondResult(this, target, Activity.Offensive, Singular(perform));
            }
        }
    }

    /// <summary>
    /// 19  连弩 - 武将攻击时，有几率连续射击2次。
    /// </summary>
    public class LianNuOperator : HeroOperator
    {
        protected virtual int Combo => 2;
        protected virtual int ComboRate => DataTable.GetGameValue(48);
        protected override void MilitaryPerforms()
        {
            var target = Grid.GetContraPositionInSequence(Chessman);
            for (int i = 0; i < Combo; i++)
            {
                var result = Chessboard.ActionRespondResult(this, target, Activity.Offensive,
                    Singular(InstanceHeroPerformDamage()));
                if (result.IsDeath) return;
                if (!Chessboard.IsRandomPass(ComboRate))
                    break;
            }
        }
    }

    /// <summary>
    /// 51  强弩 - 武将攻击时，有几率连续射击3次。
    /// </summary>
    public class QiangNuOperator : LianNuOperator
    {
        protected override int Combo => 3;
        protected override int ComboRate => DataTable.GetGameValue(48);
    }

    /// <summary>
    /// 18  大斧 - 挥动大斧，攻击时，可破除敌方护盾。受击目标血量越低，造成的伤害越高。
    /// </summary>
    public class DaFuOperator : HeroOperator
    {
        protected override void MilitaryPerforms()
        {
            var target = Grid.GetContraPositionInSequence(Chessman);
            var status = Chessboard.GetStatus(target);
            var damageRate = HpDepletedRatioWithGap(status, 0, DataTable.GetGameValue(93), DataTable.GetGameValue(94));
            var performDamage = InstanceHeroPerformDamage(GetBasicDamage() * (int) (damageRate * 0.01f));
            Chessboard.ActionRespondResult(this, target, Activity.Offensive, new[]
            {
                CombatConduct.InstanceBuff(FightState.Cons.Shield, -1),
                performDamage
            });
        }
    }

    /// <summary>
    /// 17  大刀 - 斩杀当前目标后，武将继续攻击下一个目标。每次连斩后攻击提升。
    /// </summary>
    public class DaDaoOperator : HeroOperator
    {
        protected override void MilitaryPerforms()
        {
            bool isDeath = false;
            do
            {
                var target = Grid.GetContraPositionInSequence(Chessman);
                isDeath = Chessboard
                    .ActionRespondResult(this, target, Activity.Offensive, Singular(InstanceHeroPerformDamage())).IsDeath;
            } while (isDeath);
        }
    }

    /// <summary>
    /// 15  大戟 - 挥动大戟，攻击时，可横扫攻击目标周围全部单位。横扫伤害比攻击伤害略低。
    /// </summary>
    public class DaJiOperator : ExtendedDamageHero
    {
        protected override CombatConduct GetExtendedDamage(CombatConduct currentDamage)
        {
            var penetrateDmg = currentDamage.Total * DataTable.GetGameValue(95) * 0.01f;
            return CombatConduct.InstanceDamage(penetrateDmg);
        }

        protected override IEnumerable<IChessPos<FightCardData>> ExtendedTargets(IChessPos<FightCardData> target) => Grid.GetNeighbors(target.Pos, target.Chessman.IsPlayer);
    }

    /// <summary>
    /// 14  长枪 - 手持长枪，攻击时，可穿刺攻击目标身后2个单位。穿刺伤害比攻击伤害略低。
    /// </summary>
    public class ChangQiangOperator : ExtendedDamageHero
    {
        /// <summary>
        /// 刺穿单位数
        /// </summary>
        protected virtual int PenetrateUnits => 2;

        protected override CombatConduct GetExtendedDamage(CombatConduct currentDamage)
        {
            var penetrateDmg = currentDamage.Total * DataTable.GetGameValue(96) * 0.01f;
            return CombatConduct.InstanceDamage(penetrateDmg);
        }

        protected override IEnumerable<IChessPos<FightCardData>> ExtendedTargets(IChessPos<FightCardData> target)
        {
            return Grid.GetRivalScope(Chessman).Where(p => p.Key % 5 == target.Pos && p.Value != target)
                .Select(p=>p.Value)
                .OrderBy(p => p.Pos)
                .Take(PenetrateUnits);
        }
    }

    /// <summary>
    /// 给予二度伤害的武将类型
    /// </summary>
    public abstract class ExtendedDamageHero : HeroOperator
    {
        protected abstract CombatConduct GetExtendedDamage(CombatConduct currentDamage);

        protected abstract IEnumerable<IChessPos<FightCardData>> ExtendedTargets(IChessPos<FightCardData> target);

        protected override void MilitaryPerforms()
        {
            var target = Grid.GetContraPositionInSequence(Chessman);
            var penetrates = ExtendedTargets(target);
            var damage = InstanceHeroPerformDamage();
            var penetrateDmg = GetExtendedDamage(damage);
            Chessboard.ActionRespondResult(this, target, Activity.Offensive, Singular(damage));
            foreach (var penetrate in penetrates)
            {
                Chessboard.ActionRespondResult(this, penetrate, Activity.OffendTrigger,
                    Singular(penetrateDmg));
            }
        }
    }

    /// <summary>
    /// 13  禁卫 - 持剑而待，武将受到攻击时，即刻进行反击。
    /// </summary>
    /// 
    public class JinWeiOperator : HeroOperator
    {
        protected override void OnCounter(Activity activity, IChessOperator<FightCardData> offender)
        {
            Chessboard.ActionRespondResult(this, Grid.GetChessPos(offender.Chessman), Activity.Counter,
                Singular(InstanceHeroPerformDamage()));
        }
    }
    /// <summary>
    /// 12  神武 - 每次攻击时，获得1层【战意】，【战意】可提升伤害。
    /// </summary>
    public class ShenWuOperator : HeroOperator
    {
        protected override void MilitaryPerforms()
        {
            Chessboard.ActionRespondResult(this, Grid.GetChessPos(Chessman), Activity.Self,
                Singular(CombatConduct.InstanceBuff(FightState.Cons.Stimulate)));
            base.MilitaryPerforms();
        }
    }

    /// <summary>
    /// 11  白马 - 攻防兼备。武将血量越少，攻击和防御越高。
    /// </summary>
    public class BaiMaOperator : HeroOperator
    {
        private int ConfigGap => DataTable.GetGameValue(98);
        private int ConfigMultipleRate => DataTable.GetGameValue(99);

        protected override int GetPhysicArmor() =>
            HpDepletedRatioWithGap(Status, CombatInfo.PhysicalResist, ConfigGap, ConfigMultipleRate);

        protected override int GetBasicDamage() =>
            HpDepletedRatioWithGap(Status, Card.damage, ConfigGap, ConfigMultipleRate);
    }

    /// <summary>
    /// 10  先登 - 武将血量越少，闪避越高。即将覆灭之时，下次攻击对敌方全体造成伤害。
    /// </summary>
    public class XianDengOperator : HeroOperator
    {
        protected override void MilitaryPerforms()
        {
            if(Status.HpRate * 100 > DataTable.GetGameValue(100))
            {
                base.MilitaryPerforms();
                return;
            }

            var damage = InstanceHeroPerformDamage((int) (Card.damage * DataTable.GetGameValue(101) / 100f));
            foreach (var pos in Grid.GetRivalScope(Chessman).Values.Where(p=>p.Chessman!=null && p.Chessman.IsAlive))
            {
                Chessboard.ActionRespondResult(this, pos, Activity.OffendTrigger, Singular(damage));
            }

            Chessboard.ActionRespondResult(this, Grid.GetChessPos(Chessman), Activity.Self,
                Singular(CombatConduct.InstanceDamage(Status.MaxHp, 1)));
        }

        protected override int GetDodgeRate() => HpDepletedRatioWithGap(Status, CombatInfo.DodgeRatio,
            DataTable.GetGameValue(108),
            DataTable.GetGameValue(109));
    }

    /// <summary>
    /// (8)象兵 - 践踏战场。攻击时可让敌方武将【眩晕】。
    /// </summary>
    public class XiangBingOperator : HeroOperator
    {
        protected override void MilitaryPerforms()
        {
            var target = Grid.GetContraPositionInSequence(Chessman);
            var list = new List<CombatConduct> {InstanceHeroPerformDamage()};
            if (Chessboard.IsRandomPass(DataTable.GetGameValue(102)))
                list.Add(CombatConduct.InstanceBuff(FightState.Cons.Stunned));
            Chessboard.ActionRespondResult(this, target, Activity.Offensive, list.ToArray());
        }
    }

    /// <summary>
    /// (7)刺甲 - 装备刺甲，武将受到近战伤害时，可将伤害反弹。
    /// </summary>
    public class ChiJiaOperator : HeroOperator
    {
        protected override void OnSufferConduct(IChessOperator<FightCardData> offender, CombatConduct[] damages)
        {
            var damage = damages.Sum(c => c.Total);
            Chessboard.ActionRespondResult(this, Grid.GetChessPos(offender.Chessman), Activity.OffendTrigger,
                Singular(CombatConduct.InstanceDamage(damage)));
        }
    }

    /// <summary>
    /// (6)虎卫 - 横行霸道，武将进攻时，可吸收血量。
    /// </summary>
    public class HuWeiOperator : HeroOperator
    {
        protected override void MilitaryPerforms()
        {
            var target = Grid.GetContraPositionInSequence(Chessman);
            var result = Chessboard.ActionRespondResult(this, target, Activity.Offensive, Singular(InstanceHeroPerformDamage()));
            var totalSuffer = result.Status.LastSuffers.Sum();
            Chessboard.ActionRespondResult(this, Grid.GetChessPos(Chessman), Activity.Self,
                Singular(CombatConduct.InstanceHeal(totalSuffer)));
        }
    }

    /// <summary>
    /// (5)陷阵 - 武将陷入危急之时，进入【无敌】状态。
    /// </summary>
    public class XianZhenOperator : HeroOperator
    {
        protected override void OnAfterSubtractHp(int damage, CombatConduct conduct)
        {
            Chessboard.ActionRespondResult(this, Grid.GetChessPos(Chessman), Activity.Self,
                Singular(CombatConduct.InstanceBuff(FightState.Cons.Invincible)));
        }

        public override IEnumerable<KeyValuePair<int, IEnumerable<Activity>>> OnRoundEnd()
        {
            if (Status.GetBuff(FightState.Cons.Invincible) > 0)
                return Singular(new KeyValuePair<int, IEnumerable<Activity>>(RoundAction.RoundBuffing,
                    Singular(Chessboard.InstanceRoundAction(this, Chessman.Pos, Activity.Self,
                        Singular(CombatConduct.InstanceBuff(FightState.Cons.Invincible, -1))))));
            return base.OnRoundEnd();
        }
    }

    /// <summary>
    /// (4)大盾 - 战斗前装备1层【护盾】。每次进攻后装备1层【护盾】。
    /// </summary>
    public class DaDunOperator : HeroOperator
    {
        public override IEnumerable<KeyValuePair<int, IEnumerable<Activity>>> OnRoundStart() =>
            Singular(new KeyValuePair<int, IEnumerable<Activity>>(RoundAction.RoundBuffing,
                Singular(Chessboard.InstanceRoundAction(this, Chessman.Pos, Activity.Self,
                    Singular(CombatConduct.InstanceBuff(FightState.Cons.Shield))))));
    }

    /// <summary>
    /// (3)飞甲 - 有几率闪避攻击。武将剩余血量越少，闪避率越高。
    /// </summary>
    public class FeiJiaOperator : HeroOperator
    {
        protected override int GetDodgeRate() => HpDepletedRatioWithGap(Status, CombatInfo.DodgeRatio,
            DataTable.GetGameValue(106), DataTable.GetGameValue(107));
    }

    /// <summary>
    /// (2)铁卫 - 武将剩余血量越少，防御越高。
    /// </summary>
    public class TeiWeiOperator : HeroOperator
    {
        protected override int GetPhysicArmor()
        {
            var armor = CombatInfo.PhysicalResist;
            var gap = DataTable.GetGameValue(110);
            var addOn = gap - (Status.Hp / Status.MaxHp * 100) % gap;
            return armor + addOn * DataTable.GetGameValue(111);
        }
    }

    public class WeightElement<T> : IWeightElement
    {
        public static Random Random { get; } = new Random();
        public int Weight { get; set; }
        public T Obj { get; set; }
    }
}