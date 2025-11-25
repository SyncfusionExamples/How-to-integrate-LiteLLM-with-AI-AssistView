using AssistViewLiteLLMSample.Models;
using Markdig;
using Syncfusion.Maui.AIAssistView;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace AssistViewLiteLLMSample;


public class GettingStartedViewModel : INotifyPropertyChanged
{

#if ANDROID
    /// <summary>
    /// Android Emulator uses 10.0.2.2 as gateway to host machine.
    /// </summary>
    private const string LiteLLMEndpoint = "http://10.0.2.2:4000/v1/chat/completions";
#else
    /// <summary>
    /// Windows/Desktop uses localhost for LiteLLM proxy.
    /// </summary>
    private const string LiteLLMEndpoint = "http://localhost:4000/v1/chat/completions";
#endif

    /// <summary>
    /// Model name must match the model_name in litellm_config.yaml.
    /// </summary>
    private const string ModelName = "azure-gpt-4.1";

    /// <summary>
    /// HTTP request timeout in seconds. Increase for longer queries.
    /// </summary>
    private const int HttpTimeoutSeconds = 300;

    /// <summary>
    /// Collection of assistant items displayed in the UI.
    /// </summary>
    public ObservableCollection<IAssistItem> AssistItems { get; set; }

    /// <summary>
    /// Command executed when user submits a query.
    /// </summary>
    public ICommand AssistViewRequestCommand { get; set; }

    /// <summary>
    /// Constructor - initializes collections and commands.
    /// </summary>
    public GettingStartedViewModel()
    {
        AssistItems = new ObservableCollection<IAssistItem>();
        AssistViewRequestCommand = new Command<object>(ExecuteRequestCommand);
    }

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

    /// <summary>
    /// Extracts the message content from the LiteLLM JSON response.
    /// Navigates the response structure: choices[0].message.content
    /// </summary>
    private string ExtractMessageFromResponse(string jsonResponse)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(jsonResponse);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("choices", out var choicesArray) &&
                choicesArray.GetArrayLength() > 0)
            {
                var firstChoice = choicesArray[0];

                // Navigate to message object
                if (firstChoice.TryGetProperty("message", out var messageObj) &&
                    messageObj.TryGetProperty("content", out var contentElement))
                {
                    string content = contentElement.GetString();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        return content;
                    }
                }
            }

            return "The API returned an empty response. Please check your query and try again.";
        }
        catch (JsonException)
        {
            return "Failed to parse the API response. The response format may be unexpected.";
        }
    }

    /// <summary>
    /// Handles HTTP error status codes with user-friendly messages.
    /// </summary>
    private string HandleHttpError(HttpResponseMessage response)
    {
        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized =>
                "Authentication failed. Please verify your Azure API key in litellm_config.yaml.",

            System.Net.HttpStatusCode.Forbidden =>
                "Access denied. Please check your Azure credentials and permissions.",

            System.Net.HttpStatusCode.NotFound =>
                "The requested model or endpoint was not found. Please check your configuration.",

            System.Net.HttpStatusCode.TooManyRequests =>
                "Too many requests. You have exceeded the rate limit. Please wait before retrying.",

            System.Net.HttpStatusCode.InternalServerError =>
                "Azure OpenAI service encountered an error. Please try again later.",

            _ => $"API error ({response.StatusCode}). Please check the LiteLLM logs for details."
        };
    }

    /// <summary>
    /// Handles HTTP connection errors with platform-specific guidance.
    /// </summary>
    private string HandleConnectionError(HttpRequestException ex)
    {
#if ANDROID
        return $"Connection failed: {ex.Message}\n\nEnsure LiteLLM is running on http://10.0.2.2:4000";
#else
        return $"Connection failed: {ex.Message}\n\nEnsure LiteLLM is running on http://localhost:4000";
#endif
    }


    /// <summary>
    /// Cleans response text by removing markdown code fences and normalizing whitespace.
    /// </summary>
    private string CleanResponseText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Normalize line endings
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");

        // Remove leading/trailing markdown code fences
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"^\s*```(?:[a-zA-Z0-9_\-]+)?\s*\n",
            ""
        );
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n```\s*$", "");

        return text.Trim();
    }

    /// <summary>
    /// Generates professionally styled HTML content from Markdown text.
    /// Includes comprehensive CSS for proper formatting and typography.
    /// </summary>
    private string GenerateHtmlContent(string markdownText)
    {
        try
        {
            // Convert Markdown to HTML using Markdig library
            string htmlBody = Markdown.ToHtml(markdownText);

            // Wrap in styled HTML template
            return $@"
            <html>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    body {{
                        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
                        line-height: 1.6;
                        color: #333;
                        padding: 16px;
                        margin: 0;
                        background-color: #fff;
                    }}
                    h1, h2, h3, h4, h5, h6 {{
                        color: #1976D2;
                        margin-top: 20px;
                        margin-bottom: 10px;
                    }}
                    h1 {{ font-size: 24px; }}
                    h2 {{ font-size: 20px; }}
                    h3 {{ font-size: 18px; }}
                    p {{
                        margin: 10px 0;
                    }}
                    pre {{
                        background-color: #F5F5F5;
                        border: 1px solid #E0E0E0;
                        border-radius: 4px;
                        padding: 12px;
                        overflow-x: auto;
                        font-size: 13px;
                    }}
                    code {{
                        font-family: 'Courier New', Courier, monospace;
                        background-color: #F0F0F0;
                        padding: 2px 6px;
                        border-radius: 3px;
                    }}
                    blockquote {{
                        border-left: 4px solid #1976D2;
                        margin: 16px 0;
                        padding-left: 12px;
                        color: #666;
                    }}
                    ul, ol {{
                        margin: 10px 0;
                        padding-left: 24px;
                    }}
                    li {{
                        margin: 5px 0;
                    }}
                    a {{
                        color: #1976D2;
                        text-decoration: none;
                    }}
                    a:hover {{
                        text-decoration: underline;
                    }}
                    table {{
                        border-collapse: collapse;
                        width: 100%;
                        margin: 16px 0;
                    }}
                    th, td {{
                        border: 1px solid #E0E0E0;
                        padding: 8px;
                        text-align: left;
                    }}
                    th {{
                        background-color: #F5F5F5;
                        font-weight: 600;
                    }}
                </style>
            </head>
            <body>
                {htmlBody}
            </body>
            </html>";
        }
        catch
        {
            return $@"<html><body><p>{System.Net.WebUtility.HtmlEncode(markdownText)}</p></body></html>";
        }
    }

    /// <summary>
    /// PropertyChanged event for data binding updates.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises PropertyChanged event for UI updates.
    /// </summary>
    protected void OnPropertyChanged(string? propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

