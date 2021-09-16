using System;
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
    }
    public abstract class ChessOperator : IChessOperator
    {
        public int InstanceId { get; protected set; }
        public abstract CombatStyle Style { get; }
        public virtual int GetStrength => Style.Strength;
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

        protected abstract int GeneralDamage();
        public virtual void OnRoundStart() {}

        public virtual void OnRoundEnd() {}

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
            if (activity.RePos >= 0) SetPos(activity.RePos);
            //反击逻辑。当对面执行进攻类型的行动将进行，并且是可反击的对象，执行反击
            if (offender!=null && 
                activity.Intent == Activity.Offensive &&
                Chessboard.OnCounterTriggerPass(this,offender)) 
                OnCounter(activity, offender);
            return result;

        }

        //更新棋子代理反馈的结果
        private ActivityResult ProcessActivityResult(Activity activity, IChessOperator offender)
        {
            var result = ActivityResult.Instance(ActivityResult.Types.Suffer);

            //直接击杀
            var kills = activity.Conducts.Where(c => c.Kind == CombatConduct.KillingKind).ToArray();
            if (kills.Length > 0)
            {
                result.Result = (int)ActivityResult.Types.Kill;
                ProceedActivity(activity);
                result.SetStatus(Chessboard.GetStatus(this));
                return result;
            }

            //无敌判断
            if (Chessboard.OnInvincibleTrigger(this))
            {
                result.SetStatus(Chessboard.GetStatus(this));
                result.Result = (int) ActivityResult.Types.Invincible;
                return result;
            }
            //友方执行判定
            if (activity.Intent == Activity.Friendly ||
                activity.Intent == Activity.Self)
                result.Result = (int)ActivityResult.Types.Friendly;

            //友军 与 非棋子 的执行结果
            if (result.Type == ActivityResult.Types.Friendly 
                || offender == null) 
            {
                ProceedActivity(activity);
                result.SetStatus(Chessboard.GetStatus(this));
                return result;
            }

            //执行对方棋子的攻击
            result = Chessboard.OnOffensiveActivity(activity, this ,offender);

            //执行结果判断，伤害或是防护盾执行伤害触发
            if(result.Type == ActivityResult.Types.Suffer||
               result.Type == ActivityResult.Types.EaseShield)
                OnSufferConduct(offender,activity);
            return result;
        }

        public void ProceedActivity(Activity activity)
        {
            foreach (var conduct in activity.Conducts)
            {
                if (Chessboard.GetStatus(this).IsDeath) break;
                UpdateConduct(conduct);
            }
        }

        public abstract int GetDodgeRate();

        /// <summary>
        /// 当被攻击伤害后
        /// </summary>
        /// <param name="offender"></param>
        /// <param name="activity"></param>
        protected virtual void OnSufferConduct(IChessOperator offender, Activity activity){}

        public void SetPos(int pos)
        {
            Chessboard.PosOperator(this, pos);
        }

        protected virtual void OnCounter(Activity activity, IChessOperator offender){}

        /// <summary>
        /// 更新行动，主要是分类调用
        /// 另外如果来自伤害死亡，将触发<see cref="OnDeadTrigger"/>
        /// </summary>
        /// <param name="conduct"></param>
        public void UpdateConduct(CombatConduct conduct)
        {
            var conductTotal = (int) conduct.Total;
            var status = Chessboard.GetStatus(this);
            switch (conduct.Kind)
            {
                case CombatConduct.BuffKind:

                    if (conductTotal == -1) //如果buff -1是清除状态
                    {
                        status.AddBuff(conduct.Element,conductTotal);
                        return;
                    }
                    // 状态执行<see cref="CombatConduct"/>的状态值转换
                    var value = Chessboard.OnBuffingConvert(this, conduct);
                    if (value == 0) return;
                    status.AddBuff(conduct.Element, value);
                    break;
                case CombatConduct.HealKind:
                    // 治疗执行<see cref="CombatConduct"/>的血量增值转换
                    status.AddHp(Chessboard.OnHealConvert(this, conduct));
                    break;
                case CombatConduct.DamageKind:
                    //伤害执行<see cref="CombatConduct"/>的伤害值转换
                    int finalDamage = conductTotal;
                    if (conduct.Element != CombatConduct.FixedDmg) //固定伤害
                    {
                        Chessboard.OnArmorReduction(this, conduct);
                        //自身(武将技)伤害转化
                        finalDamage = OnMilitaryDamageConvert(conduct);
                    }
                    SubtractHp(finalDamage);
                    if(IsAlive) OnAfterSubtractHp(finalDamage, conduct);
                    break;
                case CombatConduct.PlayerDegreeKind://特别类不是棋子维度可执行的
                case CombatConduct.KillingKind://属于直接提取棋子的类型
                    status.Kill();
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            // 扣除血量的方法,血量最低为0
            void SubtractHp(int damage)
            {
                status.SubtractHp(damage);
                if (status.IsDeath)
                    OnDeadTrigger(damage);
            }
        }

        protected virtual void OnAfterSubtractHp(int damage, CombatConduct conduct){}

        /// <summary>
        /// 最后伤害免伤后的伤害转化
        /// </summary>
        /// <param name="conduct"></param>
        /// <returns></returns>
        protected virtual int OnMilitaryDamageConvert(CombatConduct conduct) => (int) conduct.Total;

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

        protected virtual void OnDeadTrigger(int damage)
        {
        }
        public virtual void OnPostingTrigger(IChessPos chessPos){}
        public abstract ChessStatus GenerateStatus();
    }

    public abstract class CardOperator : ChessOperator
    {
        private IChessman chessman;
        private CombatStyle combatStyle;
        private ChessboardOperator chessboard;
        private int pos;

        protected GameCardInfo Info { get; private set; }
        protected override ChessboardOperator Chessboard => chessboard;
        public override CombatStyle Style => combatStyle;
        public override GameCardType CardType => chessman.CardType;
        public override int CardId => chessman.CardId;
        public override bool IsChallenger => chessman.IsPlayer;
        public override int Level => chessman.Level;
        protected override int GeneralDamage() => Style.Strength;

        public virtual void Init(IChessman card, ChessboardOperator chessboardOp)
        {
            InstanceId = card.InstanceId;
            chessman = card;
            combatStyle = card.GetStyle();
            chessboard = chessboardOp;
            pos = card.Pos;
            if (card.CardType != GameCardType.Base)
                Info = card.Info;
        }

        public override ChessStatus GenerateStatus() => ChessStatus.Instance(chessman.HitPoint, chessman.HitPoint, chessman.Pos,
            chessman.Pos,
            new Dictionary<int, int>());

        public override string ToString()
        {
            var sideText = IsChallenger ? "玩家" : "对方";
            if (CardType == GameCardType.Base)
                return $"{sideText} 老巢{CardId}({Level})";
            return $"{sideText} {Info.Name}({CardId})等级({Level})力量({Style.Strength})";
        }
    }
}