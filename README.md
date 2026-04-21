# AI-Powered .NET MAUI Chat App with LiteLLM and Syncfusion AI AssistView

This repository demonstrates how to build an AI-powered chat experience
in .NET MAUI by integrating [Syncfusion AI AssistView](https://www.syncfusion.com/maui-controls/maui-aiassistview)
with [LiteLLM](https://github.com/BerriAI/litellm) as a local AI proxy.
It showcases multi-model switching, Markdown response rendering, and a
clean MVVM architecture backed by Azure OpenAI.

---

## Key Highlights

- Seamless LiteLLM proxy + AI AssistView integration
- Single OpenAI-compatible endpoint for multiple AI providers
- Runtime model switching via a Picker control
- Rich Markdown-to-HTML response rendering using Markdig
- Platform-aware endpoint configuration: `localhost` on Windows, `10.0.2.2` on Android
- Extensible, MVVM-friendly architecture

---

## Features

This sample application showcases an AI-powered chat assistant with the
following capabilities:

- **Multi-Model Support** — Switch between AI models at runtime without
  changing any code:
  - `azure-gpt-4.1` (Azure OpenAI)
  - `gpt-4.1-mini` (Azure OpenAI)
- **LiteLLM Proxy** — A single local endpoint routes all requests to the
  configured provider, keeping your app provider-agnostic.
- **Rich Response Rendering** — AI responses are converted from Markdown
  to styled HTML and displayed inside the AssistView using a WebView.
- **Error and Timeout Handling** — Graceful error messages when the proxy
  is unreachable or the request times out.
- **Accept Flow** — Rendered responses are selectable and copyable within
  the AssistView.

---

## Technologies Used

- **.NET MAUI** – Cross-platform framework for Android, iOS, macOS, and Windows
- **Syncfusion AI AssistView** – AI chat UI control
- **LiteLLM** – Python-based proxy server for multi-provider AI routing
- **Markdig** – Markdown-to-HTML conversion for rich response rendering
- **Azure OpenAI Services** – Backend AI processing

---

## Prerequisites

- .NET SDK 8.0 or later (compatible with .NET MAUI)
- Visual Studio 2022 with the .NET MAUI workload installed
- Python 3.8 or later (for running the LiteLLM proxy)
- Syncfusion NuGet packages:
  - `Syncfusion.Maui.AIAssistView`
  - `Syncfusion.Maui.Core`
- Azure OpenAI account with:
  - API endpoint
  - API key
  - Deployment/model name

> **Note:** Syncfusion components may require a license key.

---

## Setup Instructions

### 1. Clone the Repository

```bash
git clone https://github.com/your-org/AssistViewLiteLLMSample.git
cd AssistViewLiteLLMSample
```

### 2. Install NuGet Packages

Ensure all required Syncfusion and supporting packages are installed:

```bash
dotnet restore
```

| Package | Version |
|---|---|
| `Syncfusion.Maui.AIAssistView` | 31.2.15 |
| `Markdig` | 0.37.0 |
| `Microsoft.Maui.Controls` | `$(MauiVersion)` |
| `Microsoft.Extensions.Logging.Debug` | 10.0.0 |

### 3. Set Up LiteLLM

Open a PowerShell terminal and run:

```powershell
# Create and activate a virtual environment
py -3.11 -m venv .venv
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned -Force
.\.venv\Scripts\Activate.ps1

# Install LiteLLM with proxy support
python -m pip install -U pip
python -m pip install "litellm[proxy]"

# Verify installation
litellm --version
```

### 4. Configure Azure OpenAI

Open `litellm_config.yaml` at the root of the solution and replace the
placeholder values with your actual Azure OpenAI credentials:

```yaml
model_list:
  - model_name: azure-gpt-4.1
    litellm_params:
      model: "azure/gpt-4.1"
      api_base: "YOUR_AZURE_API_BASE"
      api_key: "YOUR_AZURE_API_KEY"
      api_version: "YOUR_API_VERSION"

  - model_name: gpt-4.1-mini
    litellm_params:
      model: "azure/gpt-4.1-mini"
      api_base: "YOUR_AZURE_API_BASE"
      api_key: "YOUR_AZURE_API_KEY"
      api_version: "YOUR_API_VERSION"

litellm_settings:
  enable_router_logging: true
```

> ⚠️ **Never commit real API keys to source control.**
> Use environment variables or a secrets manager for production use.

---

## How It Works

### Navigation Flow

- **Windows / Desktop** — The AI AssistView loads inline on the main page
- **Android / iOS** — Uses platform-specific endpoint configuration
  (`http://10.0.2.2:4000`) to reach the host machine proxy

### Request Processing

1. User types a query in the AssistView input box and presses **Send**
2. `RequestCommand` in `AssistViewViewModel` handles the event
3. The ViewModel posts the query to `http://localhost:4000/v1/chat/completions`
   (or `http://10.0.2.2:4000` on Android)
4. LiteLLM routes the request to the model selected in the Picker
5. The JSON response is parsed and the content is extracted from
   `choices[0].message.content`
6. Markdig converts the Markdown response to styled HTML
7. The formatted response is added to `AssistItems` and rendered in the
   AssistView

### Model Switching

Changing the selected model in the Picker updates `SelectedModel` in the
ViewModel. The next request automatically uses the new model alias — no
restart required.

---

## Project Structure

```
AssistViewLiteLLMSample/
├── Models/
│   └── ExtendedAssistItem.cs       # Extends AssistItem with HtmlText property
├── ViewModels/
│   └── AssistViewModel.cs          # Core MVVM logic, API calls, response formatting
├── Views/
│   └── MainPage.xaml               # XAML layout with AssistView and model Picker
│   └── MainPage.xaml.cs
├── Resources/
│   └── Styles/                     # App styles and item templates
├── litellm_config.yaml             # LiteLLM model and provider configuration
├── AssistViewLiteLLMSample.csproj
└── README.md
```

---

## Platform Notes

| Platform | Proxy Endpoint |
|---|---|
| Windows / Desktop | `http://localhost:4000` |
| Android Emulator | `http://10.0.2.2:4000` |
| iOS Simulator | `http://localhost:4000` |

The project handles this automatically using conditional compilation:

```csharp
#if ANDROID
    private const string LiteLLMEndpoint = "http://10.0.2.2:4000/v1/chat/completions";
#else
    private const string LiteLLMEndpoint = "http://localhost:4000/v1/chat/completions";
#endif
```

---

## Troubleshooting

### No AI Response

- Verify that the LiteLLM proxy is running:
  `litellm --config litellm_config.yaml --port 4000`
- Check that `litellm_config.yaml` contains valid Azure OpenAI credentials
- Confirm network connectivity from your device or emulator to the host machine
- Review exception handling in `GetResponseFromLiteLLMAsync` for logged errors

### Responses Not Appearing in AssistView

- Confirm `AssistItems` binding is set correctly in XAML
- Verify `RequestCommand` is bound to `AssistViewRequestCommand` in the ViewModel
- Check that `ExtendedAssistItem` is being added to the `AssistItems` collection
  on the main thread via `MainThread.InvokeOnMainThreadAsync`

### Android Emulator Cannot Reach Proxy

- Ensure you are using `http://10.0.2.2:4000` and not `localhost`
- Confirm the LiteLLM proxy is bound to `0.0.0.0` (default behavior)
- Check that no firewall rule is blocking port `4000`

### Path Too Long Exception

If you encounter a path too long exception when building on Windows,
close Visual Studio, rename the repository folder to a shorter name,
then rebuild the project.

---

## Screenshot

> _(Add screenshots of the running app here)_

| Windows | Android |
|---|---|
| _(screenshot)_ | _(screenshot)_ |

---

## Related Blog Post

For a full step-by-step walkthrough, refer to:
[Integrate LiteLLM with Syncfusion .NET MAUI AI AssistView — One Proxy, Any Model, Clean UI](https://www.syncfusion.com/blogs)

---

## Documentation

- [Syncfusion .NET MAUI AI AssistView Documentation](https://help.syncfusion.com/maui/aiassistview/overview)
- [LiteLLM Documentation](https://docs.litellm.ai)
- [Azure OpenAI Service Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [.NET MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)

---

## Support

- 💬 [Syncfusion Support Portal](https://support.syncfusion.com)
- 🐛 [LiteLLM Issues](https://github.com/BerriAI/litellm/issues)
- 📖 [Syncfusion Community Forums](https://www.syncfusion.com/forums)
- 📧 For questions about this sample, open a GitHub Issue in this repository

---

## License

This project is licensed under the **MIT License**.
See [LICENSE](./LICENSE) for details.

Syncfusion controls are subject to the
[Syncfusion License](https://www.syncfusion.com/sales/licensing).
A free community license is available for qualifying individuals and
small businesses.