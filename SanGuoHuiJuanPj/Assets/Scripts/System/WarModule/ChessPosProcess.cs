using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 棋子主进程,主要描述所有有效棋格的主要行动
    /// </summary>
    [Serializable]
    public class ChessPosProcess
    {
        public static ChessPosProcess Instance(int id, int pos, bool isChallenger)
            => new ChessPosProcess()
            {
                InstanceId = id, Pos = pos, Scope = isChallenger ? 0 : 1,
                Activities = new List<Activity>()
            };


        [JsonProperty("I")] public int InstanceId { get; set; }
        /// <summary>
        /// 棋子命令集
        /// </summary>
        [JsonProperty("A")] public List<Activity> Activities { get; set; }
        /// <summary>
        /// 棋格，-1 = Challenger, -2 = Opposite，
        /// 正数 = 棋位
        /// </summary>
        [JsonProperty("P")] public int Pos { get; set; }
        [JsonProperty("S")] public int Scope { get; set; }
        public override string ToString() => $"{InstanceId}.Pos[{Pos}]({Scope}).Act[{Activities.Count}]";
    }
}