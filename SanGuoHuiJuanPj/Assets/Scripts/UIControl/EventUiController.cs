using System;
using UnityEngine;

public class EventUiController : MonoBehaviour
{
    public static bool IsInit { get; set; }
    [SerializeField] UDate ExpiredDate;

    void Start()
    {
        gameObject.SetActive(!IsInit);
        IsInit = true;
        gameObject.SetActive(!ExpiredDate.IsExpired());
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
            return date < DateTime.Now;
        }
    }
}