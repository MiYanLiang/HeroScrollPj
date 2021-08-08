using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.Contracts;
using System.IO.Pipelines;
using System.Linq;
using UnityEngine;

namespace Assets.System.WarModule
{
    public interface IChessOperator<TChess> where TChess : class, IChessman, new()
    {
        IChessman Chessman { get; }
        /// <summary>
        /// 棋子攻击方式
        /// </summary>
        AttackStyle Style { get; }
        /// <summary>
        /// 棋子状态
        /// </summary>
        PieceStatus Status { get; }
        /// <summary>
        /// 棋子的反馈行动。
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="offender"></param>
        /// <returns></returns>
        ActivityResult Respond(Activity activity, IChessOperator<TChess> offender);
        /// <summary>
        /// 更新状态
        /// </summary>
        ActivityResult UpdateConducts(Activity activity, IChessOperator<TChess> offender);
        /// <summary>
        /// 棋子主进程的行动
        /// </summary>
        /// <param name="grid"></param>
        /// <returns></returns>
        void StartActions();
    }
    public abstract class ChessOperator : IChessOperator<FightCardData>
    {
        public IChessman Chessman => Card;
        public abstract AttackStyle Style { get; }
        /// <summary>
        /// 棋子数据
        /// </summary>
        public abstract FightCardData Card { get; }
        /// <summary>
        /// 棋子时事状态
        /// </summary>
        public abstract PieceStatus Status { get; }
        protected abstract ChessGrid<FightCardData> Grid { get; }

        public virtual IEnumerable<KeyValuePair<int, IEnumerable<Activity>>> OnRoundStart() => null;

        public virtual IEnumerable<KeyValuePair<int, IEnumerable<Activity>>> OnRoundEnd() => null;

        public abstract void StartActions();

        public ActivityResult Respond(Activity activity, IChessOperator<FightCardData> offender)
        {
            //处理行动
            var result = UpdateConducts(activity,offender);
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
        public ActivityResult UpdateConducts(Activity activity, IChessOperator<FightCardData> offender)
        {
            //位置改变的战斗因子
            var rePoses = activity.Conducts.Where(f => f.Kind == CombatConduct.RePosKind).ToArray();
            var damages = activity.Conducts.Where(c => c.Kind == CombatConduct.DamageKind).ToArray();
            var buffs = activity.Conducts.Where(c => c.Kind == CombatConduct.BuffKind).ToArray();
            var heals = activity.Conducts.Where(c => c.Kind == CombatConduct.HealKind).ToArray();
            var result = new ActivityResult {Result = ActivityResult.Suffer};

            if (activity.Intent == Activity.Friendly ||
                activity.Intent == Activity.FriendlyTrigger ||
                activity.Intent == Activity.Self)
                result.Result = ActivityResult.Friendly;

            if (result.Result == ActivityResult.Friendly) //同势力活动
            {
                foreach (var conduct in activity.Conducts)
                {
                    if (Status.Hp <= 0) break;
                    UpdateConduct(conduct);
                }
                result.Status = Status;
                return result;
            }

            if (DodgeOnAttack(offender)) //闪避
                result.Result = ActivityResult.Dodge;
            else if (Status.GetBuff(FightState.Cons.Invincible) > 0) //无敌
                result.Result = ActivityResult.Invincible;
            else if (Status.TryDeplete(FightState.Cons.Shield)) //护盾
                result.Result = ActivityResult.Shield;
            else if (Status.GetBuff(FightState.Cons.ExtendedHp) > 0)
                result.Result = ActivityResult.ExtendedShield;
        
            /***执行Activities***/
            //治疗
            ProceedConducts(heals);
            //伤害
            if (result.Result == ActivityResult.Suffer ||
                result.Result == ActivityResult.ExtendedShield)
            {
                ProceedConducts(damages); //伤害
                if (activity.Intent != Activity.OffendTrigger)
                    OnSufferConduct(offender, damages);
            }

            if (result.Result != ActivityResult.Invincible) 
                ProceedConducts(buffs);//添加状态
            //调位
            ProceedConducts(rePoses);
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
        protected virtual void OnSufferConduct(IChessOperator<FightCardData> iChessOperator, CombatConduct[] damages){}

        public bool SetPos(int pos)
        {
            var cPos = Grid.GetChessPos(pos, Card.isPlayerCard);
            if (cPos == null || cPos.Chessman != null) return false;
            Status.Pos = pos;
            cPos.SetPos(Card);
            return true;
        }

        protected virtual void OnCounter(Activity activity, IChessOperator<FightCardData> offender){}
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
                case CombatConduct.RePosKind:
                    RePosition(conduct);
                    break;
                case CombatConduct.PlayerDegreeKind://特别类不是棋子维度可执行的
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 执行移位命令，主要是如果目标位置上不是空的，将不执行
        /// </summary>
        /// <param name="conduct"></param>
        protected virtual void RePosition(CombatConduct conduct)
        {
            if (conduct.Kind != CombatConduct.RePosKind)
                throw new InvalidOperationException($"Expected Conduct kind is {CombatConduct.RePosKind} != {conduct.Kind}!");
            var origin = Grid.GetChessPos(Card.Pos, Card.IsPlayer);
            var targetPos = Grid.GetChessPos(conduct.Element, Card.IsPlayer);
            if(targetPos.Chessman!=null)return;//如果目标位置有卡牌，不执行换位
            origin.RemovePos();
            targetPos.SetPos(Card);
        }

        protected abstract bool DodgeOnAttack(IChessOperator<FightCardData> offender);

        protected virtual IList<Activity> OnDeadTrigger(CombatConduct conduct) => null;

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
            Status.AddBuff(conduct.Element, OnBuffingConvert(conduct));
        }
    }

    public abstract class ChessmanOperator : ChessOperator
    {
        private FightCardData chessman;
        private AttackStyle attackStyle;
        private PieceStatus dynamicStatus;
        private IChessboardOperator<FightCardData> chessboard;

        protected IChessboardOperator<FightCardData> Chessboard => chessboard;
        protected GameCardInfo Info { get; private set; }
        public override FightCardData Card => chessman;
        public override AttackStyle Style => attackStyle;
        public override PieceStatus Status => dynamicStatus;

        protected override ChessGrid<FightCardData> Grid => chessboard.Grid;

        public virtual void Init(FightCardData card, AttackStyle style, IChessboardOperator<FightCardData> chessboardOp)
        {
            chessman = card;
            attackStyle = style;
            chessboard = chessboardOp;
            dynamicStatus = PieceStatus.Instance(card.Hp.Value, card.Hp.Max, card.Pos,
                card.fightState.Data.ToDictionary(s => s.Key, s => s.Value));
            Info = card.cardObj.CardInfo;
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