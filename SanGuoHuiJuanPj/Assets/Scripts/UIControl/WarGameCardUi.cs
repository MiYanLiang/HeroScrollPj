using CorrelateLib;
using UnityEngine;
using UnityEngine.UI;

public class WarGameCardUi : GameCardUiBase,IPoolObject
{
    public GameCardWarUiOperation War;
    public DragController DragComponent;

    public override void Init(GameCard card)
    {
        base.Init(card);
        gameObject.SetActive(true);
        War.Init(DragComponent);
        War.Show(this);
    }

    public void SetSelected(bool selected) => SetAnimateGameObject(War.Selected, selected);

    public void SetHighLight(bool on) => SetAnimateGameObject(War.Highlight, on);

    public void SetLose(bool isLose) => SetAnimateGameObject(War.Lose,isLose);

    private void SetAnimateGameObject(Component obj,bool isActive)
    {
        if (isActive) obj.gameObject.SetActive(false);//如果物件是开着状态，先关掉再开启以触发播放
        obj.gameObject.SetActive(isActive);
    }

    //public void DragDisable() => DragComponent?.Disable();
    public void ObjReset()
    {
        SetSelected(false);
        SetLose(false);
        Display(true);
        ResetUi();
        gameObject.SetActive(false);
    }
}

public class GameCardUiBase : MonoBehaviour
{
    public Image Image;
    public Text Name;
    public Image Level;
    public TextImageUi Short;
    public Image Frame;
    public GrayScale GrayScale;
    public GameCardInfo CardInfo { get; private set; }
    public GameCard Card { get; private set; }

    public virtual void Init(GameCard card)
    {
        Card = card;
        CardInfo = card.GetInfo();
        Image.sprite = GameResources.Instance.GetCardImage(card);
        Set();
        SetName(CardInfo);
        SetLevel(Card.Level);
        ShowFrame(false);
    }
    
    public void Display(bool isShow)
    {
        Image.enabled = isShow;
        Name.enabled = isShow;
        Level.enabled = isShow;
        Frame.enabled = isShow;
        if(isShow)
            Short.Show();
        else Short.Off();
    }

    public void ResetUi()
    {
        Card = null;
        CardInfo = null;
    }

    protected void Set()
    {
        Image.sprite = CardInfo.Type == GameCardType.Hero
            ? GameResources.Instance.HeroImg[Card.CardId]
            : GameResources.Instance.FuZhuImg[CardInfo.ImageId];
        Short.Set(CardInfo.Short, GameResources.Instance.ClassImg[CardInfo.Type == GameCardType.Hero ? 0 : 1]);
    }

    public void SetSize(Vector3 size) => transform.localScale = size;
    public void ShowFrame(bool on) => Frame.gameObject.SetActive(on);
    public void SetName(GameCardInfo info)
    {
        NameTextSizeAlignment(Name, info.Name);
        Name.color = info.GetNameColor();
    }

    public void SetLevel(int level) => Level.sprite = GameResources.Instance.GradeImg[level];

    /// <summary> 
    /// 名字显示规则 
    /// </summary> 
    /// <param name="nameText"></param> 
    /// <param name="str"></param> 
    public static void NameTextSizeAlignment(Text nameText, string str)
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

    public void SetGray(bool isGray) => GrayScale.SetGray(isGray ? 1 : 0);
}
