using System.Collections.Generic;
using System.Linq;
using CorrelateLib;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 塔单位棋子处理器
    /// </summary>
    public abstract class TowerOperator : CardOperator
    {
        protected override int OnHealConvert(CombatConduct conduct) => (int) conduct.Total;

        protected override int OnDamageConvert(CombatConduct conduct) => (int) conduct.Total;

        protected override int OnBuffingConvert(CombatConduct conduct) => 0;

        protected override bool DodgeOnAttack(IChessOperator iChessOperator) => false;
    }

    public class BlankTowerOperator : TowerOperator
    {
        public override void StartActions() {}
    }
    /// <summary>
    /// 营寨
    /// </summary>
    public class YingZhaiOperator : TowerOperator
    {
        public override void StartActions()
        {
            var targets = Grid.GetFriendlyNeighbors(this).Where(c=>c.IsPostedAlive && c.Operator.CardType == GameCardType.Hero).ToArray();
            if (targets.Length == 0) return;
            var target = targets.OrderBy(c => Chessboard.GetStatus(c).HpRate).First();
            var status = Chessboard.GetStatus(target);
            var targetGap = status.MaxHp - status.Hp;
            var healingHp = Status.Hp - targetGap > 1 ? targetGap : Status.Hp - 1;
            Status.Hp -= healingHp;
            Chessboard.ActionRespondResult(this, target, Activity.Friendly,
                Singular(CombatConduct.InstanceHeal(healingHp)),1);
        }
    }
    /// <summary>
    /// 投石台
    /// </summary>
    public class TouShiTaiOperator : TowerOperator
    {
        public override void StartActions()
        {
            // 获取对位与周围的单位
            var targets = Grid.GetNeighbors(Pos, !IsChallenger).Where(p => p.IsPostedAlive).ToArray();
            if (targets.Length == 0) return;
            foreach (var target in targets)
            {
                Chessboard.ActionRespondResult(this, target, Activity.Offensive,
                    Singular(CombatConduct.InstanceDamage(Style.Strength)),1);
            }
        }
    }
    /// <summary>
    /// 奏乐台
    /// </summary>
    public class ZouYueTaiOperator : TowerOperator
    {
        public override void StartActions()
        {
            var targets = Grid.GetFriendlyNeighbors(this).Where(p =>
                p.IsAliveHero);
            foreach (var target in targets)
            {
                Chessboard.ActionRespondResult(this, target, Activity.Friendly,
                    Singular(CombatConduct.InstanceHeal(Style.Strength)),1);
            }
        }
    }
    /// <summary>
    /// 箭楼
    /// </summary>
    public class JianLouOperator : TowerOperator
    {

        public override void StartActions()
        {
            var chessPoses = Grid.GetRivalScope(this).Values.Where(p => p.IsPostedAlive).ToArray();
            var maxTargets = DataTable.GetGameValue(18); //箭楼攻击数量
            var pick = chessPoses.Length > maxTargets ? maxTargets : chessPoses.Length;
            //17, 箭楼远射伤害百分比
            var damage = Style.Strength * DataTable.GetGameValue(17);
            var targets = chessPoses.Select(RandomElement.Instance).Pick(pick);
            foreach (var target in targets)
            {
                Chessboard.ActionRespondResult(this, target.Pos, Activity.Offensive,
                    Singular(CombatConduct.InstanceDamage(damage)),1);
            }

        }

        class RandomElement : IWeightElement
        {
            public static RandomElement Instance(IChessPos card) => new RandomElement(card);
            public int Weight => 1;
            public IChessPos Pos { get; }

            public RandomElement(IChessPos pos)
            {
                Pos = pos;
            }
        }

    }
    /// <summary>
    /// 轩辕台
    /// </summary>
    public class XuanYuanTaiOperator : TowerOperator
    {
        public override void StartActions()
        {
            var targets = Grid.GetFriendlyNeighbors(this)
                .Where(p => p.IsPostedAlive &&
                            p.Operator.CardType == GameCardType.Hero)
                .OrderBy(p => p.Operator.Status.GetBuff((int) FightState.Cons.Shield))
                .Take(Style.Strength); //Style.Strength = 最大添加数
            foreach (var target in targets)
            {
                Chessboard.ActionRespondResult(this, target, Activity.FriendlyAttach,
                    Singular(CombatConduct.InstanceBuff(FightState.Cons.Shield)),1);
            }
        }
    }
}