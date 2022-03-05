using System;
using UnityEngine;

public class EventUiController : MonoBehaviour
{
    public static bool ShowOnce { get; set; }
    [SerializeField] UDate ExpiredDate;

    public void Init()
    {
        gameObject.SetActive(!ShowOnce && !ExpiredDate.IsExpired());
        ShowOnce = true;
    }

    [Serializable]
    private class UDate
    {
        public int Year;
        public int Month;
        public int Day;

        public bool IsExpired()
        {
            var date = new DateTime(Year, Month, Day);
            return DateTime.Now > date;
        }
    }
}