using System;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using UnityEngine;

public class DataTable : MonoBehaviour
{
    private const string TableSuffix = "Table";
    private const string DataSuffix = "Data";
    private static Type IntType = typeof(int);
    private static Type StringType = typeof(string);
    private static Type DataType = typeof(Dictionary<int, IReadOnlyList<string>>);
    private static Type TextAssetType = typeof(TextAsset);
    private static Type ArrayType = typeof(Array);

    public static DataTable instance;
    //private static TableData Data { get; set; }

    public static IReadOnlyDictionary<int, PlayerInitialConfigTable> PlayerInitialConfig { get; private set; }
    public static IReadOnlyDictionary<int, ResourceConfigTable> ResourceConfig { get; private set; }
    public static IReadOnlyDictionary<int, HeroTable> Hero { get; private set; }
    public static IReadOnlyDictionary<int, PlayerLevelConfigTable> PlayerLevelConfig { get; private set; }
    public static IReadOnlyDictionary<int, TowerTable> Tower { get; private set; }
    public static IReadOnlyDictionary<int, MilitaryTable> Military { get; private set; }
    public static IReadOnlyDictionary<int, CardLevelTable> CardLevel { get; private set; }
    public static IReadOnlyDictionary<int, TrapTable> Trap { get; private set; }
    public static IReadOnlyDictionary<int, WarChestTable> WarChest { get; private set; }
    public static IReadOnlyDictionary<int, WarTable> War { get; private set; }
    public static IReadOnlyDictionary<int, BaseLevelTable> BaseLevel { get; private set; }
    public static IReadOnlyDictionary<int, CheckpointTable> Checkpoint { get; private set; }
    public static IReadOnlyDictionary<int, BattleEventTable> BattleEvent { get; private set; }
    public static IReadOnlyDictionary<int, EnemyTable> Enemy { get; private set; }
    public static IReadOnlyDictionary<int, EnemyUnitTable> EnemyUnit { get; private set; }
    public static IReadOnlyDictionary<int, QuestTable> Quest { get; private set; }
    public static IReadOnlyDictionary<int, QuestRewardTable> QuestReward { get; private set; }
    public static IReadOnlyDictionary<int, MercenaryTable> Mercenary { get; private set; }
    public static IReadOnlyDictionary<int, GameModeTable> GameMode { get; private set; }
    public static IReadOnlyDictionary<int, GuideTable> Guide { get; private set; }
    public static IReadOnlyDictionary<int, TipsTable> Tips { get; private set; }
    public static IReadOnlyDictionary<int, RCodeTable> RCode { get; private set; }
    public static IReadOnlyDictionary<int, ChickenTable> Chicken { get; private set; }
    public static IReadOnlyDictionary<int, StaticArrangementTable> StaticArrangement { get; private set; }
    public static IReadOnlyDictionary<int, TextTable> Text { get; private set; }
    public static IReadOnlyDictionary<int, NumericalConfigTable> NumericalConfig { get; private set; }
    public static IReadOnlyDictionary<int, JiBanTable> JiBan { get; private set; }
    public static IReadOnlyDictionary<int, ForceTable> Force { get; private set; }
    public static IReadOnlyDictionary<int, BaYeCityTable> BaYeCity { get; private set; }
    public static IReadOnlyDictionary<int, BaYeCityEventTable> BaYeCityEvent { get; private set; }
    public static IReadOnlyDictionary<int, BaYeLevelMappingTable> BaYeLevelMapping { get; private set; }
    public static IReadOnlyDictionary<int, BaYeTaskTable> BaYeTask { get; private set; }
    public static IReadOnlyDictionary<int, BaYeStoryPoolTable> BaYeStoryPool { get; private set; }
    public static IReadOnlyDictionary<int, BaYeStoryEventTable> BaYeStoryEvent { get; private set; }
    public static IReadOnlyDictionary<int, BaYeTvTable> BaYeTv { get; private set; }
    public static IReadOnlyDictionary<int, BaYeNameTable> BaYeName { get; private set; }
    public static IReadOnlyDictionary<int, CityTable> City { get; private set; }
    public static IReadOnlyDictionary<int, PlayerNameTable> PlayerName { get; set; }
    public static IReadOnlyDictionary<int, PlayerNicknameTable> PlayerNickname { get; set; }
    public static IReadOnlyDictionary<int, PlayerSignTable> PlayerSign { get; set; }
    public static IReadOnlyDictionary<int, DirtyWordTable> DirtyWord { get; set; }

    public TextAsset PlayerInitialConfigTable;
    public TextAsset ResourceConfigTable;
    public TextAsset HeroTable;
    public TextAsset PlayerLevelConfigTable;
    public TextAsset TowerTable;
    public TextAsset MilitaryTable;
    public TextAsset CardLevelTable;
    public TextAsset TrapTable;
    public TextAsset WarChestTable;
    public TextAsset WarTable;
    public TextAsset BaseLevelTable;
    public TextAsset CheckpointTable;
    public TextAsset BattleEventTable;
    public TextAsset EnemyTable;
    public TextAsset EnemyUnitTable;
    public TextAsset QuestTable;
    public TextAsset QuestRewardTable;
    public TextAsset MercenaryTable;
    public TextAsset GameModeTable;
    public TextAsset GuideTable;
    public TextAsset TipsTable;
    public TextAsset RCodeTable;
    public TextAsset ChickenTable;
    public TextAsset StaticArrangementTable;
    public TextAsset TextTable;
    public TextAsset NumericalConfigTable;
    public TextAsset JiBanTable;
    public TextAsset ForceTable;
    public TextAsset BaYeCityTable;
    public TextAsset BaYeCityEventTable;
    public TextAsset BaYeLevelMappingTable;
    public TextAsset BaYeTaskTable;
    public TextAsset BaYeStoryPoolTable;
    public TextAsset BaYeStoryEventTable;
    public TextAsset BaYeTvTable;
    public TextAsset BaYeNameTable;
    public TextAsset CityTable;
    public TextAsset PlayerNameTable;
    public TextAsset PlayerNicknameTable;
    public TextAsset PlayerSignTable;
    public TextAsset DirtyWordTable;

    //private static Dictionary<string, Dictionary<int, IReadOnlyList<string>>> data;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    public void Init()
    {
        PlayerInitialConfig = NewConvert(PlayerInitialConfigTable.text, l => new PlayerInitialConfigTable(l));
        ResourceConfig = NewConvert(ResourceConfigTable.text, l => new ResourceConfigTable(l));
        Hero = NewConvert(HeroTable.text, l => new HeroTable(l));
        PlayerLevelConfig = NewConvert(PlayerLevelConfigTable.text, l => new PlayerLevelConfigTable(l));
        Tower = NewConvert(TowerTable.text, l => new TowerTable(l));
        Military = NewConvert(MilitaryTable.text, l => new MilitaryTable(l));
        CardLevel = NewConvert(CardLevelTable.text, l => new CardLevelTable(l));
        Trap = NewConvert(TrapTable.text, l => new TrapTable(l));
        WarChest = NewConvert(WarChestTable.text, l => new WarChestTable(l));
        War = NewConvert(WarTable.text, l => new WarTable(l));
        BaseLevel = NewConvert(BaseLevelTable.text, l => new BaseLevelTable(l));
        Checkpoint = NewConvert(CheckpointTable.text, l => new CheckpointTable(l));
        BattleEvent = NewConvert(BattleEventTable.text, l => new BattleEventTable(l));
        Enemy = NewConvert(EnemyTable.text, l => new EnemyTable(l));
        EnemyUnit = NewConvert(EnemyUnitTable.text, l => new EnemyUnitTable(l));
        Quest = NewConvert(QuestTable.text, l => new QuestTable(l));
        QuestReward = NewConvert(QuestRewardTable.text, l => new QuestRewardTable(l));
        Mercenary = NewConvert(MercenaryTable.text, l => new MercenaryTable(l));
        GameMode = NewConvert(GameModeTable.text, l => new GameModeTable(l));
        Guide = NewConvert(GuideTable.text, l => new GuideTable(l));
        Tips = NewConvert(TipsTable.text, l => new TipsTable(l));
        RCode = NewConvert(RCodeTable.text, l => new RCodeTable(l));
        Chicken = NewConvert(ChickenTable.text, l => new ChickenTable(l));
        StaticArrangement = NewConvert(StaticArrangementTable.text, l => new StaticArrangementTable(l));
        Text = NewConvert(TextTable.text, l => new TextTable(l));
        NumericalConfig = NewConvert(NumericalConfigTable.text, l => new NumericalConfigTable(l));
        JiBan = NewConvert(JiBanTable.text, l => new JiBanTable(l));
        Force = NewConvert(ForceTable.text, l => new ForceTable(l));
        BaYeCity = NewConvert(BaYeCityTable.text, l => new BaYeCityTable(l));
        BaYeCityEvent = NewConvert(BaYeCityEventTable.text, l => new BaYeCityEventTable(l));
        BaYeLevelMapping = NewConvert(BaYeLevelMappingTable.text, l => new BaYeLevelMappingTable(l));
        BaYeTask = NewConvert(BaYeTaskTable.text, l => new BaYeTaskTable(l));
        BaYeStoryPool = NewConvert(BaYeStoryPoolTable.text, l => new BaYeStoryPoolTable(l));
        BaYeStoryEvent = NewConvert(BaYeStoryEventTable.text, l => new BaYeStoryEventTable(l));
        BaYeTv = NewConvert(BaYeTvTable.text, l => new BaYeTvTable(l));
        BaYeName = NewConvert(BaYeNameTable.text, l => new BaYeNameTable(l));
        City = NewConvert(CityTable.text, l => new CityTable(l));
        PlayerName = NewConvert(PlayerNameTable.text, l => new PlayerNameTable(l));
        PlayerNickname = NewConvert(PlayerNicknameTable.text, l => new PlayerNicknameTable(l));
        PlayerSign = NewConvert(PlayerSignTable.text, l => new PlayerSignTable(l));
        DirtyWord = NewConvert(DirtyWordTable.text, l => new DirtyWordTable(l));
    }

    private static Dictionary<int, T> NewConvert<T>(string text, Func<IList<string>, T> func)
    {
        text = text.Replace(@"\\", @"\");
        return Json.DeserializeList<List<string>>(text).ToDictionary(row => int.Parse(row[0]), row => func(row));
    }

    /// <summary>
    /// 根据id获取文本内容
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static string GetStringText(int id) => Text[id].Text;

    /// <summary>
    /// 根据id获取游戏数值内容
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static int GetGameValue(int id) => NumericalConfig[id].Value;


}