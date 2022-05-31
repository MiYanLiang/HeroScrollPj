using CorrelateLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.System.WarModule
{
    public class HeroOperator : CardOperator
    {
        //public HeroCombatInfo CombatInfo { get; private set; }

        /// <summary>
        /// 根据当前血量损失的<see cref="gap"/>%，根据<see cref="gapValue"/>的值倍增
        /// </summary>
        /// <param name="maxHp"></param>
        /// <param name="basicValue">起始值</param>
        /// <param name="gap"></param>
        /// <param name="gapValue"></param>
        /// <param name="hp"></param>
        /// <returns></returns>
        private static int HpDepletedRatioWithGap(int hp, int maxHp, int basicValue, int gap, int gapValue)
        {
            var value = (int)((maxHp - hp) * 1f / maxHp * 100);
            var multiply = value / gap;
            return basicValue + (multiply * gapValue);
        }
        /// <summary>
        /// 根据当前血量损失的<see cref="gap"/>%，根据<see cref="gapValue"/>的值倍增
        /// </summary>
        /// <param name="status"></param>
        /// <param name="basicValue"></param>
        /// <param name="gap"></param>
        /// <param name="gapValue"></param>
        /// <returns></returns>
        protected static int HpDepletedRatioWithGap(ChessStatus status, int basicValue, int gap, int gapValue) =>
            HpDepletedRatioWithGap(status.Hp, status.MaxHp, basicValue, gap, gapValue);

        protected int MagicResist { get; private set; }
        protected int Armor { get; private set; }
        protected int Dodge { get; private set; }
        public override void Init(IChessman card, ChessboardOperator chessboardOp)
        {
            var combatInfo = HeroCombatInfo.GetInfo(chessboardOp.HeroTable[card.CardId]);
            MagicResist = combatInfo.MagicResist;
            Armor = combatInfo.Armor;
            Dodge = combatInfo.DodgeRatio;
            base.Init(card, chessboardOp);
        }

        protected override void StartActions()
        {
            if (!Chessboard.OnHeroPerformAvailable(this))
            {
                NoSkillAction();
                return;
            }
            MilitaryPerforms();
        }

        private IChessPos GetTargetPos() => Chessboard.GetTargetByMode(this, TargetingMode);
        protected virtual ChessboardOperator.Targeting TargetingMode => ChessboardOperator.Targeting.Lane;
        protected void NoSkillAction()
        {
            var target = GetTargetPos();
            if (target == null) return;
            Chessboard.AppendOpActivity(this, target, Activity.Intentions.Offensive, BasicDamage(), 0, 0);
        }

        private CombatConduct[] BasicDamage() =>
            Helper.Singular(CombatConduct.InstanceDamage(InstanceId, StateDamage(), Style.Element));

        /// <summary>
        /// 兵种攻击，base攻击是基础英雄属性攻击
        /// </summary>
        /// <param name="skill">标记1以上为技能，如果超过1个技能才继续往上加</param>
        /// <returns></returns>
        protected virtual void MilitaryPerforms(int skill = 1)
        {
            var target = GetTargetPos();
            if (target.IsPostedAlive)
                Chessboard.AppendOpActivity(this, target, Activity.Intentions.Offensive,
                    Helper.Singular(InstanceGenericDamage()), 0, skill);
        }

        /// <summary>
        /// 武将技活动
        /// </summary>
        /// <param name="target">目标</param>
        /// <param name="intent">活动标记，查<see cref="Activity.Intentions"/></param>
        /// <param name="actId">组合技标记，-1 = 追随上套组合，0或大于0都是各别组合标记。例如大弓：攻击多人，但是组合技都标记为0，它將同时间多次攻击。而连弩的连击却是每个攻击标记0,1,2,3(不同标记)，这样它会依次执行而不是一次执行一组</param>
        /// <param name="skill">武将技标记，-1为隐式技能，0为普通技能，大于0为武将技</param>
        /// <param name="conducts">招式，每一式会有一个招式效果。如赋buff，伤害，补血等...一个活动可以有多个招式</param>
        /// <returns></returns>
        protected ActivityResult OnPerformActivity(IChessPos target, Activity.Intentions intent, int actId, int skill,
            params CombatConduct[] conducts) => OnPerformActivity(target, intent, conducts, actId, skill);
        /// <summary>
        /// 武将技活动
        /// </summary>
        /// <param name="target">目标</param>
        /// <param name="intent">活动标记，查<see cref="Activity.Intentions"/></param>
        /// <param name="actId">组合技标记，-1 = 追随上套组合，0或大于0都是各别组合标记。例如大弓：攻击多人，但是组合技都标记为0，它將同时间多次攻击。而连弩的连击却是每个攻击标记0,1,2,3(不同标记)，这样它会依次执行而不是一次执行一组</param>
        /// <param name="skill">武将技标记，-1为隐式技能，0为普通技能，大于0为武将技</param>
        /// <param name="conducts">招式，每一式会有一个招式效果。如赋buff，伤害，补血等...一个活动可以有多个招式</param>
        /// <param name="rePos">移位，-1为无移位，而大或等于0将会指向目标棋格(必须是空棋格)</param>
        /// <returns></returns>
        protected ActivityResult OnPerformActivity(IChessPos target, Activity.Intentions intent, CombatConduct[] conducts, int actId,
            int skill,int rePos = -1)
        {
            if (Chessboard.GetCondition(this, CardState.Cons.Imprisoned) > 0) return null;
            return Chessboard.AppendOpActivity(this, target, intent, conducts, actId: actId, skill: skill, rePos);
        }

        /// <summary>
        /// 自buff活动。
        /// </summary>
        /// <param name="con"></param>
        /// <param name="value"></param>
        /// <param name="skill"></param>
        protected void SelfBuffering(CardState.Cons con, int value, int skill = 0)
        {
            if (Chessboard.GetCondition(this, CardState.Cons.Imprisoned) > 0) return;
            Chessboard.InstanceChessboardActivity(IsChallenger, this, Activity.Intentions.Self,
                Helper.Singular(CombatConduct.InstanceBuff(InstanceId, con, value)), skill: skill);
        }


        /// <summary>
        /// 法术免伤
        /// </summary>
        /// <returns></returns>
        public override int GetMagicArmor() => MagicResist;

        /// <summary>
        /// 物理免伤
        /// </summary>
        /// <returns></returns>
        public override int GetPhysicArmor() => Armor;

        /// <summary>
        /// 根据通用伤害，根据几率暴击和会心
        /// </summary>
        /// <returns></returns>
        protected CombatConduct InstanceGenericDamage(float additionDamage = 0)
        {
            var damage = StateDamage() + additionDamage;
            if (Chessboard.IsRouseDamagePass(this))
            {
                var rouse = RouseAddOn();
                if (rouse > 0)
                    return CombatConduct.Instance(damage, 0, rouse, Style.Element,
                        CombatConduct.DamageKind, 0, InstanceId);
            }

            if (Chessboard.IsCriticalDamagePass(this))
            {
                var critical = CriticalAddOn();
                if (critical > 0) return CombatConduct.InstanceDamage(InstanceId, damage, critical, 0, Style.Element);
            }

            return CombatConduct.InstanceDamage(InstanceId, damage, Style.Element);
        }

        /// <summary>
        /// 根据状态算出基础伤害
        /// </summary>
        /// <returns></returns>
        protected override int StateDamage() => Chessboard.GetCompleteDamageWithBond(this);

        protected override int StateIntelligent() => Chessboard.GetHeroBuffedIntelligent(this);

        protected override int StateSpeed() => Chessboard.GetOperatorBuffedSpeed(this);

        /// <summary>
        /// 会心加成
        /// </summary>
        /// <returns></returns>
        private float RouseAddOn() => StateDamage();

        /// <summary>
        /// 暴击加成
        /// </summary>
        /// <returns></returns>
        private float CriticalAddOn() => StateDamage() * 0.5f;
        public override int GetDodgeRate() => Dodge;

        protected int CountRate(CombatConduct conduct, int basic, int critical = 0, int rouse = 0) =>
            CountRate(Damage.GetType(conduct), basic, critical, rouse);
        protected int CountRate(Damage.Types type,int basic, int critical, int rouse)
        {
            switch (type)
            {
                case Damage.Types.General:
                    return basic;
                case Damage.Types.Critical:
                    return basic + critical;
                case Damage.Types.Rouse:
                    return basic + rouse;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 智力差
        /// </summary>
        /// <returns></returns>
        protected int StateIntelligentDiff(IChessOperator op) =>
            StateIntelligent() - Chessboard.GetHeroBuffedIntelligent(op);

        protected static MilitaryNotValid MilitaryNotValidError(HeroOperator op) =>
            new MilitaryNotValid(nameof(op.Style.Military), op.Style.Military.ToString());
        protected class MilitaryNotValid : ArgumentOutOfRangeException
        {
            public MilitaryNotValid(string paramName, string message) : base(paramName, message)
            {

            }
        }
    }
    /// <summary>
    /// 短弓
    /// </summary>
    public class DuanGongOperator : HeroOperator
    {
        protected override ChessboardOperator.Targeting TargetingMode => ChessboardOperator.Targeting.Any;

    }

    /// <summary>
    /// 短弩
    /// </summary>
    public class DuanNuOperator : HeroOperator
    {
        protected override ChessboardOperator.Targeting TargetingMode => ChessboardOperator.Targeting.Contra;

    }
    /// <summary>
    /// 壮士
    /// </summary>
    public class ZhuangShiOperator : HeroOperator 
    {
        protected override ChessboardOperator.Targeting TargetingMode => ChessboardOperator.Targeting.Lane;
    }

    /// <summary>
    /// 文士
    /// </summary>
    public class WenShiOperator : HeroOperator
    {
        protected override ChessboardOperator.Targeting TargetingMode => ChessboardOperator.Targeting.AnyHero;
    }

    /// <summary>
    /// (1)近战 - 武将受到重创时，为自身添加1层【护盾】，可叠加。
    /// </summary>
    public class JinZhanOperator : HeroOperator
    {
        protected int GetShieldRate()//根据兵种id获取概率
        {
            switch (Style.Military)
            {
                case 1: return 20;
                case 66: return 30;
                case 67: return 50;
            }
            throw MilitaryNotValidError(this);
        }

        protected int RouseShieldRate => 60;
        protected int CriticalShieldRate => 30;

        protected override void OnSufferConduct(Activity activity, IChessOperator offender = null)
        {
            var type = Damage.GetType(activity);
            OnPerformActivity(Chessboard.GetChessPos(this), Activity.Intentions.Self, actId: 0, skill: 1,
                CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Shield,
                    rate: CountRate(type, GetShieldRate(), CriticalShieldRate, RouseShieldRate)));
        }

        protected override void MilitaryPerforms(int skill = 1) => base.MilitaryPerforms(0);
    }

    /// <summary>
    /// (9)飞骑 - 骑马舞枪，攻击时，有概率连续攻击2次。
    /// </summary>
    public class FeiQiOperator : HeroOperator
    {
        private int ComboTimes()
        {
            switch (Style.Military)
            {
                case 9: return 2;
                case 60: return 3;
                case 136: return 5;
                default: throw MilitaryNotValidError(this);
            }
        }
        private int ComboRate => 40;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetLaneTarget(this);
            if (target == null) return;
            var combos = Chessboard.IsRandomPass(ComboRate) ? ComboTimes() : 1;
            for (int i = 0; i < combos; i++)
            {
                OnPerformActivity(target, Activity.Intentions.Offensive, i, 1, InstanceGenericDamage());
                if (Chessboard.GetStatus(this).IsDeath) break;
                if (Chessboard.GetStatus(this).UnAvailable) break;
                if (Chessboard.GetStatus(target.Operator).IsDeath) break;
            }
        }

    }
    /// <summary>
    /// (16)骠骑处理器
    /// </summary>
    public class PiaoQiOperator : HeroOperator
    {
        protected virtual int ComboRatio()
        {
            switch (Style.Military)
            {
                case 16: return 25;
                case 142: return 35;
                case 143: return 50;
                default: throw MilitaryNotValidError(this);
            }
        }

        private int CriticalAddOn => 30;
        private int RouseAddOn => 50;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetLaneTarget(this);
            if (target == null) return;
            bool combo = true;
            var actId = 0;
            do
            {
                int comboRate = ComboRatio();
                var hit = InstanceGenericDamage();
                var result = OnPerformActivity(target, Activity.Intentions.Offensive, actId, 1, hit);
                if (result == null || result.IsDeath) break;
                if (Chessboard.GetStatus(this).IsDeath) break;//自身死亡
                if (Chessboard.GetStatus(this).UnAvailable) break;
                if (hit.IsRouseDamage())
                    comboRate += RouseAddOn;
                if (hit.IsCriticalDamage())
                    comboRate += CriticalAddOn;
                combo = Chessboard.IsRandomPass(comboRate);
                actId++;
            } while (combo);
        }

    }
    /// <summary>
    /// 枪骑
    /// </summary>
    public class QiangQiOperator : HeroOperator
    {
        protected virtual int ComboRatio()
        {
            switch (Style.Military)
            {
                case 160: return 15;
                case 161: return 20;
                case 162: return 35;
                default: throw MilitaryNotValidError(this);
            }
        }
        protected virtual float PenetrateDecreases() 
        {
            switch (Style.Military) 
            {
                case 160: return 0.5f;
                case 161: return 0.7f;
                case 162: return 0.9f;
                default: throw MilitaryNotValidError(this);
            }
        } 
        private int CriticalAddOn => 20;
        private int RouseAddOn => 30;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetLaneTarget(this);
            if (target == null) return;
            bool combo = true;
            var actId = 0;
            do
            {
                int comboRate = ComboRatio();
                var hit = InstanceGenericDamage();
                var result = OnPerformActivity(target, Activity.Intentions.Offensive, actId, 1, hit);

                Penetrate(target,hit);

                if (result == null) break;
                if (!target.IsAliveHero) break;//目标死亡
                if (Chessboard.GetStatus(this).IsDeath) break;//自身死亡
                if (Chessboard.GetStatus(this).UnAvailable) break;//自身死亡
                if (hit.IsRouseDamage())
                    comboRate += RouseAddOn;
                if (hit.IsCriticalDamage())
                    comboRate += CriticalAddOn;
                combo = Chessboard.IsRandomPass(comboRate);
                actId++;
            } while (combo);

            void Penetrate(IChessPos p,CombatConduct c) 
            {
                var backPos = Chessboard.BackPos(p);
                if (backPos==null||backPos.Operator == null) { }
                else 
                {
                    OnPerformActivity(backPos, Activity.Intentions.Offensive, actId, -1, c.Clone(PenetrateDecreases()));             
                }
            }
        }

    }

    /// <summary>
    /// 58  铁骑 - 多铁骑上阵发动【连环战马】。所有铁骑分担伤害，并按铁骑数量提升伤害。
    /// </summary>
    public class TieQiOperator : HeroOperator
    {
        public static bool IsChainable(IChessOperator op)
        {
            switch (op.Style.Military)
            {
                case 58:
                case 152:
                case 153:
                    return true;
                default: return false;
            }
        }
        private const int ChainMax = 8;

        private int ArmorRate()
        {
            switch (Style.Military)
            {
                case 58: return 3;
                case 152:return 5;
                case 153:return 7;
                default: throw MilitaryNotValidError(this);
            }
        }

        private int DamageRate()
        {
            switch (Style.Military)
            {
                case 58: return 5;
                case 152: return 7;
                case 153: return 10;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetLaneTarget(this);
            if (Chessboard.GetCondition(this, CardState.Cons.Chained) <= 0)
            {
                base.MilitaryPerforms(0);
                return;
            }

            if (!GetChained().Any())
            {
                base.MilitaryPerforms(0);
                return;
            }

            var conduct = InstanceGenericDamage();
            OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 1, conduct);
        }

        public override int OnSpritesValueConvert(PosSprite[] sprites, CardState.Cons con)
        {
            int rate;
            switch (con)
            {
                case CardState.Cons.ArmorUp:
                    rate = ArmorRate();
                    break;
                case CardState.Cons.StrengthUp:
                    rate = DamageRate();
                    break;
                default:
                    rate = 0;
                    break;
            }
            return rate * Math.Min(sprites.Length, ChainMax);
        }

        private IChessPos[] GetChained() => ChainSprite.GetChained(Chessboard, this, IsChainable).ToArray();

        public override void OnRoundStart() => UpdateChain();
        public override void OnRePosTrigger(int pos) => UpdateChain();

        private void UpdateChain()
        {
            var chainedList = GetChained();
            var chainSprite = Chessboard.GetSpritesInChessPos(this)
                .FirstOrDefault(s => s.GetKind() == PosSprite.Kinds.Chained && s.Lasting == InstanceId);

            if (chainSprite != null &&
                chainedList.Length < 2)
            {
                ChainSprite.RemoveSprite(Chessboard, chainSprite);
                return;
            }

            if (chainedList.Length < 2 || chainSprite != null) return;
            ChainSprite.UpdateChain(Chessboard, chainedList);
        }

        public override void OnSomebodyDie(ChessOperator death)
        {
            if (IsChainable(death)) UpdateChain();
        }
    }

    /// <summary>
    /// 146 弓骑
    /// </summary>
    public class GongQiOperator : HeroOperator
    {
        private bool IsSameType(int value)
        {
            switch (value)
            {
                case 146: 
                case 147: 
                case 148: 
                    return true;
                default: return false;
            }
        }

        private int CoordinationRate()
        {
            switch (Style.Military)
            {
                case 146: return 30;
                case 147: return 50;
                case 148: return 80;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override void MilitaryPerforms(int skill = 1)
        {
            var marked = Chessboard.GetMarked(this, IsSameType);
            var combat = InstanceGenericDamage();
            if (marked != null)
            {
                combat.Multiply(1 + CoordinationRate() * 0.01f);
                OnPerformActivity(marked, Activity.Intentions.Offensive, -1, 2, combat);
                return;
            }
                                    
            //如果对面没有标记         
            
            var target = Chessboard.GetRivals(this)
                .Select(p=>new {p,random= Chessboard.Randomize(3)})
                .OrderByDescending(p => p.p.Operator.IsRangeHero)
                .ThenByDescending(p => p.p.Operator.CardType == GameCardType.Hero)
                .ThenBy(p=>p.random)
                .FirstOrDefault();
            if (target?.p?.Operator == null)
            {
                base.MilitaryPerforms(0);
                return;
            }

            var result = OnPerformActivity(target.p, Activity.Intentions.Offensive, 0, 1, combat);
            if (!IsHit(result)) return;
            var mark = CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Mark, value: Style.Military);
            OnPerformActivity(target.p, Activity.Intentions.Offensive, -1, -1, mark);
        }
    }

    /// <summary>
    /// 149 狼骑
    /// </summary>
    public class LangQiOperator : HeroOperator
    {
        private bool IsSameType(IChessOperator op) => op.Style.ArmedType >= 0 &&
                                                      (op.Style.Military == 149 ||
                                                       op.Style.Military == 150 ||
                                                       op.Style.Military == 151);

        private int PerformRate()
        {
            switch (Style.Military)
            {
                case 149: return 40;
                case 150: return 60;
                case 151: return 80;
                default: throw MilitaryNotValidError(this);
            }
        }

        private int CriticalRate => 10;
        private int RouseRate => 30;
        protected override void MilitaryPerforms(int skill = 1)
        {
            var langQiList = Chessboard.GetFriendly(this, p => p.IsAliveHero && 
                                                          IsSameType(p.Operator)).ToArray();
            var combat = InstanceGenericDamage();

            var buffRate = CountRate(combat, PerformRate(), CriticalRate, RouseRate);
            if(Chessboard.IsRandomPass(buffRate))
            {
                Chessboard.InstanceSprite<ShadySprite>(Chessboard.GetChessPos(this), 1, 1, -1, skill);
                foreach (var langQi in langQiList)
                {
                    OnPerformActivity(langQi, Activity.Intentions.Friendly, actId: 0, skill: 1,
                        CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Murderous));
                }
            }

            var target = Chessboard.GetLaneTarget(this);
            var howl = Chessboard.GetCondition(this, CardState.Cons.Murderous);
            OnPerformActivity(target, Activity.Intentions.Offensive, 0, howl > 0 ? 2 : 0, combat);
            for (int i = 0; i < howl; i++)
            {
                if (target.IsPostedAlive && IsAlive)
                    OnPerformActivity(target, Activity.Intentions.Offensive, i + 1, 2, combat);
            }
        }

        public override void OnRoundEnd()
        {
            var howl = Chessboard.GetCondition(this, CardState.Cons.Murderous);
            if (howl > 0)
                OnPerformActivity(Chessboard.GetChessPos(this), Activity.Intentions.Self, actId: -1, skill: -1,
                    CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Murderous, value: -howl));
        }
    }

    /// <summary>
    /// 65  黄巾 - 水能载舟，亦能覆舟。场上黄巾数量越多，攻击越高。
    /// </summary>
    public class HuangJinOperator : HeroOperator
    {
        private const int YellowBand = (int)CardState.Cons.YellowBand;
        private const int Max = 15;

        public override int OnSpritesValueConvert(PosSprite[] sprites, CardState.Cons con)
        {
            var count = sprites.Where(s => s.Pos != Chessboard.GetChessPos(this).Pos && s.TypeId == YellowBand)
                .Take(Max).Count();
            return count * GetDamageRate();
        }

        protected override void MilitaryPerforms(int skill = 1) => base.MilitaryPerforms(Chessboard.GetCondition(this, CardState.Cons.YellowBand) > 0 ? 1 : 0);

        public static bool IsYellowBand(IChessOperator op)
        {
            switch (op.Style.Military)
            {
                case 65:
                case 127:
                case 128:
                    return true;
                default:
                    return false;
            }
        }

        protected virtual int GetDamageRate()
        {
            switch (Style.Military)
            {
                case 65: return 15;
                case 127: return 20;
                case 128: return 30;
                default:
                    throw MilitaryNotValidError(this);
            }
        }

        public override void OnRoundStart()
        {
            if (Chessboard.GetSpritesInChessPos(this).Any(s => s.GetKind() == PosSprite.Kinds.YellowBand)) return;
            var cluster = Chessboard.GetFriendly(this,
                p => p.IsAliveHero &&
                     p.Operator != this &&
                     IsYellowBand(p.Operator)).ToArray();
            if (cluster.Length == 0) return;
            Chessboard.InstanceSprite<YellowBandSprite>(Chessboard.GetChessPos(this), InstanceId, 1, -1);
        }

    }

    /// <summary>
    /// 57  藤甲 - 藤甲护体，刀枪不入。高度免疫物理伤害。
    /// </summary>
    public class TengJiaOperator : HeroOperator
    {
        private int PhysicArmorAddedValue() 
        {
            switch (Style .Military) 
            {
                case 57:return 20;
                case 96:return 30;
                case 97:return 50;
                default: throw MilitaryNotValidError(this);
            }
        }
        public override int GetPhysicArmor()
        {
            var armor = Armor;
            armor += PhysicArmorAddedValue();
            return armor;
        }
        protected override void OnMilitaryDamageConvert(CombatConduct conduct)
        {
            if (conduct.Element == CombatConduct.FireDmg)
            {
                conduct.Multiply(10);
            }
        }
    }
    /// <summary>
    /// 鬼兵
    /// </summary>
    public class GuiBingOperator : HeroOperator 
    {
        private int HealRatio() 
        {
            switch (Style.Military) 
            {
                case 98:return 20;
                case 99:return 25;
                case 100:return 30;
                default: throw MilitaryNotValidError(this);
            }
        }
        public override void OnRoundStart()
        {
            var stat = Chessboard.GetStatus(this);
            if (!stat.UnAvailable && stat.HpRate < 1) 
            {
                OnPerformActivity(Chessboard.GetChessPos(this), Activity.Intentions.Self, -1, 2, CombatConduct.InstanceHeal(stat.MaxHp * HealRatio() * 0.01f, InstanceId));
            }
        }
        private int GetHealRatio() 
        {
            switch (Style.Military) 
            {
                case 98: return 20;
                case 99: return 20;
                case 100: return 25;
                default: throw MilitaryNotValidError(this);
            }
        }
        public override void OnSomebodyDie(ChessOperator death)
        {
            var stat = Chessboard.GetStatus(this);
            if (stat.UnAvailable || stat.HpRate == 1||death.Style.ArmedType<0||!Chessboard.IsRandomPass(GetHealRatio()))
            {
                return;
            }
            else
            {
                OnPerformActivity(Chessboard.GetChessPos(this), Activity.Intentions.Self, -1, 3, CombatConduct.InstanceHeal(stat.MaxHp, InstanceId));
            }
        }
    }
    /// <summary>
    /// 59  长枪 - 手持长枪，攻击时，可穿刺攻击目标身后1个单位。
    /// </summary>
    public class ChangQiangOperator : HeroOperator
    {
        /// <summary>
        /// 刺穿单位数
        /// </summary>
        private int PenetrateUnits()
        {
            switch (Style.Military)
            {
                case 59: return 1;
                case 14: return 2;
                case 101: return 2;
                default: throw MilitaryNotValidError(this);
            }
        }

        private float[] PenetrateDecreases = new[] { 0.8f, 0.6f };

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetLaneTarget(this);
            var damage = InstanceGenericDamage();

            OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 1, damage);
            for (var i = 1; i < PenetrateUnits() + 1; i++)
            {
                var backPos = Chessboard.BackPos(target);
                target = backPos;
                if (target == null) break;
                if (target.Operator == null) continue;
                OnPerformActivity(target, Activity.Intentions.Attach, actId: 0, skill: -1,
                    damage.Clone(PenetrateDecreases[i - 1]));
            }
        }
    }

    /// <summary>
    /// 56  蛮族 - 剧毒粹刃，攻击时有概率使敌方武将【中毒】。
    /// </summary>
    public class ManZuOperator : HeroOperator
    {
        private int PoisonRate()
        {
            switch (Style.Military)
            {
                case 56: return 30;
                case 131: return 50;
                case 132: return 70;
                default: throw MilitaryNotValidError(this);
            }
        }

        private int RouseAddOn => 30;
        private int CriticalAddOn => 15;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetLaneTarget(this);
            if (!target.IsAliveHero)
            {
                base.MilitaryPerforms(1);
                return;
            }
            var combat = InstanceGenericDamage();
            var poisonRate = PoisonRate();
            var result = OnPerformActivity(target, Activity.Intentions.Offensive, 0, 2, combat);
            if (IsHit(result))
                OnPerformActivity(target, Activity.Intentions.Offensive, -1, -1,
                    CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Poison, rate: CountRate(combat,poisonRate, CriticalAddOn, RouseAddOn)));
        }
    }

    /// <summary>
    /// 55  火船 - 驱动火船，可引燃敌方武将，或自爆对敌方造成大范围伤害及【灼烧】。
    /// </summary>
    public class HuoChuanOperator : HeroOperator
    {
        private float ExplodeDamageRate()
        {
            switch (Style.Military)
            {
                case 55: return 4f;
                case 196: return 5f;
                case 197: return 7f;
                default: throw MilitaryNotValidError(this);
            }
        }

        private int ExplodeBurningRate()
        {
            switch (Style.Military) 
            {
                case 55:return 70;
                case 196:return 80;
                case 197:return 100;
                default:throw MilitaryNotValidError(this);
            }
        }
        private int BurningRate()
        {
            switch (Style.Military)
            {
                case 55: return 50;
                case 196: return 70;
                case 197: return 90;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetLaneTarget(this);
            if (Chessboard.GetStatus(this).HpRate < 0.5)
            {
                var explode = CombatConduct.InstanceDamage(InstanceId, (int)(StateDamage() * ExplodeDamageRate()),
                    Style.Element);
                var burnBuff = CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Burn,
                    rate: CountRate(explode, ExplodeBurningRate()));
                var surrounded = Chessboard.GetNeighbors(target, false).ToList();
                surrounded.Insert(0, target);
                for (var i = 0; i < surrounded.Count; i++)
                {
                    var chessPos = surrounded[i];
                    OnPerformActivity(chessPos, Activity.Intentions.Inevitable, actId: 0, skill: i == 0 ? 2 : -1, explode, burnBuff);
                }

                OnPerformActivity(Chessboard.GetChessPos(this), Activity.Intentions.Self, actId: -1, skill: -1,
                    CombatConduct.InstanceKilling(InstanceId));
                return;
            }

            var damage= InstanceGenericDamage();
            OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 1, damage,
                CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Burn, rate: CountRate(damage, BurningRate())));
        }
    }

    /// <summary>
    /// 53  隐士 - 召唤激流，攻击敌方3个武将，造成伤害并将其击退。
    /// </summary>
    public class YinShiOperator : HeroOperator
    {
        private int Targets()
        {
            switch (Style.Military)
            {
                case 53: return 3;
                case 54: return 5;
                case 210: return 7;
                default: throw MilitaryNotValidError(this);
            }
        }
        private int PushBackRate => 20;
        private int CriticalRate => 10;
        private int RouseRate => 20;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Chessboard.GetRivals(this,
                    p => p.IsPostedAlive &&
                         p.Operator.CardType == GameCardType.Hero)
                .OrderBy(p => p.Pos)
                .Take(Targets())
                .ToArray();

            if (targets.Length == 0)
            {
                base.MilitaryPerforms(0);
                return;
            }

            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                var damage = InstanceGenericDamage();
                damage.Element = CombatConduct.WaterDmg;
                var intDif = StateIntelligentDiff(target.Operator)/5;
                var pushBackRate = Math.Max(0, CountRate(damage, intDif + PushBackRate, CriticalRate, RouseRate));
                var backPos = Chessboard.BackPos(target);
                var rePos = -1;
                if (backPos != null &&
                    backPos.Operator == null &&
                    Chessboard.IsRandomPass(pushBackRate)) rePos = backPos.Pos;

                OnPerformActivity(target, Activity.Intentions.Offensive, Helper.Singular(damage), actId: 0, skill: 1, rePos: rePos);
            }
        }
    }

    /// <summary>
    /// 47  说客 - 随机选择3个敌方武将进行游说，有概率对其造成【怯战】，无法暴击和会心一击。
    /// </summary>
    public class ShuiKeOperator : HeroBuffingOperator
    {
        protected override int Targets()
        {
            switch (Style.Military)
            {
                case 47: return 3;
                case 48: return 5;
                case 222: return 7;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override int SkillRate(IChessOperator op) => 5 + StateIntelligentDiff(op) / 5;

        protected override CardState.Cons PerformState => CardState.Cons.Confuse;
    }

    /// <summary>
    /// 46  大美人 - 以倾国之姿激励友方武将，有概率使其获得【神助】，下次攻击时必定会心一击。
    /// </summary>
    public class QingGuoOperator : QingChengOperator
    {
        protected override int Targets()
        {
            switch (Style.Military)
            {
                case 46: return 2;
                case 233: return 3;
                case 234: return 5;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override int BuffRate => 10 + StateIntelligent() / 5;
        protected override int CriticalAddOn => 10;
        protected override int RouseAddOn => 20;
        protected override CardState.Cons Buff => CardState.Cons.ShenZhu;
    }

    /// <summary>
    /// 45  美人 - 以倾城之姿激励友方武将，有概率使其获得【内助】，下次攻击时必定暴击。
    /// </summary>
    public class QingChengOperator : HeroOperator
    {
        protected virtual int Targets()
        {
            switch (Style.Military)
            {
                case 45: return 2;
                case 231: return 3;
                case 232: return 5;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected virtual int BuffRate => 20 + StateIntelligent() / 5;
        protected virtual int CriticalAddOn => 15;
        protected virtual int RouseAddOn => 30;
        protected virtual CardState.Cons Buff => CardState.Cons.Neizhu;

        private CombatConduct BuffToFriendly(CombatConduct conduct) =>
            CombatConduct.InstanceBuff(InstanceId, Buff, rate: CountRate(conduct, BuffRate, CriticalAddOn, RouseAddOn));
        protected override void MilitaryPerforms(int skill = 1)
        {
            var combat = InstanceGenericDamage();
            var targets = Chessboard.GetFriendly(this,
                    p => p.IsAliveHero)
                .OrderByDescending(p => p.Operator.Style.Strength)
                .Take(Targets())
                .ToArray();
            if (!targets.Any())
            {
                base.MilitaryPerforms(0);
                return;
            }

            foreach (var target in targets)
                OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 1, BuffToFriendly(combat));
        }
    }

    /// <summary>
    /// 44  巾帼 - 巾帼不让须眉，攻击同时对目标造成【卸甲】，大幅降低其攻击和防御。
    /// </summary>
    public class JingGuoOperator : HeroOperator
    {
        private int DisarmedRate()
        {
            switch (Style.Military)
            {
                case 44: return 50;
                case 144: return 70;
                case 145: return 90;
                default: throw MilitaryNotValidError(this);
            }
        }

        private int CriticalAddOn => 15;
        private int RouseAddOn => 30;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetLaneTarget(this);
            if (!target.IsAliveHero)
            {
                base.MilitaryPerforms(1);
                return;
            }

            var conduct = InstanceGenericDamage();
            var result = OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 2, conduct);
            if (IsHit(result))
            {
                //如果对目标造成卸甲，将再次进行一次攻击
                //这一段不理想的代码
                var disarmedRate = CountRate(conduct, DisarmedRate(), CriticalAddOn, RouseAddOn);
                if (Chessboard.IsRandomPass(disarmedRate))
                { 
                OnPerformActivity(target, Activity.Intentions.Offensive, actId: -1, skill: -1,
                    CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Disarmed, value: 1, rate: 100));
                OnPerformActivity(target, Activity.Intentions.Offensive, actId: 1, 2, InstanceGenericDamage());
                }
            }
        }
    }

    /// <summary>
    /// 42  医士 - 分发草药，治疗1个友方武将。
    /// </summary>
    public class YiShiOperator : HeroOperator
    {
        protected virtual int Targets()
        {
            switch (Style.Military)
            {
                case 42: return 2;
                case 43: return 3;
                case 242: return 5;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected virtual int Healing() => StateDamage() * StateIntelligent() / 25;

        protected virtual Func<IChessPos, bool> TargetFilter() =>
            p => p.IsAliveHero && Chessboard.GetStatus(p.Operator).HpRate < 1;

        private int CriticalRate => 50;
        private int RouseRate => 100;
        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Chessboard.GetFriendly(this, TargetFilter())
                .OrderBy(p => Chessboard.GetStatus(p.Operator).HpRate)
                .Take(Targets()).ToArray();

            if (!targets.Any())
            {
                base.MilitaryPerforms(0);
                return;
            }

            var basicDamage = InstanceGenericDamage();

            var heal = CountRate(basicDamage, Healing(), CriticalRate, RouseRate);
            foreach (var target in targets)
            {
                if (heal <= 0) break;
                var stat = Chessboard.GetStatus(target.Operator);
                var healPoint = stat.MaxHp - stat.Hp;
                if (heal > healPoint)
                    heal -= healPoint;
                else healPoint = heal;
                OnPerformActivity(target, Activity.Intentions.Friendly, Helper.Singular(CombatConduct.InstanceHeal(healPoint, InstanceId)), actId: 0, skill: 1);
            }
        }

    }

    /// <summary>
    /// 41  敢死 - 武将陷入危急之时进入【死战】状态，将受到的伤害转化为自身血量数。
    /// </summary>
    public class GanSiOperator : HeroOperator
    {
        protected virtual int TriggerRate => 30;
        private int restTimes;
        private int GetTimes()
        {
            switch (Style.Military)
            {
                case 41: return 2;
                case 83: return 1;
                case 84: return 0;
                default: throw MilitaryNotValidError(this);
            }
        }

        public override void OnRoundEnd()
        {
            if (Chessboard.GetCondition(this, CardState.Cons.DeathFight) > 0)
            {
                SelfBuffering(CardState.Cons.DeathFight,-1);
                restTimes = GetTimes();
                return;
            }

            restTimes--;
        }

        protected override void OnAfterSubtractHp(CombatConduct conduct)
        {
            if (Chessboard.GetStatus(this).UnAvailable ||
                Chessboard.GetStatus(this).HpRate > TriggerRate * 0.01f ||
                Chessboard.GetCondition(this, CardState.Cons.DeathFight) > 0 ||
                restTimes > 0) return;
            OnPerformActivity(Chessboard.GetChessPos(this), Activity.Intentions.Self, actId: -1, skill: 1,
                CombatConduct.InstanceBuff(InstanceId, CardState.Cons.DeathFight));
        }

        protected override void MilitaryPerforms(int skill = 1) => base.MilitaryPerforms(0);
        /// <summary>
        /// 自身回血
        /// </summary>
        /// <param name="conduct"></param>
        protected override void OnMilitaryDamageConvert(CombatConduct conduct)
        {
            if (Chessboard.GetCondition(this, CardState.Cons.DeathFight) > 0)
            {
                OnPerformActivity(Chessboard.GetChessPos(this), Activity.Intentions.Self, -1, skill: 2,
                    CombatConduct.InstanceHeal(conduct.Total, InstanceId));
                conduct.SetZero();
            }
        }
    }

    /// <summary>
    /// 40  器械 - 神斧天工，最多选择3个建筑（包括大营）进行修复。修复单位越少，修复量越高。
    /// </summary>
    public class QiXieOperator : YiShiOperator
    {
        protected override int Targets()
        {
            switch (Style.Military)
            {
                case 40: return 2;
                case 240: return 3;
                case 241: return 5;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override int Healing() => StateDamage() * StateIntelligent() / 15;

        /// <summary>
        /// 获取目标的条件
        /// </summary>
        /// <returns></returns>
        protected override Func<IChessPos, bool> TargetFilter() =>
            p => p.IsPostedAlive &&
                 p.Operator.CardType != GameCardType.Hero &&
                 Chessboard.GetStatus(p.Operator).HpRate < 1;
    }

    /// <summary>
    /// 39  辅佐 - 为血量数最低的友方武将添加【防护盾】，可持续抵挡伤害。
    /// </summary>
    public class FuZuoOperator : HeroOperator
    {
        private int Targets()
        {
            switch (Style.Military)
            {
                case 39: return 1;
                case 238: return 2;
                case 239: return 3;
                default: throw MilitaryNotValidError(this);
            }
        }

        private int EaseShieldAddOn => StateDamage() * StateIntelligent() / 30;
        private int CriticalRate => 50;
        private int RouseRate => 100;
        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Chessboard.GetFriendly(this,
                    p => p.IsAliveHero &&
                         Chessboard.GetStatus(p.Operator)
                             .GetBuff(CardState.Cons.EaseShield) < CardState.EaseShieldMax)
                .Select(p => new { Chessboard.GetStatus(p.Operator).HpRate, p })
                .OrderBy(o => o.HpRate).Take(Targets()).Select(o => o.p).ToArray();

            if (!targets.Any())
            {
                base.MilitaryPerforms(0);
                return;
            }

            var basicDamage = InstanceGenericDamage();
            foreach (var target in targets)
            {
                OnPerformActivity(target, Activity.Intentions.Friendly, actId: 0, skill: 1,
                    CombatConduct.InstanceBuff(InstanceId, CardState.Cons.EaseShield, value: CountRate(basicDamage, EaseShieldAddOn, CriticalRate, RouseRate)));
            }
        }
    }

    /// <summary>
    /// 38  内政 - 稳定军心，选择有减益的友方武将，有概率为其清除减益状态。
    /// </summary>
    public class NeiZhengOperator : HeroOperator
    {
        private int Targets()
        {
            switch (Style.Military)
            {
                case 38: return 2;
                case 226: return 3;
                case 227: return 5;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Chessboard.GetFriendly(this,
                    p => p.IsPostedAlive &&
                         p.Operator.CardType == GameCardType.Hero)
                .Select(pos => new
                {
                    pos.Operator,
                    Buffs = CardState.NegativeBuffs.Sum(n => Chessboard.GetStatus(pos.Operator).GetBuff(n))
                }) //找出所有武将的负面数
                .Where(o => o.Buffs > 0)
                .OrderByDescending(b => b.Buffs).Take(Targets()).ToArray();

            if (targets.Length == 0)
            {
                base.MilitaryPerforms(0);
                return;
            }

            var rate = StateIntelligent() / 2;
            var combat = InstanceGenericDamage();
            var deplete = 1;
            if (combat.IsCriticalDamage())
                deplete += 1;
            if (combat.IsRouseDamage())
                deplete += 2;

            for (var i = 0; i < targets.Length; i++)
            {
                var combats = new List<CombatConduct>();
                if (Chessboard.IsRandomPass(rate))
                {
                    var target = targets[i];
                    var tarStat = Chessboard.GetStatus(target.Operator);
                    var buffs = tarStat.Buffs.Where(b => b.Value > 0)
                        .Join(CardState.NegativeBuffs, b => b.Key, n => (int)n,
                            (p, c) => new RandomPick { Buff = c, Value = p.Value, Weight = Chessboard.Randomize(1, 4) })
                        .OrderByDescending(o => o.Weight)
                        .ToArray();
                    var value = deplete;
                    for (int j = 0; j < buffs.Length; j++)
                    {
                        var con = buffs[Chessboard.Randomize(buffs.Length)];
                        if (con.Buff != CardState.Cons.Mark && value <= con.Value)
                        {
                            combats.Add(CombatConduct.InstanceBuff(InstanceId, con.Buff, -value));
                            break;
                        }

                        combats.Add(CombatConduct.InstanceBuff(InstanceId, con.Buff, -con.Value));
                        value -= con.Value;
                        if (value <= 0) break;
                    }

                    OnPerformActivity(Chessboard.GetChessPos(target.Operator), Activity.Intentions.Friendly,
                        actId: 0, skill: 1, combats.ToArray());
                }
            }
        }

        private class RandomPick : IWeightElement
        {
            public CardState.Cons Buff { get; set; }
            public int Value { get; set; }
            public int Weight { get; set; }
        }
    }

    /// <summary>
    /// 36  谋士 - 善用诡计，随机选择3个敌方武将，有概率对其造成【眩晕】，无法行动。
    /// </summary>
    public class MouShiOperator : HeroBuffingOperator
    {
        protected override int Targets()
        {
            switch (Style.Military)
            {
                case 36: return 3;
                case 37: return 5;
                case 217: return 7;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override int SkillRate(IChessOperator op) => 10 + (StateIntelligentDiff(op)) / 5;

        protected override CardState.Cons PerformState => CardState.Cons.Stunned;
    }

    /// <summary>
    /// 34  辩士 - 厉声呵斥，随机选择3个敌方武将，有概率对其造成【禁锢】，无法使用兵种特技。
    /// </summary>
    public class BianShiOperator : HeroBuffingOperator
    {
        protected override int Targets()
        {
            switch (Style.Military)
            {
                case 34: return 3;
                case 35: return 5;
                case 221: return 7;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override int SkillRate(IChessOperator op) => 15 + (StateIntelligentDiff(op)) / 5;
        

        protected override CardState.Cons PerformState => CardState.Cons.Imprisoned;
    }
    /// <summary>
    /// 对一定数量的对方武将类逐一对其发出武将技
    /// </summary>
    public abstract class HeroBuffingOperator : HeroOperator
    {
        protected abstract int Targets();
        /// <summary>
        /// 暴击增率
        /// </summary>
        protected virtual int CriticalRate => 5;
        /// <summary>
        /// 法术随机值
        /// </summary>
        protected abstract int SkillRate(IChessOperator op);

        /// <summary>
        /// 会心增率
        /// </summary>
        protected virtual int RouseRate => 10;
        protected abstract CardState.Cons PerformState { get; }
        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Chessboard.GetRivals(this,
                    p => p.IsPostedAlive && p.Operator.CardType == GameCardType.Hero)
                .Select(c => new WeightElement<IChessPos> { Obj = c, Weight = Chessboard.Randomize(3) + 1 })
                .Pick(Targets()).Select(c => c.Obj).ToArray();

            if (targets.Length == 0) base.MilitaryPerforms(0);

            var damage = InstanceGenericDamage();
            
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 1,
                    conducts: CombatConduct.InstanceBuff(InstanceId, PerformState,
                        rate: CountRate(damage, SkillRate(target.Operator), CriticalRate, RouseRate)));
            }
        }
    }

    /// <summary>
    /// 32  统帅 - 在敌阵中心纵火，火势每回合向外扩展一圈。多个统帅可接力纵火。
    /// </summary>
    public class TongShuaiOperator : HeroOperator
    {
        private float DamageRate()
        {
            switch (Style.Military)
            {
                case 32: return 1f;
                case 33: return 1.2f;
                case 209: return 1.5f;
                default: throw MilitaryNotValidError(this);
            }
        }
        private int CriticalRate => 10;
        private int RouseRate => 20;
        private int FireBurningRate => 30;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var damage = InstanceGenericDamage();
            damage.Multiply(DamageRate());
            damage.Element = CombatConduct.FireDmg;
            damage.Rate = CountRate(damage, 20 + StateIntelligent() / 5, CriticalRate, RouseRate);
            Chessboard.DelegateSpriteActivity<YeHuoSprite>(this, Chessboard.GetChessPos(!IsChallenger, 7),
                Helper.Singular(damage), actId: 0, skill: 1, lasting: 2, value: FireBurningRate);
        }
    }

    /// <summary>
    /// 30  毒士 - 暗中作祟，随机攻击3个敌方武将，有概率对其造成【中毒】。
    /// </summary>
    public class DuShiOperator : HeroOperator
    {
        private int Targets()
        {
            switch (Style.Military)
            {
                case 30: return 3;
                case 31: return 5;
                case 208: return 7;
                default: throw MilitaryNotValidError(this);
            }
        }

        private int BasicPoisonBasicRate => 10;
        private int CriticalRate => 5;
        private int RouseRate => 10;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Chessboard.GetRivals(this,
                    p => p.IsAliveHero)
                .Select(p => new
                {
                    chessPos = p,
                    Random = Chessboard.Randomize(4)
                }).OrderBy(p => p.Random).Take(Targets()).ToArray();
            if (targets.Length == 0) base.MilitaryPerforms(0);
            var damage = InstanceGenericDamage();
            damage.Element = CombatConduct.PoisonDmg;

            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i].chessPos;
                var BasicAddDif = BasicPoisonBasicRate + StateIntelligentDiff(target.Operator)/ 10;
                var posionRate = Math.Max(0, CountRate(damage, BasicAddDif, CriticalRate, RouseRate));
                damage.Rate = posionRate;
                OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 1, damage);
            }
            
        }
    }

    /// <summary>
    /// 元素执行者。
    /// </summary>
    public abstract class ElementOperator : HeroOperator
    {
        private bool isInit = false;
        protected abstract int Ultimate { get; }
        protected abstract int CriticalAddOn { get; }
        protected abstract int RouseAddOn { get; }
        protected abstract int ElementRate { get; }
        protected abstract int BasicElementRate { get; }
        protected abstract int CriticalElementAddOn { get; }
        protected abstract int RouseElementAddOn { get; }
        protected abstract PosSprite.Kinds SpriteKind { get; }
        protected abstract int ConductElement { get; }
        protected abstract int MurderousRate();
        protected abstract int PresetBuff();
        protected abstract int[] TargetRange();
        protected abstract int UltiAttackTimes();
        protected override void MilitaryPerforms(int skill = 1)
        {
            var murderous = Chessboard.GetCondition(this, CardState.Cons.Murderous);
            var range = TargetRange();
            var isUlti = murderous >= Ultimate;//是不是大招
            var attackTimes = isUlti ? UltiAttackTimes() : 1;
            if (isUlti)
                OnPerformActivity(Chessboard.GetChessPos(this), Activity.Intentions.Self, actId: -1, skill: -1,
                    CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Murderous, value: -Ultimate));

            for (var j = 0; j < attackTimes; j++)
            {
                var conduct = InstanceGenericDamage();
                var intelligent = StateIntelligent();
                conduct.Rate = BasicElementRate + intelligent / ElementRate;
                conduct.Element = ConductElement;
                var addOn = 0;
                switch (Damage.GetType(conduct))
                {
                    case Damage.Types.General:
                        break;
                    case Damage.Types.Critical:
                        addOn = CriticalAddOn;
                        conduct.Rate += CriticalElementAddOn;
                        break;
                    case Damage.Types.Rouse:
                        addOn = RouseAddOn;
                        conduct.Rate += RouseElementAddOn;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var elements = Chessboard.Randomize(range[0], range[1]) + addOn;
                var targetPoses = Chessboard.GetRivals(this, _ => true).Select(pos => new
                        { Obj = pos, Random = Chessboard.Randomize(4) }).OrderByDescending(c => c.Random).Take(elements)
                    .ToArray();
                foreach (var pos in targetPoses)
                {
                    Chessboard.CastSpriteActivity(this, pos.Obj, SpriteKind, Helper.Singular(conduct), actId: j,
                        isUlti ? 2 : 1);
                }
            }
        }

        public override void OnSomebodyDie(ChessOperator death)
        {
            if (death.Style.ArmedType < 0 ||
                death.IsChallenger == IsChallenger ||
                !Chessboard.IsRandomPass(MurderousRate())) return;
            OnPerformActivity(Chessboard.GetChessPos(this), Activity.Intentions.Self, actId: -1, skill: 3,
                CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Murderous));
        }

        public override void OnRoundStart()
        {
            if (!isInit)
            {
                isInit = true;
                SelfBuffering(CardState.Cons.Murderous, value: PresetBuff(), skill: 3);
            }
            else SelfBuffering(CardState.Cons.Murderous, value: 1, skill: 3);
        }
    }

    /// <summary>
    /// 28  术士 - 洞察天机，召唤3次天雷，随机落在敌阵中，并有几率造成【眩晕】。
    /// </summary>
    public class ShuShiOperator : ElementOperator
    {
        protected override int Ultimate => 3;
        protected override int CriticalAddOn => 1;
        protected override int RouseAddOn => 2;
        protected override int ElementRate => 10;
        protected override int BasicElementRate => 5;
        protected override int CriticalElementAddOn => 3;
        protected override int RouseElementAddOn => 5;
        protected override PosSprite.Kinds SpriteKind => PosSprite.Kinds.Thunder;
        protected override int ConductElement => CombatConduct.ThunderDmg;

        protected override int MurderousRate()
        {
            switch (Style.Military)
            {
                case 28: return 15;
                case 29: return 15;
                case 204: return 30;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override int PresetBuff()
        {
            switch (Style.Military)
            {
                case 28: return 1;
                case 29: return 2;
                case 204: return 3;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override int[] TargetRange()
        {
            switch (Style.Military)
            {
                case 28: return new[] { 2, 5 };
                case 29: return new[] { 3, 6 };
                case 204: return new[] { 4, 7 };
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override int UltiAttackTimes()
        {
            switch (Style.Military)
            {
                case 28: return 3;
                case 29: return 5;
                case 204: return 5;
                default: throw MilitaryNotValidError(this);
            }
        }
    }
    /// <summary>
    /// 68 狂士
    /// </summary>
    public class KuangShiOperator : ElementOperator
    {
        protected override int Ultimate => 3;
        protected override int CriticalAddOn => 1;
        protected override int RouseAddOn => 2;
        protected override int ElementRate => 10;
        protected override int BasicElementRate => 5;
        protected override int CriticalElementAddOn => 3;
        protected override int RouseElementAddOn => 5;

        protected override PosSprite.Kinds SpriteKind => PosSprite.Kinds.Earthquake;
        protected override int ConductElement => CombatConduct.EarthDmg;

        protected override int MurderousRate()
        {
            switch (Style.Military)
            {
                case 214: return 15;
                case 215: return 15;
                case 216: return 30;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override int PresetBuff()
        {
            switch (Style.Military)
            {
                case 214: return 1;
                case 215: return 2;
                case 216: return 3;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override int[] TargetRange()
        {
            switch (Style.Military)
            {
                case 214: return new[] { 2, 5 };
                case 215: return new[] { 3, 6 };
                case 216: return new[] { 4, 7 };
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override int UltiAttackTimes()
        {
            switch (Style.Military)
            {
                case 214: return 3;
                case 215: return 5;
                case 216: return 5;
                default: throw MilitaryNotValidError(this);
            }
        }
    }

    /// <summary>
    /// 26  军师 - 借助东风，攻击血量剩余百分比最少的3个敌方武将，有小概率将其绝杀。
    /// </summary>
    public class JunShiOperator : HeroOperator
    {
        private int Targets()
        {
            switch (Style.Military)
            {
                case 26: return 3;
                case 27: return 5;
                case 203: return 7;
                default: throw MilitaryNotValidError(this);
            }
        }

        private int BasicKillRate => 3;
        private int CriticalAddRate => 2;
        private int RouseAddRate => 4;
        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Chessboard.GetRivals(this, c => c.IsAliveHero)
                .Select(p => new { chessPos = p, random = Chessboard.Randomize(5) })
                .OrderBy(p => p.random)
                .Take(Targets()).ToArray();
            if (targets.Length == 0)
            {
                base.MilitaryPerforms(0);
                return;
            }

            var baseDamage = InstanceGenericDamage();
            baseDamage.Element = CombatConduct.WindDmg;

            var kill = CombatConduct.InstanceKilling(InstanceId);
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i].chessPos;
                var killingBasicAddDiffRate =  BasicKillRate + StateIntelligentDiff(target.Operator) / 10;
                var killingRate = CountRate(baseDamage, killingBasicAddDiffRate, CriticalAddRate, RouseAddRate);
                if (target.IsAliveHero &&
                    Chessboard.IsRandomPass(killingRate))
                {
                    OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 1, kill);
                    continue;
                }

                OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 1, baseDamage);
            }
        }
    }

    /// <summary>
    /// 25  刺客 - 深入敌方，选择远程单位中攻击最高的目标进行攻击。造成伤害，并为其添加【流血】效果。
    /// </summary>
    public class CiKeOperator : HeroOperator
    {
        private int BleedRate()
        {
            switch (Style.Military)
            {
                case 25: return 30;
                case 129: return 50;
                case 130: return 70;
                default: throw MilitaryNotValidError(this);
            }
        }

        private int CriticalAddOn => 15;
        private int RouseAddOn => 30;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetRivals(this)
                .Where(p => p.Operator.IsRangeHero)
                .Select(p => new { Pos = p, Random = Chessboard.Randomize(3) })
                .OrderByDescending(p => p.Random).FirstOrDefault()?.Pos;
            if (target == null) target = Chessboard.GetLaneTarget(this);
            var combat = InstanceGenericDamage();
            var result = OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 2, combat);
            if (IsHit(result))
                OnPerformActivity(target, Activity.Intentions.Offensive, actId: -1, skill: -1,
                    CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Bleed, 1,
                        rate: CountRate(combat, BleedRate(), CriticalAddOn, RouseAddOn)));
        }
    }

    /// <summary>
    /// 24  投石车 - 发射巨石，随机以敌方大营及大营周围单位为目标，进行攻击。
    /// </summary>
    public class TouShiCheOperator : HeroOperator
    {
        private int[] TargetPoses = new[] { 12, 15, 16, 17 };
        private int ComboRate()
        {
            switch (Style.Military)
            {
                case 24: return 1;
                case 178: return 2;
                case 179: return 2;
                default: throw MilitaryNotValidError(this);
            }
        }
        private float IntelligentRate => 40f;
        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Chessboard.GetRivals(this,
                    p => p.IsPostedAlive)
                .Join(TargetPoses, t => t.Pos, p => p, (t, _) => t).ToArray();
            for (int i = 0; i < ComboRate(); i++)
            {
                var damage = InstanceGenericDamage();
                damage.Element = CombatConduct.MechanicalDmg;
                damage.Multiply(StateIntelligent() / IntelligentRate);
                var target = targets.RandomPick();
                OnPerformActivity(target, Activity.Intentions.Offensive, i, skill: 1, damage);
            }
        }
    }

    /// <summary>
    /// 23  攻城车 - 驱动冲车，对武将造成少量伤害，对塔和陷阱造成高额伤害。
    /// </summary>
    public class GongChengCheOperator : HeroOperator
    {
        private float CombatRate()
        {
            switch (Style.Military)
            {
                case 23: return 1;
                case 176: return 2;
                case 177: return 3;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetLaneTarget(this);
            if (target.IsAliveHero)
            {
                base.MilitaryPerforms(0);
                return;
            }

            if (!target.IsPostedAlive) return;
            var damage = InstanceGenericDamage();
            damage.Element = CombatConduct.MechanicalDmg;
            for (int i = 0; i < CombatRate(); i++) 
            { 
                OnPerformActivity(target, Activity.Intentions.Offensive, actId: i, skill: 1, damage);
                if (!target.IsPostedAlive) break;
            }
        }

        public static bool IsGongChengChe(IChessOperator chess)
        {
            return chess.Style.Military == 23 ||
                   chess.Style.Military == 176 ||
                   chess.Style.Military == 177;
        }
    }

    /// <summary>
    /// 22  战车 - 战车攻击时，有概率对目标造成【眩晕】。对已【眩晕】目标造成高额伤害。
    /// </summary>
    public class ZhanCheOperator : HeroOperator
    {
        private int StunningRate()
        {
            switch (Style.Military)
            {
                case 22: return 30;
                case 172: return 50;
                case 173: return 70;
                default: throw MilitaryNotValidError(this);
            }
        }
        private int CriticalRate => 15;
        private int RouseRate => 30;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetLaneTarget(this);
            if (target.Operator.CardType != GameCardType.Hero) 
            {
                base.MilitaryPerforms(0);
                return;
            }
            var damage = InstanceGenericDamage();
            if (Chessboard.GetCondition(target.Operator, CardState.Cons.Stunned) > 0)
            {
                damage.Multiply(2);
                OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 1, damage);
                return;
            }

            OnPerformActivity(target, Activity.Intentions.Offensive, actId: -1, skill: 1, damage,
                CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Stunned, value: 1,
                    rate: CountRate(damage, StunningRate(), CriticalRate, RouseRate)));
        }
    }

    /// <summary>
    /// 21  艨艟 - 驾驭战船，攻击时可击退敌方武将。否则对其造成双倍伤害。
    /// </summary>
    public class ZhanChuanOperator : HeroOperator
    {
        private float DamageRate()
        {
            switch (Style.Military)
            {
                case 21: return 2f;
                case 194: return 2f;
                case 195: return 2.5f;
                default: throw MilitaryNotValidError(this);
            }
        }
        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetLaneTarget(this);
            var combatConducts = new List<CombatConduct>();
            var backPos = Chessboard.BackPos(target);
            var rePos = backPos != null &&
                        backPos.Operator == null &&
                        target.IsAliveHero ? backPos.Pos : -1;
            combatConducts.Add(rePos == -1 && target.IsAliveHero
                ? InstanceGenericDamage((int)(StateDamage() * (DamageRate() - 1f)))
                : InstanceGenericDamage());
            OnPerformActivity(target, Activity.Intentions.Offensive, combatConducts.ToArray(), 0, 1, rePos);
        }
    }

    /// <summary>
    /// 20  大弓 - 乱箭齐发，最多攻击3个目标。目标数量越少，造成伤害越高。
    /// </summary>
    public class DaGongOperator : HeroOperator
    {
        private int Targets()
        {
            switch (Style.Military)
            {
                case 20: return 3;
                case 52: return 5;
                case 184: return 7;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Chessboard.GetRivals(this).Select(p => new { chessPos = p, random = Chessboard.Randomize(5)})
                .OrderBy(p => p.random).Take(Targets()).ToArray();
            if (targets.Length == 0) return;
            var perform = InstanceGenericDamage();
            perform.Multiply(0.6f);
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i].chessPos;
                OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 1, perform);
            }
        }
    }
    /// <summary>
    /// 火弓
    /// </summary>
    public class HuoGongOperator : HeroOperator
    {
        private int Targets()
        {
            switch (Style.Military)
            {
                case 185: return 3;
                case 186: return 5;
                case 187: return 7;
                default: throw MilitaryNotValidError(this);
            }
        }

        private int BurnRate() 
        {
            switch (Style.Military) 
            {
                case 185:return 50;
                case 186:return 50;
                case 187:return 70;
                default:throw MilitaryNotValidError(this);
            }
        }
        private int CriticalAddOn => 10;
        private int RouseAddOn => 30;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Chessboard.GetRivals(this).Select(p => new { chessPos = p, random = Chessboard.Randomize(5) })
                .OrderBy(p => p.random).Take(Targets()).ToArray();
            if (targets.Length == 0) return;
            var perform = InstanceGenericDamage();
            perform.Multiply(0.5f);

            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i].chessPos;
                OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 1, perform,
                    CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Burn, rate: CountRate(perform, BurnRate(), CriticalAddOn, RouseAddOn)));
            }
        }
    }
    /// <summary>
    /// 狼弓
    /// </summary>
    public class LangGongOperator : HeroOperator
    {
        private int Targets()
        {
            switch (Style.Military)
            {
                case 191: return 3;
                case 192: return 5;
                case 193: return 7;
                default: throw MilitaryNotValidError(this);
            }
        }

        private int PosionRate()
        {
            switch (Style.Military)
            {
                case 191: return 30;
                case 192: return 30;
                case 193: return 50;
                default: throw MilitaryNotValidError(this);
            }
        }
        private int CriticalAddOn => 5;
        private int RouseAddOn => 15;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Chessboard.GetRivals(this).Select(p => new { chessPos = p, random = Chessboard.Randomize(5) })
                .OrderBy(p => p.random).Take(Targets()).ToArray();
            if (targets.Length == 0) return;
            var perform = InstanceGenericDamage();
            perform.Multiply(0.5f);

            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i].chessPos;
                OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 1, perform,
                    CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Poison, rate: CountRate(perform, PosionRate(), CriticalAddOn, RouseAddOn)));
            }
        }
    }
    /// <summary>
    /// 重弩车
    /// </summary>
    public class ZhongNuCheOperator : HeroOperator 
    {
        private int PenetrateUnits(Damage.Types types)
        {
            switch (types)
            {
                case Damage.Types.General: return 1;
                case Damage.Types.Critical: return 2;
                case Damage.Types.Rouse: return 3;
                default: throw MilitaryNotValidError(this);
            }
        }

        private float[] PenetrateDecreases ()
        {
            switch (Style.Military) 
            {
                case 188:return new[]{ 0.5f, 0.3f, 0.2f };
                case 189: return new[] { 0.6f, 0.4f, 0.3f }; 
                case 190: return new[] { 0.7f, 0.5f, 0.4f }; 
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetLaneTarget(this);
            var damage = InstanceGenericDamage();
            damage.Element = CombatConduct.MechanicalDmg;

            var damageType = Damage.GetType(damage);
            OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 1, damage);

            var targetsNum = PenetrateUnits(damageType);
            for (var i = 1; i < targetsNum + 1; i++)
            {
                var backPos = Chessboard.BackPos(target);
                target = backPos;
                if (target == null) break;
                if (target.Operator == null) continue;
                OnPerformActivity(target, Activity.Intentions.Attach, actId: 0, skill: -1,
                    damage.Clone(PenetrateDecreases()[i - 1]));
            }
        }
    }
    /// <summary>
    /// 19  连弩 - 武将攻击时，有几率连续射击2次。
    /// </summary>
    public class LianNuOperator : HeroOperator
    {
        private int Combo()
        {
            switch (Style.Military)
            {
                case 19: return 2;
                case 51: return 3;
                case 180: return 5;
                default: throw MilitaryNotValidError(this);
            }
        }

        private int ComboRate => 50;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetLaneTarget(this);
            var combo = Chessboard.IsRandomPass(ComboRate) ? Combo() : 1;
            for (int i = 0; i < combo; i++)
            {
                var result = OnPerformActivity(target, Activity.Intentions.Offensive,
                    i, 1, InstanceGenericDamage());
                if (result == null || result.IsDeath) return;
            }
        }
    }
    /// <summary>
    /// 飞弩
    /// </summary>
    public class FeiNuOprater : HeroOperator 
    {
        private int Combo()
        {
            switch (Style.Military)
            {
                case 181: return 3;
                case 182: return 5;
                case 183: return 7;
                default: throw MilitaryNotValidError(this);
            }
        }

        private int ComboRate => 40;

        protected override void MilitaryPerforms(int skill = 1)
        {
            for (int i = 0; i < Combo(); i++)
            {
                var damage = InstanceGenericDamage();
                damage.Multiply(ComboRate * 0.01f);
                var target = Chessboard.GetRivals(this, p => p.IsPostedAlive)
                    .Select(p => new { p, random = Chessboard.Randomize(5) })
                    .OrderBy(o => o.random).FirstOrDefault();
                if (target == null || target.p == null) continue;
                OnPerformActivity(target.p, Activity.Intentions.Offensive,
                    actId: i, skill: 1, damage);
            }
        }
    }

    /// <summary>
    /// 18  大斧 - 挥动大斧，攻击时，可破除敌方护盾。受击目标血量越低，造成的伤害越高。
    /// </summary>
    public class DaFuOperator : HeroOperator
    {
        public override bool IsIgnoreShieldUnit => true;

        private int BreakShields()
        {
            switch (Style.Military)
            {
                case 18: return 3;
                case 106: return 5;
                case 107: return 7;
                default: throw MilitaryNotValidError(this);
            }
        }
        private int DamageRate()
        {
            switch (Style.Military)
            {
                case 18: return 10;
                case 106: return 15;
                case 107: return 20;
                default: throw MilitaryNotValidError(this);
            }
        }
        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetLaneTarget(this);
            if (target.IsAliveHero)
            {
                var targetStatus = Chessboard.GetStatus(target.Operator);
                var targetShields = Chessboard.GetCondition(target.Operator, CardState.Cons.Shield);
                var shieldBalance = targetShields - BreakShields();
                var dmgGapValue = StateDamage() * DamageRate() * 0.01; //每10%掉血增加数
                var additionDamage = HpDepletedRatioWithGap(targetStatus, 0, 10, (int)dmgGapValue);
                var performDamage = InstanceGenericDamage(additionDamage);
                var combat = new List<CombatConduct>();
                if (shieldBalance > 0)
                {
                    combat.Add(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Shield, -BreakShields()));
                }
                else
                {
                    combat.Add(performDamage);
                    combat.Add(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Shield, -targetShields));
                }
                OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 1, combat.ToArray());
                return;
            }
            OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 1, Helper.Singular(InstanceGenericDamage()));
        }
    }
    public class FuQiOperator : HeroOperator
    {
        public override bool IsIgnoreShieldUnit => true;

        private int BreakShields()
        {
            switch (Style.Military)
            {
                case 163: return 3;
                case 164: return 5;
                case 165: return 7;
                default: throw MilitaryNotValidError(this);
            }
        }
        private int DamageRate()
        {
            switch (Style.Military)
            {
                case 163: return 10;
                case 164: return 15;
                case 165: return 20;
                default: throw MilitaryNotValidError(this);
            }
        }
        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetLaneTarget(this);
            if (target.IsAliveHero)
            {
                var targetStatus = Chessboard.GetStatus(target.Operator);
                var targetShields = Chessboard.GetCondition(target.Operator, CardState.Cons.Shield);
                var shieldBalance = targetShields - BreakShields();
                var dmgGapValue = StateDamage() * DamageRate() * 0.01; //每10%掉血增加数
                var additionDamage = HpDepletedRatioWithGap(targetStatus, 0, 10, (int)dmgGapValue);
                var performDamage = InstanceGenericDamage(additionDamage);
                var combat = new List<CombatConduct>();
                if (shieldBalance > 0)
                {
                    combat.Add(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Shield, -BreakShields()));
                }
                else
                {
                    combat.Add(performDamage);
                    combat.Add(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Shield, -targetShields));
                }
                OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 1, combat.ToArray());
                return;
            }
            OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 1, Helper.Singular(InstanceGenericDamage()));
        }
    }
    /// <summary>
    /// 17  大刀 - 斩杀当前目标后，武将继续攻击下一个目标。每次连斩后攻击提升。
    /// </summary>
    public class DaDaoOperator : HeroOperator
    {
        private int DamageRate()
        {
            switch (Style.Military)
            {
                case 17: return 15;
                case 104: return 20;
                case 105: return 30;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override void MilitaryPerforms(int skill = 1)
        {
            var paths = Chessboard.GetAttackPath(this);
            var targets = paths.Where(p => p.IsPostedAlive).ToArray();
            var addOnDmg = 0;
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                var result = OnPerformActivity(target, Activity.Intentions.Offensive, actId: i, skill: 1,
                    InstanceGenericDamage(addOnDmg)); //第1斩开始算技能连斩
                if (!IsAlive) break;
                if (result is { IsDeath: false }) break;
                addOnDmg += (int)(StateDamage() * DamageRate() * 0.01f);
            }
        }
    }

    /// <summary>
    /// 15  大戟 - 挥动大戟，攻击时，可横扫攻击目标周围全部单位。横扫伤害比攻击伤害略低。
    /// </summary>
    public class DaJiOperator : HeroOperator
    {
        private int DamageRate()
        {
            switch (Style.Military)
            {
                case 15:  return 50;
                case 102: return 70;
                case 103: return 90;
                default: throw MilitaryNotValidError(this);
            }
        }
        private IEnumerable<IChessPos> ExtendedTargets(IChessPos target) => Chessboard.GetNeighbors(target, false);
        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetLaneTarget(this);
            var splashTargets = ExtendedTargets(target);
            var damage = InstanceGenericDamage();
            var splash = damage.Clone(DamageRate() * 0.01f);
            OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 1, damage);
            foreach (var spTarget in splashTargets)
            {
                OnPerformActivity(spTarget, Activity.Intentions.Attach, actId: -1, skill: -1, splash);
            }
        }
    }

    /// <summary>
    /// 13  羽林 - 持剑而待，武将受到攻击时，即刻进行反击。
    /// </summary>
    /// 
    public class YuLinOperator : HeroOperator
    {
        private int CounterRate()
        {
            switch (Style.Military)
            {
                case 13: return 70;
                case 122: return 80;
                case 123: return 90;
                default: throw MilitaryNotValidError(this);
            }
        }
        protected override void OnCounter(Activity activity, IChessOperator offender)
        {
            if (Chessboard.IsRandomPass(CounterRate()))
                OnPerformActivity(Chessboard.GetChessPos(offender), Activity.Intentions.Counter,
                    actId: -1, skill: 2, InstanceGenericDamage());
        }
    }
    /// <summary>
    /// 12  神武 - 每次攻击时，获得1层【战意】，【战意】可提升伤害。
    /// </summary>
    public class ShenWuOperator : HeroOperator
    {
        private int DamageRate()
        {
            switch (Style.Military)
            {
                case 12: return 10;
                case 114: return 15;
                case 115: return 20;
                default: throw MilitaryNotValidError(this);
            }
        }

        private const int RecursiveLimit = 15;//武魂：上限
        private static int loopCount = 0;
        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetLaneTarget(this);
            var soul = Chessboard.GetCondition(this, CardState.Cons.BattleSoul);
            var addOn = DamageRate() * 0.01f * soul * StateDamage();
            var result = OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 1, InstanceGenericDamage(addOn));
            if (result == null) return;

            if (target.Operator.CardType == GameCardType.Hero)
                OnPerformActivity(Chessboard.GetChessPos(this), Activity.Intentions.Self,
                    -1, -1, BattleSoulConduct);

            var targetStat = Chessboard.GetStatus(target.Operator);
            var resultType = result.Type;
            while (target.IsAliveHero &&
                   (resultType == ActivityResult.Types.Shield ||
                    resultType == ActivityResult.Types.Dodge) &&
                   !targetStat.IsDeath &&
                   loopCount <= RecursiveLimit)
            {
                OnPerformActivity(Chessboard.GetChessPos(this), Activity.Intentions.Self,
                    loopCount + 1, -1, BattleSoulConduct);
                resultType = OnPerformActivity(target, Activity.Intentions.Offensive,
                    loopCount + 1, 1, InstanceGenericDamage(addOn)).Type;
                loopCount++;
            }

            loopCount = 0;
        }

        private CombatConduct BattleSoulConduct => CombatConduct.InstanceBuff(InstanceId, CardState.Cons.BattleSoul);
        protected override void OnSufferConduct(Activity activity, IChessOperator offender = null)
        {
            if (offender != null && offender.CardType == GameCardType.Hero)
                OnPerformActivity(Chessboard.GetChessPos(this), Activity.Intentions.Self, actId: 0, skill: 2,
                    BattleSoulConduct);
        }
    }

    /// <summary>
    /// 11  白马 - 攻防兼备。武将血量越少，攻击和防御越高。
    /// </summary>
    public class BaiMaOperator : HeroOperator
    {
        private int DamageGapRate()
        {
            switch (Style.Military)
            {
                case 137: return 10;
                case 138: return 15;
                case 139: return 20;
                default: throw MilitaryNotValidError(this);
            }
        }
        private int DodgeGapRate()
        {
            switch (Style.Military)
            {
                case 137: return 3;
                case 138: return 5;
                case 139: return 7;
                default: throw MilitaryNotValidError(this);
            }
        }

        public override int GetDodgeRate() =>
            HpDepletedRatioWithGap(Chessboard.GetStatus(this), Dodge, 10, DodgeGapRate());

        protected override int StateDamage() => HpDepletedRatioWithGap(Chessboard.GetStatus(this), base.StateDamage(), 10, DamageGapRate());
    }
    /// <summary>
    /// 虎豹骑
    /// </summary>
    public class HuBaoQiOperator : HeroOperator
    {
        private int DamageGapRate()
        {
            switch (Style.Military)
            {
                case 11: return 10;
                case 140: return 15;
                case 141: return 20;
                default: throw MilitaryNotValidError(this);
            }
        }
        private int DamageResistGapRate()
        {
            switch (Style.Military)
            {
                case 11: return 3;
                case 140: return 5;
                case 141: return 7;
                default: throw MilitaryNotValidError(this);
            }
        }

        public override int GetMagicArmor() =>
            HpDepletedRatioWithGap(Chessboard.GetStatus(this), MagicResist, 10, DamageResistGapRate());
        public override int GetPhysicArmor() =>
            HpDepletedRatioWithGap(Chessboard.GetStatus(this), Armor, 10, DamageResistGapRate());

        protected override int StateDamage() => HpDepletedRatioWithGap(Chessboard.GetStatus(this),base.StateDamage() , 10, DamageGapRate());
    }

    /// <summary>
    /// 10  先登 - 武将血量越少，闪避越高。即将覆灭之时，下次攻击对敌方全体造成伤害。
    /// </summary>
    public class XianDengOperator : HeroOperator
    {
        private int DodgeAddingRate => 5;
        private float DamageRate()
        {
            switch (Style.Military)
            {
                case 10: return 2.5f;
                case 85: return 3f;
                case 86: return 4f;
                default: throw MilitaryNotValidError(this);
            }
        }
        protected override void MilitaryPerforms(int skill = 1)
        {
            if (Chessboard.GetStatus(this).HpRate * 100 > 30)
            {
                base.MilitaryPerforms(skill);
                return;
            }
            //当自爆的时候
            var damage = StateDamage() * DamageRate();
            var target = Chessboard.GetLaneTarget(this);
            var array = Chessboard.GetRivals(this,
                    p => p != target &&
                         p.IsAliveHero)
                .ToArray();

            OnPerformActivity(target, Activity.Intentions.Inevitable, actId: 0, skill: 2,
                CombatConduct.InstanceDamage(InstanceId, damage));
            for (var i = 0; i < array.Length; i++)
            {
                var pos = array[i];
                if (pos.Pos == target.Pos) continue;
                OnPerformActivity(pos, Activity.Intentions.Inevitable, actId: 0, skill: 2, CombatConduct.InstanceDamage(InstanceId, damage));
            }

            OnPerformActivity(Chessboard.GetChessPos(this), Activity.Intentions.Self, actId: 0, skill: 2, CombatConduct.InstanceKilling(InstanceId));
        }

        public override int GetDodgeRate() =>
            HpDepletedRatioWithGap(Chessboard.GetStatus(this), Dodge, 10, DodgeAddingRate);
    }

    /// <summary>
    /// (8)战象 - 践踏战场。攻击时可让敌方武将【眩晕】。
    /// </summary>
    public class ZhanXiangOperator : HeroOperator
    {
        private int StunRate(Damage.Types dmgType, bool major)
        {
            var rate = 0;
            switch (Style.Military)
            {
                case 8:
                    if (major) return 30;
                    switch (dmgType)
                    {
                        case Damage.Types.Critical: rate = 50; break;
                        case Damage.Types.Rouse: rate = 60; break;
                    }
                    break;
                case 174:
                    if (major) return 50;
                    switch (dmgType)
                    {
                        case Damage.Types.Critical: rate = 70; break;
                        case Damage.Types.Rouse: rate = 80; break;
                    }
                    break;
                case 175:
                    if (major) return 70;
                    switch (dmgType)
                    {
                        case Damage.Types.Critical: rate = 90; break;
                        case Damage.Types.Rouse: rate = 100; break;
                    }
                    break;
                default: throw MilitaryNotValidError(this);
            }
            return rate;
        }
        private int SplashDamageRate(Damage.Types type)
        {
            switch (Style.Military)
            {
                case 8:
                    switch (type)
                    {
                        case Damage.Types.Critical: return 100;
                        case Damage.Types.Rouse: return 150;
                    }
                    break;
                case 174:
                    switch (type)
                    {
                        case Damage.Types.Critical: return 125;
                        case Damage.Types.Rouse: return 175;
                    }
                    break;
                case 175:
                    switch (type)
                    {
                        case Damage.Types.Critical: return 150;
                        case Damage.Types.Rouse: return 200;
                    }
                    break;
                default: throw MilitaryNotValidError(this);
            }

            return 0;
        }

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetLaneTarget(this);
            var majorCombat = InstanceGenericDamage();
            var combatState = Damage.GetType(majorCombat);
            OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0,
                skill: combatState == Damage.Types.General ? 1 : 2, majorCombat, CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Stunned,
                    rate: CountRate(majorCombat, StunRate(combatState, true))));

            if (combatState == Damage.Types.General) return;
            //会心与暴击才触发周围伤害
            var splashTargets = Chessboard.GetNeighbors(target, false).ToArray();
            var splashDamage = majorCombat.Clone(SplashDamageRate(combatState) * 0.01f);
            foreach (var splashTarget in splashTargets)
            {
                OnPerformActivity(splashTarget, Activity.Intentions.Offensive, actId: -1, skill: -1, splashDamage, CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Stunned,
                        rate: CountRate(splashDamage, StunRate(Damage.GetType(splashDamage), false))));
            }
        }
    }

    /// <summary>
    /// (7)刺盾 - 装备刺甲，武将受到近战伤害时，可将伤害反弹。
    /// </summary>
    public class CiDunOperator : HeroOperator
    {
        private float ReflectRate()
        {
            switch (Style.Military)
            {
                case 7: return 1f;
                case 76: return 1.25f;
                case 77: return 1.5f;
                default: throw MilitaryNotValidError(this);
            }
        }
        protected override void MilitaryPerforms(int skill = 1)
        {
            base.MilitaryPerforms(0);//刺甲攻击是普通攻击
        }

        protected override void OnReflectingConduct(Activity activity, ChessOperator offender)
        {
            var damage = activity.Conducts.Where(c => c.Kind == CombatConduct.DamageKind)
                             .Sum(c => c.Total) * ReflectRate();
            OnPerformActivity(Chessboard.GetChessPos(offender), Activity.Intentions.Reflect, actId: -1, skill: 1,
                CombatConduct.InstanceDamage(InstanceId, damage));
        }
    }

    /// <summary>
    /// (6)虎卫 - 横行霸道，武将进攻时，可吸收血量。
    /// </summary>
    public class HuWeiOperator : HeroOperator
    {
        private int ComboRatio()
        {
            switch (Style.Military)
            {
                case 6: return 3;
                case 74: return 5;
                case 75: return 7;
                default: throw MilitaryNotValidError(this);
            }
        }
        protected override void MilitaryPerforms(int skill = 1)
        {
            var stat = Chessboard.GetStatus(this);
            var target = Chessboard.GetLaneTarget(this);
            if (!target.IsAliveHero)
            {
                base.MilitaryPerforms(1);
                return;
            }
            for (int i = 0; i < ComboRatio(); i++)
            {
                //不攻击的判断
                if ((i > 0 && stat.HpRate >= 1) || //第二击开始，血量满了
                    stat.UnAvailable) break;

                var result = OnPerformActivity(target, Activity.Intentions.Offensive, actId: i, skill: 1,
                    InstanceGenericDamage());//伤害活动

                var lastDmg = result?.Status?.LastSuffers?.LastOrDefault();
                if (result == null) return;
                if (!target.IsAliveHero ||
                    result.Status == null ||
                    result.Type != ActivityResult.Types.Suffer ||
                    !lastDmg.HasValue ||
                    lastDmg.Value <= 0 || //对手伤害=0
                    stat.Hp >= stat.MaxHp) //自身满血
                    break;

                OnPerformActivity(Chessboard.GetChessPos(this), Activity.Intentions.Self, actId: i, skill: 2,
                    CombatConduct.InstanceHeal(lastDmg.Value, InstanceId)); //吸血活动

                if (result.IsDeath)
                    break;
            }
        }
    }

    /// <summary>
    /// (5)陷阵 - 武将陷入危急之时，进入【无敌】状态。
    /// </summary>
    public class XianZhenOperator : HeroOperator
    {
        protected int GetInvincibleRate()//根据兵种id获取概率
        {
            switch (Style.Military)
            {
                case 5: return 5;
                case 81: return 10;
                case 82: return 15;
            }
            throw MilitaryNotValidError(this);
        }

        private int CriticalAddRate => 10;
        private int RouseAddRate => 20;
        protected override void OnSufferConduct(Activity activity, IChessOperator offender = null)
        {
            OnPerformActivity(Chessboard.GetChessPos(this), Activity.Intentions.Self, actId: 0, skill: 1,
                Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Invincible,
                    rate: CountRate(Damage.GetType(activity), GetInvincibleRate(), CriticalAddRate, RouseAddRate))));
        }
        protected override void MilitaryPerforms(int skill = 1) => base.MilitaryPerforms(0);
    }

    /// <summary>
    /// (4)大盾 - 战斗前装备1层【护盾】。每次进攻后装备1层【护盾】。
    /// </summary>
    public class DaDunOperator : HeroOperator
    {
        private int Shield()
        {
            switch (Style.Military)
            {
                case 4: return 1;
                case 68: return 2;
                case 69: return 2;
                default: throw MilitaryNotValidError(this);
            }
        }
        public override void OnRoundStart()
        {
            OnPerformActivity(Chessboard.GetChessPos(this), Activity.Intentions.Self, actId: -1, skill: 1,
                CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Shield, value: Shield()));
        }

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetLaneTarget(this);
            if (target == null) return;
            var damage = InstanceGenericDamage();
            var shield = 1;
            if (damage.IsRouseDamage())
                shield++;
            OnPerformActivity(Chessboard.GetChessPos(this), Activity.Intentions.Self,
                actId: -1, skill: damage.IsRouseDamage() ? 1 : 2,
                CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Shield, value: shield));
            OnPerformActivity(target, Activity.Intentions.Offensive, actId: 0, skill: 0, Helper.Singular(damage));
        }
    }

    /// <summary>
    /// (3)飞甲 - 有几率闪避攻击。武将剩余血量越少，闪避率越高。
    /// </summary>
    public class FeiJiaOperator : HeroOperator
    {
        private int DodgeRate()
        {
            switch (Style.Military)
            {
                case 3: return 3;
                case 72: return 5;
                case 73: return 7;
                default: throw MilitaryNotValidError(this);
            }
        }

        public override int GetDodgeRate() => HpDepletedRatioWithGap(Chessboard.GetStatus(this), Dodge,
            10, DodgeRate());
    }

    /// <summary>
    /// (2)铁卫 - 武将剩余血量越少，防御越高。
    /// </summary>
    public class TieWeiOperator : HeroOperator
    {
        private int ArmorRate()
        {
            switch (Style.Military)
            {
                case 2: return 3;
                case 70: return 5;
                case 71: return 7;
                default: throw MilitaryNotValidError(this);
            }
        }
        public override int GetPhysicArmor()
        {
            var armor = Armor;
            var status = Chessboard.GetStatus(this);
            return HpDepletedRatioWithGap(status, armor, 10, ArmorRate());
        }
    }

    public class WeightElement<T> : IWeightElement
    {
        public int Weight { get; set; }
        public T Obj { get; set; }
    }
}