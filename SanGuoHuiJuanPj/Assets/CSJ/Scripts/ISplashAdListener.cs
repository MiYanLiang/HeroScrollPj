//------------------------------------------------------------------------------
// Copyright (c) 2018-2019 Beijing Bytedance Technology Co., Ltd.
// All Right Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//------------------------------------------------------------------------------

namespace ByteDance.Union
{
    /// <summary>
    /// The listener for splash Ad.
    /// </summary>
    public interface ISplashAdListener
    {
        /// <summary>
        /// Invoke when load Ad fail.
        /// </summary>
        void OnSplashLoadFail(int code, string message);

        /// <summary>
        /// Invoke when the Ad load success.
        /// </summary>
        void OnSplashLoadSuccess();

        /// <summary>
        /// Invoke when the Ad render success.
        /// </summary>
        void OnSplashRenderSuccess(BUSplashAd ad);
        
        /// <summary>
        /// Invoke when the Ad render fail.
        /// </summary>
        void OnSplashRenderFail(int code, string message);
    }
}
