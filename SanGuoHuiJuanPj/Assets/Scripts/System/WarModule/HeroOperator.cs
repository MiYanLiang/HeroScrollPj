using CorrelateLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.System.WarModule
{
    public class HeroOperator : CardOperator
    {
        protected HeroCombatInfo CombatInfo { get; private set; }

        /// <summary>
        /// 根据当前血量损失的<see cref="gap"/>%，根据<see cref="gapValue"/>的值倍增
        /// </summary>
        /// <param name="maxHp"></param>
        /// <param name="basicValue">起始值</param>
        /// <param name="gap"></param>
        /// <param name="gapValue"></param>
        /// <param name="hp"></param>
        /// <returns></returns>
        public static int HpDepletedRatioWithGap(int hp, int maxHp, int basicValue, int gap, int gapValue)
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

        public override void Init(IChessman card, ChessboardOperator chessboardOp)
        {
            CombatInfo = HeroCombatInfo.GetInfo(card.CardId);
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

        private IChessPos GetTarget() => Chessboard.GetTargetByMode(this, TargetingMode);
        protected virtual ChessboardOperator.Targeting TargetingMode => ChessboardOperator.Targeting.Contra;
        protected void NoSkillAction()
        {
            var target = GetTarget();
            if (target == null) return;
            Chessboard.AppendOpActivity(this, target, Activity.Offensive, BasicDamage(), 0, 0);
        }

        private CombatConduct[] BasicDamage() =>
            Helper.Singular(CombatConduct.InstanceDamage(InstanceId, Strength, Style.Element));

        /// <summary>
        /// 兵种攻击，base攻击是基础英雄属性攻击
        /// </summary>
        /// <param name="skill">标记1以上为技能，如果超过1个技能才继续往上加</param>
        /// <returns></returns>
        protected virtual void MilitaryPerforms(int skill = 1)
        {
            var target = GetTarget();
            if (target == null) return;
            Chessboard.AppendOpActivity(this, target, Activity.Offensive, Helper.Singular(InstanceHeroGenericDamage()), 0, skill);
        }

        protected int MagicResist => CombatInfo.MagicResist;
        /// <summary>
        /// 法术免伤
        /// </summary>
        /// <returns></returns>
        public override int GetMagicArmor() => MagicResist;

        protected int Armor => CombatInfo.PhysicalResist;
        /// <summary>
        /// 物理免伤
        /// </summary>
        /// <returns></returns>
        public override int GetPhysicArmor() => Armor;

        /// <summary>
        /// 根据武将属性生成伤害
        /// </summary>
        /// <returns></returns>
        protected CombatConduct InstanceHeroGenericDamage(float additionDamage = 0)
        {
            var damage = GeneralDamage() + additionDamage;
            if (Chessboard.IsRouseDamagePass(this))
            {
                var rouse = RouseDamage();
                if (rouse > 0)
                    return CombatConduct.Instance(damage, 0, rouse, Style.Element,
                        CombatConduct.DamageKind, 0, InstanceId);
            }

            if (Chessboard.IsCriticalDamagePass(this))
            {
                var critical = CriticalDamage();
                if (critical > 0) return CombatConduct.InstanceDamage(InstanceId, damage, critical, 0, Style.Element);
            }

            return CombatConduct.InstanceDamage(InstanceId, damage, Style.Element);
        }
        /// <summary>
        /// 武力值
        /// </summary>
        public override int Strength => Style.Strength;

        /// <summary>
        /// 根据状态算出基础伤害
        /// </summary>
        /// <returns></returns>
        protected override int GeneralDamage() => Chessboard.GetCompleteDamageWithBond(this);

        /// <summary>
        /// 跟据会心率获取会心伤害
        /// </summary>
        /// <returns></returns>
        private float RouseDamage() => CombatInfo.GetRouseDamage(Strength) - Strength;

        /// <summary>
        /// 根据暴击率获取暴击伤害
        /// </summary>
        /// <returns></returns>
        private float CriticalDamage() => CombatInfo.GetCriticalDamage(Strength) - Strength;

        protected int Dodge => CombatInfo.DodgeRatio;
        public override int GetDodgeRate() => Dodge;

        protected static MilitaryNotValid MilitaryNotValidError(HeroOperator op) =>
            new MilitaryNotValid(nameof(op.Style.Military), op.Style.Military.ToString());
        protected class MilitaryNotValid : ArgumentOutOfRangeException
        {
            public MilitaryNotValid(string paramName, string message) : base(paramName, message)
            {

            }
        }
    }

    public class DuanGongOperator : HeroOperator
    {
        protected override ChessboardOperator.Targeting TargetingMode => ChessboardOperator.Targeting.Any;

    }
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
                case 66: return 25;
                case 67: return 30;
            }
            throw MilitaryNotValidError(this);
        }

        protected int RouseShieldRate = 60;
        protected int CriticalShieldRate = 30;
        protected override void OnSufferConduct(IChessOperator offender, Activity activity)
        {
            var shieldRate = GetShieldRate();
            var isRouse = activity.Conducts.Any(c => c.Rouse > 0);//是否会心一击
            var isCritical = activity.Conducts.Any(c => c.Critical > 0);//是否暴击
            shieldRate += isRouse ? RouseShieldRate : isCritical ? CriticalShieldRate : 0;
            if (Chessboard.IsRandomPass(shieldRate))
                Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(this), Activity.Self,
                    Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Shield)), 0, 1);
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
        private int ComboRate => 50;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetContraTarget(this);
            if (target == null) return;
            var combos = Chessboard.IsRandomPass(ComboRate) ? ComboTimes() : 1;
            for (int i = 0; i < combos; i++)
            {
                Chessboard.AppendOpActivity(this, target, Activity.Offensive,
                    Helper.Singular(InstanceHeroGenericDamage()), i, 1);
                if (Chessboard.GetStatus(this).IsDeath) break;
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
                case 16: return 10;
                case 142: return 15;
                case 143: return 20;
                default: throw MilitaryNotValidError(this);
            }
        }

        private int RouseAddOn => 80;
        private int CriticalAddOn => 50;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetContraTarget(this);
            if (target == null) return;
            //var tOp = Chessboard.GetOperator(target);
            int comboRate = ComboRatio();
            bool combo = true;
            var actId = 0;
            do
            {
                var hit = InstanceHeroGenericDamage();
                var result = Chessboard.AppendOpActivity(this, target, Activity.Offensive, Helper.Singular(hit), actId,
                    actId == 0 ? 0 : 1);
                if (result == null || result.IsDeath) break;
                if (Chessboard.GetStatus(this).IsDeath) break;
                if (target.Operator == null || Chessboard.GetStatus(target.Operator).IsDeath) break;
                if (target.IsAliveHero) break;
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
    /// 58  铁骑 - 多铁骑上阵发动【连环战马】。所有铁骑分担伤害，并按铁骑数量提升伤害。
    /// </summary>
    public class TieJiOperator : HeroOperator
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
        private const int Chained = (int)CardState.Cons.Chained;
        private const int ChainMax = 10;
        private int ArmorRate => 5;
        private int StrengthRate => 5;

        private int DamageRate()
        {
            switch (Style.Military)
            {
                case 58: return 5;
                case 152: return 10;
                case 153: return 15;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetContraTarget(this);
            if (Chessboard.GetCondition(this, CardState.Cons.Chained) <= 0)
            {
                base.MilitaryPerforms(0);
                return;
            }
            var chainedList = GetChained();
            if (!chainedList.Any())
            {
                base.MilitaryPerforms(0);
                return;
            }

            var conduct = InstanceHeroGenericDamage(Strength * chainedList.Length * DamageRate() * 0.01f);
            Chessboard.AppendOpActivity(this, target, Activity.Offensive, Helper.Singular(conduct), 0, 1);
        }

        public override int OnSpritesValueConvert(PosSprite[] sprites, CardState.Cons con)
        {
            var rate = con == CardState.Cons.ArmorUp ?
                ArmorRate : con == CardState.Cons.StrengthUp ?
                    StrengthRate : 0;
            return rate * Math.Min(sprites.Length, ChainMax);
        }

        private IChessPos[] GetChained() => Chessboard.GetChainedPos(this, p => p.IsAliveHero && IsChainable(p.Operator)).ToArray();

        public override void OnPlaceInvocation()
        {
            if (Chessboard.GetSpriteInChessPos(this).Any(s => s.TypeId == Chained)) return;
            var chainedList = GetChained();
            if (chainedList.Length > 1)
                Chessboard.InstanceSprite<ChainSprite>(Chessboard.GetChessPos(this), Chained,
                    InstanceId, 1, -1);
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
            sprites = sprites.Where(s => s.TypeId == YellowBand).Take(Max).ToArray();
            return !sprites.Any() ? 0 : sprites.Sum(s => s.Value);
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
        protected virtual int DamageRate => 20;
        public override void OnPlaceInvocation()
        {
            if (Chessboard.GetSpriteInChessPos(this).Any(s => s.TypeId == YellowBand)) return;
            var cluster = Chessboard.GetFriendly(this,
                p => p.IsAliveHero &&
                     p.Operator != this &&
                     IsYellowBand(p.Operator)).ToArray();
            if (cluster.Length == 0) return;
            Chessboard.InstanceSprite<YellowBandSprite>(Chessboard.GetChessPos(this), YellowBand,
                InstanceId, DamageRate, -1);
        }

    }

    /// <summary>
    /// 57  藤甲 - 藤甲护体，刀枪不入。高度免疫物理伤害。
    /// </summary>
    public class TengJiaOperator : HeroOperator
    {
        protected override int OnMilitaryDamageConvert(CombatConduct conduct)
        {
            if (conduct.Element == CombatConduct.FireDmg) return (int)(conduct.Total * 3);
            return base.OnMilitaryDamageConvert(conduct);
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

        private float[] PenetrateDecreases = new[] { 0.75f, 0.5f };

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetContraTarget(this);
            var damage = InstanceHeroGenericDamage();

            Chessboard.AppendOpActivity(this, target, Activity.Offensive, Helper.Singular(damage), actId: 0, skill: 1);
            for (var i = 1; i < PenetrateUnits() + 1; i++)
            {
                var backPos = Chessboard.BackPos(target);
                target = backPos;
                if (target == null) break;
                if (target.Operator == null) continue;
                Chessboard.AppendOpActivity(this, target, Activity.Offensive,
                    Helper.Singular(damage.Clone(PenetrateDecreases[i - 1])), actId: 0, skill: -1);
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

        private int RouseAddOn = 30;
        private int CriticalAddOn = 15;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetContraTarget(this);
            if (!target.IsAliveHero)
            {
                base.MilitaryPerforms(0);
                return;
            }
            var combat = InstanceHeroGenericDamage();
            var dmg = new List<CombatConduct> { combat };
            var poisonRate = PoisonRate();
            if (combat.IsRouseDamage())
                poisonRate += RouseAddOn;
            if (combat.IsCriticalDamage())
                poisonRate += CriticalAddOn;
            if (Chessboard.IsRandomPass(poisonRate))
                dmg.Add(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Poison));
            Chessboard.AppendOpActivity(this, target, Activity.Offensive, dmg.ToArray(), 0, 1);
        }
    }

    /// <summary>
    /// 55  火船 - 驱动火船，可引燃敌方武将，或自爆对敌方造成大范围伤害及【灼烧】。
    /// </summary>
    public class HuoChuanOperator : HeroOperator
    {
        private float ExplodeRate()
        {
            switch (Style.Military)
            {
                case 55: return 1.5f;
                case 196: return 2f;
                case 197: return 2.5f;
                default: throw MilitaryNotValidError(this);
            }
        }

        private int ExplodeBurningRate => 50;
        private int BurningRate()
        {
            switch (Style.Military)
            {
                case 55: return 30;
                case 196: return 50;
                case 197: return 70;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetContraTarget(this);
            if (Chessboard.GetStatus(this).HpRate < 0.5)
            {
                var explode = new List<CombatConduct>
                    { CombatConduct.InstanceDamage(InstanceId,(int)(GeneralDamage() * ExplodeRate()), Style.Element) };
                var surrounded = Chessboard.GetNeighbors(target, false).ToList();
                surrounded.Insert(0, target);
                for (var i = 0; i < surrounded.Count; i++)
                {
                    var chessPos = surrounded[i];
                    if (Chessboard.IsRandomPass(ExplodeBurningRate))
                        explode.Add(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Burn));
                    Chessboard.AppendOpActivity(this, chessPos, Activity.Offensive, explode.ToArray(), 0,
                        i == 0 ? 2 : -1);
                }

                Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(this), Activity.Self,
                    Helper.Singular(CombatConduct.InstanceKilling(InstanceId)), -1, -1);
                return;
            }

            var combat = new List<CombatConduct> { InstanceHeroGenericDamage() };
            if (Chessboard.IsRandomPass(BurningRate()))
                combat.Add(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Burn));
            Chessboard.AppendOpActivity(this, target, Activity.Offensive, combat.ToArray(), 0, 1);
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
        private int PushBackRate => 10;
        private int CriticalRate => 5;
        private int RouseRate => 10;

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
                var pushBackRate = PushBackRate;
                var target = targets[i];
                var damage = InstanceHeroGenericDamage();
                damage.Element = CombatConduct.WaterDmg;
                if (damage.IsCriticalDamage())
                    pushBackRate += CriticalRate;
                if (damage.IsRouseDamage())
                    pushBackRate += RouseRate;
                var backPos = Chessboard.BackPos(target);
                var rePos = -1;
                if (backPos != null &&
                    backPos.Operator == null &&
                    Chessboard.IsRandomPass(pushBackRate))
                {
                    rePos = backPos.Pos;
                    //Chessboard.AppendOpActivity(this, target, Activity.Offensive, Helper.Singular(damage), 0, 1,backPos.Pos);
                    //continue;
                }

                Chessboard.AppendOpActivity(this, target, Activity.Offensive, Helper.Singular(damage), 0, 1, rePos);
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
                case 47: return 1;
                case 48: return 3;
                case 222: return 5;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override CombatConduct[] Skills(int buffValue) =>
            Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Confuse, buffValue));
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

        protected virtual int BuffRate => 50;
        protected virtual int CriticalAddOn => 15;
        protected virtual int RouseAddOn => 30;
        protected virtual CardState.Cons Buff => CardState.Cons.Neizhu;
        private CombatConduct BuffToFriendly => CombatConduct.InstanceBuff(InstanceId, Buff);
        protected override void MilitaryPerforms(int skill = 1)
        {
            var combat = InstanceHeroGenericDamage();
            var rate = BuffRate;
            if (combat.IsRouseDamage()) rate += RouseAddOn;
            if (combat.IsCriticalDamage()) rate += CriticalAddOn;
            var targets = Chessboard.GetFriendly(this,
                    p => p.IsAliveHero)
                .OrderByDescending(p => p.Operator.Style.Strength)
                .Take(Targets())
                .ToArray();
            if (!targets.Any() || !Chessboard.IsRandomPass(rate))
            {
                base.MilitaryPerforms(0);
                return;
            }
            foreach (var target in targets)
                Chessboard.AppendOpActivity(this, target, Activity.Friendly, Helper.Singular(BuffToFriendly), 0, 1);
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
            var target = Chessboard.GetContraTarget(this);
            if (!target.IsAliveHero)
            {
                base.MilitaryPerforms(0);
                return;
            }

            var conduct = InstanceHeroGenericDamage();
            var list = new List<CombatConduct> { conduct };
            var disarmedRate = DisarmedRate();
            if (conduct.IsRouseDamage())
                disarmedRate += RouseAddOn;
            if (conduct.IsCriticalDamage())
                disarmedRate += CriticalAddOn;
            if (Chessboard.IsRandomPass(disarmedRate))
                list.Add(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Disarmed));
            Chessboard.AppendOpActivity(this, target, Activity.Offensive, list.ToArray(), 0, 1);
        }
    }

    /// <summary>
    /// 42  医师 - 分发草药，治疗1个友方武将。
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

        protected virtual Func<IChessPos, bool> TargetFilter() =>
            p => p.IsAliveHero && Chessboard.GetStatus(p.Operator).HpRate < 1;

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

            var basicDamage = InstanceHeroGenericDamage();
            float rate = 1;//Chessboard.Randomize(1, 5);
            if (basicDamage.IsCriticalDamage()) rate += 1.5f;
            if (basicDamage.IsRouseDamage()) rate += 2f;
            var heal = (int)(rate * Strength);
            foreach (var target in targets)
            {
                if (heal <= 0) break;
                var stat = Chessboard.GetStatus(target.Operator);
                var healPoint = stat.MaxHp - stat.Hp;
                if (heal > healPoint)
                    heal -= healPoint;
                else healPoint = heal;
                Chessboard.AppendOpActivity(this, target, Activity.Friendly,
                    Helper.Singular(CombatConduct.InstanceHeal(healPoint, InstanceId)), 0, 1);
            }
        }

    }

    /// <summary>
    /// 41  敢死 - 武将陷入危急之时进入【死战】状态，将受到的伤害转化为自身血量数。
    /// </summary>
    public class GanSiOperator : HeroOperator
    {
        protected virtual int TriggerRate => 30;
        private int rest;
        private int RestingRate()
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
                Chessboard.InstanceChessboardActivity(IsChallenger, this, Activity.Self,
                    Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.DeathFight, -1)));
                rest = RestingRate();
                return;
            }

            rest--;
        }

        protected override void OnAfterSubtractHp(int damage, CombatConduct conduct)
        {
            if (Chessboard.GetStatus(this).IsDeath ||
                Chessboard.GetStatus(this).HpRate > TriggerRate * 0.01f ||
                Chessboard.GetCondition(this, CardState.Cons.DeathFight) > 0 ||
                rest > 0) return;
            Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(this), Activity.Self,
                Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.DeathFight)), -1, 1);
        }

        protected override void MilitaryPerforms(int skill = 1) => base.MilitaryPerforms(0);

        protected override int OnMilitaryDamageConvert(CombatConduct conduct)
        {
            if (Chessboard.GetCondition(this, CardState.Cons.DeathFight) > 0)
            {
                Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(this), Activity.Self,
                    Helper.Singular(CombatConduct.InstanceHeal(conduct.Total, InstanceId)), -1, skill: -1);
                conduct.SetZero();
            }

            return (int)conduct.Total;
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
        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Chessboard.GetFriendly(this,
                    p => p.IsAliveHero)
                .OrderBy(p =>
                {
                    var status = Chessboard.GetStatus(p.Operator);
                    return (status.Hp + status.GetBuff(CardState.Cons.EaseShield)) /
                           status.MaxHp;
                })
                .Take(Targets()).ToArray();

            if (!targets.Any())
            {
                base.MilitaryPerforms(0);
                return;
            }

            var basicDamage = InstanceHeroGenericDamage();
            float rate = 1;// Chessboard.Randomize(1, 5);
            if (basicDamage.IsCriticalDamage()) rate += 1.5f;
            if (basicDamage.IsRouseDamage()) rate += 2f;
            var shield = rate * Strength;
            foreach (var target in targets)
            {
                Chessboard.AppendOpActivity(this, target, Activity.Friendly,
                    Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.EaseShield, shield)), 0, 1);
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
            var rate = Info.Rare * 20;//暂时随机值是根据稀有度
            var combat = InstanceHeroGenericDamage();
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
                        if (value <= con.Value)
                        {
                            combats.Add(CombatConduct.InstanceBuff(InstanceId, con.Buff, -value));
                            break;
                        }
                        combats.Add(CombatConduct.InstanceBuff(InstanceId, con.Buff, -con.Value));
                        value -= con.Value;
                        if (value <= 0) break;
                    }

                    Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(target.Operator), Activity.Friendly,
                        combats.ToArray(), 0, 1);
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

        protected override CombatConduct[] Skills(int buffValue) =>
            Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Stunned, buffValue));
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

        protected override CombatConduct[] Skills(int buffValue) =>
            Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Imprisoned, buffValue));
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
        protected virtual int SkillRate => 10;

        /// <summary>
        /// 会心增率
        /// </summary>
        protected virtual int RouseRate => 10;
        protected abstract CombatConduct[] Skills(int buffValue);

        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Chessboard.GetRivals(this,
                    p => p.IsPostedAlive && p.Operator.CardType == GameCardType.Hero)
                .Select(c => new WeightElement<IChessPos> { Obj = c, Weight = Chessboard.Randomize(3) + 1 })
                .Pick(Targets()).Select(c => c.Obj).ToArray();

            if (targets.Length == 0) base.MilitaryPerforms(0);

            var damage = InstanceHeroGenericDamage();
            var rate = 10 + Chessboard.Randomize(SkillRate);
            if (damage.IsRouseDamage())
                rate += RouseRate;
            if (damage.IsCriticalDamage())
                rate += CriticalRate;
            for (var i = 0; i < targets.Length; i++)
            {
                var buff = 0;
                var target = targets[i];
                if (Chessboard.IsRandomPass(rate))
                    buff = 1;
                Chessboard.AppendOpActivity(this, target, Activity.Offensive, Skills(buff), 0, 1);
            }
        }
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

        private int BurnRatio => 10;

        /// <summary>
        /// 统帅武力倍数
        /// </summary>
        private float DamageRate()
        {
            switch (Style.Military)
            {
                case 32: return 0.3f;
                case 33: return 0.5f;
                case 209: return 1.0f;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override void MilitaryPerforms(int skill = 1)
        {
            var scope = Chessboard.GetRivals(this, _ => true).ToArray();
            var ringIndex = -1;

            for (int i = FireRings.Length - 1; i >= 0; i--)
            {
                var sprite = Chessboard.GetSpriteInChessPos(FireRings[i][0], !IsChallenger)
                    .FirstOrDefault(s => s.TypeId == PosSprite.YeHuo);
                if (sprite == null) continue;
                ringIndex = i;
                break;
            }
            ringIndex++;
            if (ringIndex >= FireRings.Length)
                ringIndex = 0;
            //var outRingDamageDecrease = GeneralDamage() * DamageRatio * ringIndex * 0.01f;
            var damage = InstanceHeroGenericDamage();
            damage.Multiply(DamageRate());
            damage.Element = CombatConduct.FireDmg;
            damage.Rate = BurnRatio;
            //var burnBuff = CombatConduct.InstanceBuff(CardState.Cons.Burn, 3);
            var burnPoses = scope
                .Join(FireRings.SelectMany(i => i), p => p.Pos, i => i, (p, _) => p)
                .All(p => p.Terrain.Sprites.Any(s => s.TypeId == PosSprite.YeHuo))
                ? //是否满足满圈条件
                scope
                : scope.Join(FireRings[ringIndex], p => p.Pos, i => i, (p, _) => p).ToArray();
            var combat = Helper.Singular(damage);
            for (var index = 0; index < burnPoses.Length; index++)
            {
                var chessPos = burnPoses[index];
                //if (Chessboard.IsRandomPass(BurnRatio + Chessboard.Randomize(10))) combat.Add(burnBuff);
                Chessboard.InstanceSprite<FireSprite>(chessPos, typeId: PosSprite.YeHuo, lasting: 2, value: 5, actId: -1);
                if (chessPos.Operator == null || Chessboard.GetStatus(chessPos.Operator).IsDeath) continue;
                Chessboard.AppendOpActivity(this, chessPos, Activity.Offensive, combat, 0, 1);
            }
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
        private float DamageRate => 0.3f;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Chessboard.GetRivals(this,
                    p => p.IsAliveHero)
                .Select(p => new WeightElement<IChessPos>
                {
                    Obj = p,
                    Weight = WeightElement<IChessPos>.Random.Next(1, 4)
                }).Pick(Targets()).ToArray();
            if (targets.Length == 0) base.MilitaryPerforms(0);
            var damage = InstanceHeroGenericDamage();
            damage.Element = CombatConduct.PoisonDmg;
            damage.Multiply(DamageRate);
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                Chessboard.AppendOpActivity(this, target.Obj, Activity.Offensive, Helper.Singular(damage), 0, 1);
            }
        }
    }

    /// <summary>
    /// 28  术士 - 洞察天机，召唤3次天雷，随机落在敌阵中，并有几率造成【眩晕】。
    /// </summary>
    public class ShuShiOperator : HeroOperator
    {
        private bool isInit = false;
        private static int PresetBuff = 2;
        private int[] TargetRange()
        {
            switch (Style.Military)
            {
                case 28: return new[] { 1, 3 };
                case 29: return new[] { 2, 5 };
                case 204: return new[] { 3, 5 };
                default: throw MilitaryNotValidError(this);
            }
        }
        private int UltiAttackTimes()
        {
            switch (Style.Military)
            {
                case 28: return 3;
                case 29: return 5;
                case 204: return 5;
                default: throw MilitaryNotValidError(this);
            }
        }
        private int Ultimate => 3;
        private int CriticalAddOn => 1;
        private int RouseAddOn => 2;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var murderous = Chessboard.GetCondition(this, CardState.Cons.Murderous);
            var conduct = InstanceHeroGenericDamage();
            conduct.Element = CombatConduct.ThunderDmg;
            conduct.Rate = Chessboard.Randomize(10);
            var addOn = conduct.IsRouseDamage() ? RouseAddOn : conduct.IsCriticalDamage() ? CriticalAddOn : 0;
            var range = TargetRange();
            var isUlti = murderous >= Ultimate;//是不是大招
            var attackTimes = isUlti ? UltiAttackTimes() : 1;
            Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(this), Activity.Self,
                Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Murderous, -1)), -1, -1);
            for (var j = 0; j < attackTimes; j++)
            {
                var thunders = Chessboard.Randomize(range[0], range[1]) + addOn;
                var targetPoses = Chessboard.GetRivals(this, _ => true).Select(pos => new WeightElement<IChessPos>
                { Obj = pos, Weight = WeightElement<IChessPos>.Random.Next(1, 4) }).Pick(thunders).ToArray();
                for (int index = 0; index < targetPoses.Length; index++)
                {
                    var pos = targetPoses[index];
                    Chessboard.DelegateSpriteActivity<ThunderSprite>(this, pos.Obj, PosSprite.Thunder,
                        Helper.Singular(conduct), j, isUlti ? 2 : 1, 1);
                }
            }
        }

        public override void OnSomebodyDie(ChessOperator death)
        {
            Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(this), Activity.Self,
                Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Murderous)), -1, 3);
        }

        public override void OnRoundStart()
        {
            if (isInit) return;
            isInit = true;
            Chessboard.InstanceChessboardActivity(IsChallenger, this, Activity.Self,
                Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Murderous, PresetBuff)), 3);
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

        private int CriticalRate() => 5;
        private int RouseRate() => 10;
        private int KillingRate => 10;
        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Chessboard.GetRivals(this,
                    c => c.IsAliveHero)
                .OrderBy(p => Chessboard.GetStatus(p.Operator).HpRate)
                .Take(Targets()).ToArray();
            var baseDamage = InstanceHeroGenericDamage();
            baseDamage.Element = CombatConduct.WindDmg;
            var killingRate = KillingRate;
            if (baseDamage.IsCriticalDamage())
                killingRate += CriticalRate();
            if (baseDamage.Rouse > 0)
                killingRate += RouseRate();
            if (targets.Length == 0)
            {
                base.MilitaryPerforms(0);
                return;
            }

            var kill = CombatConduct.InstanceKilling(InstanceId);
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                if (target.IsAliveHero &&
                    Chessboard.IsRandomPass(killingRate))
                {
                    Chessboard.AppendOpActivity(this, target, Activity.Offensive, Helper.Singular(kill), 0, 1);
                    continue;
                }
                Chessboard.AppendOpActivity(this, target, Activity.Offensive, Helper.Singular(baseDamage), 0, 1);
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
                case 25: return 50;
                case 129: return 75;
                case 130: return 100;
                default: throw MilitaryNotValidError(this);
            }
        }
        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetRivals(this,
                    p => p.IsAliveHero)
                .OrderByDescending(p => p.Operator.Style.Type)
                .ThenByDescending(p => p.Operator.Style.Strength)
                .ThenByDescending(p => p.Pos).FirstOrDefault();
            if (target == null) base.MilitaryPerforms(0);
            var combats = new List<CombatConduct> { InstanceHeroGenericDamage() };
            if (Chessboard.IsRandomPass(BleedRate()))
                combats.Add(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Bleed));
            Chessboard.AppendOpActivity(this, target, Activity.Offensive, combats.ToArray(), 0, 1);
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
                case 179: return 3;
                default: throw MilitaryNotValidError(this);
            }
        }
        private float DamageRate => 1f;
        protected override void MilitaryPerforms(int skill = 1)
        {
            var targets = Chessboard.GetRivals(this,
                    p => p.IsPostedAlive)
                .Join(TargetPoses, t => t.Pos, p => p, (t, _) => t).ToArray();
            var damage = InstanceHeroGenericDamage();
            damage.Element = CombatConduct.MechanicalDmg;
            for (int i = 0; i < ComboRate(); i++)
            {
                var target = targets.RandomPick();
                damage.Multiply(DamageRate);
                Chessboard.AppendOpActivity(this, target, Activity.Offensive,
                    Helper.Singular(damage), 0, 1);
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
            var target = Chessboard.GetContraTarget(this);
            if (target.IsAliveHero)
            {
                base.MilitaryPerforms(0);
                return;
            }

            var damage = InstanceHeroGenericDamage();
            damage.Element = CombatConduct.MechanicalDmg;
            for (int i = 0; i < CombatRate(); i++)
                Chessboard.AppendOpActivity(this, target, Activity.Offensive, Helper.Singular(damage), i, 1);
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
                case 22: return 50;
                case 172: return 70;
                case 173: return 90;
                default: throw MilitaryNotValidError(this);
            }
        }
        private int CriticalRate => 15;
        private int RouseRate => 30;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetContraTarget(this);

            var basicDamage = GeneralDamage();
            if (Chessboard.GetCondition(target.Operator, CardState.Cons.Stunned) > 0)
            {
                basicDamage *= 2;
                Chessboard.AppendOpActivity(this, target, Activity.Offensive,
                    Helper.Singular(CombatConduct.InstanceDamage(InstanceId, basicDamage)), 0, 1);
                return;
            }

            var damage = CombatConduct.InstanceDamage(InstanceId, basicDamage);
            var combats = new List<CombatConduct> { damage };
            var stunningRate = StunningRate();
            if (damage.IsRouseDamage())
                stunningRate += RouseRate;
            if (damage.IsCriticalDamage())
                stunningRate += CriticalRate;
            if (Chessboard.IsRandomPass(stunningRate))
                combats.Add(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Stunned));
            Chessboard.AppendOpActivity(this, target, Activity.Offensive, combats.ToArray(), 0, 1);
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
                case 21: return 1.5f;
                case 194: return 2f;
                case 195: return 2.5f;
                default: throw MilitaryNotValidError(this);
            }
        }
        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetContraTarget(this);
            var combatConducts = new List<CombatConduct>();
            var backPos = Chessboard.BackPos(target);
            var rePos = backPos != null &&
                        backPos.Operator == null &&
                        target.IsAliveHero ? backPos.Pos : -1;
            combatConducts.Add(rePos == -1
                ? InstanceHeroGenericDamage((int)(GeneralDamage() * DamageRate()))
                : InstanceHeroGenericDamage());

            Chessboard.AppendOpActivity(this, target, Activity.Offensive, combatConducts.ToArray(), 0, 1, rePos);
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
            var targets = Chessboard.GetRivals(this).OrderBy(p => p.Pos).Take(Targets()).ToArray();
            if (targets.Length == 0) return;
            var addOn = 0.5f * GeneralDamage();
            var perform = InstanceHeroGenericDamage(addOn);
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                Chessboard.AppendOpActivity(this, target, Activity.Offensive, Helper.Singular(perform), 0, 1);
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
            var target = Chessboard.GetContraTarget(this);
            var combo = Chessboard.IsRandomPass(ComboRate) ? Combo() : 1;
            for (int i = 0; i < combo; i++)
            {
                var result = Chessboard.AppendOpActivity(this, target, Activity.Offensive,
                    Helper.Singular(InstanceHeroGenericDamage()), i, i > 0 ? 1 : 0);
                if (result == null || result.IsDeath) return;
                if (!Chessboard.IsRandomPass(ComboRate))
                    break;
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
                case 18: return 2;
                case 106: return 3;
                case 107: return 5;
                default: throw MilitaryNotValidError(this);
            }
        }
        private int DamageRate()
        {
            switch (Style.Military)
            {
                case 18: return 5;
                case 106: return 10;
                case 107: return 15;
                default: throw MilitaryNotValidError(this);
            }
        }
        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetContraTarget(this);
            var status = Chessboard.GetStatus(target.Operator);
            var dmgGapValue = Strength * DamageRate() * 0.01;//每10%掉血增加数
            var additionDamage = HpDepletedRatioWithGap(status, 0, 10, (int)dmgGapValue);
            var performDamage = InstanceHeroGenericDamage(additionDamage);
            var breakShield = Chessboard.GetCondition(target.Operator, CardState.Cons.Shield) > 0 ? 1 : 0;
            Chessboard.AppendOpActivity(this, target, Activity.Offensive, new[]
            {
                CombatConduct.InstanceBuff(InstanceId,CardState.Cons.Shield, -BreakShields()),
                performDamage
            }, 0, breakShield);
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
                if (!Chessboard
                    .AppendOpActivity(this, target, Activity.Offensive, Helper.Singular(InstanceHeroGenericDamage(addOnDmg)), i,
                        i > 0 ? 1 : 0) //第二斩开始算技能连斩
                    .IsDeath)
                    break;
                addOnDmg += (int)(GeneralDamage() * DamageRate() * 0.01f);
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
                case 15: return 30;
                case 102: return 45;
                case 103: return 60;
                default: throw MilitaryNotValidError(this);
            }
        }
        private IEnumerable<IChessPos> ExtendedTargets(IChessPos target) => Chessboard.GetNeighbors(target, false);
        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetContraTarget(this);
            var splashTargets = ExtendedTargets(target);
            var damage = InstanceHeroGenericDamage();
            var splash = damage.Clone(DamageRate() * 0.01f);
            Chessboard.AppendOpActivity(this, target, Activity.Offensive, Helper.Singular(damage), actId: 0, skill: 1);
            foreach (var spTarget in splashTargets)
            {
                Chessboard.AppendOpActivity(this, spTarget, Activity.Offensive, Helper.Singular(splash), actId: -1, skill: -1);
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
                Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(offender), Activity.Counter,
                    Helper.Singular(InstanceHeroGenericDamage()), -1, skill: 1);
        }
    }
    /// <summary>
    /// 12  神武 - 每次攻击时，获得1层【战意】，【战意】可提升伤害。
    /// </summary>
    public class ShenWuOperator : HeroOperator
    {
        private const int RecursiveLimit = 10;
        private static int loopCount = 0;
        private int DamageRate => 10;

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetContraTarget(this);
            var soul = Chessboard.GetCondition(this, CardState.Cons.BattleSoul);
            var addOn = DamageRate * 0.01f * soul * GeneralDamage();
            var result = Chessboard.AppendOpActivity(this, target, Activity.Offensive,
                Helper.Singular(InstanceHeroGenericDamage(addOn)), 0, 0);

            Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(this), Activity.Self,
                Helper.Singular(BattleSoulConduct), -1, 2);

            var targetStat = Chessboard.GetStatus(target.Operator);
            var resultType = result.Type;
            while ((resultType == ActivityResult.Types.Shield ||
                    resultType == ActivityResult.Types.Dodge) &&
                   !targetStat.IsDeath &&
                   loopCount <= RecursiveLimit)
            {
                Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(this), Activity.Self,
                    Helper.Singular(BattleSoulConduct), loopCount + 1, 2);
                resultType = Chessboard.AppendOpActivity(this, target, Activity.Offensive,
                    Helper.Singular(InstanceHeroGenericDamage(addOn)), loopCount + 1, 1).Type;
                loopCount++;
            }

            loopCount = 0;
        }

        private CombatConduct BattleSoulConduct => CombatConduct.InstanceBuff(InstanceId, CardState.Cons.BattleSoul);
        protected override void OnSufferConduct(IChessOperator offender, Activity activity)
        {
            Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(this), Activity.Self,
                Helper.Singular(BattleSoulConduct), 0, 2);
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

        protected override int GeneralDamage() => HpDepletedRatioWithGap(Chessboard.GetStatus(this), Strength, 10, DamageGapRate());
    }
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

        protected override int GeneralDamage() => HpDepletedRatioWithGap(Chessboard.GetStatus(this), Strength, 10, DamageGapRate());
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
                case 10: return 1f;
                case 85: return 1.5f;
                case 86: return 2f;
                default: throw MilitaryNotValidError(this);
            }
        }
        protected override void MilitaryPerforms(int skill = 1)
        {
            if (Chessboard.GetStatus(this).HpRate * 100 > 30)
            {
                base.MilitaryPerforms(0);
                return;
            }

            var damage = Strength * DamageRate();
            var array = Chessboard.GetRivals(this)
                .Where(p => p.IsAliveHero)
                .ToArray();
            for (var i = 0; i < array.Length; i++)
            {
                var pos = array[i];
                Chessboard.AppendOpActivity(this, pos, Activity.Offensive,
                    Helper.Singular(CombatConduct.InstanceDamage(InstanceId, damage)), 0, 1);
            }

            Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(this), Activity.Self, Helper.Singular(CombatConduct.InstanceKilling(InstanceId)), 0, 1);
        }

        public override int GetDodgeRate() =>
            HpDepletedRatioWithGap(Chessboard.GetStatus(this), CombatInfo.DodgeRatio, 10, DodgeAddingRate);
    }

    /// <summary>
    /// (8)战象 - 践踏战场。攻击时可让敌方武将【眩晕】。
    /// </summary>
    public class ZhanXiangOperator : HeroOperator
    {
        private enum DamageState
        {
            Basic,
            Critical,
            Rouse
        }
        private int StunRate(DamageState state, bool major)
        {
            var rate = 0;
            switch (Style.Military)
            {
                case 8:
                    if (major) return 30;
                    switch (state)
                    {
                        case DamageState.Critical: rate = 20; break;
                        case DamageState.Rouse: rate = 30; break;
                    }
                    break;
                case 174:
                    if (major) return 50;
                    switch (state)
                    {
                        case DamageState.Critical: rate = 25; break;
                        case DamageState.Rouse: rate = 40; break;
                    }
                    break;
                case 175:
                    if (major) return 70;
                    switch (state)
                    {
                        case DamageState.Critical: rate = 30; break;
                        case DamageState.Rouse: rate = 50; break;
                    }
                    break;
                default: throw MilitaryNotValidError(this);
            }
            return rate;
        }
        private int SplashDamageRate(DamageState state)
        {
            switch (Style.Military)
            {
                case 8:
                    switch (state)
                    {
                        case DamageState.Critical: return 20;
                        case DamageState.Rouse: return 20;
                    }
                    break;
                case 174:
                    switch (state)
                    {
                        case DamageState.Critical: return 30;
                        case DamageState.Rouse: return 30;
                    }
                    break;
                case 175:
                    switch (state)
                    {
                        case DamageState.Critical: return 50;
                        case DamageState.Rouse: return 50;
                    }
                    break;
                default: throw MilitaryNotValidError(this);
            }

            return 0;
        }

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetContraTarget(this);
            var majorCombat = InstanceHeroGenericDamage();
            var list = new List<CombatConduct> { majorCombat };
            var combatState = majorCombat.IsRouseDamage() ? DamageState.Rouse :
                majorCombat.IsCriticalDamage() ? DamageState.Critical : DamageState.Basic;
            if (Chessboard.IsRandomPass(StunRate(combatState, true)))
                list.Add(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Stunned));
            Chessboard.AppendOpActivity(this, target, Activity.Offensive, list.ToArray(), 0,
                combatState == DamageState.Basic ? 0 : 1);

            if (combatState == DamageState.Basic) return;
            //会心与暴击才触发周围伤害
            var splashTargets = Chessboard.GetNeighbors(target, false).ToArray();
            var splashDamage = majorCombat.Clone(SplashDamageRate(combatState) * 0.01f);
            foreach (var splashTarget in splashTargets)
            {
                var spList = new List<CombatConduct> { splashDamage };
                if (Chessboard.IsRandomPass(StunRate(combatState, false)))
                    spList.Add(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Stunned));
                Chessboard.AppendOpActivity(this, splashTarget, Activity.Offensive, spList.ToArray(), -1, -1);
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
                case 76: return 1.5f;
                case 77: return 2f;
                default: throw MilitaryNotValidError(this);
            }
        }
        protected override void MilitaryPerforms(int skill = 1)
        {
            base.MilitaryPerforms(0);//刺甲攻击是普通攻击
        }

        protected override void OnSufferConduct(IChessOperator offender, Activity activity)
        {
            if (offender.IsRangeHero) return;
            var damage = activity.Conducts.Where(c => c.Kind == CombatConduct.DamageKind)
                             .Sum(c => c.Total) * ReflectRate();
            Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(offender), Activity.Reflect,
                Helper.Singular(CombatConduct.InstanceDamage(InstanceId, damage)), -1, 1);
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
            var target = Chessboard.GetContraTarget(this);
            if (!target.IsAliveHero) base.MilitaryPerforms(0);
            for (int i = 0; i < ComboRatio(); i++)
            {
                var result = Chessboard.AppendOpActivity(this, target, Activity.Offensive,
                    Helper.Singular(InstanceHeroGenericDamage()), i, i > 0 ? 1 : 0);//伤害活动

                var lastDmg = result?.Status?.LastSuffers?.LastOrDefault();
                if (result == null) return;
                if (result.IsDeath || //对手死亡
                    result.Type != ActivityResult.Types.Suffer || //对手反馈非承受
                    result.Status == null ||
                    !lastDmg.HasValue ||
                    lastDmg.Value <= 0 || //对手伤害=0
                    stat.Hp >= stat.MaxHp) //自身满血
                    break;
                Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(this), Activity.Self,
                    Helper.Singular(CombatConduct.InstanceHeal(lastDmg.Value, InstanceId)), i, 2); //吸血活动
            }
        }
    }

    /// <summary>
    /// (5)陷阵 - 武将陷入危急之时，进入【无敌】状态。
    /// </summary>
    public class XianZhenOperator : HeroOperator
    {
        private int InvincibleRate()
        {
            switch (Style.Military)
            {
                case 5: return 30;
                case 81: return 50;
                case 82: return 70;
                default: throw MilitaryNotValidError(this);
            }
        }

        protected override void MilitaryPerforms(int skill = 1) => base.MilitaryPerforms(0);

        protected override void OnAfterSubtractHp(int damage, CombatConduct conduct)
        {
            if ((conduct.Rouse > 0 || conduct.Critical > 0) &&
                Chessboard.IsRandomPass(InvincibleRate()))
                Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(this), Activity.Self,
                    Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Invincible)), 0, 1);
        }
    }

    /// <summary>
    /// (4)大盾 - 战斗前装备1层【护盾】。每次进攻后装备1层【护盾】。
    /// </summary>
    public class DaDunOperator : HeroOperator
    {
        private int ShieldRate()
        {
            switch (Style.Military)
            {
                case 4: return 1;
                case 68: return 2;
                case 69: return 3;
                default: throw MilitaryNotValidError(this);
            }
        }
        public override void OnRoundStart()
        {
            Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(this), Activity.Self,
                Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Shield, ShieldRate())), -1, 1);
        }

        protected override void MilitaryPerforms(int skill = 1)
        {
            var target = Chessboard.GetContraTarget(this);
            if (target == null) return;
            var damage = InstanceHeroGenericDamage();
            Chessboard.AppendOpActivity(this, target, Activity.Offensive, Helper.Singular(damage), 0, 0);
            var shield = 1;
            if (damage.IsRouseDamage())
                shield++;
            Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(this), Activity.Self,
                Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Shield, shield)), -1,
                damage.IsRouseDamage() ? 2 : 1);
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

        public override int GetDodgeRate() => HpDepletedRatioWithGap(Chessboard.GetStatus(this), CombatInfo.DodgeRatio,
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
            var armor = CombatInfo.PhysicalResist;
            var status = Chessboard.GetStatus(this);
            return HpDepletedRatioWithGap(status, armor, 10, ArmorRate());
        }
    }

    public class WeightElement<T> : IWeightElement
    {
        public static Random Random { get; } = new Random();
        public int Weight { get; set; }
        public T Obj { get; set; }
    }
}