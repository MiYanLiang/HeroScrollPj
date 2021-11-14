using System;
using System.ComponentModel.Design;
using System.Linq;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 棋子的增益效果，与<see cref="PosSprite"/>不同的是<see cref="BuffOperator"/>是在棋子身上挂着的状态
    /// </summary>
    // todo ChessOperator 与buffOperator存在共同管理buff问题，需要统一管理
    public abstract class BuffOperator
    {
        private static Activity[] ZeroActivity = Array.Empty<Activity>();
        public BuffOperator(ChessboardOperator chessboard)
        {
            Chessboard = chessboard;
        }

        public abstract CardState.Cons Buff { get; }
        protected ChessboardOperator Chessboard { get; }

        public virtual bool IsRoundStartTrigger { get; protected set; }

        /// <summary>
        /// 配合标记<see cref="IsRoundStartTrigger"/>
        /// 主要是回合开始的执行方法
        /// </summary>
        /// <param name="op"></param>
        public virtual void RoundStart(ChessOperator op)
        {
        }

        public virtual bool IsRoundEndTrigger { get; protected set; }

        /// <summary>
        /// 配合标记<see cref="IsRoundEndTrigger"/>在这里执行的方法。
        /// 主要是回合结束的执行方法
        /// </summary>
        /// <param name="op"></param>
        public virtual void RoundEnd(ChessOperator op)
        {
        }
        public virtual bool IsHeroPerformTrigger { get; protected set; }
        /// <summary>
        /// 配合标记<see cref="IsHeroPerformTrigger"/>触发，并判断给出是否禁用武将技
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public virtual bool IsDisableHeroPerform(HeroOperator op) => false;

        public virtual bool IsElementTrigger => false;

        /// <summary>
        /// 配合<see cref="IsElementTrigger"/>改变伤害
        /// </summary>
        /// <param name="op"></param>
        /// <param name="conduct"></param>
        /// <returns></returns>
        public virtual void OnElementConduct(ChessOperator op, CombatConduct conduct) { }

        public virtual bool IsDodgeRateTrigger => false;
        /// <summary>
        /// 配合<see cref="IsDodgeRateTrigger"/>产出闪避值
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public virtual int OnAppendDodgeRate(ChessOperator op) => 0;

        public virtual bool IsMainActionTrigger { get; protected set; }

        public virtual bool IsRouseRatioTrigger { get; set; }
        /// <summary>
        /// 配合<see cref="IsRouseRatioTrigger"/>标签触发改变会心值
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public virtual int OnRouseRatioAddOn(ChessOperator op) => 0;

        public virtual bool IsCriticalRatioTrigger { get; set; }
        /// <summary>
        /// 配合<see cref="IsCriticalRatioTrigger"/>标签触发改变暴击值
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public virtual int OnCriticalRatioAddOn(ChessOperator op) => 0;

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
        /// 配合<see cref="Buff"/>来削减或加深状态值
        /// </summary>
        /// <param name="op"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual int OnBuffConvert(ChessOperator op, int value) => value;
        /// <summary>
        /// 根据攻击元素改变免伤值
        /// </summary>
        /// <returns></returns>
        public virtual bool IsArmorAddOnTrigger => false;


        public virtual int OnArmorAddOn(float armor, ChessOperator op, CombatConduct conduct) => 0;
        #region Helper

        protected bool IsBuffActive(ChessOperator op) => Chessboard.GetCondition(op, Buff) > 0;
        protected CombatConduct DepleteBuff(int value = 1) => CombatConduct.InstanceBuff((int)Buff, Buff, -value);

        protected void SelfConduct(ChessOperator op, Activity.Intentions activityIntent, params CombatConduct[] conducts) =>
            Chessboard.InstanceChessboardActivity(op.IsChallenger, op, activityIntent, conducts,
                skill: (int)Buff);
        #endregion

        public virtual bool IsHealingTrigger(ChessOperator op) => false;
        /// <summary>
        /// 配合<see cref="IsHealingTrigger"/>实现改变治疗值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual int OnHealingConvert(int value) => value;

        public virtual bool IsSufferConductTrigger => false;
        public virtual void OnSufferConduct(Activity.Intentions activityIntention, ChessOperator op,
            CombatConduct conduct, ActivityResult activityResult)
        {
        }
    }

    public abstract class RoundEndDepleteSelfBuff : BuffOperator
    {
        protected RoundEndDepleteSelfBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override bool IsRoundEndTrigger => true;

        public override void RoundEnd(ChessOperator op)
        {
            if (IsBuffActive(op))
                SelfConduct(op, Activity.Intentions.ChessboardBuffing, DepleteBuff());
        }

    }
    //1 眩晕
    public class StunnedBuff : RoundEndDepleteSelfBuff
    {
        public override CardState.Cons Buff => CardState.Cons.Stunned;
        public StunnedBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override bool IsDodgeRateTrigger => true;
        public override int OnAppendDodgeRate(ChessOperator op) => IsBuffActive(op) ? -999 : 0;


        public override bool IsElementTrigger => true;
        public override void OnElementConduct(ChessOperator op, CombatConduct conduct)
        {
            if (conduct.Kind != CombatConduct.DamageKind ||
                conduct.Element != CombatConduct.ThunderDmg ||
                !Chessboard.IsRandomPass(conduct.GetRate())) return;
            SelfConduct(op, Activity.Intentions.Offensive, Helper.Singular(CombatConduct.InstanceBuff((int)Buff, CardState.Cons.Stunned)));
        }

        public override bool IsMainActionTrigger => true;

        public override bool IsDisableMainAction(ChessOperator op) => IsBuffActive(op);

        public override bool IsDisableCounter(ChessOperator op) => IsBuffActive(op);

    }
    //2 护盾，护盾基本不是护甲削减伤害(完全免伤)，所以不需要buff代理处理逻辑
    //public class ShieldBuff : BuffOperator
    //{
    //    public override CardState.Cons Buff => CardState.Cons.Shield;

    //    public ShieldBuff(ChessboardOperator chessboard) : base(chessboard)
    //    {
    //    }
    //}
    //3 无敌
    public class InvincibleBuff : RoundEndDepleteSelfBuff//回合结束自动去buff
    {
        public override CardState.Cons Buff => CardState.Cons.Invincible;

        public InvincibleBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }
    }
    //4 流血
    public class BleedBuff : RoundEndDepleteSelfBuff
    {
        public override CardState.Cons Buff => CardState.Cons.Bleed;

        public BleedBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override bool IsSufferConductTrigger => true;

        public override void OnSufferConduct(Activity.Intentions activityIntention, ChessOperator op,
            CombatConduct conduct, ActivityResult activityResult)
        {
            if (!IsBuffActive(op) || Damage.GetKind(conduct) != Damage.Kinds.Physical) return;
            conduct.Multiply(1.5f);
        }

    }
    //5 毒
    public class PoisonBuff : RoundEndDepleteSelfBuff
    {
        public override CardState.Cons Buff => CardState.Cons.Poison;
        private int PoisonRate => 20;
        public PoisonBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override bool IsElementTrigger => true;
        public override void OnElementConduct(ChessOperator op, CombatConduct conduct)
        {
            if (conduct.Kind != CombatConduct.DamageKind ||
                conduct.Element != CombatConduct.PoisonDmg ||
                !Chessboard.IsRandomPass(conduct.GetRate())) return;
            SelfConduct(op, Activity.Intentions.Offensive, Helper.Singular(CombatConduct.InstanceBuff((int)Buff, CardState.Cons.Poison)));
        }

        public override bool IsRoundEndTrigger => true;

        public override void RoundEnd(ChessOperator op)
        {
            if (!IsBuffActive(op)) return;
            var damage = Chessboard.GetStatus(op).MaxHp * PoisonRate * 0.01f;
            SelfConduct(op, Activity.Intentions.Inevitable, CombatConduct.InstanceElementDamage((int)Buff, damage, CombatConduct.PoisonDmg));
            SelfConduct(op, Activity.Intentions.ChessboardBuffing, DepleteBuff());
        }
    }
    //6 灼烧
    public class BurnBuff : RoundEndDepleteSelfBuff
    {
        public override CardState.Cons Buff => CardState.Cons.Burn;
        public BurnBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }
        public override bool IsElementTrigger => true;
        public override void OnElementConduct(ChessOperator op, CombatConduct conduct)
        {
            if (conduct.Kind != CombatConduct.DamageKind ||
                conduct.Element != CombatConduct.FireDmg ||
                !Chessboard.IsRandomPass(conduct.GetRate())) return;
            SelfConduct(op, Activity.Intentions.Offensive, Helper.Singular(CombatConduct.InstanceBuff((int)Buff, CardState.Cons.Burn)));
        }

        private int DamageRate => 5;
        public override bool IsRoundEndTrigger => true;

        public override void RoundEnd(ChessOperator op)
        {
            var stacks = Chessboard.GetCondition(op, CardState.Cons.Burn);
            if (stacks <= 0) return;
            var damage = Chessboard.GetStatus(op).MaxHp * DamageRate * stacks * 0.01f;
            SelfConduct(op, Activity.Intentions.ChessboardBuffing, DepleteBuff(stacks));
            SelfConduct(op, Activity.Intentions.Inevitable, CombatConduct.InstanceElementDamage((int)Buff, damage, CombatConduct.FireDmg));
        }
    }

    //7 战意-不属于buff控制器可空类型

    //8 禁锢
    public class Imprisoned : RoundEndDepleteSelfBuff
    {
        public override CardState.Cons Buff => CardState.Cons.Imprisoned;
        public Imprisoned(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override bool IsHeroPerformTrigger => true;
        public override bool IsDisableHeroPerform(HeroOperator op) => IsBuffActive(op);
    }

    //9 怯战
    public class CowardlyBuff : RoundEndDepleteSelfBuff
    {
        public override CardState.Cons Buff => CardState.Cons.Cowardly;
        public CowardlyBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }
        public override bool IsCriticalRatioTrigger => true;

        public override int OnCriticalRatioAddOn(ChessOperator op)
        {
            if (IsBuffActive(op)) return -100;
            return 0;
        }

        public override bool IsRouseRatioTrigger => true;
        public override int OnRouseRatioAddOn(ChessOperator op)
        {
            if (IsBuffActive(op)) return -100;
            return 0;
        }
    }
    //16 卸甲
    public class DisarmedBuff : RoundEndDepleteSelfBuff
    {
        public override CardState.Cons Buff => CardState.Cons.Disarmed;

        public DisarmedBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override bool IsArmorAddOnTrigger => true;
        public override int OnArmorAddOn(float armor, ChessOperator op, CombatConduct conduct)
        {
            if (IsBuffActive(op) &&
                Damage.GetKind(conduct) == Damage.Kinds.Physical)
                return -(int)armor;
            return 0;
        }
    }

    //17 内助
    public class NeiZhuBuff : BuffOperator
    {
        public override CardState.Cons Buff => CardState.Cons.Neizhu;
        public NeiZhuBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override bool IsCriticalRatioTrigger => true;
        public override int OnCriticalRatioAddOn(ChessOperator op)
        {
            if (!IsBuffActive(op)) return 0;
            SelfConduct(op, Activity.Intentions.ChessboardBuffing, DepleteBuff());
            return 100;
        }
    }
    //18 神助
    public class ShenZhuBuff : BuffOperator
    {
        public override CardState.Cons Buff => CardState.Cons.ShenZhu;
        public ShenZhuBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override bool IsRouseRatioTrigger => true;
        public override int OnRouseRatioAddOn(ChessOperator op)
        {
            if (!IsBuffActive(op)) return 0;
            SelfConduct(op, Activity.Intentions.ChessboardBuffing, DepleteBuff());
            return 100;
        }
    }

    //19 抵消盾
    public class ExtendedShieldBuff : BuffOperator
    {
        public override CardState.Cons Buff => CardState.Cons.EaseShield;
        public ExtendedShieldBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override bool IsSufferConductTrigger => true;
        public override void OnSufferConduct(Activity.Intentions activityIntention, ChessOperator op,
            CombatConduct conduct, ActivityResult activityResult)
        {
            if (!IsBuffActive(op)) return;
            var status = Chessboard.GetStatus(op);
            var balance = status.EaseShieldOffset(conduct.Total);
            if (balance <= 0) activityResult.Result = (int)ActivityResult.Types.EaseShield;
            conduct.SetBasic(balance);
        }
    }
    //22 混乱
    public class ConfuseBuff : RoundEndDepleteSelfBuff
    {
        public override CardState.Cons Buff => CardState.Cons.Confuse;
        public ConfuseBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override bool IsHeroPerformTrigger => true;

        public override bool IsDisableHeroPerform(HeroOperator op) => IsBuffActive(op);
    }
    //链环buff管理防守时分享伤害
    public class ChainedBuff : BuffOperator
    {
        private static bool IsChaining { get; set; }
        public override CardState.Cons Buff => CardState.Cons.Chained;

        public ChainedBuff(ChessboardOperator chessboard) : base(chessboard)
        {
        }

        public override bool IsSufferConductTrigger => true;

        public override void OnSufferConduct(Activity.Intentions activityIntention, ChessOperator op,
            CombatConduct conduct, ActivityResult activityResult)
        {

            if (IsChaining) return;
            var activityIntent = activityIntention;
            if (!IsBuffActive(op) ||
                activityIntent == Activity.Intentions.Self ||
                activityIntent == Activity.Intentions.Friendly) return;
            var poses = Chessboard.GetChainedPos(op, ChainSprite.ChainedFilter).ToArray();
            var chainCount = poses.Count();
            if (chainCount == 0) return;
            IsChaining = true;
            conduct.Multiply(1f / chainCount);
            if (conduct.Total > 0)
            {
                foreach (var pos in poses)
                {
                    if (pos.Operator == op)
                        continue;
                    Chessboard.AppendOpActivity(op, pos, Activity.Intentions.Friendly, Helper.Singular(conduct),
                        actId: -1,
                        skill: 2);
                }
            }
            IsChaining = false;
        }
    }
}