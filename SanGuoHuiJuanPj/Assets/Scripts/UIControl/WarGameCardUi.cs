using UnityEngine;
using UnityEngine.UI;

public class WarGameCardUi : GameCardUiBase
{
    public GameCardWarUiOperation War;
    public DragController DragComponent;

    public void SetSelected(bool selected) => War.Selected.gameObject.SetActive(selected);
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
        SetName(CardInfo);
        SetLevel(Card.Level);
        ShowFrame(false);
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
