using System;
using Newtonsoft.Json;

/// <summary>
/// 棋子主进程,主要描述所有有效棋格的主要行动
/// </summary>
[Serializable]
public class ChessPosProcess
{
    public static ChessPosProcess Instance(int id, PieceAction[] actions = null)
        => new ChessPosProcess() { InstanceId = id, Actions = actions ?? new PieceAction[0] };


    [JsonProperty("I")] public int InstanceId { get; set; }
    /// <summary>
    /// 棋子主属性
    /// </summary>
    [JsonProperty("S")] public PieceStatus Status { get; set; }
    /// <summary>
    /// 棋子命令集
    /// </summary>
    [JsonProperty("A")] public PieceAction[] Actions { get; set; }
}