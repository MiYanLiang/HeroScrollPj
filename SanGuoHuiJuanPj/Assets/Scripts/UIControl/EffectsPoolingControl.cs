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
            List<GameObject> iconList = new List<GameObject>();
            for (int j = 0; j < maxCount; j++)
            {
                GameObject iconObj = Instantiate(GameResources.Instance.StateDin[iconNameStr[i]], effectContentTran);
                iconObj.SetActive(false);
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

            GameObject iconObj = Instantiate(GameResources.Instance.StateDin[iconName], usedTran);
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
            go.transform.localScale = Vector3.one;
            go.transform.SetParent(effectContentTran);
            go.SetActive(false);
        }
    }
}

public class Effect
{
    public const string GetGold = "GetGold";
    public const string DropBlood = "dropBlood";
    public const string SpellTextH = "spellTextH";
    public const string SpellTextV = "spellTextV";
    
    public const string BasicAttack = "0A";
    public const string ReflectDamage = "7A";
    public const string Explode = "201A";
    public const string Dropping = "209A";
    public const string LongBow = "20A";
    public const string FeiJia = "3A";
    public const string Shield = "4A";
    public const string SuckBlood = "6A";
    public const string CavalryCharge = "9A";
    public const string CavalryAssault = "60A";
    public const string CavalryGallop = "16A";
    public const string BladeCombo = "17A";
    public const string CrossBowCombo = "19A";
    public const string Crossbow = "49A";
    public const string MagicStrike = "50A";
    public const string TengJiaAttack = "57A";
    public const string Stimulate = "12A";
    public const string GuardCounter = "13A";
    public const string YellowBand = "65A";
    public const string YellowBandB = "65B";
    public const string HeavyCavalry = "58A";
    public const string Barbarians = "56A";
    public const string FireShipExplode = "55A0";
    public const string FireShipAttack = "55A";
    public const string DisarmAttack = "44A";
    public const string Mechanical = "40A";
    public const string Debate = "34A";
    public const string Controversy = "35A";
    public const string Persuade = "47A";
    public const string Convince = "48A";
    public const string ThrowRocks = "24A";
    public const string SiegeMachine = "23A";
    public const string Support = "39A";
    public const string StateAffairs = "38A";
    public const string Heal = "42A";
    public const string Cure = "43A";
    public const string AssassinStrike = "25A";
    public const string WarshipAttack = "21A";
    public const string ChariotAttack = "22A";
    public const string AxeStrike = "18A";
    public const string HalberdSweep = "15A";
    public const string KnightAttack = "11A";
    public const string Daredevil = "10A";
    public const string ElephantAttack = "8A";
}