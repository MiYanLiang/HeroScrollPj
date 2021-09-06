using System.Collections.Generic;
using System.Linq;

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
        protected ChessboardOperator Chessboard { get; }
        protected ChessOperator[] ActiveBondList { get; private set; }
        /// <summary>
        /// 获取有效羁绊，如果返回null表示羁绊未被激活
        /// </summary>
        /// <param name="ops"></param>
        /// <returns></returns>
        public ChessOperator[] GetActiveBonds(IEnumerable<ChessOperator> ops)
        {
            var list = BondList.Join(ops.Where(o => o.IsAlive), b => b, o => o.CardId, (_, o) => o).ToArray();
            return list.Length != BondList.Length ? null : list;
        }

        protected BondOperator(ChessboardOperator chessboard, int[] bondList)
        {
            BondList = bondList;
            Chessboard = chessboard;
        }

        protected bool IsInBondList(IChessOperator op) => BondList.Contains(op.CardId);
        /// <summary>
        /// 根据比率给出羁绊者的平均伤害加成
        /// </summary>
        /// <param name="rate"></param>
        /// <returns></returns>
        protected int AverageAdditionalDamageFromBonds(int rate) => (int)(ActiveBondList.Average(o => Chessboard.ConvertHeroDamage(o)) * 0.01f * rate);
        public void OnRoundStart(IEnumerable<ChessOperator> list)
        {
            var ops = list.ToArray();
            ActiveBondList = GetActiveBonds(ops);
            if (ActiveBondList == null || ActiveBondList.Length == 0) return;
            var first = ActiveBondList.First();
            foreach (var op in ops)
            {
                var conducts = RoundStartConducts(op);
                if (conducts == null || conducts.Length == 0) continue;
                Chessboard.InstanceChessboardActivity(op.IsChallenger,op, RoundAction.JiBan, conducts);
            }

            var rivals = Chessboard.GetRivals(first, 
                pos => pos.IsPostedAlive &&
                       pos.IsAliveHero).Select(p => p.Operator);

            foreach (var rival in rivals)
            {
                var result = RoundStartRivalConduct(rival);
                if (result == null) return;
                if (!result.PushBackPos)
                {
                    var conducts = result.Conducts;
                    if (conducts == null || conducts.Length == 0) continue;
                    Chessboard.InstanceChessboardActivity(first.IsChallenger, rival, RoundAction.JiBan, conducts);
                    continue;
                }

                var backPos = Chessboard.BackPos(Chessboard.GetChessPos(rival));
                if (backPos == null || backPos.IsPostedAlive) continue;
                Chessboard.InstanceChessboardActivity(first.IsChallenger, rival, RoundAction.JiBan, result.Conducts,
                    0, backPos.Pos);
            }
        }

        protected virtual ConductResult RoundStartRivalConduct(IChessOperator rival) => null;

        protected abstract CombatConduct[] RoundStartConducts(ChessOperator op);
        public virtual int OnDamageAddOn(ChessOperator[] ops, ChessOperator op) => 0;
        public virtual int OnBuffingRatioAddOn(ChessOperator[] ops, ChessOperator op) => 0;

        protected class ConductResult
        {
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
        public TaoYuanJieYi(ChessboardOperator chessboard, int[] bondList) : base(chessboard, bondList)
        {
        }

        protected override ConductResult RoundStartRivalConduct(IChessOperator rival) => null;

        protected override CombatConduct[] RoundStartConducts(ChessOperator op)
        {
            if (!IsInBondList(op)) return null;
            if (!Chessboard.IsRandomPass(ShenZhuRatio) ||
                Chessboard.GetCondition(op, CardState.Cons.ShenZhu) > 0) return null;
            return Helper.Singular(CombatConduct.InstanceBuff(CardState.Cons.ShenZhu));
        }

        public override int OnDamageAddOn(ChessOperator[] ops, ChessOperator op)
        {
            var bonds = GetActiveBonds(ops);
            if (bonds == null || bonds.Length == 0) return 0;
            //if (!bonds.Contains(op)) return 0;
            return (int)(op.GeneralDamage() * DamageAdditionRatio * 0.01f);
        }
    }
    /// <summary>
    /// 五虎上将
    /// </summary>
    public class WuHuSHangJiang :BondOperator
    {
        public WuHuSHangJiang(ChessboardOperator chessboard, int[] bondList) : base(chessboard, bondList)
        {
        }

        protected override ConductResult RoundStartRivalConduct(IChessOperator rival)
        {
            var damage = AverageAdditionalDamageFromBonds(DataTable.GetGameValue(149));
            var conducts = new List<CombatConduct> { CombatConduct.InstanceDamage(damage) };
            if (Chessboard.IsRandomPass(DataTable.GetGameValue(150)))
                conducts.Add(CombatConduct.InstanceBuff(CardState.Cons.Cowardly));
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
        public WoLongFengChu(ChessboardOperator chessboard, int[] bondList) : base(chessboard, bondList)
        {
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op)
        {
            if (!IsInBondList(op)) return null;
            return Chessboard.IsRandomPass(ShenZhuRatio) ? 
                Helper.Singular(CombatConduct.InstanceBuff(CardState.Cons.ShenZhu)) : null;
        }

        public override int OnDamageAddOn(ChessOperator[] ops, ChessOperator op)
        {
            if (op.IsRangeHero) return (int)(Chessboard.ConvertHeroDamage(op) * DataTable.GetGameValue(151) * 0.01f);
            return 0;
        }
    }
    /// <summary>
    /// 虎痴恶来
    /// </summary>
    public class HuChiELai : BondOperator
    {
        public HuChiELai(ChessboardOperator chessboard, int[] bondList) : base(chessboard, bondList)
        {
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op)
        {
            if (IsInBondList(op) &&
                Chessboard.IsRandomPass(DataTable.GetGameValue(137)))
                return Helper.Singular(CombatConduct.InstanceBuff(CardState.Cons.Shield));
            return null;
        }

        public override int OnDamageAddOn(ChessOperator[] ops, ChessOperator op)
        {
            return op.Style.Troop == 1
                ? (int)(Chessboard.ConvertHeroDamage(op) * DataTable.GetGameValue(152) * 0.01f)
                : 0;
        }
    }
    /// <summary>
    /// 五子良将
    /// </summary>
    public class WuZiLiangJiang : BondOperator
    {
        public WuZiLiangJiang(ChessboardOperator chessboard, int[] bondList) : base(chessboard, bondList)
        {
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op)
        {
            if (!IsInBondList(op) ||
                !Chessboard.IsRandomPass(DataTable.GetGameValue(138)))
                return Helper.Singular(CombatConduct.InstanceBuff(CardState.Cons.Shield));
            return null;
        }

        public override int OnDamageAddOn(ChessOperator[] ops, ChessOperator op)
        {
            if (op.IsMeleeHero) return (int)(Chessboard.ConvertHeroDamage(op) * DataTable.GetGameValue(153) * 0.01f);
            return 0;
        }
    }

    /// <summary>
    /// 魏五谋士
    /// </summary>
    public class WeiWuMouShi : BondOperator
    {
        public WeiWuMouShi(ChessboardOperator chessboard, int[] bondList) : base(chessboard, bondList)
        {
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op)
        {
            //干扰系
            if (op.Style.ArmedType != 12 ||
                !Chessboard.IsRandomPass(DataTable.GetGameValue(139))) return null;
            return Helper.Singular(CombatConduct.InstanceBuff(CardState.Cons.Neizhu));
        }

        protected override ConductResult RoundStartRivalConduct(IChessOperator rival)
        {
            var damage = AverageAdditionalDamageFromBonds(DataTable.GetGameValue(154));
            return new ConductResult(Helper.Singular(CombatConduct.InstanceDamage(damage, 1)));
        }
    }
    /// <summary>
    /// 虎踞江东
    /// </summary>
    public class HuJuJiangDong : BondOperator
    {
        public HuJuJiangDong(ChessboardOperator chessboard, int[] bondList) : base(chessboard, bondList)
        {
        }

        public override int OnDamageAddOn(ChessOperator[] ops, ChessOperator op)
        {
            if (op.Style.Troop != 2) return 0;
            return (int)(Chessboard.ConvertHeroDamage(op) * DataTable.GetGameValue(157) * 0.01f);
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op)
        {
            if (!IsInBondList(op))
                return null;
            return Helper.Singular(CombatConduct.InstanceBuff(CardState.Cons.ShenZhu));
        }
    }
    /// <summary>
    /// 水师都督
    /// </summary>
    public class ShuiShiDuDu : BondOperator
    {
        public ShuiShiDuDu(ChessboardOperator chessboard, int[] bondList) : base(chessboard, bondList)
        {
        }

        protected override ConductResult RoundStartRivalConduct(IChessOperator rival)
        {
            var damage = AverageAdditionalDamageFromBonds(DataTable.GetGameValue(154));
            return new ConductResult(Helper.Singular(CombatConduct.InstanceDamage(damage, 1)));
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op) => null;

        public override int OnDamageAddOn(ChessOperator[] ops, ChessOperator op)
        {
            if (op.Style.ArmedType == 8)//水师都督激活时战船系武将伤害加成50%
                return (int)(Chessboard.ConvertHeroDamage(op) * DataTable.GetGameValue(160) * 0.01f);
            return 0;
        }
    }
    /// <summary>
    /// 天作之合
    /// </summary>
    public class TianZuoZhiHe : BondOperator
    {
        public TianZuoZhiHe(ChessboardOperator chessboard, int[] bondList) : base(chessboard, bondList)
        {
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op)
        {
            if (IsInBondList(op) &&
                Chessboard.IsRandomPass(DataTable.GetGameValue(142)))
                return Helper.Singular(CombatConduct.InstanceBuff(CardState.Cons.ShenZhu));
            return null;
        }
    }
    /// <summary>
    /// 河北四庭柱
    /// </summary>
    public class HeBeiSiTingZhu : BondOperator
    {
        public HeBeiSiTingZhu(ChessboardOperator chessboard, int[] bondList) : base(chessboard, bondList)
        {
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op)
        {
            if (IsInBondList(op) &&
                Chessboard.IsRandomPass(DataTable.GetGameValue(143)))
                return Helper.Singular(CombatConduct.InstanceBuff(CardState.Cons.Shield));
            return null;
        }

        public override int OnDamageAddOn(ChessOperator[] ops, ChessOperator op)
        {
            if (op.Style.Troop == 3)
                return Chessboard.ConvertHeroDamage(op) * DataTable.GetGameValue(163);
            return 0;
        }
    }
    /// <summary>
    /// 绝世无双
    /// </summary>
    public class JueShiWuShuang : BondOperator
    {
        public JueShiWuShuang(ChessboardOperator chessboard, int[] bondList) : base(chessboard, bondList)
        {
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op)
        {
            if (IsInBondList(op) &&
                Chessboard.IsRandomPass(DataTable.GetGameValue(144)))
                return Helper.Singular(CombatConduct.InstanceBuff(CardState.Cons.ShenZhu));
            return null;
        }

        public override int OnDamageAddOn(ChessOperator[] ops, ChessOperator op)
        {
            if (op.Style.ArmedType == 5)
                return (int)(Chessboard.ConvertHeroDamage(op) * DataTable.GetGameValue(164) * 0.01f);
            return 0;
        }
    }
    /// <summary>
    /// 汉末三仙
    /// </summary>
    public class HanMoSanXian : BondOperator
    {
        public HanMoSanXian(ChessboardOperator chessboard, int[] bondList) : base(chessboard, bondList)
        {
        }

        protected override CombatConduct[] RoundStartConducts(ChessOperator op) => null;

        public override int OnDamageAddOn(ChessOperator[] ops, ChessOperator op)
        {
            if (op.Style.Element > 0)
                return (int)(Chessboard.ConvertHeroDamage(op) * DataTable.GetGameValue(161) * 0.01f);
            return 0;
        }
    }
}