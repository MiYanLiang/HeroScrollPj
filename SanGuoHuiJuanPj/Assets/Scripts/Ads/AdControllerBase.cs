﻿using System;
using System.Threading;
using Donews.mediation;
using UnityEngine;
using UnityEngine.Events;

public abstract class AdControllerBase : MonoBehaviour
{
    public abstract AdAgentBase.States Status { get; }
    public abstract void RequestShow(UnityAction<bool, string> requestAction);
    //todo: 当初实现控制器的时候没设计好，几个不同类型的广告代理控制绕来绕去。需要花时间重构
    public abstract void RequestLoad(UnityAction<bool, string> loadingAction);
}