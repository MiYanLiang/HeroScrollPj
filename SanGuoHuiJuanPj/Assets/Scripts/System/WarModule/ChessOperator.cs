using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.Contracts;
using System.IO.Pipelines;
using System.Linq;
using CorrelateLib;
using UnityEngine;

namespace Assets.System.WarModule
{
    public interface IChessOperator
    {
        int InstanceId { get; }
        //IChessman Chessman { get; }
        /// <summary>
        /// 棋子攻击方式
        /// </summary>
        AttackStyle Style { get; }
        /// <summary>
        /// 棋子状态
        /// </summary>
        PieceStatus Status { get; }

        bool IsAlive { get; }
        GameCardType CardType { get; }
        int CardId { get; }
        bool IsChallenger { get; }
        int Pos { get; }

        
        //ActivityResult Respond(Activity activity, IChessOperator offender);
        //
        //ActivityResult UpdateConducts(Activity activity, IChessOperator offender);
        //
        //void StartActions();
    }
    public abstract class ChessOperator : IChessOperator
    {
        //protected abstract IChessman Chessman { get; }
        public int InstanceId { get; protected set; }
        public abstract AttackStyle Style { get; }
        public abstract PieceStatus Status { get; }

        public bool IsAlive => !Status.IsDeath;
        public abstract GameCardType CardType { get; }
        public abstract int CardId { get; }
        public abstract bool IsChallenger { get; }
        public int Pos => Status.Pos;
        protected abstract ChessGrid Grid { get; }

        public virtual IEnumerable<KeyValuePair<int, IEnumerable<Activity>>> OnRoundStart() => null;

        public virtual IEnumerable<KeyValuePair<int, IEnumerable<Activity>>> OnRoundEnd() => null;

        /// <summary>
        /// 棋子主进程的行动
        /// </summary>
        /// <returns></returns>
        public abstract void StartActions();

        /// <summary>
        /// 棋子的反馈行动。
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="offender"></param>
        /// <returns></returns>
        public ActivityResult Respond(Activity activity, IChessOperator offender)
        {
            //处理行动
            var result = UpdateConducts(activity, offender);
            if (activity.RePos >= 0) SetPos(activity.RePos);
            //反击逻辑。当对面执行进攻类型的行动将进行，并且是可反击的对象，执行反击
            if (offender!=null && 
                Status.Hp > 0 &&
                activity.Intent == Activity.Offensive && 
                offender.Style.CounterStyle == 0)
            {
                OnCounter(activity, offender);
            }
            return result;

        }

        /// <summary>
        /// 更新多个行动，分别更新棋子状态<see cref="Status"/>
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="offender"></param>
        public ActivityResult UpdateConducts(Activity activity, IChessOperator offender)
        {
            var result = ActivityResult.Instance(ActivityResult.Types.Suffer);

            if (activity.Intent == Activity.Friendly ||
                activity.Intent == Activity.FriendlyAttach ||
                activity.Intent == Activity.Self)
                result.Result = (int)ActivityResult.Types.Friendly;

            if (result.Type == ActivityResult.Types.Friendly) //同势力活动
            {
                foreach (var conduct in activity.Conducts)
                {
                    if (Status.Hp <= 0) break;
                    UpdateConduct(conduct);
                }
                result.SetStatus(Status);
                return result;
            }

            return OnOffensiveActivity(activity, offender, result);
        }

        private ActivityResult OnOffensiveActivity(Activity activity, IChessOperator offender,
            ActivityResult result)
        {
            //位置改变的战斗因子
            var damages = activity.Conducts.Where(c => c.Kind == CombatConduct.DamageKind).ToArray();
            var buffs = activity.Conducts.Where(c => c.Kind == CombatConduct.BuffKind).ToArray();
            var heals = activity.Conducts.Where(c => c.Kind == CombatConduct.HealKind).ToArray();
            if (DodgeOnAttack(offender)) //闪避
                result.Result = (int)ActivityResult.Types.Dodge;
            else if (Status.GetBuff(FightState.Cons.Invincible) > 0) //无敌
                result.Result = (int)ActivityResult.Types.Invincible;
            else if (Status.TryDeplete(FightState.Cons.Shield)) //护盾
                result.Result = (int)ActivityResult.Types.Shield;
            else if (Status.GetBuff(FightState.Cons.ExtendedHp) > 0)
                result.Result = (int)ActivityResult.Types.ExtendedShield;

            /***执行Activities***/
            //治疗
            ProceedConducts(heals);
            //伤害
            if (result.Type == ActivityResult.Types.Suffer ||
                result.Type == ActivityResult.Types.ExtendedShield)
            {
                ProceedConducts(damages); //伤害
                if (activity.Intent != Activity.OffendAttach)
                    OnSufferConduct(offender, damages);
            }

            if (result.Type != ActivityResult.Types.Invincible)
                ProceedConducts(buffs);//添加状态

            result.Status = Status.Clone();
            return result;

            void ProceedConducts(IEnumerable<CombatConduct> conducts)
            {
                foreach (var conduct in conducts)
                {
                    if (Status.IsDeath) break;
                    UpdateConduct(conduct);
                }
            }
        }

        /// <summary>
        /// 当被攻击伤害后
        /// </summary>
        /// <param name="iChessOperator"></param>
        /// <param name="damages"></param>
        protected virtual void OnSufferConduct(IChessOperator iChessOperator, CombatConduct[] damages){}

        public void SetPos(int pos)
        {
            if (Pos >= 0) Grid.Remove(this);
            //var pPos = Grid.GetChessPos(Chessman);
            //pPos?.RemoveOperator();
            var chess = Grid.Replace(pos, this);
            if(chess!=null)
                throw new InvalidOperationException(
                    $"Pos({pos}) has [{chess.CardId}({chess.CardType})] exist!");
            //var cPos = Grid.GetChessPos(pos, Chessman.IsPlayer);
            //if (cPos == null) throw new NullReferenceException($"Pos({pos}) is null!");
            //if (cPos.Operator != null)
            //cPos.SetPos(this);
            Status.SetPos(pos);
        }

        protected virtual void OnCounter(Activity activity, IChessOperator offender){}
        /// <summary>
        /// 更新行动，主要是分类调用
        /// 更新状态:<see cref="UpdateBuffs"/>，
        /// 更新伤害<see cref="OnDamageConvert"/>，
        /// 更新治疗<see cref="OnHealConvert"/>，
        /// 来更新状态<see cref="Status"/>。
        /// 另外如果来自伤害死亡，将触发<see cref="OnDeadTrigger"/>
        /// </summary>
        /// <param name="conduct"></param>
        protected void UpdateConduct(CombatConduct conduct)
        {
            switch (conduct.Kind)
            {
                case CombatConduct.BuffKind:
                    UpdateBuffs(conduct);
                    break;
                case CombatConduct.DamageKind:
                    SubtractHp(conduct);
                    if (Status.Hp <= 0) 
                        OnDeadTrigger(conduct);
                    break;
                case CombatConduct.HealKind:
                    Healing(conduct);
                    break;
                case CombatConduct.PlayerDegreeKind://特别类不是棋子维度可执行的
                case CombatConduct.KillingKind://属于直接提取棋子的类型
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected abstract bool DodgeOnAttack(IChessOperator offender);

        protected virtual void OnDeadTrigger(CombatConduct conduct)
        {
        }

        /// <summary>
        /// 治疗执行因子<see cref="CombatConduct"/>的血量增值转换
        /// </summary>
        /// <param name="conduct"></param>
        /// <returns></returns>
        protected abstract int OnHealConvert(CombatConduct conduct);

        /// <summary>
        /// 伤害执行因子<see cref="CombatConduct"/>的伤害值转换
        /// </summary>
        /// <param name="conduct"></param>
        /// <returns></returns>
        protected abstract int OnDamageConvert(CombatConduct conduct);

        /// <summary>
        /// 状态执行因子<see cref="CombatConduct"/>的状态值转换
        /// </summary>
        /// <param name="conduct"></param>
        /// <returns></returns>
        protected abstract int OnBuffingConvert(CombatConduct conduct);
        // 加血量的方法。血量不会超过最大数
        private void Healing(CombatConduct conduct)
        {
            Status.Hp += OnHealConvert(conduct);
            if (Status.Hp > Status.MaxHp)
                Status.Hp = Status.MaxHp;
        }
        // 扣除血量的方法,血量最低为0
        private void SubtractHp(CombatConduct conduct)
        {
            var damage = OnDamageConvert(conduct);
            Status.SubtractHp(damage);
            OnAfterSubtractHp(damage, conduct);
            if (Status.Hp < 0) Status.Hp = 0;
        }

        /// <summary>
        /// 当被扣血伤害了之后
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="conduct"></param>
        protected virtual void OnAfterSubtractHp(int damage, CombatConduct conduct)
        {
        }

        // 状态添加或删减。如果状态值小或等于0，将直接删除。
        private void UpdateBuffs(CombatConduct conduct)
        {
            if ((int) conduct.Total == -1) //如果
            {
                Status.ClearBuff(conduct.Element);
                return;
            }
            var value = OnBuffingConvert(conduct);
            if (value == 0) return;
            Status.AddBuff(conduct.Element, value);
        }
    }

    public abstract class CardOperator : ChessOperator
    {
        private IChessman chessman;
        private AttackStyle attackStyle;
        private PieceStatus dynamicStatus;
        private IChessboardOperator chessboard;

        protected IChessboardOperator Chessboard => chessboard;
        protected GameCardInfo Info { get; private set; }
        //public override IChessman Chessman => chessman;
        public override AttackStyle Style => attackStyle;
        public override PieceStatus Status => dynamicStatus;
        public override GameCardType CardType => chessman.CardType;
        public override int CardId => chessman.CardId;
        public override bool IsChallenger => chessman.IsPlayer;

        protected override ChessGrid Grid => chessboard.Grid;

        public virtual void Init(IChessman card, IChessboardOperator chessboardOp)
        {
            InstanceId = card.InstanceId;
            chessman = card;
            attackStyle = card.Style;
            chessboard = chessboardOp;
            dynamicStatus = PieceStatus.Instance(card.HitPoint, card.HitPoint, card.Pos, new Dictionary<int, int>());
            if (card.CardType != GameCardType.Base)
                Info = card.Info;
        }

        /// <summary>
        /// 单体 一列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        protected static T[] Singular<T>(T t) => new[] {t};
    }
}