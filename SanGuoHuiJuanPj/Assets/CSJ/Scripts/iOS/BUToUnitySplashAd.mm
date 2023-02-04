//
//  BUToUnitySplashAd.cpp
//  Unity-iPhone
//
//  Created by wangchao on 2020/6/8.
//

#import <BUAdSDK/BUAdSDK.h>

#import "UnityAppController.h"
#import "BUToUnityAdManager.h"
#import "AdSlot.h"
const char* AutonomousStringCopy_Splash(const char* string)
{
    if (string == NULL) {
        return NULL;
    }
    
    char* res = (char*)malloc(strlen(string) + 1);
    strcpy(res, string);
    return res;
}


// ISplashAdListener callbacks.
typedef void(*SplashAd_OnLoad)(void* splashAd, int context);
typedef void(*SplashAd_OnLoadError)(int errCode, const char* errMsg, int context);
typedef void(*SplashAd_WillShow)(int context, int type);
typedef void(*SplashAd_DidShow)(int context, int type);
typedef void(*SplashAd_DidClick)(int context, int type);
typedef void(*SplashAd_DidClose)(int contextm,int closeType);
typedef void(*SplashAd_VideoAdDidPlayFinish)(int context);
typedef void(*SplashAd_RenderSuccess)(int context);
typedef void(*SplashAd_RenderFailed)(int context);
typedef void(*SplashAd_SplashAdVCClosed)(int context);
typedef void(*SplashAd_DidCloseOtherController)(int interactionType, int context);

@interface BUToUnitySplashAd : NSObject<BUSplashAdDelegate,BUAdObjectProtocol>

@property (nonatomic, strong) BUSplashAd *splashAd;

@property (nonatomic, assign) int loadContext;
@property (nonatomic, assign) int listenContext;

@property (nonatomic, assign) SplashAd_OnLoad onLoad;
@property (nonatomic, assign) SplashAd_OnLoadError onLoadError;
@property (nonatomic, assign) SplashAd_WillShow willShow;
@property (nonatomic, assign) SplashAd_DidShow didShow;
@property (nonatomic, assign) SplashAd_DidClick didClick;
@property (nonatomic, assign) SplashAd_DidClose didClose;
@property (nonatomic, assign) SplashAd_RenderSuccess renderSuccess;
@property (nonatomic, assign) SplashAd_RenderFailed renderFailed;
@property (nonatomic, assign) SplashAd_VideoAdDidPlayFinish didPlayFinish;
@property (nonatomic, assign) SplashAd_SplashAdVCClosed vcClosed;
@property (nonatomic, assign) SplashAd_DidCloseOtherController didCloseOtherController;

@end


@implementation BUToUnitySplashAd

//+ (BUToUnitySplashAd *)sharedInstance {
//    static BUToUnitySplashAd *manager = nil;
//    static dispatch_once_t onceToken;
//    dispatch_once(&onceToken, ^{
//        if(!manager) {
//            manager = [[self alloc] init];
//        }
//    });
//    return manager;
//}

//- (void)splashAdDidLoad:(BUSplashAdView *)splashAd {
////    NSLog(@"splashAd DidLoad");
//    if (self.onLoad) {
//        self.onLoad((__bridge void*)self, self.loadContext);
//    }
//}


//- (void)splashAd:(BUSplashAdView *)splashAd didFailWithError:(NSError * _Nullable)error {
//    if (self.onLoadError) {
//        self.onLoadError((int)error.code, AutonomousStringCopy_Splash([[error localizedDescription] UTF8String]), self.loadContext);
//    }
//    [_splashAdView removeFromSuperview];
//    _splashAd = nil;
//}

//
//- (void)splashAdWillVisible:(BUSplashAdView *)splashAd {
////    NSLog(@"splashAd WillVisible");
//    if (self.willVisible) {
//        self.willVisible(self.listenContext, 0);
//    }
//}
//
//
//- (void)splashAdDidClick:(BUSplashAdView *)splashAd {
//    if (self.didClick) {
//        self.didClick(self.listenContext, 0);
//    }
//}


//- (void)splashAdDidClose:(BUSplashAdView *)splashAd {
//    if (self.didClose) {
//        self.didClose(self.listenContext);
//    }
//}


//- (void)splashAdWillClose:(BUSplashAdView *)splashAd {
//    if (self.willClose) {
//        self.willClose(self.listenContext);
//    }
//}

/**
 This method is called when spalashAd skip button  is clicked.
 */
//- (void)splashAdDidClickSkip:(BUSplashAdView *)splashAd {
//
//    if (self.didSkip) {
//        self.didSkip(self.listenContext);
//    }
//}
//
///**
// This method is called when spalashAd countdown equals to zero
// */
//- (void)splashAdCountdownToZero:(BUSplashAdView *)splashAd {
//
//    if (self.didCountdownToZero) {
//        self.didCountdownToZero(self.listenContext);
//    }
//}
//
//- (void)splashAdDidCloseOtherController:(BUSplashAdView *)splashAd interactionType:(BUInteractionType)interactionType {
//
////    NSLog(@"splashAd DidCloseOtherController");
//    if (self.didCloseOtherController) {
//        self.didCloseOtherController((int)interactionType, self.listenContext);
//    }
//}

// new splash ad delegate
/// This method is called when material load successful
- (void)splashAdLoadSuccess:(BUSplashAd *)splashAd {
    if (self.onLoad) {
        self.onLoad((__bridge void*)self, self.loadContext);
    }
}

/// This method is called when material load failed
- (void)splashAdLoadFail:(BUSplashAd *)splashAd error:(BUAdError *_Nullable)error {
    if (self.onLoadError) {
        self.onLoadError((int)error.code, AutonomousStringCopy_Splash([[error localizedDescription] UTF8String]), self.loadContext);
    }
    [self.splashAd.splashView removeFromSuperview];
    _splashAd = nil;
}

/// This method is called when splash view render successful
- (void)splashAdRenderSuccess:(BUSplashAd *)splashAd {
    if (self.renderSuccess) {
        self.renderSuccess(self.listenContext);
        [self.splashAd showSplashViewInRootViewController:GetAppController().rootViewController];
    }
}

/// This method is called when splash view render failed
- (void)splashAdRenderFail:(BUSplashAd *)splashAd error:(BUAdError *_Nullable)error {
    if (self.renderFailed) {
        self.renderFailed(self.listenContext);
    }
}

/// This method is called when splash view will show
- (void)splashAdWillShow:(BUSplashAd *)splashAd {
    if (self.willShow) {
        self.willShow(self.listenContext, 0);
    }
}

/// This method is called when splash view did show
- (void)splashAdDidShow:(BUSplashAd *)splashAd {
    if(self.didShow) {
        self.didShow(self.listenContext, 0);
    }
}

/// This method is called when splash view is clicked.
- (void)splashAdDidClick:(BUSplashAd *)splashAd {
    if (self.didClick) {
        self.didClick(self.listenContext, 0);
    }
}

/// This method is called when splash view is closed.
- (void)splashAdDidClose:(BUSplashAd *)splashAd closeType:(BUSplashAdCloseType)closeType {
    if (self.didClose) {
        self.didClose(self.listenContext,(int)closeType);
    }
}

/// This method is called when splash viewControllr is closed.
- (void)splashAdViewControllerDidClose:(BUSplashAd *)splashAd {
    if(self.vcClosed) {
        self.vcClosed(self.listenContext);
    }
}

/**
 This method is called when another controller has been closed.
 @param interactionType : open appstore in app or open the webpage or view video ad details page.
 */
- (void)splashDidCloseOtherController:(BUSplashAd *)splashAd interactionType:(BUInteractionType)interactionType {
    if (self.didCloseOtherController) {
        self.didCloseOtherController((int)interactionType, self.listenContext);
    }
}

/// This method is called when when video ad play completed or an error occurred.
- (void)splashVideoAdDidPlayFinish:(BUSplashAd *)splashAd didFailWithError:(NSError *)error {
    if (self.didPlayFinish) {
        self.didPlayFinish(self.listenContext);
    }
}

- (id<BUAdClientBiddingProtocol>)adObject {
    return self.splashAd;
}

#if defined (__cplusplus)
extern "C" {
#endif
    void* UnionPlatform_SplashAd_Load(
                                      AdSlotStruct *adSlot,
                                     int timeout,
                                     SplashAd_OnLoadError onLoadError,
                                     SplashAd_OnLoad onLoad,
                                      SplashAd_RenderSuccess renderSuccess,
                                      SplashAd_RenderFailed renderFailed,
                                     int context) {
        
        BUToUnitySplashAd* instance = [[BUToUnitySplashAd alloc] init];
        instance.loadContext = context;
        instance.onLoad = onLoad;
        instance.onLoadError = onLoadError;
        instance.renderSuccess = renderSuccess;
        instance.renderFailed = renderFailed;
        
        CGRect frame = CGRectMake(0, 0, CGRectGetWidth([UIScreen mainScreen].bounds), CGRectGetHeight([UIScreen mainScreen].bounds));
        BUAdSlot *slot = [BUAdSlot new];
        slot.ID = [NSString stringWithUTF8String:adSlot->slotId?:""];
        slot.AdType = BUAdSlotAdTypeSplash;
        slot.position = BUAdSlotPositionFullscreen;
        BUSize *size = [[BUSize alloc] init];
        size.width = frame.size.width * [[UIScreen mainScreen] scale];
        size.height = frame.size.height * [[UIScreen mainScreen] scale];
        slot.imgSize = size;
        slot.adloadSeq = 0;
        slot.primeRit = nil;
        if (adSlot->adLoadType != 0) {
           slot.adLoadType = (BUAdLoadType)[@(adSlot->adLoadType) integerValue];
        }
        instance.splashAd = [[BUSplashAd alloc] initWithSlot:slot adSize:CGSizeMake(frame.size.width, frame.size.height)];
        instance.splashAd.delegate = instance;
        
        if (timeout > 0) {
            instance.splashAd.tolerateTimeout = timeout;
        }
        
        [instance.splashAd loadAdData];
        
        // 强持有，是引用加+1
       
        return (__bridge_retained void*)instance;
    }
    
    void UnionPlatform_SplashAd_SetInteractionListener(void* splashAdPtr,
                                                            SplashAd_WillShow willshow,
                                                            SplashAd_DidShow didShow,
                                                            SplashAd_DidClick didClick,
                                                            SplashAd_DidClose didClose,
                                                       SplashAd_VideoAdDidPlayFinish playFinish,
                                                            int context) {
        BUToUnitySplashAd* splashAd = (__bridge BUToUnitySplashAd*)splashAdPtr;
        
        splashAd.listenContext = context;
        splashAd.willShow = willshow;
        splashAd.didClick = didClick;
        splashAd.didClose = didClose;
        splashAd.didShow  = didShow;
        splashAd.didPlayFinish = playFinish;
    }
    
    void UnionPlatform_SplashAd_Show (void* splashAdPtr) {
        BUToUnitySplashAd *splashAd = (__bridge BUToUnitySplashAd*)splashAdPtr;
        [splashAd.splashAd showSplashViewInRootViewController:GetAppController().rootViewController];
    }
    
    void UnionPlatform_SplashAd_Dispose(void* splashAdPtr) {
        
//        (__bridge_transfer BUToUnitySplashAd*)splashAd;
        dispatch_async(dispatch_get_main_queue(), ^{
            BUToUnitySplashAd *splashAd = (__bridge BUToUnitySplashAd*)splashAdPtr;
            [splashAd.splashAd.splashView removeFromSuperview];
            splashAd.splashAd = nil;
            (__bridge_transfer BUToUnitySplashAd*)splashAdPtr;
        });
        // [[BUToUnityAdManager sharedInstance] deleteAdManager:splashAd];
    
    }
    
#if defined (__cplusplus)
}
#endif
    
@end
