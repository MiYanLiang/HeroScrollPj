using System;
using System.Collections.Generic;
using CorrelateLib;
using Newtonsoft.Json;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 棋格精灵，存在地块上执行任务。与<see cref="BuffOperator"/>不一样的是，它是挂在棋格上的状态
    /// </summary>
    public abstract class TerrainSprite
    {
        /// <summary>
        /// 类型
        /// </summary>
        public enum LastingType
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
        /// 统帅-业火精灵id(回合制)
        /// </summary>
        public const int YeHuo = 6;
        /// <summary>
        /// 迷雾精灵(依赖类)
        /// </summary>
        public const int Forge = 20;
        public const int Strength = 10;
        public const int Armor = 14;
        public const int Dodge = 11;
        public const int Critical = 12;
        public const int Rouse = 13;

        public static T Instance<T>(int instanceId,LastingType lasting,int value,int typeId ,int pos, bool isChallenger)
            where T : TerrainSprite, new()
        {
            return new T
            {
                InstanceId = instanceId,
                Lasting = lasting,
                Value = value,
                Pos = pos,
                IsChallenger = isChallenger,
                TypeId = typeId
            };
        }

        public int InstanceId { get; set; }
        /// <summary>
        /// 类型标签，用来识别单位类型
        /// </summary>
        public int TypeId { get; set; }
        public LastingType Lasting {  get; set; }
        /// <summary>
        /// 宿主<see cref="ChessOperator.InstanceId"/>
        /// -1 =回合类型，正数：棋子id
        /// </summary>
        //public int Host { get; set; }
        public int Value { get; set; }
        /// <summary>
        /// 棋格
        /// </summary>
        public int Pos { get; set; }

        public bool IsChallenger { get; set; }

        protected TerrainSprite()
        {
            
        }

        /// <summary>
        /// 对不同的buff给出增/减值
        /// </summary>
        /// <param name="buff"></param>
        /// <param name="op"></param>
        /// <returns></returns>
        public virtual int ServedBuff(CardState.Cons buff,IChessOperator op) => 0;
        ///// <summary>
        ///// 回合开始动作
        ///// </summary>
        //public virtual void RoundStart(){}
        ///// <summary>
        ///// 回合结束动作
        ///// </summary>
        //public virtual void RoundEnd(){}

        public virtual int GetBuff(CardState.Cons con) => 0;

        public override string ToString()
        {
            var hostText = Lasting == LastingType.Relation ? $"宿主({Value})" : $"回合({Value})";
            var challengerText = IsChallenger ? $"玩家({Pos})" : $"对手({Pos})";
            var typeText = "未定";
            switch (TypeId)
            {
                case YeHuo: typeText = "业火";break;
                case Forge: typeText = "迷雾";break;
            }
            return $"精灵({InstanceId})({typeText}).{hostText} Pos:{challengerText}";
        }

        public virtual CombatConduct[] RoundStart(IChessOperator op, ChessboardOperator chessboard) => null;
    }

    /// <summary>
    /// 火精灵
    /// </summary>
    public class FireSprite : TerrainSprite
    {
        public int BuffRatio { get; } = 10;
        public int Damage { get; } = 100;
        public override CombatConduct[] RoundStart(IChessOperator op, ChessboardOperator chessboard)
        {
            if (!op.IsAlive) return null;
            var combat = new List<CombatConduct> { CombatConduct.InstanceDamage(Damage, CombatConduct.FireDmg) };
            if (op.CardType == GameCardType.Hero && chessboard.IsRandomPass(BuffRatio))
                combat.Add(CombatConduct.InstanceBuff(CardState.Cons.Burn));
            return combat.ToArray();
        }
    }
    // 依赖型精灵
    public abstract class RelationSprite : TerrainSprite
    {
        //依赖类不执行(回合)消耗
    }
    /// <summary>
    /// 力量精灵
    /// </summary>
    public abstract class StrengthSprite : RelationSprite
    {
        /// <summary>
        /// 代理特殊条件
        /// </summary>
        protected abstract Func<IChessOperator, bool> OpCondition { get; }
        public override int ServedBuff(CardState.Cons buff, IChessOperator op) => OpCondition(op) ? GetBuff(buff) : 0;
        public override int GetBuff(CardState.Cons con) => con == CardState.Cons.StrengthUp ? Value : 0;
    }
    public class ArmorSprite : RelationSprite
    {

        public override int ServedBuff(CardState.Cons buff, IChessOperator op) => GetBuff(buff);

        public override int GetBuff(CardState.Cons con) => con == CardState.Cons.DefendUp ? Value : 0;
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
        protected override Func<IChessOperator, bool> OpCondition => op => op.Style.Element <= 0 && op.Style.Element != CombatConduct.FixedDmg;
    }
    /// <summary>
    /// 法力精灵
    /// </summary>
    public class MagicForceSprite : StrengthSprite
    {
        protected override Func<IChessOperator, bool> OpCondition => op => op.Style.Element > 0;
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
        public override int ServedBuff(CardState.Cons buff, IChessOperator op) =>
            op.CardType == GameCardType.Hero ? GetBuff(buff) : 0;
        public override int GetBuff(CardState.Cons con) => con == CardState.Cons.DodgeUp ? Value : 0;
    }
    /// <summary>
    /// 暴击精灵
    /// </summary>
    public class CriticalSprite : RelationSprite
    {
        public override int ServedBuff(CardState.Cons buff, IChessOperator op) =>
            op.CardType == GameCardType.Hero ? GetBuff(buff) : 0;
        public override int GetBuff(CardState.Cons con) => con == CardState.Cons.CriticalUp ? Value : 0;
    }
    /// <summary>
    /// 会心精灵
    /// </summary>
    public class RouseSprite : RelationSprite
    {
        public override int ServedBuff(CardState.Cons buff, IChessOperator op) =>
            op.CardType == GameCardType.Hero ? GetBuff(buff) : 0;
        public override int GetBuff(CardState.Cons con) => con == CardState.Cons.RouseUp ? Value : 0;
    }
    /// <summary>
    /// 迷雾精灵
    /// </summary>
    public class ForgeSprite : DodgeSprite
    {
    }

}