using System.Collections.Generic;
using Newtonsoft.Json;

namespace Assets.System.WarModule
{
    public class ChessRound
    {
        public List<ChessProcess> PlaceActions { get; set; }
        public Dictionary<int,ChessStatus> PreRoundStats { get; set; }
        //[JsonProperty("I")] 
        public int InstanceId { get; set; }
        //[JsonProperty("C")] 
        public List<int> ChallengerJiBans { get; set; }
        //[JsonProperty("O")] 
        public List<int> OppositeJiBans { get; set; }
        //[JsonProperty("P")] 
        public RoundAction PreAction { get; set; }
        //[JsonProperty("M")] 
        public List<ChessProcess> Processes { get; set; }
        //[JsonProperty("F")]
        public RoundAction FinalAction { get; set; }

        public bool IsEnd { get; set; }
        //[JsonProperty("L")] 

        public override string ToString() =>
            $"{InstanceId}.Process[{Processes.Count}].Pre[{PreAction.ChessProcesses.Count}].Fin[{FinalAction.ChessProcesses.Count}]";
    }
}