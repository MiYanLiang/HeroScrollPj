using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CorrelateLib;
using Newtonsoft.Json;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 棋格精灵，存在地块上执行任务。与<see cref="BuffOperator"/>不一样的是，它是挂在棋格上的状态
    /// </summary>
    public abstract class PosSprite
    {
        public enum Kinds
        {
            /// <summary>
            /// 通用,一般用于回合类提升武，智，速的一种精灵
            /// </summary>
            Generic = -1,
            /// <summary>
            /// 爆炸+火焰
            /// </summary>
            YeHuo = 101,
            /// <summary>
            /// 一般火焰
            /// </summary>
            FireFlame = 100,
            /// <summary>
            /// 落雷
            /// </summary>
            Thunder = 102,
            /// <summary>
            /// 投射精灵， 
            /// </summary>
            CastSprite = 106,
            /// <summary>
            /// 地震
            /// </summary>
            Earthquake = 104,
            /// <summary>
            /// 抛石精灵
            /// </summary>
            Catapult = 105,
            /// <summary>
            /// 箭
            /// </summary>
            Arrow = 107,
            /// <summary>
            /// 滚石
            /// </summary>
            RollingStone = 108,
            /// <summary>
            /// 滚木
            /// </summary>
            RollingWood = 109,
            /// <summary>
            /// 阴天精灵
            /// </summary>
            Shady = 110,
            /**下列是跟状态有关的精灵类型**/
            YellowBand = 23,
            Chained = 24,
            Forge = 20,
            Strength = 10,
            Armor = 14,
            Dodge = 11,
            Critical = 12,
            Rouse = 13,
        }

        public static Kinds GetKind(int typeId) => (Kinds)typeId;
        public static Kinds GetKind(CombatConduct conduct) => (Kinds)conduct.Element;

        /// <summary>
        /// 类型
        /// </summary>
        public enum HostType
        {
            /// <summary>
            /// 依赖类型
            /// </summary>
            Relation,
            /// <summary>
            /// 回合类型
            /// </summary>
            Round
        }

        public static T Instance<T>(ChessboardOperator chessboardOp, int instanceId, int lasting, int value, int pos,
            bool isChallenger)
            where T : PosSprite, new()
        {
            return new T
            {
                Grid = chessboardOp.Grid,
                InstanceId = instanceId,
                Value = value,
                Pos = pos,
                IsChallenger = isChallenger,
                Lasting = lasting,
            };
        }

        /// <summary>
        /// ChessboardOperator千万别用来添加活动。它仅仅用在地块查询
        /// </summary>
        protected ChessGrid Grid { get; private set; }
        public int InstanceId { get; set; }
        /// <summary>
        /// 类型标签，用来识别单位类型
        /// </summary>
        public abstract int TypeId { get; }
        public abstract HostType Host { get; }
        /// <summary>
        /// <see cref="HostType.Round"/> = 回合数，<see cref="HostType.Relation"/> = 宿主Id
        /// </summary>
        public int Lasting { get; set; } = -1;
        /// <summary>
        /// 状态值
        /// </summary>
        public int Value { get; set; }
        /// <summary>
        /// 棋格
        /// </summary>
        public int Pos { get; set; }
        public bool IsChallenger { get; set; }
        protected PosSprite()
        {
        }

        /// <summary>
        /// 对不同的buff给出增/减值
        /// </summary>
        /// <param name="buff"></param>
        /// <param name="op"></param>
        /// <returns></returns>
        public virtual int ServedBuff(CardState.Cons buff,IChessOperator op) => 0;

        public Kinds GetKind() => GetKind(TypeId);

        public string GetName() => GetName(GetKind());
        public static string GetName(Kinds kind)
        {
            switch (kind)
            {
                case Kinds.Generic: return (nameof(Kinds.Generic));
                case Kinds.YeHuo: return (nameof(Kinds.YeHuo));
                case Kinds.FireFlame: return (nameof(Kinds.FireFlame));
                case Kinds.Thunder: return (nameof(Kinds.Thunder));
                case Kinds.CastSprite: return (nameof(Kinds.CastSprite));
                case Kinds.Earthquake: return (nameof(Kinds.Earthquake));
                case Kinds.Catapult: return (nameof(Kinds.Catapult));
                case Kinds.Arrow: return (nameof(Kinds.Arrow));
                case Kinds.YellowBand: return (nameof(Kinds.YellowBand));
                case Kinds.Chained: return (nameof(Kinds.Chained));
                case Kinds.Forge: return (nameof(Kinds.Forge));
                case Kinds.Strength: return (nameof(Kinds.Strength));
                case Kinds.Armor: return (nameof(Kinds.Armor));
                case Kinds.Dodge: return (nameof(Kinds.Dodge));
                case Kinds.Critical: return (nameof(Kinds.Critical));
                case Kinds.Rouse: return (nameof(Kinds.Rouse));
                case Kinds.RollingStone: return (nameof(Kinds.RollingStone));
                case Kinds.RollingWood: return (nameof(Kinds.RollingWood));
                case Kinds.Shady: return (nameof(Kinds.Shady));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        public override string ToString()
        {
            var sb = new StringBuilder("精灵");
            sb.Append($"({InstanceId})");
            sb.Append(GetName());
            sb.Append(Host == HostType.Relation ? $".宿主({Lasting})" : $".回合({Lasting})");
            sb.Append($"[{Value}]");
            sb.Append("Pos:");
            sb.Append(IsChallenger ? $"玩家({Pos})" : $"对手({Pos})");
            //return $"精灵({InstanceId})({kindText}).{hostText} [{Value}] Pos:{challengerText}";
            return sb.ToString();
        }

        public virtual void OnActivity(ChessOperator offender, ChessboardOperator chessboard,
            CombatConduct[] conducts, int actId, int skill)
        {
            var target = chessboard.GetChessPos(IsChallenger, Pos);
            if (target.IsPostedAlive)
                chessboard.AppendOpActivity(offender, target, Activity.Intentions.Offensive, conducts, actId, skill);
        }
        public virtual void RoundStart(IChessOperator op, ChessboardOperator chessboard)
        {
        }
    }

    public abstract class RoundSprite : PosSprite
    {
        public override HostType Host { get; } = HostType.Round;
    }

    public class JiBanStrengthSprite : RoundSprite
    {
        public override int TypeId { get; } = (int)Kinds.Generic;
        public override int ServedBuff(CardState.Cons buff, IChessOperator op) => buff == CardState.Cons.StrengthUp ? Value : 0;
    }
    public class JiBanSpeedSprite : RoundSprite
    {
        public override int TypeId { get; } = (int)Kinds.Generic;
        public override int ServedBuff(CardState.Cons buff, IChessOperator op) => buff == CardState.Cons.SpeedUp ? Value : 0;
    }
    public class JiBanIntelligentSprite : RoundSprite
    {
        public override int TypeId { get; } = (int)Kinds.Generic;
        public override int ServedBuff(CardState.Cons buff, IChessOperator op) => buff == CardState.Cons.IntelligentUp ? Value : 0;
    }
    // 依赖型精灵
    public abstract class RelationSprite : PosSprite
    {
        public override HostType Host { get; } = HostType.Relation;
        /// <summary>
        /// 代理特殊条件
        /// </summary>
        protected abstract Func<IChessOperator, bool> OpCondition { get; }

        public override int ServedBuff(CardState.Cons buff, IChessOperator op) =>
            op == null ? 0 : OpCondition(op) ? BuffRespond(buff, op) : 0;
        //依赖类不执行(回合)消耗
        protected abstract int BuffRespond(CardState.Cons con, IChessOperator op);
    }
    /// <summary>
    /// 力量精灵
    /// </summary>
    public abstract class StrengthSprite : RelationSprite
    {
        public override int TypeId => (int)Kinds.Strength;
        protected override int BuffRespond(CardState.Cons con, IChessOperator op) => con == CardState.Cons.StrengthUp ? Value : 0;
    }
    public class ArmorSprite : RelationSprite
    {
        public override int TypeId => (int)Kinds.Armor;
        protected override Func<IChessOperator, bool> OpCondition => op => op.CardType == GameCardType.Hero;
        protected override int BuffRespond(CardState.Cons con, IChessOperator op) => con == CardState.Cons.ArmorUp ? Value : 0;
    }
    /// <summary>
    /// 近战力量精灵
    /// </summary>
    public class MeleeStrengthSprite : StrengthSprite
    {
        protected override Func<IChessOperator, bool> OpCondition => op => op.IsMeleeHero;
    }
    /// <summary>
    /// 远程力量精灵
    /// </summary>
    public class RangeStrengthSprite : StrengthSprite
    {
        protected override Func<IChessOperator, bool> OpCondition => op => op.IsRangeHero;
    }
    /// <summary>
    /// 物理精灵
    /// </summary>
    public class PhysicalSprite : StrengthSprite
    {
        protected override Func<IChessOperator, bool> OpCondition => op => Damage.GetKind(op.Style.Element) == Damage.Kinds.Physical;
    }
    /// <summary>
    /// 法力精灵
    /// </summary>
    public class MagicForceSprite : StrengthSprite
    {
        protected override Func<IChessOperator, bool> OpCondition => op => Damage.GetKind(op.Style.Element) == Damage.Kinds.Magic;
    }
    /// <summary>
    /// 曹魏精灵
    /// </summary>
    public class CaoWeiSprite : StrengthSprite
    {
        protected override Func<IChessOperator, bool> OpCondition => op => op.Style.Troop == 1;
    }
    /// <summary>
    /// 蜀汉精灵
    /// </summary>
    public class SuHanSprite : StrengthSprite
    {
        protected override Func<IChessOperator, bool> OpCondition => op => op.Style.Troop == 0;
    }
    /// <summary>
    /// 东吴精灵
    /// </summary>
    public class DongWuSprite : StrengthSprite
    {
        protected override Func<IChessOperator, bool> OpCondition => op => op.Style.Troop == 2;
    }
    /// <summary>
    /// 步兵精灵
    /// </summary>
    public class InfantrySprite : StrengthSprite
    {
        protected override Func<IChessOperator, bool> OpCondition => op => op.Style.ArmedType == 2;
    }
    /// <summary>
    /// 长持精灵
    /// </summary>
    public class StickWeaponSprite : StrengthSprite
    {
        protected override Func<IChessOperator, bool> OpCondition => op => op.Style.ArmedType == 3;
    }
    /// <summary>
    /// 骑兵精灵
    /// </summary>
    public class CavalrySprite : StrengthSprite
    {
        protected override Func<IChessOperator, bool> OpCondition => op => op.Style.ArmedType == 5;
    }
    /// <summary>
    /// 箭系精灵
    /// </summary>
    public class ArrowUpSprite : StrengthSprite
    {
        protected override Func<IChessOperator, bool> OpCondition => op => op.Style.ArmedType == 7;
    }
    /// <summary>
    /// 战船精灵
    /// </summary>
    public class WarshipSprite : StrengthSprite
    {
        protected override Func<IChessOperator, bool> OpCondition => op => op.Style.ArmedType == 8;
    }
    /// <summary>
    /// 闪避精灵
    /// </summary>
    public class DodgeSprite : RelationSprite
    {
        protected override Func<IChessOperator, bool> OpCondition => op => op.CardType == GameCardType.Hero;

        protected override int BuffRespond(CardState.Cons con, IChessOperator op) => con == CardState.Cons.DodgeUp ? Value : 0;
        public override int TypeId => (int)Kinds.Dodge;
    }
    /// <summary>
    /// 暴击精灵
    /// </summary>
    public class CriticalSprite : RelationSprite
    {
        protected override Func<IChessOperator, bool> OpCondition => op => op.CardType == GameCardType.Hero;

        protected override int BuffRespond(CardState.Cons con, IChessOperator op) => con == CardState.Cons.CriticalUp ? Value : 0;
        public override int TypeId => (int)Kinds.Critical;
    }
    /// <summary>
    /// 会心精灵
    /// </summary>
    public class RouseSprite : RelationSprite
    {
        protected override Func<IChessOperator, bool> OpCondition => op => op.CardType == GameCardType.Hero;
        protected override int BuffRespond(CardState.Cons con, IChessOperator op) => con == CardState.Cons.RouseUp ? Value : 0;
        public override int TypeId => (int)Kinds.Rouse;
    }
    /// <summary>
    /// 迷雾精灵
    /// </summary>
    public class ForgeSprite : DodgeSprite
    {
        public override int TypeId => (int)Kinds.Forge;

        protected override int BuffRespond(CardState.Cons con, IChessOperator op)
        {
            switch (con)
            {
                case CardState.Cons.DodgeUp:
                    return Value;
                case CardState.Cons.Forge:
                    return 1;
                default: return 0;
            }
        }
    }

    public class YellowBandSprite : StrengthSprite
    {
        private int _typeId = (int)Kinds.YellowBand;
        public override int TypeId => _typeId;
        protected override Func<IChessOperator, bool> OpCondition => op => op.InstanceId == Lasting;
        protected override int BuffRespond(CardState.Cons con, IChessOperator op)
        {
            if (con == CardState.Cons.YellowBand) return Value;
            if (!HuangJinOperator.IsYellowBand(op)) return 0;
            if (con == CardState.Cons.StrengthUp)
            {
                var sprites = Grid.GetScope(IsChallenger).Values
                    .SelectMany(p => p.Terrain.Sprites.Where(s => s.TypeId == TypeId)).ToArray();
                return op.OnSpritesValueConvert(sprites, con);
            }
            return 0;
        }
    }
    /// <summary>
    /// 链环精灵管理精灵实例和伤害转化，分享伤害由buff<see cref="ChainedBuff"/>管理
    /// </summary>
    public class ChainSprite : RelationSprite
    {
        public static IChessPos[] GetChained(ChessboardOperator chessboard, ChessOperator op,
            Func<IChessOperator, bool> chainFunc) =>
            chessboard.GetChainedPos(op, p => p.IsAliveHero && chainFunc(p.Operator)).ToArray();

        public static void UpdateChain(ChessboardOperator chessboard, IChessPos[] chained)
        {
            foreach (var pos in chained)
            {
                //先清除不在本棋格的连环精灵
                foreach (var remoteSprite in chessboard.ChessSprites.Where(s =>
                             s.Lasting == pos.Operator.InstanceId &&
                             s.GetKind() == ThisKind &&
                             pos.IsChallenger == s.IsChallenger &&
                             pos.Pos != s.Pos).ToList())
                    RemoveSprite(chessboard, remoteSprite);

                var localSprite = chessboard.GetSpritesInChessPos(pos.Pos, pos.IsChallenger)
                    .FirstOrDefault(s => s.GetKind() == ThisKind && s.Lasting == pos.Operator.InstanceId);
                if (localSprite == null)
                    AddChain(pos.Operator);
            }

            void AddChain(IChessOperator op) => chessboard.InstanceSprite<ChainSprite>(chessboard.GetChessPos(op), lasting: op.InstanceId, value: 1, actId: -1);
        }

        public static void RemoveSprite(ChessboardOperator chessboard, PosSprite sprite) =>
            chessboard.SpriteRemoveActivity(sprite, ChessProcess.Types.Chessman);

        private const Kinds ThisKind = Kinds.Chained;
        public override int TypeId { get; } = (int)ThisKind;

        protected override Func<IChessOperator, bool> OpCondition => op=> op.InstanceId == Lasting;
        protected override int BuffRespond(CardState.Cons con, IChessOperator op)
        {
            if (con == CardState.Cons.Chained) return Value;
            if (!TieQiOperator.IsChainable(op)) return 0;
            if (con != CardState.Cons.ArmorUp && con != CardState.Cons.StrengthUp) return 0;
            var chainedList = Grid.GetChained(Pos, IsChallenger, ChainedFilter);

            return Grid.GetChessPos(Pos, IsChallenger).Operator
                .OnSpritesValueConvert(chainedList.SelectMany(p => p.Terrain.Sprites).ToArray(), con);
        }

        public static Func<IChessPos, bool> ChainedFilter =>
            p => p.IsAliveHero && p.Terrain.Sprites.Any(s => s.TypeId == (int)ThisKind);

    }

    /// <summary>
    /// 落雷精灵
    /// </summary>
    public class ThunderSprite : RoundSprite
    {
        public override int TypeId { get; } = (int)Kinds.Thunder;
        public override HostType Host => HostType.Round;

        public override void OnActivity(ChessOperator offender, ChessboardOperator chessboard,
            CombatConduct[] conducts, int actId, int skill)
        {
            var target = chessboard.GetChessPos(IsChallenger, Pos);
            if (target.IsPostedAlive)
                chessboard.AppendOpActivity(offender, target, Activity.Intentions.Offensive, conducts, actId, skill);
        }
    }

    /// <summary>
    /// 投射精灵
    /// </summary>
    public class CastSprite : RoundSprite
    {
        private int _typeId = (int)Kinds.CastSprite;

        public override int TypeId => _typeId;

        public override HostType Host => HostType.Round;

        public override void OnActivity(ChessOperator offender, ChessboardOperator chessboard,
            CombatConduct[] conducts, int actId, int skill)
        {
            var target = chessboard.GetChessPos(IsChallenger, Pos);
            if (target.IsPostedAlive)
                chessboard.AppendOpActivity(offender, target, Activity.Intentions.Offensive, conducts, actId, skill);
        }
        public void SetKind(Kinds kind) => _typeId = (int)kind;
    }

    /// <summary>
    /// 箭精灵
    /// </summary>
    public class ArrowSprite : RoundSprite
    {
        public override int TypeId { get; } = (int)Kinds.Arrow;
        public override void OnActivity(ChessOperator offender, ChessboardOperator chessboard, CombatConduct[] conducts, int actId, int skill)
        {
            var target = chessboard.GetChessPos(IsChallenger, Pos);
            if (target.IsPostedAlive)
                chessboard.AppendOpActivity(offender, target, Activity.Intentions.Offensive, conducts, actId, skill);
        }
    }
    
    /// <summary>
    /// 投石精灵
    /// </summary>
    public class CatapultSprite : RoundSprite
    {
        public override int TypeId { get; } = (int)Kinds.Catapult;
        public override HostType Host => HostType.Round;

        public override void OnActivity(ChessOperator offender, ChessboardOperator chessboard,
            CombatConduct[] conducts, int actId, int skill)
        {
            var target = chessboard.GetChessPos(IsChallenger, Pos);
            var targets = chessboard.GetNeighbors(target, false).ToList();
            targets.Add(target);
            foreach (var pos in targets)
                if (pos.IsPostedAlive)
                    chessboard.AppendOpActivity(offender, pos, Activity.Intentions.Offensive, conducts, actId, skill);
        }
    }
    /// <summary>
    /// 滚石精灵
    /// </summary>
    public class RollingStoneSprite : RoundSprite
    {
        public override int TypeId { get; } = (int)Kinds.RollingStone;
    }
    /// <summary>
    /// 滚木精灵
    /// </summary>
    public class RollingWoodSprite : RoundSprite
    {
        public override int TypeId { get; } = (int)Kinds.RollingWood;
    }
    /// <summary>
    /// 阴天精灵
    /// </summary>
    public class ShadySprite : RoundSprite
    {
        public override int TypeId { get; } = (int)Kinds.Shady;
    }

    /// <summary>
    /// 火精灵
    /// </summary>
    public class FireSprite : RoundSprite
    {
        public override int TypeId { get; } = (int)Kinds.FireFlame;

        public override void RoundStart(IChessOperator op, ChessboardOperator chessboard)
        {
            if (!op.IsAlive && chessboard.IsRandomPass(Value)) return;
            chessboard.InstanceChessboardActivity(IsChallenger, op, Activity.Intentions.Self,
                Helper.Singular(CombatConduct.InstanceBuff(Host == HostType.Relation ? Lasting : IsChallenger ? -1 : -2,
                    CardState.Cons.Burn)));
        }
    }
    /// <summary>
    /// 业火精灵
    /// </summary>
    public class YeHuoSprite : FireSprite
    {

        private int[][] FireRings { get; } = new int[][]
        {
            new int[] { 7 },
            new int[] { 2, 5, 6, 10, 11, 12 },
            new int[] { 0, 1, 3, 4, 8, 9, 13, 14, 15, 16, 17 },
            new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 },
        };

        public override int TypeId { get; } = (int)Kinds.YeHuo;

        private int[] GetRelay(ChessboardOperator chessboard)
        {
            var yeHuoState =
                chessboard.StateFlags.FirstOrDefault(s => s.StateId == TypeId && s.IsChallenger == IsChallenger);
            if (yeHuoState == null)
            {
                yeHuoState = new ChessboardOperator.StateFlag
                    { IsChallenger = IsChallenger, State = 0, StateId = TypeId };
                chessboard.StateFlags.Add(yeHuoState);
            }

            return CounterFunc(yeHuoState);

            int[] CounterFunc(ChessboardOperator.StateFlag state)
            {
                var index = state.State;
                if (index >= FireRings.Length)
                    index = FireRings.Length -1;
                var array = FireRings[index];
                state.State++;
                return array;
            }
        }

        public override void OnActivity(ChessOperator offender, ChessboardOperator chessboard, CombatConduct[] conducts, int actId,
            int skill)
        {
            var scope = chessboard.GetRivals(offender, _ => true).ToArray();

            var burnPoses = scope
                .Join(GetRelay(chessboard).Select(i => i), p => p.Pos, i => i, (p, _) => p).ToArray();
            
            for (var index = 0; index < burnPoses.Length; index++)
            {
                var chessPos = burnPoses[index];
                chessboard.InstanceSprite<YeHuoSprite>(chessPos, lasting: 2, value: -1, actId: -1, skill);
                if (chessPos.Operator == null || chessboard.GetStatus(chessPos.Operator).IsDeath) continue;
                chessboard.AppendOpActivity(offender, chessPos, Activity.Intentions.Offensive, conducts, actId: 0, skill: 1);
            }
        }
    }
}