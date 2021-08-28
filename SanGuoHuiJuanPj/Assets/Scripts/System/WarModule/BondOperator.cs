﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using NotImplementedException = System.NotImplementedException;

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
                Chessboard.InstanceChessboardActivity(op.IsChallenger,op, Activity.Self, conducts);
            }

            var rivals = Chessboard.GetRivals(first, 
                pos => pos.IsPostedAlive &&
                       pos.IsAliveHero).Select(p => p.Operator);

            foreach (var rival in rivals)
            {
                var conducts = RoundStartRivalConduct(rival);
                if (conducts == null || conducts.Length == 0) continue;
                Chessboard.InstanceChessboardActivity(first.IsChallenger, rival, Activity.OffendAttach, conducts);
            }
        }

        protected virtual CombatConduct[] RoundStartRivalConduct(IChessOperator rival) => null;

        protected abstract CombatConduct[] RoundStartConducts(ChessOperator op);
        public virtual int OnDamageAddOn(ChessOperator[] ops, ChessOperator op) => 0;
        public virtual int OnBuffingRatioAddOn(ChessOperator[] ops, ChessOperator op) => 0;
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

        protected override CombatConduct[] RoundStartRivalConduct(IChessOperator rival) => null;

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

        protected override CombatConduct[] RoundStartRivalConduct(IChessOperator rival)
        {
            var damage = (int)ActiveBondList.Average(o => Chessboard.ConvertHeroDamage(o)) *
                         DataTable.GetGameValue(149) * 0.01f;
            var conducts = new List<CombatConduct> { CombatConduct.InstanceDamage(damage) };
            if (Chessboard.IsRandomPass(DataTable.GetGameValue(150)))
                conducts.Add(CombatConduct.InstanceBuff(CardState.Cons.Cowardly));
            return conducts.ToArray();
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
    }
}