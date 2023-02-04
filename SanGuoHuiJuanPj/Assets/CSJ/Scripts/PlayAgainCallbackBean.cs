namespace ByteDance.Union
{
    public class PlayAgainCallbackBean
    {
        /**
         * 是否允许下次再看
         * value为 boolean类型
         */
        public const string KEY_PLAY_AGAIN_ALLOW = "play_again_allow";
        
        /**
         * 下次再看可得的奖励名称
         * value为 string类型
         */
        public const string KEY_PLAY_AGAIN_REWARD_NAME = "play_again_reward_name";
        
        /**
         * 下次再看可得的奖励数量
         * value为 string类型
         */
        public const string KEY_PLAY_AGAIN_REWARD_AMOUNT = "play_again_reward_amount";
        public PlayAgainCallbackBean(bool isAgain, string rewardName, string rewardCount)
        {
            IsAgain = isAgain;
            RewardName = rewardName;
            RewardCount = rewardCount;
        }
        public bool IsAgain { get; set; }
        
        public string RewardName { get; set; }
        
        public string RewardCount { get; set; }
    }
}