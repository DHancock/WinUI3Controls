using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TestSolution.GroupBox
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PropertiesPage : Page
    {
        public PropertiesPage()
        {
            this.InitializeComponent();
            RootPanel.Loaded += (s, e) => InitialiseControls();
        }



        private const double cBorderStartPadding = 4;
        private const double cBorderEndPadding = 3;
        private const double cHeadingMargin = 16;
        private const double cHeadingBaseLineRatio = 0.61;
        private const double cBorderThickness = 1;
        private static readonly SolidColorBrush sBorderBrush = new SolidColorBrush(Colors.LightGray);
        private static readonly CornerRadius sCornerRadius = new CornerRadius(8);
        private const double cFontSize = 14;


        private void DarkMode_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch ts)
                ExamplePanel.RequestedTheme = ts.IsOn ? ElementTheme.Dark : ElementTheme.Light;
        }

        private void FlowDirection_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch ts)
                TestGroupBox.FlowDirection = ts.IsOn ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }

        private void UniformCornerRadius_Toggled(object sender, RoutedEventArgs e)
        {
            if ((sender is ToggleSwitch ts) && ts.IsOn)
                trcr.Value = brcr.Value = blcr.Value = tlcr.Value;
        }


        private void CornerRadius_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (RootPanel.IsLoaded)
            {
                if (crts.IsOn)
                {
                    trcr.Value = brcr.Value = blcr.Value = tlcr.Value = e.NewValue;
                    TestGroupBox.CornerRadius = new CornerRadius(e.NewValue);
                }
                else
                {
                    double tl = TestGroupBox.CornerRadius.TopLeft;
                    double tr = TestGroupBox.CornerRadius.TopRight;
                    double br = TestGroupBox.CornerRadius.BottomRight;
                    double bl = TestGroupBox.CornerRadius.BottomLeft;

                    switch (((Slider)sender).Name)
                    {
                        case "tlcr": tl = e.NewValue; break;
                        case "trcr": tr = e.NewValue; break;
                        case "brcr": br = e.NewValue; break;
                        case "blcr": bl = e.NewValue; break;
                        default: throw new InvalidOperationException();
                    }

                    TestGroupBox.CornerRadius = new CornerRadius(tl, tr, br, bl);
                }
            }
        }

        private void ZoomSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            svz.ChangeView(null, null, (float)e.NewValue);
        }

        private void BorderThickness_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            TestGroupBox.BorderThickness = new Thickness(e.NewValue);
        }


        private void ConfirmColor_Click(object sender, RoutedEventArgs e)
        {
            colorPickerButton.Flyout.Hide();
            TestGroupBox.BorderBrush = new SolidColorBrush(colorPicker.Color);
        }
        private void CancelColor_Click(object sender, RoutedEventArgs e)
        {
            colorPickerButton.Flyout.Hide();
        }

        private void CopyStyleButton_Click(object sender, RoutedEventArgs e)
        {
            // xml always uses a full stop as the decimal separator and a 
            // comma to separate property values
            CultureInfo ci = CultureInfo.InvariantCulture;
            const string format = "###.##;;0";

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<Style x:Key=\"your_key\" TargetType=\"your_xmlns_identifier:GroupBox\">");

            // always include at least the text
            sb.AppendLine($"\t<Setter Property=\"Heading\" Value=\"{TestGroupBox.Heading}\" />");

            if (((SolidColorBrush)TestGroupBox.BorderBrush).Color != sBorderBrush.Color)
            {
                sb.AppendLine("\t<Setter Property=\"BorderBrush\">");
                sb.AppendLine("\t\t<Setter.Value>");
                sb.AppendLine($"\t\t\t<SolidColorBrush Color=\"{((SolidColorBrush)TestGroupBox.BorderBrush).Color}\"/>");
                sb.AppendLine("\t\t</Setter.Value>");
                sb.AppendLine("\t</Setter>");
            }
            
            if (TestGroupBox.BorderThickness.Left != cBorderThickness)
                sb.AppendLine($"\t<Setter Property=\"BorderThickness\" Value=\"{TestGroupBox.BorderThickness.Left.ToString(format, ci)}\" />");

            if (TestGroupBox.BorderStartPadding != cBorderStartPadding)
                sb.AppendLine($"\t<Setter Property=\"BorderStartPadding\" Value=\"{TestGroupBox.BorderStartPadding.ToString(format, ci)}\"/>");

            if (TestGroupBox.BorderEndPadding != cBorderEndPadding)
                sb.AppendLine($"\t<Setter Property=\"BorderEndPadding\" Value=\"{TestGroupBox.BorderEndPadding.ToString(format, ci)}\"/>");

            if (TestGroupBox.HeadingMargin != cHeadingMargin)
                sb.AppendLine($"\t<Setter Property=\"HeadingMargin\" Value=\"{TestGroupBox.HeadingMargin.ToString(format, ci)}\"/>");

            if (TestGroupBox.HeadingBaseLineRatio != cHeadingBaseLineRatio)
                sb.AppendLine($"\t<Setter Property=\"HeadingBaseLineRatio\" Value=\"{TestGroupBox.HeadingBaseLineRatio.ToString(format, ci)}\"/>");

            if (TestGroupBox.CornerRadius != sCornerRadius)
            {
                if (TestGroupBox.CornerRadius == new CornerRadius(TestGroupBox.CornerRadius.TopLeft))
                    sb.AppendLine($"\t<Setter Property=\"CornerRadius\" Value=\"{TestGroupBox.CornerRadius.TopLeft.ToString(format, ci)}\"/>");
                else
                {
                    sb.Append($"\t<Setter Property=\"CornerRadius\" Value=\"{TestGroupBox.CornerRadius.TopLeft.ToString(format, ci)},");
                    sb.Append($"{TestGroupBox.CornerRadius.TopRight.ToString(format, ci)},");
                    sb.Append($"{TestGroupBox.CornerRadius.BottomRight.ToString(format, ci)},");
                    sb.AppendLine($"{TestGroupBox.CornerRadius.BottomLeft.ToString(format, ci)}\"/>");
                }
            }

            if (TestGroupBox.FontSize != cFontSize)
                sb.AppendLine($"\t<Setter Property=\"FontSize\" Value=\"{TestGroupBox.FontSize}\"/>");

            sb.AppendLine("</Style>");

            DataPackage dp = new();
            dp.SetText(sb.ToString());
            Clipboard.SetContent(dp);
        }

        private void InitialiseControls()
        {
            bsp.Value = cBorderStartPadding;
            bep.Value = cBorderEndPadding;
            hm.Value = cHeadingMargin;
            hblr.Value = cHeadingBaseLineRatio;

            crts.IsOn = true;
            trcr.Value = sCornerRadius.TopRight;

            bt.Value = cBorderThickness;

            fs.Value = cFontSize;

            darkMode.IsOn = Application.Current.RequestedTheme == ApplicationTheme.Dark;
            flowDirection.IsOn = RootPanel.FlowDirection == FlowDirection.RightToLeft;
        }

        private void RevertToDefaults()
        {
            TestGroupBox.BorderStartPadding = cBorderStartPadding;
            TestGroupBox.BorderEndPadding = cBorderEndPadding;
            TestGroupBox.HeadingMargin = cHeadingMargin;
            TestGroupBox.HeadingBaseLineRatio = cHeadingBaseLineRatio;
            TestGroupBox.BorderThickness = new Thickness(cBorderThickness);
            TestGroupBox.BorderBrush = new SolidColorBrush(sBorderBrush.Color);
            TestGroupBox.CornerRadius = sCornerRadius;
        }

        private void RevertButton_Click(object sender, RoutedEventArgs e)
        {
            RevertToDefaults();
            InitialiseControls();
        }

        private void ShowExtraButton_Toggled(object sender, RoutedEventArgs e)
        {
            extraButton.Visibility = ((ToggleSwitch)sender).IsOn ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}