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

        protected override void OnSufferConduct(IChessOperator offender, Activity activity)
        {
            if (IsGongChengChe(offender) ||
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
            Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(offender), Activity.Offensive, CounterConducts, actId: 0, skill: 1);
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
            Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(offender), Activity.Offensive,
                Helper.Singular(CombatConduct.InstanceDamage(InstanceId, reflectDamage)), actId: 0, skill: 1);
        }

        protected override CombatConduct[] CounterConducts => null;//拒马不需要基础伤害
    }
    /// <summary>
    /// 滚木处理器
    /// </summary>
    public class GunMuOperator : TrapOperator
    {
        protected override void OnDeadTrigger(int conduct)
        {
            var poses = Chessboard.Grid.GetRivalScope(this).Where(c => c.Value.IsPostedAlive)
                .OrderBy(c => c.Key).Select(c => c.Value);
            var mapper = Chessboard.FrontRows.ToDictionary(i => i, _ => false); //init mapper
            var max = Chessboard.FrontRows.Length;
            var targets = new List<IChessPos>();
            foreach (var pos in poses)
            {
                var column = pos.Pos % max; //获取直线排数
                if (mapper[column]) continue; //如果当前排已有伤害目标，不记录后排
                targets.Add(pos);
            }

            for (var i = 0; i < targets.Count; i++)
            {
                var pos = targets[i];
                Chessboard.AppendOpActivity(this, pos, Activity.Offensive, InstanceConduct(), actId: 0, skill: 1);
            }

            CombatConduct[] InstanceConduct()
            {
                var basicDmg = CombatConduct.InstanceDamage(InstanceId, Strength);
                //根据比率给出是否眩晕
                if (Chessboard.RandomFromConfigTable(15))
                    return new[]
                    {
                        basicDmg,
                        CombatConduct.InstanceBuff(InstanceId,CardState.Cons.Stunned)
                    };
                return Helper.Singular(basicDmg);
            }
        }
    }
    /// <summary>
    /// 滚石处理器
    /// </summary>
    public class GunShiOperator : TrapOperator
    {
        protected override void OnDeadTrigger(int damage)
        {
            var verticalIndex = Chessboard.GetStatus(this).Pos % 5;//后排直线
            var targets = Chessboard.GetRivals(this, p => p.IsPostedAlive && p.Pos == verticalIndex)
                .OrderBy(p => p.Pos).ToList();

            for (var i = 0; i < targets.Count; i++)
            {
                var pos = targets[i];
                Chessboard.AppendOpActivity(this, pos, Activity.Offensive, InstanceConduct(), actId: 0, skill: 1);
            }

            CombatConduct[] InstanceConduct()
            {
                var basicDmg = CombatConduct.InstanceDamage(InstanceId, Strength);
                //根据比率给出是否眩晕
                if (Chessboard.RandomFromConfigTable(16))
                    return new[]
                    {
                        basicDmg,
                        CombatConduct.InstanceBuff(InstanceId,CardState.Cons.Stunned)
                    };
                return Helper.Singular(basicDmg);
            }
        }
    }
    /// <summary>
    /// 地雷
    /// </summary>
    public class DiLeiOperator : ReflexiveTrapOperator
    {
        protected override CombatConduct[] CounterConducts =>
            Helper.Singular(CombatConduct.InstanceDamage(InstanceId, Strength * Chessboard.ConfigPercentage(9)));
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
        protected override CombatConduct[] CounterConducts => Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Stunned, Chessboard.ConfigValue(133)));
    }
    /// <summary>
    /// 金锁阵
    /// </summary>
    public class JinSuoZhenOperator : ReflexiveTrapOperator
    {
        protected override CombatConduct[] CounterConducts => Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Imprisoned, DataTable.GetGameValue(10)));
    }
    /// <summary>
    /// 鬼兵阵
    /// </summary>
    public class GuiBingZhenOperator : ReflexiveTrapOperator
    {
        protected override CombatConduct[] CounterConducts => Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Cowardly, DataTable.GetGameValue(11)));
    }
    /// <summary>
    /// 火墙
    /// </summary>
    public class FireWallOperator : ReflexiveTrapOperator
    {
        protected override CombatConduct[] CounterConducts => Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Burn, DataTable.GetGameValue(12)));
    }
    /// <summary>
    /// 毒泉
    /// </summary>
    public class PoisonSpringOperator : ReflexiveTrapOperator
    {
        protected override CombatConduct[] CounterConducts => Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Poison, DataTable.GetGameValue(13)));
    }
    /// <summary>
    /// 刀墙
    /// </summary>
    public class BladeWallOperator : ReflexiveTrapOperator
    {
        protected override CombatConduct[] CounterConducts => Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Bleed, DataTable.GetGameValue(14)));
    }
    /// <summary>
    /// 金币宝箱
    /// </summary>
    public class TreasureOperator : TrapOperator
    {
        protected override void OnDeadTrigger(int damage)
        {
            Chessboard.RegResources(this, !IsChallenger, -1, DataTable.EnemyUnit[CardId].GoldReward);
        }
    }
    /// <summary>
    /// 战役宝箱
    /// </summary>
    public class WarChestOperator : TrapOperator
    {
        protected override void OnDeadTrigger(int damage)
        {
            foreach (var warChestId in DataTable.EnemyUnit[CardId].WarChest)
            {
                Chessboard.RegResources(this, !IsChallenger, warChestId, 1);
            }
        }
    }
}