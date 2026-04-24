using Syncfusion.Maui.AIAssistView;

namespace AssistViewLiteLLMSample
{
    /// <summary>
    /// Extended AssistItem with HTML rendering support for Markdown responses
    /// </summary>
    public class ExtendedAssistItem : AssistItem
    {
        private string _htmlText = string.Empty;

        /// <summary>
        /// HTML-rendered version of the response text (for Markdown formatting)
        /// </summary>
        public string HtmlText
        {
            get => _htmlText;
            set
            {
                if (_htmlText != value)
                {
                    _htmlText = value;
                    OnPropertyChanged(nameof(HtmlText));
                }
            }
        }
    }
}
