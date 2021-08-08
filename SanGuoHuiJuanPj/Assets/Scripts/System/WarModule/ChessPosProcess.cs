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
        public static ChessPosProcess Instance(int id, PieceStatus status)
            => new ChessPosProcess() {InstanceId = id, Status = status, Actions = new List<Activity>()};


        [JsonProperty("I")] public int InstanceId { get; set; }
        /// <summary>
        /// 棋子主属性
        /// </summary>
        [JsonProperty("S")] public PieceStatus Status { get; set; }
        /// <summary>
        /// 棋子命令集
        /// </summary>
        [JsonProperty("A")] public List<Activity> Actions { get; set; }

        public override string ToString() => $"{InstanceId}.Sta[{Status}].Act[{Actions.Count}]";
    }
}