using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using Blum.Exceptions;
using Blum.Models;
using Blum.Services;
using Blum.Utilities;

namespace Blum.Core
{
    internal class ArgumentParser
    {
        private class LoggerWithoutDateInOutput : Logger
        {
            override protected void Log(string message, LogMessageType type)
            {
                lock (_consoleLock)
                {
                    SetConsoleColor(logColors[type], () => _loggingAction(LogMessageTypeName[type]));

                    _loggingAction(" | ");
                    _loggingAction(message);
                    _loggingAction("\n");
                }
            }

            override protected void Log(LogMessageType type, string separator = " | ", params (string message, ConsoleColor? color)[] messages)
            {
                lock (_consoleLock)
                {
                    SetConsoleColor(logColors[type], () => _loggingAction(LogMessageTypeName[type]));
                    _loggingAction(separator);

                    for (int i = 0; i < messages.Length; i++)
                    {
                        var (message, color) = messages[i];
                        if (color.HasValue)
                            SetConsoleColor(color.Value, () => _loggingAction(message));
                        else
                            _loggingAction(message);

                        if (i < messages.Length - 1)
                            _loggingAction(separator);
                    }
                    _loggingAction("\n");
                }
            }
        }

        private static readonly LoggerWithoutDateInOutput logger = new();

        public static async Task ParseArgs(string[] args)
        {
            var apiIdOption = new Option<string>(
            name: "--api-id",
            description: $"Sets the API ID for this session. Can be parsed from '{TelegramSettings.configPath}'.",
            getDefaultValue: () => string.Empty
            )
            {
                IsRequired = false
            };

            var apiHashOption = new Option<string>(
                name: "--api-hash",
                description: $"Sets the API hash for this session. Can be parsed from '{TelegramSettings.configPath}'.",
                getDefaultValue: () => string.Empty
            )
            {
                IsRequired = false
            };

            apiIdOption.AddValidator(result =>
            {
                var value = result.GetValueOrDefault<string>();
                if (!string.IsNullOrEmpty(value) && !TelegramSettings.IsValidApiId(value))
                {
                    result.ErrorMessage = $"The provided API ID '{value}' is not valid.";
                }
                if (TelegramSettings.IsValidApiId(value ?? string.Empty))
                {
                    TelegramSettings.ApiId = value;
                    logger.Info($"API ID set to: '{TelegramSettings.ApiId}'");
                }
            });

            apiHashOption.AddValidator(result =>
            {
                var value = result.GetValueOrDefault<string>();
                if (!string.IsNullOrEmpty(value) && !TelegramSettings.IsValidApiHash(value))
                {
                    result.ErrorMessage = $"The provided API hash '{value}' is not valid.";
                }
                if (TelegramSettings.IsValidApiHash(value ?? string.Empty))
                {
                    TelegramSettings.ApiHash = value;
                    logger.Info($"API Hash set to '{TelegramSettings.ApiHash}'");
                }
            });

            var maxPlaysOption = new Option<int>(
            name: "--max-plays",
            description: $"Sets the max passes amount used for playing games. Can be parsed from '{TelegramSettings.configPath}'.",
            getDefaultValue: () => 7)
            {
                IsRequired = false
            };

            maxPlaysOption.AddValidator(result =>
            {
                var value = result.GetValueOrDefault<int>();
                if (!TelegramSettings.IsValidMaxPlays(value))
                {
                    result.ErrorMessage = $"The provided API ID '{value}' is not valid.";
                }
                if (TelegramSettings.IsValidMaxPlays(value))
                {
                    TelegramSettings.MaxPlays = value;
                    logger.Info($"API ID set to: '{TelegramSettings.ApiId}'");
                }
            });

            var rootCommand = new RootCommand("Blum Core Command Line Interface")
            {
                apiIdOption,
                apiHashOption,
                CreateConfigCommand(),
                AddAccountCommand(),
                DeleteAccountCommand(),
                StartFarmCommand(),
                ShowHelpCommand()
            };

            var builder = new CommandLineBuilder(rootCommand).UseDefaults();
            var parser = builder.Build();

            try
            {
                await parser.InvokeAsync(args);
            }
            catch (Exception ex)
            {
                PrintErrorAndExitWithCode($"An error occurred: {ex.Message}", 1);
            }
        }

        private static Command CreateConfigCommand()
        {
            var apiIdOption = new Option<string>(
                name: "--api-id",
                description: $"Sets the API ID for this session. Can be parsed from '{TelegramSettings.configPath}' and will be saved to same path."
            )
            {
                IsRequired = false
            };

            var apiHashOption = new Option<string>(
                name: "--api-hash",
                description: $"Sets the API hash for this session. Can be parsed from '{TelegramSettings.configPath}' and will be saved to same path."
            )
            {
                IsRequired = false
            };

            var command = new Command("--create-config", $"Generates an configuration file ({TelegramSettings.configPath}), if it's not exists, with provided API ID and Hash. If none or only one of them provided, will be generated empty file")
            {
                apiIdOption,
                apiHashOption
            };

            command.SetHandler((string apiId, string apiHash) =>
            {
                bool apiIdProvided = !string.IsNullOrEmpty(apiId);
                bool apiHashProvided = !string.IsNullOrEmpty(apiHash);

                if (apiIdProvided || apiHashProvided)
                {
                    short count = 0;
                    if (apiIdProvided)
                    {
                        if (!TelegramSettings.IsValidApiId(apiId))
                        {
                            logger.Warning($"The provided API ID '{apiId}' is not valid.");
                            return;
                        }
                        count++;
                    }

                    if (apiHashProvided)
                    {
                        if (!TelegramSettings.IsValidApiHash(apiHash))
                        {
                            logger.Warning($"The provided API hash '{apiHash}' is not valid.");
                            return;
                        }
                        count++;
                    }

                    if (count == 2)
                    {
                        TelegramSettings.ApiId = apiId;
                        logger.Info($"API ID set to '{TelegramSettings.ApiId}'");

                        TelegramSettings.ApiHash = apiHash;
                        logger.Info($"API Hash set to '{TelegramSettings.ApiHash}'");

                        HandleCreateConfig();
                    }
                    else
                    {
                        logger.Warning($"{TelegramSettings.configPath} was not created as not both valid API ID and API Hash were passed. You have to specify them both.\n" +
                            $"Pro Tip: if you want to change only one of the parameters, you can specify them before all commands using the global options --api-id and --api-hash");
                    }
                }
                else
                {
                    logger.Info($"No parameters were provided. {TelegramSettings.configPath} might be left unchanged or created empty");
                    logger.Info($"API ID remained unchanged: '{TelegramSettings.ApiId}'");
                    logger.Info($"API Hash  remained unchanged: '{TelegramSettings.ApiHash}'");

                    HandleCreateConfig();
                }

            }, apiIdOption, apiHashOption);

            return command;
        }

        private static Command AddAccountCommand()
        {
            var command = new Command("--add-account", "Adds an existing account with the provided details.");
            command.SetHandler(() =>
            {
                if (string.IsNullOrEmpty(TelegramSettings.ApiHash))
                {
                    logger.Info("API Hash is required. Please set it using --api-hash option.");
                    return;
                }
                AddAccount();
            });
            return command;
        }

        private static Command DeleteAccountCommand()
        {
            var command = new Command("--delete-account", "Deletes an existing account.");
            command.SetHandler(() => DeleteAccount());
            return command;
        }

        private static Command StartFarmCommand()
        {
            var command = new Command("--start-farm", "Starts the farming process for all accounts in the configuration.");

            command.SetHandler(async () =>
            {
                if (string.IsNullOrWhiteSpace(TelegramSettings.ApiId) || string.IsNullOrWhiteSpace(TelegramSettings.ApiHash))
                {
                    logger.Info("API settings are not fully configured. Please provide --api-id and --api-hash.");
                    return;
                }
                await HandleStartFarm();
            });

            return command;
        }

        private static Command ShowHelpCommand()
        {
            var command = new Command("--help", "Shows the help text.");
            command.SetHandler(() => ShowHelp());
            return command;
        }

        private static void HandleCreateConfig()
        {
            try
            {
                string? accountDataFilepath = TelegramSettings.CreateConfigFileWithCurrentSettings();
                logger.Info($"Config created as {accountDataFilepath}");
            }
            catch (TypeInitializationException ex) when (ex.InnerException is not BlumException)
            {
                logger.Error((ex.Message, null), (ex.InnerException?.Message ?? "", null));
            }
            catch (BlumException ex)
            {
                PrintErrorAndExitWithCode(ex.Message, 1);
            }
            catch (Exception ex)
            {
                PrintErrorAndExitWithCode(ex.Message, 1);
            }
        }

        private static async Task HandleStartFarm()
        {
            try
            {
                TelegramSettings.ParseConfig();
                await FarmingService.AutoStartBlumFarming();
            }
            catch (BlumException ex)
            {
                logger.Error($"{ex.Message} Restart the program with valid config values");
            }
        }

        private static void AddAccount()
        {
            try
            {
                if (!TelegramSettings.IsValidApiHash(TelegramSettings.ApiHash))
                {
                    logger.Error($"'{TelegramSettings.ApiHash}' is not a valid api_hash.");
                    return;
                }

                Console.Write("Enter session name (only letters, numbers, and underscores are allowed): ");
                string sessionName = Console.ReadLine() ?? "";

                if (!AccountService.IsValidSessionName(sessionName, out string feedback))
                {
                    logger.Error(feedback);
                    return;
                }
                feedback = "";

                Console.Write("Enter phone number: ");
                string phoneNumber = Console.ReadLine() ?? "";
                if (!AccountService.IsValidPhoneNumber(phoneNumber, out feedback))
                {
                    logger.Error(feedback);
                    return;
                }

                var aes = new Encryption(TelegramSettings.ApiHash);
                var accountManager = new AccountService(aes);
                Console.WriteLine(accountManager.AddAccount(sessionName, phoneNumber));
            }
            catch (Exception ex)
            {
                PrintErrorAndExitWithCode(ex.Message, 1);
            }
        }

        private static void DeleteAccount()
        {
            try
            {
                var aes = new Encryption(TelegramSettings.ApiHash);
                AccountService accountManager = new(aes);
                var accounts = accountManager.GetAccounts().Accounts;

                if (accounts.Count == 0)
                {
                    logger.Info("No valid accounts were found.");
                    return;
                }

                Console.WriteLine("\nCurrent accounts:");
                foreach (var account in accounts)
                {
                    string input = account.PhoneNumber;
                    string result = input.Length > 3
                        ? new string('*', input.Length - 3) + input[^3..]
                        : input;
                    Console.WriteLine($"{account.SessionName}, {result}");
                }
                Console.Write("Enter session name to be deleted: ");
                string sessionName = Console.ReadLine() ?? "";

                Account? foundAccount = accounts.Find((Account x) => x.SessionName == sessionName);
                if (foundAccount != null)
                {
                    string result = accountManager.DeleteAccount(foundAccount.SessionName ?? "", foundAccount.PhoneNumber ?? "");
                    Console.WriteLine(result);
                }
            }
            catch (Exception ex)
            {
                PrintErrorAndExitWithCode(ex.Message, 1);
            }
        }

        private static void ShowHelp()
        {
            var processName = Process.GetCurrentProcess().ProcessName;
            Console.WriteLine($"""
            Usage: {processName} [options]

            Options:
              --create-config        Generates an empty configuration file.
              --add-account          Adds an existing account. Requires valid api_id and api_hash credentials.
              --delete-account       Deletes an existing account.
              --start-farm           Starts the farming process for all accounts in the configuration.

            Notes:
            - The options --add-account and --delete-account require valid api_hash credentials.
            - Use --api-id and --api-hash to specify the API ID and hash for this session.
            - If no parameters are provided, the program will attempt to start farming with the existing configuration.
            """);
        }

        private static void PrintErrorAndExitWithCode(string message, int code)
        {
            logger.Error($"Unexpected error occurred: {message}");
            Console.WriteLine("Press any key to exit program...");
            Console.ReadKey();
            Environment.Exit(code);
        }
    }
}
