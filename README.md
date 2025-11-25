# How-to-integrate-LiteLLM-with-AI-AssistView
This demo shows how to integrate LiteLLM with .NET MAUI AI AssistView.

Introduction
Integrating LiteLLM with Syncfusion MAUI AI AssistView empowers developers to deliver intelligent, responsive, and scalable AI-driven experiences in .NET applications. This blog provides a clear, customer-focused guide to setting up LiteLLM with AI AssistView, ensuring smooth integration and optimal performance.

Why LiteLLM with AI AssistView?
LiteLLM acts as a lightweight proxy that supports multiple AI models like OpenAI, Azure OpenAI, Anthropic with minimal configuration. When combined with AI AssistView, it enables:
•	Multi-model flexibility: Switch between AI providers easily.
•	Secure API calls: Centralized configuration for keys and endpoints.
•	Enhanced user experience: Real-time, contextual responses in your MAUI app.
•	Scalability: Handle multiple requests efficiently.

Integrating LiteLLM with the .NET MAUI app
Step 1: Install LiteLLM

LiteLLM acts as a middleware for AI models. Install it using pip:

$ pip install litellm
Verify installation:
  $ litellm --version
Step 2: Create a configuration file
Create a configuraton file: litellm_config.yaml
Content:
---
model_list:
  - model_name: YOUR_MODEL_NAME
    litellm_params:
      model: “YOUR_MODEL”
      api_base: "YOUR_API_BASE"
      api_key: "YOUR_AZURE_API_KEY_HERE"
      api_version: "API_VERSION"

router_settings:
 enable_router_logging: true

debug: true
---
Step 3: Start LiteLLM Proxy
Open a new PowerShell window and run the proxy to route requests:
  $ cd "CONFIGURATION_FILE_LOCATION"
  $ litellm --config "litellm_config.yaml" --port 4000

Keep this terminal window open during development.

After you run the command, you should see messages like:
  INFO:     Application startup complete.
  INFO:     Uvicorn running

 

Integrate LiteLLM with AI AssistView 

Step 1: Handle AssistView Request
Use the following C# method to capture the user query from AI AssistView:
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

Step 2: Process the User Query 
This method sends the query to LiteLLM, formats the response, and updates the UI:
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

Step 3: Send Request to LiteLLM

    /// <summary>
    /// Sends request to LiteLLM proxy and retrieves AI response.
    /// Includes comprehensive error handling for various failure scenarios.
    /// </summary>
    private async Task<string> GetResponseFromLiteLLMAsync(string userPrompt)
    {
        try
        {
            var httpHandler = new HttpClientHandler();

#if DEBUG
            httpHandler.ServerCertificateCustomValidationCallback =
                (message, cert, chain, errors) => true;
#endif

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

            var jsonPayload = JsonSerializer.Serialize(requestPayload);
            var httpContent = new StringContent(
                jsonPayload,
                Encoding.UTF8,
                "application/json"
            );

            httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "AssistViewSample/1.0");

            // Send POST request to LiteLLM
            var httpResponse = await httpClient.PostAsync(LiteLLMEndpoint, httpContent);

            if (!httpResponse.IsSuccessStatusCode)
            {
                return HandleHttpError(httpResponse);
            }

            string responseBody = await httpResponse.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return "No response received from the API.";
            }

            return ExtractMessageFromResponse(responseBody);
        }
        catch (HttpRequestException ex)
        {
            return HandleConnectionError(ex);
        }
        catch (TaskCanceledException)
        {
            return $"Request timeout after {HttpTimeoutSeconds} seconds. Please try again.";
        }
        catch (JsonException ex)
        {
            return $"Failed to parse API response: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Unexpected error: {ex.GetType().Name} - {ex.Message}";
        }
    }

    var json = await response.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(json);
    return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
}

