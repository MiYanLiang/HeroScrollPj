﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using MoreLinq;
using UnityEngine;
using UnityEngine.UI;

public class JiBanAnimationManager : MonoBehaviour
{
    [SerializeField]private JiBanController Player;
    [SerializeField]private JiBanController Opposite;
    [SerializeField]private JiBanOffensiveField[] JiBanOffensiveFields;
    public Dictionary<int, (int CardId, GameCardType CardType)[]> JiBanMap;
    public void Init()
    {
        Player.JiBanImageObj.SetActive(false);
        Opposite.JiBanImageObj.SetActive(false);
        JiBanMap = new Dictionary<int, (int, GameCardType)[]>();
        foreach (var itm in DataTable.JiBan.Where(j=>j.Value.IsOpen > 0))
        {
            var jiBan = itm.Value;
            JiBanMap.Add(itm.Key, jiBan.Cards.Select(j => (j.CardId, (GameCardType)j.CardType)).ToArray());
        }
    }
    /// <summary>
    /// 返回可触发羁绊
    /// </summary>
    /// <param name="cards"></param>
    /// <returns></returns>
    public (int JiBanId, bool IsChallenger)[] GetAvailableJiBan(List<FightCardData> cards)
    {
        var list = new List<(int, bool)>();
        foreach (var jb in JiBanMap)
        {
            if (cards.Count == 0) break;
            var jbCards = cards.Join(jb.Value, c => (c.cardId, c.CardType), j => (j.CardId, j.CardType), (c, _) => c)
                .DistinctBy(j => j.CardId).ToArray();
            if (jbCards.Length != jb.Value.Length) continue;
            list.Add((jb.Key, jbCards.First().IsPlayer));
        }
        return list.ToArray();
    }

    public IEnumerator JiBanDisplay(int jbId, bool isChallenger)
    {
        var con = GetController(isChallenger);
        con.MainImage.sprite = GameResources.Instance.JiBanBg[jbId];
        con.MainTitle.sprite = GameResources.Instance.JiBanHText[jbId];
        con.JiBanImageObj.transform.localPosition = Vector3.zero;
        con.JiBanImageObj.SetActive(true);
        yield return new WaitForSeconds(CardAnimator.instance.Misc.JBAnimLasting);
        con.JiBanImageObj.SetActive(false);
    }
    public IEnumerator JiBanOffensive(int jbId,bool isChallenger)
    {
        var field = GetField(jbId);
        var con = GetController(!isChallenger);
        field.OffensiveAnim.transform.SetParent(con.AnimTransform);
        field.OffensiveAnim.transform.localPosition = Vector3.zero;
        field.OffensiveAnim.SetActive(true);
        yield return new WaitForSeconds(field.AnimSecs);
        field.OffensiveAnim.SetActive(false);
    }

    private JiBanController GetController(bool isChallenger) => isChallenger ? Player : Opposite;
    private JiBanOffensiveField GetField(int jbId)
    {
        var field = JiBanOffensiveFields.FirstOrDefault(f => f.JiBanId == jbId);
        if(field==null)
            XDebug.LogError<JiBanAnimationManager>($"找不到羁绊Id={jbId}的攻击动画，请确保动画已经设置好。");
        return field;
    }
    /// <summary>
    /// 羁绊演示控制器
    /// </summary>
    [Serializable]
    public class JiBanController
    {
        public GameObject JiBanImageObj;
        public Transform AnimTransform;
        public Image MainImage;
        public Image MainTitle;
    }
    /// <summary>
    /// 羁绊进攻动画演示
    /// </summary>
    [Serializable]
    public class JiBanOffensiveField
    {
        public int JiBanId;
        public GameObject OffensiveAnim;
        public float AnimSecs = 1.5f;
    }

    public bool IsOffensiveJiBan(int jbId) => JiBanOffensiveFields.Any(j => j.JiBanId == jbId);
}