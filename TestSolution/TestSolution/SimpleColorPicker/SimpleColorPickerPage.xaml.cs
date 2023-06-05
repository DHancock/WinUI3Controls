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

namespace TestSolution.SimpleColorPicker
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SimpleColorPickerPage : Page, INotifyPropertyChanged
    {
        private Color bc;

        private readonly List<Color> paletteA = new List<Color>()
        {
            Color.FromArgb(0xFF, 0x7C, 0x2D, 0x12),
            Color.FromArgb(0xFF, 0x9A, 0x34, 0x12),
            Color.FromArgb(0xFF, 0xC2, 0x41, 0x0C),
            Color.FromArgb(0xFF, 0xEA, 0x58, 0x0C),
            Color.FromArgb(0xFF, 0xF9, 0x73, 0x16),
            Color.FromArgb(0xFF, 0xFB, 0x92, 0x3C),
            Color.FromArgb(0xFF, 0xFD, 0xBA, 0x74),
            Color.FromArgb(0xFF, 0xFE, 0xD7, 0xAA),
            Color.FromArgb(0xFF, 0xFF, 0xED, 0xD5),
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


        private static List<Color> BuildRandomPalette()
        {
            List<Color> palette = new List<Color>(12);
            byte[] rgb = new byte[3];
            Random rand = new Random();

            for (int index = 0; index < palette.Capacity; ++index)
            {
                rand.NextBytes(rgb);
                palette.Add(Color.FromArgb(0xFF, rgb[0], rgb[1], rgb[2]));
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
    }
}
