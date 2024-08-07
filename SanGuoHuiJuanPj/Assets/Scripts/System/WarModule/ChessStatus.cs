﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 棋子当前状态信息，
    /// 主要是：血量，状态值
    /// </summary>
    public class ChessStatus : BuffStatus
    {
        public static ChessStatus Instance(int hp, int maxHp, int pos, Dictionary<int, int> states,
            List<int> last, int lastHeal, int lastEaseShield = 0) =>
            new ChessStatus(hp, maxHp, pos, states, last, lastHeal, lastEaseShield);

        public static ChessStatus Instance(ChessStatus ps) => Instance(ps.Hp, ps.MaxHp, ps.Pos, ps.Buffs.ToDictionary(s => s.Key, s => s.Value), ps.LastSuffers.ToList(), ps.LastHeal,
            ps.LastEaseShieldDamage);
        

        public int Hp { get; set; }
        public int Pos { get; set; }
        public int MaxHp { get; set; }

        //[JsonIgnore] 
        public bool IsDeath => Hp <= 0;
        //[JsonIgnore] 
        public float HpRate => 1f * Hp / MaxHp;
        public int LastEaseShieldDamage { get; set; }
        public int EaseHp => GetBuff(CardState.Cons.EaseShield) + Hp;
        public int LastHeal { get; set; }
        private ChessStatus()
        {
        }

        private ChessStatus(int hp, int maxHp, int pos, Dictionary<int, int> buffs, List<int> last,
            int lastHeal, int lastEaseShield)
        {
            Hp = hp;
            Buffs = buffs;
            Pos = pos;
            MaxHp = maxHp;
            LastSuffers = last;
            LastHeal = lastHeal;
            LastEaseShieldDamage = lastEaseShield;
        }

        public ChessStatus CloneHp() => Instance(this);

        public void SubtractHp(int damage)
        {
            LastSuffers.Add(damage);
            Hp -= damage;
            if (Hp < 0) Hp = 0;
        }

        public void SetPos(int pos) => Pos = pos;

        public override string ToString() =>
            $"[{Hp}/{MaxHp}]Buffs[{BuffCount()}].LastS[{LastSuffersCount()}]";

        private int LastSuffersCount()
        {
            if (LastSuffers == null || !LastSuffers.Any()) return 0;
            return LastSuffers.Sum();
        }
        private int BuffCount()
        {
            if (Buffs == null || !Buffs.Any()) return 0;
            return Buffs.Count(b => b.Value > 0);
        }
        public void OnHeal(int value, bool overLimit = false)
        {
            if (value < 0)
                throw new InvalidOperationException("补血数量不可以低于0");
            LastHeal = value;
            Hp += value;
            if (overLimit) return;
            Hp = Math.Min(MaxHp, Hp);
        }

        public void Kill() => Hp = 0;

        public int EaseShieldOffset(float damage)
        {
            LastEaseShieldDamage = (int)damage;
            var ease = GetBuff(CardState.Cons.EaseShield);
            if (ease > damage)
            {
                DepleteBuff(CardState.Cons.EaseShield, (int) damage);
                return 0;
            }
            ClearBuff(CardState.Cons.EaseShield);
            return (int) damage - ease;
        }

        public void ResetHp(int maxHp)
        {
            Hp = maxHp;
            MaxHp = maxHp;
        }

        public void SetHp(int hp) => Hp = hp;

        public bool UnAvailable => GetBuff(CardState.Cons.Stunned) > 0 ||
                                   GetBuff(CardState.Cons.Imprisoned) > 0 ||
                                   IsDeath;
    }
}