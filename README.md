# Blum Core Command Line Interface

The Blum Core CLI is a command-line tool for managing and interacting with the Blum farming service. It allows you to configure API credentials, manage accounts, and start the farming process.

## Table of Contents
- [Quickstart](#quickstart)
- [Installation](#installation)
- [Usage](#usage)
  - [Global Options](#global-options)
  - [Commands](#commands)
    - [Create Config](#create-config)
    - [Add Account](#add-account)
    - [Delete Account](#delete-account)
    - [Start Farm](#start-farm)
    - [Help](#help)
- [Examples](#examples)
- [Error Handling](#error-handling)

## Quickstart

1. [Install](#Installation) the package.

2. Open cmd in folder `Blum.exe` is located.
> [!TIP]\
> You can type `cmd` in windows file explorer instead of folder path to open cmd in this folder.

3. Run
```Bash
Blum.exe --create-config
```
After config file is created, edit it with your api_id and api_hash from [my.telegram.org](https://my.telegram.org)

4. Run
```Bash
Blum.exe --add-account
```
This will add an telegram account.

> [!IMPORTANT]\
> Before adding an account, you need to add api_hash to the configuration file or enter it BEFORE calling the `--add-account` using `--api-hash <api-hash>` as it's required for encryption. This also means that after adding a session, you should not change the api_hash to avoid data loss.

5. Now, ypu can simply run the `Blum.exe` or `Blum.exe --start-farm`, this will

## Installation

The Blum Core CLI can be used with [dotnet](https://dotnet.microsoft.com/en-us/download/dotnet/8.0). Download framework-dependent version and make sure you have .NET 8 installed.
The Blum Core CLI is also built as a self-contained application, meaning you don't need to have the .NET runtime installed on your machine. Follow the steps below to install and use the CLI.

1. **Download the CLI:**
   - Navigate to the release section of this repository and download the appropriate version.

2. **Extract the package:**
   - Unzip the downloaded file to a directory of your choice.

## Usage

### Global Options

- `--api-id`: Sets the API ID for the session. If not provided, it attempts to use the value from the configuration file.
- `--api-hash`: Sets the API hash for the session. If not provided, it attempts to use the value from the configuration file.

These options can be provided with any command and are useful for overriding the configuration file values.

### Commands

#### Create Config

Generates a configuration file with the provided API ID and hash. If only one or neither of these values are provided, an empty or partial configuration file is created.

Usage:
```bash
Blum.exe --create-config [--api-id <API_ID>] [--api-hash <API_HASH>]
```

#### Add Account

Adds an account to the configuration. This command requires that a valid `api_hash` is set either through the global option or within the configuration file.

Usage:
```bash
Blum.exe --add-account
```

#### Delete Account

Deletes an account from the configuration.

Usage:
```bash
Blum.exe --delete-account
```

#### Start Farm

Starts the farming process for all configured accounts. Ensure that both `api_id` and `api_hash` are set before running this command.

Usage:
```bash
Blum.exe --start-farm
```

#### Help

Displays the help text, which provides usage instructions for all commands.

Usage:
```bash
Blum.exe --help
```

## Examples

1. **Creating a configuration file:**
   ```bash
   Blum.exe --create-config --api-id your_api_id --api-hash your_api_hash
   ```
   
  or
  
  ```bash
   Blum.exe --create-config
   ```
This will create empty config file, or with existing settings, if they are valid.

2. **Adding a new account:**
   ```bash
   Blum.exe --add-account --api-hash your_api_hash
   ```

3. **Starting the farming process:**
   ```bash
   Blum.exe --start-farm
   ```

## Error Handling

If an error occurs during the execution of any command, the tool will display an error message but won't exit if error occurred while farming

Common issues include:
- Invalid API credentials: Ensure the `api_id` and `api_hash` are correct.
- Missing configuration: Make sure to run [`--create-config`](#create-config) before attempting to add accounts or start farming.

For any unexpected errors, the program will prompt you to press any key to exit.
