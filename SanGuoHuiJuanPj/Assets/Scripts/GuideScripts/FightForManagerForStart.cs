using System;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.System.WarModule;
using CorrelateLib;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class FightForManagerForStart : MonoBehaviour
{
    public static FightForManagerForStart instance;

    [SerializeField]
    Transform enemyChechBoard;          //敌方棋盘
    [SerializeField]
    Transform playerChechBoard;         //我方棋盘
    [SerializeField]
    public Transform enemyCardsBox;     //敌方卡牌父级
    [SerializeField]
    public Transform playerCardsBox;    //我方卡牌父级
    [SerializeField]
    GameObject fightCardPre;            //敌方战斗卡牌预制件
    [SerializeField]
    GameObject fightCardPyPre;          //我方战斗卡牌预制件

    [SerializeField]
    WarGameCardUi homeCardObj;             //老巢卡牌预制件

    public Transform herosCardListTran; //我方卡牌备战位

    public List<FightCardData> playerCardsDatas; //我方备战位卡牌信息集合

    public List<GameObject> enemyCardsPos;  //敌方卡牌位置列表
    public List<GameObject> playerCardsPos; //我方卡牌位置列表

    public FightCardData[] enemyFightCardsDatas;    //敌方战斗卡牌信息集合
    public FightCardData[] playerFightCardsDatas;   //我方战斗卡牌信息集合

    [SerializeField]
    GridLayoutGroup gridLayoutGroup;    //棋盘Group
    [HideInInspector]
    public float floDisY;  //加成比
    [HideInInspector]
    public float oneDisY;  //半格高

    [HideInInspector]
    public int battleIdIndex;   //指引战役编号索引

    [SerializeField]
    GameObject guideStoryObj;   //剧情obj

    public Transform cardListTran;          //备战位总位
    public ScrollRect cardListScrollRect;   //备战位ScrollRect

    public AudioClip[] audioClipsFightEffect;
    public float[] audioVolumeFightEffect;

    public AudioClip[] audioClipsFightBack; //战斗背景音乐
    public float[] audioVolumeFightBack;

    [SerializeField]
    Image fightBackImage;   //战斗背景img

    [HideInInspector]
    public bool _isDragItem;    //记录是否有卡牌在拖动

    private int storyIndex;    //当前剧情故事编号
    private GuideTable[] guides;          //剧情故事数量

    [SerializeField]
    GameObject[] guideObjs;     //指引obj
    bool isShowed0 = false;
    bool isShowed1 = false;
    [SerializeField]
    Button startFightBtn;   //开始战斗按钮

    //卡牌附近单位遍历次序
    public int[][] CardNearbyAdditionForeach = new int[20][] {
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

    private GameResources GameResources;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    public void Init()
    {
        playerCardsDatas = new List<FightCardData>();
        enemyFightCardsDatas = new FightCardData[20];
        playerFightCardsDatas = new FightCardData[20];
        Input.multiTouchEnabled = false;    //限制多指拖拽
        _isDragItem = false;
        GameResources = new GameResources();
        GameResources.Init();
        oneDisY = Screen.height / (1920 / gridLayoutGroup.cellSize.y) / 9;
        float xFlo = (1920f / 1080f) / ((float)Screen.height / Screen.width);
        floDisY = 2 * oneDisY * xFlo;
        storyIndex = 0;
        guides = DataTable.Guide.Values.ToArray();

        barragesList = new List<string[]>();
        barragesList.Add(pTBarrages_1);
        barragesList.Add(zhanQianBarrages_1);
        barragesList.Add(zhanZhongBarrages_1);
        barragesList.Add(pTBarrages_2);
        barragesList.Add(zhanQianBarrages_2);
        barragesList.Add(zhanZhongBarrages_2);
        barragesList.Add(pTBarrages_3);
        barragesList.Add(feiHuaBarrages);
    }

    /////////////////弹幕指引////////////////////////////////////

    List<string[]> barragesList;

    //故事一片头弹幕////////////////////////////////////////////////////
    string[] pTBarrages_1 = new string[10] {
        "是三国呀",
        "这游戏好玩么？",
        "新手剧情？？？",
        "惊天动地之伟业……",
        "有妹子一起玩？？",
        "？？？",
        "赶快开战！！！",
        "……",
        "要氪金么？",
        "80后前来匡扶汉室",
    };
    //战前弹幕
    string[] zhanQianBarrages_1 = new string[14] {
        "这怎么玩？",
        "要把赵云拖上去？",
        "拖动上阵",
        "先随便放个位置",
        "对面人好多",
        "滚石滚木干嘛用的",
        "滚石砸一路，滚木砸一排",
        "上来就打张角2333",
        "奏乐台啥效果？？",
        "公孙瓒随便放？",
        "怎么开始战斗……",
        "要点中间那个战",
        "关羽上去砍啊",
        "张角，好怕怕",
    };
    //战中弹幕
    string[] zhanZhongBarrages_1 = new string[19] {
        "会心一击2333",
        "滚石是怎么触发的？",
        "张角：打雷下雨收衣服了……",
        "预警，前方高能",
        "刘备可以反击",
        "关羽张飞优先打边路",
        "关羽直接砍到了敌方老巢",
        "张飞厉害啊",
        "关羽貌似可以无限连斩……",
        "啊啊啊，关羽被闪电打晕了",
        "抛石塔貌似只能打固定范围",
        "箭塔的群伤……",
        "差点没打过",
        "险胜",
        "米兔",
        "张角怎么这么厉害",
        "刘备是啥技能？",
        "要打掉老巢才能赢？",
        "关羽太爽了",
    };
    //故事二片头弹幕////////////////////////////////////////////////////
    string[] pTBarrages_2 = new string[7] {
        "刚差点被张角的雷劈死",
        "这么快就要三英战吕布了？？",
        "好快……",
        "现在还是新手剧情？",
        "有妹子一起玩？？",
        "还在新手中……",
        "快快快"
    };
    //战前弹幕
    string[] zhanQianBarrages_2 = new string[7] {
        "三英战吕布23333",
        "华雄：杀鸡焉用牛刀……",
        "韩馥：我有上将潘凤，可斩华雄！",
        "毒士李儒……",
        "没董卓居然",
        "我上袁绍和曹操了",
        "我全上了",
    };
    //战中弹幕
    string[] zhanZhongBarrages_2 = new string[23] {
        "潘无双上来就被华雄斩了",
        "关东潘凤，关西吕布",
        "一吕二赵三典韦",
        "4关5张6马超",
        "我老曹左典韦，右许褚",
        "吾儿奉先何在？！",
        "我左貂蝉右小乔",
        "一吕二赵三潘凤",
        "无双上将潘凤前来报道",
        "拒马可以挡两路……",//10
        "潘无双前来送人头",
        "刘备：来啊，互相伤害啊",
        "盾是哪来的？轩辕台加的？",
        "我关羽是被毒死的",
        "骑兵被拒马弄死了",
        "吕布被围殴了",
        "红将是最强武将？",
        "有点意思",
        "温酒斩华雄",
        "酒且斟下，某去便来。",//20
        "杀呀杀呀",
        "拒马强无敌",
        "祈愿拒马",
    };
    //故事三片头弹幕/////////////////////////////////////////////////////
    string[] pTBarrages_3 = new string[13] {
        "结束了？",
        "在哪抽武将？",
        "祈愿关羽",
        "祈愿吕布",
        "祈愿郭嘉",
        "典韦！典韦！",
        "祈愿关羽+1",
        "抽了个貂蝉，厉害么？",
        "貌似是个辅助",
        "祈愿潘凤233",
        "祈愿张角",
        "祈愿关羽+2",
        "祈愿诸葛亮"
    };
    //随机废话弹幕//////////////////////////////////////////////////////
    string[] feiHuaBarrages = new string[55] {
        "80后前来观战",
        "90后前来挨打",
        "00后前来打人",
        "10后前来匡扶汉室",
        "80后前来围观",
        "有妹纸一起玩？",
        "怎么加群？",
        "群里有兑换码？",
        "周瑜厉害么？",
        "关羽是最强武将？",//10
        "吕布看着好弱",
        "我要关羽",
        "我要典韦",
        "单位看着好多种类",
        "不知道好不好玩",
        "新人前来报道",
        "有没有人一起玩",
        "加群加群加群",
        "到底哪个武将厉害？",
        "匡扶汉室233",//20
        "在哪领兑换码？",
        "铁骑连环，吓死我了",
        "祈愿郭嘉",
        "听说有很多塔和陷阱",
        "50多种职业，是真的么？",
        "90后前来一统天下",
        "80后前来匡扶汉室",
        "不知道啊",
        "雪花飘飘",
        "尔等插标卖首之辈",//30,
        "70后前来打卡",
        "哈哈哈哈哈",
        "求赵云求赵云",
        "求兑换码",
        "有群么，拉我",
        "加我一起玩",
        "不错啊",
        "求吕布",
        "看样子是国产",
        "抽到关羽了",//40
        "看样子是我的菜",
        "玩法有点深啊",
        "厉害厉害",
        "啥时候出的",
        "好玩么",
        "求小哥哥一起玩",
        "老版三国的感觉",
        "要点中间那个战",
        "战棋啊",
        "看着挺难的样子",//50
        "新游戏？",
        "先给5星再说",
        "首抽许褚",
        "首抽马超",
        "首抽出了吕布",
    };

    public BarrageUiController barragesController;

    //播放固定弹幕
    IEnumerator ShowBarrageForStory(int storyIndex)
    {
        for (int i = 0; i < barragesList[storyIndex].Length; i++)
        {
            int randTime = Random.Range(2, 5);  //间隔时间
            yield return new WaitForSeconds(randTime);
            barragesController.PlayBarrage(barragesList[storyIndex][i]);
        }
    }

    bool isShowFHDM = false;
    //播放废话弹幕
    IEnumerator ShowFeiHuaBarrage()
    {
        while (true)
        {
            int randTime = Random.Range(3, 7);  //间隔时间
            yield return new WaitForSeconds(randTime);
            barragesController.PlayBarrage(barragesList[7][Random.Range(0, barragesList[7].Length)]);
        }
    }

    bool isShowedZZDM = false;
    //播放战中弹幕
    public void PlayZhanZhongBarrage()
    {
        if (!isShowedZZDM)
        {
            isShowedZZDM = true;
            StartCoroutine(ShowBarrageForStory(storyIndex == 1 ? 3 : 6));
        }
    }
    ///////////////////////////////////////////

    //展示指引
    public void ChangeGuideForFight(int index)
    {
        switch (index)
        {
            case 0:
                if (!isShowed0)
                {
                    guideObjs[0].SetActive(false);
                    guideObjs[1].SetActive(true);
                    isShowed0 = true;
                    startFightBtn.enabled = true;
                }
                break;
            case 1:
                if (!isShowed1)
                {
                    guideObjs[1].SetActive(false);
                    isShowed1 = true;
                }
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 创建玩家家园卡牌
    /// </summary>
    private void CreatePlayerHomeCard()
    {
        FightCardData playerHomeData = new FightCardData();
        playerHomeData.cardObj= Instantiate(homeCardObj, playerCardsBox);
        playerHomeData.cardObj.transform.position = playerCardsPos[17].transform.position;
        playerHomeData.hpr = 0;
        playerHomeData.cardType = 522;
        playerHomeData.posIndex = 17;
        playerHomeData.isPlayerCard = true;
        playerHomeData.activeUnit = false;
        playerHomeData.CardState = new CardState();
        playerFightCardsDatas[17] = playerHomeData;
        playerFightCardsDatas[17].ResetHp(guides[storyIndex].BaseHp);  //我方血量

        FightCardData enemyHomeData = new FightCardData();
        enemyHomeData.cardObj = Instantiate(homeCardObj, enemyCardsBox);
        enemyHomeData.cardObj.transform.position = enemyCardsPos[17].transform.position;
        enemyHomeData.hpr = 0;
        enemyHomeData.cardType = 522;
        enemyHomeData.posIndex = 17;
        enemyHomeData.isPlayerCard = false;
        enemyHomeData.activeUnit = false;
        enemyHomeData.CardState = new CardState();
        enemyFightCardsDatas[17] = enemyHomeData;
        enemyFightCardsDatas[17].ResetHp(guides[storyIndex].EnemyBaseHp);    //敌方血量
    }

    //隐藏故事界面
    public void HideStoryWin()
    {
        Transform storyObj = guideStoryObj.transform.GetChild(3);
        storyObj.GetChild(0).gameObject.SetActive(false);
        storyObj.GetChild(1).gameObject.SetActive(false);
        storyObj.GetChild(2).gameObject.SetActive(false);
        storyObj.GetComponent<Button>().enabled = false;
        var bottonImg = storyObj.GetChild(0).GetChild(3).GetComponent<Image>();

        if (storyIndex < guides.Length)
        {
            AudioController1.instance.isNeedPlayLongMusic = true;
            AudioController1.instance.ChangeAudioClip(audioClipsFightBack[storyIndex], audioVolumeFightBack[storyIndex]);
            AudioController1.instance.PlayLongBackMusInit();

            fightBackImage.sprite = Resources.Load("Image/battleBG/" + guides[storyIndex].MapBg, typeof(Sprite)) as Sprite;

            bottonImg.sprite = Resources.Load("Image/startFightImg/Image/" + (storyIndex + 1), typeof(Sprite)) as Sprite;
            storyObj.GetComponent<Image>().DOFade(0, 1f).OnComplete(delegate ()
            {
                //播放战前弹幕
                StartCoroutine(ShowBarrageForStory(storyIndex == 1 ? 2 : 5));
                storyObj.gameObject.SetActive(false);
                isShowedZZDM = false;
            });
        }
        else
        {
            guideStoryObj.transform.GetChild(1).gameObject.SetActive(false);
            guideStoryObj.transform.GetChild(2).gameObject.SetActive(false);
            StartSceneUIManager.instance.EndStoryToChooseForce();
            //终止弹幕触发
            StopAllCoroutines();
            storyObj.GetComponent<Image>().DOFade(0, 2f).OnComplete(delegate ()
            {
                guideStoryObj.SetActive(false);
                return;
            });
        }
    }

    /// <summary>
    /// 初始化卡牌到战斗位上
    /// </summary>
    public void InitEnemyCardForFight()
    {
        AudioController1.instance.isNeedPlayLongMusic = true;
        AudioController1.instance.ChangeAudioClip(audioClipsFightBack[0], audioVolumeFightBack[0]);
        AudioController1.instance.PlayLongBackMusInit();

        guideStoryObj.SetActive(true);

        Transform storyObj = guideStoryObj.transform.GetChild(3);
        storyObj.GetChild(0).gameObject.SetActive(true);

        storyObj.GetChild(1).GetChild(0).GetComponent<Image>().sprite = Resources.Load("Image/startFightImg/Title/" + (storyIndex + 1), typeof(Sprite)) as Sprite;
        storyObj.GetChild(1).gameObject.SetActive(true);
        Text storyText = storyObj.GetChild(0).GetChild(2).GetComponent<Text>();
        storyText.text = "";
        storyObj.gameObject.SetActive(true);
        storyObj.GetComponent<Image>().DOFade(1, 1.5f).OnComplete(delegate ()
        {
            //播放片头弹幕
            StartCoroutine(ShowBarrageForStory(storyIndex));
            if (!isShowFHDM)
            {
                StartCoroutine(ShowFeiHuaBarrage());
                isShowFHDM = true;
            }

            if (storyIndex < guides.Length)
            {
                FightControlForStart.instance.InitStartFight();
                UpdateCardDataForStory();
            }
            storyText.color = new Color(storyText.color.r, storyText.color.g, storyText.color.b, 0);
            storyText.DOFade(1, 5f);
            storyText.DOText(guides[storyIndex].Intro, 8f).OnComplete(delegate ()
            {
                guideStoryObj.transform.GetChild(1).gameObject.SetActive(true);
                guideStoryObj.transform.GetChild(2).gameObject.SetActive(true);
                storyObj.GetComponent<Button>().enabled = true;
                storyObj.GetChild(2).gameObject.SetActive(true);
            }).SetEase(Ease.Linear);

            storyIndex++;
        });
    }

    //更新故事战斗数据
    private void UpdateCardDataForStory()
    {
        //羁绊数据重置
        CardManager.ResetJiBan(FightControlForStart.instance.playerJiBanAllTypes);
        CardManager.ResetJiBan(FightControlForStart.instance.enemyJiBanAllTypes);
        if (playerFightCardsDatas[17] == null)
        {
            CreatePlayerHomeCard();
        }
        else
        {
            playerFightCardsDatas[17].cardObj.transform.GetChild(4).gameObject.SetActive(false);
            enemyFightCardsDatas[17].cardObj.transform.GetChild(4).gameObject.SetActive(false);

            playerFightCardsDatas[17].ResetHp(guides[storyIndex].BaseHp); //我方血量
            enemyFightCardsDatas[17].ResetHp(guides[storyIndex].EnemyBaseHp); //敌方血量

            playerFightCardsDatas[17].cardObj.transform.GetChild(2).GetComponent<Image>().fillAmount = 0;
            enemyFightCardsDatas[17].cardObj.transform.GetChild(2).GetComponent<Image>().fillAmount = 0;
        }

        //清空备战位
        for (int i = 0; i < herosCardListTran.childCount; i++)
        {
            Destroy(herosCardListTran.GetChild(i).gameObject);
        }

        playerCardsDatas.Clear();
        //清空棋盘单位
        for (int i = 0; i < 20; i++)
        {
            if (i != 17)
            {
                if (playerFightCardsDatas[i] != null)
                {
                    Destroy(playerFightCardsDatas[i].cardObj);
                    playerFightCardsDatas[i] = null;
                }

                if (enemyFightCardsDatas[i] != null)
                {
                    Destroy(enemyFightCardsDatas[i].cardObj);
                    enemyFightCardsDatas[i] = null;
                }
            }
        }

        FightControlForStart.instance.ClearEmTieQiCardList();
        var guide = guides[storyIndex];
        //创建玩家备战单位
        foreach (var card in guide.Poses(GuideProps.Card))
        {
            if(card == null)continue;
            FightCardData data = CreateFightUnit(card, herosCardListTran, true);
            data.cardObj.GetComponent<CardDragForStart>().posIndex = -1;
            data.cardObj.GetComponent<CardDragForStart>().isFightCard = false;
            data.posIndex = -1;
            data.isPlayerCard = true;
            playerCardsDatas.Add(data);
        }

        //创建玩家棋盘单位
        var playerChessmen = guide.Poses(GuideProps.Player);
        for (var i = 0; i < playerChessmen.Length; i++)
        {
            var card = playerChessmen[i];
            if(card == null)continue;
            FightCardData data = CreateFightUnit(card, playerCardsBox, true);
            data.cardObj.GetComponent<CardDragForStart>().posIndex = i;
            data.cardObj.GetComponent<CardDragForStart>().isFightCard = true;
            data.posIndex = i;
            data.isPlayerCard = true;
            data.cardObj.transform.position = playerCardsPos[i].transform.position;
            playerFightCardsDatas[i] = data;
            CardGoIntoBattleProcess(playerFightCardsDatas[i], i, playerFightCardsDatas, true);
        }

        //创建敌人棋盘单位
        var enemyChessmen = guide.Poses(GuideProps.Enemy);
        for (int i = 0; i < 20; i++)
        {
            var card = enemyChessmen[i];
            if(card == null)continue;
            FightCardData data = CreateFightUnit(card, enemyCardsBox, false);
            data.posIndex = i;
            data.isPlayerCard = false;
            data.cardObj.transform.position = enemyCardsPos[i].transform.position;
            enemyFightCardsDatas[i] = data;
            CardGoIntoBattleProcess(enemyFightCardsDatas[i], i, enemyFightCardsDatas, true);
        }
    }

    //创建敌方卡牌战斗单位数据
    private FightCardData CreateFightUnit(Chessman chessman, Transform cardsBoxTran, bool isPlayerCard)
    {
        FightCardData data = new FightCardData();
        data.cardObj = PrefabManager.NewWarGameCardUi(cardsBoxTran); //Instantiate(isPlayerCard ? fightCardPyPre : fightCardPre, cardsBoxTran);
        data.cardType = chessman.CardType;
        data.cardId = chessman.CardId;
        data.level = chessman.Star;
        data.CardState = new CardState();
        var cardType = (GameCardType) chessman.CardType;
        var card = new GameCard().Instance(cardType, chessman.CardId, chessman.Star);
        var info = card.GetInfo();
        //兵种
        data.cardObj.transform.GetChild(1).GetComponent<Image>().sprite = cardType == GameCardType.Hero
            ? GameResources.HeroImg[card.id]
            : GameResources.FuZhuImg[info.ImageId];
        //血量
        data.cardObj.transform.GetChild(2).GetComponent<Image>().fillAmount = 0;
        //名字
        ShowNameTextRules(data.cardObj.transform.GetChild(3).GetComponent<Text>(), info.Name);
        //名字颜色
        data.cardObj.transform.GetChild(3).GetComponent<Text>().color = NameColorChoose(info.Rare);
        //星级
        data.cardObj.transform.GetChild(4).GetComponent<Image>().sprite = GameResources.GradeImg[card.level];
        //兵种
        data.cardObj.transform.GetChild(5).GetComponentInChildren<Text>().text = info.Short;
        data.cardObj.transform.GetChild(5).GetComponentInChildren<Image>().sprite = cardType == GameCardType.Hero
            ? GameResources.ClassImg[0]
            : GameResources.ClassImg[1];
        //边框
        FrameChoose(info.Rare, data.cardObj.transform.GetChild(6).GetComponent<Image>());
        data.damage = info.GetDamage(card.level);
        data.hpr = info.GameSetRecovery;
        data.ResetHp(info.GetHp(card.level));
        data.activeUnit = cardType == GameCardType.Hero || ((cardType == GameCardType.Tower) &&
                                                            (info.Id == 0 || info.Id == 1 || info.Id == 2 ||
                                                             info.Id == 3 || info.Id == 6));
        data.combatType = info.CombatType;
        data.cardDamageType = info.DamageType;
        if (cardType != GameCardType.Hero) data.cardObj.transform.GetChild(5).GetComponent<Image>().sprite = GameResources.ClassImg[1];
        return data;
    }

    /// <summary>
    /// 根据obj找到其父数据
    /// </summary>
    public int FindDataFromCardsDatas(GameObject obj)
    {
        int i = 0;
        for (; i < playerCardsDatas.Count; i++)
        {
            if (playerCardsDatas[i].cardObj == obj)
            {
                break;
            }
        }
        if (i >= playerCardsDatas.Count)
        {
            return -1;
        }
        else
        {
            return i;
        }
    }

    /// <summary>
    /// 名字显示规则
    /// </summary>
    private void ShowNameTextRules(Text nameText, string str)
    {
        nameText.text = str;
        switch (str.Length)
        {
            case 1:
                nameText.fontSize = 50;
                nameText.lineSpacing = 1.1f;
                break;
            case 2:
                nameText.fontSize = 50;
                nameText.lineSpacing = 1.1f;
                break;
            case 3:
                nameText.fontSize = 50;
                nameText.lineSpacing = 0.9f;
                break;
            case 4:
                nameText.fontSize = 45;
                nameText.lineSpacing = 0.8f;
                break;
            default:
                nameText.fontSize = 45;
                nameText.lineSpacing = 0.8f;
                break;
        }
    }

    //匹配稀有度边框
    private void FrameChoose(int rarity, Image img)
    {
        img.enabled = false;
        return;
        img.enabled = true;
        switch (rarity)
        {
            case 4:
                img.sprite = Resources.Load("Image/frameImage/tong", typeof(Sprite)) as Sprite;
                break;
            case 5:
                img.sprite = Resources.Load("Image/frameImage/yin", typeof(Sprite)) as Sprite;
                break;
            case 6:
                img.sprite = Resources.Load("Image/frameImage/jin", typeof(Sprite)) as Sprite;
                break;
            default:
                img.enabled = false;
                break;
        }
    }

    //主动塔行动
    public void ActiveTowerFight(FightCardData cardData, FightCardData[] cardsDatas)
    {
        switch (cardData.cardId)
        {
            case 0://营寨
                YingZhaiFun(cardData, cardsDatas);
                break;
            case 1://投石台
                TouShiTaiAttackFun(cardData);
                break;
            case 2://奏乐台
                ZouYueTaiAddtionFun(cardData, cardsDatas);
                break;
            case 3://箭楼
                FightControlForStart.instance.JianLouYuanSheSkill(cardData, GetTowerAddValue(cardData.cardId, cardData.level));
                break;
            case 6://轩辕台
                XuanYuanTaiAddtionFun(cardData, cardsDatas);
                break;
            default:
                break;
        }
    }

    //营寨行动
    private void YingZhaiFun(FightCardData cardData, FightCardData[] cardsDatas)
    {
        if (cardData.Hp > 1)
        {
            FightControlForStart.instance.indexAttackType = 0;
            FightControlForStart.instance.PlayAudioForSecondClip(42, 0);

            int indexCutMaxHp = 0;  //最多扣除血量
            FightCardData needAddHpCard = new FightCardData();

            for (int i = 0; i < CardNearbyAdditionForeach[cardData.posIndex].Length; i++)
            {
                FightCardData addedFightCard = cardsDatas[CardNearbyAdditionForeach[cardData.posIndex][i]];
                if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                {
                    if (addedFightCard.Hp - addedFightCard.Hp > indexCutMaxHp)
                    {
                        indexCutMaxHp = addedFightCard.Hp.Max - addedFightCard.Hp;
                        needAddHpCard = addedFightCard;
                    }
                }
            }

            if (indexCutMaxHp > 0)
            {
                int canAddHpNum = cardData.Hp - 1;
                if (canAddHpNum > indexCutMaxHp)
                {
                    canAddHpNum = indexCutMaxHp;
                }
                //自身减血
                cardData.Hp.Add(-canAddHpNum);
                FightControlForStart.instance.AttackedAnimShow(cardData, canAddHpNum, false);
                //单位加血
                FightControlForStart.instance.AttackToEffectShow(needAddHpCard, false, Effect.Heal42A);
                needAddHpCard.Hp.Add(canAddHpNum);
                FightControlForStart.instance.ShowSpellTextObj(needAddHpCard.cardObj, DataTable.GetStringText(15), true, false);
                FightControlForStart.instance.AttackedAnimShow(needAddHpCard, canAddHpNum, true);
            }
        }
    }

    /// <summary>
    /// 投石台攻击,对位与周围单位
    /// </summary>
    /// <param name="cardData"></param>
    private void TouShiTaiAttackFun(FightCardData cardData)
    {
        FightCardData[] attackedUnits = cardData.isPlayerCard ? enemyFightCardsDatas : playerFightCardsDatas;
        int damage = (int)(DataTable.GetGameValue(122) / 100f * GetTowerAddValue(cardData.cardId, cardData.level)); //造成的伤害
        FightControlForStart.instance.indexAttackType = 0;

        FightControlForStart.instance.PlayAudioForSecondClip(24, 0);

        List<GameObject> posListToThunder = cardData.isPlayerCard ? enemyCardsPos : playerCardsPos;
        EffectsPoolingControl.instance.GetEffectToFight1("101A", 1f, posListToThunder[cardData.posIndex].transform);

        if (attackedUnits[cardData.posIndex] != null && attackedUnits[cardData.posIndex].Hp > 0)
        {
            int finalDamage = FightControlForStart.instance.DefDamageProcessFun(cardData, attackedUnits[cardData.posIndex], damage);
            attackedUnits[cardData.posIndex].Hp.Add(-finalDamage);
            FightControlForStart.instance.AttackedAnimShow(attackedUnits[cardData.posIndex], finalDamage, false);
            if (attackedUnits[cardData.posIndex].cardType == 522)
            {
                if (attackedUnits[cardData.posIndex].Hp <= 0)
                {
                    FightControlForStart.instance.recordWinner = attackedUnits[cardData.posIndex].isPlayerCard ? -1 : 1;
                }
            }
        }
        for (int i = 0; i < CardNearbyAdditionForeach[cardData.posIndex].Length; i++)
        {
            FightCardData attackedUnit = attackedUnits[CardNearbyAdditionForeach[cardData.posIndex][i]];
            if (attackedUnit != null && attackedUnit.Hp > 0)
            {
                int finalDamage = FightControlForStart.instance.DefDamageProcessFun(cardData, attackedUnit, damage);
                attackedUnit.Hp.Add(-finalDamage);
                FightControlForStart.instance.AttackedAnimShow(attackedUnit, finalDamage, false);
                if (attackedUnit.cardType == 522)
                {
                    if (attackedUnit.Hp <= 0)
                    {
                        FightControlForStart.instance.recordWinner = attackedUnit.isPlayerCard ? -1 : 1;
                    }
                }
            }
        }
    }

    //奏乐台血量回复
    private void ZouYueTaiAddtionFun(FightCardData cardData, FightCardData[] cardsDatas)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.level);    //回复血量基值
        addtionNums = (int)(addtionNums * DataTable.GetGameValue(123) / 100f);
        FightControlForStart.instance.indexAttackType = 0;
        FightControlForStart.instance.PlayAudioForSecondClip(42, 0);
        for (int i = 0; i < CardNearbyAdditionForeach[cardData.posIndex].Length; i++)
        {
            FightCardData addedFightCard = cardsDatas[CardNearbyAdditionForeach[cardData.posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
            {
                FightControlForStart.instance.AttackToEffectShow(addedFightCard, false, Effect.Heal42A);
                addedFightCard.Hp.Add(addtionNums);
                FightControlForStart.instance.ShowSpellTextObj(addedFightCard.cardObj, DataTable.GetStringText(15), true, false);
                FightControlForStart.instance.AttackedAnimShow(addedFightCard, addtionNums, true);
            }
        }
    }

    //轩辕台护盾加成
    private void XuanYuanTaiAddtionFun(FightCardData cardData, FightCardData[] cardsDatas)
    {
        FightControlForStart.instance.PlayAudioForSecondClip(4, 0);
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.level);    //添加护盾的单位最大数
        for (int i = 0; i < CardNearbyAdditionForeach[cardData.posIndex].Length; i++)
        {
            FightCardData addedFightCard = cardsDatas[CardNearbyAdditionForeach[cardData.posIndex][i]];
            if (addedFightCard != null && addedFightCard.Hp > 0)
            {
                if (addedFightCard.cardType == 0 && addedFightCard.CardState.Shield <= 0)
                {
                    FightControlForStart.instance.AttackToEffectShow(addedFightCard, false, Effect.Shield4A);
                    addedFightCard.CardState.Shield = 1;
                    CreateSateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_withStand, true);
                    addtionNums--;
                    if (addtionNums <= 0)
                        break;
                }
            }
        }
    }

    //武将卡牌移动消除附加状态
    private void HeroCardRemoveStatus(FightCardData cardData)
    {
        //奏乐台图标
        {
            Transform tranZyt = cardData.cardObj.transform.GetChild(7).Find(StringNameStatic.StateIconPath_zouyuetaiAddtion);
            if (tranZyt != null)
                DestroyImmediate(tranZyt.gameObject);
        }
        //战斗力图标
        if (cardData.CardState.StrengthUp > 0)
        {
            cardData.CardState.StrengthUp = 0;
            DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
        }
        //风神台图标
        if (cardData.CardState.DodgeUp > 0)
        {
            cardData.CardState.DodgeUp = 0;
            DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_fengShenTaiAddtion, false);
        }
        //霹雳台图标
        if (cardData.CardState.CriticalUp > 0)
        {
            cardData.CardState.CriticalUp = 0;
            DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_pilitaiAddtion, false);
        }
        //霹雳台图标
        if (cardData.CardState.CriticalUp > 0)
        {
            cardData.CardState.CriticalUp = 0;
            DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_pilitaiAddtion, false);
        }
        //狼牙台图标
        if (cardData.CardState.RouseUp > 0)
        {
            cardData.CardState.RouseUp = 0;
            DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_langyataiAddtion, false);
        }
        //烽火台图标
        if (cardData.CardState.FengHuoTaiAddOn > 0)
        {
            cardData.CardState.FengHuoTaiAddOn = 0;
            DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_fenghuotaiAddtion, false);
        }
        //迷雾阵图标
        if (cardData.CardState.MiWuZhenAddOn > 0)
        {
            cardData.CardState.MiWuZhenAddOn = 0;
            DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_miWuZhenAddtion, false);
        }
    }

    /// <summary>
    /// 尝试激活或取消羁绊
    /// </summary>
    /// <param name="cardData">行动卡牌</param>
    /// <param name="isAdd">是否上阵</param>
    public void TryToActivatedBond(FightCardData cardData, bool isAdd)
    {
        var jiBanIds = DataTable.Hero[cardData.cardId].JiBanIds;
        if (jiBanIds == null || jiBanIds.Length == 0) return;

        Dictionary<int, JiBanActivedClass> jiBanActivedClasses = cardData.isPlayerCard
            ? FightControlForStart.instance.playerJiBanAllTypes
            : FightControlForStart.instance.enemyJiBanAllTypes;

        //遍历所属羁绊
        for (int i = 0; i < jiBanIds.Length; i++)
        {
            var jiBan = DataTable.JiBan[jiBanIds[i]];
            if (jiBan.IsOpen == 0) continue;
            JiBanActivedClass jiBanActivedClass = jiBanActivedClasses[jiBan.Id];
            bool isActived = true;
            for (int j = 0; j < jiBanActivedClass.List.Count; j++)
            {
                if (jiBanActivedClass.List[j].CardType == cardData.cardType &&
                    jiBanActivedClass.List[j].CardId == cardData.cardId)
                {
                    if (isAdd)
                    {
                        if (!jiBanActivedClass.List[j].Cards.Exists(t => t == cardData))
                        {
                            jiBanActivedClass.List[j].Cards.Add(cardData);
                        }
                    }
                    else
                    {
                        jiBanActivedClass.List[j].Cards.Remove(cardData);
                    }
                }

                if (jiBanActivedClass.List[j].Cards.Count <= 0)
                {
                    isActived = false;
                }
            }

            if (!jiBanActivedClass.IsActive && isActived)
            {
                for (int k = 0; k < jiBanActivedClass.List.Count; k++)
                {
                    for (int s = 0; s < jiBanActivedClass.List[k].Cards.Count; s++)
                    {
                        jiBanActivedClass.List[k].Cards[s].cardObj.transform.GetChild(10).gameObject
                            .SetActive(false);
                        jiBanActivedClass.List[k].Cards[s].cardObj.transform.GetChild(10).gameObject
                            .SetActive(true);
                    }
                }
            }
            jiBanActivedClass.IsActive = isActived;
        }
    }

    //上阵卡牌特殊相关处理
    public void CardGoIntoBattleProcess(FightCardData cardData, int posIndex, FightCardData[] cardDatas, bool isAdd)
    {

        switch (cardData.cardType)
        {
            case 0:
                TryToActivatedBond(cardData, isAdd);
                switch (DataTable.Hero[cardData.cardId].MilitaryUnitTableId)
                {
                    case 4://盾兵
                        if (isAdd)
                        {
                            if (cardData.CardState.Shield <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_withStand, true);
                            }
                            cardData.CardState.Shield = 1;
                        }
                        else
                        {
                            cardData.CardState.Shield = 0;
                            DestroySateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_withStand, true);
                        }
                        break;
                    case 58://铁骑
                        FightControlForStart.instance.UpdateTieQiStateIconShow(cardData, isAdd);
                        break;
                    default:
                        break;
                }
                if (!isAdd)
                {
                    HeroCardRemoveStatus(cardData);
                }
                else
                {
                    HeroSoldierAddtionFun(cardData, posIndex, cardDatas);
                }
                break;
            case 1:
                if (!isAdd)
                {
                    HeroCardRemoveStatus(cardData);
                }
                else
                {
                    HeroSoldierAddtionFun(cardData, posIndex, cardDatas);
                }
                break;
            case 2:
                switch (cardData.cardId)
                {
                    case 2://奏乐台
                        ZouYueTaiAddIcon(posIndex, cardDatas, isAdd);
                        break;
                    case 4://战鼓台
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 5://风神台
                        FengShenTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 7://霹雳台
                        PiLiTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 8://狼牙台
                        LangYaTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 9://烽火台
                        FengHuoTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 10://号角台
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 11://瞭望台
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 12://七星坛
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 13://斗神台
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 14://曹魏旗
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 15://蜀汉旗
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 16://东吴旗
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 17://迷雾阵
                        miWuZhenAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 18://迷雾阵
                        miWuZhenAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 19://骑兵营
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 20://弓弩营
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 22://长持营
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    case 23://战船营
                        ZhanGuTaiAddtionFun(cardData, posIndex, cardDatas, isAdd);
                        break;
                    default:
                        break;
                }

                break;
            default:
                break;
        }
    }

    //武将士兵卡牌加成效果
    private void HeroSoldierAddtionFun(FightCardData cardData, int posIndex, FightCardData[] cardDatas)
    {
        for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
        {
            FightCardData addtionCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
            
            if (addtionCard != null && addtionCard.cardType == 2 && addtionCard.Hp > 0)
            {
                int addtionNums = GetTowerAddValue(addtionCard.cardId, addtionCard.level);
                var armedType = MilitaryInfo.GetInfo(cardData.cardId).ArmedType;
                switch (addtionCard.cardId)
                {
                    case 2://奏乐台
                        if (cardData.cardObj.transform.GetChild(7).Find(StringNameStatic.StateIconPath_zouyuetaiAddtion) == null)
                        {
                            CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zouyuetaiAddtion, false);
                        }
                        break;
                    case 4://战鼓台
                        if (cardData.CardState.StrengthUp <= 0)
                        {
                            CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                        }
                        cardData.CardState.StrengthUp += addtionNums;
                        break;
                    case 5://风神台
                        if (cardData.CardState.DodgeUp <= 0)
                        {
                            CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_fengShenTaiAddtion, false);
                        }
                        cardData.CardState.DodgeUp += addtionNums;
                        break;
                    case 7://霹雳台
                        if (cardData.CardState.CriticalUp <= 0)
                        {
                            CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_pilitaiAddtion, false);
                        }
                        cardData.CardState.CriticalUp += addtionNums;
                        break;
                    case 8://狼牙台
                        if (cardData.CardState.RouseUp <= 0)
                        {
                            CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_langyataiAddtion, false);
                        }
                        cardData.CardState.RouseUp += addtionNums;
                        break;
                    case 9://烽火台
                        if (cardData.CardState.FengHuoTaiAddOn <= 0)
                        {
                            CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_fenghuotaiAddtion, false);
                        }
                        cardData.CardState.FengHuoTaiAddOn += addtionNums;
                        break;
                    case 10://号角台
                        if (cardData.combatType == 0)   //近战
                        {
                            if (cardData.CardState.StrengthUp <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.CardState.StrengthUp += addtionNums;
                        }
                        break;
                    case 11://瞭望台
                        if (cardData.combatType == 1)   //远程
                        {
                            if (cardData.CardState.StrengthUp <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.CardState.StrengthUp += addtionNums;
                        }
                        break;
                    case 12://七星坛
                        if (cardData.cardDamageType == 1) //法术
                        {
                            if (cardData.CardState.StrengthUp <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.CardState.StrengthUp += addtionNums;
                        }
                        break;
                    case 13://斗神台
                        if (cardData.cardDamageType == 0) //物理
                        {
                            if (cardData.CardState.StrengthUp <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.CardState.StrengthUp += addtionNums;
                        }
                        break;
                    case 14://曹魏旗
                        if (DataTable.Hero[cardData.cardId].ForceTableId == 1)
                        {
                            if (cardData.CardState.StrengthUp <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.CardState.StrengthUp += addtionNums;
                        }
                        break;
                    case 15://蜀汉旗
                        if (DataTable.Hero[cardData.cardId].ForceTableId == 0)
                        {
                            if (cardData.CardState.StrengthUp <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.CardState.StrengthUp += addtionNums;
                        }
                        break;
                    case 16://东吴旗
                        if (DataTable.Hero[cardData.cardId].ForceTableId == 2)
                        {
                            if (cardData.CardState.StrengthUp <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.CardState.StrengthUp += addtionNums;
                        }
                        break;
                    case 17://迷雾阵
                        if (cardData.CardState.MiWuZhenAddOn <= 0)
                        {
                            CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_miWuZhenAddtion, false);
                        }
                        cardData.CardState.MiWuZhenAddOn += addtionNums;
                        break;
                    case 18://迷雾阵
                        if (cardData.CardState.MiWuZhenAddOn <= 0)
                        {
                            CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_miWuZhenAddtion, false);
                        }
                        cardData.CardState.MiWuZhenAddOn += addtionNums;
                        break;
                    case 19://骑兵营
                        if (armedType == 5)
                        {
                            if (cardData.CardState.StrengthUp <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.CardState.StrengthUp += addtionNums;
                        }
                        break;
                    case 20://弓弩营
                        if (armedType == 9)
                        {
                            if (cardData.CardState.StrengthUp <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.CardState.StrengthUp += addtionNums;
                        }
                        break;
                    case 22://长持营
                        if (armedType == 3)
                        {
                            if (cardData.CardState.StrengthUp <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.CardState.StrengthUp += addtionNums;
                        }
                        break;
                    case 23://战船营
                        if (armedType == 8)
                        {
                            if (cardData.CardState.StrengthUp <= 0)
                            {
                                CreateSateIcon(cardData.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
                            }
                            cardData.CardState.StrengthUp += addtionNums;
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }

    //奏乐台状态栏添加
    private void ZouYueTaiAddIcon(int posIndex, FightCardData[] cardDatas, bool isAdd)
    {
        for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
        {
            FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
            {
                if (isAdd)
                {
                    if (addedFightCard.cardObj.transform.GetChild(7).Find(StringNameStatic.StateIconPath_zouyuetaiAddtion) == null)
                    {
                        CreateSateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zouyuetaiAddtion, false);
                    }
                }
                else
                {
                    if (addedFightCard.cardObj.transform.GetChild(7).Find(StringNameStatic.StateIconPath_zouyuetaiAddtion) != null)
                    {
                        DestroyImmediate(addedFightCard.cardObj.transform.GetChild(7).Find(StringNameStatic.StateIconPath_zouyuetaiAddtion).gameObject);
                    }
                }
            }
        }
    }

    //烽火台加成技能
    private void FengHuoTaiAddtionFun(FightCardData cardData, int posIndex, FightCardData[] cardDatas, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.level);

        for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
        {
            FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
            {
                if (isAdd)
                {
                    if (addedFightCard.CardState.FengHuoTaiAddOn <= 0)
                    {
                        CreateSateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_fenghuotaiAddtion, false);
                    }
                    addedFightCard.CardState.FengHuoTaiAddOn += addtionNums;
                }
                else
                {
                    addedFightCard.CardState.FengHuoTaiAddOn -= addtionNums;
                    if (addedFightCard.CardState.FengHuoTaiAddOn <= 0)
                    {
                        addedFightCard.CardState.FengHuoTaiAddOn = 0;
                        DestroySateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_fenghuotaiAddtion, false);
                    }
                }
            }
        }
    }

    //狼牙台加成技能
    private void LangYaTaiAddtionFun(FightCardData cardData, int posIndex, FightCardData[] cardDatas, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.level);

        for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
        {
            FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
            {
                if (isAdd)
                {
                    if (addedFightCard.CardState.RouseUp <= 0)
                    {
                        CreateSateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_langyataiAddtion, false);
                    }
                    addedFightCard.CardState.RouseUp += addtionNums;
                }
                else
                {
                    addedFightCard.CardState.RouseUp -= addtionNums;
                    if (addedFightCard.CardState.RouseUp <= 0)
                    {
                        addedFightCard.CardState.RouseUp = 0;
                        DestroySateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_langyataiAddtion, false);
                    }
                }
            }
        }
    }

    //霹雳台加成技能
    private void PiLiTaiAddtionFun(FightCardData cardData, int posIndex, FightCardData[] cardDatas, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.level);

        for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
        {
            FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
            {
                if (isAdd)
                {
                    if (addedFightCard.CardState.CriticalUp <= 0)
                    {
                        CreateSateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_pilitaiAddtion, false);
                    }
                    addedFightCard.CardState.CriticalUp += addtionNums;
                }
                else
                {
                    addedFightCard.CardState.CriticalUp -= addtionNums;
                    if (addedFightCard.CardState.CriticalUp <= 0)
                    {
                        addedFightCard.CardState.CriticalUp = 0;
                        DestroySateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_pilitaiAddtion, false);
                    }
                }
            }
        }
    }

    //迷雾阵加成技能
    private void miWuZhenAddtionFun(FightCardData cardData, int posIndex, FightCardData[] cardDatas, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.level);

        List<GameObject> posListToSetMiWu = cardData.isPlayerCard ? playerCardsPos : enemyCardsPos;

        for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
        {
            if (!cardData.isPlayerCard)
            {
                //迷雾动画
                GameObject stateDinObj = EffectsPoolingControl.instance.GetBuffEffect(StringNameStatic.StateIconPath_miWuZhenAddtion, cardData.cardObj.transform)?.gameObject;
                stateDinObj.name = StringNameStatic.StateIconPath_miWuZhenAddtion + "Din";
                stateDinObj.transform.position = posListToSetMiWu[CardNearbyAdditionForeach[posIndex][i]].transform.position;
                stateDinObj.GetComponent<Animator>().enabled = false;
            }
            FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
            {
                if (isAdd)
                {
                    if (addedFightCard.CardState.MiWuZhenAddOn <= 0)
                    {
                        CreateSateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_miWuZhenAddtion, false);
                    }
                    addedFightCard.CardState.MiWuZhenAddOn += addtionNums;
                }
                else
                {
                    addedFightCard.CardState.MiWuZhenAddOn -= addtionNums;
                    if (addedFightCard.CardState.MiWuZhenAddOn <= 0)
                    {
                        addedFightCard.CardState.MiWuZhenAddOn = 0;
                        DestroySateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_miWuZhenAddtion, false);
                    }
                }
            }
        }

        if (!isAdd && !cardData.isPlayerCard)
        {
            for (int i = 0; i < cardData.cardObj.transform.childCount; i++)
            {
                Transform tran = cardData.cardObj.transform.GetChild(i);
                if (tran.name == StringNameStatic.StateIconPath_miWuZhenAddtion + "Din")
                {
                    tran.gameObject.SetActive(false);
                }
            }
        }
    }

    //风神台加成技能
    private void FengShenTaiAddtionFun(FightCardData cardData, int posIndex, FightCardData[] cardDatas, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.level);

        for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
        {
            FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
            if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
            {
                if (isAdd)
                {
                    if (addedFightCard.CardState.DodgeUp <= 0)
                    {
                        CreateSateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_fengShenTaiAddtion, false);
                    }
                    addedFightCard.CardState.DodgeUp += addtionNums;
                }
                else
                {
                    addedFightCard.CardState.DodgeUp -= addtionNums;
                    if (addedFightCard.CardState.DodgeUp <= 0)
                    {
                        addedFightCard.CardState.DodgeUp = 0;
                        DestroySateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_fengShenTaiAddtion, false);
                    }
                }
            }
        }
    }

    //战鼓台(10-13塔)加成技能
    private void ZhanGuTaiAddtionFun(FightCardData cardData, int posIndex, FightCardData[] cardDatas, bool isAdd)
    {
        int addtionNums = GetTowerAddValue(cardData.cardId, cardData.level);

        switch (cardData.cardId)
        {
            case 4://战鼓台
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                        DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                }
                break;
            case 10://号角台
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        if (addedFightCard.combatType == 0)   //近战
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 11://瞭望台
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        if (addedFightCard.combatType == 1)   //远程
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 12://七星坛
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        if (addedFightCard.cardDamageType == 1) //法术
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 13://斗神台
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        if (addedFightCard.cardDamageType == 0) //物理
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 14://曹魏旗
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        if (DataTable.Hero[addedFightCard.cardId].ForceTableId == 1) //魏势力
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 15://蜀汉旗
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        if (DataTable.Hero[addedFightCard.cardId].ForceTableId == 0) //蜀势力
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 16://东吴旗
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        if (DataTable.Hero[addedFightCard.cardId].ForceTableId == 2) //吴势力
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 19://骑兵营
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        if (MilitaryInfo.GetInfo(addedFightCard.cardId).ArmedType == 5)
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 20://弓弩营
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        if (MilitaryInfo.GetInfo(addedFightCard.cardId).ArmedType == 9)
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 22://长持营
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        if (MilitaryInfo.GetInfo(addedFightCard.cardId).ArmedType == 3)
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            case 23://战船营
                for (int i = 0; i < CardNearbyAdditionForeach[posIndex].Length; i++)
                {
                    FightCardData addedFightCard = cardDatas[CardNearbyAdditionForeach[posIndex][i]];
                    if (addedFightCard != null && addedFightCard.cardType == 0 && addedFightCard.Hp > 0)
                    {
                        if (MilitaryInfo.GetInfo(addedFightCard.cardId).ArmedType == 8)
                        {
                            DamageTowerAdditionFun(addedFightCard, isAdd, addtionNums);
                        }
                    }
                }
                break;
            default:
                break;
        }

    }

    //伤害加成塔的上阵下阵对周为单位的处理
    private void DamageTowerAdditionFun(FightCardData addedFightCard, bool isAdd, int addtionNums)
    {
        if (isAdd)
        {
            if (addedFightCard.CardState.StrengthUp <= 0)
            {
                CreateSateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
            }
            addedFightCard.CardState.StrengthUp += addtionNums;
        }
        else
        {
            addedFightCard.CardState.StrengthUp -= addtionNums;
            if (addedFightCard.CardState.StrengthUp <= 0)
            {
                addedFightCard.CardState.StrengthUp = 0;
                DestroySateIcon(addedFightCard.cardObj.transform.GetChild(7), StringNameStatic.StateIconPath_zhangutaiAddtion, false);
            }
        }
    }

    //获取塔的加成值
    private int GetTowerAddValue(int towerId, int towerGrade) => GameCardInfo.GetInfo(GameCardType.Tower,towerId).GetDamage(towerGrade);

    /// <summary>
    /// 创建状态图标
    /// </summary>
    /// <param name="tran"></param>
    /// <param name="stateName"></param>
    public void CreateSateIcon(Transform tran, string stateName, bool isShowEffect)
    {
        if (tran.Find(stateName) == null)
        {
            GameObject stateIconObj = EffectsPoolingControl.instance.GetBuffEffect("stateIcon", tran).gameObject;
            stateIconObj.name = stateName;
            stateIconObj.GetComponent<Image>().sprite = Resources.Load("Image/fightStateIcon/" + stateName, typeof(Sprite)) as Sprite;
        }
        if (isShowEffect && tran.parent.Find(stateName + "Din") == null)
        {
            GameObject stateDinObj = EffectsPoolingControl.instance.GetBuffEffect(stateName, tran.parent).gameObject;
            stateDinObj.name = stateName + "Din";
        }
    }

    //删除状态图标
    public void DestroySateIcon(Transform tran, string stateName, bool isShowEffect)
    {
        Transform tran0 = tran.Find(stateName);
        if (tran0 != null)
            EffectsPoolingControl.instance.RecycleEffect(tran0.gameObject.GetComponent<EffectStateUi>());
        if (isShowEffect)
        {
            Transform tran1 = tran.parent.Find(stateName + "Din");
            if (tran1 != null)
                EffectsPoolingControl.instance.RecycleEffect(tran1.gameObject.GetComponent<EffectStateUi>());
        }
    }

    /// <summary>
    /// 匹配稀有度的颜色
    /// </summary>
    /// <returns></returns>
    private Color NameColorChoose(int rarity)
    {
        Color color = new Color();
        switch (rarity)
        {
            case 1:
                color = ColorDataStatic.name_gray;
                break;
            case 2:
                color = ColorDataStatic.name_green;
                break;
            case 3:
                color = ColorDataStatic.name_blue;
                break;
            case 4:
                color = ColorDataStatic.name_purple;
                break;
            case 5:
                color = ColorDataStatic.name_orange;
                break;
            case 6:
                color = ColorDataStatic.name_red;
                break;
            case 7:
                color = ColorDataStatic.name_black;
                break;
            default:
                color = ColorDataStatic.name_gray;
                break;
        }
        return color;
    }
}