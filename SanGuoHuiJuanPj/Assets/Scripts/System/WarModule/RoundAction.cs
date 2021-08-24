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
        public Dictionary<int, List<Activity>> Activities { get; set; } = new Dictionary<int, List<Activity>>();

        public void ConcatAction(RoundAction round)
        {
            var dic = round.Activities.ToDictionary(s => s.Key, s => s.Value);
            foreach (var act in dic.Where(act => dic.ContainsKey(act.Key)))
            {
                act.Value.AddRange(dic[act.Key]);
                dic.Remove(act.Key);
            }

            foreach (var obj in dic)
            {
                if (Activities.ContainsKey(obj.Key))
                    throw new InvalidOperationException($"Duplicated key!");
                Activities.Add(obj.Key, obj.Value);
            }
        }
    }
}