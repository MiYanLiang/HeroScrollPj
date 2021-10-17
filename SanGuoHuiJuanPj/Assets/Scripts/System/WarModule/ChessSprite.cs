using System;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using Newtonsoft.Json;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 棋格精灵，存在地块上执行任务。与<see cref="BuffOperator"/>不一样的是，它是挂在棋格上的状态
    /// </summary>
    public abstract class PosSprite
    {
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
        /// <summary>
        /// 爆炸+火焰
        /// </summary>
        public const int YeHuo = 6;
        /// <summary>
        /// 一般火焰
        /// </summary>
        public const int FireFlame = 7;
        /// <summary>
        /// 落雷
        /// </summary>
        public const int Thunder = 3;
        /// <summary>
        /// 投射精灵， 
        /// </summary>
        public const int CastSprite = 4;
        /// <summary>
        /// 地震
        /// </summary>
        public const int Eerthquake = 8;
        /// <summary>
        /// 迷雾精灵(依赖类)
        /// </summary>
        public const int Forge = 20;
        public const int Strength = 10;
        public const int Armor = 14;
        public const int Dodge = 11;
        public const int Critical = 12;
        public const int Rouse = 13;

        public static T Instance<T>(ChessboardOperator chessboardOp,int instanceId,int lasting,int value ,int typeId, int pos,bool isChallenger)
            where T : PosSprite, new()
        {
            return new T
            {
                Grid = chessboardOp.Grid,
                InstanceId = instanceId,
                Value = value,
                Pos = pos,
                IsChallenger = isChallenger,
                TypeId = typeId,
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
        public int TypeId { get; set; }
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

        public override string ToString()
        {
            var hostText = Host == HostType.Relation ? $"宿主({Lasting})" : $"回合({Lasting})";
            var challengerText = IsChallenger ? $"玩家({Pos})" : $"对手({Pos})";
            var typeText = "未定";
            switch (TypeId)
            {
                case YeHuo: typeText = "业火";break;
                case Forge: typeText = "迷雾";break;
                case Thunder: typeText = "落雷";break;
            }
            return $"精灵({InstanceId})({typeText}).{hostText} [{Value}] Pos:{challengerText}";
        }

        public virtual ActivityResult OnActivity(ChessOperator offender, ChessboardOperator chessboard,
            CombatConduct[] conducts,int actId,int skill) => null;
        public virtual void RoundStart(IChessOperator op, ChessboardOperator chessboard)
        {
        }
    }

    public abstract class RoundSprite : PosSprite
    {
        public override HostType Host { get; } = HostType.Round;
    }
    // 依赖型精灵
    public abstract class RelationSprite : PosSprite
    {
        public override HostType Host { get; } = HostType.Relation;
        /// <summary>
        /// 代理特殊条件
        /// </summary>
        protected abstract Func<IChessOperator, bool> OpCondition { get; }
        public override int ServedBuff(CardState.Cons buff, IChessOperator op) => OpCondition(op) ? BuffRespond(buff,op) : 0;
        //依赖类不执行(回合)消耗
        protected abstract int BuffRespond(CardState.Cons con, IChessOperator op);
    }
    /// <summary>
    /// 力量精灵
    /// </summary>
    public abstract class StrengthSprite : RelationSprite
    {
        protected override int BuffRespond(CardState.Cons con, IChessOperator op) => con == CardState.Cons.StrengthUp ? Value : 0;
    }
    public class ArmorSprite : RelationSprite
    {
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
    public class ArrowSprite : StrengthSprite
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
    }
    /// <summary>
    /// 暴击精灵
    /// </summary>
    public class CriticalSprite : RelationSprite
    {
        protected override Func<IChessOperator, bool> OpCondition => op => op.CardType == GameCardType.Hero;

        protected override int BuffRespond(CardState.Cons con, IChessOperator op) => con == CardState.Cons.CriticalUp ? Value : 0;
    }
    /// <summary>
    /// 会心精灵
    /// </summary>
    public class RouseSprite : RelationSprite
    {
        protected override Func<IChessOperator, bool> OpCondition => op => op.CardType == GameCardType.Hero;
        protected override int BuffRespond(CardState.Cons con, IChessOperator op) => con == CardState.Cons.RouseUp ? Value : 0;
    }
    /// <summary>
    /// 迷雾精灵
    /// </summary>
    public class ForgeSprite : DodgeSprite
    {
        protected override int BuffRespond(CardState.Cons con, IChessOperator op) =>
            con == CardState.Cons.DodgeUp || 
            con == CardState.Cons.Forge ? Value : 0;
    }

    public class YellowBandSprite : StrengthSprite
    {
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
    //链环精灵管理伤害转化，分享伤害由buff管理
    public class ChainSprite : RelationSprite
    {
        private const int ChainedId = (int)CardState.Cons.Chained;
        protected override Func<IChessOperator, bool> OpCondition => op=> op.InstanceId == Lasting;
        protected override int BuffRespond(CardState.Cons con, IChessOperator op)
        {
            if (con == CardState.Cons.Chained) return Value;
            if (!TieQiOperator.IsChainable(op)) return 0;
            if (con != CardState.Cons.ArmorUp && 
                con != CardState.Cons.StrengthUp) return 0;
            var chainedList = Grid.GetChained(Pos, IsChallenger, ChainedFilter);

            return Grid.GetChessPos(Pos,IsChallenger).Operator
                .OnSpritesValueConvert(chainedList.SelectMany(p => p.Terrain.Sprites).ToArray(), con);
        }
        public static Func<IChessPos, bool> ChainedFilter =>
            p => p.IsAliveHero && p.Terrain.Sprites.Any(s => s.TypeId == ChainedId);
    }

    /// <summary>
    /// 投射精灵
    /// </summary>
    public class CastSprite : RoundSprite
    {
        public override HostType Host => HostType.Round;

        public override ActivityResult OnActivity(ChessOperator offender, ChessboardOperator chessboard,
            CombatConduct[] conducts, int actId, int skill)
        {
            var target = chessboard.GetChessPos(IsChallenger, Pos);
            return target.IsPostedAlive
                ? chessboard.AppendOpActivity(offender, target, Activity.Offensive, conducts, actId, skill)
                : null;
        }
    }

    /// <summary>
    /// 火精灵
    /// </summary>
    public class FireSprite : RoundSprite
    {
        public override void RoundStart(IChessOperator op, ChessboardOperator chessboard)
        {
            if (!op.IsAlive && chessboard.IsRandomPass(Value)) return;
            chessboard.InstanceChessboardActivity(IsChallenger, op, Activity.Self,
                Helper.Singular(CombatConduct.InstanceBuff(Host == HostType.Relation ? Lasting : IsChallenger ? -1 : -2,
                    CardState.Cons.Burn)));
        }
    }

}