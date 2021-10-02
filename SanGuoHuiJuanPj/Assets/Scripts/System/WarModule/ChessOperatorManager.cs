using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CorrelateLib;
using Microsoft.Extensions.Logging;

namespace Assets.System.WarModule
{
    public class ChessOperatorManager<TCard> : ChessboardOperator where TCard : IChessman
    {
        protected override Dictionary<ChessOperator, ChessStatus> StatusMap { get; }
        protected Dictionary<int, ChessOperator> ops = new Dictionary<int, ChessOperator>();
        private int cardSeed;
        protected override List<PosSprite> Sprites { get; }
        protected override BondOperator[] JiBan { get; }
        private BuffOperator[] BuffOps { get; }

        public ChessOperatorManager(ChessGrid grid, ILogger log = null) : base(grid, log)
        {
            StatusMap = new Dictionary<ChessOperator, ChessStatus>();
            Sprites = new List<PosSprite>();
            BuffOps = new BuffOperator[]
            {
                new StunnedBuff(this), //1 眩晕
                //new ShieldBuff(this), //2 护甲
                new InvincibleBuff(this), //3 无敌
                new BleedBuff(this), // 4 流血
                new PoisonBuff(this), // 5 毒
                new BurnBuff(this), // 6 灼烧
                new Imprisoned(this), // 8 禁锢
                new CowardlyBuff(this), //9 怯战
                new DisarmedBuff(this), //16 卸甲
                new NeiZhuBuff(this),//17 内助
                new ShenZhuBuff(this),//18 神助
                new ExtendedShieldBuff(this), // 19 缓冲盾
                new ConfuseBuff(this),//22 混乱
                new ChainedBuff(this),//24 铁骑
            };
            JiBan = GetJiBan();
        }

        private BondOperator[] GetJiBan() => DataTable.JiBan.Values.Where(j => j.IsOpen > 0).Select(GetBondOperator).Where(b=>b!=null).ToArray();

        private BondOperator GetBondOperator(JiBanTable jb)
        {
            switch (jb.Id)
            {
                case 0: return new TaoYuanJieYi(jb,this);
                case 1: return new WuHuSHangJiang(jb, this);
                case 2: return new WoLongFengChu(jb, this);
                case 3: return new HuChiELai(jb, this);
                case 4: return new WuZiLiangJiang(jb, this);
                case 5: return new WeiWuMouShi(jb, this);
                case 6: return new HuJuJiangDong(jb, this);
                case 7: return new ShuiShiDuDu(jb, this);
                case 8: return new TianZuoZhiHe(jb, this);
                case 9: return new HeBeiSiTingZhu(jb, this);
                case 10: return new JueShiWuShuang(jb, this);
                case 11: return new HanMoSanXian(jb, this);
                default: return null;
            }
        }

        public int GetCardSeed()
        {
            var seed = cardSeed;
            cardSeed++;
            return seed;
        }

        public TCard RegOperator(TCard card)
        {
            card.InstanceId = GetCardSeed();
            var op = InstanceOperator(card);
            StatusMap.Add(op, op.GenerateStatus());
            ops.Add(op.InstanceId,op);
            op.SetPos(card.Pos);
            PlaceList.Add(op);
            return card;
        }


        public ChessOperator DropOperator(TCard card)
        {
            if (ops.ContainsKey(card.InstanceId))
                throw new NullReferenceException($"Card[{card.CardId}]:Type.{card.CardType}");
            var op = ops[card.InstanceId];
            ops.Remove(op.InstanceId);
            StatusMap.Remove(op);
            PlaceList.Remove(op);
            return op;
        }

        private ChessOperator InstanceOperator(TCard card)
        {
            switch (card.CardType)
            {
                case GameCardType.Hero:
                    return InstanceHero(card);
                case GameCardType.Tower:
                    return InstanceTower(card);
                case GameCardType.Trap:
                    return InstanceTrap(card);
                case GameCardType.Spell:
                case GameCardType.Base:
                    return InstanceBase(card); //todo 需要实现一个老巢的类
                case GameCardType.Soldier:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private ChessOperator InstanceBase(TCard card)
        {
            var op = new BlankTowerOperator();
            op.Init(card, this);
            return op;
        }

        private ChessOperator InstanceHero(TCard card)
        {
            HeroOperator op = null;
            var military = MilitaryInfo.GetInfo(card.CardId);
            switch (military.Id)
            {
                case 1:
                    op = new JinZhanOperator();
                    break; //1   近战
                case 2:
                    op = new TeiWeiOperator();
                    break; //2   铁卫
                case 3:
                    op = new FeiJiaOperator();
                    break; //3   飞甲
                case 4:
                    op = new DaDunOperator();
                    break; //4   大盾
                case 5:
                    op = new XianZhenOperator();
                    break; //5   陷阵
                case 6:
                    op = new HuWeiOperator();
                    break; //6   虎卫
                case 7:
                    op = new CiDunOperator();
                    break; //7   刺甲
                case 8:
                    op = new ZhanXiangOperator();
                    break; //8   象兵
                case 9:
                case 60:
                    op = new FeiQiOperator();
                    break; //9   先锋 //60  急先锋
                case 10:
                    op = new XianDengOperator();
                    break; //10  先登
                case 11:
                    op = new HuBaoQiOperator();
                    break; //11  白马
                case 12:
                    op = new ShenWuOperator();
                    break; //12  神武
                case 13:
                    op = new YuLinOperator();
                    break; //13  禁卫
                
                case 15:
                    op = new DaJiOperator();
                    break; //15  大戟
                case 16:
                    op = new PiaoQiOperator();
                    break; //16  骠骑
                case 17:
                    op = new DaDaoOperator();
                    break; //17  大刀
                case 18:
                    op = new DaFuOperator();
                    break; //18  大斧
                case 19:
                case 51:
                    op = new LianNuOperator();
                    break; //19  连弩  //51  强弩
                case 20:
                case 52:
                    op = new GongBingOperator();
                    break; //20  弓兵 //52  大弓
                case 21:
                    op = new ZhanChuanOperator();
                    break; //21  战船
                case 22:
                    op = new ZhanCheOperator();
                    break; //22  战车
                case 23:
                    op = new GongChengCheOperator();
                    break; //23  攻城车
                case 24:
                    op = new TouShiCheOperator();
                    break; //24  投石车
                case 25:
                    op = new CiKeOperator();
                    break; //25  刺客
                case 26:
                case 27:
                    op = new JunShiOperator();
                    break; //26  军师 //27  大军师
                case 28:
                case 29:
                    op = new ShuShiOperator();
                    break; //28  术士 //29  大术士
                case 30:
                case 31:
                    op = new DuShiOperator();
                    break; //30  毒士 //31  大毒士
                case 32:
                case 33:
                    op = new TongShuaiOperator();
                    break; //32  统帅//33  大统帅
                case 34:
                case 35:
                    op = new BianShiOperator();
                    break; //34  辩士 //35  大辩士
                case 36:
                case 37:
                    op = new MouShiOperator();
                    break; //36  谋士 //37  大谋士
                case 38:
                    op = new NeiZhengOperator();
                    break; //38  内政
                case 39:
                    op = new FuZuoOperator();
                    break; //39  辅佐
                case 40:
                    op = new QiXieOperator();
                    break; //40  器械
                case 41:
                    op = new GanSiOperator();
                    break; //41  敢死
                case 42:
                case 43:
                    op = new YiShiOperator();
                    break; //42  医师 //43  大医师
                case 44:
                    op = new JingGuoOperator();
                    break; //44  巾帼
                case 45:
                    op = new QingChengOperator();
                    break; //45  美人
                case 46:
                    op = new QingGuoOperator();
                    break; //46  大美人
                case 47:
                case 48:
                    op = new ShuiKeOperator();
                    break; //47  说客 //48  大说客
                case 49:
                    op = new HeroOperator();
                    break; //49  弩兵
                case 50:
                    op = new HeroOperator();
                    break; //50  文士
                case 53:
                case 54:
                    op = new YinShiOperator();
                    break; //53  隐士 //54  大隐士
                case 55:
                    op = new HuoChuanOperator();
                    break; //55  火船
                case 56:
                    op = new ManZuOperator();
                    break; //56  蛮族
                case 57:
                    op = new TengJiaOperator();
                    break; //57  藤甲
                case 58:
                    op = new TieJiOperator();
                    break; //58  铁骑
                case 59:
                case 14:
                    op = new ChangQiangOperator();
                    break; //59  短枪
                ////61  红颜
                ////62  妖师
                ////63  大妖师
                ////64  锦帆
                case 65:
                    op = new HuangJinOperator();
                    break; //65  黄巾
                default:
                    op = new HeroOperator();
                    break; //0   普通
            }

            op.Init(card, this);
            return op;
        }

        private ChessOperator InstanceTrap(TCard card)
        {
            TrapOperator op = null;
            switch (card.CardId)
            {
                case 0:
                    op = new JuMaOperator();
                    break;
                case 1:
                    op = new DiLeiOperator();
                    break;
                case 2:
                    op = new ShiQiangOperator();
                    break;
                case 3:
                    op = new BaZhenTuOperator();
                    break;
                case 4:
                    op = new JinSuoZhenOperator();
                    break;
                case 5:
                    op = new GuiBingZhenOperator();
                    break;
                case 6:
                    op = new FireWallOperator();
                    break;
                case 7:
                    op = new PoisonSpringOperator();
                    break;
                case 8:
                    op = new BladeWallOperator();
                    break;
                case 9:
                    op = new GunShiOperator();
                    break;
                case 10:
                    op = new GunMuOperator();
                    break;
                case 11:
                    op = new TreasureOperator();
                    break;
                case 12:
                    op = new JuMaOperator();
                    break;
                default:
                    op = new BlankTrapOperator();
                    break;
            }

            op.Init(card, this);
            return op;
        }

        private ChessOperator InstanceTower(TCard card)
        {
            TowerOperator op = null;
            switch (card.CardId)
            {
                //营寨
                case 0:
                    op = new YingZhaiOperator();
                    break;
                //投石台
                case 1:
                    op = new TouShiTaiOperator();
                    break;
                //奏乐台
                case 2:
                    op = new ZouYueTaiOperator();
                    break;
                //箭楼
                case 3:
                    op = new JianLouOperator();
                    break;
                //战鼓台
                case 4:
                    op = new ZhanGuTaiOperator();
                    break;
                //风神台
                case 5:
                    op = new FengShenTaiOperator();
                    break;
                //轩辕台
                case 6:
                    op = new XuanYuanTaiOperator();
                    break;
                //铸铁炉
                case 7:
                    op = new ZhuTieLuOperator();
                    break;
                //四方鼎
                case 8:
                    op = new SiFangDingOperator();
                    break;
                //烽火台
                case 9:
                    op = new FengHuoTaiOperator();
                    break;
                //号角台
                case 10:
                    op = new HaoJiaoTaiOperator();
                    break;
                //瞭望塔
                case 11:
                    op = new LiaoWangTaiOperator();
                    break;
                //七星坛
                case 12:
                    op = new QiXingTanOperator();
                    break;
                //演武场
                case 13:
                    op = new YanWuChangOperator();
                    break;
                //曹魏旗
                case 14:
                    op = new CaoWeiQiOperator();
                    break;
                //蜀汉旗
                case 15:
                    op = new SuHanQiOperator();
                    break;
                //东吴旗
                case 16:
                    op = new DongWuQiOperator();
                    break;
                //迷雾阵
                case 18:
                //迷雾阵 
                case 17:
                    op = new MiWuOperator();
                    break;
                //骑兵营
                case 19:
                    op = new QiBingYingOperator();
                    break;
                //弓弩营
                case 20:
                    op = new GongNuYingOperator();
                    break;
                //步兵营
                case 21:
                    op = new BuBingYingOperator();
                    break;
                //长持营
                case 22:
                    op = new ChangChiYingOperator();
                    break;
                //战船营
                case 23:
                    op = new ZhanChuanYingOperator();
                    break;
                default:
                    op = new BlankTowerOperator();
                    break;
            }

            op.Init(card, this);
            return op;
        }

        #region RoundTrigger

        protected override void InvokeRoundEndTriggers()
        {
            //卡牌回合结束触发器
            StatusMap.Keys
                .Where(o => o.IsAlive)
                .ToList().ForEach(o=>o.OnRoundEnd());
            //buff触发器
            foreach (var bo in GetBuffOperator(b => b.IsRoundEndTrigger))//buff回合结束触发
            foreach (var op in StatusMap.Keys.Where(o => o != null && !GetStatus(o).IsDeath))
                bo.RoundEnd(op);
        }

        protected override void InvokePreRoundTriggers()
        {
            //卡牌回合开始触发器
            StatusMap.Keys
                .Where(o => o.IsAlive)
                .ToList().ForEach(o => o.OnRoundStart());
            //羁绊触发器
            //玩家方
            OnRoundStartJiBan(Grid.Challenger.Where(p => p.Value.IsPostedAlive)
                .Select(p => GetOperator(p.Value.Operator.InstanceId)).ToArray());
            //对手方
            OnRoundStartJiBan(Grid.Opposite.Where(p => p.Value.IsPostedAlive)
                .Select(p => GetOperator(p.Value.Operator.InstanceId)).ToArray());
            
            //每个回合开始先计算回合制精灵
            foreach (var sprite in ChessSprites.ToArray())
            {
                if (sprite.Host == PosSprite.HostType.Round)
                    sprite.Lasting--;
                if(UpdateRemovable(sprite))continue;

                var pos = Grid.GetChessPos(sprite.Pos, sprite.IsChallenger);
                if (!pos.IsPostedAlive) continue;
                sprite.RoundStart(pos.Operator,this);
            }

            //buff触发器
            foreach (var bo in GetBuffOperator(b => b.IsRoundStartTrigger))
            foreach (var op in StatusMap.Keys.Where(o => o != null && !GetStatus(o).IsDeath))
                bo.RoundStart(op);
        }

        private void OnRoundStartJiBan(ChessOperator[] chessOperators)
        {
            foreach (var jb in JiBan.Where(j => JiBanActivation(j, chessOperators)))
                jb.OnRoundStart(chessOperators);
        }

        protected override ChessOperator GetOperator(int id) => ops.ContainsKey(id) ? ops[id] : null;

        protected override IEnumerable<BuffOperator> GetBuffOperator(Func<BuffOperator, bool> func) =>
            BuffOps.Where(func);

        protected override void OnPlayerResourcesActivity(Activity activity)
        {
            foreach (var conduct in activity.Conducts)
            {
                if (conduct.Element == -1)
                {
                    AddGold(activity.To, conduct);
                    continue;
                }

                AddWarChest(activity.To, conduct);
            }

            void AddGold(int to, CombatConduct c)
            {
                switch (to)
                {
                    case -1: //Challenger
                        ChallengerGold += (int)c.Total;
                        break;
                    case -2: //Opposite
                        OpponentGold += (int)c.Total;
                        break;
                    default: throw new ArgumentOutOfRangeException($"Unknown target({to}) for gold action!");
                }
            }

            void AddWarChest(int to, CombatConduct c)
            {
                switch (to)
                {
                    case -1: //Challenger
                        ChallengerChests.Add(c.Element);
                        break;
                    case -2: //Opposite
                        OpponentChests.Add(c.Element);
                        break;
                    default: throw new ArgumentOutOfRangeException($"Unknown target({to}) for warChest action!");
                }
            }
        }
        #endregion
    }

}