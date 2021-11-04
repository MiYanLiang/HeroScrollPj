﻿using System;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;

namespace Assets.System.WarModule
{
    public interface IChessOperator
    {
        int InstanceId { get; }
        //IChessman Chessman { get; }
        /// <summary>
        /// 棋子攻击方式
        /// </summary>
        CombatStyle Style { get; }
        /// <summary>
        /// 棋子状态
        /// </summary>
        //PieceStatus Status { get; }

        GameCardType CardType { get; }
        int CardId { get; }
        int Level { get; }
        bool IsChallenger { get; }
        bool IsMeleeHero { get; }
        bool IsRangeHero { get; }
        
        bool IsAlive { get; }
        int OnSpritesValueConvert(PosSprite[] sprites, CardState.Cons con);
    }
    public abstract class ChessOperator : IChessOperator
    {
        public int InstanceId { get; protected set; }
        public abstract CombatStyle Style { get; }
        public virtual int Damage => Style.Strength;
        public bool IsAlive => !Chessboard.GetStatus(this).IsDeath;
        public abstract GameCardType CardType { get; }
        public abstract int CardId { get; }
        public abstract int Level { get; }
        public abstract bool IsChallenger { get; }
        public bool IsMeleeHero => Style!=null && Style.ArmedType >= 0 && Style.Type == CombatStyle.Types.Melee;       
        public bool IsRangeHero => Style!=null && Style.ArmedType >= 0 && Style.Type == CombatStyle.Types.Range;

        protected abstract ChessboardOperator Chessboard { get; }
        /// <summary>
        /// 无视盾牌单位
        /// </summary>
        public virtual bool IsIgnoreShieldUnit { get; set; }

        protected abstract int StateDamage();
        public virtual void OnRoundStart() {}

        public virtual void OnRoundEnd() {}

        /// <summary>
        /// 与己方全局地块精灵交互值
        /// </summary>
        /// <param name="sprites"></param>
        /// <param name="cons"></param>
        /// <returns></returns>
        public virtual int OnSpritesValueConvert(PosSprite[] sprites, CardState.Cons cons) => 0;

        /// <summary>
        /// 棋子主进程的行动
        /// </summary>
        /// <returns></returns>
        public void MainActivity()
        {
            if(!Chessboard.OnMainProcessAvailable(this))return;
            StartActions();
        }

        protected virtual void StartActions(){}

        /// <summary>
        /// 棋子的反馈行动。
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="offender"></param>
        /// <returns></returns>
        public ActivityResult Respond(Activity activity, ChessOperator offender)
        {
            //处理行动
            var result = ProcessActivityResult(activity, offender);
            if (result.Type == ActivityResult.Types.Suffer && activity.RePos >= 0)
                SetPos(activity.RePos);
            else activity.RePos = -1;
            //反击逻辑。当对面执行进攻类型的行动将进行，并且是可反击的对象，执行反击
            if (offender != null &&
                Chessboard.IsMajorTarget(this) &&
                !Chessboard.GetStatus(offender).IsDeath &&
                activity.Intention == Activity.Intentions.Offensive &&
                Chessboard.OnCounterTriggerPass(this, offender))
                OnCounter(activity, offender);
            return result;

        }

        //更新棋子代理反馈的结果
        private ActivityResult ProcessActivityResult(Activity activity, ChessOperator offender)
        {
            //棋盘执行
            var result = ActivityResult.Instance(ActivityResult.Types.Suffer);
            if (activity.Intention == Activity.Intentions.ChessboardBuffing)
            {
                foreach (var conduct in activity.Conducts)
                    UpdateConduct(offender, activity.Intention, conduct);
                result.Status = Chessboard.GetStatus(this);
                return result;
            }

            //直接击杀
            var kills = activity.Conducts.Where(c => c.Kind == CombatConduct.KillingKind).ToArray();
            if (kills.Length > 0)
            {
                if (kills.Any(kill => kill.Rate == 0 || Chessboard.IsRandomPass(kill.Rate)))
                {
                    result.Result = activity.Intention == Activity.Intentions.Self
                        ? (int)ActivityResult.Types.Suicide
                        : (int)ActivityResult.Types.Kill;
                    ProceedActivity(offender, activity, result.Type);
                    result.Status = Chessboard.GetStatus(this);
                    return result;
                }

                result.Result = (int)ActivityResult.Types.Suffer;
            }

            //无敌判断
            if (Chessboard.OnInvincibleTrigger(this))
            {
                result.Status = Chessboard.GetStatus(this);
                result.Result = (int)ActivityResult.Types.Invincible;
                return result;
            }

            //友方执行判定
            if (activity.Intention == Activity.Intentions.Friendly ||
                activity.Intention == Activity.Intentions.Self)
            {
                if (activity.Conducts.Any(c => c.Kind == CombatConduct.DamageKind || 
                                               c.Kind == CombatConduct.ElementDamageKind))
                    result.Result = (int)ActivityResult.Types.Suffer;
                else if(activity.Conducts.Any(c => c.Kind == CombatConduct.HealKind))
                        result.Result = (int)ActivityResult.Types.Heal;
                else if (activity.Conducts.Any(c => c.Kind == CombatConduct.BuffKind))
                    result.Result = (int)ActivityResult.Types.Assist;
            }

            //友军 与 非棋子 的执行结果
            if (result.Type == ActivityResult.Types.Assist || 
                result.Type == ActivityResult.Types.Heal)
            {
                ProceedActivity(offender, activity, result.Type);
                result.Status = Chessboard.GetStatus(this);
            }
            else
            {
                //执行对方棋子的攻击
                result = Chessboard.OnOffensiveActivity(activity, this, offender);
                OnSuffering(activity, offender);
            }

            return result;
        }

        public void ProceedActivity(ChessOperator offender,Activity activity, ActivityResult.Types result)
        {
            var conducts = activity.Conducts;
            if (result == ActivityResult.Types.Shield)
                conducts = conducts.Where(c =>
                    !(c.Kind == CombatConduct.DamageKind && WarModule.Damage.GetKind(c) == WarModule.Damage.Kinds.Physical)).ToList();
            foreach (var conduct in conducts)
            {
                if (Chessboard.GetStatus(this).IsDeath) break;
                UpdateConduct(offender, activity.Intention, conduct);
            }
        }

        public abstract int GetDodgeRate();

        
        private void OnSuffering(Activity activity, ChessOperator offender)
        {
            OnSufferConduct(activity, offender);
            if (offender == null || //没对象
                !offender.IsAlive || //对象已死亡
                offender.Style.ArmedType < 0 || //对象不是武将
                offender.Style.Type == CombatStyle.Types.Range || //对方是远程
                activity.Intention != Activity.Intentions.Offensive) //对象并非主动攻击标签
                return;
            OnReflectingConduct(activity, offender);
        }

        /// <summary>
        /// 反伤逻辑。预先判断死亡，非武将，远程，或是非<see cref="Activity.Intentions.Offensive"/>标签，不会触发
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="offender"></param>
        protected virtual void OnReflectingConduct(Activity activity, ChessOperator offender)
        {
        }

        /// <summary>
        /// 当被攻击伤害后
        /// </summary>
        /// <param name="offender"></param>
        /// <param name="activity"></param>
        protected virtual void OnSufferConduct(Activity activity, IChessOperator offender = null){}

        public void SetPos(int pos)
        {
            Chessboard.PosOperator(this, pos);
        }

        protected virtual void OnCounter(Activity activity, IChessOperator offender){}

        /// <summary>
        /// 更新行动，主要是分类调用
        /// 另外如果来自伤害死亡，将触发<see cref="OnDeadTrigger"/>
        /// </summary>
        /// <param name="offender"></param>
        /// <param name="activityIntent"></param>
        /// <param name="conduct"></param>
        /// <param name="chessboardInvocation"></param>
        private void UpdateConduct(ChessOperator offender,Activity.Intentions activityIntent,CombatConduct conduct,bool chessboardInvocation = false)
        {
            var conductTotal = (int) conduct.Total;
            var status = Chessboard.GetStatus(this);
            switch (conduct.Kind)
            {
                case CombatConduct.BuffKind:
                {
                    if (!Chessboard.IsRandomPass(conduct.GetRate())) return;//如果状态有几率，判定不成功
                    if (conductTotal == -1 || //如果是清除状态不执行转换
                        chessboardInvocation) //如果是棋盘活动，直接执行
                    {
                        status.AddBuff(conduct.Element, conductTotal);
                        return;
                    }

                    // 状态执行<see cref="CombatConduct"/>的状态值转换
                    var value = Chessboard.OnBuffingConvert(this, conduct);
                    if (value == 0) return;
                    status.AddBuff(conduct.Element, value);
                    return;
                }
                case CombatConduct.HealKind:
                    // 治疗执行<see cref="CombatConduct"/>的血量增值转换
                    status.OnHeal(Chessboard.OnHealConvert(this, conduct));
                    return;
                case CombatConduct.DamageKind:
                case CombatConduct.ElementDamageKind:
                {
                    if (!chessboardInvocation)
                    {
                        if (conduct.Element != CombatConduct.FixedDmg) //固定伤害
                        {
                            Chessboard.OnCombatMiddlewareConduct(this, activityIntent, conduct);
                            if (conduct.Element == CombatConduct.MechanicalDmg && Style.ArmedType < 0)
                                conduct.Multiply(2);

                            //自身(武将技)伤害转化
                            OnMilitaryDamageConvert(conduct);
                        }

                        SubtractHp((int)conduct.Total);
                        if (IsAlive) OnAfterSubtractHp(conduct);
                    }
                    else
                    {
                        SubtractHp((int)conduct.Total);
                    }

                    return;
                }
                case CombatConduct.PlayerScopeKind://特别类不是棋子维度可执行的
                case CombatConduct.KillingKind://属于直接提取棋子的类型
                    status.Kill();
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(conduct.Kind), conduct.Kind.ToString());
            }
            // 扣除血量的方法,血量最低为0
            void SubtractHp(int damage)
            {
                status.SubtractHp(damage);
                if (status.IsDeath)
                    OnDeadTrigger(offender, damage);
            }
        }

        protected virtual void OnAfterSubtractHp(CombatConduct conduct){}

        /// <summary>
        /// 最后伤害免伤后的伤害转化
        /// </summary>
        /// <param name="conduct"></param>
        /// <returns></returns>
        protected virtual void OnMilitaryDamageConvert(CombatConduct conduct)
        {
        }

        /// <summary>
        /// 法术免伤
        /// </summary>
        /// <returns></returns>
        public virtual int GetMagicArmor() => 0;

        /// <summary>
        /// 物理免伤
        /// </summary>
        /// <returns></returns>
        public virtual int GetPhysicArmor() => 0;


        protected virtual int OnArmorReduction(CombatConduct damage) => (int) damage.Total;

        protected virtual void OnDeadTrigger(ChessOperator offender, int damage)
        {
        }

        public virtual void OnPostingTrigger(IChessPos chessPos){}
        public abstract ChessStatus GenerateStatus();

        ///// <summary>
        ///// 棋子放置的预设活动，严禁任何攻击活动
        ///// </summary>
        //public virtual void OnPlaceInvocation() { }

        public virtual void OnSomebodyDie(ChessOperator death){}

        public virtual bool IsNotCounterAble(ChessOperator op) => false;

    }

    public abstract class CardOperator : ChessOperator
    {
        private IChessman chessman;
        private CombatStyle combatStyle;
        private ChessboardOperator chessboard;
        public string Name { get; private set; }
        protected override ChessboardOperator Chessboard => chessboard;
        public override CombatStyle Style => combatStyle;
        public override GameCardType CardType => chessman.CardType;
        public override int CardId => chessman.CardId;
        public override bool IsChallenger => chessman.IsPlayer;
        public override int Level => chessman.Level;
        protected override int StateDamage() => Style.Strength;

        public virtual void Init(IChessman card, ChessboardOperator chessboardOp)
        {
            Name = GameCardInfo.GetInfo(card).Name;
            InstanceId = card.InstanceId;
            chessman = card;
            combatStyle = card.GetStyle();
            chessboard = chessboardOp;
        }

        public override ChessStatus GenerateStatus() => ChessStatus.Instance(chessman.HitPoint, chessman.MaxHitPoint,
            chessman.Pos, new Dictionary<int, int>(), new List<int>(), 0);

        public override string ToString()
        {
            var sideText = IsChallenger ? "玩家" : "对方";
            if (CardType == GameCardType.Base)
                return $"{sideText} 老巢{CardId}({Level})";
            return $"{sideText} {Name}({CardId})等级({Level})伤害({Damage})";
        }

        /// <summary>
        /// 基础伤害
        /// </summary>
        public override int Damage => Style.Strength;
    }
}