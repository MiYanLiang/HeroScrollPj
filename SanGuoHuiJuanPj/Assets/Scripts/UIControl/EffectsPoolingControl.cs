using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectsPoolingControl : MonoBehaviour
{
    public static EffectsPoolingControl instance;

    private int maxCount = 3;   //最大展示数目

    [SerializeField]
    Transform effectContentTran;

    [SerializeField]
    string[] effectsNameStr;    //技能特效名

    [SerializeField]
    string[] iconNameStr;    //状态特效名

    List<List<GameObject>> effectsPoolingList = new List<List<GameObject>>();   //技能特效池

    List<List<GameObject>> iconPoolingList = new List<List<GameObject>>();   //状态特效池

    private GameResources GameResources => GameResources.Instance;
    public bool IsInit { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void Init()
    {
        if(IsInit)return;
        InitializedEffectsObj();
        InitializedIconsObj();
        IsInit = true;
    }

    private void InitializedEffectsObj()
    {
        for (int i = 0; i < effectsNameStr.Length; i++)
        {
            List<GameObject> effectList = new List<GameObject>();
            for (int j = 0; j < maxCount; j++)
            {
                GameObject effectObj = Instantiate(GameResources.Effects[effectsNameStr[i]], effectContentTran);
                effectObj.SetActive(false);
                effectList.Add(effectObj);
            }
            effectsPoolingList.Add(effectList);
        }
    }

    private void InitializedIconsObj()
    {
        for (int i = 0; i < iconNameStr.Length; i++)
        {
            //Debug.Log("i = :" + i);
            List<GameObject> iconList = new List<GameObject>();
            for (int j = 0; j < maxCount; j++)
            {
                GameObject iconObj = Instantiate(GameResources.StateDin[iconNameStr[i]], effectContentTran);
                iconObj.SetActive(false);
                iconList.Add(iconObj);
            }
            iconPoolingList.Add(iconList);
        }
    }

    /// <summary>
    /// 获取技能特效,跟随卡牌动
    /// </summary>
    /// <param name="effectName">特效名</param>
    /// <param name="takeBackTime">回收时间</param>
    /// <param name="usedTran">使用者</param>
    /// <returns></returns>
    public GameObject GetEffectToFight(string effectName, float takeBackTime, Transform usedTran)
    {
        int index = -1;
        for (int i = 0; i < effectsNameStr.Length; i++)
        {
            if (effectsNameStr[i] == effectName)
            {
                index = i;
                break;
            }
        }
        if (index != -1)
        {
            foreach (GameObject go in effectsPoolingList[index])
            {
                if (go == null)
                    continue;
                if (!go.activeSelf)
                {
                    go.transform.position = usedTran.position;
                    go.transform.SetParent(usedTran);
                    go.SetActive(true);
                    //if (go.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips[0]!=null)
                    //    takeBackTime = go.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips[0].length;
                    StartCoroutine(TakeBackEffect(go, takeBackTime));
                    return go;
                }
            }

            GameObject effectObj = Instantiate(GameResources.Effects[effectName], usedTran);
            effectObj.transform.position = usedTran.position;
            effectObj.SetActive(true);
            effectsPoolingList[index].Add(effectObj);
            //if (effectObj.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips[0] != null)
            //    takeBackTime = effectObj.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips[0].length;
            StartCoroutine(TakeBackEffect(effectObj, takeBackTime));
            return effectObj;
        }
        else
        {
            return null;
        }
    }

    public GameObject GetEffectToFight(string effectName, float takeBackTime, WarGameCardUi ui)
    {
        int index = -1;
        for (int i = 0; i < effectsNameStr.Length; i++)
        {
            if (effectsNameStr[i] == effectName)
            {
                index = i;
                break;
            }
        }
        if (index != -1)
        {
            foreach (GameObject go in effectsPoolingList[index])
            {
                if (go == null)
                    continue;
                if (!go.activeSelf)
                {
                    go.transform.position = ui.transform.position;
                    go.transform.SetParent(ui.transform);
                    go.SetActive(true);
                    //if (go.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips[0]!=null)
                    //    takeBackTime = go.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips[0].length;
                    StartCoroutine(TakeBackEffect(go, takeBackTime));
                    return go;
                }
            }

            GameObject effectObj = Instantiate(GameResources.Effects[effectName], ui.transform);
            effectObj.transform.position = ui.transform.position;
            effectObj.SetActive(true);
            effectsPoolingList[index].Add(effectObj);
            //if (effectObj.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips[0] != null)
            //    takeBackTime = effectObj.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips[0].length;
            StartCoroutine(TakeBackEffect(effectObj, takeBackTime));
            return effectObj;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 获取技能特效,不跟随卡牌位置
    /// </summary>
    /// <param name="effectName"></param>
    /// <param name="tekeBackTime"></param>
    /// <param name="usedTran"></param>
    /// <returns></returns>
    public GameObject GetEffectToFight1(string effectName, float tekeBackTime, Transform usedTran)
    {
        int index = -1;
        for (int i = 0; i < effectsNameStr.Length; i++)
        {
            if (effectsNameStr[i] == effectName)
            {
                index = i;
                break;
            }
        }
        if (index != -1)
        {
            foreach (GameObject go in effectsPoolingList[index])
            {
                if (go == null)
                    continue;
                if (!go.activeSelf)
                {
                    go.transform.position = usedTran.position;
                    //go.transform.SetParent(usedTran);
                    go.SetActive(true);
                    //if (go.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips[0] != null)
                    //    tekeBackTime = go.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips[0].length;
                    StartCoroutine(TakeBackEffect(go, tekeBackTime));
                    return go;
                }
            }

            GameObject effectObj = Instantiate(GameResources.Effects[effectName], effectContentTran);
            effectObj.transform.position = usedTran.position;
            effectObj.SetActive(true);
            effectsPoolingList[index].Add(effectObj);
            //if (effectObj.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips[0] != null)
            //    tekeBackTime = effectObj.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips[0].length;
            StartCoroutine(TakeBackEffect(effectObj, tekeBackTime));
            return effectObj;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 获取状态特效图标,跟随卡牌动
    /// </summary>
    public GameObject GetStateIconToFight(string iconName, Transform usedTran)
    {
        int index = -1;
        for (int i = 0; i < iconNameStr.Length; i++)
        {
            if (iconNameStr[i] == iconName)
            {
                index = i;
                break;
            }
        }
        if (index != -1)
        {
            foreach (GameObject go in iconPoolingList[index])
            {
                if (go == null)
                    continue;
                if (!go.activeSelf)
                {
                    go.transform.position = usedTran.position;
                    go.transform.SetParent(usedTran);
                    go.SetActive(true);
                    return go;
                }
            }

            GameObject iconObj = Instantiate(GameResources.StateDin[iconName], usedTran);
            iconObj.transform.position = usedTran.position;
            iconObj.SetActive(true);
            iconPoolingList[index].Add(iconObj);
            return iconObj;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 回收状态图标特效
    /// </summary>
    public void TakeBackStateIcon(GameObject go)
    {
        go.transform.SetParent(effectContentTran);
        go.SetActive(false);
    }

    //回收特效
    IEnumerator TakeBackEffect(GameObject go, float takeBackTime)
    {
        yield return new WaitForSeconds(takeBackTime);
        if (go != null)
        {
            if (go.transform.localScale.x != 1)
            {
                go.transform.localScale = new Vector3(1, 1, 1);
            }
            go.transform.SetParent(effectContentTran);
            go.SetActive(false);
        }
    }
}