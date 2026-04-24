using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;

namespace AssistViewLiteLLMSample
{
    // Auto-sizes to fit HTML content so the parent list scrolls on Android.
    public class AutoSizingWebView : WebView
    {
        /// <summary>
        /// Field to track whether we've already wired the Navigated event to avoid multiple subscriptions.
        /// </summary>
        private bool _eventsWired;

        /// <summary>
        /// Identifies the Html bindable property for the AutoSizingWebView control.
        /// </summary>
        public static readonly BindableProperty HtmlProperty = BindableProperty.Create(
            nameof(Html), typeof(string), typeof(AutoSizingWebView), default(string),
            propertyChanged: OnHtmlChanged);

        /// <summary>
        /// Initializes a new instance of the AutoSizingWebView class with default layout and minimum height settings.
        /// </summary>
        public AutoSizingWebView()
        {
            MinimumHeightRequest = 60;
            VerticalOptions = LayoutOptions.Start;
            HorizontalOptions = LayoutOptions.Fill;
        }

        /// <summary>
        /// Gets or sets the HTML markup to display.
        /// </summary>
        public string Html
        {
            get => (string)GetValue(HtmlProperty);
            set => SetValue(HtmlProperty, value);
        }

        /// <summary>
        /// Invoked when the underlying platform handler changes for this element.
        /// </summary>
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            WireEvents();
        }

        /// <summary>
        /// Handles changes to the HTML content property and updates the web view source accordingly.
        /// </summary>
        /// <param name="bindable">The bindable object whose HTML property has changed. Must be an instance of AutoSizingWebView.</param>
        /// <param name="obj">The old value of the property. This parameter is not used.</param>
        /// <param name="newValue">The new value assigned to the HTML property. If null, an empty string is used.</param>
        private static void OnHtmlChanged(BindableObject bindable, object obj, object newValue)
        {
            var view = (AutoSizingWebView)bindable;
            var html = (string?)newValue ?? string.Empty;
            view.Source = new HtmlWebViewSource { Html = WrapHtml(html) };
            view.WireEvents();
        }

        /// <summary>
        /// Attaches the required event handlers to enable navigation event processing.
        /// </summary>
        private void WireEvents()
        {
            if (_eventsWired)
                return;

            Navigated -= OnNavigated;
            Navigated += OnNavigated;
            _eventsWired = true;
        }

        /// <summary>
        /// Handles the navigation event for a web view and performs layout adjustments after navigation completes.
        /// </summary>
        /// <param name="sender">The source of the navigation event. This is typically the web view that triggered the event.</param>
        /// <param name="e">The event data containing information about the navigation that has occurred.</param>
        private async void OnNavigated(object? sender, WebNavigatedEventArgs e)
        {
            try
            {
                await MeasureAndResizeAsync();
                await Task.Delay(200);
                await MeasureAndResizeAsync();
            }
            catch { }
        }

        /// <summary>
        /// Measures the rendered height of the web content and updates the control's height accordingly.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task MeasureAndResizeAsync()
        {
            if (Handler == null)
                return;

            var script =
                "(function(){" +
                "var de=document.documentElement, b=document.body;" +
                "var h=Math.max(de.scrollHeight, b.scrollHeight, de.offsetHeight, b.offsetHeight, de.clientHeight);" +
                "return h;" +
                "})()";

            var result = await EvaluateJavaScriptAsync(script);
            if (string.IsNullOrWhiteSpace(result)) return;

            // Some platforms return the number already quoted; trim quotes/spaces
            result = result.Trim().Trim('\"');
            if (!double.TryParse(result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cssPx))
                return;

            // On Android, CSS px map closely to device-independent units in WebView
            var targetHeight = Math.Max(1, cssPx) + 8; // small safety pad

            if (!double.IsNaN(targetHeight) && !double.IsInfinity(targetHeight))
            {
                Application.Current?.Dispatcher.Dispatch(() =>
                {
                    HeightRequest = targetHeight;
                });
            }
        }

        /// <summary>
        /// Wraps the specified HTML fragment in a minimal HTML document structure if it does not already contain an html tag.
        /// </summary>
        /// <remarks>This method ensures that HTML fragments are rendered consistently by providing a
        /// standard document structure, including viewport and basic styling. If the input is null, empty, or
        /// whitespace, an empty HTML document is returned.</remarks>
        /// <param name="html">The HTML content to wrap. May be a full HTML document or a fragment.</param>
        /// <returns>A complete HTML document containing the original content if it was a fragment; otherwise, the original HTML if it already contains an html tag.</returns>
        private static string WrapHtml(string html)
        {
            // If the incoming content isn't full HTML, wrap it with basics for consistent sizing
            if (string.IsNullOrWhiteSpace(html))
                html = string.Empty;

            var hasHtmlTag = html.IndexOf("<html", StringComparison.OrdinalIgnoreCase) >= 0;
            if (hasHtmlTag)
                return html;

            return $"<!DOCTYPE html><html><head>" +
                   "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">" +
                   "<style>body{margin:0;padding:0 8px;font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;line-height:1.4;overflow-wrap:anywhere;}img,table,pre,code{max-width:100%;}ul{padding-left:20px;margin:0 0 0.75rem 0;}h1,h2,h3{margin:0.6rem 0;}hr{border:none;border-top:1px solid #e0e0e0;margin:0.75rem 0;}</style>" +
                   "</head><body>" + html + "</body></html>";
        }
    }
}