using UnityEngine;
using UnityEngine.UI;

public class BaYeWarStoryUi : BaYeStoryUi
{
    [SerializeField] private Text LevelText;
    [SerializeField] private Text LevelValue;

    public void Set(int level, Color color)
    {
        if(LevelValue)
        {
            LevelValue.color = color;
            LevelValue.text = level.ToString();
        }
        if(LevelText) LevelText.color = color;
    }
}