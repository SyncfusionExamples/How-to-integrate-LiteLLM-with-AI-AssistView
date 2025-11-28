# How-to-integrate-LiteLLM-with-AI-AssistView
This demo shows how to integrate  **LiteLLM** in **Syncfusion® .NET MAUI AI AssistView**

## Introduction
Imagine building a .NET MAUI app where users expect intelligent responses without delays or complexity. You want to integrate AI, but managing multiple models and ensuring a seamless, responsive UI can feel overwhelming.
Enter **LiteLLM** and **Syncfusion® .NET MAUI AI AssistView** — a powerful duo that turns complexity into simplicity.

LiteLLM acts as a lightweight proxy for multiple AI models like OpenAI, Azure OpenAI, and Anthropic, requiring minimal configuration. Meanwhile, AI AssistView provides an interactive, user-friendly interface for handling queries. Together, they enable dynamic, AI-powered experiences across all your app’s supported platforms—delivering scalability, security, and responsiveness without the headaches.

In this blog, we will guide you through integrating LiteLLM in AI AssistView step by step, ensuring a smooth setup and optimal performance.
When combined with AI AssistView, LiteLLM enables:
- **Multi-model flexibility:** Switch between AI providers effortlessly.
- **Secure API calls:** Centralized configuration for keys and endpoints.
- **Enhanced user experience:** Real-time, contextual responses in your MAUI app.
- **Scalability:** Manage multiple requests efficiently without performance issues.



## Integrating LiteLLM with the .NET MAUI app
### Install LiteLLM

LiteLLM acts as a middleware for AI models. Install it using pip:

$ pip install litellm

Verify installation:

$ litellm --version
  
### Create a configuration file
Create a configuraton file: litellm_config.yaml


Content:

---
model_list:
  - model_name: YOUR_MODEL_NAME
    litellm_params:
      model: "YOUR_MODEL"
      api_base: "YOUR_API_BASE"
      api_key: "YOUR_AZURE_API_KEY_HERE"
      api_version: "API_VERSION"

router_settings:
  enable_router_logging: true

debug: true


---



### Start LiteLLM Proxy
Open a new PowerShell window and run the proxy to route requests:
  $ cd "CONFIGURATION_FILE_LOCATION"
  $ litellm --config "litellm_config.yaml" --port 4000

Keep this terminal window open during development.

After you run the command, you should see messages like:
  INFO:     Application startup complete.
  INFO:     Uvicorn running

 

## Integrate LiteLLM with AI AssistView 

### Handle AssistView Request
Use the following C# method to capture the user query from AI AssistView:

```
    /// <summary>
    /// Handles the AssistView request event when user submits a query.
    /// </summary>
    private async void ExecuteRequestCommand(object obj)
    {
        if (obj is RequestEventArgs args && args.RequestItem is IAssistItem request)
        {
            await ProcessUserQuery(request);
        }
    }
```

### Process the User Query 
This method sends the query to LiteLLM, formats the response, and updates the UI:
```
    /// <summary>
    /// Processes a user query: calls API, formats response, updates UI.
    /// </summary>
    private async Task ProcessUserQuery(IAssistItem userQuery)
    {
        try
        {
            string responseText = await GetResponseFromLiteLLMAsync(userQuery.Text);

            string formattedText = CleanResponseText(responseText);

            string htmlContent = GenerateHtmlContent(formattedText);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                AssistItems.Add(new ExtendedAssistItem
                {
                    Text = formattedText,
                    HtmlText = htmlContent,
                    RequestItem = userQuery
                });
            });
        }
        catch (Exception ex)
        {
            // Display error in UI
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                AssistItems.Add(new ExtendedAssistItem
                {
                    Text = $"An error occurred: {ex.Message}",
                    HtmlText = $"<html><body><p style='color:#D32F2F;'>Error: {ex.Message}</p></body></html>",
                    RequestItem = userQuery
                });
            });
        }
    }
```
### Send Request to LiteLLM
```
    /// <summary>
    /// Sends request to LiteLLM proxy and retrieves AI response.
    /// Includes comprehensive error handling for various failure scenarios.
    /// </summary>
    private async Task<string> GetResponseFromLiteLLMAsync(string userPrompt)
    {
        try
        {
            var httpHandler = new HttpClientHandler();


            using var httpClient = new HttpClient(httpHandler)
            {
                Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds)
            };

            var requestPayload = new
            {
                model = ModelName,
                messages = new[]
                {
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.7,      // Balance between focused and creative
                max_tokens = 2048       // Maximum response length
            };

            .....

            // Send POST request to LiteLLM
            var httpResponse = await httpClient.PostAsync(LiteLLMEndpoint, httpContent);

            .....

        }
    }

```

## Requirements to run the demo
To run the demo, refer to [System Requirements for .NET MAUI](https://help.syncfusion.com/maui/system-requirements)

## Troubleshooting:
### Path too long exception
If you are facing path too long exception when building this example project, close Visual Studio and rename the repository to short and build the project.

## License
Syncfusion® has no liability for any damage or consequence that may arise from using or viewing the samples. The samples are for demonstrative purposes. If you choose to use or access the samples, you agree to not hold Syncfusion® liable, in any form, for any damage related to use, for accessing, or viewing the samples. By accessing, viewing, or seeing the samples, you acknowledge and agree Syncfusion®'s samples will not allow you seek injunctive relief in any form for any claim related to the sample. If you do not agree to this, do not view, access, utilize, or otherwise do anything with Syncfusion®'s samples.

