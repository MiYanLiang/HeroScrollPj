using System.Collections.Generic;
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
        protected override void OnReflectingConduct(Activity activity, ChessOperator offender)
        {
            if (GongChengCheOperator.IsGongChengChe(offender)) //对象是攻城车
                return;
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
            Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(offender), Activity.Intentions.Inevitable,
                CounterConducts, actId: -1, skill: 1);
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
        private float DamageRate => 1.2f;
        /// <summary>
        /// 当基本反击条件已达到，所执行的反击方法
        /// </summary>
        /// <param name="conducts"></param>
        /// <param name="offender"></param>
        /// <returns></returns>
        protected override void InstanceReflection(IEnumerable<CombatConduct> conducts, IChessOperator offender)
        {
            var conduct = conducts.FirstOrDefault(c => c.Kind == CombatConduct.DamageKind);
            if (conduct == null) return;
            var reflectDamage = conduct.Total * DamageRate;
            Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(offender), Activity.Intentions.Inevitable,
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
        protected override void OnDeadTrigger(ChessOperator offender, int conduct)
        {
            var scope = Chessboard.GetRivals(this, _ => true).OrderBy(pos => pos.Pos).ToArray();
            var mapper = Chessboard.FrontRows.ToDictionary(i => i, _ => false); //init mapper
            var lastRow = -1;
            foreach (var pos in scope)
            {
                var column = pos.Pos % 5; //获取直线排数
                var row = pos.Pos / 5;
                if (mapper[column]) continue; //如果当前排已有伤害目标，不记录后排
                Chessboard.DelegateSpriteActivity<RollingWoodSprite>(this, pos, InstanceConduct(),
                    actId: lastRow != row ? -2 : -1,
                    skill: 1);
                lastRow = row;
                mapper[column] = pos.IsPostedAlive;
            }
            CombatConduct[] InstanceConduct()
            {
                var basicDmg = InstanceMechanicalDamage(StateDamage());
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
        protected override void OnDeadTrigger(ChessOperator offender, int damage)
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
                var basicDmg = InstanceMechanicalDamage(StateDamage());
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
            Helper.Singular(InstanceMechanicalDamage(StateDamage() * DamageRate));
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
        private int StunnedRate => 60;
        protected override CombatConduct[] CounterConducts => Helper.Singular(CombatConduct.InstanceBuff(InstanceId,
            CardState.Cons.Stunned, value: 1, rate: StunnedRate));
    }
    /// <summary>
    /// 金锁阵
    /// </summary>
    public class JinSuoZhenOperator : ReflexiveTrapOperator
    {
        private int ImprisonRate => 75;
        protected override CombatConduct[] CounterConducts => Helper.Singular(CombatConduct.InstanceBuff(InstanceId,
            CardState.Cons.Imprisoned, value: 1, rate: ImprisonRate));
    }
    /// <summary>
    /// 鬼兵阵
    /// </summary>
    public class GuiBingZhenOperator : ReflexiveTrapOperator
    {
        private int CowardlyRate => 90;

        protected override CombatConduct[] CounterConducts => Helper.Singular(CombatConduct.InstanceBuff(InstanceId,
            CardState.Cons.Cowardly, value: 1, rate: CowardlyRate));
    }
    /// <summary>
    /// 火墙
    /// </summary>
    public class FireWallOperator : ReflexiveTrapOperator
    {
        private int FireRate => 90;

        protected override CombatConduct[] CounterConducts =>
            Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Burn, value: 2, rate: FireRate));
    }
    /// <summary>
    /// 毒泉
    /// </summary>
    public class PoisonSpringOperator : ReflexiveTrapOperator
    {
        private int PoisonRate => 75;
        protected override CombatConduct[] CounterConducts => Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Poison, value: 1, rate: PoisonRate));
    }
    /// <summary>
    /// 刀墙
    /// </summary>
    public class BladeWallOperator : ReflexiveTrapOperator
    {
        private int BleedRate => 75;

        protected override CombatConduct[] CounterConducts =>
            Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Bleed, value: 1, rate: BleedRate));
    }
    /// <summary>
    /// 金币宝箱
    /// </summary>
    public class TreasureOperator : TrapOperator
    {
        protected override void OnDeadTrigger(ChessOperator offender, int damage) => Chessboard.RegResources(this, !IsChallenger, -1, Level);
    }
    /// <summary>
    /// 战役宝箱
    /// </summary>
    public class WarChestOperator : TrapOperator
    {
        protected override void OnDeadTrigger(ChessOperator offender, int damage) => Chessboard.RegResources(this, !IsChallenger, Level, 1);
    }
}