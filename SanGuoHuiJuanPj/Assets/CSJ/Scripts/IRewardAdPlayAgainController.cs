
using System;

namespace ByteDance.Union
{
    public interface IRewardAdPlayAgainController
    {
        /**
         * 此方法在主线程回调，请注意另起子线程发起网络请求
         * 当广告可出下次再看时，回调给开发者，由开发者决定下次再看可否再出
         * @param nextPlayAgainCount 下一次再看是第几次再看，值从1开始
         * @param callback 开发者处理完返回给sdk的回调
         */
        void GetPlayAgainCondition(int nextPlayAgainCount, Action<PlayAgainCallbackBean> callback);
    }
}