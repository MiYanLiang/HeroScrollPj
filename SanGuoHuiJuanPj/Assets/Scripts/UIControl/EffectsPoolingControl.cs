using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.System.WarModule;
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

    List<List<EffectStateUi>> iconPoolingList = new List<List<EffectStateUi>>();   //状态特效池

    private Dictionary<int, List<GameObject>> EffectPool;
    private Dictionary<int, List<EffectStateUi>> BuffPool;
    private Dictionary<int, List<EffectStateUi>> FloorBuffPool;
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
        EffectPool = new Dictionary<int, List<GameObject>>();
        BuffPool = new Dictionary<int, List<EffectStateUi>>();
        FloorBuffPool = new Dictionary<int, List<EffectStateUi>>();
        IsInit = true;
    }

    private void InitializedEffectsObj()
    {
        for (int i = 0; i < effectsNameStr.Length; i++)
        {
            List<GameObject> effectList = new List<GameObject>();
            for (int j = 0; j < maxCount; j++)
            {
                GameObject effectObj = Instantiate(GameResources.Instance.Effects[effectsNameStr[i]], effectContentTran);
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
            var iconList = new List<EffectStateUi>();
            for (int j = 0; j < maxCount; j++)
            {
                var iconObj = Instantiate(GameResources.Instance.StateDin[iconNameStr[i]], effectContentTran);
                iconObj.gameObject.SetActive(false);
                iconList.Add(iconObj);
            }
            iconPoolingList.Add(iconList);
        }
    }

    public GameObject GetEffectToFight(string effectName, float takeBackTime, WarGameCardUi ui) =>
        GetEffectToFight(effectName, takeBackTime, ui.transform);
    public GameObject GetEffectToFight(string effectName, float takeBackTime, Transform trans)
    {
        var index = -1;
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
                    go.transform.localPosition = Vector3.zero;
                    go.transform.position = trans.position;
                    go.transform.SetParent(trans);
                    go.SetActive(true);
                    StartCoroutine(TakeBackEffect(go, takeBackTime));
                    return go;
                }
            }

            GameObject effectObj = Instantiate(GameResources.Instance.Effects[effectName], trans);
            effectObj.transform.position = trans.position;
            effectObj.SetActive(true);
            effectsPoolingList[index].Add(effectObj);
            StartCoroutine(TakeBackEffect(effectObj, takeBackTime));
            return effectObj;
        }

        return null;
    }

    public GameObject GetSparkEffect(int sparkId, Transform targetTransform, float lasting = 1.5f)
    {
        GameObject effect = null;
        if (!EffectPool.ContainsKey(sparkId))
            EffectPool.Add(sparkId, new List<GameObject>());
        
        effect = EffectPool[sparkId].FirstOrDefault(e => !e.activeSelf);
        if (effect == null)
        {
            effect = Instantiate(GameResources.Instance.Spark[sparkId]);
            EffectPool[sparkId].Add(effect);
        }

        effect.transform.localPosition = Vector3.zero;
        effect.transform.position = targetTransform.position;
        effect.transform.SetParent(targetTransform);
        effect.SetActive(true);
        if (lasting > 0) StartCoroutine(RecycleSpark(effect, lasting));
        return effect;
    }

    public IEnumerator RecycleSpark(GameObject effect, float lasting)
    {
        yield return new WaitForSeconds(lasting);
        RecycleEffect(effect);
    }

    private void RecycleEffect(GameObject effect)
    {
        if (effect != null)
        {
            effect.transform.localScale = Vector3.one;
            effect.transform.localRotation = Quaternion.identity;
            effect.SetActive(false);
            effect.transform.SetParent(effectContentTran);
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

            GameObject effectObj = Instantiate(GameResources.Instance.Effects[effectName], effectContentTran);
            effectObj.transform.position = usedTran.position;
            effectObj.SetActive(true);
            effectsPoolingList[index].Add(effectObj);
            //if (effectObj.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips[0] != null)
            //    tekeBackTime = effectObj.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips[0].length;
            StartCoroutine(TakeBackEffect(effectObj, tekeBackTime));
            return effectObj;
        }

        return null;
    }

    /// <summary>
    /// 获取状态特效图标
    /// </summary>
    public EffectStateUi GetBuffEffect(string stateName, Transform usedTran)
    {
        int index = -1;
        for (int i = 0; i < iconNameStr.Length; i++)
        {
            if (iconNameStr[i] == stateName)
            {
                index = i;
                break;
            }
        }
        if (index != -1)
        {
            foreach (var go in iconPoolingList[index])
            {
                if (go == null)
                    continue;
                if (!go.gameObject.activeSelf)
                {
                    go.transform.position = usedTran.position;
                    go.transform.SetParent(usedTran);
                    go.gameObject.SetActive(true);
                    return go;
                }
            }

            var iconObj = Instantiate(GameResources.Instance.StateDin[stateName], usedTran);
            iconObj.transform.position = usedTran.position;
            iconObj.gameObject.SetActive(true);
            iconPoolingList[index].Add(iconObj);
            return iconObj;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 获取状态特效图标
    /// </summary>
    public EffectStateUi GetStateBuff(CardState.Cons con, Transform trans)
    {
        var id = Effect.GetBuffId(con);
        if (id == -1) return null;
        if (!BuffPool.ContainsKey(id))
            BuffPool.Add(id, new List<EffectStateUi>());
        var buff = BuffPool[id].FirstOrDefault(e => !e.gameObject.activeSelf);
        if (buff == null)
        {
            buff = Instantiate(GameResources.Instance.Buff[id], trans);
            BuffPool[id].Add(buff);
        }

        buff.transform.position = trans.position;
        buff.transform.SetParent(trans);
        buff.gameObject.SetActive(true);

        return buff;
    }
    public EffectStateUi GetFloorBuff(int id, Transform trans)
    {
        if (id == -1) return null;
        if (!FloorBuffPool.ContainsKey(id))
            FloorBuffPool.Add(id, new List<EffectStateUi>());
        var buff = FloorBuffPool[id].FirstOrDefault(e => !e.gameObject.activeSelf);
        if (buff == null)
        {
            buff = Instantiate(GameResources.Instance.FloorBuff[id], trans);
            FloorBuffPool[id].Add(buff);
        }

        buff.transform.position = trans.position;
        buff.transform.SetParent(trans);
        buff.gameObject.SetActive(true);

        return buff;
    }

    public void RecycleStateBuff(EffectStateUi go)
    {
        go.transform.SetParent(effectContentTran);
        go.gameObject.SetActive(false);
    }

    /// <summary>
    /// 回收状态图标特效
    /// </summary>
    public void RecycleEffect(EffectStateUi go)
    {
        go.transform.SetParent(effectContentTran);
        go.gameObject.SetActive(false);
    }

    //回收特效
    IEnumerator TakeBackEffect(GameObject go, float takeBackTime)
    {
        yield return new WaitForSeconds(takeBackTime);
        if (go != null)
        {
            go.transform.localScale = Vector3.one;
            go.transform.SetParent(effectContentTran);
            go.SetActive(false);
        }
    }
}