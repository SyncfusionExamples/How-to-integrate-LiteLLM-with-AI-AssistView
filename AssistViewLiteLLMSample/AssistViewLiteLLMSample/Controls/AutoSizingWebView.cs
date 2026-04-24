using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;

namespace AssistViewLiteLLMSample
{
    // Auto-sizes to fit HTML content so the parent list scrolls on Android.
    public class AutoSizingWebView : WebView
    {
        public static readonly BindableProperty HtmlProperty = BindableProperty.Create(
            nameof(Html), typeof(string), typeof(AutoSizingWebView), default(string),
            propertyChanged: OnHtmlChanged);

        public string Html
        {
            get => (string)GetValue(HtmlProperty);
            set => SetValue(HtmlProperty, value);
        }

        bool _eventsWired;

        public AutoSizingWebView()
        {
            MinimumHeightRequest = 60;
            VerticalOptions = LayoutOptions.Start;
            HorizontalOptions = LayoutOptions.Fill;
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            WireEvents();
        }

        static void OnHtmlChanged(BindableObject bindable, object _, object newValue)
        {
            var view = (AutoSizingWebView)bindable;
            var html = (string?)newValue ?? string.Empty;
            view.Source = new HtmlWebViewSource { Html = WrapHtml(html) };
            view.WireEvents();
        }

        void WireEvents()
        {
            if (_eventsWired)
                return;

            Navigated -= OnNavigated;
            Navigated += OnNavigated;
            _eventsWired = true;
        }

        async void OnNavigated(object? sender, WebNavigatedEventArgs e)
        {
            try
            {
                await MeasureAndResizeAsync();
                await Task.Delay(200);
                await MeasureAndResizeAsync();
            }
            catch { }
        }

        async Task MeasureAndResizeAsync()
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

        static string WrapHtml(string html)
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
