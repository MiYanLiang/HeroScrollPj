using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Assets.System.WarModule
{
    public class BuffStatus
    {
        public Dictionary<int, int> Buffs { get; set; } = new Dictionary<int, int>();
        public List<int> LastSuffers { get; set; } = new List<int>();
        public int GetBuff(int buffId) => Buffs.ContainsKey(buffId) ? Buffs[buffId] : 0;
        public int GetBuff(CardState.Cons buff) => GetBuff((int) buff);
        public void DepleteBuff(CardState.Cons buff,int value) => DepleteBuff((int) buff,value);

        public void DepleteBuff(int buffId,int value = 0)
        {
            if (GetBuff(buffId) <= 0) return;
            if (value == 0)
                Buffs[buffId]--;
            else Buffs[buffId] -= value;
            NegativeBuffAlignment();
        }

        public void AddBuff(CardState.Cons con, int value = 1) => AddBuff((int) con, value);
        public void AddBuff(int buffId, int value)
        {
            if (!Buffs.ContainsKey(buffId))
                Buffs.Add(buffId, 0);
            Buffs[buffId] += value;
            //防护盾最大值
            if (buffId == (int)CardState.Cons.EaseShield)
                Buffs[buffId] = Math.Min(Buffs[buffId] + value, CardState.EaseShieldMax);
            if (buffId == (int)CardState.Cons.BattleSoul)
                Buffs[buffId] = Math.Min(Buffs[buffId] + value, 20);//战意最大值10

            //去掉负数或是0的状态
            NegativeBuffAlignment(); 
        }

        //负值校正
        private void NegativeBuffAlignment()
        {
            foreach (var buff in Buffs.ToDictionary(b => b.Key, b => b.Value).Where(BuffPredicate))
                Buffs[buff.Key] = 0;

            bool BuffPredicate(KeyValuePair<int, int> arg)
            {
                var buffId = (CardState.Cons)arg.Key;
                var buffValue = arg.Value;
                //属于基础属性的值加成类的会有负值,所以不会有负值校正
                switch (buffId)
                {
                    case CardState.Cons.StrengthUp:
                    case CardState.Cons.DodgeUp:
                    case CardState.Cons.CriticalUp:
                    case CardState.Cons.RouseUp:
                    case CardState.Cons.ArmorUp:
                    case CardState.Cons.SpeedUp:
                    case CardState.Cons.IntelligentUp:
                        return false;
                    default:
                        return buffValue < 0;
                }
            }
        }

        public void ClearBuff(CardState.Cons con) => ClearBuff((int) con);
        public void ClearBuff(int element)
        {
            if (Buffs.ContainsKey(element))
                Buffs.Remove(element);
        }
    }
}