using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NotImplementedException = System.NotImplementedException;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 棋子当前状态信息，
    /// 主要是：血量，状态值
    /// </summary>
    public class ChessStatus : BuffStatus
    {
        public static ChessStatus Instance(int hp, int maxHp, int pos, int speed, Dictionary<int, int> states) =>
            new ChessStatus(hp, maxHp, pos, speed, states);

        public static ChessStatus Instance(ChessStatus ps) =>
            Instance(ps.Hp, ps.MaxHp, ps.Pos, ps.Speed, ps.Buffs.ToDictionary(s => s.Key, s => s.Value));

        public int Hp { get; private set; }
        public int Pos { get; private set; }
        public int MaxHp { get; private set; }
        public int Speed { get; private set; }

        [JsonIgnore] public bool IsDeath => Hp <= 0;
        [JsonIgnore] public float HpRate => 1f * Hp / MaxHp;

        private ChessStatus(int hp, int maxHp, int pos,int speed, Dictionary<int, int> buffs)
        {
            Hp = hp;
            Buffs = buffs;
            Pos = pos;
            MaxHp = maxHp;
            Speed = speed;
        }

        public ChessStatus Clone() => Instance(this);

        public void SubtractHp(int damage)
        {
            LastSuffers.Add(damage);
            Hp -= damage;
            if (Hp < 0) Hp = 0;
        }

        public void SetSpeed(int speed) => Speed = speed;
        public void SetPos(int pos) => Pos = pos;

        public override string ToString() =>
            $"[{Hp}/{MaxHp}]Buffs[{Buffs.Count(b => b.Value > 0)}].LastS[{LastSuffers.Sum()}]";

        public void AddHp(int value, bool overLimit = false)
        {
            Hp += value;
            if (overLimit) return;
            if (Hp > MaxHp)
                Hp = MaxHp;
        }

        public void Kill() => Hp = 0;

        public int EaseShieldOffset(float damage)
        {
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
    }
}