using CorrelateLib;
using UnityEngine;
using UnityEngine.UI;

public class GuanQiaUi:MonoBehaviour
{
    public Image Image;
    public Button Button;
    public Text Title;
    public FlagUi Flag;
    public Image SelectedImg;

    public void Set(Vector3 scale, GameStage stage, bool isBattle)
    {
        transform.localScale = scale;
        var checkPoint = stage.Checkpoint;
        var eventType = checkPoint.EventType;
        string flagTitle = string.Empty;
        Sprite img = null;
        if (isBattle)
        {
            var battle = stage.BattleEvent;
            var index = stage.RandomId;
            int selectedEnemy = battle.EnemyTableIndexes[index];
            var flagId = -1;
            if (battle.IsStaticEnemies > 0)
            {
                flagId = DataTable.StaticArrangement[selectedEnemy].Flag;
                flagTitle = DataTable.StaticArrangement[selectedEnemy].FlagTitle;

            }
            else
            {
                flagId = DataTable.Enemy[selectedEnemy].Flag;
                flagTitle = DataTable.Enemy[selectedEnemy].FlagTitle;
            }

            SetTitle(checkPoint.Title);
            Button.gameObject.SetActive(true);
            if (eventType != 7) img = GameResources.Instance.CityFlag[flagId];
        }

        SetFlag(img, flagTitle);
        SetIcon(GameResources.Instance.GuanQiaEventImg[checkPoint.ImageId]);
    }

    private void SetIcon(Sprite img)
    {
        Image.gameObject.SetActive(true);
        Image.sprite = img;
    }
    private void SetTitle(string title)
    {
        Title.gameObject.SetActive(true);
        Title.text = title;
    }

    private void SetFlag(Sprite img, string title)
    {
        Flag.gameObject.SetActive(!string.IsNullOrWhiteSpace(title));
        if (!Flag.gameObject.activeSelf) return;
        Flag.Image.sprite = img;
        Flag.Short.text = title;
        Flag.Short.fontSize = title.Length > 2 ? 45 : 50;
    }
}