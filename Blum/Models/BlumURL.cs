namespace Blum.Models
{
    internal readonly struct BlumUrls
    {
        /// <summary>https://game-domain.blum.codes/api/v1/user/balance</summary>
        public static readonly string BALANCE = "https://game-domain.blum.codes/api/v1/user/balance";

        /// <summary>https://user-domain.blum.codes/api/v1/auth/provider/PROVIDER_TELEGRAM_MINI_APP</summary>
        public static readonly string PROVIDER_MINIAPP = "https://user-domain.blum.codes/api/v1/auth/provider/PROVIDER_TELEGRAM_MINI_APP";

        /// <summary>https://game-domain.blum.codes/api/v1/farming/claim</summary>
        public static readonly string FARMING_CLAIM = "https://game-domain.blum.codes/api/v1/farming/claim";

        /// <summary>https://game-domain.blum.codes/api/v1/farming/start</summary>
        public static readonly string FARMING_START = "https://game-domain.blum.codes/api/v1/farming/start";

        /// <summary>https://game-domain.blum.codes/api/v1/game/play</summary>
        public static readonly string GAME_START = "https://game-domain.blum.codes/api/v2/game/play";

        /// <summary>https://game-domain.blum.codes/api/v1/game/claim</summary>
        public static readonly string GAME_CLAIM = "https://game-domain.blum.codes/api/v2/game/claim";

        /// <summary>https://game-domain.blum.codes/api/v2/game/eligibility/dogs_drop</summary>
        public static readonly string DOGS_ELIGIBILITY = "https://game-domain.blum.codes/api/v2/game/eligibility/dogs_drop";

        /// <summary>https://gateway.blum.codes/v1/auth/refresh</summary>
        public static readonly string REFRESH = "https://user-domain.blum.codes/api/v1/auth/refresh";

        /// <summary>https://game-domain.blum.codes/api/v1/daily-reward?offset=-180</summary>
        public static readonly string CLAIM_DAILY_REWARD = "https://game-domain.blum.codes/api/v1/daily-reward?offset=-180";

        /// <summary> https://raw.githubusercontent.com/zuydd/database/main/blum.json </summary>
        public static readonly string PAYLOAD_ENDPOINTS_DATABASE = "https://raw.githubusercontent.com/zuydd/database/main/blum.json";

        public static string GetGameClaimPayloadURL(string payloadServerID)
        {
            return $"https://{payloadServerID}.vercel.app/api/blum";
        }
    }
}
