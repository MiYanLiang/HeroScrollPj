using CorrelateLib;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using File = System.IO.File;

namespace Assets.Editor.Config
{
    public class GameConfigWindow : EditorWindow
    {
        public static ConfigAsset configAsset;
        public static DataTable dataTable;

        public static string Message
        {
            get => message;
            set
            {
                isMessageDisplay = false;
                message = value;
            }
        }

        public static bool isMessageDisplay;
        [MenuItem("GameConfig/Window")]
        public static void Init()
        {
            var window = GetWindow<GameConfigWindow>();
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("请选入要替换的ConfigAsset");
            var obj = EditorGUILayout.ObjectField("ConfigAsset", configAsset, typeof(ConfigAsset), true);
            configAsset = obj as ConfigAsset;
            if (GUILayout.Button("储存")) Save();
            GUILayout.Label("---数据表---");
            var _dataTableObj = EditorGUILayout.ObjectField("DataTable", dataTable, typeof(DataTable), true);
            dataTable = _dataTableObj as DataTable;
            if (GUILayout.Button("绑定数据表文件")) InitializeDataTable(dataTable);
            if (!string.IsNullOrWhiteSpace(Message) && !isMessageDisplay) GUILayout.Label(Message);
        }

        void Save()
        {
            message = string.Empty;
            var serverConfig = new ServerFields(configAsset);
            var encrypt = EncryptDecipherTool.DESEncrypt(Json.Serialize(serverConfig));
            File.WriteAllText("assets/resources/game/pz.bytes", encrypt);
            XDebug.Log<GameConfigWindow>("配置文件已加密存档！");
        }

        private static int totalProceeds;
        private static string message;
        private const string JsonPath = "jsons/";
        static void InitializeDataTable(DataTable dt)
        {
            message = string.Empty;
            totalProceeds = 0;
            var textAssetType = typeof(TextAsset);
            foreach (var field in dt.GetType().GetFields().Where(f=>f.FieldType == textAssetType))
            {
                var obj = field.GetValue(dt) as TextAsset;
                object value = Init(obj, field.Name);
                field.SetValue(dt, value);
            }
            //dt.playerInitialTable=Init(dt.playerInitialTable,nameof(dt.playerInitialTable  ));
            //dt.assetTable        =Init(dt.assetTable        ,nameof(dt.assetTable          ));
            //dt.heroTable         =Init(dt.heroTable         ,nameof(dt.heroTable           ));
            //dt.playerLevelTable  =Init(dt.playerLevelTable  ,nameof(dt.playerLevelTable    ));
            //dt.soldierTable      =Init(dt.soldierTable      ,nameof(dt.soldierTable        ));
            //dt.towerTable        =Init(dt.towerTable        ,nameof(dt.towerTable          ));
            //dt.classTable        =Init(dt.classTable        ,nameof(dt.classTable          ));
            //dt.upGradeTable      =Init(dt.upGradeTable      ,nameof(dt.upGradeTable        ));
            //dt.trapTable         =Init(dt.trapTable         ,nameof(dt.trapTable           ));
            //dt.spellTable        =Init(dt.spellTable        ,nameof(dt.spellTable          ));
            //dt.warChestTable     =Init(dt.warChestTable     ,nameof(dt.warChestTable       ));
            //dt.warTable          =Init(dt.warTable          ,nameof(dt.warTable            ));
            //dt.cityLevelTable    =Init(dt.cityLevelTable    ,nameof(dt.cityLevelTable      ));
            //dt.pointTable        =Init(dt.pointTable        ,nameof(dt.pointTable          ));
            //dt.battleEventTable  =Init(dt.battleEventTable  ,nameof(dt.battleEventTable    ));
            //dt.enemyTable        =Init(dt.enemyTable        ,nameof(dt.enemyTable          ));
            //dt.enemyUnitTable    =Init(dt.enemyUnitTable    ,nameof(dt.enemyUnitTable      ));
            //dt.storyTable        =Init(dt.storyTable        ,nameof(dt.storyTable          ));
            //dt.storyRTable       =Init(dt.storyRTable       ,nameof(dt.storyRTable         ));
            //dt.testTable         =Init(dt.testTable         ,nameof(dt.testTable           ));
            //dt.testRTable        =Init(dt.testRTable        ,nameof(dt.testRTable          ));
            //dt.encounterTable    =Init(dt.encounterTable    ,nameof(dt.encounterTable      ));
            //dt.shoppingTable     =Init(dt.shoppingTable     ,nameof(dt.shoppingTable       ));
            //dt.choseWarTable     =Init(dt.choseWarTable     ,nameof(dt.choseWarTable       ));
            //dt.guideTable        =Init(dt.guideTable        ,nameof(dt.guideTable          ));
            //dt.knowledgeTable    =Init(dt.knowledgeTable    ,nameof(dt.knowledgeTable      ));
            //dt.rCodeTable        =Init(dt.rCodeTable        ,nameof(dt.rCodeTable          ));
            //dt.tiLiStoreTable    =Init(dt.tiLiStoreTable    ,nameof(dt.tiLiStoreTable      ));
            //dt.enemyBOSSTable    =Init(dt.enemyBOSSTable    ,nameof(dt.enemyBOSSTable      ));
            //dt.stringTextTable   =Init(dt.stringTextTable   ,nameof(dt.stringTextTable     ));
            //dt.numParametersTable=Init(dt.numParametersTable,nameof(dt.numParametersTable  ));
            //dt.jiBanTable        =Init(dt.jiBanTable        ,nameof(dt.jiBanTable          ));
            //dt.shiLiTable        =Init(dt.shiLiTable        ,nameof(dt.shiLiTable          ));
            //dt.baYeDiTuTable     =Init(dt.baYeDiTuTable     ,nameof(dt.baYeDiTuTable       ));
            //dt.baYeShiJianTable  =Init(dt.baYeShiJianTable  ,nameof(dt.baYeShiJianTable    ));
            //dt.baYeBattleTable   =Init(dt.baYeBattleTable   ,nameof(dt.baYeBattleTable     ));
            //dt.baYeRenWuTable    =Init(dt.baYeRenWuTable    ,nameof(dt.baYeRenWuTable      ));
            //dt.storyPoolTable    =Init(dt.storyPoolTable    ,nameof(dt.storyPoolTable      ));
            //dt.storyIdTable      =Init(dt.storyIdTable      ,nameof(dt.storyIdTable        ));
            //dt.baYeTVTable       =Init(dt.baYeTVTable       ,nameof(dt.baYeTVTable         ));
            //dt.baYeNameTable     =Init(dt.baYeNameTable     ,nameof(dt.baYeNameTable       ));
            Message = $"转码完成，文件数[{totalProceeds}]";
        }

        private static TextAsset Init(TextAsset asset,string tableName)
        {
            if (asset != null) return asset;
            var jsonPath = JsonPath + tableName;
            var obj = Resources.Load(jsonPath);
            var textAsset = obj as TextAsset;
            if (textAsset == null) throw XDebug.Throw<DataTable>($"找不到文件[{jsonPath}]");
                var data = Json.DeserializeList<List<string>>(textAsset.text);
            if (data == null)
            {
                Message = "错误！详细看调试窗口!";
                throw XDebug.Throw<DataTable>($"无法解析[{tableName}]");
            }
            totalProceeds++;
            return textAsset;
        }
    }
}
