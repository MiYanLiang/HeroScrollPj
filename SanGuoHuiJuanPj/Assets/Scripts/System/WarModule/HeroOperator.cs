﻿using System.Collections.Generic;

namespace Assets.System.WarModule
{
    public class HeroOperator : ChessmanOperator
    {
        public const int HeroArmorLimit = 90;
        public const int HeroDodgeLimit = 75;
        public HeroCombatInfo CombatInfo => combatInfo;
        private HeroCombatInfo combatInfo;

        /// <summary>
        /// 根据当前血量损失的<see cref="gap"/>%，根据<see cref="gapValue"/>的值倍增
        /// </summary>
        /// <param name="maxHp"></param>
        /// <param name="basicValue">起始值</param>
        /// <param name="gap"></param>
        /// <param name="gapValue"></param>
        /// <param name="hp"></param>
        /// <returns></returns>
        public static int HpDepletedRatioWithGap(int hp,int maxHp,int basicValue,int gap, int gapValue)
        {
            var addOn = gap - (hp / maxHp * 100 / gap);
            return basicValue + addOn * gapValue;
        }

        public static int HpDepletedRatioWithGap(PieceStatus status, int basicValue, int gap, int gapValue) =>
            HpDepletedRatioWithGap(status.Hp,status.MaxHp, basicValue, gap, gapValue);

        public override void Init(FightCardData card, AttackStyle style, IChessboardOperator<FightCardData> chessboardOp)
        {
            combatInfo = HeroCombatInfo.GetInfo(card.cardId);
            base.Init(card, style, chessboardOp);
        }

        public override void StartActions()
        {
            if (Status.GetBuff(FightState.Cons.Stunned) > 0) return;
            if (Status.GetBuff(FightState.Cons.Imprisoned) > 0)
            {
                BasicActions();
                return;
            }
            MilitaryPerforms();
        }

        protected ActivityResult BasicActions()
        {
            var target = Grid.GetContraPositionInSequence(Chessman);
            if (target == null) return null;
            return Chessboard.ActionRespondResult(this, target, Activity.Offensive, BasicDamage());
        }

        private CombatConduct[] BasicDamage() => Singular(CombatConduct.InstanceDamage(Card.damage, Card.cardDamageType));

        /// <summary>
        /// 兵种攻击
        /// </summary>
        /// <returns></returns>
        protected virtual void MilitaryPerforms()
        {
            var target = Grid.GetContraPositionInSequence(Chessman);
            if (target == null) return;
            Chessboard.ActionRespondResult(this, target, Activity.Offensive, MilitaryDamages(target));
        }

        protected virtual CombatConduct[] MilitaryDamages(IChessPos<FightCardData> targetPos) => Singular(InstanceHeroPerformDamage());

        /// <summary>
        /// 当治疗的时候血量转化
        /// </summary>
        /// <param name="conduct"></param>
        /// <returns></returns>
        protected override int OnHealConvert(CombatConduct conduct) => (int)conduct.Total;
        /// <summary>
        /// 当被伤害的时候，伤害值转化
        /// </summary>
        /// <param name="conduct"></param>
        /// <returns></returns>
        protected override int OnDamageConvert(CombatConduct conduct)
        {
            var damage = conduct.Total;
            if (conduct.Element <= 0)
            {
                if (Status.GetBuff(FightState.Cons.Bleed) > 0)
                    damage += GetRatio(damage, DataTable.GetGameValue(117));
                if (Status.GetBuff(FightState.Cons.Unarmed) > 0) return (int) damage;
            }
            var armor = conduct.Element > 0 ? GetMagicArmor() : GetPhysicArmor();
            if (armor > HeroArmorLimit) armor = HeroArmorLimit;
            //法术
            return (int) (damage + GetRatio(damage, -armor));
        }
        /// <summary>
        /// 法术免伤
        /// </summary>
        /// <returns></returns>
        protected virtual int GetMagicArmor() => combatInfo.MagicResist;
        /// <summary>
        /// 物理免伤
        /// </summary>
        /// <returns></returns>
        protected virtual int GetPhysicArmor() => combatInfo.PhysicalResist;

        protected override int OnBuffingConvert(CombatConduct conduct) => (int) conduct.Total;

        /// <summary>
        /// 根据武将属性生成伤害
        /// </summary>
        /// <returns></returns>
        protected CombatConduct InstanceHeroPerformDamage(int damage = -1)
        {
            if (damage < 0)
                damage = GetBasicDamage();
            //胆怯不暴击和会心
            if (Status.GetBuff(FightState.Cons.Cowardly) <= 0)
            {
                var critical = TryGenerateCritical();
                if (critical > 0) return CombatConduct.InstanceDamage(damage, critical, Card.cardDamageType);
                var rouse = TryGenerateRouseDamage();
                if (rouse > 0)
                    return CombatConduct.Instance(damage, 0, rouse, Card.cardDamageType, CombatConduct.DamageKind);
            }
            return CombatConduct.InstanceDamage(damage, Card.cardDamageType);
        }

        protected virtual int GetBasicDamage() => Card.damage;

        //跟据会心率获取会心伤害
        private float TryGenerateRouseDamage()
        {
            var rouse = Card.damage - combatInfo.GetRouseDamage(Card.damage);
            if (Status.TryDeplete(FightState.Cons.ShenZhu))
                return rouse;
            if (Chessboard.RandomFromHeroTable(Card.cardId, 2))
                return rouse;
            return 0;
        }
        //根据暴击率获取暴击伤害
        private float TryGenerateCritical()
        {
            var critical = Card.damage - combatInfo.GetCriticalDamage(Card.damage);
            if (Status.TryDeplete(FightState.Cons.Neizhu))
                return critical;
            if (Chessboard.RandomFromHeroTable(Card.cardId, 1))
                return critical;
            return 0;
        }

        protected override bool DodgeOnAttack(IChessOperator<FightCardData> offender)
        {
            var dodgeRate = GetDodgeRate();
            var buffAddOn = Status.GetBuff(FightState.Cons.FengShenTaiAddOn);
            if (offender != null && offender.Style.CombatStyle == AttackStyle.RangeCombatStyle)
                buffAddOn += Status.GetBuff(FightState.Cons.MiWuZhenAddOn);
            var value = dodgeRate + buffAddOn;
            if (value > HeroDodgeLimit)
                value = HeroDodgeLimit;
            return Chessboard.IsRandomPass(value);
        }

        protected virtual int GetDodgeRate() => combatInfo.DodgeRatio;

        /// <summary>
        /// 计算比率值(不包括原来的值)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        private static float GetRatio(float value, int ratio) => ratio * 0.01f * value;
    }

    /// <summary>
    /// (1)近战 - 武将受到重创时，为自身添加1层【护盾】，可叠加。
    /// </summary>
    public class JinZhanOperator : HeroOperator
    {
        protected override void OnAfterSubtractHp(int damage, CombatConduct conduct)
        {
            Chessboard.ActionRespondResult(this, Grid.GetChessPos(Chessman), Activity.Self,
                Singular(CombatConduct.InstanceBuff(FightState.Cons.Shield)));
        }
    }

    /// <summary>
    /// (9)先锋 - 骑马舞枪，攻击时，有概率连续攻击2次。
    /// </summary>
    public class XianFengOperator : HeroOperator
    {
        protected virtual int ComboRatio => DataTable.GetGameValue(42);
        protected virtual int ComboTimes => DataTable.GetGameValue(43);

        protected override void MilitaryPerforms()
        {
            var target = Grid.GetContraPositionInSequence(Chessman);
            if (target == null) return;

            for (int i = 0; i < ComboTimes; i++)
            {
                Chessboard.ActionRespondResult(this, target, Activity.Offensive, Singular(InstanceHeroPerformDamage()));
                if (!Chessboard.IsRandomPass(ComboRatio))
                    break;//如果不触发，就直接停止
            }
        }
    }
    /// <summary>
    /// (60)急先锋处理器
    /// </summary>
    public class JiXianFengOperator : XianFengOperator
    {
        protected override int ComboRatio => DataTable.GetGameValue(44);
        protected override int ComboTimes => DataTable.GetGameValue(45);
    }
    /// <summary>
    /// (16)骠骑处理器
    /// </summary>
    public class PiaoQiOperator : HeroOperator
    {
        protected virtual int ComboRatio => DataTable.GetGameValue(47);
        protected override void MilitaryPerforms()
        {
            var target = Grid.GetContraPositionInSequence(Chessman);
            if (target == null) return;
            //var tOp = Chessboard.GetOperator(target);
            var combo = false;
            do
            {
                combo = false;
                var hit = InstanceHeroPerformDamage();
                var result = Chessboard.ActionRespondResult(this, target, Activity.Offensive, Singular(hit));
                if (!result.IsDeath && result.Result != ActivityResult.Friendly)
                    combo = hit.Critical > 0 || hit.Rouse > 0;
                if (!combo) combo = Chessboard.RandomFromConfigTable(47);
            } while (combo);
        }

    }
}