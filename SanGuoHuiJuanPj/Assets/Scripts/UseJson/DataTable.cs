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

        public static IReadOnlyDictionary<int, PlayerInitialConfigTable> PlayerInitialConfig{get; private set;}
        public static IReadOnlyDictionary<int, ResourceConfigTable> ResourceConfig{get; private set;}          
        public static IReadOnlyDictionary<int, HeroTable> Hero{get; private set;}                              
        public static IReadOnlyDictionary<int, PlayerLevelConfigTable> PlayerLevelConfig{get; private set;}    
        public static IReadOnlyDictionary<int, TowerTable> Tower{get; private set;}                            
        public static IReadOnlyDictionary<int, MilitaryTable> Military{get; private set;}                      
        public static IReadOnlyDictionary<int, CardLevelTable> CardLevel{get; private set;}                    
        public static IReadOnlyDictionary<int, TrapTable> Trap{get; private set;}                              
        public static IReadOnlyDictionary<int, WarChestTable> WarChest{get; private set;}                      
        public static IReadOnlyDictionary<int, WarTable> War{get; private set;}                                
        public static IReadOnlyDictionary<int, BaseLevelTable> BaseLevel{get; private set;}                    
        public static IReadOnlyDictionary<int, CheckpointTable> Checkpoint{get; private set;}                  
        public static IReadOnlyDictionary<int, BattleEventTable> BattleEvent{get; private set;}                
        public static IReadOnlyDictionary<int, EnemyTable> Enemy{get; private set;}                            
        public static IReadOnlyDictionary<int, EnemyUnitTable> EnemyUnit{get; private set;}                    
        public static IReadOnlyDictionary<int, QuestTable> Quest{get; private set;}                            
        public static IReadOnlyDictionary<int, QuestRewardTable> QuestReward{get; private set;}                
        public static IReadOnlyDictionary<int, MercenaryTable> Mercenary{get; private set;}                    
        public static IReadOnlyDictionary<int, GameModeTable> GameMode{get; private set;}                      
        public static IReadOnlyDictionary<int, GuideTable> Guide{get; private set;}                            
        public static IReadOnlyDictionary<int, TipsTable> Tips{get; private set;}                              
        //public static IReadOnlyDictionary<int, RCodeTable> RCode{get; private set;}                            
        public static IReadOnlyDictionary<int, ChickenTable> Chicken{get; private set;}                        
        public static IReadOnlyDictionary<int, StaticArrangementTable> StaticArrangement{get; private set;}    
        public static IReadOnlyDictionary<int, TextTable> Text{get; private set;}                              
        public static IReadOnlyDictionary<int, NumericalConfigTable> NumericalConfig{get; private set;}        
        public static IReadOnlyDictionary<int, JiBanTable> JiBan{get; private set;}                            
        public static IReadOnlyDictionary<int, ForceTable> Force{get; private set;}                            
        public static IReadOnlyDictionary<int, BaYeCityTable> BaYeCity{get; private set;}                      
        public static IReadOnlyDictionary<int, BaYeCityEventTable> BaYeCityEvent{get; private set;}            
        public static IReadOnlyDictionary<int, BaYeLevelMappingTable> BaYeLevelMapping{get; private set;}      
        public static IReadOnlyDictionary<int, BaYeTaskTable> BaYeTask{get; private set;}                      
        public static IReadOnlyDictionary<int, BaYeStoryPoolTable> BaYeStoryPool{get; private set;}            
        public static IReadOnlyDictionary<int, BaYeStoryEventTable> BaYeStoryEvent{get; private set;}          
        public static IReadOnlyDictionary<int, BaYeTvTable> BaYeTv{get; private set;}                          
        public static IReadOnlyDictionary<int, BaYeNameTable> BaYeName{get; private set;}
        public static IReadOnlyDictionary<int, CityTable> City { get; private set; }
        public static IReadOnlyDictionary<int,PlayerNameTable>PlayerName{get;set;}
        public static IReadOnlyDictionary<int,PlayerNicknameTable>PlayerNickname{get;set;}
        public static IReadOnlyDictionary<int,PlayerSignTable>PlayerSign{get;set;}
        public static IReadOnlyDictionary<int,DirtyWordTable>DirtyWord{get;set;}

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
            //RCode = NewConvert(RCodeTable.text, l => new RCodeTable(l));
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

        private static Dictionary<int, T> NewConvert<T>(string text,Func<IList<string>,T> func)
        {
            text = text.Replace(@"\\", @"\");
            try
            {
                return Json.DeserializeList<List<string>>(text)
                    .ToDictionary(row => int.Parse(row[0]), row => func(row));
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"{typeof(T).Name} 数据表异常，请检查结构！");
#endif
                throw;
            }

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
//#region 主要数据表
//public class PlayerInitialConfigTable
//{
//    public int Id { get; set; }
//    public string Force { get; set; }
//    public int[] InitialHero { get; set; }
//    public int[] InitialTower { get; set; }
//    public string ForceIntro { get; set; }

//    public PlayerInitialConfigTable()
//    {
        
//    }

//    public PlayerInitialConfigTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Force = d[1];
//        InitialHero = d[2].TableStringToInts().ToArray();
//        InitialTower = d[3].TableStringToInts().ToArray();
//        ForceIntro = d[4];
//    }
//}
//public class ResourceConfigTable
//{
//    public int Id { get; set; }
//    public string Type { get; set; }
//    public int NewPlayerValue { get; set; }
//    public int Limit { get; set; }

//    public ResourceConfigTable()
//    {
        
//    }

//    public ResourceConfigTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Type = d[1];
//        NewPlayerValue = int.Parse(d[2]);
//        Limit = d[3].IntParse();
//    }
//}

//public class PlayerLevelConfigTable
//{
//    public int Level { get; set; }
//    public int Exp { get; set; }
//    public int CardLimit { get; set; }
//    public int BaseHpAddOn { get; set; }
//    public int YuanZhengWarTableId { get; set; }
//    public int UnlockForces { get; set; }
//    public int[] BaYeCityPoints { get; set; }
//    public int[] BaYeStoryPoints { get; set; }
//    public int BaYeLevel { get; set; }
//    public int[] BaYeForceLings { get; set; }
//    public int BaYeLevelMappingId { get; set; }

//    public PlayerLevelConfigTable()
//    {
        
//    }

//    public PlayerLevelConfigTable(IList<string> d)
//    {
//        Level = d[0].IntParse();
//        Exp = d[1].IntParse();
//        CardLimit = d[2].IntParse();
//        BaseHpAddOn = d[3].IntParse();
//        YuanZhengWarTableId = d[4].IntParse();
//        UnlockForces = d[5].IntParse();
//        BaYeCityPoints = d[6].TableStringToInts().ToArray();
//        BaYeStoryPoints = d[7].TableStringToInts().ToArray();
//        BaYeLevel = d[8].IntParse();
//        BaYeForceLings = d[9].TableStringToInts().ToArray();
//        BaYeLevelMappingId = d[10].IntParse();
//    }

//}
//public class CardLevelTable
//{
//    public int Star { get; set; }
//    public int ChipsConsume { get; set; }
//    public int YuanBaoConsume { get; set; }

//    public CardLevelTable()
//    {
        
//    }

//    public CardLevelTable(IList<string> d)
//    {
//        Star = d[0].IntParse();
//        ChipsConsume = d[1].IntParse();
//        YuanBaoConsume = d[2].IntParse();
//    }
//}
//public class MilitaryTable
//{
//    public int Id { get; set; }
//    public string Type { get; set; }
//    public string Specialty { get; set; }
//    public string Short { get; set; }
//    public string Info { get; set; }
//    public int ArmedType { get; set; }

//    public MilitaryTable()
//    {
        
//    }

//    public MilitaryTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Type = d[1];
//        Specialty = d[2];
//        Short = d[3];
//        Info = d[4];
//        ArmedType = d[5].IntParse();
//    }
//}
//public class HeroTable
//{
//    public int Id { get; set; }
//    public string Name { get; set; }
//    public string Intro { get; set; }
//    public int Rarity { get; set; }
//    public int Price { get; set; }
//    public int MilitaryUnitTableId { get; set; }
//    public int ForceTableId { get; set; }
//    public int[] Damages { get; set; }
//    public int[] Hps { get; set; }
//    public int GameSetRecovery { get; set; }
//    public int DodgeRatio { get; set; }
//    public int ArmorResist { get; set; }
//    public int CriticalRatio { get; set; }
//    public int CriticalDamage { get; set; }
//    public int RouseRatio { get; set; }
//    public int RouseDamage { get; set; }
//    public int ImageId { get; set; }
//    public int CombatType { get; set; }
//    public int DamageType { get; set; }
//    public WeightCardProduction ZhanYiChestProduction { get; set; }
//    public WeightCardProduction ConsumeChestProduction { get; set; }
//    public int IsProduce { get; set; }
//    public int ConditionResist { get; set; }
//    public int PhysicalResist { get; set; }
//    public int MagicResist { get; set; }
//    public int[] JiBanIds { get; set; }

//    public HeroTable()
//    {
        
//    }

//    public HeroTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Name = d[1];
//        Intro = d[2];
//        Rarity = d[3].IntParse();
//        Price = d[4].IntParse();
//        MilitaryUnitTableId = d[5].IntParse();
//        ForceTableId = d[6].IntParse();
//        Damages = d[7].TableStringToInts().ToArray();
//        Hps = d[8].TableStringToInts().ToArray();
//        GameSetRecovery = d[9].IntParse();
//        DodgeRatio = d[10].IntParse();
//        ArmorResist = d[11].IntParse();
//        CriticalRatio = d[12].IntParse();
//        CriticalDamage = d[13].IntParse();
//        RouseRatio = d[14].IntParse();
//        RouseDamage = d[15].IntParse();
//        ImageId = d[16].IntParse();
//        CombatType = d[17].IntParse();
//        DamageType = d[18].IntParse();
//        ZhanYiChestProduction = TableStructure<WeightCardProduction>.InstanceOrDefault(d[19]);
//        ConsumeChestProduction = TableStructure<WeightCardProduction>.InstanceOrDefault(d[20]);
//        IsProduce = d[21].IntParse();
//        ConditionResist = d[22].IntParse();
//        PhysicalResist = d[23].IntParse();
//        MagicResist = d[24].IntParse();
//        JiBanIds = d[25].TableStringToInts().ToArray();
//    }
//}
//public class TowerTable
//{
//    public int Id { get; set; }
//    public string Name { get; set; }
//    public string Intro { get; set; }
//    public int Rarity { get; set; }
//    public int Price { get; set; }
//    public string Short { get; set; }
//    public int[] Damages { get; set; }
//    public int[] Hps { get; set; }
//    public int GameSetRecovery { get; set; }
//    public int[] ScopeSet { get; set; }
//    public int ImageId { get; set; }
//    public WeightCardProduction ZhanYiChestProduction { get; set; }
//    public WeightCardProduction ConsumeChestProduction { get; set; }
//    public string About { get; set; }
//    public int IsProduce { get; set; }
//    public int ForceId { get; set; }

//    public TowerTable()
//    {
        
//    }

//    public TowerTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Name = d[1];
//        Intro = d[2];
//        Rarity = d[3].IntParse();
//        Price = d[4].IntParse();
//        Short = d[5];
//        Damages = d[6].TableStringToInts().ToArray();
//        Hps = d[7].TableStringToInts().ToArray();
//        GameSetRecovery = d[8].IntParse();
//        ScopeSet = d[9].TableStringToInts().ToArray();
//        ImageId = d[10].IntParse();
//        ZhanYiChestProduction = TableStructure<WeightCardProduction>.InstanceOrDefault(d[11]);
//        ConsumeChestProduction = TableStructure<WeightCardProduction>.InstanceOrDefault(d[12]);
//        About = d[13];
//        IsProduce = d[14].IntParse();
//        ForceId = d[15].IntParse();
//    }
//}
//public class TrapTable
//{
//    public int Id { get; set; }
//    public string Name { get; set; }
//    public string Intro { get; set; }
//    public int Rarity { get; set; }
//    public int Price { get; set; }
//    public string Short { get; set; }
//    public int[] Damages { get; set; }
//    public int[] Hps { get; set; }
//    public int GameSetRecovery { get; set; }
//    public int ImageId { get; set; }
//    public WeightCardProduction ZhanYiChestProduction { get; set; }
//    public WeightCardProduction ConsumeChestProduction { get; set; }
//    public string About { get; set; }
//    public int IsProduce { get; set; }
//    public int ForceId { get; set; }

//    public TrapTable()
//    {
        
//    }

//    public TrapTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Name = d[1];
//        Intro = d[2];
//        Rarity = d[3].IntParse();
//        Price = d[4].IntParse();
//        Short = d[5];
//        Damages = d[6].TableStringToInts().ToArray();
//        Hps = d[7].TableStringToInts().ToArray();
//        GameSetRecovery = d[8].IntParse();
//        ImageId = d[9].IntParse();
//        ZhanYiChestProduction = TableStructure<WeightCardProduction>.InstanceOrDefault(d[10]);
//        ConsumeChestProduction = TableStructure<WeightCardProduction>.InstanceOrDefault(d[11]);
//        About = d[12];
//        IsProduce = d[13].IntParse();
//        ForceId = d[14].IntParse();
//    }
//}
//public class BaseLevelTable
//{
//    public int Level { get; set; }
//    public int Cost { get; set; }
//    public int BaseHp { get; set; }
//    public int CardMax { get; set; }

//    public BaseLevelTable()
//    {
        
//    }

//    public BaseLevelTable(IList<string> d)
//    {
//        Level = d[0].IntParse();
//        Cost = d[1].IntParse();
//        BaseHp = d[2].IntParse();
//        CardMax = d[3].IntParse();
//    }
//}
//public class WarTable
//{
//    public int Id { get; set; }
//    public string Title { get; set; }
//    public string Intro { get; set; }
//    public int BeginPoint { get; set; }
//    public int CheckPoints { get; set; }
//    public CardFixedProduction AchievementCardProduce { get; set; }
//    public ConsumeResources AchievementReward { get; set; }
//    public int[] ForceLimit { get; set; }
//    public string ForceIntro { get; set; }

//    public WarTable()
//    {
        
//    }
//    public WarTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Title = d[1];
//        Intro = d[2];
//        BeginPoint = d[3].IntParse();
//        CheckPoints = d[4].IntParse();
//        AchievementCardProduce = TableStructure<CardFixedProduction>.InstanceOrDefault(d[5]);
//        AchievementReward = TableStructure<ConsumeResources>.InstanceOrDefault(d[6]);
//        ForceLimit = d[7].TableStringToInts().ToArray();
//        ForceIntro = d[8];
//    }
//}
//public class WarChestTable
//{
//    public int Id { get; set; }
//    public string ChestType { get; set; }//已修正
//    public string ChestTitle { get; set; }
//    public int Exp { get; set; }
//    public RangeElement YvQue { get; set; }
//    public RangeElement YuanBao { get; set; }
//    public CardRandomProduction[] Trap { get; set; }
//    public CardRandomProduction[] Tower { get; set; }
//    public CardRandomProduction[] Hero { get; set; }

//    public WarChestTable()
//    {
        
//    }

//    public WarChestTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        ChestType = d[1];
//        ChestTitle = d[2];
//        Exp = d[3].IntParse();
//        YvQue = TableStructure<RangeElement>.InstanceOrDefault(d[4]);
//        YuanBao = TableStructure<RangeElement>.InstanceOrDefault(d[5]);
//        Trap = d[6].TableStringToEnumerable().Select(TableStructure<CardRandomProduction>.InstanceOrDefault).ToArray();
//        Tower = d[7].TableStringToEnumerable().Select(TableStructure<CardRandomProduction>.InstanceOrDefault).ToArray();
//        Hero = d[8].TableStringToEnumerable().Select(TableStructure<CardRandomProduction>.InstanceOrDefault).ToArray();
//    }
//}
//public class CheckpointTable
//{
//    public int Id { get; set; }
//    public int[] Next { get; set; }
//    public string Title { get; set; }
//    public int EventType { get; set; }
//    public int BattleEventTableId { get; set; }
//    public string Intro { get; set; }
//    public int ImageId { get; set; }
//    public int BattleBG { get; set; }
//    public int BattleBGM { get; set; }
//    public string FlagTitle { get; set; }

//    public CheckpointTable()
//    {
        
//    }

//    public CheckpointTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Next = d[1].TableStringToInts().ToArray();
//        Title = d[2];
//        EventType = d[3].IntParse();
//        BattleEventTableId = d[4].IntParse();
//        Intro = d[5];
//        ImageId = d[6].IntParse();
//        BattleBG = d[7].IntParse();
//        BattleBGM = d[8].IntParse();
//        FlagTitle = d[9];
//    }
//}
//public class BattleEventTable//UnrestChessBoardTable
//{
//    public int Id { get; set; }
//    public string EnemyForce { get; set; }
//    public int[] EnemyTableIndexes { get; set; }
//    public int BaseHp { get; set; }
//    public int[] WarChestTableIds { get; set; }
//    public int GoldReward { get; set; }
//    public int IsStaticEnemies { get; set; }

//    public BattleEventTable()
//    {
        
//    }
//    public BattleEventTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        EnemyForce = d[1];
//        EnemyTableIndexes = d[2].TableStringToInts().ToArray();
//        BaseHp = d[3].IntParse();
//        WarChestTableIds = d[4].TableStringToInts().ToArray();
//        GoldReward = d[5].IntParse();
//        IsStaticEnemies = d[6].IntParse();
//    }
//}
//public class EnemyTable
//{
//    public int Id { get; set; }
//    public int Pos1 { get; set; }
//    public int Pos2 { get; set; }
//    public int Pos3 { get; set; }
//    public int Pos4 { get; set; }
//    public int Pos5 { get; set; }
//    public int Pos6 { get; set; }
//    public int Pos7 { get; set; }
//    public int Pos8 { get; set; }
//    public int Pos9 { get; set; }
//    public int Pos10 { get; set; }
//    public int Pos11 { get; set; }
//    public int Pos12 { get; set; }
//    public int Pos13 { get; set; }
//    public int Pos14 { get; set; }
//    public int Pos15 { get; set; }
//    public int Pos16 { get; set; }
//    public int Pos17 { get; set; }
//    public int Pos18 { get; set; }
//    public int Pos19 { get; set; }
//    public int Pos20 { get; set; }

//    public EnemyTable()
//    {
        
//    }

//    public EnemyTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Pos1 = d[1].IntParse();
//        Pos2 = d[2].IntParse();
//        Pos3 = d[3].IntParse();
//        Pos4 = d[4].IntParse();
//        Pos5 = d[5].IntParse();
//        Pos6 = d[6].IntParse();
//        Pos7 = d[7].IntParse();
//        Pos8 = d[8].IntParse();
//        Pos9 = d[9].IntParse();
//        Pos10 = d[10].IntParse();
//        Pos11 = d[11].IntParse();
//        Pos12 = d[12].IntParse();
//        Pos13 = d[13].IntParse();
//        Pos14 = d[14].IntParse();
//        Pos15 = d[15].IntParse();
//        Pos16 = d[16].IntParse();
//        Pos17 = d[17].IntParse();
//        Pos18 = d[18].IntParse();
//        Pos19 = d[19].IntParse();
//        Pos20 = d[20].IntParse();
//    }
//}
//public class EnemyUnitTable
//{
//    public int Id { get; set; }
//    public int CardType { get; set; }
//    public int Rarity { get; set; }
//    public int Star { get; set; }
//    public int GoldReward { get; set; }
//    public int[] WarChest { get; set; }

//    public EnemyUnitTable()
//    {
        
//    }

//    public EnemyUnitTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        CardType = d[1].IntParse();
//        Rarity = d[2].IntParse();
//        Star = d[3].IntParse();
//        GoldReward = d[4].IntParse();
//        WarChest = d[5].TableStringToInts().ToArray();
//    }
//}
//public class QuestTable
//{
//    public int Id { get; set; }
//    public string Question { get; set; }
//    public int Answer { get; set; }
//    public string A { get; set; }
//    public string B { get; set; }
//    public string C { get; set; }
//    public int Weight { get; set; }

//    public QuestTable()
//    {
        
//    }

//    public QuestTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Question = d[1];
//        Answer = d[2].IntParse();
//        A = d[3];
//        B = d[4];
//        C = d[5];
//        Weight = d[6].IntParse();
//    }
//}
//public class QuestRewardTable
//{
//    public int Id { get; set; }
//    public int Weight { get; set; }
//    public CardRarityProduction Produce { get; set; }

//    public QuestRewardTable()
//    {
        
//    }

//    public QuestRewardTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Weight = d[1].IntParse();
//        Produce = TableStructure<CardRarityProduction>.InstanceOrDefault(d[2]);
//    }
//}

//public class MercenaryTable
//{
//    public int Id { get; set; }
//    public int Weight { get; set; }
//    public CardRarityProduction Produce { get; set; }
//    public int Cost { get; set; }//已修正

//    public MercenaryTable()
//    {
        
//    }

//    public MercenaryTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Weight = d[1].IntParse();
//        Produce = TableStructure<CardRarityProduction>.InstanceOrDefault(d[2]);
//        Cost = d[3].IntParse();
//    }
//}
//public class GameModeTable//GameMode
//{
//    public int Id { get; set; }
//    public string Title { get; set; }
//    public RangeElement WarList { get; set; }
//    public int Unlock { get; set; }//已修正
//    public StaminaCost StaminaCost { get; set; }
//    public string Intro { get; set; }

//    public GameModeTable()
//    {
        
//    }

//    public GameModeTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Title = d[1];
//        WarList = TableStructure<RangeElement>.InstanceOrDefault(d[2]);
//        Unlock = d[3].IntParse();
//        StaminaCost = TableStructure<StaminaCost>.InstanceOrDefault(d[4]);
//        Intro = d[5];
//    }
//}
//public class GuideTable
//{
//    public int Id { get; set; }
//    public int MapBg { get; set; }
//    public string Intro { get; set; }
//    public Chessman Card1 { get; set; }
//    public Chessman Card2 { get; set; }
//    public Chessman Card3 { get; set; }
//    public Chessman Card4 { get; set; }
//    public Chessman Card5 { get; set; }
//    public int BaseHp { get; set; }
//    public int EnemyBaseHp { get; set; }
//    public Chessman Pos1 { get; set; }
//    public Chessman Pos2 { get; set; }
//    public Chessman Pos3 { get; set; }
//    public Chessman Pos4 { get; set; }
//    public Chessman Pos5 { get; set; }
//    public Chessman Pos6 { get; set; }
//    public Chessman Pos7 { get; set; }
//    public Chessman Pos8 { get; set; }
//    public Chessman Pos9 { get; set; }
//    public Chessman Pos10 { get; set; }
//    public Chessman Pos11 { get; set; }
//    public Chessman Pos12 { get; set; }
//    public Chessman Pos13 { get; set; }
//    public Chessman Pos14 { get; set; }
//    public Chessman Pos15 { get; set; }
//    public Chessman Pos16 { get; set; }
//    public Chessman Pos17 { get; set; }
//    public Chessman Pos18 { get; set; }
//    public Chessman Pos19 { get; set; }
//    public Chessman Pos20 { get; set; }
//    public Chessman EPos1 { get; set; }
//    public Chessman EPos2 { get; set; }
//    public Chessman EPos3 { get; set; }
//    public Chessman EPos4 { get; set; }
//    public Chessman EPos5 { get; set; }
//    public Chessman EPos6 { get; set; }
//    public Chessman EPos7 { get; set; }
//    public Chessman EPos8 { get; set; }
//    public Chessman EPos9 { get; set; }
//    public Chessman EPos10 { get; set; }
//    public Chessman EPos11 { get; set; }
//    public Chessman EPos12 { get; set; }
//    public Chessman EPos13 { get; set; }
//    public Chessman EPos14 { get; set; }
//    public Chessman EPos15 { get; set; }
//    public Chessman EPos16 { get; set; }
//    public Chessman EPos17 { get; set; }
//    public Chessman EPos18 { get; set; }
//    public Chessman EPos19 { get; set; }
//    public Chessman EPos20 { get; set; }

//    public GuideTable()
//    {
        
//    }

//    public GuideTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        MapBg = d[1].IntParse();
//        Intro = d[2];
//        Card1 = TableStructure<Chessman>.InstanceOrDefault(d[3]);
//        Card2 = TableStructure<Chessman>.InstanceOrDefault(d[4]);
//        Card3 = TableStructure<Chessman>.InstanceOrDefault(d[5]);
//        Card4 = TableStructure<Chessman>.InstanceOrDefault(d[6]);
//        Card5 = TableStructure<Chessman>.InstanceOrDefault(d[7]);
//        BaseHp = d[8].IntParse();
//        EnemyBaseHp = d[9].IntParse();
//        Pos1 = TableStructure<Chessman>.InstanceOrDefault(d[10]);
//        Pos2 = TableStructure<Chessman>.InstanceOrDefault(d[11]);
//        Pos3 = TableStructure<Chessman>.InstanceOrDefault(d[12]);
//        Pos4 = TableStructure<Chessman>.InstanceOrDefault(d[13]);
//        Pos5 = TableStructure<Chessman>.InstanceOrDefault(d[14]);
//        Pos6 = TableStructure<Chessman>.InstanceOrDefault(d[15]);
//        Pos7 = TableStructure<Chessman>.InstanceOrDefault(d[16]);
//        Pos8 = TableStructure<Chessman>.InstanceOrDefault(d[17]);
//        Pos9 = TableStructure<Chessman>.InstanceOrDefault(d[18]);
//        Pos10 = TableStructure<Chessman>.InstanceOrDefault(d[19]);
//        Pos11 = TableStructure<Chessman>.InstanceOrDefault(d[20]);
//        Pos12 = TableStructure<Chessman>.InstanceOrDefault(d[21]);
//        Pos13 = TableStructure<Chessman>.InstanceOrDefault(d[22]);
//        Pos14 = TableStructure<Chessman>.InstanceOrDefault(d[23]);
//        Pos15 = TableStructure<Chessman>.InstanceOrDefault(d[24]);
//        Pos16 = TableStructure<Chessman>.InstanceOrDefault(d[25]);
//        Pos17 = TableStructure<Chessman>.InstanceOrDefault(d[26]);
//        Pos18 = TableStructure<Chessman>.InstanceOrDefault(d[27]);
//        Pos19 = TableStructure<Chessman>.InstanceOrDefault(d[28]);
//        Pos20 = TableStructure<Chessman>.InstanceOrDefault(d[29]);
//        EPos1 = TableStructure<Chessman>.InstanceOrDefault(d[30]);
//        EPos2 = TableStructure<Chessman>.InstanceOrDefault(d[31]);
//        EPos3 = TableStructure<Chessman>.InstanceOrDefault(d[32]);
//        EPos4 = TableStructure<Chessman>.InstanceOrDefault(d[33]);
//        EPos5 = TableStructure<Chessman>.InstanceOrDefault(d[34]);
//        EPos6 = TableStructure<Chessman>.InstanceOrDefault(d[35]);
//        EPos7 = TableStructure<Chessman>.InstanceOrDefault(d[36]);
//        EPos8 = TableStructure<Chessman>.InstanceOrDefault(d[37]);
//        EPos9 = TableStructure<Chessman>.InstanceOrDefault(d[38]);
//        EPos10 = TableStructure<Chessman>.InstanceOrDefault(d[39]);
//        EPos11 = TableStructure<Chessman>.InstanceOrDefault(d[40]);
//        EPos12 = TableStructure<Chessman>.InstanceOrDefault(d[41]);
//        EPos13 = TableStructure<Chessman>.InstanceOrDefault(d[42]);
//        EPos14 = TableStructure<Chessman>.InstanceOrDefault(d[43]);
//        EPos15 = TableStructure<Chessman>.InstanceOrDefault(d[44]);
//        EPos16 = TableStructure<Chessman>.InstanceOrDefault(d[45]);
//        EPos17 = TableStructure<Chessman>.InstanceOrDefault(d[46]);
//        EPos18 = TableStructure<Chessman>.InstanceOrDefault(d[47]);
//        EPos19 = TableStructure<Chessman>.InstanceOrDefault(d[48]);
//        EPos20 = TableStructure<Chessman>.InstanceOrDefault(d[49]);
//    }
//}
//public class TipsTable//Tips
//{
//    public int Id { get; set; }
//    public int Color { get; set; }//已修正
//    public string Text { get; set; }
//    public int Stamina { get; set; }//已修正
//    public RangeElement YuanBaoReward { get; set; }
//    public string Sign { get; set; }

//    public TipsTable()
//    {
        
//    }

//    public TipsTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Color = d[1].IntParse();
//        Text = d[2];
//        Stamina = d[3].IntParse();
//        YuanBaoReward = TableStructure<RangeElement>.InstanceOrDefault(d[4]);
//        Sign = d[5];
//    }
//}
//public class ChickenTable//chicken
//{
//    public int Id { get; set; }
//    public int Stamina { get; set; }//已修正
//    public int YuQueCost { get; set; }//已修正

//    public ChickenTable()
//    {
        
//    }

//    public ChickenTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Stamina = d[1].IntParse();
//        YuQueCost = d[2].IntParse();
//    }
//}
//public class RCodeTable//兑换码RedeemCode
//{
//    public int Id { get; set; }
//    public string Code { get; set; }
//    public string Lasting { get; set; }
//    public string Info { get; set; }
//    public int YuQue { get; set; }//已修正
//    public int YuanBao { get; set; }//已修正
//    public int TiLi { get; set; }//已修正
//    public CardFixedProduction[] Cards { get; set; }

//    public RCodeTable()
//    {
        
//    }

//    public RCodeTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Code = d[1];
//        Lasting = d[2];
//        Info = d[3];
//        YuQue = d[4].IntParse();
//        YuanBao = d[5].IntParse();
//        TiLi = d[6].IntParse();
//        Cards = d[7].TableStringToEnumerable().Select(TableStructure<CardFixedProduction>.InstanceOrDefault).ToArray();
//    }
//}
//public class StaticArrangementTable
//{
//    public int Id { get; set; }//已修正
//    public Chessman Pos1 { get; set; }
//    public Chessman Pos2 { get; set; }
//    public Chessman Pos3 { get; set; }
//    public Chessman Pos4 { get; set; }
//    public Chessman Pos5 { get; set; }
//    public Chessman Pos6 { get; set; }
//    public Chessman Pos7 { get; set; }
//    public Chessman Pos8 { get; set; }
//    public Chessman Pos9 { get; set; }
//    public Chessman Pos10 { get; set; }
//    public Chessman Pos11 { get; set; }
//    public Chessman Pos12 { get; set; }
//    public Chessman Pos13 { get; set; }
//    public Chessman Pos14 { get; set; }
//    public Chessman Pos15 { get; set; }
//    public Chessman Pos16 { get; set; }
//    public Chessman Pos17 { get; set; }
//    public Chessman Pos18 { get; set; }
//    public Chessman Pos19 { get; set; }
//    public Chessman Pos20 { get; set; }

//    public StaticArrangementTable()
//    {
        
//    }

//    public StaticArrangementTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Pos1 = TableStructure<Chessman>.InstanceOrDefault(d[1]);
//        Pos2 = TableStructure<Chessman>.InstanceOrDefault(d[2]);
//        Pos3 = TableStructure<Chessman>.InstanceOrDefault(d[3]);
//        Pos4 = TableStructure<Chessman>.InstanceOrDefault(d[4]);
//        Pos5 = TableStructure<Chessman>.InstanceOrDefault(d[5]);
//        Pos6 = TableStructure<Chessman>.InstanceOrDefault(d[6]);
//        Pos7 = TableStructure<Chessman>.InstanceOrDefault(d[7]);
//        Pos8 = TableStructure<Chessman>.InstanceOrDefault(d[8]);
//        Pos9 = TableStructure<Chessman>.InstanceOrDefault(d[9]);
//        Pos10 = TableStructure<Chessman>.InstanceOrDefault(d[10]);
//        Pos11 = TableStructure<Chessman>.InstanceOrDefault(d[11]);
//        Pos12 = TableStructure<Chessman>.InstanceOrDefault(d[12]);
//        Pos13 = TableStructure<Chessman>.InstanceOrDefault(d[13]);
//        Pos14 = TableStructure<Chessman>.InstanceOrDefault(d[14]);
//        Pos15 = TableStructure<Chessman>.InstanceOrDefault(d[15]);
//        Pos16 = TableStructure<Chessman>.InstanceOrDefault(d[16]);
//        Pos17 = TableStructure<Chessman>.InstanceOrDefault(d[17]);
//        Pos18 = TableStructure<Chessman>.InstanceOrDefault(d[18]);
//        Pos19 = TableStructure<Chessman>.InstanceOrDefault(d[19]);
//        Pos20 = TableStructure<Chessman>.InstanceOrDefault(d[20]);
//    }
//}
//public class TextTable//TextTable
//{
//    public int Id { get; set; }
//    public string Text { get; set; }

//    public TextTable()
//    {
        
//    }

//    public TextTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Text = d[1];
//    }
//}
//public class NumericalConfigTable//NumericalConfig
//{
//    public int Id { get; set; }
//    public int Value { get; set; }//已修正

//    public NumericalConfigTable()
//    {
        
//    }

//    public NumericalConfigTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Value = d[1].IntParse();
//    }
//}
//public class JiBanTable
//{
//    public int Id { get; set; }
//    public string JiBanTitle { get; set; }
//    public int IsOpen { get; set; }//已修正
//    public CardElement[] Cards { get; set; }
//    public string JiBanEffect { get; set; }
//    public CardElement[] BossCards { get; set; }

//    public JiBanTable()
//    {
        
//    }

//    public JiBanTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        JiBanTitle = d[1];
//        IsOpen = d[2].IntParse();
//        Cards = d[3].TableStringToEnumerable().Select(TableStructure<CardElement>.InstanceOrDefault).ToArray();
//        JiBanEffect = d[4];
//        BossCards = d[5].TableStringToEnumerable().Select(TableStructure<CardElement>.InstanceOrDefault).ToArray();
//    }
//}
//public class ForceTable //forceTable
//{
//    public int Id { get; set; }
//    public string Short { get; set; }
//    public int ImageId { get; set; }//已修正
//    public int FlagId { get; set; }//已修正
//    public int UnlockLevel { get; set; }//已修正
//    public string Info { get; set; }
//    public string Leader { get; set; }

//    public ForceTable()
//    {
        
//    }

//    public ForceTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Short = d[1];
//        ImageId = d[2].IntParse();
//        FlagId = d[3].IntParse();
//        UnlockLevel = d[4].IntParse();
//        Info = d[5];
//        Leader = d[6];
//    }
//}
//public class BaYeCityTable//BaYeCityTable
//{
//    public int EventPoint { get; set; }
//    public int[] BaYeCityEventTableIds { get; set; }
//    public string Name { get; set; }

//    public BaYeCityTable()
//    {
        
//    }

//    public BaYeCityTable(IList<string> d)
//    {
//        EventPoint = d[0].IntParse();
//        BaYeCityEventTableIds = d[1].TableStringToInts().ToArray();
//        Name = d[2];
//    }
//}
//public class BaYeCityEventTable//BaYeEventTable
//{
//    public int Id { get; set; }
//    public int Weight { get; set; }//已修正
//    public int[] BaYeExps { get; set; }
//    public int BaYeLevelMappingId { get; set; }//已修正
//    public int FlagId { get; set; }//已修正
//    public string FlagText { get; set; }

//    public BaYeCityEventTable()
//    {
        
//    }

//    public BaYeCityEventTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Weight = d[1].IntParse();
//        BaYeExps = d[2].TableStringToInts().ToArray();
//        BaYeLevelMappingId = d[3].IntParse();
//        FlagId = d[4].IntParse();
//        FlagText = d[5];
//    }
//}
//public class BaYeLevelMappingTable
//{
//    public int Id { get; set; }
//    public int[] Level0 { get; set; }
//    public int[] Level1 { get; set; }
//    public int[] Level2 { get; set; }
//    public int[] Level3 { get; set; }
//    public int[] Level4 { get; set; }
//    public int[] Level5 { get; set; }
//    public int[] Level6 { get; set; }
//    public int[] Level7 { get; set; }
//    public int[] Level8 { get; set; }
//    public int[] Level9 { get; set; }

//    public BaYeLevelMappingTable()
//    {
        
//    }

//    public BaYeLevelMappingTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Level0 = d[1].TableStringToInts().ToArray();
//        Level1 = d[2].TableStringToInts().ToArray();
//        Level2 = d[3].TableStringToInts().ToArray();
//        Level3 = d[4].TableStringToInts().ToArray();
//        Level4 = d[5].TableStringToInts().ToArray();
//        Level5 = d[6].TableStringToInts().ToArray();
//        Level6 = d[7].TableStringToInts().ToArray();
//        Level7 = d[8].TableStringToInts().ToArray();
//        Level8 = d[9].TableStringToInts().ToArray();
//        Level9 = d[10].TableStringToInts().ToArray();
//    }
//}
//public class BaYeTaskTable//BaYeRenWuTable
//{ 
//    public int Id { get; set; }
//    public int Exp { get; set; }//已修正
//    public int WarChestTableId { get; set; }//已修正

//    public BaYeTaskTable()
//    {
        
//    }

//    public BaYeTaskTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Exp = d[1].IntParse();
//        WarChestTableId = d[2].IntParse();
//    }
//}
//public class BaYeStoryPoolTable //BaYeStoryPoolTable
//{
//    public int EventId { get; set; }//已修正
//    public int[] BaYeStoryTableIds { get; set; }

//    public BaYeStoryPoolTable()
//    {
        
//    }
//    public BaYeStoryPoolTable(IList<string> d)
//    {
//        EventId = d[0].IntParse();
//        BaYeStoryTableIds = d[1].TableStringToInts().ToArray();
//    }
//}
//public class BaYeStoryEventTable//BaYeStoryEventTable
//{
//    public int Id { get; set; }//已修正
//    public int Weight { get; set; }//已修正
//    public int StoryType { get; set; }//已修正
//    public RangeElement Gold { get; set; }
//    public RangeElement Exp { get; set; }
//    public RangeElement YvQue { get; set; }
//    public RangeElement YuanBao { get; set; }
//    public RangeElement ForceLing { get; set; }
//    public string Time { get; set; }
//    public int WarId { get; set; }//已修正

//    public BaYeStoryEventTable()
//    {
        
//    }

//    public BaYeStoryEventTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Weight = d[1].IntParse();
//        StoryType = d[2].IntParse();
//        Gold = TableStructure<RangeElement>.InstanceOrDefault(d[3]);
//        Exp = TableStructure<RangeElement>.InstanceOrDefault(d[4]);
//        YvQue = TableStructure<RangeElement>.InstanceOrDefault(d[5]);
//        YuanBao = TableStructure<RangeElement>.InstanceOrDefault(d[6]);
//        ForceLing = TableStructure<RangeElement>.InstanceOrDefault(d[7]);
//        Time = d[8];
//        WarId = d[9].IntParse();
//    }
//}
//public class BaYeTvTable //BaYeTvTable
//{
//    public int Id { get; set; }
//    public int Weight { get; set; }//已修正
//    public string Text { get; set; }
//    public string Time { get; set; }
//    public int Format { get; set; }//已修正

//    public BaYeTvTable()
//    {
        
//    }

//    public BaYeTvTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Weight = d[1].IntParse();
//        Text = d[2];
//        Time = d[3];
//        Format = d[4].IntParse();
//    }
//}
//public class BaYeNameTable
//{
//    public int Id { get; set; }
//    public int Weight { get; set; }//已修正
//    public string Name { get; set; }

//    public BaYeNameTable()
//    {
        
//    }

//    public BaYeNameTable(IList<string> d)
//    {
//        Id = d[0].IntParse();
//        Weight = d[1].IntParse();
//        Name = d[2];
//    }
//}
// #endregion
