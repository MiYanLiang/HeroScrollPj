using Newtonsoft.Json;

public class ChessRound
{
    [JsonProperty("I")] public int InstanceId { get; set; }
    [JsonProperty("P")] public StateAction PreAction { get; set; }
    [JsonProperty("M")] public ChessPosProcess[] Processes { get; set; }
    [JsonProperty("F")] public StateAction FinalAction { get; set; }
    [JsonProperty("L")] public int LastRound { get; set; }
    [JsonIgnore] public bool IsLastRound => LastRound > 0;
}