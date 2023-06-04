using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media.Animation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TestSolution
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly FrameNavigationOptions frameNavigationOptions = new()
        {
            TransitionInfoOverride = new SuppressNavigationTransitionInfo(),
            IsNavigationStackEnabled = false,
        };


        public MainWindow()
        {
            this.InitializeComponent();

            if (RootNavigationView.SelectionFollowsFocus == NavigationViewSelectionFollowsFocus.Disabled)
            {
                NavigationViewItem parent = (NavigationViewItem)RootNavigationView.MenuItems[0];
                parent.IsExpanded = true;
                RootNavigationView.SelectedItem = parent.MenuItems[0];
            }
        }


        private void RootNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                Type? type = Type.GetType($"TestSolution.{item.Tag}");

                if (type is not null)
                    _ = ContentFrame.NavigateToType(type, null, frameNavigationOptions);
            }
        }
    }

}
