using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using EmptyBrowser.Built_in_Pages;

namespace EmptyBrowser
{
    public class Tab_List
    {
        public string Title { get; set; }
        public BitmapImage iconSource { get; set; }
    }

    public sealed partial class MainPage : Page
    {
        public ObservableCollection<Tab_List> TabsCollection { get; set; } = new ObservableCollection<Tab_List>();
        private string currentDownloadFile = "";

        public MainPage()
        {
            this.InitializeComponent();

            TabsCollection.Add(new Tab_List { Title = "⌀ Browser 主页", iconSource = new BitmapImage(new Uri("ms-appx:///Assets/FaviconPlaceholder.png")) });
            TabsCollection.Add(new Tab_List { Title = "必应", iconSource = new BitmapImage(new Uri("ms-appx:///Assets/FaviconPlaceholder.png")) });
            TabsCollection.Add(new Tab_List { Title = "GitHub", iconSource = new BitmapImage(new Uri("ms-appx:///Assets/FaviconPlaceholder.png")) });
            ListView.ItemsSource = TabsCollection;
        }

        public void NavigateToUrl(string url)
        {
            if (MicrosoftEdge.SelectedItem is TabViewItem selectedTab &&
                selectedTab.Content is Grid grid &&
                grid.Children[1] is WebView2 webView &&
                webView.CoreWebView2 != null)
            {
                webView.CoreWebView2.Navigate(url);
            }
        }

        private async void AddNewTab(string url = null)
        {
            if (url == "empty://newtab")
            {
                var newTabPage = new NewTab();
                var newTabContent = new Grid();
                newTabContent.Children.Add(newTabPage);
                var emptyTab = new TabViewItem
                {
                    Header = "新标签页",
                    Content = newTabContent
                };
                MicrosoftEdge.TabItems.Add(emptyTab);
                MicrosoftEdge.SelectedItem = emptyTab;
                return;
            }

            // 导航按钮：完全使用默认样式，图标缩小为 12x12
            var backButton = new Button
            {
                Width = 36,
                Height = 32,
                Margin = new Thickness(2, 0, 2, 0)
            };
            var backIcon = new SymbolIcon { Symbol = Symbol.Back, Width = 12, Height = 12 };
            backButton.Content = backIcon;
            backButton.IsEnabled = false;

            var forwardButton = new Button
            {
                Width = 36,
                Height = 32,
                Margin = new Thickness(2, 0, 2, 0)
            };
            var forwardIcon = new SymbolIcon { Symbol = Symbol.Forward, Width = 12, Height = 12 };
            forwardButton.Content = forwardIcon;
            forwardButton.IsEnabled = false;

            var homeButton = new Button
            {
                Width = 36,
                Height = 32,
                Margin = new Thickness(2, 0, 2, 0)
            };
            var homeIcon = new SymbolIcon { Symbol = Symbol.Home, Width = 12, Height = 12 };
            homeButton.Content = homeIcon;

            var refreshButton = new Button
            {
                Width = 36,
                Height = 32,
                Margin = new Thickness(2, 0, 2, 0)
            };
            var refreshIcon = new SymbolIcon { Symbol = Symbol.Refresh, Width = 12, Height = 12 };
            refreshButton.Content = refreshIcon;

            // 地址栏
            var addressBar = new TextBox
            {
                PlaceholderText = "输入网址或搜索内容",
                FontSize = 14,
                CornerRadius = new CornerRadius(1),
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Stretch
            };
            addressBar.GotFocus += (s, e) => addressBar.SelectAll();

            // 地址栏和按钮的容器
            var addressPanel = new Grid
            {
                Background = (Windows.UI.Xaml.Media.Brush)Application.Current.Resources["SystemControlBackgroundChromeMediumBrush"],
                Padding = new Thickness(8, 4, 12, 4)
            };
            addressPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 后退
            addressPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 前进
            addressPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 主页
            addressPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 刷新
            addressPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 地址栏

            addressPanel.Children.Add(backButton);
            Grid.SetColumn(backButton, 0);
            addressPanel.Children.Add(forwardButton);
            Grid.SetColumn(forwardButton, 1);
            addressPanel.Children.Add(homeButton);
            Grid.SetColumn(homeButton, 2);
            addressPanel.Children.Add(refreshButton);
            Grid.SetColumn(refreshButton, 3);
            addressPanel.Children.Add(addressBar);
            Grid.SetColumn(addressBar, 4);

            var webView = new WebView2();
            await webView.EnsureCoreWebView2Async();

            // 按钮事件
            backButton.Click += (s, e) => webView.CoreWebView2.GoBack();
            forwardButton.Click += (s, e) => webView.CoreWebView2.GoForward();
            homeButton.Click += (s, e) => webView.CoreWebView2.Navigate("https://www.bing.com");
            refreshButton.Click += (s, e) => webView.CoreWebView2.Reload();

            // 更新按钮状态
            void UpdateNavigationButtons()
            {
                backButton.IsEnabled = webView.CoreWebView2.CanGoBack;
                forwardButton.IsEnabled = webView.CoreWebView2.CanGoForward;
            }

            webView.CoreWebView2.HistoryChanged += (sender, args) => UpdateNavigationButtons();
            webView.CoreWebView2.NavigationStarting += (sender, args) =>
            {
                string urlToCheck = args.Uri;
                if (IsDownloadableUrl(urlToCheck))
                {
                    args.Cancel = true;
                    string fileName = ExtractFileName(urlToCheck);
                    ShowDownloadBanner(fileName, "未知速度", true);
                }
            };
            webView.CoreWebView2.NavigationCompleted += (sender, args) =>
            {
                var currentTab = FindTabByWebView2(webView);
                if (currentTab != null)
                {
                    currentTab.Header = webView.CoreWebView2.DocumentTitle;
                    if (currentTab == MicrosoftEdge.SelectedItem)
                    {
                        addressBar.Text = webView.CoreWebView2.Source;
                        webView.Focus(FocusState.Programmatic);
                    }
                }
                UpdateNavigationButtons();
            };
            webView.CoreWebView2.NewWindowRequested += (sender, args) =>
            {
                args.Handled = true;
                AddNewTab(args.Uri);
            };

            // 地址栏导航
            void Navigate()
            {
                string input = addressBar.Text;
                string urlToNavigate = ParseInput(input);

                if (urlToNavigate == "empty://newtab")
                {
                    var currentTab = MicrosoftEdge.SelectedItem as TabViewItem;
                    if (currentTab != null && currentTab.Content is Grid currentGrid)
                    {
                        var newTabPage = new NewTab();
                        var newContentGrid = new Grid();
                        newContentGrid.Children.Add(newTabPage);
                        currentGrid.Children[1] = newContentGrid;
                        currentTab.Header = "新标签页";
                        addressBar.Text = "empty://newtab";
                    }
                    return;
                }

                try
                {
                    webView.CoreWebView2.Navigate(urlToNavigate);
                }
                catch (Exception ex)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "无效的地址",
                        Content = $"无法解析地址: {ex.Message}",
                        CloseButtonText = "确定"
                    };
                    _ = dialog.ShowAsync();
                }
            }

            addressBar.KeyDown += (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    Navigate();
                    e.Handled = true;
                }
            };

            // 布局
            var contentGrid = new Grid();
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            contentGrid.Children.Add(addressPanel);
            Grid.SetRow(addressPanel, 0);
            contentGrid.Children.Add(webView);
            Grid.SetRow(webView, 1);

            var newTab = new TabViewItem
            {
                Header = "新标签页",
                Content = contentGrid
            };
            MicrosoftEdge.TabItems.Add(newTab);
            MicrosoftEdge.SelectedItem = newTab;

            if (!string.IsNullOrEmpty(url))
            {
                webView.CoreWebView2.Navigate(url);
            }
            else
            {
                webView.CoreWebView2.Navigate("https://www.bing.com");
            }
        }

        private TabViewItem FindTabByWebView2(WebView2 webView2)
        {
            foreach (TabViewItem tabItem in MicrosoftEdge.TabItems)
            {
                if (tabItem.Content is Grid grid && grid.Children[1] == webView2)
                    return tabItem;
            }
            return null;
        }

        private bool IsDownloadableUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            string lowerUrl = url.ToLower();

            string[] extensions = { ".pdf", ".zip", ".rar", ".exe", ".msi", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".png", ".mp3", ".mp4", ".avi", ".mkv", ".apk", ".iso", ".7z", ".tar", ".gz", ".torrent" };
            foreach (string ext in extensions)
            {
                if (lowerUrl.EndsWith(ext))
                    return true;
            }

            string[] keywords = { "download", "file", "attachment", "getfile", "export", "?id=", "&id=" };
            foreach (string kw in keywords)
            {
                if (lowerUrl.Contains(kw))
                    return true;
            }

            return false;
        }

        private string ExtractFileName(string url)
        {
            try
            {
                var uri = new Uri(url);
                string fileName = System.IO.Path.GetFileName(uri.LocalPath);
                if (!string.IsNullOrEmpty(fileName) && fileName.Contains("."))
                    return fileName;

                var query = uri.Query;
                if (!string.IsNullOrEmpty(query))
                {
                    var parts = query.Split('&');
                    foreach (var part in parts)
                    {
                        if (part.StartsWith("filename=", StringComparison.OrdinalIgnoreCase))
                            return part.Substring(9);
                        if (part.StartsWith("name=", StringComparison.OrdinalIgnoreCase))
                            return part.Substring(5);
                    }
                }
            }
            catch { }
            return "未知文件";
        }

        private string ParseInput(string input)
        {
            input = input.Trim();
            if (Uri.IsWellFormedUriString(input, UriKind.Absolute))
                return input;

            if (Regex.IsMatch(input, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$"))
                return "http://" + input;

            if (Regex.IsMatch(input, @"\.(com|net|org|edu|gov|cn|io|co|uk|de|jp|fr|au|ca)$", RegexOptions.IgnoreCase))
                return "https://" + input;

            return "https://www.bing.com/search?q=" + Uri.EscapeDataString(input);
        }

        private async void ShowDownloadBanner(string fileName, string speed, bool isDownloadingState)
        {
            currentDownloadFile = fileName;

            if (isDownloadingState)
            {
                DownloadStatusText.Text = $"正在下载 {fileName} ({speed})";
                DownloadingButtons.Visibility = Visibility.Visible;
                CompletedButtons.Visibility = Visibility.Collapsed;
                DownloadIcon.Glyph = "\uE896";
            }
            else
            {
                DownloadStatusText.Text = $"要对 {fileName} 执行何种操作？";
                DownloadingButtons.Visibility = Visibility.Collapsed;
                CompletedButtons.Visibility = Visibility.Visible;
                DownloadIcon.Glyph = "\uE8FB";
            }

            DownloadBar.Visibility = Visibility.Visible;
            await Task.Delay(5000);
            if (DownloadBar.Visibility == Visibility.Visible)
                DownloadBar.Visibility = Visibility.Collapsed;
        }

        private void TabView_AddButtonClick(TabView sender, object args) => AddNewTab();

        private void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args) => sender.TabItems.Remove(args.Tab);

        private void TabView_Loaded(object sender, RoutedEventArgs e)
        {
            if (MicrosoftEdge.TabItems.Count == 0)
                AddNewTab();
        }

        private void MicrosoftEdge_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MicrosoftEdge.SelectedItem is TabViewItem selectedTab &&
                selectedTab.Content is Grid grid &&
                grid.Children[0] is Grid addressPanel &&
                addressPanel.Children[4] is TextBox addressBar &&
                grid.Children[1] is WebView2 webView &&
                webView.CoreWebView2 != null)
            {
                addressBar.Text = webView.CoreWebView2.Source;
            }
        }

        private async void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog { Title = "暂停下载", Content = $"已暂停下载 {currentDownloadFile}", CloseButtonText = "确定" };
            await dialog.ShowAsync();
        }

        private async void DeleteDuringDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog { Title = "删除下载", Content = $"已取消下载 {currentDownloadFile}", CloseButtonText = "确定" };
            await dialog.ShowAsync();
            DownloadBar.Visibility = Visibility.Collapsed;
        }

        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog { Title = "打开文件", Content = $"打开 {currentDownloadFile}", CloseButtonText = "确定" };
            await dialog.ShowAsync();
        }

        private async void DeleteCompletedButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog { Title = "删除文件", Content = $"已删除 {currentDownloadFile}", CloseButtonText = "确定" };
            await dialog.ShowAsync();
            DownloadBar.Visibility = Visibility.Collapsed;
        }

        private void CloseDownloadBarButton_Click(object sender, RoutedEventArgs e) => DownloadBar.Visibility = Visibility.Collapsed;

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e) { }
        private void TabListButton_Click(object sender, RoutedEventArgs e) { }
        private void ExitFS_PointerEntered(object sender, PointerRoutedEventArgs e) { }
        private void SideWindowBackground_PointerPressed(object sender, PointerRoutedEventArgs e) { }
        private void SideWindowBackground_PointerReleased(object sender, PointerRoutedEventArgs e) { }
        private void SideGrid_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e) { }
        private void SideGrid_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e) { }
        private void SideGrid_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e) { }
        private void TabListBackground_PointerPressed(object sender, PointerRoutedEventArgs e) { }
        private void TabListBackground_PointerReleased(object sender, PointerRoutedEventArgs e) { }
        private void TabListGrid_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e) { }
        private void TabListGrid_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e) { }
        private void TabListGrid_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e) { }
        private void TabListView_ItemClick(object sender, ItemClickEventArgs e) { }
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void TabListCloseItem_Click(object sender, RoutedEventArgs e) { }
        private void SettingsBack_Click(object sender, RoutedEventArgs e) { }
    }
}