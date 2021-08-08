using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq;
using Newtonsoft.Json;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 战棋地形
    /// </summary>
    public class ChessTerrain
    {
        public const int Fire = 1;//业火
        public const int Forge = 2;//迷雾
        /// <summary>
        /// 当前状态，Key=类型, Value=回合数
        /// </summary>
        [JsonIgnore]public IReadOnlyDictionary<int, int> Con => con;
        private Dictionary<int, int> con = new Dictionary<int, int>();

        public int GetCondition(int conId) => con.ContainsKey(conId) ? con[conId] : 0;
        /// <summary>
        /// 添加状态
        /// </summary>
        /// <param name="conId">状态Id 参考<see cref="ChessTerrain"/>常数</param>
        /// <param name="value">回合数</param>
        public void AddCondition(int conId,int value = 1)
        {
            if (!con.ContainsKey(conId))
                con.Add(conId, 0);
            con[conId] += value;
        }
    }
    public class BuffStatus
    {
        public Dictionary<int, int> Buffs { get; set; } = new Dictionary<int, int>();
        [JsonIgnore] public List<int> LastSuffers { get; } = new List<int>();
        public int GetBuff(int buffId) => Buffs.ContainsKey(buffId) ? Buffs[buffId] : 0;
        public int GetBuff(FightState.Cons buff) => GetBuff((int) (FightState.Cons) (int) buff);
        public bool TryDeplete(FightState.Cons buff) => TryDeplete((int) buff);

        public bool TryDeplete(int buffId)
        {
            if (GetBuff(buffId) <= 0) return false;
            Buffs[buffId]--;
            RefreshBuffs();
            return true;
        }

        public void AddBuff(FightState.Cons con, int value = 1) => AddBuff((int) con, value);
        public void AddBuff(int buffId, int value)
        {
            if (Buffs.ContainsKey(buffId))
                Buffs.Add(buffId, 0);
            Buffs[buffId] += value;
            //去掉负数或是0的状态
            RefreshBuffs();
        }

        protected void RefreshBuffs()
        {
            if (Buffs.Any(s => s.Value <= 0))
                Buffs = Buffs.Where(s => s.Value > 0).ToDictionary(s => s.Key, s => s.Value);
        }

        public void ClearBuff(int element)
        {
            if (Buffs.ContainsKey(element))
                Buffs.Remove(element);
        }
    }

    /// <summary>
    /// 棋子当前状态信息，
    /// 主要是：血量，状态值
    /// </summary>
    public class PieceStatus : BuffStatus
    {
        public static PieceStatus Instance(int hp, int maxHp, int pos, Dictionary<int, int> states) =>
            new PieceStatus(hp, maxHp, pos, states);
        public static PieceStatus Instance(PieceStatus ps) =>
            Instance(ps.Hp, ps.MaxHp, ps.Pos, ps.Buffs.ToDictionary(s => s.Key, s => s.Value));
        public int Hp { get; set; }
        public int Pos { get; set; }
        public int MaxHp { get; set; }
        [JsonIgnore] public bool IsDeath => Hp <= 0;
        [JsonIgnore] public float HpRate => 1f * Hp / MaxHp;

        private PieceStatus(int hp, int maxHp, int pos, Dictionary<int, int> buffs)
        {
            Hp = hp;
            Buffs = buffs;
            Pos = pos;
            MaxHp = maxHp;
        }

        public PieceStatus Clone() => Instance(this);

        public void SubtractHp(int damage)
        {
            LastSuffers.Add(damage);
            if (damage > 0) Hp -= OffsetExtendedHp(damage);
            else Hp -= damage;
        }

        private int OffsetExtendedHp(int value)
        {
            if (!Buffs.ContainsKey(19)) return value;
            var extended = Buffs[19];
            var balance = 0;
            if (extended <= value)
            {
                balance = value - extended;
                Buffs[19] = 0;
            }
            else Buffs[19] -= value;

            RefreshBuffs();
            return balance;
        }

        public override string ToString() => $"[{Hp}/{MaxHp}]Buffs[{Buffs.Count}].LastS[{LastSuffers.Sum()}]";
    }

    /// <summary>
    /// 回合行动，所有行动
    /// </summary>
    [Serializable]
    public class RoundAction
    {
        public const int PlayerResources = -1;
        public const int JiBan = 0;
        public const int RoundBuffing = 1;

        /// <summary>
        /// State id , action
        /// </summary>
        [JsonProperty("A")]
        public Dictionary<int, List<Activity>> Activities { get; set; } = new Dictionary<int, List<Activity>>();

        public void Concat(RoundAction round)
        {
            var dic = round.Activities.ToDictionary(s => s.Key, s => s.Value);
            foreach (var act in dic.Where(act => dic.ContainsKey(act.Key)))
            {
                act.Value.AddRange(dic[act.Key]);
                dic.Remove(act.Key);
            }

            foreach (var obj in dic)
            {
                if (Activities.ContainsKey(obj.Key))
                    throw new InvalidOperationException($"Duplicated key!");
                Activities.Add(obj.Key, obj.Value);
            }
        }
    }

    public class ActivityResult
    {
        /// <summary>
        /// 承受
        /// </summary>
        public const int Suffer = 0;
        /// <summary>
        /// 闪避
        /// </summary>
        public const int Dodge = 1;
        /// <summary>
        /// 同势力，如果同势力表现会不同，例如被同势力伤害了(反击单位)不会反击
        /// </summary>
        public const int Friendly = 2;
        /// <summary>
        /// 盾挡状态
        /// </summary>
        public const int Shield = 4;
        /// <summary>
        /// 无敌状态
        /// </summary>
        public const int Invincible = 5;
        /// <summary>
        /// 防护盾
        /// </summary>
        public const int ExtendedShield = 6;

        public int Result { get; set; }
        public List<Activity> Activities { get; set; } = new List<Activity>();
        public PieceStatus Status { get; set; }
        [JsonIgnore]public bool IsDeath => Status.IsDeath;
        public override string ToString() => $"Res[{Result}].Sta[{Status.Hp}/{Status.MaxHp}].Act[{Activities.Count}]";
    }

    /// <summary>
    /// 棋子对1个对象的1个行动描述。主要是嵌套在棋格主进程<see cref="ChessPosProcess"/>>的每一个行动描述。
    /// 而一个棋子的行动在一次进程中可能会有多个对象的行动。
    /// 注意：如果连击将会产生多个<see cref="Activity"/>
    /// </summary>
    [Serializable]
    public class Activity
    {
        public static Activity[] Empty { get; } = new Activity[0];
        //注意，负数是非棋子行动。一般都是上升到棋手这个维度的东西如：资源，金币
        /// <summary>
        /// 棋手资源类
        /// </summary>
        public const int PlayerResource = -1;
        /// <summary>
        /// 进攻行动
        /// </summary>
        public const int Offensive = 0;
        /// <summary>
        /// 同阵营行动
        /// </summary>
        public const int Friendly = 1;
        /// <summary>
        /// 
        /// </summary>
        public const int Counter = 2;
        /// <summary>
        /// 对自己的行动
        /// </summary>
        public const int Self = 4;
        /// <summary>
        /// 攻击触发器
        /// </summary>
        public const int OffendTrigger = 5;
        /// <summary>
        /// 同阵营触发器
        /// </summary>
        public const int FriendlyTrigger = 6;

        /// <summary>
        /// 生成<see cref="Activity"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="roundId"></param>
        /// <param name="to">正数为棋格，-1=玩家，-2=对手</param>
        /// <param name="intent"></param>
        /// <param name="conducts"></param>
        /// <returns></returns>
        public static Activity Instance(int id,int roundId ,int to, int intent, CombatConduct[] conducts)
        {
            return new Activity()
            {
                InstanceId = id,
                RoundId = roundId,
                To = to,
                Conducts = conducts,
                Intent = intent,
            };
        }

        [JsonProperty("I")] public int InstanceId { get; set; }
        /// <summary>
        /// 行动描述，描述Id参考常数：
        /// <see cref="Offensive"/>,
        /// <see cref="Friendly"/>,
        /// <see cref="Counter"/>,
        /// <see cref="Self"/>,
        /// <see cref="OffendTrigger"/>,
        /// <see cref="FriendlyTrigger"/>,
        /// <see cref="PlayerResource"/>,
        /// </summary>
        [JsonProperty("K")] public int Intent { get; set; }
        /// <summary>
        /// Target Pos, > 0 = ChessPos, -1 = Player, -2 = Opponent
        /// </summary>
        [JsonProperty("T")] public int To { get; set; }
        [JsonProperty("R")] public int RoundId { get; set; }
        [JsonProperty("C")] public CombatConduct[] Conducts { get; set; }
        [JsonProperty("A")] public ActivityResult Result { get; set; }

        public override string ToString() => $"{InstanceId}.Intent[{Intent}].To[{To}].Com[{Conducts.Length}].Res[{Result.Result}]";
    }

    /// <summary>
    /// 战斗执行，描述棋子在战斗中对某一个目标执行的一个战斗行为
    /// </summary>
    [Serializable]
    public struct CombatConduct
    {
        #region Damages 伤害类型
        //正数定义为法术，负数定为物理，而0是基本物理
        public const int PhysicalDmg = 0;
        /// <summary>
        /// 非人类物理伤害，对陷阱伤害双倍
        /// </summary>
        public const int NonHumanDmg = -1;
        #endregion

        #region Kinds 因素类型
        public const int DamageKind = 0;
        public const int HealKind = 1;
        public const int BuffKind = 2;
        public const int RePosKind = 3;
        public const int KillingKind = 4;
        public const int PlayerDegreeKind = 5;
        #endregion
    
        private static CombatConduct _zeroDamage = InstanceDamage(0);

        [JsonProperty("I")] public int InstanceId { get; set; }
        /// <summary>
        /// 战斗元素类型,其余资源类型并不使用这字段，请查<see cref="Element"/>
        /// </summary>
        [JsonProperty("K")] public int Kind { get; set; }
        /// <summary>
        /// 战斗类<see cref="DamageKind"/> 或 <see cref="HealKind"/> 将为： 0 = 物理 ，大于0 = 法术元素，小于0 = 特殊物理，
        /// 状态类<see cref="BuffKind"/>将会是状态Id，详情看 <see cref="FightState.Cons"/>
        /// 移动类<see cref="RePosKind"/>将会是位置Id(PosId)。
        /// 斩杀类<see cref="KillingKind"/>
        /// 如果是玩家维度<see cref="PlayerDegreeKind"/>的资源，将视为资源Id(-1=金币,正数=宝箱id)
        /// </summary>
        [JsonProperty("E")] public int Element { get; set; }
        /// <summary>
        /// 暴击。注：已计算的暴击值
        /// </summary>
        [JsonProperty("C")] public float Critical { get; set; }
        /// <summary>
        /// 会心。注：已计算的会心值
        /// </summary>
        [JsonProperty("R")] public float Rouse { get; set; }
        /// <summary>
        /// 基础值
        /// </summary>
        [JsonProperty("B")] public float Basic { get; set; }
        /// <summary>
        /// 总伤害 = 基础伤害+暴击+会心
        /// </summary>
        public float Total => Basic + Critical + Rouse;

        public static CombatConduct Instance(float value, float critical, float rouse, int element, int kind) =>
            new CombatConduct { Basic = value, Element = element, Critical = critical, Rouse = rouse, Kind = kind };

        public static CombatConduct InstanceKilling() => Instance(0, 0, 0, 0, KillingKind);

        public static CombatConduct InstanceHeal(float heal, int element = 0) => Instance(heal, 0, 0, element, HealKind);
        /// <summary>
        /// 生成状态
        /// </summary>
        /// <param name="con"></param>
        /// <param name="value">默认1，-1为清除状态</param>
        /// <returns></returns>
        public static CombatConduct InstanceBuff(FightState.Cons con, float value = 1) => Instance(value, 0, 0, (int)con, BuffKind);
        public static CombatConduct InstanceRePos(int pos = 0) => Instance(0, 0, 0, pos, BuffKind);

        public static CombatConduct InstanceDamage(float damage, int element = 0) => Instance(damage, 0, 0, element,DamageKind);

        public static CombatConduct InstanceDamage(float damage, float critical, int element = 0) =>
            Instance(damage, critical, 0, element,DamageKind);

        /// <summary>
        /// 生成玩家维度的资源
        /// </summary>
        /// <param name="resourceId">金币=-1，正数=宝箱Id</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static CombatConduct InstancePlayerResource(int resourceId, int value = 1) =>
            Instance(value, 0, 0, resourceId, PlayerDegreeKind);
        public static CombatConduct ZeroDamage => _zeroDamage;

        public override string ToString() => $"{InstanceId}.K[{Kind}].E[{Element}].B[{Basic}].C[{Critical}].R[{Rouse}]";
    }

    /// <summary>
    /// 攻击方式。
    /// 远程，近战，(不)/可反击单位，兵种系数
    /// </summary>
    public class AttackStyle
    {
        public const int MeleeCombatStyle = 0;
        public const int RangeCombatStyle = 1;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="military">兵种</param>
        /// <param name="armedType">兵种系数，塔=-1，陷阱=-2</param>
        /// <param name="combat">攻击类型，近战=0，远程=1</param>
        /// <param name="counter">反击类型，不可反击=0，可反击>0</param>
        /// <param name="element">0=物理，负数=特殊物理，正数=魔法</param>
        /// <returns></returns>
        public static AttackStyle Instance(int military, int armedType, int combat, int counter, int element) => new AttackStyle(military, armedType, combat, counter, element);

        /// <summary>
        /// 普通系
        /// </summary>
        public const int General = 0;
        /// <summary>
        /// 护盾系
        /// </summary>
        public const int Shield = 1;
        /// <summary>
        /// 步兵系
        /// </summary>
        public const int Infantry = 2;
        /// <summary>
        /// 长持系
        /// </summary>
        public const int LongArmed = 3;
        /// <summary>
        /// 短持系
        /// </summary>
        public const int ShortArmed = 4;
        /// <summary>
        /// 骑兵系
        /// </summary>
        public const int Knight = 5;
        /// <summary>
        /// 特种系
        /// </summary>
        public const int Special = 6;
        /// <summary>
        /// 战车系
        /// </summary>
        public const int Chariot = 7;
        /// <summary>
        /// 战船系
        /// </summary>
        public const int Warship = 8;
        /// <summary>
        /// 弓兵系
        /// </summary>
        public const int Archer = 9;
        /// <summary>
        /// 蛮族系
        /// </summary>
        public const int Barbarian = 10;
        /// <summary>
        /// 统御系
        /// </summary>
        public const int Commander = 11;
        /// <summary>
        /// 干扰系
        /// </summary>
        public const int Interfere = 12;
        /// <summary>
        /// 辅助系
        /// </summary>
        public const int Assist = 13;
        /// <summary>
        /// 可反击单位
        /// 0 = no, >1 = counter
        /// </summary>
        public int CounterStyle { get; set; }
        /// <summary>
        /// 攻击分类
        /// 0 = melee, 1 = range
        /// </summary>
        public int CombatStyle { get; set; }
        /// <summary>
        /// 兵种系数
        /// -1 = 塔, -2 陷阱, 正数为兵种系数
        /// </summary>
        public int ArmedType { get; set; }
        /// <summary>
        /// 兵种
        /// </summary>
        public int Military { get; set; }
        /// <summary>
        /// 攻击元素, 与<see cref="CombatConduct"/>的元素对应。
        /// 0=物理，负数为特殊物理，正数为魔法
        /// </summary>
        public int Element { get; set; }

        [JsonConstructor]
        private AttackStyle()
        {
        }

        private AttackStyle(int military, int armedType, int combatStyle, int counterStyle, int element)
        {
            CounterStyle = counterStyle;
            Element = element;
            CombatStyle = combatStyle;
            ArmedType = armedType;
            Military = military;
        }

        public override string ToString() =>
            $"Com[{CombatStyle}].Cou[{CounterStyle}].Arm[{ArmedType}].M[{Military}].E[{Element}]";
    }

    /// <summary>
    /// 棋子接口规范
    /// </summary>
    public interface IChessman 
    {
        int Pos { get; }
        bool IsPlayer { get; }
        bool IsActed { get; }
        bool IsAvailable { get; }
        bool IsAlive { get; }
        void SetActed(bool isActed = true);
    }
    /// <summary>
    /// 棋位接口规范
    /// </summary>
    public interface IChessPos<T> where T : class, IChessman, new()
    {
        T Chessman { get; }
        ChessTerrain Terrain { get; }
        int Pos { get; }
        void SetPos(T chessman);
        void RemovePos();
        void Init(int pos);
    }

    /// <summary>
    /// 棋格管理器
    /// </summary>
    /// <typeparam name="TPos"></typeparam>
    /// <typeparam name="TChess"></typeparam>
    public class ChessGrid<TChess> where TChess : class, IChessman, new() 
    {
        private readonly Dictionary<int, IChessPos<TChess>> _challenger;
        private readonly Dictionary<int, IChessPos<TChess>> opposite;
        public IReadOnlyDictionary<int, IChessPos<TChess>> Challenger => _challenger;
        public IReadOnlyDictionary<int, IChessPos<TChess>> Opposite => opposite;
        public int[] FrontRows { get; } = { 0, 1, 2, 3, 4 };

        private static int[][] NeighborCards { get; }= new int[20][] {
            new int[3]{ 2, 3, 5},           //0
            new int[3]{ 2, 4, 6},           //1
            new int[5]{ 0, 1, 5, 6, 7},     //2
            new int[3]{ 0, 5, 8},           //3
            new int[3]{ 1, 6, 9},           //4
            new int[6]{ 0, 2, 3, 7, 8, 10}, //5
            new int[6]{ 1, 2, 4, 7, 9, 11}, //6
            new int[6]{ 2, 5, 6, 10,11,12}, //7
            new int[4]{ 3, 5, 10,13},       //8
            new int[4]{ 4, 6, 11,14},       //9
            new int[6]{ 5, 7, 8, 12,13,15}, //10
            new int[6]{ 6, 7, 9, 12,14,16}, //11
            new int[6]{ 7, 10,11,15,16,17}, //12
            new int[4]{ 8, 10,15,18},       //13
            new int[4]{ 9, 11,16,19},       //14
            new int[5]{ 10,12,13,17,18},    //15
            new int[5]{ 11,12,14,17,19},    //16
            new int[3]{ 12,15,16},          //17
            new int[2]{ 13,15},             //18
            new int[2]{ 14,16},             //19
        };
        //位置列攻击目标选择次序
        private static int[][] AttackSelectionOrder { get; }= new int[5][]
        {
            new int[11]{ 0, 2, 3, 5, 7, 8, 10,12,13,15,17},     //0列
            new int[11]{ 1, 2, 4, 6, 7, 9, 11,12,14,16,17},     //1列
            new int[12]{ 0, 1, 2, 5, 6, 7, 10,11,12,15,16,17},  //2列
            new int[8] { 0, 3, 5, 8, 10,13,15,17},              //3列
            new int[8] { 1, 4, 6, 9, 11,14,16,17},              //4列
        };

        public ChessGrid(IList<IChessPos<TChess>> player, IList<IChessPos<TChess>> enemy)
        {
            _challenger = new Dictionary<int, IChessPos<TChess>>();
            opposite = new Dictionary<int, IChessPos<TChess>>();
            for (var i = 0; i < player.Count; i++)
            {
                player[i].Init(i);
                _challenger.Add(i, player[i]);
            }
            for (var i = 0; i < enemy.Count; i++)
            {
                enemy[i].Init(i);
                opposite.Add(i, enemy[i]);
            }
        }
        public ChessGrid()
        {
            _challenger = new Dictionary<int, IChessPos<TChess>>();
            opposite = new Dictionary<int, IChessPos<TChess>>();
        }

        /// <summary>
        /// 设置棋位
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="obj"></param>
        public void Set(int pos, TChess obj) => Replace(pos, obj);
        /// <summary>
        /// 替换棋位，如果改位置有棋子，返回棋子，否则返回null
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public TChess Replace(int pos, TChess obj)
        {
            var chessPos = GetScope(obj.IsPlayer)[pos];
            var last = chessPos.Chessman;
            chessPos.SetPos(obj);
            return last;
        }
        /// <summary>
        /// 移除棋子
        /// </summary>
        public void Remove(int pos, bool isChallenger) => GetScope(isChallenger)[pos].RemovePos();
        /// <summary>
        /// 移除棋子
        /// </summary>
        /// <param name="obj"></param>
        public void Remove(TChess obj) => Remove(obj.Pos, obj.IsPlayer);

        public IChessPos<TChess> BackPos(IChessPos<TChess> pos)
        {
            var scope = GetScope(pos.Chessman.IsPlayer);
            var backPos = pos.Chessman.Pos + 5;
            return scope.ContainsKey(backPos) ? scope[backPos] : null;
        }

        public IReadOnlyDictionary<int, IChessPos<TChess>> GetScope(bool isChallenger) => isChallenger ? _challenger : opposite;
        public IReadOnlyDictionary<int, IChessPos<TChess>> GetRivalScope(bool isChallenger) => GetScope(!isChallenger);
        public IReadOnlyDictionary<int, IChessPos<TChess>> GetRivalScope(IChessman chessman) => GetRivalScope(chessman.IsPlayer);
        /// <summary>
        /// 获取改棋位的周围棋位
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public int[] GetNeighborIndexes(int pos) => NeighborCards[pos];
        /// <summary>
        /// 获取该棋位的周围棋子
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="isChallenger"></param>
        /// <returns></returns>
        public IEnumerable<IChessPos<TChess>> GetNeighbors(int pos, bool isChallenger) =>
            NeighborCards[pos].Select(i => GetScope(isChallenger)[i]).Where(o => o != null);

        public IChessPos<TChess> GetRivalChessPos(int pos, IChessman chessman) => GetRivalChessPos(pos, chessman.IsPlayer);
        public IChessPos<TChess> GetRivalChessPos(int pos, bool isChallenger) => GetChessPos(pos, !isChallenger);
        public IChessPos<TChess> GetChessPos(int pos, bool isChallenger)
        {
            var scope = GetScope(isChallenger);
            return !scope.ContainsKey(pos) ? null : scope[pos];
        }

        public IEnumerable<IChessPos<TChess>> GetFriendlyNeighbors(IChessman chessman) => GetNeighbors(chessman.Pos, chessman.IsPlayer);

        public IChessPos<TChess> GetChessmanInSequence(bool isChallenger,Func<IChessPos<TChess>,bool> condition) => GetScope(isChallenger).OrderBy(c => c.Key).Select(c => c.Value).FirstOrDefault(condition);
        /// <summary>
        /// 获取以对位开始排列的目标,
        /// 默认 p => p.Chessman != null && p.Chessman.IsAlive
        /// </summary>
        /// <param name="chessman"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public IChessPos<TChess> GetContraPositionInSequence(IChessman chessman,Func<IChessPos<TChess>,bool> condition = null)
        {
            condition ??= p => p.Chessman != null && p.Chessman.IsAlive;
            var series = AttackSelectionOrder[chessman.Pos % 5];
            return GetRivalScope(chessman).Join(series, p => p.Key, s => s, (p, _) => p.Value).FirstOrDefault(condition);
        }

        public IChessPos<TChess> GetChessPos(IChessman chessman) => GetChessPos(chessman.Pos, chessman.IsPlayer);
    }
}