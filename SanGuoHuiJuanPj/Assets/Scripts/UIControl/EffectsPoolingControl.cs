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

    private Dictionary<string, List<GameObject>> EffectPool;

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
        EffectPool = new Dictionary<string, List<GameObject>>();
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

    public GameObject GetEffect(string effectName, Transform targetTransform, float lasting = 1.5f)
    {
        GameObject effect = null;
        if (!EffectPool.ContainsKey(effectName))
            EffectPool.Add(effectName, new List<GameObject>());
        
        effect = EffectPool[effectName].FirstOrDefault(e => !e.activeSelf);
        if (effect == null)
        {
            effect = Instantiate(GameResources.Instance.Effects[effectName]);
            EffectPool[effectName].Add(effect);
        }

        effect.transform.localPosition = Vector3.zero;
        effect.transform.position = targetTransform.position;
        effect.transform.SetParent(targetTransform);
        effect.SetActive(true);
        if (lasting > 0) StartCoroutine(RecycleEffect(effect, lasting));
        return effect;
    }

    public IEnumerator RecycleEffect(GameObject effect, float lasting)
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
    /// 获取状态特效图标,跟随卡牌动
    /// </summary>
    public EffectStateUi GetStateEffect(string stateName, Transform usedTran)
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
    /// 回收状态图标特效
    /// </summary>
    public void TakeBackStateIcon(EffectStateUi go)
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

    public EffectStateUi GetPosState(CombatConduct sp, ChessPos pos)
    {
        EffectStateUi prefab = null;
        switch (sp.Element)
        {
            case TerrainSprite.YeHuo:
                prefab = GameResources.Instance.StateDin[StringNameStatic.StateIconPath_burned];
                break;
            case TerrainSprite.Forge:
                prefab = GameResources.Instance.StateDin[StringNameStatic.StateIconPath_miWuZhenAddtion];
                break;
        }

        if (prefab == null) return null;
        var effect = Instantiate(prefab, pos.transform);
        effect.transform.localScale = Vector3.one;
        effect.transform.localPosition = Vector3.zero;
        return effect;
    }
}

public class Effect
{
    public const string GetGold = "GetGold";
    public const string DropBlood = "dropBlood";
    public const string SpellTextH = "spellTextH";
    public const string SpellTextV = "spellTextV";
    
    public const string Basic0A = "0A";
    public const string Blademail7A = "7A";
    public const string Explode = "201A";
    public const string Dropping = "209A";
    public const string Bow20A = "20A";
    public const string FeiJia3A = "3A";
    public const string Shield4A = "4A";
    public const string SuckBlood6A = "6A";
    public const string Cavalry9A = "9A";
    public const string Cavalry60A = "60A";
    public const string Cavalry16A = "16A";
    public const string Blade17A = "17A";
    public const string CrossBow19A = "19A";
    public const string Crossbow49A = "49A";
    public const string Scribe50A = "50A";
    public const string TengJia57A = "57A";
    public const string Stimulate12A = "12A";
    public const string Guard13A = "13A";
    public const string YellowBand65A = "65A";
    public const string YellowBand65B = "65B";
    public const string HeavyCavalry58A = "58A";
    public const string Barbarians56A = "56A";
    public const string FireShip55A0 = "55A0";
    public const string FireShip55A = "55A";
    public const string FemaleRider44A = "44A";
    public const string Mechanical40A = "40A";
    public const string Debate34A = "34A";
    public const string Controversy35A = "35A";
    public const string Persuade47A = "47A";
    public const string Convince48A = "48A";
    public const string ThrowRocks24A = "24A";
    public const string SiegeMachine23A = "23A";
    public const string Support39A = "39A";
    public const string StateAffairs38A = "38A";
    public const string Heal42A = "42A";
    public const string Heal43A = "43A";
    public const string Assassin25A = "25A";
    public const string Warship21A = "21A";
    public const string Chariot22A = "22A";
    public const string Axe18A = "18A";
    public const string Halberd15A = "15A";
    public const string Knight11A = "11A";
    public const string Daredevil10A = "10A";
    public const string Elephant8A = "8A";
    public const string Spear59A = "59A";
    public const string LongSpear14A = "14A";
    public const string Advisor26A = "26A";
    public const string Warlock28A = "28A";
    public const string Warlock29A = "29A";
    public const string PoisonMaster30A = "30A";
    public const string PoisonMaster31A = "31A";
    public const string FlagBearer32A = "32A";
    public const string FlagBearer33A = "33A";
    public const string Counselor36A= "36A";
    public const string Counselor37A= "37A";
    public const string Lady45A = "45A";
    public const string Lady46A = "46A";
    public const string CrossBow51A="51A";
    public const string LongBow52A = "52A";
    public const string Anchorite53A= "53A";
    public const string Anchorite54A= "54A";

    public const string VTextDodge = "闪避";
    public const string VTextParry = "格挡";
    public const string VTextInvincible = "5";
    public const string VTextShield = "4";
}