﻿using System.Collections.Generic;
using System.Linq;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 陷阱处理器
    /// </summary>
    public abstract class TrapOperator : CardOperator
    {
        public override int GetDodgeRate() => 0;

        protected CombatConduct InstanceMechanicalDamage(float damage) => CombatConduct.InstanceDamage(InstanceId, damage, CombatConduct.MechanicalDmg);

    }

    public class BlankTrapOperator : TrapOperator
    {
    }

    /// <summary>
    /// 反伤类的陷阱
    /// </summary>
    public abstract class ReflexiveTrapOperator : TrapOperator
    {
        /// <summary>
        /// 是否攻城车
        /// </summary>
        /// <param name="chess"></param>
        /// <returns></returns>
        protected bool IsGongChengChe(IChessOperator chess) => GongChengCheOperator.IsGongChengChe(chess);

        protected override void OnSufferConduct(Activity activity, IChessOperator offender = null)
        {
            if (offender == null ||
                IsGongChengChe(offender) ||
                activity.Intention != Activity.Intentions.Offensive ||
                offender.IsRangeHero) return;
            InstanceReflection(activity.Conducts.Where(c => c.Kind == CombatConduct.DamageKind), offender);
        }

        /// <summary>
        /// 当基本反击条件已达到，所执行的反击方法，默认执行基础<see cref="CounterConducts"/>伤害
        /// </summary>
        /// <param name="conducts"></param>
        /// <param name="offender"></param>
        /// <returns></returns>
        protected virtual void InstanceReflection(IEnumerable<CombatConduct> conducts, IChessOperator offender)
        {
            Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(offender), Activity.Intentions.Offensive, CounterConducts, actId: -1, skill: 1);
        }

        /// <summary>
        /// 当基本反击条件已达到，所执行的反击伤害
        /// </summary>
        protected abstract CombatConduct[] CounterConducts { get; }

    }
    /// <summary>
    /// 拒马
    /// </summary>
    public class JuMaOperator : ReflexiveTrapOperator
    {
        /// <summary>
        /// 当基本反击条件已达到，所执行的反击方法
        /// </summary>
        /// <param name="conducts"></param>
        /// <param name="offender"></param>
        /// <returns></returns>
        protected override void InstanceReflection(IEnumerable<CombatConduct> conducts, IChessOperator offender)
        {
            var conduct = conducts.First(c => c.Kind == CombatConduct.DamageKind);
            var reflectDamage = conduct.Total * Chessboard.ConfigPercentage(8);
            Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(offender), Activity.Intentions.Offensive,
                Helper.Singular(InstanceMechanicalDamage(reflectDamage)), actId: -1, skill: 1);
        }

        protected override CombatConduct[] CounterConducts => null;//拒马不需要基础伤害
    }
    /// <summary>
    /// 滚木处理器
    /// </summary>
    public class GunMuOperator : TrapOperator
    {
        private int StunningRate => 100;
        protected override void OnDeadTrigger(int conduct)
        {
            var target = Chessboard.GetChessPos(!IsChallenger, Chessboard.GetStatus(this).Pos % 5);//最上一层棋格
            Chessboard.DelegateSpriteActivity<RollingWoodSprite>(this, target, InstanceConduct(), actId: -2, 1);
            CombatConduct[] InstanceConduct()
            {
                var basicDmg = InstanceMechanicalDamage(Strength);
                //根据比率给出是否眩晕
                return new[]
                {
                    basicDmg,
                    CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Stunned, value: 1, rate: StunningRate)
                };
            }
        }
    }
    /// <summary>
    /// 滚石处理器
    /// </summary>
    public class GunShiOperator : TrapOperator
    {
        /// <summary>
        /// 眩晕几率
        /// </summary>
        private int StunningRate => 100;
        protected override void OnDeadTrigger(int damage)
        {
            var verticalIndex = Chessboard.GetStatus(this).Pos % 5;//最上一排
            var targets = Chessboard.GetRivals(this, p => p.Pos % 5 == verticalIndex)
                .OrderBy(p => p.Pos).ToList();
            for (var i = 0; i < targets.Count; i++)
            {
                var pos = targets[i];
                Chessboard.DelegateSpriteActivity<RollingStoneSprite>(this, pos, InstanceConduct(), -2, 1);
            }

            CombatConduct[] InstanceConduct()
            {
                var basicDmg = InstanceMechanicalDamage(Strength);
                //根据比率给出是否眩晕
                return new[]
                {
                    basicDmg,
                    CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Stunned, value: 1, rate: StunningRate)
                };
            }
        }
    }
    /// <summary>
    /// 地雷
    /// </summary>
    public class DiLeiOperator : ReflexiveTrapOperator
    {
        private float DamageRate => 6;
        protected override CombatConduct[] CounterConducts =>
            Helper.Singular(InstanceMechanicalDamage(Strength * DamageRate));
    }
    /// <summary>
    /// 石墙
    /// </summary>
    public class ShiQiangOperator : BlankTrapOperator { }
    /// <summary>
    /// 八阵图
    /// </summary>
    public class BaZhenTuOperator : ReflexiveTrapOperator
    {
        protected override CombatConduct[] CounterConducts => Helper.Singular(CombatConduct.InstanceBuff(InstanceId,
            CardState.Cons.Stunned, value: 1, rate: DataTable.GetGameValue(133)));
    }
    /// <summary>
    /// 金锁阵
    /// </summary>
    public class JinSuoZhenOperator : ReflexiveTrapOperator
    {
        protected override CombatConduct[] CounterConducts => Helper.Singular(CombatConduct.InstanceBuff(InstanceId,
            CardState.Cons.Imprisoned, value: 1, rate: DataTable.GetGameValue(10)));
    }
    /// <summary>
    /// 鬼兵阵
    /// </summary>
    public class GuiBingZhenOperator : ReflexiveTrapOperator
    {
        protected override CombatConduct[] CounterConducts => Helper.Singular(CombatConduct.InstanceBuff(InstanceId,
            CardState.Cons.Cowardly, value: 1, rate: DataTable.GetGameValue(11)));
    }
    /// <summary>
    /// 火墙
    /// </summary>
    public class FireWallOperator : ReflexiveTrapOperator
    {
        protected override CombatConduct[] CounterConducts => Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Burn, value: 1, rate: DataTable.GetGameValue(12)));
    }
    /// <summary>
    /// 毒泉
    /// </summary>
    public class PoisonSpringOperator : ReflexiveTrapOperator
    {
        protected override CombatConduct[] CounterConducts => Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Poison, value: 1, rate: DataTable.GetGameValue(13)));
    }
    /// <summary>
    /// 刀墙
    /// </summary>
    public class BladeWallOperator : ReflexiveTrapOperator
    {
        protected override CombatConduct[] CounterConducts => Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Bleed, value: 1, rate: DataTable.GetGameValue(14)));
    }
    /// <summary>
    /// 金币宝箱
    /// </summary>
    public class TreasureOperator : TrapOperator
    {
        protected override void OnDeadTrigger(int damage) => Chessboard.RegResources(this, !IsChallenger, -1, Strength);
    }
    /// <summary>
    /// 战役宝箱
    /// </summary>
    public class WarChestOperator : TrapOperator
    {
        protected override void OnDeadTrigger(int damage) => Chessboard.RegResources(this, !IsChallenger, Strength, 1);
    }
}