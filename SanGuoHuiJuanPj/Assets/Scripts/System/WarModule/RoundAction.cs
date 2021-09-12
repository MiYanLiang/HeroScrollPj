using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 回合行动，所有行动
    /// </summary>
    [Serializable]
    public class RoundAction
    {
        public List<ChessProcess> ChessProcesses {  get; set; } = new List<ChessProcess>();
    }
}