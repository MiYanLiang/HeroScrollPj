using System;
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
        protected int[] BondList { get; }
        protected int[] BossList { get; }

        public int BondId { get; }
        protected ChessboardOperator Chessboard { get; }

        protected BondOperator(JiBanTable jiBan, ChessboardOperator chessboard)
        {
            BondList = jiBan.Cards.Select(c => c.CardId).ToArray();
            BossList = jiBan.BossCards.Select(c => c.CardId).ToArray();
            BondId = jiBan.Id;
            Chessboard = chessboard;
        }

        /// <summary>
        /// 是否是羁绊单位
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        protected bool IsInBondList(IChessOperator op) => BondList.Contains(op.CardId);

        /// <summary>
        /// 根据比率给出羁绊者的平均伤害加成
        /// </summary>
        /// <param name="ops"></param>
        /// <param name="rate"></param>
        /// <returns></returns>
        protected int AverageAdditionalDamageFromBonds(ChessOperator[] ops, int rate) =>
            (int)(ops.Average(o => Chessboard.GetHeroBuffedDamage(o)) * 0.01f * rate);

        public void OnRoundStart(IEnumerable<ChessOperator> list)
        {
            var ops = list.ToArray();
            var first = ops.First();
            foreach (var op in ops)
            {
                var conducts = RoundStartFriendlyConducts(op);
                if (conducts == null || conducts.Length == 0) continue;
                Chessboard.InstanceJiBanActivity(BondId, op.IsChallenger, op, Activity.Intentions.Friendly, conducts);
            }

            var rivals = Chessboard.GetRivals(first,
                pos => pos.IsPostedAlive &&
                       pos.IsAliveHero).Select(p => p.Operator);

            foreach (var rival in rivals)
            {
                var result = RoundStartRivalConduct(ops, rival);
                if (result == null) return;
                if (Chessboard.IsRandomPass(result.PushBackRate))
                {
                    var backPos = Chessboard.BackPos(Chessboard.GetChessPos(rival));
                    if (backPos == null || backPos.IsPostedAlive) continue;
                    Chessboard.InstanceJiBanActivity(BondId, first.IsChallenger, rival, Activity.Intentions.Offensive,
                        result.Conducts, backPos.Pos);
                    continue;

                }

                var conducts = result.Conducts;
                if (conducts == null || conducts.Length == 0) continue;
                Chessboard.InstanceJiBanActivity(BondId, first.IsChallenger, rival, Activity.Intentions.Offensive,
                    conducts);
            }
        }

        /// <summary>
        /// 对对面的活动执行.
        /// </summary>
        /// <param name="chessOperators"></param>
        /// <param name="rival"></param>
        /// <returns></returns>
        protected virtual ConductResult RoundStartRivalConduct(ChessOperator[] chessOperators, IChessOperator rival) =>
            null;

        /// <summary>
        /// 对己方的活动
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        protected abstract CombatConduct[] RoundStartFriendlyConducts(ChessOperator op);

        /// <summary>
        /// 对伤害的伤害值的增/减(直接增减，非百分比)值。
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public virtual int OnDamageAddOn(ChessOperator op) => 0;

        public virtual int OnBuffingRatioAddOn(ChessOperator[] activators, ChessOperator op) => 0;

        /// <summary>
        /// 为羁绊列表里的武将添加buff
        /// </summary>
        /// <param name="op"></param>
        /// <param name="buff"></param>
        /// <param name="rate"></param>
        /// <returns></returns>
        protected IList<CombatConduct> GetBuffIfInBondList(ChessOperator op, CardState.Cons buff, int rate) =>
            IsInBondList(op)
                ? Helper.Singular(CombatConduct.InstanceBuff(BondId, buff, rate: rate))
                : Array.Empty<CombatConduct>();

        /// <summary>
        /// 生成状态精灵为该单位的属性增值一个回合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="op"></param>
        /// <param name="value"></param>
        protected void InstanceJiBanSprite<T>(ChessOperator op, int value) where T : RoundSprite, new() =>
            Chessboard.InstanceSprite<T>(Chessboard.GetChessPos(op), lasting: 1, value: value, actId: -1);

        protected class ConductResult
        {
            public int JbId;
            public int PushBackRate;
            public CombatConduct[] Conducts;

            public ConductResult()
            {
            }

            public ConductResult(CombatConduct[] conducts)
            {
                Conducts = conducts;
            }
        }

        /// <summary>
        /// 返回羁绊id，如果未激活羁绊返回空
        /// </summary>
        /// <param name="ops"></param>
        /// <returns></returns>
        public int[] JiBanActivateList(IEnumerable<ChessOperator> ops)
        {
            var bond = BondList.ToDictionary(id => id, _ => false);
            var boss = BossList.ToDictionary(id => id, _ => false);
            foreach (var op in ops.Where(o=>o.CardType == GameCardType.Hero))
            {
                if (bond.ContainsKey(op.CardId)) bond[op.CardId] = true;
                if (boss.ContainsKey(op.CardId)) bond[op.CardId] = true;
            }

            return bond.Any() && bond.All(c => c.Value)
                ? bond.Keys.ToArray()
                : boss.Any() && boss.All(c => c.Value)
                    ? boss.Keys.ToArray()
                    : null;
        }
    }

    /// <summary>
    /// 桃园结义
    /// </summary>
    public class TaoYuanJieYi : BondOperator
    {
        private int ShenZhuRatio { get; } = 30;
        private int DamageAdditionRatio { get; } = 10;
        private int TroopId { get; } = 0; //刘备军

        public TaoYuanJieYi(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override ConductResult RoundStartRivalConduct(ChessOperator[] chessOperators, IChessOperator rival) =>
            null;

        protected override CombatConduct[] RoundStartFriendlyConducts(ChessOperator op)
        {
            var list = new List<CombatConduct>();
            list.AddRange(GetBuffIfInBondList(op, CardState.Cons.ShenZhu, ShenZhuRatio)); //给羁绊列表里的武将添加状态
            if (op.CardType == GameCardType.Hero && op.Style.Troop == TroopId) //同军团
                InstanceJiBanSprite<JiBanStrengthSprite>(op, DamageAdditionRatio);
            return list.ToArray();
        }
    }

    /// <summary>
    /// 五虎上将
    /// </summary>
    public class WuHuSHangJiang : BondOperator
    {
        private int CowardlyRate => 35;
        private int DamageRate => 35;

        public WuHuSHangJiang(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override ConductResult RoundStartRivalConduct(ChessOperator[] ops, IChessOperator rival)
        {
            var damage =
                AverageAdditionalDamageFromBonds(ops.Where(IsInBondList).ToArray(), DamageRate);
            var scope = ops.First().IsChallenger ? -1 : -2;
            var conducts = new List<CombatConduct> { CombatConduct.InstanceDamage(scope, damage) };
            conducts.Add(CombatConduct.InstanceBuff(scope, CardState.Cons.Cowardly, rate: CowardlyRate));
            return new ConductResult(conducts.ToArray());
        }

        protected override CombatConduct[] RoundStartFriendlyConducts(ChessOperator op) => null;
    }

    /// <summary>
    /// 卧龙凤雏
    /// </summary>
    public class WoLongFengChu : BondOperator
    {
        public int ShenZhuRatio { get; } = 30;
        public int DamageRate { get; } = 20;

        public WoLongFengChu(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartFriendlyConducts(ChessOperator op)
        {
            var list = new List<CombatConduct>();
            //如果是羁绊名单内的武将,添加神助
            list.AddRange(GetBuffIfInBondList(op, CardState.Cons.ShenZhu, ShenZhuRatio));

            if (op.IsRangeHero) //远程添加伤害精灵
                InstanceJiBanSprite<JiBanStrengthSprite>(op, DamageRate);
            return list.ToArray();
        }
    }

    /// <summary>
    /// 虎痴恶来
    /// </summary>
    public class HuChiELai : BondOperator
    {
        private int TroopId => 1; //魏
        private int DamageRate => 10;
        private int ShieldRate => 100;

        public HuChiELai(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartFriendlyConducts(ChessOperator op)
        {
            var list = new List<CombatConduct>();
            list.AddRange(GetBuffIfInBondList(op, CardState.Cons.Shield, ShieldRate));
            if (op.CardType == GameCardType.Hero && op.Style.Troop == TroopId)
                InstanceJiBanSprite<JiBanStrengthSprite>(op, DamageRate);
            return list.ToArray();
        }
    }

    /// <summary>
    /// 五子良将
    /// </summary>
    public class WuZiLiangJiang : BondOperator
    {
        private int DamageRate => 20;
        private int ShieldRate => 100;

        public WuZiLiangJiang(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartFriendlyConducts(ChessOperator op)
        {
            var list = new List<CombatConduct>();
            list.AddRange(GetBuffIfInBondList(op, CardState.Cons.Shield, ShieldRate));
            if (op.IsMeleeHero)
                InstanceJiBanSprite<JiBanStrengthSprite>(op, DamageRate);
            return list.ToArray();
        }
    }

    /// <summary>
    /// 魏五奇谋
    /// </summary>
    public class WeiWuQiMou : BondOperator
    {
        private int BuffRate => 25;
        private int IntelligentRate => 20;
        private int TroopId => 1; //魏

        public WeiWuQiMou(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartFriendlyConducts(ChessOperator op)
        {
            if (op.CardType == GameCardType.Hero && op.Style.Troop == TroopId)
                InstanceJiBanSprite<JiBanIntelligentSprite>(op, IntelligentRate);
            return Array.Empty<CombatConduct>();
        }


        protected override ConductResult RoundStartRivalConduct(ChessOperator[] ops, IChessOperator rival)
        {
            if (rival.CardType != GameCardType.Hero) return null;
            var buff = CardState.ControllingBuffs
                .Select(s => new { s, random = Chessboard.Randomize(20) })
                .OrderBy(s => s.random).First().s;
            return new ConductResult(
                Helper.Singular(CombatConduct.InstanceBuff(ops.First().IsChallenger ? -1 : -2, buff, rate: BuffRate)));
        }
    }

    /// <summary>
    /// 虎踞江东
    /// </summary>
    public class HuJuJiangDong : BondOperator
    {
        private int TroopId => 2; //吴
        private int BuffRate => 30;
        private int DamageRate => 10;

        public HuJuJiangDong(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartFriendlyConducts(ChessOperator op)
        {
            var list = new List<CombatConduct>();
            list.AddRange(GetBuffIfInBondList(op, CardState.Cons.ShenZhu, BuffRate));
            if (op.CardType == GameCardType.Hero && op.Style.Troop == TroopId)
                InstanceJiBanSprite<JiBanStrengthSprite>(op, DamageRate);
            return list.ToArray();
        }
    }

    /// <summary>
    /// 水师都督
    /// </summary>
    public class ShuiShiDuDu : BondOperator
    {
        private int DamageRate => 20;
        private int DamageUpRate => 50;
        private int PushBackRate => 70;
        private int WarshipArmedType => 8; //战船系

        public ShuiShiDuDu(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override ConductResult RoundStartRivalConduct(ChessOperator[] ops, IChessOperator rival)
        {
            var damage = AverageAdditionalDamageFromBonds(ops.Where(IsInBondList).ToArray(), DamageRate);
            var conduct = new ConductResult(Helper.Singular(
                CombatConduct.InstanceDamage(ops.First().IsChallenger ? -1 : -2, damage, CombatConduct.WaterDmg)));
            conduct.PushBackRate = PushBackRate;
            return conduct;
        }

        protected override CombatConduct[] RoundStartFriendlyConducts(ChessOperator op)
        {
            if (op.Style.ArmedType == WarshipArmedType)
                InstanceJiBanSprite<JiBanStrengthSprite>(op, DamageUpRate);
            return Array.Empty<CombatConduct>();
        }

    }

    /// <summary>
    /// 天作之合
    /// </summary>
    public class TianZuoZhiHe : BondOperator
    {
        private int SpeedUpRate => 20;
        private int ShenZhuRate => 50;
        private int TroopId => 2;

        public TianZuoZhiHe(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartFriendlyConducts(ChessOperator op)
        {
            var list = new List<CombatConduct>();
            list.AddRange(GetBuffIfInBondList(op, CardState.Cons.ShenZhu, ShenZhuRate));
            if (op.CardType == GameCardType.Hero && op.Style.Troop == TroopId)
                InstanceJiBanSprite<JiBanSpeedSprite>(op, SpeedUpRate);
            return list.ToArray();
        }
    }

    /// <summary>
    /// 河北四庭柱
    /// </summary>
    public class HeBeiSiTingZhu : BondOperator
    {
        private int ShenZhuRate => 30;
        private int DamageUpRate => 20;

        public HeBeiSiTingZhu(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartFriendlyConducts(ChessOperator op)
        {
            var list = new List<CombatConduct>();
            list.AddRange(GetBuffIfInBondList(op, CardState.Cons.ShenZhu, ShenZhuRate));
            if (op.IsMeleeHero)
                InstanceJiBanSprite<JiBanStrengthSprite>(op, DamageUpRate);
            return list.ToArray();
        }
    }

    /// <summary>
    /// 绝世无双
    /// </summary>
    public class JueShiWuShuang : BondOperator
    {
        private int SpeedUpRate => 20;
        private int ShenZhuRate => 100;
        private int TroopId => 4;

        public JueShiWuShuang(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartFriendlyConducts(ChessOperator op)
        {
            var list = new List<CombatConduct>();
            list.AddRange(GetBuffIfInBondList(op, CardState.Cons.ShenZhu, ShenZhuRate));
            if (op.CardType == GameCardType.Hero && op.Style.Troop == TroopId)
                InstanceJiBanSprite<JiBanSpeedSprite>(op, SpeedUpRate);
            return list.ToArray();
        }
    }
    ///// <summary>
    ///// 汉末三仙
    ///// </summary>
    //public class HanMoSanXian : BondOperator
    //{
    //    public HanMoSanXian(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
    //    {
    //    }

    //    protected override CombatConduct[] RoundStartFriendlyConducts(ChessOperator op) => null;

    //    public override int OnDamageAddOn(ChessOperator op)
    //    {
    //        if (op.Style.Element > 0)
    //            return (int)(damage * DataTable.GetGameValue(161) * 0.01f);
    //        return 0;
    //    }
    //}

    /// <summary>
    /// 四世三公
    /// </summary>
    public class SiShiSanGong : BondOperator
    {
        private int DamageUpRate => 10;
        private int ShenZhuRate => 50;
        private int TroopId => 3;

        public SiShiSanGong(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartFriendlyConducts(ChessOperator op)
        {
            var list = new List<CombatConduct>();
            list.AddRange(GetBuffIfInBondList(op, CardState.Cons.ShenZhu, ShenZhuRate));
            if (op.CardType == GameCardType.Hero && op.Style.Troop == TroopId)
                InstanceJiBanSprite<JiBanStrengthSprite>(op, DamageUpRate);
            return list.ToArray();
        }
    }

    /// <summary>
    /// 忠言逆耳
    /// </summary>
    public class ZhongYanNiEr : BondOperator
    {
        private int SpeedUpRate => 20;
        private int NeiZhuRate => 50;
        private int TroopId => 3;

        public ZhongYanNiEr(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartFriendlyConducts(ChessOperator op)
        {
            var list = new List<CombatConduct>();
            list.AddRange(GetBuffIfInBondList(op, CardState.Cons.Neizhu, NeiZhuRate));
            if (op.CardType == GameCardType.Hero && op.Style.Troop == TroopId)
                InstanceJiBanSprite<JiBanSpeedSprite>(op, SpeedUpRate);
            return list.ToArray();
        }
    }

    /// <summary>
    /// 八健将
    /// </summary>
    public class BaJianJIang : BondOperator
    {
        private int DamageUpRate => 30;
        private int NeiZhuRate => 100;
        private int QiBingId => 5;

        public BaJianJIang(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartFriendlyConducts(ChessOperator op)
        {
            var list = new List<CombatConduct>();
            list.AddRange(GetBuffIfInBondList(op, CardState.Cons.Neizhu, NeiZhuRate));
            if (op.Style.ArmedType == QiBingId)
                InstanceJiBanSprite<JiBanStrengthSprite>(op, DamageUpRate);
            return list.ToArray();
        }
    }

    /// <summary>
    /// 威震西凉
    /// </summary>
    public class WeiZhenXiLiang : BondOperator
    {
        private int DamageUpRate => 10;
        private int NeiZhuRate => 50;
        private int TroopId => 4;

        public WeiZhenXiLiang(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartFriendlyConducts(ChessOperator op)
        {
            var list = new List<CombatConduct>();
            list.AddRange(GetBuffIfInBondList(op, CardState.Cons.Neizhu, NeiZhuRate));
            if (op.CardType == GameCardType.Hero && op.Style.Troop == TroopId)
                InstanceJiBanSprite<JiBanStrengthSprite>(op, DamageUpRate);
            return list.ToArray();
        }
    }

    /// <summary>
    /// 贪狼之心
    /// </summary>
    public class TanLangZhiXin : BondOperator
    {
        private int IntelligentUpRate => 30;
        private int ShenZhuRate => 30;
        private int TroopId => 5;

        public TanLangZhiXin(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartFriendlyConducts(ChessOperator op)
        {
            var list = new List<CombatConduct>();
            list.AddRange(GetBuffIfInBondList(op, CardState.Cons.ShenZhu, ShenZhuRate));
            if (op.CardType == GameCardType.Hero && op.Style.Troop == TroopId)
                InstanceJiBanSprite<JiBanIntelligentSprite>(op, IntelligentUpRate);
            return list.ToArray();
        }

    }

    /// <summary>
    /// 不世之功
    /// </summary>
    public class BuShiZhiGong : BondOperator
    {
        private int DamageUpRate => 10;
        private int ShenZhuRate => 30;
        private int TroopId => 4;

        public BuShiZhiGong(JiBanTable jiBan, ChessboardOperator chessboard) : base(jiBan, chessboard)
        {
        }

        protected override CombatConduct[] RoundStartFriendlyConducts(ChessOperator op)
        {
            var list = new List<CombatConduct>();
            list.AddRange(GetBuffIfInBondList(op, CardState.Cons.ShenZhu, ShenZhuRate));
            if (op.CardType == GameCardType.Hero && op.Style.Troop == TroopId)
                InstanceJiBanSprite<JiBanStrengthSprite>(op, DamageUpRate);
            return list.ToArray();
        }
    }
}