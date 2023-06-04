using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TestSolution.SimpleColorPicker
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SimpleColorPickerPage : Page, INotifyPropertyChanged
    {
        public SimpleColorPickerPage()
        {
            this.InitializeComponent();

            // note: the picker doesn't validate this color against it's palette 
            BC = Microsoft.UI.Colors.Green;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ActualTheme == ElementTheme.Light)
                RequestedTheme = ElementTheme.Dark;
            else if (ActualTheme == ElementTheme.Dark)
                RequestedTheme = ElementTheme.Light;
            else if (App.Current.RequestedTheme == ApplicationTheme.Light)
                RequestedTheme = ElementTheme.Dark;
            else
                RequestedTheme = ElementTheme.Light;
        }

        private Color bc;

        public Color BC
        {
            get => bc;
            set
            {
                if (bc != value)
                {
                    bc = value;
                    border.Background = new SolidColorBrush(bc);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BC)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SimpleColorPicker_ColorChanged(AssyntSoftware.WinUI3Controls.SimpleColorPicker sender, Color args)
        {
            borderEvent.Background = new SolidColorBrush(args);
        }
    }
}
