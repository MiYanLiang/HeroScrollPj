﻿using System;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;

namespace Assets.System.WarModule
{
    public class ChessOperatorManager<TCard> : ChessboardOperator where TCard : IChessman
    {
        protected override Dictionary<ChessOperator, ChessStatus> StatusMap { get; }
        protected Dictionary<int, ChessOperator> ops = new Dictionary<int, ChessOperator>();
        protected override List<TerrainSprite> Sprites { get; }
        public int ChallengerGold { get; set; }
        public int OpponentGold { get; set; }
        public List<int> ChallengerChests { get; set; }
        public List<int> OpponentChests { get; set; }
        private BuffOperator[] BuffOps { get; }

        public ChessOperatorManager(bool isChallengerFirst, ChessGrid grid) : base(isChallengerFirst, grid)
        {
            StatusMap = new Dictionary<ChessOperator, ChessStatus>();
            Sprites = new List<TerrainSprite>();
            BuffOps = new BuffOperator[]
            {
                new StunnedBuff(this), //1 眩晕
                new ShieldBuff(this), //2 护甲
                new InvincibleBuff(this), //3 无敌
                new BleedBuff(this), // 4 流血
                new PoisonBuff(this), // 5 毒
                new BurnBuff(this), // 6 灼烧
                new Imprisoned(this), // 8 禁锢
                new CowardlyBuff(this), //9 怯战
                new DisarmedBuff(this), //16 卸甲
                new ExtendedShieldBuff(this), // 19 缓冲盾
            };
        }

        public ChessOperator RegOperator(TCard card)
        {
            var op = InstanceOperator(card);
            StatusMap.Add(op, op.GenerateStatus());
            ops.Add(op.InstanceId,op);
            return op;
        }

        public ChessOperator DropOperator(TCard card)
        {
            if (ops.ContainsKey(card.InstanceId))
                throw new NullReferenceException($"Card[{card.CardId}]:Type.{card.CardType}");
            var op = ops[card.InstanceId];
            ops.Remove(op.InstanceId);
            StatusMap.Remove(op);
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
                    op = new ChiJiaOperator();
                    break; //7   刺甲
                case 8:
                    op = new XiangBingOperator();
                    break; //8   象兵
                case 9:
                    op = new XianFengOperator();
                    break; //9   先锋
                case 10:
                    op = new XianDengOperator();
                    break; //10  先登
                case 11:
                    op = new BaiMaOperator();
                    break; //11  白马
                case 12:
                    op = new ShenWuOperator();
                    break; //12  神武
                case 13:
                    op = new JinWeiOperator();
                    break; //13  禁卫
                case 14:
                    op = new ChangQiangOperator();
                    break; //14  长枪
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
                    op = new LianNuOperator();
                    break; //19  连弩
                case 20:
                    op = new GongBingOperator();
                    break; //20  弓兵
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
                    op = new ChiKeOperator();
                    break; //25  刺客
                case 26:
                    op = new JunShiOperator();
                    break; //26  军师
                case 27:
                    op = new DaJunShiOperator();
                    break; //27  大军师
                case 28:
                    op = new ShuShiOperator();
                    break; //28  术士
                case 29:
                    op = new DaShuShiOperator();
                    break; //29  大术士
                case 30:
                    op = new DuShiOperator();
                    break; //30  毒士
                case 31:
                    op = new DaDuShiOperator();
                    break; //31  大毒士
                case 32:
                    op = new TongShuaiOperator();
                    break; //32  统帅
                case 33:
                    op = new DaTongShuaiOperator();
                    break; //33  大统帅
                case 34:
                    op = new BianShiOperator();
                    break; //34  辩士
                case 35:
                    op = new DaBianShiOperator();
                    break; //35  大辩士
                case 36:
                    op = new MouShiOperator();
                    break; //36  谋士
                case 37:
                    op = new DaMouShiOperator();
                    break; //37  大谋士
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
                    op = new YiShiOperator();
                    break; //42  医师
                case 43:
                    op = new DaYiShiOperator();
                    break; //43  大医师
                case 44:
                    op = new JingGuoOperator();
                    break; //44  巾帼
                case 45:
                    op = new MeiRenOperator();
                    break; //45  美人
                case 46:
                    op = new DaMeiRenOperator();
                    break; //46  大美人
                case 47:
                    op = new ShuiKeOperator();
                    break; //47  说客
                case 48:
                    op = new DaShuiKeOperator();
                    break; //48  大说客
                case 49:
                    op = new HeroOperator();
                    break; //49  弩兵
                case 50:
                    op = new HeroOperator();
                    break; //50  文士
                case 51:
                    op = new QiangNuOperator();
                    break; //51  强弩
                case 52:
                    op = new DaGongOperator();
                    break; //52  大弓
                case 53:
                    op = new YinShiOperator();
                    break; //53  隐士
                case 54:
                    op = new DaYinShiOperator();
                    break; //54  大隐士
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
                    op = new QiangOperator();
                    break; //59  短枪
                case 60:
                    op = new JiXianFengOperator();
                    break; //60  急先锋
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

        protected override RoundAction GetRoundEndTriggerByOperators()
        {
            var ra = new RoundAction();
            foreach (var bo in GetBuffOperator(b => b.IsRoundEndTrigger))
            foreach (var op in StatusMap.Keys.Where(o => o != null && !GetStatus(o).IsDeath))
                bo.RoundEnd(op);
            ProceedRoundCondition(o => o.OnRoundEnd(), ra);
            return ra;
        }

        protected override RoundAction GetPreRoundTriggerByOperators()
        {
            //每个回合开始先计算回合制精灵
            foreach (var sprite in ChessSprites.Where(s => s.Lasting == TerrainSprite.LastingType.Round).ToArray())
            {
                sprite.Value--;
                if (sprite.Value <= 0)
                    RemoveSprite(sprite);
            }

            var ra = new RoundAction();
            foreach (var bo in GetBuffOperator(b => b.IsRoundStartTrigger))
            foreach (var op in StatusMap.Keys.Where(o => o != null && !GetStatus(o).IsDeath))
                bo.RoundStart(op);
            ProceedRoundCondition(o => o.OnRoundStart(), ra);
            return ra;
        }

        protected override void RoundActionInvocation(int roundKey, IEnumerable<Activity> activities)
        {
            switch (roundKey)
            {
                case RoundAction.PlayerResources:
                    ActionForeach(activities, RoundActionPlayerResources);
                    break;
                case RoundAction.RoundBuffing:
                    ActionForeach(activities, RoundActionBuffering);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown RoundKey({roundKey})!");
            }

            void ActionForeach(IEnumerable<Activity> list, Action<Activity> action)
            {
                foreach (var activity in list) action(activity);
            }
        }

        protected override ChessOperator GetOperator(int id) => ops.ContainsKey(id) ? ops[id] : null;

        protected override IEnumerable<BuffOperator> GetBuffOperator(Func<BuffOperator, bool> func) =>
            BuffOps.Where(func);

        private void RoundActionBuffering(Activity activity) =>
            AppendActivityWithoutOffender(GetOperator(activity.To), activity.Intent, activity.Conducts, 0);

        private void RoundActionPlayerResources(Activity activity)
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

        protected void ProceedRoundCondition(
            Func<ChessOperator, IEnumerable<KeyValuePair<int, IEnumerable<Activity>>>> func, RoundAction roundAction)
        {
            var roundActions = StatusMap.Keys
                .Select(func)
                .Where(o => o != null)
                .SelectMany(o => o)
                .GroupBy(g => g.Key, g => g.Value)
                .ToDictionary(g => g.Key, g => g.SelectMany(s => s));
            roundAction.Activities = roundActions.ToDictionary(a => a.Key, a => a.Value.ToList());
        }
        #endregion
    }

}