using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class RetryPanel : MonoBehaviour
{
    private static RetryPanel _instance;
    [SerializeField]private Button _retryBtn;
    [SerializeField]private Button _cancelBtn;
    [SerializeField] private Text _errorText;
    [SerializeField] private Text _presetText;


    public void Init()
    {
        _instance = this;
        Display(false);
    }

    public static async UniTask<bool> AwaitRetryAsync(string methodName)
    {
        _instance._errorText.text = $"调用{methodName}失败, 是否重试?";
        var retry = false;
        var waiting = true;
        _instance._retryBtn.onClick.AddListener(() => SetRetry(true));
        _instance._cancelBtn.onClick.AddListener(() => SetRetry(false));
        Display(true);
        await UniTask.WaitWhile(() => waiting);
        return retry;

        void SetRetry(bool value)
        {
            _instance._errorText.text = string.Empty;
            _instance._retryBtn.onClick.RemoveAllListeners();
            _instance._cancelBtn.onClick.RemoveAllListeners();
            retry = value;
            waiting = false;
            Display(false);
        }
    }

    private static void Display(bool display) => _instance.gameObject.SetActive(display);

    public static void PresetMessage(string message)
    {
        _instance._presetText.text = $"上个报错:\n{message}";
    }
}