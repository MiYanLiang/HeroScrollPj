using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.System.WarModule
{
    public record ChessRoundRecord
    {
        public int Round { get; set; } 
        public List<ActivityRecord> ActivityRecords { get; set; } = new List<ActivityRecord>();
        public int Index { get; set; } = -1;
        public ActivityRecord CurrentActivity => ActivityRecords[Index];
        public Dictionary<int, ChessStatus> StatusMap { get; set; }
        public void SetRound(ChessRound round)
        {
            Round = round.InstanceId;
        }

        public override string ToString() => $"Round({Round}),MaxIndex({Index}),Count({ActivityRecords.Count})";

        public void AddChessmanActivity(int instanceId, bool isChallenger) =>
            AddActivity(instanceId, isChallenger, ActivityRecord.Types.Chessman);

        public void AddSummaryActivity() => 
            AddActivity(-1, false, ActivityRecord.Types.Summary);

        private void AddActivity(int instanceId, bool isChallenger,ActivityRecord.Types type)
        {
            if (Index >= 0 && !CurrentActivity.Data.Any())//自动删除上一个无效的记录
            {
                ActivityRecords.RemoveAt(Index);
                Index = ActivityRecords.Count - 1;
            }
            ActivityRecords.Add(new ActivityRecord(instanceId, isChallenger ? 1 : 0, type));
            Index++;
        }

        public void AddJiBanActivity(int jiBanId, bool isChallenger, bool isBuff)
        {
            if (((CurrentActivity.Type == ActivityRecord.Types.JiBanBuff && isBuff) ||
                 (CurrentActivity.Type == ActivityRecord.Types.JiBanAttack && !isBuff)) &&
                CurrentActivity.IsChallenger == isChallenger &&
                CurrentActivity.InstanceId == jiBanId)
                return;
            AddActivity(jiBanId, isChallenger,
                isBuff ? ActivityRecord.Types.JiBanBuff : ActivityRecord.Types.JiBanAttack);
        }

        public void AddFragment(CardFragment fragment) => CurrentActivity.AddFragment(fragment);

        public void AddFragment(ChessboardFragment fragment)
        {
            var list = CurrentActivity.Data
                .Where(f => f.Type == ActivityFragment.FragmentTypes.Chessboard)
                .Cast<ChessboardFragment>().ToArray();
            if (list.Length > 0 &&
                list.Any(f => f.InstanceId == fragment.InstanceId &&
                              f.ActId == fragment.ActId &&
                              f.Type == fragment.Type &&
                              f.IsChallenger == fragment.IsChallenger &&
                              f.TargetId == fragment.TargetId &&
                              f.Pos == fragment.Pos &&
                              f.StandPoint == fragment.StandPoint &&
                              f.Skill == fragment.Skill &&
                              f.Value == fragment.Value))
                return;
            CurrentActivity.AddFragment(fragment);
        }

        public ActivityFragment GetLastCardFragment(int instanceId) =>
            CurrentActivity.Data.LastOrDefault(c =>
                c.Type == ActivityFragment.FragmentTypes.Chessman && c.InstanceId == instanceId);
        public ActivityFragment GetLastCardFragment() =>
            CurrentActivity.Data.LastOrDefault(c => c.Type == ActivityFragment.FragmentTypes.Chessman);

        public int GetActId() => CurrentActivity.Index < 0 ? 0 : CurrentActivity.CurrentFragment.ActId;

        public void AddChessmenStatus(Dictionary<int, ChessStatus> statusMap) => StatusMap = statusMap;
    }

    public record ActivityRecord
    {
        public ActivityRecord(int instanceId, int standPoint, Types type)
        {
            InstanceId = instanceId;
            StandPoint = standPoint;
            Type = type;
        }

        public enum Types
        {
            /// <summary>
            /// 回合开始或结束的结算
            /// </summary>
            Summary,
            Chessman,
            JiBanBuff,
            JiBanAttack
        }

        public List<ActivityFragment> Data { get; set; } = new List<ActivityFragment>();

        /// <summary>
        /// -1 = Chessboard, 正数为卡牌
        /// </summary>
        public int InstanceId { get; }

        /// <summary>
        /// Challenger = 1; else = opponent
        /// </summary>
        public int StandPoint { get; set; }
        public int Index { get; set; } = -1;
        public bool IsChallenger => StandPoint == 1;

        public Types Type { get; }
        public ActivityFragment CurrentFragment
        {
            get
            {
                try
                {
                    return Data[Index];
                }
                catch
                {
                    throw new ArgumentOutOfRangeException(nameof(Index), Index.ToString());
                }
            }
        }

        public override string ToString() =>
            $"{Type},Instance({InstanceId}),Stand({StandPoint}),Count({Data.Count})";

        public void AddFragment(ActivityFragment fragment)
        {
            Data.Add(fragment);
            Index = Data.Count - 1;
        }
    }

    public abstract record ActivityFragment
    {
        protected ActivityFragment(FragmentTypes type)
        {
            Type = type;
        }

        protected static int ConvertChallenger(bool isChallenger) => isChallenger ? 1 : 0;
        public enum FragmentTypes
        {
            Chessman = 0,
            Chessboard = 1,
        }
        public FragmentTypes Type { get; set; }
        /// <summary>
        /// <see cref="FragmentTypes.Chessboard"/>类型忽视,
        /// <see cref="FragmentTypes.Chessman"/>的行动Id,必须正数，每个卡牌行动里不可重复，
        /// </summary>
        public int ActId { get; set; }
        public int InstanceId { get; set; }

    }

    public record ChessboardFragment : ActivityFragment
    {
        public static ChessboardFragment Instance(int instanceId, Kinds kind, 
            int skill, int actId, int pos, int targetId, int value, bool isChallenger) =>
            new(instanceId, kind: kind, skill: skill, actId: actId, pos: pos, 
                targetId: targetId, value: value, isChallenger: isChallenger);
        public ChessboardFragment(int instanceId,Kinds kind,int skill,int actId, int pos, int targetId, int value, bool isChallenger) : base(FragmentTypes.Chessboard)
        {
            InstanceId = instanceId;
            Kind = kind;
            Pos = pos;
            TargetId = targetId;
            Value = value;
            ActId = actId;
            StandPoint = isChallenger ? 1 : 0;
            Skill = skill;
        }

        public enum Kinds
        {
            /// <summary>
            /// 状态动画
            /// </summary>
            Sprite,
            /// <summary>
            /// 金币
            /// </summary>
            Gold,
            /// <summary>
            /// 宝箱
            /// </summary>
            Chest
        }

        public Kinds Kind { get; set; }
        public List<RespondAct> Responds { get; set; } = new List<RespondAct>();
        /// <summary>
        /// 发生的棋格
        /// </summary>
        public int Pos { get; set; }
        /// <summary>
        /// 1 = challenger, else = opponent
        /// </summary>
        public int StandPoint { get; set; }
        public int Skill { get; set; }
        public bool IsChallenger => StandPoint == 1;
        /// <summary>
        /// 状态精灵Id，宝箱Id,Sprite TypeId
        /// </summary>
        public int TargetId { get; set; }

        /// <summary>
        /// 金币数量,精灵行动值(1=添加,0=删除)
        /// </summary>
        public int Value { get; set; }
        public override string ToString()
        {
            var tarText = string.Empty;
            switch (Kind)
            {
                case Kinds.Sprite:
                    tarText = $"Sp({TargetId})";
                    break;
                case Kinds.Gold:
                    tarText = $"Gold({TargetId})";
                    break;
                case Kinds.Chest:
                    tarText = $"Chess({TargetId})";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return $"{Type}.{Kind},Pos({Pos}),Act[{ActId}]," +
                   tarText +
                   $",Val({Value})Resp({Responds.Count})";
        }

        public void AddRespond(int exeId, int targetId, int skill, RespondAct.Responds kind, int pop, int finalPos,
            ChessStatus status) =>
            Responds.Insert(0,
                new RespondAct(exeId, targetId, skill, kind, pop, finalPos, status, RespondAct.Modes.Attach));
    }

    public record CardFragment : ActivityFragment
    {
        public static CardFragment Instance(int instanceId, int actId) => new(instanceId, actId);

        public CardFragment(int instanceId, int actId) : base(FragmentTypes.Chessman)
        {
            InstanceId = instanceId;
            ActId = actId;
        }

        /// <summary>
        /// 每次攻击
        /// </summary>
        public List<ExecuteAct> Executes { get; set; } = new List<ExecuteAct>();
        public ExecuteAct Counter { get; set; }
        public override string ToString() => $"{InstanceId}.{Type},Act[{ActId}]Atts({Executes.Count})";
        public ExecuteAct GetCounter(Damage.Types damageType) => Counter ??= new ExecuteAct(damageType,ExecuteAct.Conducts.Chessman);

        public ExecuteAct GetOrInstanceAttack(Damage.Types damageType, ExecuteAct.Conducts conduct)
        {
            var at = Executes.FirstOrDefault(e => e.Conduct == conduct);
            switch (at)
            {
                case null:
                    at = new ExecuteAct(damageType, conduct);
                    Executes.Add(at);
                    break;
            }
            return at;
        }
    }

    public record ExecuteAct 
    {
        public enum Conducts
        {
            Chessman,
            /// <summary>
            /// 毒伤害
            /// </summary>
            Poison,
            /// <summary>
            /// 灼烧伤害
            /// </summary>
            Burn,
            /// <summary>
            /// 连环分担伤害
            /// </summary>
            Chained
        }
        public Conducts Conduct { get; set; }
        public List<RespondAct> Responds { get; set; } = new List<RespondAct>();
        public Damage.Types DamageType { get; set; }
        public ExecuteAct(Damage.Types damageType, Conducts conduct)
        {
            DamageType = damageType;
            Conduct = conduct;
        }

        public override string ToString() => $"{DamageType},Resp({Responds.Count})";

        public void AddRespond(int exeId, int targetId, RespondAct.Modes mode, int skill, RespondAct.Responds kind,
            int pop, int finalPos,
            ChessStatus status) =>
            Responds.Insert(0, new RespondAct(exeId, targetId, skill, kind, pop, finalPos, status, mode));
    }
    public record RespondAct 
    {
        public enum Responds
        {
            None,
            /// <summary>
            /// 赋buff
            /// </summary>
            Buffing,
            /// <summary>
            /// 受击
            /// </summary>
            Suffer,
            /// <summary>
            /// 治疗
            /// </summary>
            Heal,
            /// <summary>
            /// 闪避
            /// </summary>
            Dodge,
            /// <summary>
            /// 防御
            /// </summary>
            Shield,
            /// <summary>
            /// 抵消
            /// </summary>
            Ease,
            /// <summary>
            /// 绝杀
            /// </summary>
            Kill,
            /// <summary>
            /// 自杀
            /// </summary>
            Suicide,
            /// <summary>
            /// 无敌
            /// </summary>
            Invincible
        }

        public enum Modes
        {
            Major,
            Attach
        }
        /// <summary>
        /// 执行者Id,一般都是卡牌InstanceId，
        /// </summary>
        public int ExeId { get; set; }
        public int TargetId { get; set; }
        public Responds Kind { get; set; }
        public Modes Mode { get; set; }
        public int Pop { get; set; }
        /// <summary>
        /// 最终棋格
        /// </summary>
        public int FinalPos { get; set; }
        /// <summary>
        /// 技能值，如果是<see cref="ExecuteAct.Conducts.BuffDamage"/>伤害,它代表buff类型
        /// </summary>
        public int Skill { get; set; }
        public ChessStatus Status { get; set; }

        public RespondAct(int exeId, int targetId, int skill, Responds kind, int pop, int finalPos, ChessStatus status, Modes mode)
        {
            ExeId = exeId;
            TargetId = targetId;
            Kind = kind;
            Pop = pop;
            FinalPos = finalPos;
            Status = status;
            Mode = mode;
            Skill = skill;
        }

        public override string ToString() => $"{TargetId}.{Kind},Pos({FinalPos}){Status},Pop({Pop})";
    }
}