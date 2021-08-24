using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Assets.System.WarModule
{
    public class BuffStatus
    {
        public Dictionary<int, int> Buffs { get; set; } = new Dictionary<int, int>();
        [JsonIgnore] public List<int> LastSuffers { get; } = new List<int>();
        public int GetBuff(int buffId) => Buffs.ContainsKey(buffId) ? Buffs[buffId] : 0;
        public int GetBuff(CardState.Cons buff) => GetBuff((int) buff);
        public void DepleteBuff(CardState.Cons buff,int value) => DepleteBuff((int) buff,value);

        public void DepleteBuff(int buffId,int value = 0)
        {
            if (GetBuff(buffId) <= 0) return;
            if (value == 0)
                Buffs[buffId]--;
            else Buffs[buffId] -= value;
            RefreshBuffs();
        }

        public void AddBuff(CardState.Cons con, int value = 1) => AddBuff((int) con, value);
        public void AddBuff(int buffId, int value)
        {
            if (!Buffs.ContainsKey(buffId))
                Buffs.Add(buffId, 0);
            Buffs[buffId] += value;
            //防护盾最大值
            if (buffId == (int) CardState.Cons.EaseShield)
                Buffs[buffId] = Math.Max(Buffs[buffId] + value, DataTable.GetGameValue(119));

            //去掉负数或是0的状态
            RefreshBuffs();
        }

        protected void RefreshBuffs()
        {
            if (Buffs.Any(s => s.Value <= 0))
                Buffs = Buffs.Where(s => s.Value > 0).ToDictionary(s => s.Key, s => s.Value);
        }

        public void ClearBuff(CardState.Cons con) => ClearBuff((int) con);
        public void ClearBuff(int element)
        {
            if (Buffs.ContainsKey(element))
                Buffs.Remove(element);
        }
    }
}