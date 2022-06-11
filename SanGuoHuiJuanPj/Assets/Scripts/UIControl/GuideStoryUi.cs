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
    [SerializeField] private BarrageUiController barragesController;
    [SerializeField] private WarBoardUi warBoard;
    [SerializeField] private Image GuidePanel; //指引档板
    [SerializeField] private Text[] GuideTexts;//引导文本
    [Header("指示显示间隔")][SerializeField] private float GuideTextInterval = 1f;
    [Header("指示渐变时间")][SerializeField] private float GuideTextFading = 1f;
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
        GuidePanel.gameObject.SetActive(false);
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
        "好玩么？",
        "新手剧情？？？",
        "惊天动地之伟业……",
        "虎年大吉，哈哈哈",
        "虎年大吉！",
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
        "对面嗓门都挺大",
        "怎么开始战斗……",
        "要点中间那个战",
        "关羽上去砍啊",
        "张角前来送人头",
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
        "对面人好多",
        "听说吕布可以无限连击",
        "可以上五虎了",
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
        "五虎上将何在？！",
        "一吕二赵三潘凤",
        "无双上将潘凤前来报道",
        "拒马可以挡两路……",//10
        "潘无双前来送人头",
        "刘备：来啊，互相伤害啊",
        "上五虎啊",
        "我关羽是被毒死的",
        "骑兵被拒马弄死了",
        "吕布被围殴了",
        "红将是最强武将？",
        "有点意思",
        "温酒斩华雄",
        "酒且斟下，某去便来。",//20
        "狼骑兵冲呀冲呀",
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

    private Sprite GetStoryTitle() =>
        Resources.Load("Image/startFightImg/Title/" + storyIndex, typeof(Sprite)) as Sprite;
    /// <summary>
    /// 初始化卡牌到战斗位上
    /// </summary>
    public void PlayStoryIntro()
    {
        PanelAnimation.gameObject.SetActive(false);
        var guide = DataTable.Guide[storyIndex];
        if(!Story.Background.gameObject.activeSelf)
            Story.Background.gameObject.SetActive(true);
        Story.Title.sprite = GetStoryTitle();
        StartCoroutine(PlayStory(guide));
    }

    [SerializeField] private Animation PanelAnimation;
    [Header("遮罩动画时间")][SerializeField] private float PanelSec = 5f;
    private IEnumerator PlayStory(GuideTable guide)
    {
        Story.Intro.text = string.Empty;
        Story.ClickToContinue.gameObject.SetActive(false);
        yield return Story.Background.DOFade(1, 1.5f).WaitForCompletion();
        PanelAnimation.gameObject.SetActive(true);
        Story.Intro.gameObject.SetActive(true);
        Story.TitleObj.gameObject.SetActive(true);
        Story.MoonObj.gameObject.SetActive(true);
        //播放片头弹幕
        Story.Intro.text = guide.Intro;
        Story.Intro.gameObject.SetActive(true);
        PanelAnimation.Play(PlayMode.StopAll);
        StartCoroutine(ShowBarrageForStory(storyIndex - 1));
        if (!isShowFHDM)
        {
            StartCoroutine(ShowFeiHuaBarrage());
            isShowFHDM = true;
        }

        yield return new WaitForSeconds(PanelSec);
        PanelAnimation.gameObject.SetActive(false);
        InitWarboard(guide);
        Story.Button.onClick.RemoveAllListeners();
        if (guide.Id > 2)
        {
            GamePref.SetIsPlayedIntro(true);
            Story.Button.onClick.AddListener(() => GameSystem.LoginUi.gameObject.SetActive(true));
        }
        else Story.Button.onClick.AddListener(OnStartWarboard);
        Story.Button.enabled = true;
        Story.ClickToContinue.gameObject.SetActive(true);
        GuidePanel.gameObject.SetActive(guide.Id == 1);
        storyIndex++;
    }

    private void InitWarboard(GuideTable guide)
    {
        warBoard.InitNewGame(false, true);
        var racks = guide.Poses(GuideProps.Card);
        var players = guide.Poses(GuideProps.Player);
        var enemies = guide.Poses(GuideProps.Enemy);
        warBoard.InitNewChessboard(FightCardData.BaseCard(false, guide.EnemyBaseHp, 1),
            FightCardData.BaseCard(true, guide.BaseHp, 1),
            enemies.Where(e => e.Value != null).Select((e, i) =>
                    ChessCard.Instance(e.Value.CardId, e.Value.CardType, e.Value.Star, e.Key))
                .ToList());
        warBoard.MaxCards = 20;
        warBoard.UpdateHeroEnlistText();
        foreach (var c in racks.Values.Where(c => c != null))
            warBoard.CreateCardToRack(GameCard.Instance(c.CardId, c.CardType, c.Star));

        foreach (var chessman in players)
        {
            var c = chessman.Value;
            if (c == null) continue;
            var card = new FightCardData(GameCard.Instance(c.CardId, c.CardType, c.Star))
            {
                IsLock = true,
                isPlayerCard = true
            };
            card.SetPos(chessman.Key);
            warBoard.SetPlayerChessman(card);
        }

        warBoard.Chessboard.ResetStartWarUi();
        warBoard.Chessboard.SetStartWarUi(() =>
        {
            warBoard.OnLocalRoundStart();
            GuidePanel.gameObject.SetActive(false);
        });
    }

    private IEnumerator PlayGuideTexts()
    {
        for (int i = 0; i < GuideTexts.Length; i++) GuideTexts[i].gameObject.SetActive(false);
        for (int i = 0; i < GuideTexts.Length; i++)
        {
            yield return new WaitForSeconds(GuideTextInterval);
            var textUi = GuideTexts[i];
            yield return textUi.DOFade(0, 0).WaitForCompletion();
            textUi.gameObject.SetActive(true);
            yield return textUi.DOFade(1, GuideTextFading).WaitForCompletion();
        }
    }

    private void OnStartWarboard()
    {
        Story.Intro.gameObject.SetActive(false);
        Story.TitleObj.gameObject.SetActive(false);
        Story.MoonObj.gameObject.SetActive(false);
        Story.Button.onClick.RemoveAllListeners();
        if (storyIndex == 2)
        {
            StartCoroutine(PlayGuideTexts());
        }

        PlayZhanZhongBarrage();
        WarMusicController.Instance.OnBattleMusic();
        WarMusicController.Instance.PlayBgm(storyIndex - 1);
        Story.Intro.text = string.Empty;
        warBoard.gameObject.SetActive(true);
        Story.Background.DOFade(0, 1.5f).OnComplete(() => Story.Background.gameObject.SetActive(false));
        warBoard.OnGameSet.RemoveListener(FinalizeStory);
        warBoard.OnGameSet.AddListener(FinalizeStory);
        warBoard.Chessboard.DisplayStartButton(true);
    }

    private void FinalizeStory(bool isWin)
    {
        StartCoroutine(Finalization());
        IEnumerator Finalization()
        {
            AudioController1.instance.PlayLoop(StartSceneUIManager.instance.pianTouAudio, 1, 4);
            yield return new WaitForSeconds(4);
            PlayStoryIntro();
        }
    }

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