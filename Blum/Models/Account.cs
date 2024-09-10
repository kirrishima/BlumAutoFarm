using Blum.Services;
using System.Text.Json.Serialization;

namespace Blum.Models
{
    public class Account
    {
        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("session_name")]
        public string Name { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = (Account)obj;
            return PhoneNumber == other.PhoneNumber && Name == other.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PhoneNumber, Name);
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
