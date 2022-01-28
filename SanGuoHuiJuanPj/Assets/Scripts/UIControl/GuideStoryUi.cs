using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GuideStoryUi : MonoBehaviour
{
    private int storyIndex;    //当前剧情故事编号
    [SerializeField] private StoryWindow Story;
    private GuideTable[] guides;          //剧情故事数量

    bool isShowed0 = false;
    bool isShowed1 = false;
    [SerializeField] private GameObject[] guideObjs;     //指引obj
    [SerializeField] private BarrageUiController barragesController;
    [SerializeField] private WarBoardUi warBoard;
    private List<string[]> barragesList;

    public void BeginStory()
    {
        storyIndex++;
        if (!EffectsPoolingControl.instance.IsInit)
            EffectsPoolingControl.instance.Init();
        var loginUi = GameSystem.LoginUi;
        loginUi.gameObject.SetActive(false);
        warBoard.Init();
        PlayStoryIntro();
    }

    public void Init()
    {
        barragesController.Init();
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

    //播放固定弹幕
    IEnumerator ShowBarrageForStory(int index)
    {
        for (int i = 0; i < barragesList[index].Length; i++)
        {
            int randTime = Random.Range(2, 5);  //间隔时间
            yield return new WaitForSeconds(randTime);
            barragesController.PlayBarrage(barragesList[index][i]);
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
                    Story.Button.enabled = true;
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

    private Sprite GetStoryTitle() =>
        Resources.Load("Image/startFightImg/Title/" + storyIndex, typeof(Sprite)) as Sprite;
    /// <summary>
    /// 初始化卡牌到战斗位上
    /// </summary>
    public void PlayStoryIntro()
    {
        var guide = DataTable.Guide[storyIndex];
        if(!Story.Background.gameObject.activeSelf)
            Story.Background.gameObject.SetActive(true);
        Story.Title.sprite = GetStoryTitle();
        StartCoroutine(PlayStory(guide));
    }

    private IEnumerator PlayStory(GuideTable guide)
    {
        Story.Intro.text = string.Empty;
        Story.ClickToContinue.gameObject.SetActive(false);
        yield return Story.Background.DOFade(1, 1.5f).WaitForCompletion();
        Story.Intro.gameObject.SetActive(true);
        Story.TitleObj.gameObject.SetActive(true);
        Story.MoonObj.gameObject.SetActive(true);
        //播放片头弹幕

        InitWarboard(guide);

        Story.Intro.color = new Color(Story.Intro.color.r, Story.Intro.color.g, Story.Intro.color.b, 0);
        StartCoroutine(ShowBarrageForStory(storyIndex - 1));
        yield return DOTween.Sequence()
            .Append(Story.Intro.DOFade(1, 5f))
            .Join(Story.Intro.DOText(guide.Intro, 8f).SetEase(Ease.Linear))
            .WaitForCompletion();
        if (!isShowFHDM)
        {
            StartCoroutine(ShowFeiHuaBarrage());
            isShowFHDM = true;
        }
        //guideStoryObj.transform.GetChild(1).gameObject.SetActive(true);
        //guideStoryObj.transform.GetChild(2).gameObject.SetActive(true);
        Story.Button.onClick.RemoveAllListeners();
        if (guide.Id > 2)
            Story.Button.onClick.AddListener(StartSceneToServerCS.instance.PromptLoginWindow);
        else Story.Button.onClick.AddListener(()=>StartCoroutine(OnStartWarboard()));
        Story.Button.enabled = true;
        Story.ClickToContinue.gameObject.SetActive(true);
        storyIndex++;
    }

    private void InitWarboard(GuideTable guide)
    {
        var racks = guide.Poses(GuideProps.Card);
        var players = guide.Poses(GuideProps.Player);
        var enemies = guide.Poses(GuideProps.Enemy);
        warBoard.StartNewGame(FightCardData.BaseCard(false, guide.EnemyBaseHp, 1),
            FightCardData.BaseCard(true, guide.BaseHp, 1),
            enemies.Where(e => e.Value != null).Select((e, i) => ChessCard.Instance(e.Value.CardId, e.Value.CardType, e.Value.Star, e.Key))
                .ToList());
        warBoard.MaxCards = 20;
        warBoard.UpdateHeroEnlistText();
        foreach (var c in racks.Values.Where(c=>c!=null)) warBoard.CreateCardToRack(GameCard.Instance(c.CardId, c.CardType, c.Star));

        foreach (var chessman in players)
        {
            var c = chessman.Value;
            if (c == null) continue;
            var card = new FightCardData(GameCard.Instance(c.CardId, c.CardType, c.Star))
            {
                IsLock = true,
                posIndex = chessman.Key,
                isPlayerCard = true
            };
            warBoard.SetPlayerChessman(card);
        }
        warBoard.Chessboard.StartButton.gameObject.SetActive(false);
        warBoard.Chessboard.StartButton.onClick.RemoveAllListeners();
        warBoard.Chessboard.StartButton.onClick.AddListener(warBoard.OnLocalRoundStart);
    }

    private IEnumerator OnStartWarboard()
    {
        WarMusicController.Instance.OnBattleMusic();
        WarMusicController.Instance.PlayBgm(storyIndex - 1);
        Story.Intro.text = string.Empty;
        warBoard.gameObject.SetActive(true);
        yield return Story.Background.DOFade(0, 1.5f).WaitForCompletion();
        Story.Background.gameObject.SetActive(false);

        warBoard.OnGameSet.RemoveListener(FinalizeStory);
        warBoard.OnGameSet.AddListener(FinalizeStory);
        warBoard.Chessboard.StartButton.gameObject.SetActive(true);
    }

    private void FinalizeStory(bool isWin)
    {
        StartCoroutine(Finalization());
        IEnumerator Finalization()
        {
            yield return new WaitForSeconds(4f);
            AudioController1.instance.PlayLoop(StartSceneUIManager.instance.pianTouAudio, 1);
            PlayStoryIntro();
        }
    }

    ////更新故事战斗数据
    //private void UpdateCardDataForStory()
    //{
    //    //羁绊数据重置
    //    CardManager.ResetJiBan(FightControlForStart.instance.playerJiBanAllTypes);
    //    CardManager.ResetJiBan(FightControlForStart.instance.enemyJiBanAllTypes);
    //    if (playerFightCardsDatas[17] == null)
    //    {
    //        CreatePlayerHomeCard();
    //    }
    //    else
    //    {
    //        playerFightCardsDatas[17].cardObj.transform.GetChild(4).gameObject.SetActive(false);
    //        enemyFightCardsDatas[17].cardObj.transform.GetChild(4).gameObject.SetActive(false);

    //        playerFightCardsDatas[17].fullHp =
    //            playerFightCardsDatas[17].nowHp = guides[storyIndex].BaseHp; //我方血量
    //        enemyFightCardsDatas[17].fullHp =
    //            enemyFightCardsDatas[17].nowHp = guides[storyIndex].EnemyBaseHp; //敌方血量

    //        playerFightCardsDatas[17].cardObj.transform.GetChild(2).GetComponent<Image>().fillAmount = 0;
    //        enemyFightCardsDatas[17].cardObj.transform.GetChild(2).GetComponent<Image>().fillAmount = 0;
    //    }

    //    //清空备战位
    //    for (int i = 0; i < herosCardListTran.childCount; i++)
    //    {
    //        Destroy(herosCardListTran.GetChild(i).gameObject);
    //    }

    //    playerCardsDatas.Clear();
    //    //清空棋盘单位
    //    for (int i = 0; i < 20; i++)
    //    {
    //        if (i != 17)
    //        {
    //            if (playerFightCardsDatas[i] != null)
    //            {
    //                Destroy(playerFightCardsDatas[i].cardObj);
    //                playerFightCardsDatas[i] = null;
    //            }

    //            if (enemyFightCardsDatas[i] != null)
    //            {
    //                Destroy(enemyFightCardsDatas[i].cardObj);
    //                enemyFightCardsDatas[i] = null;
    //            }
    //        }
    //    }

    //    FightControlForStart.instance.ClearEmTieQiCardList();
    //    var guide = guides[storyIndex];
    //    //创建玩家备战单位
    //    foreach (var card in guide.Poses(GuideProps.Card))
    //    {
    //        if (card == null) continue;
    //        FightCardData data = CreateFightUnit(card, herosCardListTran, true);
    //        data.cardObj.GetComponent<CardDragForStart>().posIndex = -1;
    //        data.cardObj.GetComponent<CardDragForStart>().isFightCard = false;
    //        data.posIndex = -1;
    //        data.isPlayerCard = true;
    //        playerCardsDatas.Add(data);
    //    }

    //    //创建玩家棋盘单位
    //    var playerChessmen = guide.Poses(GuideProps.Player);
    //    for (var i = 0; i < playerChessmen.Length; i++)
    //    {
    //        var card = playerChessmen[i];
    //        if (card == null) continue;
    //        FightCardData data = CreateFightUnit(card, playerCardsBox, true);
    //        data.cardObj.GetComponent<CardDragForStart>().posIndex = i;
    //        data.cardObj.GetComponent<CardDragForStart>().isFightCard = true;
    //        data.posIndex = i;
    //        data.isPlayerCard = true;
    //        data.cardObj.transform.position = playerCardsPos[i].transform.position;
    //        playerFightCardsDatas[i] = data;
    //        CardGoIntoBattleProcess(playerFightCardsDatas[i], i, playerFightCardsDatas, true);
    //    }

    //    //创建敌人棋盘单位
    //    var enemyChessmen = guide.Poses(GuideProps.Enemy);
    //    for (int i = 0; i < 20; i++)
    //    {
    //        var card = enemyChessmen[i];
    //        if (card == null) continue;
    //        FightCardData data = CreateFightUnit(card, enemyCardsBox, false);
    //        data.posIndex = i;
    //        data.isPlayerCard = false;
    //        data.cardObj.transform.position = enemyCardsPos[i].transform.position;
    //        enemyFightCardsDatas[i] = data;
    //        CardGoIntoBattleProcess(enemyFightCardsDatas[i], i, enemyFightCardsDatas, true);
    //    }
    //}

    ////创建敌方卡牌战斗单位数据
    //private FightCardData CreateFightUnit(Chessman chessman, Transform cardsBoxTran, bool isPlayerCard)
    //{
    //    FightCardData data = new FightCardData();
    //    data.cardObj = Instantiate(isPlayerCard ? fightCardPyPre : fightCardPre, cardsBoxTran);
    //    data.cardType = chessman.CardType;
    //    data.cardId = chessman.CardId;
    //    data.cardGrade = chessman.Star;
    //    data.fightState = new FightState();
    //    var cardType = (GameCardType)chessman.CardType;
    //    var card = new NowLevelAndHadChip().Instance(cardType, chessman.CardId);
    //    card.level = chessman.Star;
    //    var info = card.GetInfo();
    //    //兵种
    //    data.cardObj.transform.GetChild(1).GetComponent<Image>().sprite = cardType == GameCardType.Hero
    //        ? GameResources.HeroImg[card.id]
    //        : GameResources.FuZhuImg[info.ImageId];
    //    //血量
    //    data.cardObj.transform.GetChild(2).GetComponent<Image>().fillAmount = 0;
    //    //名字
    //    ShowNameTextRules(data.cardObj.transform.GetChild(3).GetComponent<Text>(), info.Name);
    //    //名字颜色
    //    data.cardObj.transform.GetChild(3).GetComponent<Text>().color = NameColorChoose(info.Rare);
    //    //星级
    //    data.cardObj.transform.GetChild(4).GetComponent<Image>().sprite = GameResources.GradeImg[info.Rare];
    //    //兵种
    //    data.cardObj.transform.GetChild(5).GetComponentInChildren<Text>().text = info.Short;
    //    //边框
    //    FrameChoose(info.Rare, data.cardObj.transform.GetChild(6).GetComponent<Image>());
    //    data.damage = info.GetDamage(card.level);
    //    data.hpr = info.GameSetRecovery;
    //    data.fullHp = data.nowHp = info.GetHp(card.level);
    //    data.activeUnit = cardType == GameCardType.Hero || ((cardType == GameCardType.Tower) &&
    //                                                        (info.Id == 0 || info.Id == 1 || info.Id == 2 ||
    //                                                         info.Id == 3 || info.Id == 6));
    //    data.cardMoveType = info.CombatType;
    //    data.cardDamageType = info.DamageType;
    //    if (cardType != GameCardType.Hero) data.cardObj.transform.GetChild(5).GetComponent<Image>().sprite = GameResources.ClassImg[1];
    //    return data;
    //}

    [Serializable]private class StoryWindow
    {
        public int Index;
        public Image Background;
        public Image Title;
        public Text Intro;
        public Text ClickToContinue;
        public Button Button;
        public GameObject MoonObj;
        public GameObject TitleObj;
    }
}