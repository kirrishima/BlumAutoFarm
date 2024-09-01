using Blum.Services;
using System.Text.Json.Serialization;

namespace Blum.Models
{
    public class Account
    {
        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("session_name")]
        public string SessionName { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = (Account)obj;
            return PhoneNumber == other.PhoneNumber && SessionName == other.SessionName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PhoneNumber, SessionName);
        }

        public bool IsValid()
        {
            AccountsData accounts = new();
            accounts.Accounts.Add(this);
            AccountService.ValidateAccountsData(ref accounts);

            return accounts.Accounts.Count == 1;

        }
    }

    public class AccountsData
    {
        [JsonPropertyName("accounts")]
        public List<Account> Accounts { get; set; } = [];
    }
}
