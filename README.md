# MaxBot CLI

An interactive command-line chat application that leverages multiple AI providers to deliver conversational AI capabilities. This application features streaming responses, support for multiple API providers, and AOT compilation for optimal performance.

## Features

- Support both Oneshot and Interactive Chat interface with streaming AI responses
- Supports only OpenAI-compatible APIs
- Profile-based configuration for easy switching between providers and models
- Cross-platform support (Windows, MacOS, Linux)

## Prerequisites

Before running the application, ensure you have:

- Access to at least one supported API provider
- Configuration file set up with your API providers and profiles

## Setup

1. In your home directory, create a configuration file (`maxbot.config.json`) with your API provider details:

   ```json
   {
       "maxbotConfig": {
           "apiProviders": [
               {
                    "name": "MyCompanyProvider",
                    "type": "OpenAI-Compatible",
                    "apiKey": "example-key",
                    "baseUrl": "https://litellm.mycompany.com"
                },
                {
                    "name": "RequestyAI",
                    "type": "OpenAI-Compatible",
                    "apiKey": "example-key",
                    "baseUrl": "https://router.requesty.ai/v1"
                },
                {
                    "name": "Deepseek",
                    "type": "OpenAI-Compatible",
                    "apiKey": "example-key",
                    "baseUrl": "https://api.deepseek.com"
                }
           ],
           "profiles": [
               {
                   "default": true,
                   "name": "Default",
                   "apiProvider": "MyCompanyProvider",
                   "modelId": "03-mini"
               },
               {
                   "name": "Sonnet",
                   "apiProvider": "RequestyAI",
                   "modelId": "vertex/anthropic/claude-3-7-sonnet"
               },
               {
                    "name": "R1",
                    "apiProvider": "Deepseek",
                    "modelId": "deepseek-reasoner"
                },
                {
                    "name": "V3",
                    "apiProvider": "Deepseek",
                    "modelId": "deepseek-chat"
                }
           ]
       }
   }
   ```

## Usage

```bash
maxbot [options]
```

### Options

- `-h, --help`: Show help message
- `-c, --config <path>`: Specify a custom configuration file path (default: maxbot.config.json)
- `-p, --profile <name>`: Specify a profile name to use (overrides default profile in config)

### Examples

```bash
maxbot                                                # Start a chat using ~/maxbot.config.json and its default profile
maxbot -m oneshot -u "Which is the tallest pokemon?"  # Ask a oneshot question using ~/maxbot.config.json and its default profile
maxbot -p "Sonnet"                                    # Start a chat using local ./maxbot.config.json and the Sonnet profile
maxbot -c custom-config.json -p "R1"                  # Start a chat using custom-config.json and the R1 profile
```

### Chat Interface

- Start typing your messages after the `🤖 %` prompt
- AI responses will stream in real-time with green text
- Exit the chat by typing `exit`, `quit`, or pressing Enter with no message

### Configuration

The application uses a JSON configuration file with the following structure:

- **apiProviders**: List of available API providers
  - **name**: Unique identifier for the provider
  - **type**: Provider type (OpenAI-Compatible or Anthropic)
  - **apiKey**: Your API key for the provider
  - **baseUrl**: Base URL for the API (for OpenAI-Compatible providers)

- **profiles**: List of available profiles
  - **default**: Whether this is the default profile (true/false)
  - **name**: Profile name
  - **apiProvider**: Name of the API provider to use (must match a provider name)
  - **modelId**: Model ID to use for chat completion

## AOT Compilation

MaxBot supports AOT (Ahead-of-Time) compilation for improved performance:

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## Exit

To exit the application:

- Type `exit` or `quit`
- Press Enter with an empty message
- The application will cleanly terminate

