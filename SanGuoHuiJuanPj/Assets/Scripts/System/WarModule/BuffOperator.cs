using System;
using System.ComponentModel.Design;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 棋子的增益效果，与<see cref="TerrainSprite"/>不同的是<see cref="BuffOperator"/>是在棋子身上挂着的状态
    /// </summary>
    public abstract class BuffOperator
    {
        private static Activity[] ZeroActivity = new Activity[0];
        public BuffOperator(ChessboardOperator chessboard)
        {
            Chessboard = chessboard;
        }

        public abstract CardState.Cons Buff { get; }
        protected ChessboardOperator Chessboard { get; }

        public virtual bool IsRoundStartTrigger { get; protected set; }
        public virtual bool IsTriggerRoundStart(ChessOperator op) => false;

        /// <summary>
        /// 配合标记<see cref="IsRoundStartTrigger"/>并在<see cref="IsTriggerRoundStart"/>写出判断后才会在这里执行的方法。
        /// 主要是回合开始的执行方法
        /// </summary>
        /// <param name="op"></param>
        public virtual void RoundStart(ChessOperator op)
        {
        }

        public virtual bool IsRoundEndTrigger { get; protected set; }

        public virtual bool IsHeroPerformTrigger { get; protected set; }
        /// <summary>
        /// 配合标记<see cref="IsHeroPerformTrigger"/>触发，并判断给出是否禁用武将技
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public virtual bool IsDisableHeroPerform(HeroOperator op) => false;

        public virtual bool IsTriggerRoundEnd(ChessOperator op) => false;
        /// <summary>
        /// 配合标记<see cref="IsRoundEndTrigger"/>并在<see cref="IsTriggerRoundEnd"/>写出判断后才会在这里执行的方法。
        /// 主要是回合结束的执行方法
        /// </summary>
        /// <param name="op"></param>
        public virtual void RoundEnd(ChessOperator op)
        {
        }

        public virtual bool IsDamageConvertTrigger(ChessOperator op) => false;

        /// <summary>
        /// 配合<see cref="IsDamageConvertTrigger"/>改变伤害
        /// </summary>
        /// <param name="op"></param>
        /// <param name="conduct"></param>
        /// <returns></returns>
        public virtual void OnDamageConvertTrigger(ChessOperator op, CombatConduct conduct) {}

        public virtual bool IsDodgeRateTrigger(ChessOperator op) => false;
        /// <summary>
        /// 配合<see cref="IsDodgeRateTrigger"/>产出闪避值
        /// </summary>
        /// <param name="op"></param>
        /// <param name="offender"></param>
        /// <returns></returns>
        public virtual int OnAppendDodgeRate(ChessOperator op, IChessOperator offender) => 0;

        public virtual bool IsMainActionTrigger { get; protected set; }

        public virtual bool IsRouseRatioTrigger { get; set; }
        /// <summary>
        /// 配合<see cref="IsRouseRatioTrigger"/>标签触发改变会心值
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public virtual int OnRouseRatio(ChessOperator op) => 0;

        public virtual bool IsCriticalRatioTrigger { get; set; }
        /// <summary>
        /// 配合<see cref="IsCriticalRatioTrigger"/>标签触发改变暴击值
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public virtual int OnCriticalRatio(ChessOperator op) => 0;

        /// <summary>
        /// 是否禁止主行动,配合<see cref="IsMainActionTrigger"/>触发
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public virtual bool IsDisableMainAction(ChessOperator op) => false;

        /// <summary>
        /// 判定是否反击
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public virtual bool IsDisableCounter(ChessOperator op) => false;

        /// <summary>
        /// 如果<see cref="Buff"/>设为<see cref="CardState.Cons.Shield"/>，将会在攻击前进行过滤<see cref="CombatConduct"/>，
        /// 
        /// </summary>
        /// <param name="conducts"></param>
        /// <returns></returns>
        public virtual CombatConduct[] OnCombatConductFilter(CombatConduct[] conducts) => conducts;

        /// <summary>
        /// 配合<see cref="Buff"/>来削减或加深状态值
        /// </summary>
        /// <param name="op"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual int OnBuffConvert(ChessOperator op, int value) => value;

        #region Helper

        protected bool IsBuffActive(ChessOperator op) => Chessboard.GetCondition(op, Buff) > 0;
        protected CombatConduct DepleteBuff(int value = 1) => CombatConduct.InstanceBuff(Buff, -value);

        protected void SelfConduct(ChessOperator op, CombatConduct[] conducts) =>
            Chessboard.InstanceChessboardActivity(op.IsChallenger, op, RoundAction.RoundBuffing, conducts, (int)Buff);
        ///// <summary>
        /// 扣除buff值
        /// </summary>
        /// <param name="op"></param>
        /// <param name="value"></param>
        //protected void DepleteBuff(ChessOperator op, int value = 1) => op.Status.AddBuff(Buff, -value);
        //protected void IncreaseBuff(ChessOperator op, int value = 1) => op.Status.AddBuff(Buff, value);
        #endregion


        public virtual bool IsHealingTrigger(ChessOperator op) => false;
        /// <summary>
        /// 配合<see cref="IsHealingTrigger"/>实现改变治疗值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual int OnHealingConvert(int value) => value;

    }
    //1 眩晕
    public class StunnedBuff : BuffOperator
    {
        public override CardState.Cons Buff => CardState.Cons.Stunned;
        public StunnedBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override bool IsMainActionTrigger => true;

        public override bool IsDisableMainAction(ChessOperator op) => IsBuffActive(op);

        public override bool IsDisableCounter(ChessOperator op) => IsBuffActive(op);

        public override bool IsRoundStartTrigger => true;
        public override void RoundStart(ChessOperator op)
        {
            if (IsBuffActive(op)) SelfConduct(op, Helper.Singular(DepleteBuff()));
        }
    }
    //2 护盾
    public class ShieldBuff : BuffOperator
    {
        public override CardState.Cons Buff => CardState.Cons.Shield;

        public override bool IsDamageConvertTrigger(ChessOperator op) => IsBuffActive(op);

        public ShieldBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override void OnDamageConvertTrigger(ChessOperator op, CombatConduct conduct)
        {
            if (Damage.GetKind(conduct) == Damage.Kinds.Magic)
                return;
            SelfConduct(op,Helper.Singular(DepleteBuff()));
            conduct.SetZero();
        }
    }
    //3 无敌
    public class InvincibleBuff : BuffOperator
    {
        public override CardState.Cons Buff => CardState.Cons.Invincible;
        
        public InvincibleBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override bool IsRoundStartTrigger => true;

        //由于Chessboard上为无敌特别处理略过伤害，所以这里仅写触发
        public override void RoundStart(ChessOperator op)
        {
            if (IsBuffActive(op))
                SelfConduct(op,Helper.Singular(DepleteBuff()));
        }
    }
    //4 流血
    public class BleedBuff : BuffOperator
    {
        public override CardState.Cons Buff => CardState.Cons.Bleed;

        public BleedBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override bool IsRoundEndTrigger => true;
        public override void RoundEnd(ChessOperator op)
        {
            if (IsBuffActive(op))
                SelfConduct(op, Helper.Singular(DepleteBuff()));
        }

        public override bool IsDamageConvertTrigger(ChessOperator op) => IsBuffActive(op);
        public override void OnDamageConvertTrigger(ChessOperator op, CombatConduct conduct)
        {
            if (!IsBuffActive(op) || Damage.GetKind(conduct) != Damage.Kinds.Physical) return;
            conduct.TimesRate(DataTable.GetGameValue(117) * 0.01f);
        }

    }
    //5 毒
    public class PoisonBuff : BuffOperator
    {
        public override CardState.Cons Buff => CardState.Cons.Poison;
        public PoisonBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override bool IsRoundEndTrigger => true;
        public override void RoundEnd(ChessOperator op)
        {
            if (!IsBuffActive(op)) return;
            var damage = Chessboard.GetStatus(op).MaxHp * DataTable.GetGameValue(121) * 0.01f;
            SelfConduct(op,new CombatConduct[]
            {
                CombatConduct.InstanceBuff(Buff,-1),
                CombatConduct.InstanceDamage(damage,1)
            });
        }

    }
    //6 灼烧
    public class BurnBuff : BuffOperator
    {
        public override CardState.Cons Buff => CardState.Cons.Burn;
        public BurnBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }
        public override bool IsRoundEndTrigger => true;
        public override void RoundEnd(ChessOperator op)
        {
            if (!IsBuffActive(op)) return;
            var damage = Chessboard.GetStatus(op).MaxHp * DataTable.GetGameValue(118) * 0.01f;
            SelfConduct(op, new CombatConduct[]
            {
                CombatConduct.InstanceBuff(Buff,-1),
                CombatConduct.InstanceDamage(damage,1)
            });
        }
    }

    //7 战意-不属于buff控制器可空类型

    //8 禁锢
    public class Imprisoned : BuffOperator
    {
        public override CardState.Cons Buff => CardState.Cons.Imprisoned;
        public Imprisoned(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override bool IsHeroPerformTrigger => true;
        public override bool IsDisableHeroPerform(HeroOperator op) => IsBuffActive(op);
        public override bool IsRoundEndTrigger => true;
        public override void RoundEnd(ChessOperator op)
        {
            if (IsBuffActive(op))
                SelfConduct(op, Helper.Singular(DepleteBuff()));
        }
    }

    //9 怯战
    public class CowardlyBuff : BuffOperator
    {
        public override CardState.Cons Buff => CardState.Cons.Cowardly;
        public CowardlyBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override void RoundEnd(ChessOperator op)
        {
            if (IsBuffActive(op))
                SelfConduct(op, Helper.Singular(DepleteBuff()));
        }
    }
    //16 卸甲
    public class DisarmedBuff : BuffOperator
    {
        public override CardState.Cons Buff => CardState.Cons.Disarmed;

        public DisarmedBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override bool IsRoundEndTrigger => true;
        public override void RoundEnd(ChessOperator op)
        {
            if (IsBuffActive(op))
                SelfConduct(op, Helper.Singular(DepleteBuff()));
        }
    }

    //19 缓冲盾
    public class ExtendedShieldBuff : BuffOperator
    {
        public override CardState.Cons Buff => CardState.Cons.EaseShield;
        public ExtendedShieldBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override bool IsDamageConvertTrigger(ChessOperator op) => IsBuffActive(op);

        public override void OnDamageConvertTrigger(ChessOperator op, CombatConduct conduct)
        {
            var status = Chessboard.GetStatus(op);
            var balance = status.EaseShieldOffset(conduct.Total);
            conduct.SetBasic(balance);
        }

    }
}