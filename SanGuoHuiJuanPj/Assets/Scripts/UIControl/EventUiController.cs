using UnityEngine;

public class EventUiController : MonoBehaviour
{
    public static bool IsInit { get; set; }

    void Start()
    {
        gameObject.SetActive(!IsInit);
        IsInit = true;
    }
}