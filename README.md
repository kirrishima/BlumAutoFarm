# Blum Core Command Line Interface

The Blum Core CLI is a command-line tool for managing and interacting with the Blum farming service. It allows you to configure API credentials, manage accounts, and start the farming process.

## Table of Contents
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

## Installation

The Blum Core CLI is built as a self-contained application, meaning you don't need to have the .NET runtime installed on your machine. Follow the steps below to install and use the CLI.

1. **Download the CLI:**
   - Navigate to the release section of this repository and download the appropriate version for your operating system.

2. **Extract the package:**
   - Unzip the downloaded file to a directory of your choice.

3. **Run the CLI:**
   - Open a terminal (or command prompt) and navigate to the directory where you extracted the files.

   - Run the CLI tool by typing:
     ```bash
     ./blum-core --help  # On Linux or macOS
     blum-core.exe --help  # On Windows
     ```

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
./blum-core --create-config [--api-id <API_ID>] [--api-hash <API_HASH>]
```

#### Add Account

Adds an account to the configuration. This command requires that a valid `api_hash` is set either through the global option or within the configuration file.

Usage:
```bash
./blum-core --add-account
```

#### Delete Account

Deletes an account from the configuration.

Usage:
```bash
./blum-core --delete-account
```

#### Start Farm

Starts the farming process for all configured accounts. Ensure that both `api_id` and `api_hash` are set before running this command.

Usage:
```bash
./blum-core --start-farm
```

#### Help

Displays the help text, which provides usage instructions for all commands.

Usage:
```bash
./blum-core --help
```

## Examples

1. **Creating a configuration file:**
   ```bash
   ./blum-core --create-config --api-id your_api_id --api-hash your_api_hash
   ```

2. **Adding a new account:**
   ```bash
   ./blum-core --add-account --api-hash your_api_hash
   ```

3. **Starting the farming process:**
   ```bash
   ./blum-core --start-farm
   ```

## Error Handling

If an error occurs during the execution of any command, the tool will display an error message but won't exit if error occurred while farming

Common issues include:
- Invalid API credentials: Ensure the `api_id` and `api_hash` are correct.
- Missing configuration: Make sure to run [`--create-config`](#create-config) before attempting to add accounts or start farming.

For any unexpected errors, the program will prompt you to press any key to exit.
