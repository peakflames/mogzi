# MaxBot CLI 🤖✨

An interactive command-line chat application that leverages multiple AI providers to deliver conversational AI capabilities. This application features streaming responses, support for multiple API providers, and AOT compilation for optimal performance. 🚀

## Features 🌟

- LLM can read and write files as well obtain directory listings to learn about your repository 📝
- Support both Oneshot and Interactive Chat interface with streaming AI responses 💬
- Supports only OpenAI-compatible APIs 🧠
- Profile-based configuration for easy switching between providers and models 🔄
- Cross-platform support (Windows, MacOS, Linux) 💻🍎🐧

## Prerequisites ✅

Before running the application, ensure you have:

- Access to at least one supported API provider 🔑
- Configuration file set up with your API providers and profiles ⚙️

## Setup 🛠️

1. Download the latest release from the [Releases](https://github.com/tschavey/MaxBot/releases) page.

   For Windows:

   - Download loads max.exe and moves it to your WindowsApp directory
   ```sh
   Start-BitsTransfer -Source https://github.com/peakflames/maxbot/releases/latest/download/max-win-x64.exe -Destination max.exe; move -Force max.exe $env:USERPROFILE\AppData\Local\Microsoft\WindowsApps
   ```

    For MacOS (sudo):

    - Download loads max and moves it to your `/usr/local/bin`
  
    ```sh
    sudo curl -sLO https://github.com/peakflames/maxbot/releases/latest/download/max-osx-x64 -o max && chmod +x max && mv max /usr/local/bin && rm ./max
    ```

    For Linux (sudo)

    - Download loads max and moves it to your `/usr/local/bin`

    ```sh
    sudo curl -L https://github.com/peakflames/maxbot/releases/latest/download/max-linux-x64 -o max && sudo chmod +x max && sudo mv -f max /usr/local/bin
    ```

2. In your home directory, create a configuration file (`maxbot.config.json`) with your API provider details:

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
                    "name": "Gemini",
                    "apiProvider": "RequestyAI",
                    "modelId": "coding/gemini-2.5-pro"
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

## Usage 📝

```bash
max [prompt] [options]
```

To start a chat session, use the `--chat` option:

```bash
max --chat [options]
```

### Examples 💡

```bash
# oneshot request
max "Which is the tallest pokemon?"

# start full chat using your 'Sonnet' profile
max --chat -p Sonnet

# translate a README.md to portugese
cat README.md | max "Translate to portugese"
```


### Chat Interface 💬

- Start typing your messages after the `🤖 %` prompt
- AI responses will stream in real-time with green text ✨
- Exit the chat by typing `exit`, `quit`, or pressing Enter with no message 👋

### Configuration ⚙️

The application uses a JSON configuration file with the following structure:

- **apiProviders**: List of available API providers 🏢
  - **name**: Unique identifier for the provider
  - **type**: Provider type (OpenAI-Compatible at this time, more to come)
  - **apiKey**: Your API key for the provider 🔑
  - **baseUrl**: Base URL for the API (for OpenAI-Compatible providers)

- **profiles**: List of available profiles 👤
  - **default**: Whether this is the default profile (true/false)
  - **name**: Profile name
  - **apiProvider**: Name of the API provider to use (must match a provider name)
  - **modelId**: Model ID to use for chat completion

## Exit 👋

To exit the application:

- Type `exit` or `quit`
- Press Enter with an empty message
- The application will cleanly terminate

## Contributing 🤝

Contributions are welcome! Please read the [developer guidelines](.clinerules/developer_guidelines.md) for more information on how to build the project and run tests.
