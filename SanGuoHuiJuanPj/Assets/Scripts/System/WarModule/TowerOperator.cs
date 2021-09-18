﻿using System.Linq;
using CorrelateLib;
using NotImplementedException = System.NotImplementedException;

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
        protected override void StartActions()
        {
            var status = Chessboard.GetStatus(this);
            var targets = Chessboard.GetFriendlyNeighbors(this)
                .Where(c => c.IsPostedAlive && c.Operator.CardType == GameCardType.Hero).ToArray();
            if (targets.Length == 0) return;
            var target = targets.OrderBy(c => Chessboard.GetStatus(c.Operator).HpRate).First();
            var tarStat = Chessboard.GetStatus(target.Operator);
            var targetGap = tarStat.MaxHp - tarStat.Hp;
            var healingHp = status.Hp - targetGap > 1 ? targetGap : status.Hp - 1;
            Chessboard.AppendOpActivity(this, target, Activity.Friendly,
                Helper.Singular(CombatConduct.InstanceHeal(healingHp)), 0, 1);
            Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(this), Activity.Self,
                Helper.Singular(CombatConduct.InstanceDamage(healingHp, CombatConduct.FixedDmg)), -1, -1);
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
                    Helper.Singular(CombatConduct.InstanceDamage(Strength)), 0, 1);
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
                    Helper.Singular(CombatConduct.InstanceHeal(Strength)), 0, 1);
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
                    Helper.Singular(CombatConduct.InstanceDamage(damage)), 0, 1);
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
                    Helper.Singular(CombatConduct.InstanceBuff(CardState.Cons.Shield)), 0, 1);
            }
        }
    }

    /// <summary>
    /// 给周围生成属性加成的塔类型
    /// </summary>
    public abstract class NeighborSpriteTowerOperator : TowerOperator
    {
        protected abstract TerrainSprite InstanceSprite(IChessPos pos);

        /// <summary>
        /// 外圈，默认一圈 = 1
        /// </summary>
        protected virtual int Surround { get; } = 1;
        public override void OnPostingTrigger(IChessPos chessPos)
        {
            //除去所有精灵(如果被移位)
            foreach (var sprite in Chessboard.ChessSprites.Where(s => s.Value == InstanceId))
                Chessboard.DepleteSprite(sprite);
            var neighbors = Chessboard.GetNeighbors(chessPos, true, Surround);
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
        protected override TerrainSprite InstanceSprite(IChessPos pos) => Chessboard.InstanceSprite<MeleeStrengthSprite>(pos, TerrainSprite.Strength, value: InstanceId);
    }

    /// <summary>
    /// 号角台
    /// </summary>
    public class HaoJiaoTaiOperator : NeighborSpriteTowerOperator
    {
        protected override TerrainSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<MeleeStrengthSprite>(pos, TerrainSprite.Strength, value: InstanceId);
    }

    /// <summary>
    /// 瞭望台
    /// </summary>
    public class LiaoWangTaiOperator : NeighborSpriteTowerOperator
    {
        protected override TerrainSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<RangeStrengthSprite>(pos, TerrainSprite.Strength, value: InstanceId);
    }
    /// <summary>
    /// 七星坛
    /// </summary>
    public class QiXingTanOperator : NeighborSpriteTowerOperator
    {
        protected override TerrainSprite InstanceSprite(IChessPos pos) => Chessboard.InstanceSprite<MagicForceSprite>(pos,  TerrainSprite.Strength, value: InstanceId);
    }

    /// <summary>
    /// 风神台
    /// </summary>
    public class FengShenTaiOperator : NeighborSpriteTowerOperator
    {
        protected override TerrainSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<DodgeSprite>(pos, TerrainSprite.Dodge, value: InstanceId);
    }

    /// <summary>
    /// 铸铁炉
    /// </summary>
    public class ZhuTieLuOperator : NeighborSpriteTowerOperator
    {
        protected override TerrainSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<CriticalSprite>(pos,  TerrainSprite.Dodge, value: InstanceId);
    }

    /// <summary>
    /// 四方鼎
    /// </summary>
    public class SiFangDingOperator : NeighborSpriteTowerOperator
    {
        protected override TerrainSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<RouseSprite>(pos, TerrainSprite.Rouse, value: InstanceId);
    }
    /// <summary>
    /// 烽火台
    /// </summary>
    public class FengHuoTaiOperator : NeighborSpriteTowerOperator
    {
        protected override TerrainSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<ArmorSprite>(pos, TerrainSprite.Armor, value: InstanceId);
    }
    /// <summary>
    /// 演武场
    /// </summary>
    public class YanWuChangOperator : NeighborSpriteTowerOperator
    {
        protected override TerrainSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<PhysicalSprite>(pos, TerrainSprite.Strength, value: InstanceId);
    }

    /// <summary>
    /// 曹魏旗
    /// </summary>
    public class CaoWeiQiOperator : NeighborSpriteTowerOperator
    {
        protected override TerrainSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<CaoWeiSprite>(pos, TerrainSprite.Strength, value: InstanceId);
    }
    /// <summary>
    /// 蜀汉旗
    /// </summary>
    public class SuHanQiOperator : NeighborSpriteTowerOperator
    {
        protected override TerrainSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<SuHanSprite>(pos, TerrainSprite.Strength, value: InstanceId);
    }
    /// <summary>
    /// 东吴旗
    /// </summary>
    public class DongWuQiOperator : NeighborSpriteTowerOperator
    {
        protected override TerrainSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<DongWuSprite>(pos, TerrainSprite.Strength, value: InstanceId);
    }
    /// <summary>
    /// 骑兵营
    /// </summary>
    public class QiBingYingOperator : NeighborSpriteTowerOperator
    {
        protected override TerrainSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<CavalrySprite>(pos, TerrainSprite.Strength, value: InstanceId);
    }

    /// <summary>
    /// 弓弩营
    /// </summary>
    public class GongNuYingOperator : NeighborSpriteTowerOperator
    {
        protected override TerrainSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<ArrowSprite>(pos, TerrainSprite.Strength, value: InstanceId);
    }

    /// <summary>
    /// 步兵营
    /// </summary>
    public class BuBingYingOperator : NeighborSpriteTowerOperator
    {
        protected override TerrainSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<InfantrySprite>(pos, TerrainSprite.Strength, value: InstanceId);
    }

    /// <summary>
    /// 长持营
    /// </summary>
    public class ChangChiYingOperator : NeighborSpriteTowerOperator
    {
        protected override TerrainSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<StickWeaponSprite>(pos, TerrainSprite.Strength, value: InstanceId);
    }
    /// <summary>
    /// 战船营
    /// </summary>
    public class ZhanChuanYingOperator : NeighborSpriteTowerOperator
    {
        protected override TerrainSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<WarshipSprite>(pos, TerrainSprite.Strength, value: InstanceId);
    }

    /// <summary>
    /// 迷雾阵
    /// </summary>
    public class MiWuOperator : NeighborSpriteTowerOperator
    {
        protected override int Surround => CardId == 18 ? 2 : 1;

        protected override TerrainSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<ForgeSprite>(pos, TerrainSprite.Forge, value: InstanceId);
    }
}