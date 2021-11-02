using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class StartSceneUIManager : MonoBehaviour
{
    public static StartSceneUIManager instance;

    [SerializeField]
    GameObject chooseForceCityObj;  //势力选择父级
    [SerializeField]
    GameObject verseListObj;  //诗句父级
    [SerializeField]
    Text powerIntroText;  //势力介绍
    [SerializeField]
    float verseShowSpeed;
    [SerializeField]
    AudioClip pianTouAudio;
    [SerializeField]
    float pianTouAudioVolume;
    //[SerializeField]
    //Button startBtn;    //开始按钮
    [SerializeField]
    GameObject EffectPoolManagerObj;   //特效组
    bool isJumping; //是否在跳转
    [HideInInspector]public bool isPlayedStory;//是否已播放了剧情

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        isJumping = false;
        isPlayedStory = false;
    }

    public void Init()
    {
        InitPictureContentShow();
    }

    //初始加载诗句等
    private void InitPictureContentShow()
    {
        chooseForceCityObj.SetActive(false);
        powerIntroText.gameObject.SetActive(false);
        for (int i = 0; i < verseListObj.transform.childCount; i++)
        {
            verseListObj.transform.GetChild(i).gameObject.SetActive(false);
        }
        //播放诗句
        ShowNextVerse(0);
        AudioController1.instance.isNeedPlayLongMusic = true;
        AudioController1.instance.ChangeAudioClip(pianTouAudio, pianTouAudioVolume);
        AudioController1.instance.PlayLongBackMusInit();
    }

    public void DontHaveSaveDataPlayStory()
    {
        EffectPoolManagerObj.SetActive(true);

        //chooseForceCityObj.transform.GetChild(0).GetComponent<Button>().onClick.Invoke();
        for (int i = 0; i < chooseForceCityObj.transform.childCount; i++)
        {
            chooseForceCityObj.transform.GetChild(i).GetComponent<Button>().onClick.AddListener(() =>
                AudioController0.instance.RandomPlayGuZhengAudio());
        }
        //这里开始生成战斗
    }

    //结束剧情选择势力
    public void EndStoryToChooseForce()
    {
        isPlayedStory = true;
        isJumping = false;

        EffectPoolManagerObj.SetActive(false);

        verseListObj.SetActive(false);
        chooseForceCityObj.SetActive(true);
        powerIntroText.gameObject.SetActive(true);

        chooseForceCityObj.transform.GetChild(0).GetComponent<Button>().onClick.Invoke();

        StartSceneToServerCS.instance.LoginGameInfoFun();
    }

    //诗句展示
    private void ShowNextVerse(int i)
    {
        if (i < verseListObj.transform.childCount)
        {
            string str = verseListObj.transform.GetChild(i).GetComponent<Text>().text;
            verseListObj.transform.GetChild(i).GetComponent<Text>().text = "";
            verseListObj.transform.GetChild(i).gameObject.SetActive(true);
            verseListObj.transform.GetChild(i).GetComponent<Text>().text = str;
            verseListObj.transform.GetChild(i).GetComponent<Text>().DOFade(1f, verseShowSpeed).SetEase(Ease.Unset).OnComplete(
                delegate ()
                {
                    verseListObj.transform.GetChild(i).GetComponent<Text>().DOFade(1, verseShowSpeed * 2).SetEase(Ease.Unset);
                    ShowNextVerse(i + 1);
                });
            //verseListObj.transform.GetChild(i).GetComponent<Text>().DOText(str, verseShowSpeed).SetEase(Ease.Linear).SetAutoKill(false);
        }
    }

    /// <summary>
    /// 跳转场景
    /// </summary>
    public void LoadingScene(GameSystem.GameScene scene, bool isRequestSyncData)
    {
        if (isJumping)
            return;
        isJumping = true;
        AudioController0.instance.ChangeAudioClip(12);
        AudioController0.instance.PlayAudioSource(0);
        TimeSystemControl.instance.InitIsTodayFirstLoadingGame();
        PlayerDataForGame.instance.JumpSceneFun(scene, isRequestSyncData);
    }

    /// <summary>
    /// 选择初始势力
    /// </summary>
    /// <param name="forceId"></param>
    public void ChooseFirstForce(int forceId)
    {
        LoadSaveData.instance.firstForceId = forceId;
        powerIntroText.DOPause();
        powerIntroText.text = "";
        powerIntroText.color = new Color(powerIntroText.color.r, powerIntroText.color.g, powerIntroText.color.b, 0);
        powerIntroText.DOFade(1, 2.5f);
        powerIntroText.DOText(("\u2000\u2000\u2000\u2000" + DataTable.PlayerInitialConfig[forceId].ForceIntro), 2.5f).SetEase(Ease.Linear).SetAutoKill(false);
    }
}