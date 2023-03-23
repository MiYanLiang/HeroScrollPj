using System;
using System.Collections;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PreLoadController : MonoBehaviour
{
    public SplashImage[] SplashImages;
    //public Image DisplayImage;
    public Image FadeImage;
    public GameSystem GameSystem;
    public GameObject CoroutineObj;
    void Start()
    {
        //DisplayImage.gameObject.SetActive(false);
        FadeImage.gameObject.SetActive(true);
        StartCoroutine(Initialization());
    }

    private IEnumerator Initialization()
    {
        var spList = SplashImages.ToList();
        //AsyncOperation ao = null;
        //
        //ao = SceneManager.LoadSceneAsync((int) GameSystem.GameScene.StartScene, LoadSceneMode.Additive);
        //ao.allowSceneActivation = false;

        var co1 = CoroutineObj.AddComponent<CoObj>();
        co1.Set(GameSystem.Init);
        co1.StartAction();
        yield return new WaitForSeconds(0.1f);
        foreach (var sp in spList)
        {
            sp.Image.gameObject.SetActive(true);
            FadeImage.DOFade(0, sp.Duration);
            yield return new WaitForSeconds(sp.Duration);
            FadeImage.DOFade(1, sp.Duration);
            yield return new WaitForSeconds(sp.Duration);
            sp.Image.gameObject.SetActive(false);
        }

        yield return new WaitUntil(() => GameSystem.IsInit);
        //ao.allowSceneActivation = true;
        //SceneManager.UnloadSceneAsync(0);
        SceneManager.LoadScene((int) GameSystem.GameScene.StartScene);
    }
}

[Serializable]
public class SplashImage
{
    public Image Image;
    [Range(1,5)] public float Duration = 2;
}