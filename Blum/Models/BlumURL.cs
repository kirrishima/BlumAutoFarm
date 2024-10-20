namespace Blum.Models
{
    internal readonly struct BlumUrls
    {
        /// <summary>https://game-domain.blum.codes/api/v1/user/balance</summary>
        public static readonly string Balance = "https://game-domain.blum.codes/api/v1/user/balance";

        /// <summary>https://user-domain.blum.codes/api/v1/auth/provider/PROVIDER_TELEGRAM_MINI_APP</summary>
        public static readonly string ProviderMiniApp = "https://user-domain.blum.codes/api/v1/auth/provider/PROVIDER_TELEGRAM_MINI_APP";

        /// <summary>https://game-domain.blum.codes/api/v1/farming/claim</summary>
        public static readonly string FarmingClaim = "https://game-domain.blum.codes/api/v1/farming/claim";

        /// <summary>https://game-domain.blum.codes/api/v1/farming/start</summary>
        public static readonly string FarmingStart = "https://game-domain.blum.codes/api/v1/farming/start";

        /// <summary>https://game-domain.blum.codes/api/v1/game/claim</summary>
        public static readonly string GameClaim = "https://game-domain.blum.codes/api/v2/game/claim";

        /// <summary>https://game-domain.blum.codes/api/v1/game/play</summary>
        public static readonly string GameStart = "https://game-domain.blum.codes/api/v2/game/play";

        /// <summary>https://gateway.blum.codes/v1/auth/refresh</summary>
        public static readonly string Refresh = "https://user-domain.blum.codes/api/v1/auth/refresh";

        /// <summary>https://game-domain.blum.codes/api/v1/daily-reward?offset=-180</summary>
        public static readonly string ClaimDailyReward = "https://game-domain.blum.codes/api/v1/daily-reward?offset=-180";

        /// <summary> https://raw.githubusercontent.com/zuydd/database/main/blum.json </summary>
        public static readonly string PayloadEndpointsDatabase = "https://raw.githubusercontent.com/zuydd/database/main/blum.json";

        public static string GetGameClaimPayloadURL(string payloadServerID)
        {
            return $"https://{payloadServerID}.vercel.app/api/blum";
        }
    }
}
