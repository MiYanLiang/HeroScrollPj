using System.Collections.Generic;
using System.Linq;
using CorrelateLib;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 羁绊代理
    /// </summary>
    public abstract class BondOperator
    {
        /// <summary>
        /// 羁绊绑定列表。每一列代表一个身份，而必须满足每一个id才会点亮羁绊能力
        /// </summary>
        public int[] BondList { get; }
        public int BondId { get; }
        protected ChessboardOperator Chessboard { get; }

        protected BondOperator(JiBanTable jiBan, ChessboardOperator chessboard)
        {
            BondList = jiBan.Cards.Select(c => c.CardId).ToArray();
            BondId = jiBan.Id;
            Chessboard = chessboard;
        }

        protected bool IsInBondList(IChessOperator op) => BondList.Contains(op.CardId);

        /// <summary>
        /// 根据比率给出羁绊者的平均伤害加成
        /// </summary>
        /// <param name="ops"></param>
        /// <param name="rate"></param>
        /// <returns></returns>
        protected int AverageAdditionalDamageFromBonds(ChessOperator[] ops, int rate) =>
            (int)(ops.Average(o => Chessboard.GetHeroBuffDamage(o)) * 0.01f * rate);
        public void OnRoundStart(IEnumerable<ChessOperator> list)
        {
            var ops = list.ToArray();
            var first = ops.First();
            foreach (var op in ops)
            {
                var conducts = RoundStartConducts(op);
                if (conducts == null || conducts.Length == 0) continue;
                Chessboard.InstanceJiBanActivity(BondId, op.IsChallenger, op, Activity.Friendly, conducts);
            }

            var rivals = Chessboard.GetRivals(first,
                pos => pos.IsPostedAlive &&
                       pos.IsAliveHero).Select(p => p.Operator);

            foreach (var rival in rivals)
            {
                var result = RoundStartRivalConduct(ops, rival);
                if (result == null) return;
                if (!result.PushBackPos)
                {
                    var conducts = result.Conducts;
                    if (conducts == null || conducts.Length == 0) continue;
                    Chessboard.InstanceJiBanActivity(BondId, first.IsChallenger, rival, Activity.Offensive, conducts);
                    continue;
                }

                var backPos = Chessboard.BackPos(Chessboard.GetChessPos(rival));
                if (backPos == null || backPos.IsPostedAlive) continue;
                Chessboard.InstanceJiBanActivity(BondId, first.IsChallenger, rival, Activity.Offensive,
                    result.Conducts, backPos.Pos);
            }
        }

        protected virtual ConductResult RoundStartRivalConduct(ChessOperator[] chessOperators, IChessOperator rival) => null;

        protected abstract CombatConduct[] RoundStartConducts(ChessOperator op);
        public virtual int OnDamageAddOn(ChessOperator[] ops, ChessOperator op, int damage) => 0;
        public virtual int OnBuffingRatioAddOn(ChessOperator[] ops, ChessOperator op) => 0;

        protected class ConductResult
        {
            public int JbId;
            public bool PushBackPos;
            public CombatConduct[] Conducts;

            public ConductResult()
            {
            }

            public ConductResult(CombatConduct[] conducts)
            {
                Conducts = conducts;
            }
        }
    }

    /// <summary>
    /// 桃园结义
    /// </summary>
    public class TaoYuanJieYi : BondOperator
    {
        public int ShenZhuRatio { get; } = DataTable.GetGameValue(134);
        public int DamageAdditionRatio { get; } = DataTable.GetGameValue(148);
        public TaoYuanJieYi(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override ConductResult RoundStartRivalConduct(ChessOperator[] chessOperators, IChessOperator rival) => null;

        protected override CombatConduct[] RoundStartConducts(ChessOperator op)
        {
            if (!IsInBondList(op)) return null;
            if (!Chessboard.IsRandomPass(ShenZhuRatio) ||
                Chessboard.GetCondition(op, CardState.Cons.ShenZhu) > 0) return null;
            return Helper.Singular(CombatConduct.InstanceBuff(op.IsChallenger ? -1 : -2, CardState.Cons.ShenZhu));
        }

        public override int OnDamageAddOn(ChessOperator[] ops, ChessOperator op, int damage)
        {
            var bonds = ops.Where(IsInBondList).ToArray();
            if (bonds.Length == 0) return 0;
            return (int)(damage * DamageAdditionRatio * 0.01f);
        }
    }
    /// <summary>
    /// 五虎上将
    /// </summary>
    public class WuHuSHangJiang : BondOperator
    {
        public WuHuSHangJiang(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override ConductResult RoundStartRivalConduct(ChessOperator[] ops, IChessOperator rival)
        {
            var damage =
                AverageAdditionalDamageFromBonds(ops.Where(IsInBondList).ToArray(), DataTable.GetGameValue(149));
            var conducts = new List<CombatConduct>
                { CombatConduct.InstanceDamage(ops.First().IsChallenger ? -1 : -2, damage) };
            if (Chessboard.IsRandomPass(DataTable.GetGameValue(150)))
                conducts.Add(CombatConduct.InstanceBuff(ops.First().IsChallenger ? -1 : -2, CardState.Cons.Cowardly));
            return new ConductResult(conducts.ToArray());
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op) => null;
    }
    /// <summary>
    /// 卧龙凤雏
    /// </summary>
    public class WoLongFengChu : BondOperator
    {
        public int ShenZhuRatio { get; } = DataTable.GetGameValue(136);
        public WoLongFengChu(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op)
        {
            if (!IsInBondList(op)) return null;
            return Chessboard.IsRandomPass(ShenZhuRatio) ?
                Helper.Singular(CombatConduct.InstanceBuff(op.IsChallenger ? -1 : -2, CardState.Cons.ShenZhu)) : null;
        }

        public override int OnDamageAddOn(ChessOperator[] ops, ChessOperator op, int damage)
        {
            if (op.IsRangeHero) return (int)(damage * DataTable.GetGameValue(151) * 0.01f);
            return 0;
        }
    }
    /// <summary>
    /// 虎痴恶来
    /// </summary>
    public class HuChiELai : BondOperator
    {
        public HuChiELai(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op)
        {
            if (IsInBondList(op) &&
                Chessboard.IsRandomPass(DataTable.GetGameValue(137)))
                return Helper.Singular(CombatConduct.InstanceBuff(op.IsChallenger ? -1 : -2, CardState.Cons.Shield));
            return null;
        }

        public override int OnDamageAddOn(ChessOperator[] ops, ChessOperator op, int damage)
        {
            return op.Style.Troop == 1
                ? (int)(damage * DataTable.GetGameValue(152) * 0.01f)
                : 0;
        }
    }
    /// <summary>
    /// 五子良将
    /// </summary>
    public class WuZiLiangJiang : BondOperator
    {
        public WuZiLiangJiang(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op)
        {
            if (!IsInBondList(op) ||
                !Chessboard.IsRandomPass(DataTable.GetGameValue(138)))
                return Helper.Singular(CombatConduct.InstanceBuff(op.IsChallenger ? -1 : -2, CardState.Cons.Shield));
            return null;
        }

        public override int OnDamageAddOn(ChessOperator[] ops, ChessOperator op, int damage)
        {
            if (op.IsMeleeHero) return (int)(damage * DataTable.GetGameValue(153) * 0.01f);
            return 0;
        }
    }

    /// <summary>
    /// 魏五谋士
    /// </summary>
    public class WeiWuMouShi : BondOperator
    {
        public WeiWuMouShi(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op)
        {
            //干扰系
            if (op.Style.ArmedType != 12 ||
                !Chessboard.IsRandomPass(DataTable.GetGameValue(139))) return null;
            return Helper.Singular(CombatConduct.InstanceBuff(op.IsChallenger ? -1 : -2, CardState.Cons.Neizhu));
        }

        protected override ConductResult RoundStartRivalConduct(ChessOperator[] ops, IChessOperator rival)
        {
            var damage =
                AverageAdditionalDamageFromBonds(ops.Where(IsInBondList).ToArray(), DataTable.GetGameValue(154));
            return new ConductResult(
                Helper.Singular(CombatConduct.InstanceDamage(ops.First().IsChallenger ? -1 : -2, damage, CombatConduct.BasicMagicDmg)));
        }
    }
    /// <summary>
    /// 虎踞江东
    /// </summary>
    public class HuJuJiangDong : BondOperator
    {
        public HuJuJiangDong(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        public override int OnDamageAddOn(ChessOperator[] ops, ChessOperator op, int damage)
        {
            if (op.Style.Troop != 2) return 0;
            return (int)(damage * DataTable.GetGameValue(157) * 0.01f);
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op)
        {
            if (!IsInBondList(op))
                return null;
            return Helper.Singular(CombatConduct.InstanceBuff(op.IsChallenger ? -1 : -2, CardState.Cons.ShenZhu));
        }
    }
    /// <summary>
    /// 水师都督
    /// </summary>
    public class ShuiShiDuDu : BondOperator
    {
        public ShuiShiDuDu(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override ConductResult RoundStartRivalConduct(ChessOperator[] ops, IChessOperator rival)
        {
            var damage =
                AverageAdditionalDamageFromBonds(ops.Where(IsInBondList).ToArray(), DataTable.GetGameValue(154));
            return new ConductResult(Helper.Singular(CombatConduct.InstanceDamage(ops.First().IsChallenger ? -1 : -2, damage, CombatConduct.WaterDmg)));
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op) => null;

        public override int OnDamageAddOn(ChessOperator[] ops, ChessOperator op, int damage)
        {
            if (op.Style.ArmedType == 8)//水师都督激活时战船系武将伤害加成50%
                return (int)(damage * DataTable.GetGameValue(160) * 0.01f);
            return 0;
        }
    }
    /// <summary>
    /// 天作之合
    /// </summary>
    public class TianZuoZhiHe : BondOperator
    {
        public TianZuoZhiHe(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op)
        {
            if (IsInBondList(op) &&
                Chessboard.IsRandomPass(DataTable.GetGameValue(142)))
                return Helper.Singular(CombatConduct.InstanceBuff(op.IsChallenger ? -1 : -2, CardState.Cons.ShenZhu));
            return null;
        }
    }
    /// <summary>
    /// 河北四庭柱
    /// </summary>
    public class HeBeiSiTingZhu : BondOperator
    {
        public HeBeiSiTingZhu(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op)
        {
            if (IsInBondList(op) &&
                Chessboard.IsRandomPass(DataTable.GetGameValue(143)))
                return Helper.Singular(CombatConduct.InstanceBuff(op.IsChallenger ? -1 : -2, CardState.Cons.Shield));
            return null;
        }

        public override int OnDamageAddOn(ChessOperator[] ops, ChessOperator op, int damage)
        {
            if (op.Style.Troop == 3)
                return damage * DataTable.GetGameValue(163);
            return 0;
        }
    }
    /// <summary>
    /// 绝世无双
    /// </summary>
    public class JueShiWuShuang : BondOperator
    {
        public JueShiWuShuang(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op)
        {
            if (IsInBondList(op) &&
                Chessboard.IsRandomPass(DataTable.GetGameValue(144)))
                return Helper.Singular(CombatConduct.InstanceBuff(op.IsChallenger ? -1 : -2, CardState.Cons.ShenZhu));
            return null;
        }

        public override int OnDamageAddOn(ChessOperator[] ops, ChessOperator op, int damage)
        {
            if (op.Style.ArmedType == 5)
                return (int)(damage * DataTable.GetGameValue(164) * 0.01f);
            return 0;
        }
    }
    /// <summary>
    /// 汉末三仙
    /// </summary>
    public class HanMoSanXian : BondOperator
    {
        public HanMoSanXian(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op) => null;

        public override int OnDamageAddOn(ChessOperator[] ops, ChessOperator op, int damage)
        {
            if (op.Style.Element > 0)
                return (int)(damage * DataTable.GetGameValue(161) * 0.01f);
            return 0;
        }
    }
}