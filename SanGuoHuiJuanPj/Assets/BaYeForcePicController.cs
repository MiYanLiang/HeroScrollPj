using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BaYeForcePicController : MonoBehaviour
{
    [SerializeField] private ForceSelector[] forces;
    [SerializeField] private Image image;

    public void UpdateUi() => image.sprite = forces.FirstOrDefault(f => f.TroopId == BaYeManager.instance.BaYeLing)?.Sprite;

    [Serializable] private class ForceSelector
    {
        [SerializeField] private Sprite _sprite;
        [SerializeField] private int troopId;
        public Sprite Sprite => _sprite;
        public int TroopId => troopId;
    }
}
