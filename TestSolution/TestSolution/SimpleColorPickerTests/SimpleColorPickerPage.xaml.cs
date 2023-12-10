using Microsoft.UI;
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

namespace TestSolution.SimpleColorPickerTests
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SimpleColorPickerPage : Page, INotifyPropertyChanged
    {
        private Color bc;

        private readonly List<Color> paletteA = new List<Color>()
        {
            new Color{ A = 0xFF, R = 0x7C, G = 0x2D, B = 0x12 },
            new Color{ A = 0xFF, R = 0x9A, G = 0x34, B = 0x12 },
            new Color{ A = 0xFF, R = 0xC2, G = 0x41, B = 0x0C },
            new Color{ A = 0xFF, R = 0xEA, G = 0x58, B = 0x0C },
            new Color{ A = 0xFF, R = 0xF9, G = 0x73, B = 0x16 },
            new Color{ A = 0xFF, R = 0xFB, G = 0x92, B = 0x3C },
            new Color{ A = 0xFF, R = 0xFD, G = 0xBA, B = 0x74 },
            new Color{ A = 0xFF, R = 0xFE, G = 0xD7, B = 0xAA },
            new Color{ A = 0xFF, R = 0xFF, G = 0xED, B = 0xD5 },
        };

        private IEnumerable<Color> palette;

        public SimpleColorPickerPage()
        {
            this.InitializeComponent();

            palette = paletteA;

            // note: the picker doesn't validate this color against it's palette 
            BC = Colors.DarkKhaki;
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

        public IEnumerable<Color> CustomPalette
        {
            get => palette;
            set
            {
                if (palette != value)
                {
                    palette = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CustomPalette)));
                }
            }
        }


        public static List<Color> BuildRandomPalette()
        {
            List<Color> palette = new List<Color>(12);
            byte[] rgb = new byte[3];
            Random rand = new Random();

            for (int index = 0; index < palette.Capacity; ++index)
            {
                rand.NextBytes(rgb);
                palette.Add(new Color { A = 0xFF, R = rgb[0], G = rgb[1], B = rgb[2] });
            }

            return palette;
        }

        private int paletteTypeIndex = 0;

        private void TogglePalette_Click(object sender, RoutedEventArgs e)
        {
            switch (paletteTypeIndex++)
            {
                case 0: CustomPalette = BuildRandomPalette(); break;
                case 1: CustomPalette = new List<Color>(); break;  // revert to the built in default palette
                case 2: CustomPalette = paletteA; break;
            }

            paletteTypeIndex %= 3;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SimpleColorPicker_ColorChanged(AssyntSoftware.WinUI3Controls.SimpleColorPicker sender, Color args)
        {
            borderEvent.Background = new SolidColorBrush(args);
        }

        private void SimpleColorPicker_FlyoutOpened(AssyntSoftware.WinUI3Controls.SimpleColorPicker sender, bool args)
        {
            eventReceivedFeedback.Text = "event received: Opened";
        }

        private void SimpleColorPicker_FlyoutClosed(AssyntSoftware.WinUI3Controls.SimpleColorPicker sender, bool args)
        {
            eventReceivedFeedback.Text = "event received: Closed"; 
        }

        private void InitialSelectionComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            InitialSelectionComboBox.ItemsSource = Enum.GetNames<AssyntSoftware.WinUI3Controls.SimpleColorPicker.InitialSelection>();
            InitialSelectionComboBox.SelectedItem = InitialSelectionComboBox.Items[0];
        }

        private void InitialSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            String value = (String)InitialSelectionComboBox.SelectedItem;
            MiniPaletePicker.InitialSelectionMode = Enum.Parse<AssyntSoftware.WinUI3Controls.SimpleColorPicker.InitialSelection>(value);
        }
    }
}
