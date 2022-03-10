package com.bytedance.android;
import com.bytedance.sdk.openadsdk.LocationProvider;
import com.bytedance.sdk.openadsdk.TTCustomController;

/**
 * created by jijiachun on 2021/10/26
 */
public class CustomController extends TTCustomController {
    //纬度
    private double mLatitude = Double.NaN;
    //经度
    private double mLongitude = Double.NaN;
    private String mDevImei;
    private String mDevOaid;
    private String macAddress;

    private boolean mIsCanUseLocation = true;
    private boolean mIsCanReadAppList = true;
    private boolean mIsCanUsePhoneState = true;
    private boolean mIsCanUseWifiState = true;
    private boolean mIsCanUseWriteExternal = true;

    public void canUseLocation(boolean canUseLocation) {
        mIsCanUseLocation = canUseLocation;
    }

    public void setLocationInfo(double longitude, double latitude) {
        this.mLongitude = longitude;
        this.mLatitude = latitude;
    }

    public void canReadAppList(boolean canReadAppList) {
        mIsCanReadAppList = canReadAppList;
    }

    public void canUsePhoneState(boolean canUsePhoneState) {
        mIsCanUsePhoneState = canUsePhoneState;
    }

    public void setDevImei(String imei) {
        this.mDevImei = imei;
    }

    public void canUseWifiState(boolean canUseWifiState) {
        mIsCanUseWifiState = canUseWifiState;
    }

    public void setMacAddress(String macAddress) {
        this.macAddress = macAddress;
    }
    
    public void canUseWriteExternal(boolean canUseWriteExternal) {
        this.mIsCanUseWriteExternal = canUseWriteExternal;
    }

    public void setDevOaid(String oaid) {
        this.mDevOaid = oaid;
    }

    /**
     * 是否允许SDK主动使用地理位置信息
     *
     * @return true可以获取，false禁止获取。默认为true
     */
    public boolean isCanUseLocation() {
        return mIsCanUseLocation;
    }

    /**
     * 当isCanUseLocation=false时，可传入地理位置信息，穿山甲sdk使用您传入的地理位置信息
     *
     * @return 地理位置参数
     */
    public LocationProvider getTTLocation() {
        if (Double.isNaN(mLatitude) || Double.isNaN(mLongitude)) {
            return null;
        }
        return new LocationProvider() {
            @Override
            public double getLatitude() {
                return mLatitude;
            }

            @Override
            public double getLongitude() {
                return mLongitude;
            }
        };
    }

    /**
     * 是否允许sdk上报手机app安装列表
     *
     * @return true可以上报、false禁止上报。默认为true
     */
    public boolean alist() {
        return mIsCanReadAppList;
    }

    /**
     * 是否允许SDK主动使用手机硬件参数，如：imei
     *
     * @return true可以使用，false禁止使用。默认为true
     */
    public boolean isCanUsePhoneState() {
        return mIsCanUsePhoneState;
    }

    /**
     * 当isCanUsePhoneState=false时，可传入imei信息，穿山甲sdk使用您传入的imei信息
     *
     * @return imei信息
     */
    public String getDevImei() {
        return mDevImei;
    }

    /**
     * 是否允许SDK主动使用ACCESS_WIFI_STATE权限
     *
     * @return true可以使用，false禁止使用。默认为true
     */
    @Override
    public boolean isCanUseWifiState() {
        return mIsCanUseWifiState;
    }

    /**
     * 当isCanUseWifiState=false时，可传入Mac地址信息，穿山甲sdk使用您传入的Mac地址信息
     *
     * @return Mac地址信息
     */
    @Override
    public String getMacAddress() {
        return macAddress != null ? macAddress : super.getMacAddress();
    }

    /**
     * 是否允许SDK主动使用WRITE_EXTERNAL_STORAGE权限
     *
     * @return true可以使用，false禁止使用。默认为true
     */
    public boolean isCanUseWriteExternal() {
        return mIsCanUseWriteExternal;
    }

    /**
     * 开发者可以传入oaid
     *
     * @return oaid
     */
    public String getDevOaid() {
        return mDevOaid;
    }

}