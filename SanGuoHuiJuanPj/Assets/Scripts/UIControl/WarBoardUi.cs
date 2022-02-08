using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Assets.System.WarModule;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class WarBoardUi : MonoBehaviour
{
    /// <summary>
    /// 当前武将卡牌上阵最大数量
    /// </summary>
    public int MaxCards;
    public Chessboard Chessboard;
    public Animator StartBtnAnimator;
    public JiBanAnimationManager JiBanManager;
    [SerializeField] private Image Background;
    [SerializeField] private PlayerCardRack Rack;
    [SerializeField] private ChessboardInputController ChessboardInputControl;
    [SerializeField] private ChessboardVisualizeManager ChessboardManager;
    [SerializeField] private NewWarManager NewWarManager;
    [SerializeField] private Text heroEnlistText; //武将上阵文本
    [SerializeField] AboutCardUi aboutCardUi; //阵上卡牌详情展示位
    public UnityEvent<bool> OnGameSet = new GameSetEvent();
    private WarGameCardUi playerBaseObj { get; set; }
    private WarGameCardUi enemyBaseObj { get; set; }
    public bool IsDragDisable { get; private set; }
    public bool IsBusy { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsFirstRound { get; private set; }
    private Toggle AutoRoundToggle => Chessboard == null ? null : Chessboard.AutoRoundToggle;
    private float autoRoundTimer;
    [SerializeField] private float AutoRoundSecs = 1.5f;
    private Slider AutoRoundSlider => Chessboard.AutoRoundSlider;
    public event UnityAction OnRoundPause;
    public event UnityAction<int> OnRoundStart;
    private ObjectPool<WarGameCardUi> UiPool { get; set; }

    public void Init()
    {
        Rack.Init(this);
        ChessboardInputControl.Init(this);
        ChessboardManager.Init(Chessboard, JiBanManager);
        ChessboardManager.OnRoundStart += i => OnRoundStart?.Invoke(i);
        OnRoundPause += () => IsDragDisable = false;
        ChessboardManager.OnCardRemove.AddListener(OnCardRemove);
        OnGameSet.AddListener(playerWin =>
        {
            if (playerWin) OnChallengerWin();
        });
        NewWarManager.Init(Chessboard);
        UiPool = new ObjectPool<WarGameCardUi>(() => PrefabManager.NewWarGameCardUi(Rack.ScrollRect.content));
    }

    //创建玩家卡牌
    public FightCardData CreateCardToRack(GameCard card)
    {
        var fightCard = new FightCardData(card);
        fightCard.isPlayerCard = true;
        GenerateCardUi(fightCard);
        PlaceCard(fightCard);
        return fightCard;

        void GenerateCardUi(FightCardData fCard)
        {
            var wUi = UiPool.Get();
            wUi.Init(fCard.Card);
            wUi.SetSize(Vector3.one);
            wUi.tag = GameSystem.PyCard;
            wUi.DragComponent.Init(fCard);
            fCard.cardObj = wUi;
        }
    }

    //展示卡牌详细信息
    public void DisplayCardInfo(FightCardData card, bool isShow)
    {
        if (isShow) aboutCardUi.InfoText.text = card.GetInfo().About;
        aboutCardUi.gameObject.SetActive(isShow);
    }
    /// <summary>
    /// 新棋盘布局，不牵涉卡牌架子上的卡牌
    /// </summary>
    /// <param name="enemyBase"></param>
    /// <param name="playerBase"></param>
    /// <param name="enemyCards"></param>
    public void InitNewChessboard(FightCardData enemyBase, FightCardData playerBase, List<ChessCard> enemyCards)
    {
        IsFirstRound = true;
        IsGameOver = false;
        if (AutoRoundSlider) AutoRoundSlider.value = 0;
        ResetGame(true);
        SetPlayerBase(playerBase);
        SetEnemiesIncludeUis(enemyBase, enemyCards);
        GeneratePlayerScopeChessman(false);
        Chessboard.UpdateWarSpeed();
        RefreshPresetFloorBuffs(IsFirstRound);
        Chessboard.gameObject.SetActive(true);
        Background.gameObject.SetActive(true);
    }

    /// <summary>
    /// 初始化新棋局
    /// </summary>
    public void InitNewGame(bool showStartBtn, bool clearRack)
    {
        if (PlayerScope.Any())
        {
            foreach (var card in PlayerScope) RecycleCardUi(card);
            PlayerScope.Clear();
        }

        ResetGame(showStartBtn);
        if (clearRack)
        {
            foreach (var card in CardOnRack) RecycleCardUi(card);
            CardOnRack.Clear();
        }
            
    }

    private void ResetGame(bool showStartBtn)
    {
        ChessboardManager.NewGame();
        NewWarManager.NewGame();
        if(showStartBtn) StartButtonAnim(true, Chessboard.StartButton);
    }

    /// <summary>
    /// 生成玩家卡牌UI
    /// </summary>
    public void GeneratePlayerScopeChessman(bool includeUnlock)
    {
        var scope = includeUnlock ? PlayerScope : PlayerScope.Where(p => p.IsLock);
        foreach (var card in scope)
            SetPlayerChessman(card);
    }
    /// <summary>
    /// 设置玩家老巢
    /// </summary>
    /// <param name="playerBase"></param>
    public void SetPlayerBase(FightCardData playerBase)
    {
        playerBase.isPlayerCard = true;
        if (playerBaseObj != null && playerBaseObj.gameObject) 
            Destroy(playerBaseObj.gameObject);
        NewWarManager.RegCard(playerBase);
        NewWarManager.ConfirmInstancePlayers();
        ChessboardManager.SetPlayerCard(playerBase);
        playerBaseObj = playerBase.cardObj;
    }
    /// <summary>
    /// 设置对手阵容,包括UI
    /// </summary>
    /// <param name="enemyBase"></param>
    /// <param name="enemyCards"></param>
    public void SetEnemiesIncludeUis(FightCardData enemyBase, List<ChessCard> enemyCards)
    {
        if (enemyBaseObj != null && enemyBaseObj.gameObject) 
            Destroy(enemyBaseObj.gameObject);
        //enemyCards.Add(new ChessCard
        //{
        //    Id = -1,
        //    Level = enemyBase.Level,
        //    Pos = 17,
        //    Type = GameCardType.Base
        //});
        var baseCard =new ChessCard
        {
            Id = -1,
            Level = enemyBase.Level,
            Pos = 17,
            Type = GameCardType.Base
        };
        NewWarManager.Enemy = enemyCards.ToArray();
        var confirmedEnemies = NewWarManager.ConfirmInstanceEnemies();
        var eBase = NewWarManager.RegChessCard(baseCard, false, enemyBase.HitPoint);
        ChessboardManager.InstanceChessman(eBase);
        foreach (var card in confirmedEnemies) ChessboardManager.InstanceChessman(card);
        enemyBaseObj = eBase.cardObj; //confirmedEnemies.First(c=>c.Pos == 17).cardObj;
    }

    public void CloseChessboard()
    {
        //关闭所有战斗事件的物件
        if (!Chessboard.gameObject.activeSelf) return;
        Chessboard.gameObject.SetActive(false);
        Background.gameObject.SetActive(false);
        AudioController1.instance.FadeEndMusic();
    }

    //改变游戏速度
    public void ChangeTimeScale(int scale = 0, bool save = true) => Chessboard.ChangeTimeScale(scale, save);

    public void SetPlayerChessman(FightCardData card)
    {
        NewWarManager.RegCard(card);
        ChessboardManager.SetPlayerCard(card);
        var ui = card.cardObj;
        card.UpdateHpUi();
        ui.DragComponent.Init(card);
    }
    public void OnLocalRoundStart()
    {
        IsDragDisable = true;
        autoRoundTimer = 0;
        if (IsBusy) return;
        IsBusy = true;
        StartButtonAnim(false, Chessboard.StartButton);
        //移除地块精灵
        foreach (var pos in PresetMap.Keys.ToArray())
            RemovePreFloorBuff(pos);
        foreach (var card in PlayerScope.Where(c => !c.IsLock))
        {
            RecycleCardUi(card);
            SetPlayerChessman(card);
            card.IsLock = true;
        }
        IsFirstRound = false;
        var round = NewWarManager.ChessOperator.StartRound();
        StartCoroutine(PlayRound(round,true));
    }

    private IEnumerator PlayRound(ChessRound round,bool invokeRoundPauseTrigger)
    {
        yield return ChessboardManager.AnimateRound(round, playRoundStartAudio: true);
        var chess = NewWarManager.ChessOperator;
        if (chess.IsGameOver)
        {
            OnGameSet.Invoke(chess.IsChallengerWin);
            //ClearStates();
            if (chess.IsChallengerWin)
                yield return ChallengerWinAnimation();
            IsGameOver = true;
            IsBusy = false;
            yield break;
        }

        yield return new WaitForSeconds(CardAnimator.instance.Misc.OnRoundEnd);
        IsBusy = false;
        if(invokeRoundPauseTrigger)
        {
            StartButtonAnim(true, Chessboard.StartButton);
            OnRoundPause?.Invoke();
        }
    }

    public IEnumerator ChallengerWinAnimation() => ChessboardManager.ChallengerWinAnimation();

    public const string ButtonTrigger = "isShow";

    public static void StartButtonAnim(bool show,Button startButton)
    {
        startButton.GetComponent<Animator>().SetBool(ButtonTrigger, show);
        startButton.interactable = show;
    }


    #region 棋子暂存区与棋盘区
    public List<FightCardData> CardOnRack { get; } = new List<FightCardData>();

    /**
     * 主要目的：
     * 1.当棋子拖动摆放的时候，记录在暂存区，因为棋子还可以随意提取，在按下开始游戏之前，是不确定的。
     * 2.当棋局结束的时候，将残余的棋子提取以便在新棋局重复使用。(维持当前血量)
     * 3.统计当前玩家手上已上阵的棋子。(死亡忽略)
     */
    public List<FightCardData> PlayerScope { get; } = new List<FightCardData>();
    private void OnCardRemove(FightCardData remCard)
    {
        if (!remCard.IsPlayer ||
            remCard.CardType == GameCardType.Base) return;//老巢不提取棋子
        var card = PlayerScope.FirstOrDefault(f => f == remCard);
        if (card == null) return;
        PlayerScope.Remove(card);
        UpdateHeroEnlistText();
    }

    private void PlayUiAudio(int clipIndex, float delayedTime)
    {
        if (WarMusicController.Instance) WarMusicController.Instance.PlayWarEffect(clipIndex);
    }


    /// <summary>
    /// 根据棋子PosIndex标记更新暂存区
    /// </summary>
    /// <param name="card"></param>
    public void PlaceCard(FightCardData card)
    {
        if (card.Pos <= -1) //回到卡组
            SetToRack(card);
        else 
            SetToBoard(card);

        card.cardObj.SetSelected(card.Pos > -1 && !card.IsLock);
        RefreshPresetFloorBuffs(IsFirstRound);
        UpdateHeroEnlistText();

        void SetToRack(FightCardData fc)
        {
            fc.cardObj.transform.SetParent(Rack.ScrollRect.content);
            fc.cardObj.transform.localPosition = Vector3.zero;
            if (!fc.IsLock) fc.cardObj.DragComponent.SetController(Rack);
            var chessman = PlayerScope.FirstOrDefault(c => c == fc);
            if (chessman != null) PlayerScope.Remove(chessman);
            CardOnRack.Add(fc);
        }

        void SetToBoard(FightCardData fc)
        {
            var chessman = PlayerScope.FirstOrDefault(c => c == fc);
            if (chessman == null) PlayerScope.Add(fc);
            var chessPos = Chessboard.GetChessPos(fc);
            if (!fc.IsLock)
            {
                fc.cardObj.DragComponent.SetController(ChessboardInputControl);
                PlayUiAudio(85, 0);
            }

            if (CardOnRack.FirstOrDefault(c => c == fc) != null)
                CardOnRack.Remove(fc);
            fc.cardObj.transform.SetParent(chessPos.transform);
            fc.cardObj.transform.localPosition = Vector3.zero;
            EffectsPoolingControl.instance.GetSparkEffect(
                Effect.OnChessboardEventEffect(Effect.ChessboardEvent.PlaceCardToBoard), fc.cardObj.transform);
        }
    }


    public void RecycleCardUi(FightCardData card)
    {
        if (card.cardObj == null)
            throw new InvalidOperationException($"卡牌[{card.InstanceId}]({card.GetInfo().Intro})找不到UI!");
        var ui = card.cardObj;
        card.cardObj = null;
        ui.transform.SetParent(Chessboard.transform);
        UiPool.Recycle(ui);
    }

    private Dictionary<ChessPos, (FightCardData, List<Effect.PresetSprite>)> PresetMap { get; } =
        new Dictionary<ChessPos, (FightCardData, List<Effect.PresetSprite>)>();

    /// <summary>
    /// 更新预设特效
    /// </summary>
    /// <param name="includeLocked"></param>
    private void RefreshPresetFloorBuffs(bool includeLocked)
    {
        var cards = ChessboardManager.CardData.Values.Concat(PlayerScope)
            .Where(c => IncludeLock(c.IsLock, includeLocked)).ToArray();
        foreach (var pos in Chessboard.GetScope(true).Concat(Chessboard.GetScope(false)).Where(c => c.Pos != 17))
        {
            var card = cards.FirstOrDefault(c => c.Pos == pos.Pos && c.isPlayerCard == pos.IsChallenger);
            var isMapped = PresetMap.ContainsKey(pos);
            //移除FloorBuff
            if (isMapped && PresetMap[pos].Item1 == card)
            {
                RemovePreFloorBuff(pos);
                continue;
            }

            if (card == null) continue;
            var preSprite = Effect.PresetFloorBuff(card);
            //添加FloorBuff
            if (preSprite == null) continue;
            if (isMapped) continue;
            PresetMap.Add(pos, (null, new List<Effect.PresetSprite>()));
            PresetMap[pos].Item2.Add(preSprite);
            InstanceFloorBuffs(preSprite, pos);
        }
        bool IncludeLock(bool isLock, bool included)
        {
            if (included) return true;
            return !isLock;
        }
    }

    private void RemovePreFloorBuff(ChessPos pos)
    {
        foreach (var floorBuff in PresetMap[pos].Item2.SelectMany(s => s.Sprites))
            EffectsPoolingControl.instance.RecycleEffect(floorBuff);
        PresetMap.Remove(pos);
    }

    private void InstanceFloorBuffs(Effect.PresetSprite preSprite, ChessPos chessPos)
    {
        switch (preSprite.Kind)
        {
            case Effect.PresetKinds.SelfPos:
                InstanceSprite(preSprite.FloorBuffId, chessPos);
                break;
            case Effect.PresetKinds.Surround:
                foreach (var pos in Chessboard.GetNeighborIndexes(chessPos.Pos, 1, false)
                             .Join(Chessboard.GetScope(chessPos.IsChallenger), i => i, p => p.Pos, (_, p) => p))
                    InstanceSprite(preSprite.FloorBuffId, pos);
                break;
            case Effect.PresetKinds.Surround2:
                foreach (var pos in Chessboard.GetNeighborIndexes(chessPos.Pos, 2, false)
                             .Join(Chessboard.GetScope(chessPos.IsChallenger), i => i, p => p.Pos, (_, p) => p))
                    InstanceSprite(preSprite.FloorBuffId, pos);

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        void InstanceSprite(int floorBuffId, ChessPos pos)
        {
            var effect = EffectsPoolingControl.instance.GetFloorBuff(floorBuffId, pos.transform);
            effect.Animator.enabled = false;
            preSprite.Sprites.Add(effect);
        }
    }

    #endregion

    private void OnChallengerWin()
    {
        ResetChessboardPlayerScope();
        IsDragDisable = false;
    }
    private void ResetChessboardPlayerScope()
    {
        foreach (var card in PlayerScope.ToArray())
        {
            if (card.Status.IsDeath)//死亡棋子直接销毁
            {
                PlayerScope.Remove(card);
                continue;
            }
            var ui = card.cardObj; //GenerateCardUi(card);
            var hp = Math.Min((int)(card.Status.Hp + (card.Status.MaxHp * card.Style.Recovery * 0.01f)),
                card.Status.MaxHp);
            card.Status.SetHp(hp);
            //上面把ui与卡分离了。所以更新UI的时候需要手动改血量值
            ui.War.UpdateHpUi(card.Status.HpRate);//这个更新是演示层的ui更新
            ui.gameObject.SetActive(true);
            EffectsPoolingControl.instance.GetSparkEffect(092, ui.transform);
            UpdateHeroEnlistText();
        }
    }


    public void UpdateHeroEnlistText() => heroEnlistText.text =
        string.Format(DataTable.GetStringText(24),
            PlayerScope.Count,
            MaxCards);

    public bool IsPlayerAvailableToPlace(int targetPos, bool isAddIn)
    {
        var isAvailable = Chessboard.IsPlayerScopeAvailable(targetPos);
        if (!isAvailable) return false;
        if (PlayerScope.Any(c => c.Pos == targetPos && targetPos >= 0 && c.IsLock)) return false;
        var balance = MaxCards - PlayerScope.Count;
        if (balance <= 0 && isAddIn) PlayerDataForGame.instance.ShowStringTips(DataTable.GetStringText(38));
        return !isAddIn || balance > 0;
    }

    public void ChangeChessboardBg(Sprite sprite) => Chessboard.Background.sprite = sprite;

    public IEnumerator AnimRounds(List<ChessRound> rounds, bool playRoundStartAudio)
    {
        foreach (var round in rounds)
        {
            yield return ChessboardManager.AnimateRound(round, playRoundStartAudio);
        }
    }

    private void Update()
    {
        if (Chessboard == null) return;
        if (!IsGameOver && !IsBusy && !IsFirstRound && AutoRoundToggle.isActiveAndEnabled && AutoRoundToggle != null && AutoRoundToggle.isOn)
        {
            autoRoundTimer += Time.deltaTime;
            if (autoRoundTimer >= AutoRoundSecs)
            {
                OnLocalRoundStart();
            }
            if (AutoRoundSlider)
                AutoRoundSlider.value = 1 - autoRoundTimer / AutoRoundSecs;
        }
    }

    public void SetCustomInstanceCardToBoard(int pos, GameCard c, bool isChallenger, int instanceId)
    {
        var card = new FightCardData(GameCard.Instance(c.CardId, c.Type, c.Level));
        card.SetPos(pos);
        card.SetInstanceId(instanceId);
        card.isPlayerCard = isChallenger;
        ChessboardManager.InstanceChessman(card);
    }
}
public class GameSetEvent : UnityEvent<bool> { }