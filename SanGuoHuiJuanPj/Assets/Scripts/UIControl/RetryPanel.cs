using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class RetryPanel : MonoBehaviour
{
    private static RetryPanel _instance;
    [SerializeField]private Button _retryBtn;
    [SerializeField]private Button _cancelBtn;

    public void Init()
    {
        _instance = this;
        Display(false);
    }

    public static async UniTask<bool> StartRetryAsync()
    {
        var retry = false;
        var waiting = true;
        ResetUi();
        _instance._retryBtn.onClick.AddListener(() => SetRetry(true));
        _instance._cancelBtn.onClick.AddListener(() => SetRetry(false));
        Display(true);
        await UniTask.WaitWhile(() => waiting);
        return retry;

        void SetRetry(bool value)
        {
            retry = value;
            waiting = false;
        }
    }

    public static void ResetUi()
    {
        _instance._retryBtn.onClick.RemoveAllListeners();
        _instance._cancelBtn.onClick.RemoveAllListeners();
        Display(false);
    }

    private static void Display(bool display) => _instance.gameObject.SetActive(display);
}