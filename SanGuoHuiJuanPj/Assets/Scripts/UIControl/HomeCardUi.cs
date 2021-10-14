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

    public override void UpdateHpUi(float hpRate)
    {
        base.UpdateHpUi(hpRate);

        var field = BurningState.Fields.Where(f => hpRate < f.Value).OrderBy(f => f.Value)
            //.ToList();
            .FirstOrDefault();
        BurningState.MainObj.SetActive(field != null);
        if (field == null) return;
        for (var i = 0; i < field.Objs.Length; i++)
        {
            var isActive = field.Objs[i];
            //if (hpRate < obj.Value)
            //    for (var i = 0; i < obj.Objs.Length; i++)
            BurningState.AnimObjs[i].SetActive(isActive);
        }
        //obj.Obj.SetActive(obj.IsActive);
    }

    [Serializable]public class BurningField
    {
        public GameObject MainObj;
        public GameObject[] AnimObjs;
        public StageField[] Fields;
    }
    [Serializable]public class StageField
    {
        public float Value;
        public bool[] Objs;

    }
    //[Serializable]public class ObjectState
    //{
    //    public ObjectState[] ObjFields;
    //    public GameObject Obj;
    //    public bool IsActive;
    //}
}