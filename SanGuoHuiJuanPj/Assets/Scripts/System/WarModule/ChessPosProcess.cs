using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 棋子主进程,主要描述所有有效棋格的主要行动
    /// </summary>
    [Serializable]
    public class ChessProcess
    {
        public enum Types
        {
            Chessman,
            Chessboard,
            JiBan
        }

        public static ChessProcess Instance(int id,int roundId ,Types type, int major, bool isChallenger) =>
            new ChessProcess()
            {
                InstanceId = id,
                RoundId = roundId,
                Type = type,
                Major = major,
                Scope = isChallenger ? 0 : 1,
                CombatMaps = new Dictionary<int, CombatMapper>()
            };
        public static ChessProcess Instance(int id,int roundId,int pos, bool isChallenger)
            => new ChessProcess()
            {
                InstanceId = id, 
                RoundId = roundId,
                Type = Types.Chessman,
                Major = pos, 
                Scope = isChallenger ? 0 : 1,
                CombatMaps = new Dictionary<int, CombatMapper>()
            };


        //[JsonProperty("I")] 
        public int InstanceId { get; set; }
        public int RoundId { get; set; }
        /// <summary>
        /// 棋子活动集，key = actId, value = 活动
        /// </summary>
        //[JsonProperty("A")]
        public Dictionary<int,CombatMapper> CombatMaps { get; set; }
        //[JsonProperty("P")] 
        /// <summary>
        /// <see cref="Types.Chessman"/>  和 <see cref="Types.Chessboard"/> : -1 = 棋盘活动，正数为棋子InstanceId，
        /// <see cref="Types.JiBan"/> = 羁绊Id
        /// </summary>
        public int Major { get; set; }

        public Types Type { get; set; }
        //[JsonProperty("S")] 
        public int Scope { get; set; }
        public override string ToString()
        {
            string challengerText;
            switch (Type)
            {
                case Types.Chessman:
                    challengerText = Scope == 0? $"玩家Pos({Major})": $"对手Pos({Major})";
                    break;
                case Types.Chessboard:
                    challengerText = Scope == 0 ? "[玩家方]" : "[对手方]";
                    break;
                case Types.JiBan:
                    challengerText = Scope == 0 ? "[玩家羁绊]" : "[对手羁绊]";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return $"主进程({InstanceId}).{challengerText}，活动({CombatMaps.Count})";
        }
    }

    public class CombatMapper
    {
        public int Id {  get; set; }
        public List<Activity> Activities { get; set; } = new List<Activity>();
        public List<Activity> CounterActs { get; set; } = new List<Activity>();

        public CombatMapper()
        {
            
        }

        public CombatMapper(int id)
        {
            Id = id;
        }
    }
}