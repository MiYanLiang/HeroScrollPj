using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 棋子对1个对象的1个行动描述。主要是嵌套在棋格主进程<see cref="ChessProcess"/>>的每一个行动描述。
    /// 而一个棋子的行动在一次进程中可能会有多个对象的行动。
    /// 注意：如果连击将会产生多个<see cref="Activity"/>
    /// </summary>
    
    public class Activity
    {
        public static Activity[] Empty { get; } = Array.Empty<Activity>();
        //注意，负数是非棋子行动。一般都是上升到棋手这个维度的东西如：资源，金币
        /// <summary>
        /// 棋盘执行活动，如： Buff消减，必须对棋子执行的活动
        /// </summary>
        public const int ChessboardBuffing = -2;
        /// <summary>
        /// 棋手资源类
        /// </summary>
        public const int PlayerResource = -1;
        /// <summary>
        /// 精灵类型
        /// </summary>
        public const int Sprite = -3;
        /// <summary>
        /// 进攻行动
        /// </summary>
        public const int Offensive = 0;
        /// <summary>
        /// 
        /// </summary>
        public const int Counter = 1;
        /// <summary>
        /// 反伤
        /// </summary>
        public const int Reflect = 2;
        /// <summary>
        /// 同阵营行动
        /// </summary>
        public const int Friendly = 3;
        /// <summary>
        /// 对自己的行动
        /// </summary>
        public const int Self = 4;
        /// <summary>
        /// 不可避免(闪避)
        /// </summary>
        public const int Inevitable = 7;

        /// <summary>
        /// 生成<see cref="Activity"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="processId"></param>
        /// <param name="from"></param>
        /// <param name="isChallenger">0=challenger,1=opposite</param>
        /// <param name="to">正数为棋子Id，-1=玩家，-2=对手</param>
        /// <param name="intent"></param>
        /// <param name="conducts"></param>
        /// <param name="skill">技能值，普通攻击为0</param>
        /// <param name="rePos">换位</param>
        /// <returns></returns>
        public static Activity Instance(int id,int processId,int from ,int isChallenger,int to ,int intent, IList<CombatConduct> conducts,int skill,int rePos)
        {
            return new Activity
            {
                InstanceId = id,
                ProcessId = processId,
                From = from,
                IsChallenger = isChallenger,
                To = to,
                Conducts = conducts.ToList(),
                Intent = intent,
                Skill = skill,
                RePos = rePos,
            };
        }
        //[JsonProperty("I")] 
        public int InstanceId { get; set; }
        /// <summary>
        /// 行动描述，描述Id参考常数：
        /// <see cref="Offensive"/>,
        /// <see cref="Friendly"/>,
        /// <see cref="Counter"/>,
        /// <see cref="Self"/>,
        /// <see cref="OffendAttach"/>,
        /// <see cref="FriendlyAttach"/>,
        /// <see cref="PlayerResource"/>,
        /// </summary>
        //[JsonProperty("K")] 
        public int Intent { get; set; }

        /// <summary>
        /// Target Id, > 0 = InstanceId, -1 = Player, -2 = Opponent
        /// </summary>
        //[JsonProperty("T")] 
        public int To { get; set; }
        /// <summary>
        /// From InstanceId, -1 = Challenger, -2 = Opposite
        /// </summary>
        //[JsonProperty("F")] 
        public int From { get; set; }
        /// <summary>
        /// 如果正数代表换位
        /// </summary>
        //[JsonProperty("P")] 
        public int RePos { get; set; } = -1;
        /// <summary>
        /// 技能值，普通攻击=0，其余的值是根据兵种标记,
        /// 羁绊技能值会一直保持 0，而buff伤害技能值是<see cref="CardState.Cons"/>Id
        /// </summary>
        //[JsonProperty("S")] 
        public int Skill { get; set; }
        //[JsonProperty("PI")] 
        public int ProcessId { get; set; }
        //[JsonProperty("C")] 
        public List<CombatConduct> Conducts { get; set; }
        //[JsonProperty("R")] 
        public ChessStatus TargetStatus { get; set; }
        //public ActivityResult Result { get; set; }

        //[JsonProperty("OS")]
        /// <summary>
        /// 发起方 0 = Challenger, 1 = Opposite
        /// </summary>
        //[JsonProperty("IC")] 
        public int IsChallenger { get; set; }

        //[JsonIgnore] 
        public bool IsRePos => RePos >= 0;

        public string IntentText()
        {
            var intentText = string.Empty;
            var toText = string.Empty;
            switch (Intent)
            {
                case PlayerResource:
                    intentText = "玩家资源";
                    break;
                case Offensive:
                    intentText = "攻击";
                    break;
                case Friendly:
                    intentText = "队友";
                    break;
                case Counter:
                    intentText = "反击";
                    break;
                case Self:
                    intentText = "自己";
                    break;
                case Sprite:
                    intentText = "精灵";
                    toText = $"棋格[{To}]";
                    break;
                case Reflect:
                    intentText = "反伤";
                    break;

            }
            if(Intent!=Sprite)
                switch (To)
                {
                    case -1:
                        toText = "己方";
                        break;
                    case -2:
                        toText = "对方";
                        break;
                    default:
                        toText = $"单位Id({To})";
                        break;
                }

            return intentText + toText;
        }
        public string StanceText()=>IsChallenger == 0 ? "玩家" : "对方";
        public override string ToString()
        {
            var targetText = TargetStatus == null ? string.Empty : TargetStatus.ToString();
            //var result = Result == null ? string.Empty : Result.Type.ToString();
            var conducts = Conducts == null ? string.Empty : Conducts.Count.ToString();
            return $"({InstanceId}):[{StanceText()}]{IntentText()}: 武技={conducts} {targetText}";
                   //+ $" 结果[{result}]";
        }
    }

    public class ActivityResult
    {

        //[JsonConstructor]
        private ActivityResult()
        {

        }

        public static ActivityResult Instance(int resultId) => new ActivityResult { Result = resultId };
        public static ActivityResult Instance(Types type) => Instance((int)type);

        public enum Types
        {
            /// <summary>
            /// 地块结果
            /// </summary>
            ChessPos = -1,
            /// <summary>
            /// 承受
            /// </summary>
            Suffer = 0,
            /// <summary>
            /// 闪避
            /// </summary>
            Dodge = 1,
            /// <summary>
            /// 同势力，如果同势力表现会不同，例如被同势力伤害了(反击单位)不会反击
            /// </summary>
            Assist = 2,
            /// <summary>
            /// 盾挡状态
            /// </summary>
            Shield = 4,
            /// <summary>
            /// 无敌状态
            /// </summary>
            Invincible = 5,
            /// <summary>
            /// 防护盾
            /// </summary>
            EaseShield = 6,
            /// <summary>
            /// 击杀效果
            /// </summary>
            Kill = 7,
            /// <summary>
            /// 自杀
            /// </summary>
            Suicide = 8
        }

        public int Result { get; set; }
        public ChessStatus Status { get; set; }
        public Types Type => (Types)Result;
        //[JsonIgnore] 
        public bool IsDeath => Status.IsDeath;

        public override string ToString()
        {
            var resultText = string.Empty;
            var lastText = string.Empty;
            switch (Type)
            {
                case Types.ChessPos:
                    break;
                case Types.Suffer:
                    resultText = "承受";
                    lastText = GenerateLastText();
                    break;
                case Types.Dodge:
                    resultText = "闪避";
                    break;
                case Types.Assist:
                    resultText = "同伴";
                    lastText = GenerateLastText();
                    break;
                case Types.Shield:
                    resultText = "护盾";
                    break;
                case Types.Invincible:
                    resultText = "无敌";
                    break;
                case Types.EaseShield:
                    resultText = "缓冲盾";
                    lastText = GenerateLastText();
                    break;
                case Types.Kill:
                    resultText = "击杀";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (Status == null) return $"活动结果：{resultText}";
            return
                $"【活动结果】：{resultText}.Sta[{Status.Hp}/{Status.MaxHp}]Buffs({AppendBuffs(Status.Buffs.Where(b => b.Value > 0).Select(b => $"[{b.Key}:{b.Value}]"))}){lastText}";

            string GenerateLastText()
            {
                var last = Status?.LastSuffers?.LastOrDefault();
                return last != null ? $"[上个伤害:{last}]" : string.Empty;
            }
        }


        private string AppendBuffs(IEnumerable<string> texts)
        {
            var sb = new StringBuilder();
            foreach (var text in texts) sb.Append(text);
            return sb.ToString();
        }

        public void SetStatus(ChessStatus status) => Status = status.Clone();
    }

}