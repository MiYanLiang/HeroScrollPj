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
        public const int PlayerResources = -1;
        public const int JiBan = 0;
        public const int RoundBuffing = 1;

        /// <summary>
        /// State id , action
        /// </summary>
        [JsonProperty("A")]
        public List<Activity> Activities { get; set; } = new List<Activity>();

    }
}