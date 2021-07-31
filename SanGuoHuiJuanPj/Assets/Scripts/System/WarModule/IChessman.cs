using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
/// 棋子当前状态信息，
/// 主要是：血量，状态值
/// </summary>
public class PieceStatus
{
    public static PieceStatus Instance(int hp, int maxHp, int pos, Dictionary<int, int> states) =>
        new PieceStatus(hp, maxHp, pos, states);
    public static PieceStatus Clone(PieceStatus ps) =>
        Instance(ps.Hp, ps.MaxHp, ps.Pos, ps.States.ToDictionary(s => s.Key, s => s.Value));
    public int Hp { get; set; }
    public int Pos { get; set; }
    public int MaxHp { get; set; }
    public Dictionary<int, int> States { get; set; }

    private PieceStatus(int hp, int maxHp, int pos, Dictionary<int, int> states)
    {
        Hp = hp;
        States = states;
        Pos = pos;
        MaxHp = maxHp;
    }
}

/// <summary>
/// 状态行动
/// </summary>
[Serializable]
public class StateAction
{
    /// <summary>
    /// State id , action
    /// </summary>
    [JsonProperty("S")] public Dictionary<int, PieceAction> States { get; set; }
}

///// <summary>
///// 棋子主行动完成后的触发器
///// </summary>
//[Serializable]public class PieceTrigger
//{
//    /// <summary>
//    /// 特殊连击，如果暴击和会心，必定连击
//    /// int[] {Ratio,Times}
//    /// </summary>
//    public const int ComboOnSpecialAttack = -2;
//    /// <summary>
//    /// 连击
//    /// int[] {Ratio,Times}
//    /// </summary>
//    public const int Combo = 1;
//    public const int Gold = 0;
//    public const int WarChest = -1;
//    public static PieceTrigger Instance(int type, int amount) => new PieceTrigger (type, amount);
//    public static PieceTrigger Instance(int type, float amount) => new PieceTrigger (type, amount);
//    public static PieceTrigger Instance(int type, object obj) => new PieceTrigger (type, Json.Serialize(obj));
//    [JsonProperty("T")]public int Type { get; }
//    [JsonProperty("A")]public float Amount { get; }
//    [JsonProperty("S")]public string Value { get; }

//    [JsonConstructor]
//    private PieceTrigger()
//    {

//    }
//    private PieceTrigger(int type, int amount)
//    {
//        Type = type;
//        Amount = amount;
//    }
//    private PieceTrigger(int type, float amount)
//    {
//        Type = type;
//        Amount = amount;
//    }

//    private PieceTrigger(int type, string value)
//    {
//        Type = type;
//        Value = value;
//    }

//    public int GetInt => (int)Amount;

//    public float GetFloat() => Amount;
//    public T GetValue<T>() where T : class => Json.Deserialize<T>(Value);
//}
/// <summary>
/// 棋子行动, 作为棋子的各种行动描述。主要是嵌套在棋格主进程<see cref="ChessPosProcess"/>>的每个一行动描述
/// 分类看<see cref="Intents"/>。
/// </summary>
[Serializable]
public class PieceAction
{
    public enum Intents
    {
        Offensive,
        Friendly,
        Counter,
        Self,
        OffendTrigger,
        FriendlyTrigger
    }
    public static PieceAction Instance(int id, int to, Intents intent, CombatFactor[] factors, FactorAlter alter)
    {
        return new PieceAction()
        {
            InstanceId = id,
            To = to,
            Factors = factors,
            Intent = intent,
            Alter = alter
        };
    }

    [JsonProperty("I")] public int InstanceId { get; set; }
    [JsonProperty("K")] public Intents Intent { get; set; }
    /// <summary>
    /// Target Pos
    /// </summary>
    [JsonProperty("To")] public int To { get; set; }
    /// <summary>
    /// Suffered Factor
    /// </summary>
    [JsonProperty("F")] public CombatFactor[] Factors { get; set; }
    /// <summary>
    /// Factor Result
    /// </summary>
    [JsonProperty("A")] public FactorAlter Alter { get; set; }
}
/// <summary>
/// 棋子行动的影响值
/// </summary>
public struct FactorAlter
{
    public static FactorAlter Instance(int hpAdd, int stateId, int value) => new FactorAlter { Hp = hpAdd, AlterState = new FactorAlterState { StateId = stateId, Value = value } };
    public static FactorAlter Instance(int hpAdd) => Instance(hpAdd, 0, 0);
    public static FactorAlter Instance(int stateId, int value) => Instance(0, stateId, value);

    /// <summary>
    /// Hp to be add
    /// </summary>
    [JsonProperty("H")] public int Hp { get; set; }
    /// <summary>
    /// 状态结果值
    /// </summary>
    [JsonProperty("S")] public FactorAlterState AlterState { get; set; }
}
/// <summary>
/// 棋子行动的状态影响值
/// </summary>
public struct FactorAlterState
{
    [JsonProperty("I")] public int StateId { get; set; }
    [JsonProperty("V")] public int Value { get; set; }
}

[Serializable]
public struct CombatFactor
{
    /// <summary>
    /// 因素类型
    /// </summary>
    public enum Kinds
    {
        Damage = 0,
        Heal = 1,
        State = 2
    }
    ///// <summary>
    ///// 物理伤害类型
    ///// </summary>
    //public enum CombatElements
    //{
    //    /// <summary>
    //    /// 一般物理类型
    //    /// </summary>
    //    GeneralPhysic = 0,
    //    /// <summary>
    //    /// 机械伤害，机械伤害加倍
    //    /// </summary>
    //    MachineryPhysic = -1
    //}
    private static CombatFactor _zeroDamage = InstanceDamage(0);

    [JsonProperty("I")] public int InstanceId { get; set; }
    /// <summary>
    /// 战斗元素类型
    /// </summary>
    [JsonProperty("K")] public Kinds Kind { get; set; }
    /// <summary>
    /// 战斗类<see cref="Kinds.Damage"/> 或 <see cref="Kinds.Heal"/> 将为： 0 = 物理 ，大于0 = 法术元素，小于0 = 特殊物理，
    /// 状态类<see cref="Kinds.State"/>将会是状态Id，详情看 <see cref="FightState.Cons"/>
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

    public static CombatFactor Instance(float value, float critical, float rouse, int element = 0, Kinds kind = Kinds.Damage) =>
        new CombatFactor { Basic = value, Element = element, Critical = critical, Rouse = rouse, Kind = kind };

    public static CombatFactor InstanceHeal(float heal, int element = 0) => Instance(heal, 0, 0, element, Kinds.Heal);

    public static CombatFactor InstanceState(FightState.Cons con, float value = 1) => Instance(value, 0, 0, (int)con, Kinds.State);

    public static CombatFactor InstanceDamage(float damage, int element = 0) => Instance(damage, 0, 0, element);

    public static CombatFactor InstanceDamage(float damage, float critical, int element = 0) =>
        Instance(damage, critical, 0, element);

    public static CombatFactor ZeroDamage => _zeroDamage;
}

/// <summary>
/// 攻击方式。
/// 远程，近战，(不)/可反击单位，兵种系数
/// </summary>
public class AttackStyle
{
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
    /// 攻击元素, 与<see cref="CombatFactor"/>的元素对应。
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
}

/// <summary>
/// 棋子
/// </summary>
public interface IChessman 
{
    int Pos { get; }
    bool IsPlayer { get; }
    bool IsActed { get; }
    bool IsAvailable { get; }
    void SetActed(bool isActed = true);
}
/// <summary>
/// 棋位接口约束
/// </summary>
public interface IChessPos<T> where T : class, IChessman, new()
{
    T Chessman { get; }
    void SetPos(T chessman);
    void RemovePos();
    StateAction GetGapAction();
    PieceStatus GetCurrentStatus();
}

/// <summary>
/// 棋格管理器
/// </summary>
/// <typeparam name="TPos"></typeparam>
/// <typeparam name="TChess"></typeparam>
public class ChessGrid<TChess> where TChess : class, IChessman, new() 
{
    private readonly Dictionary<int, IChessPos<TChess>> _player;
    private readonly Dictionary<int, IChessPos<TChess>> _enemy;
    public IReadOnlyDictionary<int, IChessPos<TChess>> Player => _player;
    public IReadOnlyDictionary<int, IChessPos<TChess>> Enemy => _enemy;

    private static readonly int[][] NeighborCards = new int[20][] {
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

    public ChessGrid(IList<IChessPos<TChess>> player, IList<IChessPos<TChess>> enemy)
    {
        _player = new Dictionary<int, IChessPos<TChess>>();
        _enemy = new Dictionary<int, IChessPos<TChess>>();
        for (var i = 0; i < player.Count; i++) _player.Add(i, player[i]);
        for (var i = 0; i < enemy.Count; i++) _enemy.Add(i, enemy[i]);
    }
    public ChessGrid()
    {
        _player = new Dictionary<int, IChessPos<TChess>>();
        _enemy = new Dictionary<int, IChessPos<TChess>>();
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
    public void Remove(int pos, bool isPlayer) => GetScope(isPlayer)[pos].RemovePos();
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

    public IReadOnlyDictionary<int, IChessPos<TChess>> GetScope(bool isPlayer) => isPlayer ? _player : _enemy;
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
    /// <param name="isPlayer"></param>
    /// <returns></returns>
    public IEnumerable<IChessPos<TChess>> GetNeighbors(int pos, bool isPlayer) =>
        NeighborCards[pos].Select(i => GetScope(isPlayer)[i]).Where(o => o != null);

    public IChessPos<TChess> GetChessPos(int pos, bool isPlayer)
    {
        var scope = GetScope(isPlayer);
        return !scope.ContainsKey(pos) ? null : scope[pos];
    }
}

public abstract class ChessboardOperator<TChess> where TChess : class, IChessman, new()
{
    private ChessGrid<TChess> Grid { get; }
    private int _roundId = 0;
    public int RoundId => _roundId;
    private bool IsPlayerOdd => _isPlayerOdd;
    public bool IsPlayerRound => _isPlayerOdd == IsOddRound;
    public bool IsOddRound => _roundId % 2 == 0;
    public IReadOnlyList<ChessRound> Rounds => rounds;

    #region Static Fields

    private static readonly ChessPosProcess[] EmptyProcess = new ChessPosProcess[0];
    private readonly bool _isPlayerOdd;
    private readonly List<ChessRound> rounds;
    private static int PieceProcessSeed = 0;
    private static int _recursiveActionCount;
    private const int RecursiveActionsLimit = 99999;

    private static int RecursiveActionCount
    {
        get => _recursiveActionCount;
        set
        {
            if (_recursiveActionCount > RecursiveActionsLimit)
                throw new StackOverflowException($"{nameof(RecursiveActionCount)}>={nameof(RecursiveActionsLimit)}");
            _recursiveActionCount = value;
        }
    }

    #endregion

    public ChessboardOperator(bool isPlayerFirst, ChessGrid<TChess> grid)
    {
        _isPlayerOdd = isPlayerFirst;
        rounds = new List<ChessRound>();
        Grid = grid;
    }

    public ChessRound StartRound()
    {
        //instance Round
        //invoke pre-action
        //Get all sorted Chessman operators
        //invoke Chessman operations
        //invoke finalization
        RecursiveActionCount = 0;
        var list = GetSortedChessmanOperators();
        var round = new ChessRound {InstanceId = RoundId};
        round.PreAction = GetRoundPreAction();
        UpdatePosesState(round.PreAction, list);
        var roundProcesses = new List<ChessPosProcess>();
        for (var i = 0; i < list.Length; i++)
        {
            var chessPos = list[i];
            var process = PosInvocation(chessPos);
            if (process == null) continue;
            roundProcesses.Add(process);
        }

        round.Processes = roundProcesses.ToArray();
        round.FinalAction = GetRoundEndAction();
        UpdatePosesState(round.FinalAction, list);
        _roundId++;
        return round;
    }

    protected abstract StateAction GetRoundEndAction();

    protected abstract StateAction GetRoundPreAction();


    protected abstract IPieceOperator<TChess> GetOperator(IChessPos<TChess> chessPos);

    private void UpdatePosesState(StateAction stateAction, IChessPos<TChess>[] posList)
    {
        var list = stateAction.States.Join(posList, s => s.Value.To, p => p.Chessman.Pos,(s,p)=> new {s, p}).ToList();
        foreach (var obj in list)
        {
            var action = obj.s.Value;
            var chessPos = obj.p;
            var op = GetOperator(chessPos);
            // Game card operator respond to the action;
            op.UpdateFactors(action.Factors);
        }
    }


    private ChessPosProcess PosInvocation(IChessPos<TChess> chessPos)
    {
        //invoke pos operation & return pieceProcess
        //finalize pieceProcess by interactive invocation
        var op = GetOperator(chessPos);
        var status = chessPos.GetCurrentStatus();
        var actions = op.MainActions(Grid);
        if (actions == null || actions.Length == 0) return null;
        var finalList = new List<PieceAction>();
        var initStats = PieceStatus.Clone(status); 
        var process = ChessPosProcess.Instance(PieceProcessSeed, RecursiveAction(op, actions, finalList));
        process.Status = initStats;
        PieceProcessSeed++;
        return process;
    }
    
    //递归执行棋子每个行动
    private PieceAction[] RecursiveAction(IPieceOperator<TChess> op, PieceAction[] actions, List<PieceAction> finalList)
    {
        if (actions == null || actions.Length == 0) return null;
        for (int i = 0; i < actions.Length; i++)
        {
            RecursiveActionCount++;
            var pieceAction = actions[i];
            finalList.Add(pieceAction);

            var targetPos = GetTarget(pieceAction);
            var targetOp = GetOperator(targetPos);
            var respondActions = targetOp.Respond(pieceAction, op, Grid);
            //while all actions invoke will add to finalList sequentially
            if (respondActions == null || respondActions.Length <= 0) continue;
            RecursiveAction(targetOp, respondActions, finalList);
        }
        return finalList.ToArray();
    }


    private IChessPos<TChess> GetTarget(PieceAction action)
    {
        var isOpposite = action.Intent == PieceAction.Intents.Offensive ||
                         action.Intent == PieceAction.Intents.Counter ||
                         action.Intent == PieceAction.Intents.OffendTrigger;
        return Grid.GetChessPos(action.To, isOpposite);
    }

    private IChessPos<TChess>[] GetSortedChessmanOperators() =>
        Grid.GetScope(IsPlayerRound).OrderBy(o => o.Key).Select(o => o.Value).ToArray();

}