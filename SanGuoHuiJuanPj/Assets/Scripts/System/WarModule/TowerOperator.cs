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
        public override int GetDodgeRate() => 0;

        protected CombatConduct InstanceMechanicalDamage(float damage) => CombatConduct.InstanceDamage(InstanceId, damage, CombatConduct.MechanicalDmg);
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
            Chessboard.AppendOpActivity(this, target, Activity.Intentions.Friendly,
                Helper.Singular(CombatConduct.InstanceHeal(healingHp, InstanceId)), actId: 0, skill: 1);
            Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(this), Activity.Intentions.Self,
                Helper.Singular(CombatConduct.InstanceDamage(InstanceId, healingHp, CombatConduct.FixedDmg)), actId: -1, skill: -1);
        }

        public override void OnRoundStart()
        {
            var stat = Chessboard.GetStatus(this);
            if (!stat.IsDeath && stat.HpRate < 1)
                Chessboard.AppendOpActivity(this, Chessboard.GetChessPos(this), Activity.Intentions.Self,
                    Helper.Singular(CombatConduct.InstanceHeal(stat.MaxHp * RecoverRate * 0.01f, InstanceId)), actId: -1, skill: 2);
        }
    }
    /// <summary>
    /// 抛石台
    /// </summary>
    public class PaoShiTaiOperator : TowerOperator
    {
        protected override void StartActions()
        {
            // 获取对位与周围的单位
            var target = Chessboard.GetTargetByMode(this, ChessboardOperator.Targeting.Contra);
            var combat = Helper.Singular(InstanceMechanicalDamage(StateDamage()));
            Chessboard.DelegateSpriteActivity<CatapultSprite>(this, target, combat, actId: 0, skill: 1);
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
                Chessboard.AppendOpActivity(this, target, Activity.Intentions.Friendly,
                    Helper.Singular(CombatConduct.InstanceHeal(StateDamage(), InstanceId)), actId: 0, skill: 1);
            }
        }
    }
    /// <summary>
    /// 箭楼
    /// </summary>
    public class JianLouOperator : TowerOperator
    {
        private int Targets => 7;
        private int DamageRate => 100;
        protected override void StartActions()
        {
            var chessPoses = Chessboard.GetRivals(this, _ => true)
                .Select(p => new { p, random = Chessboard.Randomize(3) })
                .OrderBy(p => p.random).Take(Targets).ToArray();
            //17, 箭楼远射伤害百分比
            var damage = InstanceMechanicalDamage(StateDamage() * DamageRate * 0.01f);
            for (var i = 0; i < chessPoses.Length; i++)
            {
                var target = chessPoses[i].p;
                Chessboard.DelegateSpriteActivity<ArrowSprite>(this, target, Helper.Singular(damage), 0, 1);
            }
        }
    }
    /// <summary>
    /// 轩辕台
    /// </summary>
    public class XuanYuanTaiOperator : TowerOperator
    {
        private int GetTarget() => 1 + (Level - 1) / 2;
        protected override void StartActions()
        {
            var targets = Chessboard.GetFriendlyNeighbors(this)
                .Where(p => p.IsPostedAlive &&
                            p.Operator.CardType == GameCardType.Hero)
                .OrderBy(p => Chessboard.GetCondition(p.Operator, CardState.Cons.Shield))
                .Take(GetTarget()).ToArray();
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                Chessboard.AppendOpActivity(this, target, Activity.Intentions.Friendly,
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

        public override void OnRoundStart()
        {
            var neighbors = Chessboard.GetNeighbors(Chessboard.GetChessPos(this), true, Surround).ToArray();
            var sprites = new List<PosSprite>();
            foreach (var neighbor in neighbors)
            {
                var sp = neighbor.Terrain.Sprites.FirstOrDefault(s =>
                    s.Host == PosSprite.HostType.Relation && s.Lasting == InstanceId);
                if (sp != null)
                {
                    sprites.Add(sp);
                    continue;
                }
                sprites.Add(InstanceSprite(neighbor));
            }

            //除去所有精灵(如果被移位)
            var removeList = Chessboard.ChessSprites
                .Where(s => s.Host == PosSprite.HostType.Relation && s.Lasting == InstanceId)
                .Except(sprites).ToArray();
            foreach (var remove in removeList) 
                Chessboard.UpdateRemovableSprite(remove);
        }
    }

    /// <summary>
    /// 战鼓台
    /// </summary>
    public class ZhanGuTaiOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<MeleeStrengthSprite>(pos, lasting: InstanceId, value: StateIntelligent(), actId: -1);
    }

    /// <summary>
    /// 号角台
    /// </summary>
    public class HaoJiaoTaiOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<MeleeStrengthSprite>(pos, lasting: InstanceId, value: StateIntelligent(), actId: -1);
    }

    /// <summary>
    /// 瞭望台
    /// </summary>
    public class LiaoWangTaiOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<RangeStrengthSprite>(pos, lasting: InstanceId, value: StateIntelligent(), actId: -1);
    }
    /// <summary>
    /// 七星坛
    /// </summary>
    public class QiXingTanOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) => Chessboard.InstanceSprite<MagicForceSprite>(pos, lasting: InstanceId, value: StateIntelligent(), actId: -1);
    }

    /// <summary>
    /// 风神台
    /// </summary>
    public class FengShenTaiOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<DodgeSprite>(pos, lasting: InstanceId, value: StateIntelligent(), actId: -1);
    }

    /// <summary>
    /// 铸铁炉
    /// </summary>
    public class ZhuTieLuOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<CriticalSprite>(pos, lasting: InstanceId, value: StateIntelligent(), actId: -1);
    }

    /// <summary>
    /// 四方鼎
    /// </summary>
    public class SiFangDingOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<RouseSprite>(pos, lasting: InstanceId, value: StateIntelligent(), actId: -1);
    }
    /// <summary>
    /// 烽火台
    /// </summary>
    public class FengHuoTaiOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<ArmorSprite>(pos, lasting: InstanceId, value: StateIntelligent(), actId: -1);
    }
    /// <summary>
    /// 演武场
    /// </summary>
    public class YanWuChangOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<PhysicalSprite>(pos, lasting: InstanceId, value: StateIntelligent(), actId: -1);
    }

    /// <summary>
    /// 曹魏旗
    /// </summary>
    public class CaoWeiQiOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<CaoWeiSprite>(pos, lasting: InstanceId, value: StateIntelligent(), actId: -1);
    }
    /// <summary>
    /// 蜀汉旗
    /// </summary>
    public class SuHanQiOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<SuHanSprite>(pos, lasting: InstanceId, value: StateIntelligent(), actId: -1);
    }
    /// <summary>
    /// 东吴旗
    /// </summary>
    public class DongWuQiOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<DongWuSprite>(pos, lasting: InstanceId, value: StateIntelligent(), actId: -1);
    }
    /// <summary>
    /// 骑兵营
    /// </summary>
    public class QiBingYingOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<CavalrySprite>(pos, lasting: InstanceId, value: StateIntelligent(), actId: -1);
    }

    /// <summary>
    /// 弓弩营
    /// </summary>
    public class GongNuYingOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<ArrowUpSprite>(pos, lasting: InstanceId, value: StateIntelligent(), actId: -1);
    }

    /// <summary>
    /// 步兵营
    /// </summary>
    public class BuBingYingOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<InfantrySprite>(pos, lasting: InstanceId, value: StateIntelligent(), actId: -1);
    }

    /// <summary>
    /// 长持营
    /// </summary>
    public class ChangChiYingOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<StickWeaponSprite>(pos, lasting: InstanceId, value: StateIntelligent(), actId: -1);
    }
    /// <summary>
    /// 战船营
    /// </summary>
    public class ZhanChuanYingOperator : NeighborSpriteTowerOperator
    {
        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<WarshipSprite>(pos, lasting: InstanceId, value: StateIntelligent(), actId: -1);
    }

    /// <summary>
    /// 迷雾阵
    /// </summary>
    public class MiWuOperator : NeighborSpriteTowerOperator
    {
        protected override int Surround => Style.Military == 17 ? 2 : 1;

        protected override PosSprite InstanceSprite(IChessPos pos) =>
            Chessboard.InstanceSprite<ForgeSprite>(pos, lasting: InstanceId, value: StateIntelligent(), actId: -1);
    }
}