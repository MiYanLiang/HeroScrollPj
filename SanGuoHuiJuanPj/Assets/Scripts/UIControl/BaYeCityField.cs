using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 霸业地图Ui字段类，用于标记字段
/// </summary>
public class BaYeCityField : MonoBehaviour
{
    public int id = -1;
    //public bool IsLevelAvailable;
    public List<int> boundWars = new List<int>();
    public Button button;
}