using System;
using System.Collections.Generic;
using System.Linq;
using Assets.System.WarModule;
using UnityEngine;
using UnityEngine.UI;

public class Chessboard : MonoBehaviour
{
    public Image[] GridImages;
    [SerializeField] private Button SpeedBtn;
    [SerializeField] private Text SpeedText;
    public Image Background;
    public ChessPos[] PlayerScope;
    public ChessPos[] EnemyScope;
    public Button StartButton;
    public Transform EffectTransform;
    public Animator RouseAnim;
    public Image ShadyImage;
    public Animator WinFire;
    public Animator WinExplode;
    public Animator WinText;
    public Toggle AutoRoundToggle;
    public Slider AutoRoundSlider;

    public IReadOnlyDictionary<int, FightCardData> Data => data;

    private ChessGrid grid;
    private Dictionary<int, FightCardData> data;

    public int PlayerCardsOnBoard => PlayerScope.Count(p => p.Card != null && p.Card.cardType != 522);
    public int EnemyCardsOnBoard => EnemyScope.Count(p => p.Card != null && p.Card.cardType != 522);

    public void Init()
    {
        for (var i = 0; i < PlayerScope.Length; i++) PlayerScope[i].Init(i, true);
        for (var i = 0; i < EnemyScope.Length; i++) EnemyScope[i].Init(i, false);
        grid = new ChessGrid(PlayerScope.Cast<IChessPos>().ToArray(), EnemyScope.Cast<IChessPos>().ToArray());
        data = new Dictionary<int, FightCardData>();
        SpeedBtn.onClick.RemoveAllListeners();
        SpeedBtn.onClick.AddListener(() => ChangeTimeScale(0, true));
    }

    public void ResetChessboard()
    {
        foreach (var chessPos in PlayerScope.Concat(EnemyScope)) chessPos.ResetPos();
        data.Clear();
    }
    /// <summary>
    /// 棋子控件置高，避免被其它UI挡到
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="isPlayer"></param>
    public void OnActivityBeginTransformSibling(int pos, bool isPlayer)
    {
        var card = GetChessPos(pos, isPlayer).Card;
        var trans = card.cardObj.transform;
        trans.SetParent(transform,true);
        trans.SetAsLastSibling();
    }

    public void ResetPos(FightCardData card)
    {
        //注意这里是获取状态里的位置而不是原始位置。
        PlaceCard(card.Pos , card);
    }

    public ChessPos[] GetScope(bool isPlayer) => isPlayer ? PlayerScope : EnemyScope;
    public ChessPos GetChessPos(int index, bool isPlayer) => GetScope(isPlayer)[index];
    public ChessPos GetChessPos(FightCardData card) => GetScope(card.IsPlayer)[card.Pos];

    public void PlaceCard(int index, FightCardData card)
    {
        try
        {
            if (!data.ContainsKey(card.InstanceId))
                data.Add(card.InstanceId, card);
            var scope = GetScope(card.isPlayerCard);
            var pos = scope.FirstOrDefault(p => p.Card == card);
            if (pos != null) RemoveCard(pos.Pos, card.IsPlayer); //移除卡牌前位置
            if (scope[index].Card != null) RemoveCard(index, card.isPlayerCard); //移除目标位置上的卡牌
            scope[index].PlaceCard(card, true);
            card.SetPos(index);
            card.cardObj.transform.SetAsFirstSibling();
        }
        catch (IndexOutOfRangeException e)
        {
            Debug.LogError($"注意棋格Index问题，检查是否有棋子重叠。:{e}");
        }
    }

    public FightCardData RemoveCard(int index, bool isPlayer)
    {
        var scope = GetScope(isPlayer);
        if (scope[index].Card == null)
            throw XDebug.Throw<Chessboard>($"位置[{index}] 没有卡牌！");
        var card = scope[index].Card;
        scope[index].RemoveCard();
        card.SetPos(-1);
        return card;
    }

    public bool IsPlayerScopeAvailable(int index) =>
        index != 17 &&
        (PlayerScope[index].Card == null ||
         PlayerScope[index].Card.Status.IsDeath);

    public void ClearEnemyCards()
    {
        for (var i = 0; i < EnemyScope.Length; i++)
        {
            if (EnemyScope[i].Card != null)
            {
                var card = RemoveCard(i, false);
                Destroy(card.cardObj.gameObject);
            }
        }
    }

    public void DestroyCard(FightCardData card)
    {
        var scope = GetScope(card.isPlayerCard);
        if (card.PosIndex >= 0)
        {
            var pos = scope[card.PosIndex];
            pos.RemoveCard();
        }

        if (card.cardObj != null && card.cardObj.gameObject)
            Destroy(card.cardObj.gameObject);
    }

    public int[] GetNeighborIndexes(int pos, int round = 1, bool includeHome = true) => includeHome
        ? grid.GetNeighborIndexes(pos, round)
        : grid.GetNeighborIndexes(pos, round).Where(i => i != 17).ToArray();

    public void ChangeTimeScale(int scale, bool save)
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
        SpeedText.text = Multiply + warScale;
    }
    private const string Multiply = "×";
    public void UpdateWarSpeed()
    {
        //调整游戏速度
        var speed = GamePref.PrefWarSpeed;
        Time.timeScale = speed;
        SpeedText.text = Multiply + speed;
    }
}
