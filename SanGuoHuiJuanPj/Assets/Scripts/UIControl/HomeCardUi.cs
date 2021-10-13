using System;
using System.Linq;
using UnityEngine;

public class HomeCardUi : GameCardWarUiOperation
{
    [SerializeField] private BurningField BurningState;

    public override void Init(DragController drag)
    {
        BurningState.MainObj.SetActive(false);
        Lose.gameObject.SetActive(false);
    }

    public override void UpdateHpUi(float hp)
    {
        base.UpdateHpUi(hp);
        BurningState.MainObj.SetActive(true);
        var field = BurningState.Fields.Where(f => hp > f.Value).OrderByDescending(f => f.Value).FirstOrDefault();
        if (field == null) return;
        foreach (var obj in field.ObjFields) obj.Obj.SetActive(obj.IsActive);
    }

    [Serializable]public class BurningField
    {
        public GameObject MainObj;
        public StageField[] Fields;
    }
    [Serializable]public class StageField
    {
        public float Value;
        public ObjectState[] ObjFields;

    }
    [Serializable]public class ObjectState
    {
        public GameObject Obj;
        public bool IsActive;
    }
}