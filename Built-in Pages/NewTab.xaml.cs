using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace EmptyBrowser.Built_in_Pages
{
    public sealed partial class NewTab : Page
    {
        // 定义导航请求事件
        public event EventHandler<string> NavigationRequested;

        // 默认搜索引擎
        private const string SearchEngine = "https://www.bing.com/search?q=";

        public NewTab()
        {
            this.InitializeComponent();
        }

        // 提交搜索或网址
        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string query = args.QueryText?.Trim();
            if (string.IsNullOrEmpty(query)) return;

            string url = ParseInput(query);
            // 触发事件，由 MainPage 处理导航
            NavigationRequested?.Invoke(this, url);
        }

        // 搜索建议
        private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                string query = sender.Text;
                if (string.IsNullOrEmpty(query))
                {
                    sender.ItemsSource = null;
                    return;
                }
                var suggestions = await GetBingSuggestions(query);
                sender.ItemsSource = suggestions;
            }
        }

        // 获取必应建议
        private async Task<string[]> GetBingSuggestions(string query)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    string url = $"https://api.bing.com/osjson.aspx?query={Uri.EscapeDataString(query)}";
                    string response = await client.GetStringAsync(url);
                    // 解析 JSONP 格式
                    int start = response.IndexOf('[');
                    int end = response.LastIndexOf(']');
                    if (start >= 0 && end > start)
                    {
                        string jsonArray = response.Substring(start, end - start + 1);
                        var matches = Regex.Matches(jsonArray, "\"([^\"]+)\"");
                        if (matches.Count > 1)
                        {
                            var list = new System.Collections.Generic.List<string>();
                            for (int i = 1; i < matches.Count; i++)
                            {
                                list.Add(matches[i].Groups[1].Value);
                            }
                            return list.ToArray();
                        }
                    }
                }
            }
            catch { }
            return new string[0];
        }

        // 智能解析输入（与 MainPage 逻辑一致）
        private string ParseInput(string input)
        {
            input = input.Trim();
            if (Uri.IsWellFormedUriString(input, UriKind.Absolute))
                return input;

            if (Regex.IsMatch(input, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$"))
                return "http://" + input;

            if (Regex.IsMatch(input, @"\.(com|net|org|edu|gov|cn|io|co|uk|de|jp|fr|au|ca)$", RegexOptions.IgnoreCase))
                return "https://" + input;

            return SearchEngine + Uri.EscapeDataString(input);
        }
    }
}