﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;

namespace Assets.System.WarModule
{
    public record ChessRoundRecord
    {
        public int Round { get; set; } 
        public List<ActivityRecord> ActivityRecords { get; set; } = new List<ActivityRecord>();
        public int Index { get; set; } = -1;
        public ActivityRecord CurrentActivity => ActivityRecords[Index];
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

        public void AddJiBanActivity(int jiBanId, bool isChallenger, bool isBuff) =>
            AddActivity(jiBanId, isChallenger,
                isBuff ? ActivityRecord.Types.JiBanBuff : ActivityRecord.Types.JiBanAttack);

        public void AddFragment(ActivityFragment fragment) => CurrentActivity.AddFragment(fragment);

        public ActivityFragment GetLastCardFragment() =>
            CurrentActivity.Data.LastOrDefault(c => c.Type == ActivityFragment.FragmentTypes.Chessman);

        public int GetActId() => CurrentActivity.Index < 0 ? 0 : CurrentActivity.CurrentFragment.ActId;
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

    }

    public record ChessboardFragment : ActivityFragment
    {
        public static ChessboardFragment Instance(Kinds kind, int skill, int actId, int pos, int targetId, int value,
            bool isChallenger) =>
            new(kind: kind, skill: skill, actId: actId, pos: pos, targetId: targetId, value: value,
                isChallenger: isChallenger);
        public ChessboardFragment(Kinds kind,int skill ,int actId,int pos, int targetId, int value, bool isChallenger) : base(FragmentTypes.Chessboard)
        {
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
            Responds.Add(new RespondAct(exeId, targetId, skill, kind, pop, finalPos, status));
    }

    public record CardFragment : ActivityFragment
    {
        public static CardFragment Instance(int instanceId, int actId) => new(instanceId, actId);

        public CardFragment(int instanceId, int actId) : base(FragmentTypes.Chessman)
        {
            InstanceId = instanceId;
            ActId = actId;
        }

        public int InstanceId { get; set; }
        
        /// <summary>
        /// 每次攻击
        /// </summary>
        public List<ExecuteAct> Executes { get; set; } = new List<ExecuteAct>();

        public override string ToString() => $"{InstanceId}.{Type},Act[{ActId}]Atts({Executes.Count})";

        public ExecuteAct GetOrInstanceAttack(ExecuteAct.Actions kind, Damage.Types damageType)
        {
            var at = Executes.FirstOrDefault();
            switch (at)
            {
                case null:
                    at = new ExecuteAct(kind, damageType);
                    Executes.Add(at);
                    break;
            }
            return at;
        }
    }

    public record ExecuteAct 
    {
        public enum Actions
        {
            /// <summary>
            /// 无，可以用于Attach类的非主攻击
            /// </summary>
            None,
            /// <summary>
            /// 基本攻击
            /// </summary>
            Basic,
            /// <summary>
            /// 兵种技能
            /// </summary>
            Perform,
        }
        public Actions Kind { get; set; }
        public List<RespondAct> Responds { get; set; } = new List<RespondAct>();
        public Damage.Types DamageType { get; set; }
        public CardFragment Inner { get; set; }

        public ExecuteAct(Actions kind, Damage.Types damageType)
        {
            Kind = kind;
            DamageType = damageType;
        }

        public override string ToString() =>
            $"{Kind},{DamageType},Resp({Responds.Count})," + (Inner == null ? string.Empty : "+Inner");

        public void AddRespond(int exeId, int targetId, int skill, RespondAct.Responds kind, int pop, int finalPos,
            ChessStatus status) => Responds.Add(new RespondAct(exeId, targetId, skill, kind, pop, finalPos, status));
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
            Suicide
        }
        public int ExeId { get; set; }
        public int TargetId { get; set; }
        public Responds Kind { get; set; }
        public int Pop { get; set; }
        /// <summary>
        /// 最终棋格
        /// </summary>
        public int FinalPos { get; set; }
        public int Skill { get; set; }
        public ChessStatus Status { get; set; }

        public RespondAct(int exeId, int targetId, int skill, Responds kind, int pop, int finalPos, ChessStatus status)
        {
            ExeId = exeId;
            TargetId = targetId;
            Kind = kind;
            Pop = pop;
            FinalPos = finalPos;
            Status = status;
            Skill = skill;
        }

        public override string ToString() => $"{TargetId}.{Kind},Pos({FinalPos}){Status},Pop({Pop})";
    }
}