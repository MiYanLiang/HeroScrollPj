using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaYeTester : MonoBehaviour
{
    public void OnGenerateStoryEvents() => BaYeManager.instance.GenerateBaYeStoryEvents();
    public void AddExp(int exp = 10) => BaYeManager.instance.AddExp(-10, exp);
}
