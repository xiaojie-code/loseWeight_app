using System;

namespace LoseWeight.Platform
{
    /// <summary>
    /// 平台服务接口 - 抽象登录、分享、广告、权限等平台能力
    /// </summary>
    public interface IPlatformService
    {
        void Login(Action<LoginResult> callback);
        void Share(ShareData data);
        void ShowRewardedAd(string placementId, Action<bool> callback);
        void RequestCameraPermission(Action<bool> callback);
        string GetDeviceLevel(); // "low", "mid", "high"
        string GetChannel();
    }

    [Serializable]
    public class LoginResult
    {
        public bool Success;
        public string UserId;
        public string Token;
        public string ErrorMessage;
    }

    [Serializable]
    public class ShareData
    {
        public string Title;
        public string ImageUrl;
        public string Payload;
    }
}
