using Blum.Exceptions;
using Blum.Models;
using Blum.Utilities;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Blum.Services
{
    public partial class AccountService
    {
        private readonly string _filePath;
        private readonly Encryption _aes;
        private static readonly Logger _logger = new();
        public static readonly string DefaultAccountsFilepath = Path.Combine(TelegramSettings.settingsDirectory, "accounts.dat");

        public AccountService(string? filePath = null)
        {
            var aes = new Encryption(TelegramSettings.ApiHash);

            _filePath = filePath ?? DefaultAccountsFilepath;
            _aes = aes;
        }

        public AccountsData GetAccounts()
        {
            if (!File.Exists(_filePath))
            {
                return new AccountsData();
            }

            string jsonContent;
            try
            {
                string encryptedJson = File.ReadAllText(_filePath);
                if (string.IsNullOrWhiteSpace(encryptedJson))
                {
                    return new AccountsData();
                }
                jsonContent = _aes.Decrypt(encryptedJson);
            }
            catch (Exception)
            {
                return new AccountsData();
            }

            AccountsData? accountsData;
            try
            {
                accountsData = JsonSerializer.Deserialize<AccountsData>(jsonContent);
            }
            catch (JsonException)
            {
                return new AccountsData();
            }

            if (accountsData == null || accountsData.Accounts == null || accountsData.Accounts.Count == 0)
            {
                return new AccountsData();
            }

            return accountsData;
        }

        public string AddAccount(string sessionName, string phoneNumber)
        {
            BlumException.ThrowIfNull(phoneNumber, nameof(phoneNumber));
            BlumException.ThrowIfNull(sessionName, nameof(sessionName));

            if (!IsValidPhoneNumber(phoneNumber, out string _))
                throw new BlumException("Phone number is not valid.");

            if (!IsValidSessionName(sessionName, out string _))
                throw new BlumException("Session name is not valid.");

            var accountsData = GetAccounts();
            ValidateAccountsData(ref accountsData);

            var newAccount = new Account
            {
                PhoneNumber = phoneNumber.StartsWith("+") ? phoneNumber : "+" + phoneNumber,
                Name = sessionName
            };

            if (accountsData.Accounts.Contains(newAccount))
            {
                return "Account with this credentials already exists";
            }

            foreach (var item in accountsData.Accounts)
            {
                if (item.Name == sessionName)
                {
                    return $"Account with name {sessionName} already exists";
                }
                if (item.PhoneNumber.Replace("+", "") == phoneNumber.Replace("+", ""))
                {
                    return $"Account with phone number {phoneNumber} already exists";
                }
            }

            accountsData.Accounts.Add(newAccount);
            ValidateAccountsData(ref accountsData);

            SaveJsonFile(accountsData);

            return "Account added!";
        }

        public string DisableEnableAccount(string accountName, bool enable = false)
        {
            var accountsData = GetAccounts();

            foreach (var account in accountsData.Accounts)
            {
                if (account.Name == accountName)
                {
                    account.Enabled = enable;

                    SaveJsonFile(accountsData);

                    return $"Account '{account.Name}' is {(account.Enabled ? "enabled" : "disabled")}";
                }
            }
            return $"Account with name '{accountName}' was not found";
        }

        public string DeleteAccountByName(string accountName)
        {
            var accountsData = GetAccounts();
            ValidateAccountsData(ref accountsData);

            Account? foundAccount = accountsData.Accounts.Find((Account acc) => acc.Name == accountName);

            if (foundAccount != null)
            {
                accountsData.Accounts.Remove(foundAccount);

                string sessionFilepath = Path.Combine(TelegramSessionsPaths.SessionsFolder, accountName);
                string sessionLogfilePath = TelegramSessionsPaths.GetWTelegramLogFilePath(accountName);

                if (File.Exists(sessionFilepath))
                {
                    File.Delete(sessionFilepath);
                }

                if (File.Exists(sessionLogfilePath))
                {
                    File.Delete(sessionLogfilePath);
                }

                ValidateAccountsData(ref accountsData);

                SaveJsonFile(accountsData);

                return $"Account '{accountName}' deleted successfully";
            }

            return $"Account '{accountName}' was not found.";
        }

        private void SaveJsonFile(AccountsData accountsData)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonContent = JsonSerializer.Serialize(accountsData, options);
                string encryptedJson = _aes.Encrypt(jsonContent);
                File.WriteAllText(_filePath, encryptedJson);
            }
            catch (Exception ex)
            {
                throw new BlumException("Error saving JSON file.", ex);
            }
        }

        public static void ValidateAccountsData(ref AccountsData accountsData)
        {
            var validAccounts = new List<Account>();

            foreach (var account in accountsData.Accounts)
            {
                bool isValid = true;

                if (!IsValidPhoneNumber(account.PhoneNumber, out string _))
                {
                    _logger.Warning($"Warning: Phone number '{account.PhoneNumber}' is not valid and will be removed.");
                    isValid = false;
                }

                if (!IsValidSessionName(account.Name, out string _))
                {
                    _logger.Warning($"Warning: Account name '{account.Name}' is not valid and will be removed.");
                    isValid = false;
                }

                if (isValid)
                {
                    validAccounts.Add(account);
                }
            }

            accountsData.Accounts = validAccounts;

            //if (accountsData.Accounts.Count == 0)
            //{
            //    throw new BlumException("No valid accounts left after validation.");
            //}
        }

        public void ProcessAccountsData(ref AccountsData accountsData)
        {
            ValidateAccountsData(ref accountsData);

            SaveJsonFile(accountsData);

            List<Account> enabledAccounts = [.. accountsData.Accounts];

            foreach (var account in accountsData.Accounts)
            {
                if (!account.Enabled)
                {
                    _logger.Warning($"Warning: Account'{account.Name}' is disabled and will not be used.");
                    enabledAccounts.Remove(account);
                }
            }

            accountsData.Accounts.Clear();
            accountsData.Accounts.AddRange(enabledAccounts);
        }

        public static bool IsValidPhoneNumber(string phoneNumber, out string feedback)
        {
            if (PhoneNumberRegex().IsMatch(phoneNumber))
            {
                feedback = "Valid phone number.";
                return true;
            }

            var invalidMatches = InvalidPhoneNumberCharRegex().Matches(phoneNumber);
            if (invalidMatches.Count > 0)
            {
                var invalidChars = string.Join(", ", invalidMatches.Select(m => m.Value).Distinct());
                feedback = $"Invalid phone number. Disallowed characters: {invalidChars}";
            }
            else
            {
                feedback = "Invalid phone number format.";
            }

            return false;
        }

        public static bool IsValidSessionName(string sessionName, out string feedback)
        {
            if (SessionNameRegex().IsMatch(sessionName))
            {
                feedback = "Valid session name.";
                return true;
            }

            var invalidMatches = InvalidSessionNameCharRegex().Matches(sessionName);
            if (invalidMatches.Count > 0)
            {
                var invalidChars = string.Join(", ", invalidMatches.Select(m => m.Value).Distinct());
                feedback = $"Invalid session name. Disallowed characters: {invalidChars}";
            }
            else
            {
                feedback = "Invalid session name format.";
            }

            return false;
        }


        [GeneratedRegex(@"^[a-zA-Z0-9_]+$")]
        private static partial Regex SessionNameRegex();

        [GeneratedRegex(@"[^a-zA-Z0-9_]")]
        private static partial Regex InvalidSessionNameCharRegex();

        [GeneratedRegex(@"^\+?\d+$")]
        private static partial Regex PhoneNumberRegex();

        [GeneratedRegex(@"[^+\d]")]
        private static partial Regex InvalidPhoneNumberCharRegex();
    }
}
