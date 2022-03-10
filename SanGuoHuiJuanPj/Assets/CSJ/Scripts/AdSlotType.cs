//------------------------------------------------------------------------------
// Copyright (c) 2018-2019 Beijing Bytedance Technology Co., Ltd.
// All Right Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//------------------------------------------------------------------------------

namespace ByteDance.Union
{
    /// <summary>
    /// The type of AdSlot.
    /// </summary>
    public enum AdSlotType
    {
        /// <summary>
        /// The banner Ad type.
        /// </summary>
        Banner,

        /// <summary>
        /// The interaction Ad type.
        /// </summary>
        InteractionAd,

        /// <summary>
        /// The splash Ad type.
        /// </summary>
        Splash,

        /// <summary>
        /// The cached splash Ad type.
        /// </summary>
        CachedSplash,

        /// <summary>
        /// The feed Ad type.
        /// </summary>
        Feed,

        /// <summary>
        /// The reward video Ad type.
        /// </summary>
        RewardVideo,

        /// <summary>
        /// The full screen video Ad type.
        /// </summary>
        FullScreenVideo,

        /// <summary>
        /// The draw feed Ad type.
        /// </summary>
        DrawFeed,
        
        UNKOWN
    }
    /// <summary>
    /// SplashView Button Type
    /// config the type in AdSlot,define which area will action when click the splash ad
    /// </summary>
    public enum SplashButtonType :int {
        /// <summary>
        /// Full Screen will response to click
        /// </summary>
        FullScreen = 1,

        /// <summary>
        /// The area of download bar will response to click
        /// </summary>
        DownloadBar = 2
    }

    public enum AdLoadType : int
    {
        /// <summary>
        /// Unknown type
        /// </summary>
        Unknown = -1,

        /// <summary>
        /// Preload resources
        /// </summary>
        PreLoad = 1,

        /// <summary>
        /// Load resources in real time
        /// </summary>
        Load = 3
    }
    
    public enum DownloadType
    {
        DownloadTypeNoPopup=0,
        DownloadTypePopup=1
    }
}
