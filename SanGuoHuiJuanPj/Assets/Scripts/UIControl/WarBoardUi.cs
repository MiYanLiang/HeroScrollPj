using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.System.WarModule;
using CorrelateLib;
using UnityEngine;
using UnityEngine.UI;

public class WarBoardUi : MonoBehaviour
{
    /// <summary>
    /// 当前武将卡牌上阵最大数量
    /// </summary>
    public int MaxCards;
    public Chessboard Chessboard;
    public JiBanAnimationManager JiBanManager;
    [SerializeField] private Image Background;
    [SerializeField] private PlayerCardRack Rack;
    [SerializeField] private ChessboardInputController ChessboardInputControl;
    [SerializeField] private Button speedBtn;
    [SerializeField] private Text speedBtnText;
    [SerializeField] private ChessboardVisualizeManager ChessboardManager;
    [SerializeField] private Text heroEnlistText; //武将上阵文本
    [SerializeField] AboutCardUi aboutCardUi; //阵上卡牌详情展示位
    private WarGameCardUi playerBaseObj { get; set; }
    private WarGameCardUi enemyBaseObj { get; set; }
    public bool IsDragDisable { get; private set; }
    private const string Multiply = "×";

    private ObjectPool<WarGameCardUi> UiPool { get; set; }

    public void Init()
    {
        Rack.Init(this);
        ChessboardInputControl.Init(this);
        ChessboardManager.Init(Chessboard, JiBanManager);
        ChessboardManager.OnRoundBegin += OnRoundStart;
        ChessboardManager.OnRoundPause += () => IsDragDisable = false;
        ChessboardManager.OnCardRemove.AddListener(OnCardRemove);
        ChessboardManager.OnGameSet.AddListener(playerWin =>
        {
            if (playerWin) OnChallengerWin();
        });
        speedBtn.onClick.AddListener(() => ChangeTimeScale());
        UiPool = new ObjectPool<WarGameCardUi>(() => PrefabManager.NewWarGameCardUi(Rack.ScrollRect.content));
    }

    //创建玩家卡牌
    public void CreateCardToRack(GameCard card)
    {
        var fightCard = new FightCardData(card);
        fightCard.isPlayerCard = true;
        GenerateCardUi(fightCard);
        PlaceCard(fightCard);

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

    public void StartNewGame(FightCardData enemyBase, FightCardData playerBase, List<ChessCard> enemyCards)
    {
        NewGame();
        SetPlayerBase(playerBase);
        SetEnemies(enemyBase, enemyCards);
        GeneratePlayerScopeChessman();
        UpdateGameSpeed();
        OnPreChessboardFloorBuff(ChessboardManager.IsFirstRound);
        Chessboard.gameObject.SetActive(true);
        Background.gameObject.SetActive(true);
    }

    public void NewGame() => ChessboardManager.NewGame();

    public void UpdateGameSpeed()
    {
        //调整游戏速度
        var speed = GamePref.PrefWarSpeed;
        Time.timeScale = speed;
        speedBtnText.text = Multiply + speed;
    }
    public void GeneratePlayerScopeChessman()
    {
        foreach (var card in PlayerScope.Where(p => p.IsLock))
            ChessmanInit(card);
    }

    public void SetPlayerBase(FightCardData playerBase)
    {
        playerBase.isPlayerCard = true;
        if (playerBaseObj != null && playerBaseObj.gameObject) Destroy(playerBaseObj.gameObject);
        ChessboardManager.SetPlayerBase(playerBase);
        ChessboardManager.InstanceChessman(playerBase);
        playerBaseObj = playerBase.cardObj;
    }

    public void SetEnemies(FightCardData enemyBase, List<ChessCard> enemyCards)
    {
        if (enemyBaseObj != null && enemyBaseObj.gameObject) Destroy(enemyBaseObj.gameObject);
        foreach (var card in ChessboardManager.SetEnemyChess(enemyBase, enemyCards.ToArray()))
            ChessboardManager.InstanceChessman(card);
        enemyBaseObj = enemyBase.cardObj;
    }

    public void CloseChessboard()
    {
        //关闭所有战斗事件的物件
        if (!Chessboard.gameObject.activeSelf) return;
        Chessboard.gameObject.SetActive(false);
        Background.gameObject.SetActive(false);
        AudioController1.instance.ChangeBackMusic();
    }

    //改变游戏速度
    public void ChangeTimeScale(int scale = 0, bool save = true)
    {
        var warScale = GamePref.PrefWarSpeed;
        if (scale <= 0)
        {
            warScale *= 2;
            if (warScale > 2)
                warScale = 1;
        }
        else warScale = scale;
        if (save) GamePref.SetPrefWarSpeed(warScale);
        Time.timeScale = warScale;
        speedBtnText.text = Multiply + warScale;
    }



    private void ChessmanInit(FightCardData card)
    {
        ChessboardManager.SetPlayerChess(card);
        ChessboardManager.InstanceChessman(card);
        var ui = card.cardObj;
        card.UpdateHpUi();
        ui.DragComponent.Init(card);
    }
    private bool OnRoundStart()
    {
        IsDragDisable = true;
        //移除地块精灵
        foreach (var pos in PresetMap.Keys.ToArray())
            RemovePreFloorBuff(pos);
        foreach (var card in PlayerScope.Where(c => !c.IsLock))
        {
            RecycleCardUi(card);
            ChessmanInit(card);
            card.IsLock = true;
        }

        return true;
    }
    #region 棋子暂存区与棋盘区

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
        if (card == null)
            throw new InvalidOperationException($"找不到战败的卡牌，Id = {remCard.InstanceId}");
        PlayerScope.Remove(card);
        UpdateHeroEnlistText();
    }

    private void PlayUiAudio(int clipIndex, float delayedTime)
    {
        if(WarsUIManager.instance) WarsUIManager.instance.PlayAudioForSecondClip(clipIndex, delayedTime);
    }


    /// <summary>
    /// 根据棋子PosIndex标记更新暂存区
    /// </summary>
    /// <param name="card"></param>
    public void PlaceCard(FightCardData card)
    {
        var chessman = PlayerScope.FirstOrDefault(c => c == card);
        if (card.Pos <= -1)//回到卡组
        {
            card.cardObj.transform.SetParent(Rack.ScrollRect.content);
            card.cardObj.transform.localPosition = Vector3.zero;
            if (!card.IsLock) card.cardObj.DragComponent.SetController(Rack);
            if (chessman != null) PlayerScope.Remove(chessman);

        }
        else
        {
            if (chessman == null) PlayerScope.Add(card);
            var chessPos = Chessboard.GetChessPos(card);
            if (!card.IsLock)
            {
                card.cardObj.DragComponent.SetController(ChessboardInputControl);
                PlayUiAudio(85, 0);
            }
            card.cardObj.transform.SetParent(chessPos.transform);
            card.cardObj.transform.localPosition = Vector3.zero;
            EffectsPoolingControl.instance.GetSparkEffect(
                Effect.OnChessboardEventEffect(Effect.ChessboardEvent.PlaceCardToBoard), card.cardObj.transform);
        }

        card.cardObj.SetSelected(card.Pos > -1 && !card.IsLock);
        OnPreChessboardFloorBuff(ChessboardManager.IsFirstRound);
        UpdateHeroEnlistText();
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

    private void OnPreChessboardFloorBuff(bool includeLocked)
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

    public void PlayResult(List<ChessRound> rounds)
    {
        StartCoroutine(AnimRounds(rounds));
    }

    private IEnumerator AnimRounds(List<ChessRound> rounds)
    {
        foreach (var round in rounds)
            yield return ChessboardManager.AnimateRound(round);
    }
}