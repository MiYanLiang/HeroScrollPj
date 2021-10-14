using System.Linq;
using CorrelateLib;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 塔单位棋子处理器
    /// </summary>
    public abstract class TowerOperator : CardOperator
    {
        public override int GetDodgeRate() => 0;
    }

    public class BlankTowerOperator : TowerOperator
    {
    }
    /// <summary>
    /// 营寨
    /// </summary>
    public class YingZhaiOperator : TowerOperator
    {
        private int RecoverRate => 20;
        protected override void StartActions()
        {
            var status = Chessboard.GetStatus(this);
            var targets = Chessboard.GetFriendlyNeighbors(this)
                .Where(c => c.IsPostedAlive &&
                            c.Operator.CardType == GameCardType.Hero &&
                            Chessboard.GetStatus(c.Operator).HpRate < 1).ToArray();
            if (targets.Length == 0) return;
            var target = targets.OrderBy(c => Chessboard.GetStatus(c.Operator).HpRate).First();
            var tarStat = Chessboard.GetStatus(target.Operator);
            var targetGap = tarStat.MaxHp - tarStat.Hp;
            var healingHp = status.Hp - targetGap > 1 ? targetGap : status.Hp - 1;
            Chessboard.AppendOpActivity(this, target, Activity.Friendly,
                Helper.Singular(CombatConduct.InstanceHeal(healingHp, InstanceId)), actId: 0, skill: 1);
            Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(this), Activity.Self,
                Helper.Singular(CombatConduct.InstanceDamage(InstanceId, healingHp, CombatConduct.FixedDmg)), actId: -1, skill: -1);
        }

        public override void OnRoundStart()
        {
            var stat = Chessboard.GetStatus(this);
            if (!stat.IsDeath && stat.HpRate < 1)
                Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(this), Activity.Self,
                    Helper.Singular(CombatConduct.InstanceHeal(stat.MaxHp * RecoverRate * 0.01f, InstanceId)), -1, -1);
        }
    }
    /// <summary>
    /// 投石台
    /// </summary>
    public class TouShiTaiOperator : TowerOperator
    {
        protected override void StartActions()
        {
            // 获取对位与周围的单位
            var targets = Chessboard.GetContraNeighbors(this).Where(p => p.IsPostedAlive).ToArray();
            if (targets.Length == 0) return;
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                Chessboard.AppendOpActivity(this, target, Activity.Offensive,
                    Helper.Singular(CombatConduct.InstanceDamage(InstanceId, Strength)), actId: 0, skill: 1);
            }
        }
    }
    /// <summary>
    /// 奏乐台
    /// </summary>
    public class ZouYueTaiOperator : TowerOperator
    {
        protected override void StartActions()
        {
            var targets = Chessboard.GetFriendlyNeighbors(this).Where(p =>
                p.IsAliveHero).ToArray();
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                Chessboard.AppendOpActivity(this, target, Activity.Friendly,
                    Helper.Singular(CombatConduct.InstanceHeal(Strength, InstanceId)), actId: 0, skill: 1);
            }
        }
    }
    /// <summary>
    /// 箭楼
    /// </summary>
    public class JianLouOperator : TowerOperator
    {
        protected override void StartActions()
        {
            var chessPoses = Chessboard.GetRivals(this).ToArray();
            var maxTargets = DataTable.GetGameValue(18); //箭楼攻击数量
            var pick = chessPoses.Length > maxTargets ? maxTargets : chessPoses.Length;
            //17, 箭楼远射伤害百分比
            var damage = Strength * DataTable.GetGameValue(17);
            var targets = chessPoses.Select(RandomElement.Instance).Pick(pick).ToArray();
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                Chessboard.AppendOpActivity(this, target.Pos, Activity.Offensive,
                    Helper.Singular(CombatConduct.InstanceDamage(InstanceId, damage)), actId: 0, skill: 1);
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
        protected override void StartActions()
        {
            var targets = Chessboard.GetFriendlyNeighbors(this)
                .Where(p => p.IsPostedAlive &&
                            p.Operator.CardType == GameCardType.Hero)
                .OrderBy(p => Chessboard.GetCondition(p.Operator, CardState.Cons.Shield))
                .Take(Strength).ToArray(); //Style.Strength = 最大添加数
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                Chessboard.AppendOpActivity(this, target, Activity.Friendly,
                    Helper.Singular(CombatConduct.InstanceBuff(InstanceId, CardState.Cons.Shield)), actId: 0, skill: 1);
            }
        }
    }

    /// <summary>
    /// 给周围生成属性加成的塔类型
    /// </summary>
    public abstract class NeighborSpriteTowerOperator : TowerOperator
    {
        protected abstract PosSprite InstanceSprite(IChessPos pos);

        /// <summary>
        /// 外圈，默认一圈 = 1
        /// </summary>
        protected virtual int Surround { get; } = 1;

        public override void OnPlaceInvocation()
        {
            //除去所有精灵(如果被移位)
            foreach (var sprite in Chessboard.ChessSprites.Where(s => s.Value == InstanceId))
                Chessboard.UpdateRemovable(sprite);
            var neighbors = Chessboard.GetNeighbors(Chessboard.GetChessPos(this), true, Surround);
            //在周围生成精灵
            foreach (var pos in neighbors)
                InstanceSprite(pos);
        }
    }

    /// <summary>
    /// 战鼓台
    /// </summary>
    public class ZhanGuTaiOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<MeleeStrengthSprite>(pos, PosSprite.Strength, lasting: InstanceId, value: Strength, actId: -1);
    }

    /// <summary>
    /// 号角台
    /// </summary>
    public class HaoJiaoTaiOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<MeleeStrengthSprite>(pos, PosSprite.Strength, InstanceId, value: Strength, actId: -1);
    }

    /// <summary>
    /// 瞭望台
    /// </summary>
    public class LiaoWangTaiOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<RangeStrengthSprite>(pos, PosSprite.Strength, InstanceId, Strength, -1);
    }
    /// <summary>
    /// 七星坛
    /// </summary>
    public class QiXingTanOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) => Chessboard.InstanceSprite<MagicForceSprite>(pos, PosSprite.Strength, InstanceId, Strength, -1);
    }

    /// <summary>
    /// 风神台
    /// </summary>
    public class FengShenTaiOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<DodgeSprite>(pos, PosSprite.Dodge, InstanceId, Strength, -1);
    }

    /// <summary>
    /// 铸铁炉
    /// </summary>
    public class ZhuTieLuOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<CriticalSprite>(pos, PosSprite.Dodge, InstanceId, Strength, -1);
    }

    /// <summary>
    /// 四方鼎
    /// </summary>
    public class SiFangDingOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<RouseSprite>(pos, PosSprite.Rouse, InstanceId, Strength, -1);
    }
    /// <summary>
    /// 烽火台
    /// </summary>
    public class FengHuoTaiOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<ArmorSprite>(pos, PosSprite.Armor, InstanceId, Strength, -1);
    }
    /// <summary>
    /// 演武场
    /// </summary>
    public class YanWuChangOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<PhysicalSprite>(pos, PosSprite.Strength, InstanceId, Strength, -1);
    }

    /// <summary>
    /// 曹魏旗
    /// </summary>
    public class CaoWeiQiOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<CaoWeiSprite>(pos, PosSprite.Strength, InstanceId, Strength, -1);
    }
    /// <summary>
    /// 蜀汉旗
    /// </summary>
    public class SuHanQiOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<SuHanSprite>(pos, PosSprite.Strength, InstanceId, Strength, -1);
    }
    /// <summary>
    /// 东吴旗
    /// </summary>
    public class DongWuQiOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<DongWuSprite>(pos, PosSprite.Strength, InstanceId, Strength, -1);
    }
    /// <summary>
    /// 骑兵营
    /// </summary>
    public class QiBingYingOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<CavalrySprite>(pos, PosSprite.Strength, InstanceId, Strength, -1);
    }

    /// <summary>
    /// 弓弩营
    /// </summary>
    public class GongNuYingOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<ArrowSprite>(pos, PosSprite.Strength, InstanceId, Strength, -1);
    }

    /// <summary>
    /// 步兵营
    /// </summary>
    public class BuBingYingOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<InfantrySprite>(pos, PosSprite.Strength, InstanceId, Strength, -1);
    }

    /// <summary>
    /// 长持营
    /// </summary>
    public class ChangChiYingOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<StickWeaponSprite>(pos, PosSprite.Strength, InstanceId, Strength, -1);
    }
    /// <summary>
    /// 战船营
    /// </summary>
    public class ZhanChuanYingOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<WarshipSprite>(pos, PosSprite.Strength, InstanceId, Strength, -1);
    }

    /// <summary>
    /// 迷雾阵
    /// </summary>
    public class MiWuOperator : NeighborSpriteTowerOperator
    {
        protected override int Surround => CardId == 18 ? 2 : 1;

        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<ForgeSprite>(pos, PosSprite.Forge, InstanceId, Strength, -1);
    }
}