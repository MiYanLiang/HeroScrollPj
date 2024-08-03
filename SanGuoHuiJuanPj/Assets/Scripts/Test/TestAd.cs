using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class TestAd : MonoBehaviour
{
    public Text reportText;
    public Text stateText;
    //public UnityAdController controller;
    //
    //
    //void Start()
    //{
    //    controller.Init();
    //}
    //
    //public void Load() => controller.RequestLoad((b, m) => TextDisplay(b, m));
    //
    //public void Show() => controller.RequestShow((b, m) => TextDisplay(b, m));
    //
    //private void TextDisplay(bool isSuccess, string msg, [CallerMemberName] string method = null) => reportText.text = $"{method}: isSuccess= {isSuccess}, msg:{msg}";
    //
    //void Update()
    //{
    //    stateText.text = controller.Status.ToString();
    //}
}
