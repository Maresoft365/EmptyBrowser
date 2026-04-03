using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace EmptyBrowser.Built_in_Pages
{
    public sealed partial class Settings : Page
    {
        public Settings()
        {
            this.InitializeComponent();
            SettingsNavView.SelectedItem = SettingsNavView.MenuItems[0];
            NavigateToSettingsPage("General");
        }

        private void SettingsNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem selectedItem && selectedItem.Tag != null)
            {
                string tag = selectedItem.Tag.ToString();
                NavigateToSettingsPage(tag);
            }
        }

        private void NavigateToSettingsPage(string pageTag)
        {
            // 动态创建简单的设置内容，避免引用不存在的子页面
            var content = new StackPanel { Margin = new Thickness(20) };
            string titleText = "";
            switch (pageTag)
            {
                case "General":
                    titleText = "常规设置\n\n此页面用于配置浏览器的常规选项。";
                    break;
                case "Privacy":
                    titleText = "隐私设置\n\n管理浏览数据、Cookie 等。";
                    break;
                case "Download":
                    titleText = "下载设置\n\n设置下载路径、是否显示下载横幅等。";
                    break;
                case "SearchEngine":
                    titleText = "搜索引擎设置\n\n选择默认搜索引擎（必应、百度、Google 等）。";
                    break;
                default:
                    titleText = "设置";
                    break;
            }
            content.Children.Add(new TextBlock
            {
                Text = titleText,
                FontSize = 20,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 20)
            });
            SettingsFrame.Content = content;
        }

        private void SettingsFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            e.Handled = true;
        }
    }
}