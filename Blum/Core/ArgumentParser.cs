using Blum.Exceptions;
using Blum.Models;
using Blum.Services;
using Blum.Utilities;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using static Blum.Models.AppFilepaths;

namespace Blum.Core
{
    internal class ArgumentParser
    {
        private static readonly LoggerWithoutDateInOutput logger = new();

        public static async Task ParseArgs(string[] args)
        {
            var apiIdOption = new Option<string>(
            name: "--api-id",
            description: $"Sets the API ID for this session. Can be parsed from '{ConfigPath}'.",
            getDefaultValue: () => string.Empty
            )
            {
                IsRequired = false
            };

            var apiHashOption = new Option<string>(
                name: "--api-hash",
                description: $"Sets the API hash for this session. Can be parsed from '{ConfigPath}'.",
                getDefaultValue: () => string.Empty
            )
            {
                IsRequired = false
            };

            apiIdOption.AddValidator(result =>
            {
                var value = result.GetValueOrDefault<string>();

                if (!result.IsImplicit)
                {
                    if (string.IsNullOrEmpty(value) || !TelegramSettings.IsValidApiId(value))
                    {
                        result.ErrorMessage = $"The provided API ID '{value}' is not valid.";
                        return;
                    }
                    TelegramSettings.ApiId = value;
                    logger.Info($"API ID set to: '{TelegramSettings.ApiId}'");
                }
            });

            apiHashOption.AddValidator(result =>
            {
                var value = result.GetValueOrDefault<string>();

                if (!result.IsImplicit)
                {
                    if (!TelegramSettings.IsValidApiHash(value))
                    {
                        result.ErrorMessage = $"The provided API hash '{value}' is not valid.";
                        return;
                    }
                    TelegramSettings.ApiHash = value;
                    logger.Info($"API Hash set to '{TelegramSettings.ApiHash}'");
                }
            });

            var maxPlaysOption = new Option<int>(
            name: "--max-plays",
            description: $"Sets the max passes amount used for playing games. Can be parsed from '{ConfigPath}'.",
            getDefaultValue: () => TelegramSettings.MaxPlays)
            {
                IsRequired = false
            };

            maxPlaysOption.AddValidator(result =>
            {
                if (!result.IsImplicit)
                {
                    var value = result.GetValueOrDefault<int>();
                    if (!TelegramSettings.IsValidMaxPlays(value))
                    {
                        result.ErrorMessage = $"The provided max plays '{value}' is not valid. It must be non-negative number: [0, {int.MaxValue}]";
                        return;
                    }
                    TelegramSettings.MaxPlays = value;
                    logger.Info($"Max plays is set to: '{TelegramSettings.MaxPlays}'");
                }
            });

            var rootCommand = new RootCommand("Blum Core Command Line Interface")
            {
                apiIdOption,
                apiHashOption,
                maxPlaysOption,
                CreateConfigCommand(),
                AddAccountCommand(),
                ShowAccountsCommand(),
                DeleteAccountCommand(),
                UseAccount(),
                StartFarmCommand()
            };

            var parser = new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .UseHelp(ctx =>
                {
                    ctx.HelpBuilder.CustomizeLayout(
                        _ =>
                        {
                            var defaultLayout = HelpBuilder.Default.GetLayout().ToList();

                            if (ctx.Command.Name == rootCommand.Name ||
                                ctx.Command.Name == AddAccountCommand().Name ||
                                ctx.Command.Name == DeleteAccountCommand().Name ||
                                ctx.Command.Name == ShowAccountsCommand().Name ||
                                ctx.Command.Name == UseAccount().Name)
                            {
                                defaultLayout.Add(ctx =>
                                {
                                    ctx.Output.WriteLine();
                                    ctx.Output.WriteLine("Notes:");
                                    ctx.Output.WriteLine($"- The commands {AddAccountCommand().Name}, {DeleteAccountCommand().Name}, {ShowAccountsCommand().Name} and {UseAccount().Name} require valid API credentials.");
                                    ctx.Output.WriteLine("- Use --api-id and --api-hash to specify the API ID and hash for this session.");
                                    ctx.Output.WriteLine("- If no parameters are provided, the program will attempt to start farming with the existing configuration.");
                                });
                            }

                            if (ctx.Command.Name == StartFarmCommand().Name)
                            {
                                defaultLayout.Add(ctx =>
                                {
                                    ctx.Output.WriteLine();
                                    ctx.Output.WriteLine("Notes:");
                                    ctx.Output.WriteLine($"- This command requires valid API credentials.");
                                    ctx.Output.WriteLine("- Use --api-id and --api-hash to specify the API ID and hash for this session.");
                                    ctx.Output.WriteLine("- If no parameters are provided, the program will attempt to start farming with the existing configuration.");
                                });
                            }

                            return defaultLayout;
                        });
                })
                .Build();

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
                description: $"Sets the API ID for this session. Can be parsed from '{ConfigPath}' and will be saved to same path."
            )
            {
                IsRequired = false
            };

            var apiHashOption = new Option<string>(
                name: "--api-hash",
                description: $"Sets the API hash for this session. Can be parsed from '{ConfigPath}' and will be saved to same path."
            )
            {
                IsRequired = false
            };

            var command = new Command("create-config", $"Generates an configuration file ({ConfigPath}), if it's not exists, with provided API ID and Hash. If none or only one of them provided, will be generated empty file")
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
                    if (apiIdProvided)
                    {
                        if (!TelegramSettings.IsValidApiId(apiId))
                        {
                            logger.Warning($"The provided API ID '{apiId}' is not valid.");
                        }
                        else
                        {
                            TelegramSettings.ApiId = apiId;
                            logger.Info($"API ID set to '{TelegramSettings.ApiId}'");
                        }
                    }

                    if (apiHashProvided)
                    {
                        if (!TelegramSettings.IsValidApiHash(apiHash))
                        {
                            logger.Warning($"The provided API hash '{apiHash}' is not valid.");
                        }
                        else
                        {
                            TelegramSettings.ApiHash = apiHash;
                            logger.Info($"API Hash set to '{TelegramSettings.ApiHash}'");
                        }
                    }
                    HandleCreateConfig();
                    /*                    else
                                        {
                                            logger.Warning($"{ConfigPath} was not created as not both valid API ID and API Hash were passed. You have to specify them both.\n" +
                                                $"Pro Tip: if you want to change only one of the parameters, you can specify them before all commands using the global options --api-id and --api-hash");
                                        }*/
                }
                else
                {
                    logger.Info($"No parameters were provided. {ConfigPath} might be left unchanged or created empty");
                    /*                    logger.Info($"API ID remained unchanged: '{TelegramSettings.ApiId}'");
                                        logger.Info($"API Hash  remained unchanged: '{TelegramSettings.ApiHash}'");*/

                    HandleCreateConfig();
                }

            }, apiIdOption, apiHashOption);

            return command;
        }

        private static Command AddAccountCommand()
        {
            var command = new Command("add-account", "Adds an existing account with the provided details.");
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

        private static Command UseAccount()
        {
            var command = new Command("use-account", "Sets whether to use account or not");

            var accountNameOption = new Option<string>(
                "--account-name",
                description: "The name of the existing account"
            )
            {
                IsRequired = true
            };
            accountNameOption.AddAlias("--name");
            accountNameOption.AddAlias("-a");

            var useAccountFlagOption = new Option<bool>(
                "--use-account",
                description: "Flag to set whether to use the account or not",
                getDefaultValue: () => true
            )
            {
                IsRequired = false
            };
            useAccountFlagOption.AddAlias("--use");
            useAccountFlagOption.AddAlias("-u");

            command.AddOption(accountNameOption);
            command.AddOption(useAccountFlagOption);

            command.AddValidator(result =>
            {
                AccountService accountManager = new();
                var accounts = accountManager.GetAccounts().Accounts;

                if (accounts.Count == 0)
                {
                    result.ErrorMessage = "No valid accounts were found.";
                }

                var accountName = result.GetValueForOption(accountNameOption);

                if (accounts.Find((Account acc) => acc.Name == accountName) == null)
                {
                    Console.WriteLine($"There is no account with name '{accountName}'");
                }
            });

            command.SetHandler((string accountName, bool useAccount) =>
            {
                AccountService accountManager = new();
                string result = accountManager.DisableEnableAccount(accountName, useAccount);
                Console.WriteLine(result);
            }, accountNameOption, useAccountFlagOption);

            return command;
        }


        private static Command DeleteAccountCommand()
        {
            var command = new Command("delete-account", "Deletes an existing account.");
            command.SetHandler(() => DeleteAccount());
            return command;
        }

        private static Command ShowAccountsCommand()
        {
            var command = new Command("show-accounts", "Prints all existing accounts.");
            command.SetHandler(() => ShowAllAccounts());
            return command;
        }

        private static Command StartFarmCommand()
        {
            var command = new Command("start-farm", "Starts the farming process for all accounts in the configuration.");

            command.SetHandler(async () =>
            {
                if (!TelegramSettings.IsValidApiId(TelegramSettings.ApiId) || !TelegramSettings.IsValidApiHash(TelegramSettings.ApiHash))
                {
                    logger.Info("API settings are not fully configured. Please provide --api-id and --api-hash.");
                    return;
                }
                await HandleStartFarm();
            });

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
            await FarmingService.AutoStartBlumFarming();
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

                var accountManager = new AccountService();
                Console.WriteLine(accountManager.AddAccount(sessionName, phoneNumber));
            }
            catch (Exception ex)
            {
                PrintErrorAndExitWithCode(ex.Message, 1);
            }
        }

        private static void PrintAccounts(List<Account>? accounts)
        {
            if (accounts is null || accounts.Count == 0)
            {
                logger.Info("No valid accounts were found.");
                return;
            }

            Console.WriteLine("\nCurrent accounts:");
            foreach (var account in accounts)
            {
                string phoneNumber = account.PhoneNumber;
                string result = phoneNumber.Length > 9
                    ? phoneNumber[0..6] + new string('*', phoneNumber.Length - 6) + phoneNumber[^3..]
                    : phoneNumber;
                Console.WriteLine($"{account.Name}, {result}, enabled: {account.Enabled}");
            }
        }

        private static void DeleteAccount()
        {
            try
            {
                AccountService accountManager = new();
                var accounts = accountManager.GetAccounts().Accounts;

                PrintAccounts(accounts);

                Console.Write("Enter session name to be deleted: ");
                string sessionName = Console.ReadLine() ?? "";

                Console.WriteLine(accountManager.DeleteAccountByName(sessionName));
            }
            catch (Exception ex)
            {
                PrintErrorAndExitWithCode(ex.Message, 1);
            }
        }

        private static void ShowAllAccounts()
        {
            try
            {
                AccountService accountManager = new();
                var accounts = accountManager.GetAccounts().Accounts;

                PrintAccounts(accounts);
            }
            catch (Exception ex)
            {
                PrintErrorAndExitWithCode(ex.Message, 1);
            }
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
