﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 陷阱处理器
    /// </summary>
    public abstract class TrapOperator : ChessmanOperator
    {
        /// <summary>
        /// 陷阱不会主动攻击
        /// </summary>
        /// <returns></returns>
        public override void StartActions() {}

        protected override int OnHealConvert(CombatConduct conduct) => (int) conduct.Total;

        protected override int OnBuffingConvert(CombatConduct conduct) => (int) conduct.Total;

        protected override bool DodgeOnAttack(IChessOperator<FightCardData> iChessOperator) => false;
        protected override int OnDamageConvert(CombatConduct conduct) => (int)conduct.Total;
    }

    public class BlankTrapOperator : TrapOperator
    {
        protected override IList<Activity> OnDeadTrigger(CombatConduct conduct) => null;
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
        protected bool IsGongChengChe(IChessOperator<FightCardData> chess) => chess.Style.Military == 23;
        /// <summary>
        /// 非近战，非可反击目标，非武将
        /// </summary>
        /// <param name="offender"></param>
        /// <returns></returns>
        protected bool MeleeHero(IChessOperator<FightCardData> offender) => offender.Style.ArmedType < 0 ||
                                                                            offender.Style.CounterStyle == 0 || offender.Style.CombatStyle == 1;

        protected override void OnSufferConduct(IChessOperator<FightCardData> offender, CombatConduct[] damages)
        {
            if (Status.Hp <= 0 ||
                IsGongChengChe(offender) ||
                !MeleeHero(offender)) return;
            InstanceReflection(damages, offender);
        }

        /// <summary>
        /// 当基本反击条件已达到，所执行的反击方法，默认执行基础<see cref="CounterConducts"/>伤害
        /// </summary>
        /// <param name="conducts"></param>
        /// <param name="offender"></param>
        /// <returns></returns>
        protected virtual void InstanceReflection(CombatConduct[] conducts, IChessOperator<FightCardData> offender)
        {
            Chessboard.ActionRespondResult(this, Grid.GetChessPos(offender.Chessman.Pos, offender.Chessman.IsPlayer),
                Activity.OffendTrigger, CounterConducts);
        }

        /// <summary>
        /// 当基本反击条件已达到，所执行的反击伤害
        /// </summary>
        protected abstract CombatConduct[] CounterConducts { get; }

        protected override IList<Activity> OnDeadTrigger(CombatConduct conduct) => null;
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
        protected override void InstanceReflection(CombatConduct[] conducts, IChessOperator<FightCardData> offender)
        {
            var conduct = conducts.First(c => c.Kind == CombatConduct.DamageKind);
            var reflectDamage = conduct.Total * Chessboard.ConfigPercentage(8);
            Chessboard.ActionRespondResult(this, Grid.GetChessPos(offender.Chessman.Pos,offender.Chessman.IsPlayer), Activity.Counter, Singular(CombatConduct.InstanceDamage(reflectDamage)));
        }

        protected override CombatConduct[] CounterConducts => null;//拒马不需要基础伤害
    }
    /// <summary>
    /// 滚木处理器
    /// </summary>
    public class GunMuOperator : TrapOperator
    {
        protected override IList<Activity> OnDeadTrigger(CombatConduct conduct)
        {
            var poses = Chessboard.Grid.GetRivalScope(Chessman).Where(c => c.Value.Chessman.IsAlive)
                .OrderBy(c => c.Key).Select(c=>c.Value);
            var mapper = Grid.FrontRows.ToDictionary(i => i, _ => false); //init mapper
            var max = Grid.FrontRows.Length;
            var targets = new List<IChessPos<FightCardData>>();
            foreach (var pos in poses)
            {
                var column = pos.Pos % max; //获取直线排数
                if (mapper[column]) continue; //如果当前排已有伤害目标，不记录后排
                targets.Add(pos);
            }

            return targets.SelectMany(pos=>Chessboard.ActionRespondResult(this,pos,Activity.OffendTrigger,InstanceConduct()).Activities).ToArray();

            CombatConduct[] InstanceConduct()
            {
                var basicDmg = CombatConduct.InstanceDamage(Card.damage);
                //根据比率给出是否眩晕
                if (Chessboard.RandomFromConfigTable(15))
                    return new[]
                    {
                        basicDmg,
                        CombatConduct.InstanceBuff(FightState.Cons.Stunned)
                    };
                return Singular(basicDmg);
            }
        }
    }
    /// <summary>
    /// 滚石处理器
    /// </summary>
    public class GunShiOperator : TrapOperator
    {
        protected override IList<Activity> OnDeadTrigger(CombatConduct conduct)
        {
            var verticalIndex = Card.PosIndex % 5;//后排直线
            var targets = Grid.GetRivalScope(Chessman)
                .Where(c => c.Value.Chessman.IsAlive && c.Key == verticalIndex)
                .OrderBy(c => c.Key).Select(c=>c.Value);

            return targets.SelectMany(pos => Chessboard.ActionRespondResult(this,pos, Activity.OffendTrigger, InstanceConduct()).Activities).ToArray();

            CombatConduct[] InstanceConduct()
            {
                var basicDmg = CombatConduct.InstanceDamage(Card.damage);
                //根据比率给出是否眩晕
                if (Chessboard.RandomFromConfigTable(16))
                    return new[]
                    {
                        basicDmg,
                        CombatConduct.InstanceBuff(FightState.Cons.Stunned)
                    };
                return Singular(basicDmg);
            }
        }
    }
    /// <summary>
    /// 地雷
    /// </summary>
    public class DiLeiOperator : ReflexiveTrapOperator
    {
        protected override CombatConduct[] CounterConducts => Singular(CombatConduct.InstanceDamage(Card.damage * Chessboard.ConfigPercentage(9)));
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
        protected override CombatConduct[] CounterConducts => Singular(CombatConduct.InstanceBuff(FightState.Cons.Stunned, Chessboard.ConfigValue(133)));
    }
    /// <summary>
    /// 金锁阵
    /// </summary>
    public class JinSuoZhenOperator : ReflexiveTrapOperator
    {
        protected override CombatConduct[] CounterConducts =>
            Singular(CombatConduct.InstanceBuff(FightState.Cons.Imprisoned, DataTable.GetGameValue(10)));
    }
    /// <summary>
    /// 鬼兵阵
    /// </summary>
    public class GuiBingZhenOperator : ReflexiveTrapOperator
    {
        protected override CombatConduct[] CounterConducts =>
            Singular(CombatConduct.InstanceBuff(FightState.Cons.Cowardly, DataTable.GetGameValue(11)));
    }
    /// <summary>
    /// 火墙
    /// </summary>
    public class FireWallOperator : ReflexiveTrapOperator
    {
        protected override CombatConduct[] CounterConducts =>
            Singular(CombatConduct.InstanceBuff(FightState.Cons.Burn, DataTable.GetGameValue(12)));
    }
    /// <summary>
    /// 毒泉
    /// </summary>
    public class PoisonSpringOperator : ReflexiveTrapOperator
    {
        protected override CombatConduct[] CounterConducts =>
            Singular(CombatConduct.InstanceBuff(FightState.Cons.Poison, DataTable.GetGameValue(13)));
    }
    /// <summary>
    /// 刀墙
    /// </summary>
    public class BladeWallOperator : ReflexiveTrapOperator
    {
        protected override CombatConduct[] CounterConducts =>
            Singular(CombatConduct.InstanceBuff(FightState.Cons.Bleed, DataTable.GetGameValue(14)));
    }
    /// <summary>
    /// 金币宝箱
    /// </summary>
    public class TreasureOperator : TrapOperator
    {
        protected override IList<Activity> OnDeadTrigger(CombatConduct conduct)
        {
            Chessboard.RegGoldOnRoundEnd(this,-2, DataTable.EnemyUnit[Card.unitId].GoldReward);
            return Activity.Empty;
        }
    }
    /// <summary>
    /// 战役宝箱
    /// </summary>
    public class WarChestOperator : TrapOperator
    {
        protected override IList<Activity> OnDeadTrigger(CombatConduct conduct)
        {
            Chessboard.RegWarChestOnRoundEnd(this,-2, DataTable.EnemyUnit[Card.unitId].WarChest);
            return Activity.Empty;
        }
    }
}