using System;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;

namespace Assets.System.WarModule
{
    public class ChessOperatorManager : ChessboardOperator
    {
        private const int TowerArmedType = -1;
        private const int TrapArmedType = -2;
        private const int RangeCombatStyle = 1;
        private const int MeleeCombatStyle = 0;
        private const int SpecialCombatStyle = 2;
        private const int NoCounter = 0;
        private const int BasicCounterStyle = 1;

        private Dictionary<FightCardData, ChessOperator> OpMapper { get; }
        public IReadOnlyDictionary<FightCardData, ChessOperator> Data => OpMapper;
        public int ChallengerGold { get; set; }
        public int OpponentGold { get; set; }
        public List<int> ChallengerChests { get; set; }
        public List<int> OpponentChests { get; set; }

        public ChessOperatorManager(bool isChallengerFirst,ChessGrid grid) : base(isChallengerFirst,grid)
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
                    return InstanceBase(card);//todo 需要实现一个老巢的类
                case GameCardType.Soldier:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private ChessOperator InstanceBase(FightCardData card)
        {
            var op = new BlankTowerOperator();
            op.Init(card, AttackStyle.Instance(-1, -3, 0, 0, 0, 0, card.Level), this);
            return op;
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
                case 49: op = new HeroOperator();break;//49  弩兵
                case 50: op = new HeroOperator();break;//50  文士
                case 51: op = new QiangNuOperator();break;//51  强弩
                case 52: op = new DaGongOperator();break;//52  大弓
                case 53: op = new YinShiOperator();break;//53  隐士
                case 54: op = new DaYinShiOperator();break;//54  大隐士
                case 55: op = new HuoChuanOperator();break;//55  火船
                case 56: op = new ManZuOperator();break;//56  蛮族
                case 57: op = new TengJiaOperator();break;//57  藤甲
                case 58: op = new TieJiOperator();break;//58  铁骑
                case 59: op = new QiangOperator();break;//59  短枪
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
                card.cardDamageType, card.damage,card.Level);
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

            op.Init(card,
                AttackStyle.Instance(TrapArmedType, TrapArmedType, SpecialCombatStyle, NoCounter, 0, card.damage,card.Level),
                this);
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

            op.Init(card,
                AttackStyle.Instance(TowerArmedType, TowerArmedType, RangeCombatStyle, NoCounter, 0, card.damage,card.Level),
                this);
            return op;
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

        private void RoundActionBuffering(Activity activity) => ActionRespondResult(null, GetTarget(activity), activity.Intent, activity.Conducts,0);

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
    /// 58  铁骑 - 多铁骑上阵发动【连环战马】。所有铁骑分担伤害，并按铁骑数量提升伤害。
    /// </summary>
    public class TieJiOperator : HeroOperator
    {
        protected virtual int ShareRate => DataTable.GetGameValue(50);
        protected virtual int DamageStackRate => DataTable.GetGameValue(50);
        protected IEnumerable<IChessPos> GetComrades(int skill = 1) => Grid.GetScope(this).Values
                .Where(p => p.Operator != null &&
                            p.Operator.IsAlive &&
                            p.Operator.CardType == GameCardType.Hero &&
                            p.Operator.Style.Military == 58);

        protected override int OnDamageConvert(CombatConduct conduct)
        {
            var comrades = GetComrades().ToArray();
            var finalDamage = conduct.Total / (comrades.Length + 1);
            foreach (var comrade in comrades)
            {
                Chessboard.ActionRespondResult(this, comrade, Activity.FriendlyAttach,
                    Singular(CombatConduct.Instance(finalDamage, 0, 0, CombatConduct.UnResistDmg,
                        CombatConduct.DamageKind)),1);
            }
            return (int) finalDamage;
        }

        protected override CombatConduct[] MilitaryDamages(IChessPos targetPos)
        {
            var stacks = GetComrades().Count();
            if (stacks <= 0) return base.MilitaryDamages(targetPos);
            var damage = 0.01f * GetBasicDamage() * DamageStackRate * stacks;
            return Singular(InstanceHeroPerformDamage((int) damage));
        }
    }

    /// <summary>
    /// 65  黄巾 - 水能载舟，亦能覆舟。场上黄巾数量越多，攻击越高。
    /// </summary>
    public class HuangJinOperator : HeroOperator
    {
        protected virtual int DamageRate => DataTable.GetGameValue(146);

        protected override CombatConduct[] MilitaryDamages(IChessPos targetPos)
        {
            var cluster = Grid.GetScope(this).Values
                .Count(p => p.Operator != null &&
                            p.Operator.IsAlive &&
                            p.Operator.CardType == GameCardType.Hero &&
                            p.Operator.Style.Military == 65);
            return Singular(InstanceHeroPerformDamage((int) (0.01f * GetBasicDamage() * cluster * DamageRate)));
        }
    }

    /// <summary>
    /// 57  藤甲 - 藤甲护体，刀枪不入。高度免疫物理伤害。
    /// </summary>
    public class TengJiaOperator : HeroOperator
    {
        protected override int OnDamageConvert(CombatConduct conduct)
        {
            if (conduct.Element == CombatConduct.FireDmg) return (int) (conduct.Total * 3);
            return base.OnDamageConvert(conduct);
        }
    }

    /// <summary>
    /// 59  短枪 - 手持短枪，攻击时，可穿刺攻击目标身后1个单位。
    /// </summary>
    public class QiangOperator : ExtendedDamageHero
    {
        /// <summary>
        /// 刺穿单位数
        /// </summary>
        protected virtual int PenetrateUnits => 1;

        protected override CombatConduct GetExtendedDamage(CombatConduct currentDamage)
        {
            var penetrateDmg = currentDamage.Total * DataTable.GetGameValue(96) * 0.01f;
            return CombatConduct.InstanceDamage(penetrateDmg);
        }

        protected override IEnumerable<IChessPos> ExtendedTargets(IChessPos target)
        {
            var tPoss = new List<int>();
            for (var  i = 1; i < PenetrateUnits + 1; i++)
            {
                var pos = i * 5 * target.Pos;
                tPoss.Add(pos);
            }

            return Grid.GetRivalScope(this)
                .Where(p => tPoss.Contains(p.Key) && p.Value != target && p.Value.IsPostedAlive)
                .Select(p => p.Value)
                .OrderBy(p => p.Pos)
                .Take(PenetrateUnits);
        }
    }

    /// <summary>
    /// 56  蛮族 - 剧毒粹刃，攻击时有概率使敌方武将【中毒】。
    /// </summary>
    public class ManZuOperator : HeroOperator
    {
        protected virtual int PoisonRate => DataTable.GetGameValue(51);
        protected override CombatConduct[] MilitaryDamages(IChessPos targetPos)
        {
            var combat = new List<CombatConduct> {InstanceHeroPerformDamage()};
            if(Chessboard.IsRandomPass(PoisonRate))
                combat.Add(CombatConduct.InstanceBuff(FightState.Cons.Poison));
            return combat.ToArray();
        }
    }

    /// <summary>
    /// 55  火船 - 驱动火船，可引燃敌方武将，或自爆对敌方造成大范围伤害及【灼烧】。
    /// </summary>
    public class HuoChuanOperator : HeroOperator
    {
        protected virtual int BurnRate => DataTable.GetGameValue(52);
        protected virtual int BurnExplodeRatio => DataTable.GetGameValue(53);
        protected virtual int ExplodeRatio => DataTable.GetGameValue(55);

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Grid.GetContraPositionInSequence(this);
            if (Status.HpRate < 0.5)
            {
                var explode = new List<CombatConduct>
                {
                    InstanceHeroPerformDamage((int)
                        (GetBasicDamage() * ExplodeRatio * 0.01f))
                };
                var surrounded = Grid.GetNeighbors(target.Pos, target.IsChallenger)
                    .Where(p => p.IsPostedAlive).ToList();
                surrounded.Insert(0, target);
                foreach (var chessPos in surrounded)
                {
                    if (Chessboard.IsRandomPass(BurnExplodeRatio))
                        explode.Add(CombatConduct.InstanceBuff(FightState.Cons.Burn));
                    Chessboard.ActionRespondResult(this, chessPos, Activity.OffendAttach, explode.ToArray(),2);
                }

                Chessboard.ActionRespondResult(this, Grid.GetChessPos(this), Activity.Self,
                    Singular(CombatConduct.InstanceKilling()),2);
                return;
            }

            var combat = new List<CombatConduct> {InstanceHeroPerformDamage()};
            if (Chessboard.IsRandomPass(BurnRate))
                combat.Add(CombatConduct.InstanceBuff(FightState.Cons.Burn));
            Chessboard.ActionRespondResult(this, target, Activity.Offensive, combat.ToArray(),1);
        }
    }

    /// <summary>
    /// 54  大隐士 - 召唤水龙，攻击敌方5个武将，造成伤害并将其击退。
    /// </summary>
    public class DaYinShiOperator : YinShiOperator
    {
        protected override int TargetAmount => DataTable.GetGameValue(30);
    }

    /// <summary>
    /// 53  隐士 - 召唤激流，攻击敌方3个武将，造成伤害并将其击退。
    /// </summary>
    public class YinShiOperator : HeroOperator
    {
        protected virtual int TargetAmount => DataTable.GetGameValue(29);
        protected virtual int DamageRate => DataTable.GetGameValue(75);
        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Grid.GetRivalScope(this).Values
                .Where(p => p.Operator != null &&
                            p.Operator.IsAlive &&
                            p.Operator.CardType == GameCardType.Hero)
                .Select(p => new WeightElement<IChessPos>
                {
                    Obj = p,
                    Weight = Chessboard.Randomize(3)
                }).Pick(TargetAmount).ToArray();
            foreach (var target in targets)
            {
                var damage = InstanceHeroPerformDamage((int) (DamageRate * 0.01f * GetBasicDamage()));
                var backPos = Grid.BackPos(target.Obj);
                if (backPos!=null && backPos.Operator == null)
                {
                    Chessboard.ActionRespondResult(this, target.Obj, Activity.Offensive, Singular(damage), 1,
                        backPos.Pos);
                    continue;
                }
                Chessboard.ActionRespondResult(this, target.Obj, Activity.Offensive, Singular(damage), 0);
            }
        }
    }

    /// <summary>
    /// 48  大说客 - 随机选择5个敌方武将进行游说，有概率对其造成【怯战】，无法暴击和会心一击。
    /// </summary>
    public class DaShuiKeOperator : ShuiKeOperator
    {
        protected override int TargetAmount => DataTable.GetGameValue(28);
    }

    /// <summary>
    /// 47  说客 - 随机选择3个敌方武将进行游说，有概率对其造成【怯战】，无法暴击和会心一击。
    /// </summary>
    public class ShuiKeOperator : CounselorOperator
    {
        protected override int TargetAmount => DataTable.GetGameValue(27);
        protected override int LevelRate => DataTable.GetGameValue(71);
        protected override int BasicRate => DataTable.GetGameValue(70);
        protected override int MaxRate => DataTable.GetGameValue(69);
        protected override CombatConduct[] Skills() => Singular(CombatConduct.InstanceBuff(FightState.Cons.Cowardly));
    }

    /// <summary>
    /// 46  大美人 - 以倾国之姿激励友方武将，有概率使其获得【神助】，下次攻击时必定会心一击。
    /// </summary>
    public class DaMeiRenOperator : MeiRenOperator
    {
        protected override CombatConduct BuffToFriendly => CombatConduct.InstanceBuff(FightState.Cons.ShenZhu);
    }

    /// <summary>
    /// 45  美人 - 以倾城之姿激励友方武将，有概率使其获得【内助】，下次攻击时必定暴击。
    /// </summary>
    public class MeiRenOperator : HeroOperator
    {
        protected virtual CombatConduct BuffToFriendly => CombatConduct.InstanceBuff(FightState.Cons.Neizhu);
        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Grid.GetScope(this)
                .Values.Where(p => p.Operator != null &&
                                   p.Operator.IsAlive &&
                                   p.Operator.CardType == GameCardType.Hero)
                .FirstOrDefault();
            if (target == null)
            {
                base.MilitaryPerforms();
                return;
            }
            Chessboard.ActionRespondResult(this, target, Activity.Friendly, Singular(BuffToFriendly),1);
        }
    }

    /// <summary>
    /// 44  巾帼 - 巾帼不让须眉，攻击同时对目标造成【卸甲】，大幅降低其攻击和防御。
    /// </summary>
    public class JingGuoOperator : HeroOperator
    {
        protected override CombatConduct[] MilitaryDamages(IChessPos targetPos)
        {
            return new CombatConduct[]
            {
                CombatConduct.InstanceBuff(FightState.Cons.Disarmed),
                InstanceHeroPerformDamage()
            };
        }
    }

    /// <summary>
    /// 43  大医师 - 分发神药，最多治疗3个友方武将。治疗单位越少，治疗量越高。
    /// </summary>
    public class DaYiShiOperator : YiShiOperator
    {
        protected override int TargetAmount => DataTable.GetGameValue(26);
        protected override int HealingRate => DataTable.GetGameValue(81);
    }

    /// <summary>
    /// 42  医师 - 分发草药，治疗1个友方武将。
    /// </summary>
    public class YiShiOperator : HeroOperator
    {
        protected virtual int TargetAmount => DataTable.GetGameValue(25);
        protected virtual int HealingRate => DataTable.GetGameValue(80);
        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Grid.GetScope(this)
                .Values.Where(p => p.Operator != null &&
                                   p.Operator.IsAlive &&
                                   p.Operator.CardType == GameCardType.Hero &&
                                   p.Operator.Status.HpRate < 1)
                .OrderBy(p => p.Operator.Status.HpRate)
                .Take(TargetAmount).ToArray();
            if (targets.Length == 0)
            {
                base.MilitaryPerforms();
                return;
            }
            var basicHeal = GetBasicDamage() * HealingRate * 0.01f / targets.Length;
            var heal = CombatConduct.InstanceHeal(basicHeal);
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                Chessboard.ActionRespondResult(this, target, i == 0 ? Activity.Friendly : Activity.FriendlyAttach,
                    Singular(heal),1);
            }
        }
    }

    /// <summary>
    /// 41  敢死 - 武将陷入危急之时进入【死战】状态，将受到的伤害转化为自身血量数。
    /// </summary>
    public class GanSiOperator : HeroOperator
    {
        protected virtual int TriggerRate => DataTable.GetGameValue(103);
        protected override void OnAfterSubtractHp(int damage, CombatConduct conduct)
        {
            if (Status.HpRate > TriggerRate * 0.01f)return;
            Chessboard.ActionRespondResult(this, Grid.GetChessPos(this), Activity.Self,
                Singular(CombatConduct.InstanceBuff(FightState.Cons.DeathFight)),1);
        }
    }

    /// <summary>
    /// 40  器械 - 神斧天工，最多选择3个建筑（包括大营）进行修复。修复单位越少，修复量越高。
    /// </summary>
    public class QiXieOperator : HeroOperator
    {
        protected virtual int TargetAmount => DataTable.GetGameValue(132);
        protected virtual int RecoverRate => DataTable.GetGameValue(65);
        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Grid.GetScope(this).Values
                .Where(p => p.Operator != null &&
                            p.Operator.IsAlive && (
                                p.Operator.CardType == GameCardType.Tower ||
                                p.Operator.CardType == GameCardType.Trap ||
                                p.Operator.CardType == GameCardType.Base))
                .OrderBy(p => p.Operator.Status.HpRate)
                .Take(TargetAmount).ToArray();
            if (targets.Length == 0)
            {
                base.MilitaryPerforms();
                return;
            }

            var recover = GetBasicDamage() * RecoverRate * 0.01f / targets.Length;
            foreach (var target in targets)
            {
                Chessboard.ActionRespondResult(this, target, Activity.Friendly,
                    Singular(CombatConduct.InstanceHeal(recover)),1);
            }
        }
    }

    /// <summary>
    /// 39  辅佐 - 为血量数最低的友方武将添加【防护盾】，可持续抵挡伤害。
    /// </summary>
    public class FuZuoOperator : HeroOperator
    {
        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Grid.GetScope(this).Values
                .Where(p => p.Operator != null && 
                            p.Operator.IsAlive && 
                            p.Operator.CardType == GameCardType.Hero)
                .OrderBy(p => (p.Operator.Status.Hp + p.Operator.Status.GetBuff(FightState.Cons.ExtendedHp)) / p.Operator.Status.MaxHp)
                .FirstOrDefault();
            if (target == null)
            {
                base.MilitaryPerforms();
                return;
            }
            var basicDamage = InstanceHeroPerformDamage();
            Chessboard.ActionRespondResult(this, target, Activity.Friendly,
                Singular(CombatConduct.InstanceBuff(FightState.Cons.ExtendedHp, basicDamage.Total)),1);
        }
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
            FightState.Cons.Disarmed
        };

        protected virtual int BasicRate => DataTable.GetGameValue(126);
        protected virtual int LevelingIncrease => DataTable.GetGameValue(127);
        protected override void MilitaryPerforms(int skill = 1)
        {
            var rate = BasicRate + LevelingIncrease * Style.Level;
            var targets = Grid.GetScope(IsChallenger).Values
                .Where(p => p.IsPostedAlive && 
                            p.Operator.CardType == GameCardType.Hero)
                .Select(pos =>
                {
                    return new
                    {
                        pos,pos.Operator, value = pos.Operator.Status.Buffs
                            .Join(NegativeBuffs, b => b.Key, n => (int) n, (b, _) => b)
                            .Sum(b => b.Value)
                    };
                }) //找出所有武将的负面数
                .OrderByDescending(b => b.value).Take(3).ToArray();
            var basicDamage = InstanceHeroPerformDamage();

            if (targets.Length == 0)
            {
                base.MilitaryPerforms();
                return;
            }

            for (var i = 0; i < targets.Length; i++)
            {
                if(Chessboard.IsRandomPass(rate))
                {
                    var target = targets[i];
                    var keys = target.Operator.Status.Buffs.Where(p => p.Value > 0)
                        .Join(NegativeBuffs, p => p.Key, n => (int) n, (_, n) => n)
                        .ToArray();
                    var con = keys[Chessboard.Randomize(keys.Length)];
                    Chessboard.ActionRespondResult(this, target.pos, Activity.Friendly,
                        Singular(CombatConduct.InstanceBuff(con, -1)),1);
                }
                if(basicDamage.Rouse > 0 && i < 2)
                    continue;
                if(basicDamage.Critical > 0 && i < 1)
                    continue;
                break;
            }

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
        protected override int LevelRate => DataTable.GetGameValue(83);
        protected override int BasicRate => DataTable.GetGameValue(82);
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
        protected override int MaxRate => DataTable.GetGameValue(66);
        protected override int LevelRate => DataTable.GetGameValue(68);
        protected override int BasicRate => DataTable.GetGameValue(67);
        protected override CombatConduct[] Skills() => Singular(CombatConduct.InstanceBuff(FightState.Cons.Imprisoned));
    }
    /// <summary>
    /// 参军/谋士，主要技能是获取一定数量的对方武将类逐一对其发出武将技
    /// </summary>
    public abstract class CounselorOperator : HeroOperator
    {
        protected abstract int TargetAmount { get; }
        /// <summary>
        /// 暴击增率
        /// </summary>
        protected virtual int CriticalRate => DataTable.GetGameValue(84);
        /// <summary>
        /// 会心增率
        /// </summary>
        protected virtual int RouseRate => DataTable.GetGameValue(85);
        /// <summary>
        /// buff最大率
        /// </summary>
        protected virtual int MaxRate => 100;
        /// <summary>
        /// 根据等级递增率
        /// </summary>
        protected abstract int LevelRate { get; }
        /// <summary>
        /// 基础值
        /// </summary>
        protected abstract int BasicRate { get; }
        protected abstract CombatConduct[] Skills();

        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Grid.GetRivalScope(this).Values
                .Where(p => p.IsPostedAlive && p.Operator.CardType == GameCardType.Hero)
                .Select(c => new WeightElement<IChessPos> { Obj = c, Weight = Chessboard.Randomize(3) })
                .Pick(TargetAmount).Select(c => c.Obj).ToArray();
            var damage = InstanceHeroPerformDamage();
            var rate = BasicRate + Style.Level * LevelRate;
            if (damage.Rouse > 0)
                rate += RouseRate;
            else if (damage.Critical > 0)
                rate += CriticalRate;
            if (rate > MaxRate)
                rate = MaxRate;
            foreach (var target in targets)
            {
                if (Chessboard.IsRandomPass(rate))
                    Chessboard.ActionRespondResult(this, target, Activity.Offensive,
                        Skills(),1);
            }
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
        
        protected override void MilitaryPerforms(int skill = 1)
        {
            var scope = Grid.GetRivalScope(this);
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
                if (chessPos.Operator == null || chessPos.Operator.IsAlive) continue;
                Chessboard.ActionRespondResult(this, chessPos, Activity.Offensive, combat.ToArray(),1);
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

        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Grid.GetRivalScope(this).Values
                .Where(p => p.IsPostedAlive &&
                            p.Operator.CardType == GameCardType.Hero)
                .Select(p=>new WeightElement<IChessPos>
                {
                    Obj = p,
                    Weight = WeightElement<IChessPos>.Random.Next(1,4)
                }).Pick(TargetAmount);
            foreach (var target in targets)
            {
                var basicConduct = InstanceHeroPerformDamage((int) (GetBasicDamage() * DamageRate * 0.01f));
                var poisonRate = (int) (Chessboard.ConfigValue(88) + (Chessboard.ConfigValue(89) * Style.Level - 1));

                if (basicConduct.Rouse > 0)
                    poisonRate += DataTable.GetGameValue(125);
                else if (basicConduct.Critical > 0)
                    poisonRate += DataTable.GetGameValue(124);
                if (poisonRate > PoisonRateLimit) poisonRate = PoisonRateLimit;

                var poison = CombatConduct.InstanceBuff(FightState.Cons.Poison);
                var combats = new List<CombatConduct> {basicConduct};
                if (Chessboard.IsRandomPass(poisonRate))
                    combats.Add(poison);
                Chessboard.ActionRespondResult(this, target.Obj, Activity.Offensive, combats.ToArray(),1);
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

        protected override void MilitaryPerforms(int skill = 1)
        {
            for (var i = 0; i <= AttackTimes; i++)
            {
                var pick = Chessboard.Randomize(TargetLimit) + 1;
                var targets = Grid.GetRivalScope(this).Values
                    .Where(p => p.IsPostedAlive)
                    .Select(p => new WeightElement<IChessPos>
                    {
                        Obj = p,
                        Weight = WeightElement<IChessPos>.Random.Next(1, 4)
                    }).Pick(pick).ToArray();
                foreach (var target in targets)
                {
                    var combat = new List<CombatConduct> {InstanceHeroPerformDamage()};
                    if (Chessboard.RandomFromConfigTable(40))
                        combat.Add(CombatConduct.InstanceBuff(FightState.Cons.Stunned));
                    Chessboard.ActionRespondResult(this, target.Obj, Activity.Offensive,
                        combat.ToArray(),1);
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
        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Grid.GetRivalScope(this).Values
                .Where(c => c.IsPostedAlive &&
                            c.Operator.CardType == GameCardType.Hero)
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
                Chessboard.ActionRespondResult(this, target, Activity.Offensive, combat,1);
            }
        }
    }

    /// <summary>
    /// 25  刺客 - 深入敌方，选择远程单位中攻击最高的目标进行攻击。造成伤害，并为其添加【流血】效果。
    /// </summary>
    public class ChiKeOperator : HeroOperator
    {
        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Grid.GetRivalScope(this).Values
                .Where(p => p.IsPostedAlive)
                .OrderByDescending(p => p.Operator.Style.CombatStyle)
                .ThenByDescending(p => p.Operator.Style.Strength)
                .ThenByDescending(p => p.Pos).FirstOrDefault();
            var combats = new List<CombatConduct> {InstanceHeroPerformDamage()};
            if (Chessboard.RandomFromConfigTable(147)) 
                combats.Add(CombatConduct.InstanceBuff(FightState.Cons.Bleed));
            Chessboard.ActionRespondResult(this, target, Activity.Offensive, combats.ToArray(),1);
        }
    }

    /// <summary>
    /// 24  投石车 - 发射巨石，随机以敌方大营及大营周围单位为目标，进行攻击。
    /// </summary>
    public class TouShiCheOperator : HeroOperator
    {
        private int[] TargetPoses = new[] {12, 15, 16, 17};
        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Grid.GetRivalScope(this).Values.Where(p => p.Operator != null && p.Operator.IsAlive)
                .Join(TargetPoses, t => t.Pos, p => p, (t, _) => t).ToArray();
            foreach (var target in targets)
            {
                var damage = GetBasicDamage();
                if (target.Operator.CardType == GameCardType.Base)
                    damage = (int) (damage * 0.01f * DataTable.GetGameValue(72));
                Chessboard.ActionRespondResult(this, target, Activity.Offensive,
                    Singular(InstanceHeroPerformDamage(damage)),1);
            }
        }
    }

    /// <summary>
    /// 23  攻城车 - 驱动冲车，对武将造成少量伤害，对塔和陷阱造成高额伤害。
    /// </summary>
    public class GongChengCheOperator : HeroOperator
    {
        protected override CombatConduct[] MilitaryDamages(IChessPos targetPos)
        {
            var target = Grid.GetContraPositionInSequence(this);
            var damage = GetBasicDamage();
            damage = (int) (damage * 0.01f * 
                            (target.Operator.CardType != GameCardType.Hero
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
        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Grid.GetContraPositionInSequence(this);
            var status = Chessboard.GetStatus(target);
            var basicDamage = GetBasicDamage();
            if (status.GetBuff(FightState.Cons.Stunned) > 0)
                basicDamage = (int) (DataTable.GetGameValue(92) * 0.01f * basicDamage);
            var combats = new List<CombatConduct> {CombatConduct.InstanceDamage(basicDamage)};
            if (Chessboard.RandomFromConfigTable(91))
                combats.Add(CombatConduct.InstanceBuff(FightState.Cons.Stunned));
            Chessboard.ActionRespondResult(this, target, Activity.Offensive, combats.ToArray(),1);
        }
    }

    /// <summary>
    /// 21  战船 - 驾驭战船，攻击时可击退敌方武将。否则对其造成双倍伤害。
    /// </summary>
    public class ZhanChuanOperator : HeroOperator
    {
        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Grid.GetContraPositionInSequence(this);
            var combatConducts = new List<CombatConduct>();
            var backPos = Grid.BackPos(target);
            var rePos = backPos != null && backPos.Operator == null ? backPos.Pos : -1;
            combatConducts.Add(
                rePos < 0
                    ? InstanceHeroPerformDamage((int) (GetBasicDamage() * DataTable.GetGameValue(90) * 0.01f))
                    : InstanceHeroPerformDamage());

            Chessboard.ActionRespondResult(this, target, Activity.Offensive, combatConducts.ToArray(), 1, rePos);
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
        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Grid.GetRivalScope(this).Values.Where(p => p.IsPostedAlive).Take(TargetsPick).ToArray();
            if (targets.Length == 0) return;
            var damage = DamageRate * GetBasicDamage() * 0.01f / targets.Length;
            var perform = InstanceHeroPerformDamage((int)damage);
            foreach (var target in targets)
            {
                Chessboard.ActionRespondResult(this, target, Activity.Offensive, Singular(perform),1);
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
        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Grid.GetContraPositionInSequence(this);
            for (int i = 0; i < Combo; i++)
            {
                var result = Chessboard.ActionRespondResult(this, target, Activity.Offensive,
                    Singular(InstanceHeroPerformDamage()), i);
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
        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Grid.GetContraPositionInSequence(this);
            var status = Chessboard.GetStatus(target);
            var damageRate = HpDepletedRatioWithGap(status, 0, DataTable.GetGameValue(93), DataTable.GetGameValue(94));
            var performDamage = InstanceHeroPerformDamage(GetBasicDamage() * (int) (damageRate * 0.01f));
            Chessboard.ActionRespondResult(this, target, Activity.Offensive, new[]
            {
                CombatConduct.InstanceBuff(FightState.Cons.Shield, -1),
                performDamage
            },1);
        }
    }

    /// <summary>
    /// 17  大刀 - 斩杀当前目标后，武将继续攻击下一个目标。每次连斩后攻击提升。
    /// </summary>
    public class DaDaoOperator : HeroOperator
    {
        protected virtual int DamageIncreaseRate => DataTable.GetGameValue(41);
        protected override void MilitaryPerforms(int skill = 1)
        {
            var paths = Grid.GetAttackPath(this);
            var targets = Grid.GetRivalScope(this).Join(paths,p=>p.Key,p=>p,(p,_)=>p.Value)
                .Where(p => p.IsPostedAlive);
            var addOnDmg = 0;
            foreach (var target in targets)
            {
                var damage = GetBasicDamage() + addOnDmg;
                if (!Chessboard
                    .ActionRespondResult(this, target, Activity.Offensive, Singular(InstanceHeroPerformDamage()),1)
                    .IsDeath)
                    break;
                addOnDmg += (int)(damage * DamageIncreaseRate * 0.01f);
            }
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

        protected override IEnumerable<IChessPos> ExtendedTargets(IChessPos target) => Grid.GetNeighbors(target.Pos, target.IsChallenger);
    }

    /// <summary>
    /// 14  长枪 - 手持长枪，攻击时，可穿刺攻击目标身后2个单位。穿刺伤害比攻击伤害略低。
    /// </summary>
    public class ChangQiangOperator : QiangOperator
    {
        protected override int PenetrateUnits => 2;
    }

    /// <summary>
    /// 给予散列伤害的武将类型
    /// </summary>
    public abstract class ExtendedDamageHero : HeroOperator
    {
        protected abstract CombatConduct GetExtendedDamage(CombatConduct currentDamage);

        protected abstract IEnumerable<IChessPos> ExtendedTargets(IChessPos target);

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Grid.GetContraPositionInSequence(this);
            var penetrates = ExtendedTargets(target);
            var damage = InstanceHeroPerformDamage();
            var penetrateDmg = GetExtendedDamage(damage);
            Chessboard.ActionRespondResult(this, target, Activity.Offensive, Singular(damage), 1);
            foreach (var penetrate in penetrates)
            {
                Chessboard.ActionRespondResult(this, penetrate, Activity.OffendAttach,
                    Singular(penetrateDmg),1);
            }
        }
    }

    /// <summary>
    /// 13  禁卫 - 持剑而待，武将受到攻击时，即刻进行反击。
    /// </summary>
    /// 
    public class JinWeiOperator : HeroOperator
    {
        protected override void OnCounter(Activity activity, IChessOperator offender)
        {
            Chessboard.ActionRespondResult(this, Grid.GetChessPos(offender.Pos,offender.IsChallenger), Activity.Counter,
                Singular(InstanceHeroPerformDamage()),1);
        }
    }
    /// <summary>
    /// 12  神武 - 每次攻击时，获得1层【战意】，【战意】可提升伤害。
    /// </summary>
    public class ShenWuOperator : HeroOperator
    {
        protected override void MilitaryPerforms(int skill = 1)
        {
            Chessboard.ActionRespondResult(this, Grid.GetChessPos(this), Activity.Self,
                Singular(CombatConduct.InstanceBuff(FightState.Cons.Stimulate)),1);
            base.MilitaryPerforms(skill);
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
            HpDepletedRatioWithGap(Status, Style.Strength, ConfigGap, ConfigMultipleRate);
    }

    /// <summary>
    /// 10  先登 - 武将血量越少，闪避越高。即将覆灭之时，下次攻击对敌方全体造成伤害。
    /// </summary>
    public class XianDengOperator : HeroOperator
    {
        protected override void MilitaryPerforms(int skill = 1)
        {
            if(Status.HpRate * 100 > DataTable.GetGameValue(100))
            {
                base.MilitaryPerforms();
                return;
            }

            var damage = InstanceHeroPerformDamage((int) (Style.Strength * DataTable.GetGameValue(101) / 100f));
            foreach (var pos in Grid.GetRivalScope(this).Values.Where(p=>p.IsPostedAlive))
            {
                Chessboard.ActionRespondResult(this, pos, Activity.OffendAttach, Singular(damage), 1);
            }

            Chessboard.ActionRespondResult(this, Grid.GetChessPos(this), Activity.Self,
                Singular(CombatConduct.InstanceKilling()),1);
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
        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Grid.GetContraPositionInSequence(this);
            var list = new List<CombatConduct> {InstanceHeroPerformDamage()};
            if (Chessboard.IsRandomPass(DataTable.GetGameValue(102)))
                list.Add(CombatConduct.InstanceBuff(FightState.Cons.Stunned));
            Chessboard.ActionRespondResult(this, target, Activity.Offensive, list.ToArray(),1);
        }
    }

    /// <summary>
    /// (7)刺甲 - 装备刺甲，武将受到近战伤害时，可将伤害反弹。
    /// </summary>
    public class ChiJiaOperator : HeroOperator
    {
        protected override void OnSufferConduct(IChessOperator offender, CombatConduct[] damages)
        {
            var damage = damages.Sum(c => c.Total);
            Chessboard.ActionRespondResult(this, Grid.GetChessPos(offender), Activity.OffendAttach,
                Singular(CombatConduct.InstanceDamage(damage)),1);
        }
    }

    /// <summary>
    /// (6)虎卫 - 横行霸道，武将进攻时，可吸收血量。
    /// </summary>
    public class HuWeiOperator : HeroOperator
    {
        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Grid.GetContraPositionInSequence(this);
            var result = Chessboard.ActionRespondResult(this, target, Activity.Offensive, Singular(InstanceHeroPerformDamage()),0);
            var totalSuffer = result.Status.LastSuffers.Sum();
            Chessboard.ActionRespondResult(this, Grid.GetChessPos(this), Activity.Self,
                Singular(CombatConduct.InstanceHeal(totalSuffer)),1);
        }
    }

    /// <summary>
    /// (5)陷阵 - 武将陷入危急之时，进入【无敌】状态。
    /// </summary>
    public class XianZhenOperator : HeroOperator
    {
        protected override void OnAfterSubtractHp(int damage, CombatConduct conduct)
        {
            Chessboard.ActionRespondResult(this, Grid.GetChessPos(this), Activity.Self,
                Singular(CombatConduct.InstanceBuff(FightState.Cons.Invincible)),1);
        }

        public override IEnumerable<KeyValuePair<int, IEnumerable<Activity>>> OnRoundEnd()
        {
            if (Status.GetBuff(FightState.Cons.Invincible) > 0)
                return Singular(new KeyValuePair<int, IEnumerable<Activity>>(RoundAction.RoundBuffing,
                    Singular(Chessboard.InstanceRoundAction(this, this.Pos, Activity.Self,
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
                Singular(Chessboard.InstanceRoundAction(this, this.Pos, Activity.Self,
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