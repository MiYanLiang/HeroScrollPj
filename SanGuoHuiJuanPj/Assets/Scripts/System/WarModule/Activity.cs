using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 棋子对1个对象的1个行动描述。主要是嵌套在棋格主进程<see cref="ChessPosProcess"/>>的每一个行动描述。
    /// 而一个棋子的行动在一次进程中可能会有多个对象的行动。
    /// 注意：如果连击将会产生多个<see cref="Activity"/>
    /// </summary>
    [Serializable]
    public class Activity
    {
        public enum Intention
        {
            UnDefined,
            Major,
            Counter,
            Attach,
            Sprite
        }
        public static Activity[] Empty { get; } = Array.Empty<Activity>();
        //注意，负数是非棋子行动。一般都是上升到棋手这个维度的东西如：资源，金币
        /// <summary>
        /// 棋手资源类
        /// </summary>
        public const int PlayerResource = -1;
        /// <summary>
        /// 进攻行动
        /// </summary>
        public const int Offensive = 0;
        /// <summary>
        /// 同阵营行动
        /// </summary>
        public const int Friendly = 1;
        /// <summary>
        /// 
        /// </summary>
        public const int Counter = 2;
        /// <summary>
        /// 对自己的行动
        /// </summary>
        public const int Self = 4;
        /// <summary>
        /// 攻击触发器
        /// </summary>
        public const int OffendAttach = 5;
        /// <summary>
        /// 同阵营触发器
        /// </summary>
        public const int FriendlyAttach = 6;
        /// <summary>
        /// 精灵类型
        /// </summary>
        public const int Sprite = 7;

        /// <summary>
        /// 生成<see cref="Activity"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="processId"></param>
        /// <param name="from"></param>
        /// <param name="isChallenger"></param>
        /// <param name="to">正数为棋子Id，-1=玩家，-2=对手</param>
        /// <param name="intent"></param>
        /// <param name="conducts"></param>
        /// <param name="skill">技能值，普通攻击为0</param>
        /// <param name="rePos">换位</param>
        /// <returns></returns>
        public static Activity Instance(int id,int processId,int from ,int isChallenger,int to ,int intent, CombatConduct[] conducts,int skill,int rePos)
        {
            return new Activity()
            {
                InstanceId = id,
                ProcessId = processId,
                From = from,
                IsChallenger = isChallenger,
                To = to,
                Conducts = conducts,
                Intent = intent,
                Skill = skill,
                RePos = rePos
            };
        }
        [JsonProperty("I")] public int InstanceId { get; set; }
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
        [JsonProperty("K")] public int Intent { get; set; }
        /// <summary>
        /// Target Id, > 0 = InstanceId, -1 = Player, -2 = Opponent
        /// </summary>
        [JsonProperty("T")] public int To { get; set; }
        /// <summary>
        /// From InstanceId
        /// </summary>
        [JsonProperty("F")] public int From { get; set; }
        /// <summary>
        /// 如果正数代表换位
        /// </summary>
        [JsonProperty("R")] public int RePos { get; set; } = -1;
        /// <summary>
        /// 技能值，普通攻击=0，其余的值是根据兵种标记
        /// </summary>
        [JsonProperty("S")] public int Skill { get; set; }
        [JsonProperty("P")] public int ProcessId { get; set; }
        [JsonProperty("C")] public CombatConduct[] Conducts { get; set; }
        [JsonProperty("A")] public ActivityResult Result { get; set; }
        [JsonProperty("O")]public ChessStatus OffenderStatus { get; set; }
        [JsonProperty("IC")] public int IsChallenger { get; set; }

        public Intention GetIntention() => GetIntention(Intent);
        public static Intention GetIntention(int intent)
        {
            switch (intent)
            {
                case Offensive: 
                case Friendly: 
                case Self: return Intention.Major;
                case Counter: return Intention.Counter;
                case OffendAttach: 
                case FriendlyAttach: return Intention.Attach;
                case PlayerResource: return Intention.UnDefined;
                case Sprite: return Intention.Sprite;
                default:
                    throw new ArgumentOutOfRangeException($"{nameof(GetIntention)}:Unknown intent({intent})");
            }
        }

        [JsonIgnore] public bool IsRePos => RePos >= 0;
        [JsonIgnore] public bool TargetIsChallenger
        {
            get
            {
                var isChallenger = IsChallenger == 0;
                return (Intent == Offensive ||
                        Intent == OffendAttach ||
                        Intent == Counter)
                    ? !isChallenger
                    : isChallenger;
            }
        }


        public override string ToString() => $"{InstanceId}.Intent({GetIntention()})[{Intent}].From[{From}({IsChallenger})].To[{To}].Com[{Conducts.Length}].Result[{Result.Result}]";
    }

    public class ActivityResult
    {

        [JsonConstructor]
        private ActivityResult()
        {

        }

        public static ActivityResult Instance(int resultId) => new ActivityResult { Result = resultId };
        public static ActivityResult Instance(Types type) => Instance((int)type);

        public enum Types
        {
            Undefined = -1,
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
            Friendly = 2,
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
            Kill = 7
        }
        /// <summary>
        /// 精灵生成
        /// </summary>
        //public List<ChessSprite> SpriteAdded { get; set; }
        //public List<int>SpriteRemove { get; set; }
        public int Result { get; set; }
        public ChessStatus Status { get; set; }
        public Types Type => (Types)Result;
        [JsonIgnore] public bool IsDeath => Status.IsDeath;
        public override string ToString() => $"{Type}({Result}).Sta[{Status.Hp}/{Status.MaxHp}]";
        public void SetStatus(ChessStatus status) => Status = status.Clone();
    }

}