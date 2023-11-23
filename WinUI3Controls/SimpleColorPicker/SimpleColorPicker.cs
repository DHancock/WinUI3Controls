using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace AssyntSoftware.WinUI3Controls
{
    public sealed class SimpleColorPicker : Control
    {
        private const int cDefaultSamplesPerColor = 10;  // the number of shades of a color in the default palettes
        
        public event TypedEventHandler<SimpleColorPicker, Color>? ColorChanged;
        public event TypedEventHandler<SimpleColorPicker, bool>? FlyoutOpened;
        public event TypedEventHandler<SimpleColorPicker, bool>? FlyoutClosed;

        private DateTime lastKeyRepeat = DateTime.UtcNow;
        private SplitButton? pickButton;
        private Border? indicatorBorder;
        private Grid? grid;
        private Style? cellStyle;
        private Style? flyoutPresenterStyle;

        public SimpleColorPicker()
        {
            this.DefaultStyleKey = typeof(SimpleColorPicker);
        }
            
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            pickButton = GetTemplateChild("PART_PickButton") as SplitButton;

            if (pickButton is not null)
            {
                pickButton.Click += PickButton_Click;

                indicatorBorder = pickButton.Content as Border;

                if (indicatorBorder is not null)
                {
                    indicatorBorder.Width = IndicatorWidth;
                    indicatorBorder.Background = new SolidColorBrush(Color);
                }

                if ((pickButton.Flyout is Flyout flyout) && (flyout.Content is Grid root))
                {
                    grid = root;

                    flyout.Opening += (s, e) =>
                    {
                        if (grid.Children.Count == 0)
                        {
                            if (IsCustomPalette)
                                CreateCustomPaletteGrid();
                            else
                                CreateDefaultPaletteGrid();
                        }
                    };

                    flyout.Opened += (s, e) =>
                    {
                        IsFlyoutOpen = true;
                        FlyoutOpened?.Invoke(this, true);
                    };

                    flyout.Closed += (s, e) =>
                    {
                        IsFlyoutOpen = false;
                        FlyoutClosed?.Invoke(this, false);
                    };

                    // changing the constraint value after the flyout has been shown isn't supported
                    flyout.ShouldConstrainToRootBounds = ShouldConstrainToRootBounds;

                    if (FlyoutPresenterStyle is not null)
                        flyout.FlyoutPresenterStyle = FlyoutPresenterStyle;

                    if (IsFlyoutOpen)
                        Loaded += SimpleColorPicker_Loaded;
                }
            }
        }

        private static void SimpleColorPicker_Loaded(object sender, RoutedEventArgs e)
        {
            SimpleColorPicker picker = (SimpleColorPicker)sender;
            SetFlyoutOpenState(picker, picker.IsFlyoutOpen);
            picker.Loaded -= SimpleColorPicker_Loaded;
        }

        private bool IsCustomPalette => (Palette is not null) && Palette.Any() && (CellsPerColumn > 0);

        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(nameof(Color),
                typeof(string),
                typeof(SimpleColorPicker),
                new PropertyMetadata(default(Color), ColorPropertyChanged));

        private static void ColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SimpleColorPicker picker = (SimpleColorPicker)d;
            Color color = (Color)e.NewValue;

            if (picker.indicatorBorder is not null)
                picker.indicatorBorder.Background = new SolidColorBrush(color);

            picker.ColorChanged?.Invoke(picker, color);
        }

        public double IndicatorWidth
        {
            get { return (double)GetValue(IndicatorWidthProperty); }
            set { SetValue(IndicatorWidthProperty, value); }
        }

        public static readonly DependencyProperty IndicatorWidthProperty =
            DependencyProperty.Register(nameof(IndicatorWidth),
                typeof(double),
                typeof(SimpleColorPicker),
                new PropertyMetadata(32));

        public bool IsFlyoutOpen
        {
            get { return (bool)GetValue(IsFlyoutOpenProperty); }
            set { SetValue(IsFlyoutOpenProperty, value); }
        }

        public static readonly DependencyProperty IsFlyoutOpenProperty =
            DependencyProperty.Register(nameof(IsFlyoutOpen),
                typeof(bool),
                typeof(SimpleColorPicker),
                new PropertyMetadata(false, IsFlyoutOpenPropertyChanged));

        private static void IsFlyoutOpenPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetFlyoutOpenState((SimpleColorPicker)d, (bool)e.NewValue); 
        }

        private static void SetFlyoutOpenState(SimpleColorPicker picker, bool toOpen)
        {
            if ((picker.pickButton is not null) && (picker.pickButton.Flyout is not null))
            {
                FlyoutBase flyout = picker.pickButton.Flyout;

                if (toOpen)
                {
                    if (!flyout.IsOpen)
                    {
                        flyout.ShowAt(picker.pickButton);
                    }
                }
                else 
                {
                    picker.ResetFlyoutState();

                    if (flyout.IsOpen)
                        flyout.Hide();
                }
            }
        }

        public double ZoomFactor
        {
            get { return (double)GetValue(ZoomFactorProperty); }
            set { SetValue(ZoomFactorProperty, value); }
        }

        public static readonly DependencyProperty ZoomFactorProperty =
            DependencyProperty.Register(nameof(ZoomFactor),
                typeof(double),
                typeof(SimpleColorPicker),
                new PropertyMetadata(1.5));

        public Orientation PaletteOrientation
        {
            get { return (Orientation)GetValue(PaletteOrientationProperty); }
            set { SetValue(PaletteOrientationProperty, value); }
        }

        public static readonly DependencyProperty PaletteOrientationProperty =
            DependencyProperty.Register(nameof(PaletteOrientation),
                typeof(Orientation),
                typeof(SimpleColorPicker),
                new PropertyMetadata(Orientation.Horizontal));

        public bool IsMiniPalette
        {
            get { return (bool)GetValue(IsMiniPaletteProperty); }
            set { SetValue(IsMiniPaletteProperty, value); }
        }

        public static readonly DependencyProperty IsMiniPaletteProperty =
            DependencyProperty.Register(nameof(IsMiniPalette),
                typeof(bool),
                typeof(SimpleColorPicker),
                new PropertyMetadata(false));

        public IEnumerable<Color> Palette
        {
            get { return (IEnumerable<Color>)GetValue(PaletteProperty); }
            set { SetValue(PaletteProperty, value); }
        }

        public static readonly DependencyProperty PaletteProperty =
            DependencyProperty.Register(nameof(Palette),
                typeof(IEnumerable<Color>),
                typeof(SimpleColorPicker),
                new PropertyMetadata(new ColorCollection(), PalettePropertyChanged));

        private static void PalettePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SimpleColorPicker picker = (SimpleColorPicker)d;
            picker.grid?.Children.Clear();
        }

        public int CellsPerColumn
        {
            get { return (int)GetValue(CellsPerColumnProperty); }
            set { SetValue(CellsPerColumnProperty, value); }
        }

        public static readonly DependencyProperty CellsPerColumnProperty =
            DependencyProperty.Register(nameof(CellsPerColumn),
                typeof(int),
                typeof(SimpleColorPicker),
                new PropertyMetadata(0, CellsPerColumnPropertyChanged));

        private static void CellsPerColumnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SimpleColorPicker picker = (SimpleColorPicker)d;
            picker.grid?.Children.Clear();
        }

        public bool ShouldConstrainToRootBounds
        {
            get { return (bool)GetValue(ShouldConstrainToRootBoundsProperty); }
            set { SetValue(ShouldConstrainToRootBoundsProperty, value); }
        }

        public static readonly DependencyProperty ShouldConstrainToRootBoundsProperty =
            DependencyProperty.Register(nameof(ShouldConstrainToRootBounds),
                typeof(bool),
                typeof(SimpleColorPicker),
                new PropertyMetadata(true));

        private record Pos(int X, int Y)   // record structs are in language version 10.0
        {
            public Pos NextLeft() => new Pos(X - 1, Y); 
            public Pos NextRight() => new Pos(X + 1, Y);
            public Pos NextUp() => new Pos(X, Y - 1);
            public Pos NextDown() => new Pos(X, Y + 1);

            public Pos GoToStartOfNextRow() => new Pos(0, Y + 1);
            public Pos GoToStartOfNextColumn() => new Pos(X + 1, 0);
            public Pos GoToEndOfPreviousRow(int xCount) => new Pos(xCount - 1, Y - 1);
            public Pos GoToEndOfPreviousColumn(int yCount) => new Pos(X - 1, yCount - 1);
        };


        private void CreateCustomPaletteGrid()
        {
            Debug.Assert(grid is not null);
            Debug.Assert(grid.Children.Count == 0);

            int total = Palette.Count();
            int rows = Math.Min(CellsPerColumn, total);
            int columns = (total / CellsPerColumn) + ((total % CellsPerColumn) > 0 ? 1 : 0);

            if (PaletteOrientation == Orientation.Vertical)
                (rows, columns) = (columns, rows);

            SetGridColumnRowDefinitions(columns, rows);

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    int colorIndex = PaletteOrientation == Orientation.Horizontal ? (x * CellsPerColumn) + y : (y * CellsPerColumn) + x;

                    if (colorIndex < total)
                    {
                        grid.Children.Add(CreateBorder(x, y, Palette.ElementAt(colorIndex)));
                    }
                }
            }
        }

        private void SetGridColumnRowDefinitions(int columns, int rows)
        {
            Debug.Assert(grid is not null);

            if (grid.RowDefinitions.Count != rows)
            {
                grid.RowDefinitions.Clear();

                while (rows-- > 0)
                    grid.RowDefinitions.Add(new RowDefinition());
            }

            if (grid.ColumnDefinitions.Count != columns)
            {
                grid.ColumnDefinitions.Clear();

                while (columns-- > 0)
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
            }
        }

        private void CreateDefaultPaletteGrid()
        {
            Debug.Assert(grid is not null);
            Debug.Assert(grid.Children.Count == 0);

            int rows;
            int columns;

            if (IsMiniPalette)
            {
                rows = cDefaultSamplesPerColor;
                columns = sMiniPaletteColumnOffsets.Length;
            }
            else
            {
                rows = cDefaultSamplesPerColor;
                columns = sRGB.Length / cDefaultSamplesPerColor;
            }

            if (PaletteOrientation == Orientation.Vertical)
                (rows, columns) = (columns, rows);

            SetGridColumnRowDefinitions(columns, rows);

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    int colorIndex;

                    if (IsMiniPalette)
                        colorIndex = PaletteOrientation == Orientation.Horizontal ? sMiniPaletteColumnOffsets[x] + y : sMiniPaletteColumnOffsets[y] + x;
                    else
                        colorIndex = PaletteOrientation == Orientation.Horizontal ? (x * cDefaultSamplesPerColor) + y : (y * cDefaultSamplesPerColor) + x;

                    grid.Children.Add(CreateBorder(x, y, ConvertToColor(sRGB[colorIndex])));
                }
            }
        }

        private Border CreateBorder(int x, int y, Color color)
        {
            Border border = new Border();

            border.Tag = new Pos(x, y);
            border.Background = new SolidColorBrush(color);
            border.ScaleTransition = new Vector3Transition();
            border.PointerEntered += Border_PointerEntered;
            border.PointerExited += Border_PointerExited;
            border.PointerReleased += Border_PointReleased;
            border.GotFocus += Border_GotFocus;
            border.LostFocus += Border_LostFocus;
            border.KeyUp += Border_KeyUp;
            border.KeyDown += Border_KeyDown;

            if (CellStyle is not null)
                border.Style = CellStyle;

            Grid.SetRow(border, y);
            Grid.SetColumn(border, x);

            return border;
        }

        private static Color ConvertToColor(uint rgb)
        {
            return new Color()
            {
                A = 0xFF,
                R = (byte)(rgb >> 16),
                G = (byte)((rgb >> 8) & 0x000000FF),
                B = (byte)(rgb & 0x000000FF),
            };
        }

        public Style? CellStyle
        {
            private get => cellStyle;
            set
            {
                if ((value is not null) && (value.TargetType == typeof(Border)))
                    cellStyle = value;
            }
        }

        public Style? FlyoutPresenterStyle
        {
            private get => flyoutPresenterStyle;
            set
            {
                if ((value is not null) && (value.TargetType == typeof(FlyoutPresenter)))
                    flyoutPresenterStyle = value;
            }
        }
                
        private void ZoomColorOut(Border border)
        {
            border.CenterPoint = new Vector3((float)(border.ActualWidth / 2.0), (float)(border.ActualHeight / 2.0), 1.0f);
            Canvas.SetZIndex(border, 1);
            border.Scale = new Vector3((float)ZoomFactor, (float)ZoomFactor, 1.0f);
        }

        private static void ZoomColorIn(Border border)
        {
            Canvas.SetZIndex(border, 0);
            border.Scale = new Vector3(1.0f, 1.0f, 1.0f);
        }

        private void Border_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Border border = (Border)sender;

            if (border.IsTabStop)
                border.Focus(FocusState.Programmatic);
            else
                ZoomColorOut(border);
        }

        private static void Border_PointerExited(object sender, PointerRoutedEventArgs e)
        {                   
            Border border = (Border)sender;

            if (!border.IsTabStop)
                ZoomColorIn(border);
        }

        private void Border_PointReleased(object sender, PointerRoutedEventArgs e)
        {
            SetPickedColor((Border)sender);
            IsFlyoutOpen = false;
        }

        private void Border_GotFocus(object sender, RoutedEventArgs e)
        {
            ZoomColorOut((Border)sender);
        }

        private static void Border_LostFocus(object sender, RoutedEventArgs e)
        {
            ZoomColorIn((Border)sender);
        }

        private void SetPickedColor(Border border)
        {
            Color newColor = ((SolidColorBrush)border.Background).Color;

            if (newColor != Color)
                Color = newColor;
        }

        private void Border_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                SetPickedColor((Border)sender);
                IsFlyoutOpen = false;
            }
        }

        private void Border_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            Debug.Assert(grid is not null);

            if ((e.Key == VirtualKey.Up) || (e.Key == VirtualKey.Down) || (e.Key == VirtualKey.Left) || (e.Key == VirtualKey.Right))
            {
                if ((DateTime.UtcNow - lastKeyRepeat) > TimeSpan.FromMilliseconds(100)) // throttle focus changes
                {
                    lastKeyRepeat = DateTime.UtcNow;

                    Pos pos = (Pos)((Border)sender).Tag;
                    Pos newPos;

                    switch (e.Key)
                    {
                        case VirtualKey.Up: newPos = MoveUp(pos); break;
                        case VirtualKey.Down: newPos = MoveDown(pos); break;
                        case VirtualKey.Left: newPos = MoveLeft(pos); break;
                        default: newPos = MoveRight(pos); break;
                    }

                    UIElement? child = grid.Children.FirstOrDefault(x => (Pos)((Border)x).Tag == newPos);
                    Debug.Assert(child is not null);

                    child?.Focus(FocusState.Programmatic);
                }
            }
        }

        private bool IsInsideGrid(Pos pos)
        {
            Debug.Assert(grid is not null);

            int xCount = grid.ColumnDefinitions.Count;
            int yCount = grid.RowDefinitions.Count;

            if ((pos.X < 0) || (pos.Y < 0) || (pos.X >= xCount) || (pos.Y >= yCount))
                return false;

            if ((pos.X < (xCount - 1)) && (pos.Y < (yCount - 1))) // the last row or column could be partially filled
                return true;

            Pos last = Last();

            if ((pos.X == (xCount - 1)) && (pos.Y > last.Y))
                return false;
         
            if ((pos.Y == (yCount - 1)) && (pos.X > last.X))
                return false;

            return true;
        }

        private Pos Last()
        {
            Debug.Assert(grid is not null);

            int childCount = grid.Children.Count;
            int xCount = grid.ColumnDefinitions.Count;
            int yCount = grid.RowDefinitions.Count;

            if ((xCount * yCount) == childCount)
                return new Pos(xCount - 1, yCount - 1);

            if (PaletteOrientation == Orientation.Horizontal)
                return new Pos(xCount - 1, (childCount - 1) % yCount);

            return new Pos((childCount - 1) % xCount, yCount - 1);
        }

        private Pos MoveLeft(Pos currentPos)
        {
            Debug.Assert(grid is not null);

            int xCount = grid.ColumnDefinitions.Count;
            int yCount = grid.RowDefinitions.Count;

            Pos newPos = currentPos.NextLeft();

            if (IsInsideGrid(newPos))
                return newPos;

            newPos = newPos.GoToEndOfPreviousRow(xCount);

            if (IsInsideGrid(newPos))
                return newPos;

            if (newPos.Y > 0)  // valid row but a partial column
            {
                newPos = newPos.NextLeft();

                if (IsInsideGrid(newPos))
                    return newPos;
            }

            // roll back over to the end
            if (PaletteOrientation == Orientation.Horizontal) // the right most cell in the last row
            {
                newPos = new Pos(xCount - 1, yCount - 1); // bottom right corner

                if (IsInsideGrid(newPos))
                    return newPos;

                return newPos.NextLeft();
            }
            
            return Last();  
        }

        private Pos MoveRight(Pos currentPos)
        {
            Pos newPos = currentPos.NextRight();

            if (IsInsideGrid(newPos))
                return newPos;

            newPos = newPos.GoToStartOfNextRow();

            if (IsInsideGrid(newPos))
                return newPos;

            return new Pos(0, 0); // roll over to the start
        }

        private Pos MoveUp(Pos currentPos)
        {
            Debug.Assert(grid is not null);

            int childCount = grid.Children.Count;
            int xCount = grid.ColumnDefinitions.Count;
            int yCount = grid.RowDefinitions.Count;

            Pos newPos = currentPos.NextUp();

            if (IsInsideGrid(newPos))
                return newPos;

            newPos = currentPos.GoToEndOfPreviousColumn(yCount);

            if (IsInsideGrid(newPos))
                return newPos;

            newPos = newPos.NextUp();

            if (IsInsideGrid(newPos))
                return newPos;

            // roll over to end
            if (PaletteOrientation == Orientation.Horizontal)
                return Last();

            return new Pos(xCount - 1, (childCount / xCount) - 1); // last cell in the right most column
        }

        private Pos MoveDown(Pos currentPos)
        {
            Pos newPos = currentPos.NextDown();

            if (IsInsideGrid(newPos))
                return newPos;

            newPos = currentPos.GoToStartOfNextColumn();

            if (IsInsideGrid(newPos))
                return newPos;

            return new Pos(0, 0);
        }

        private void PickButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
        {
            // It's being opened via the keyboard or a click on the indicator part of the split button.
            // In this case enable keyboard navigation, an initial color border will be selected once IsTabStop is set.
            // Wen clicking the down arrow, IsTabStop will be false with no initial selection and mouse only navigation.

            if (!sender.Flyout.IsOpen)
            {
                Debug.Assert(grid is not null);

                foreach (UIElement child in grid.Children)
                    child.IsTabStop = true;

                IsFlyoutOpen = true;
            }
        }

        private void ResetFlyoutState()
        {
            Debug.Assert(grid is not null);

            foreach (UIElement child in grid.Children)
            {
                if (child is Border border)
                {
                    border.IsTabStop = false;
                    ZoomColorIn(border);
                }
            }
        }

        private readonly static int[] sMiniPaletteColumnOffsets = { 0, 20, 40, 60, 80, 100, 120, 140, 150, 180 };

        private readonly static uint[] sRGB =
        {
            // Red
            0x5F1616,
            0x7F1D1D,
            0x991B1B,
            0xB91C1C,
            0xEF1010,
            0xEF3434,
            0xF87171,
            0xFCA5A5,
            0xFECACA,
            0xFEE2E2,
            // Orange
            0x58200D,
            0x7C2D12,
            0x9A3412,
            0xC2410C,
            0xEA580C,
            0xF97316,
            0xFB923C,
            0xFDBA74,
            0xFED7AA,
            0xFFEDD5,
            // Amber
            0x5A270B,
            0x78350F,
            0x92400E,
            0xB45309,
            0xD97706,
            0xF59E0B,
            0xFBBF24,
            0xFCD34D,
            0xFDE68A,
            0xFEF3C7,
            // Yellow
            0x532E0D,
            0x713F12,
            0x854D0E,
            0xA16207,
            0xCA8A04,
            0xEAB308,
            0xFACC15,
            0xFDE047,
            0xFEF08A,
            0xFEF9C3,
            // Lime
            0x23350C,
            0x365314,
            0x3F6212,
            0x4D7C0F,
            0x65A30D,
            0x84CC16,
            0xA3E635,
            0xBEF264,
            0xD9F99D,
            0xECFCCB,
            // Green
            0x0E3D20,
            0x14532D,
            0x166534,
            0x15803D,
            0x16A34A,
            0x22C55E,
            0x4ADE80,
            0x86EFAC,
            0xBBF7D0,
            0xDCFCE7,
            // Emerald
            0x043D2E,
            0x064E3B,
            0x065F46,
            0x047857,
            0x059669,
            0x10B981,
            0x34D399,
            0x6EE7B7,
            0xA7F3D0,
            0xD1FAE5,
            // Teal
            0x0F3D39,
            0x134E4A,
            0x115E59,
            0x0F766E,
            0x0D9488,
            0x14B8A6,
            0x2DD4BF,
            0x5EEAD4,
            0x99F6E4,
            0xCCFBF1,
            // Cyan
            0x124153,
            0x164E63,
            0x155E75,
            0x0E7490,
            0x0891B2,
            0x06B6D4,
            0x22D3EE,
            0x67E8F9,
            0xA5F3FC,
            0xCFFAFE,
            // Light Blue
            0x0A3D5B,
            0x0C4A6E,
            0x075985,
            0x0369A1,
            0x0284C7,
            0x0EA5E9,
            0x38BDF8,
            0x7DD3FC,
            0xBAE6FD,
            0xE0F2FE,
            // Blue
            0x152960,
            0x1E3A8A,
            0x1E40AF,
            0x1D4ED8,
            0x2563EB,
            0x3B82F6,
            0x60A5FA,
            0x93C5FD,
            0xBFDBFE,
            0xDBEAFE,
            // Indigo
            0x222059,
            0x312E81,
            0x3730A3,
            0x4338CA,
            0x4F46E5,
            0x6366F1,
            0x818CF8,
            0xA5B4FC,
            0xC7D2FE,
            0xE0E7FF,
            // Violet
            0x311361,
            0x4C1D95,
            0x5B21B6,
            0x6D28D9,
            0x7C3AED,
            0x8B5CF6,
            0xA78BFA,
            0xC4B5FD,
            0xDDD6FE,
            0xEDE9FE,
            // Purple
            0x361154,
            0x581C87,
            0x6B21A8,
            0x7E22CE,
            0x9333EA,
            0xA855F7,
            0xC084FC,
            0xD8B4FE,
            0xE9D5FF,
            0xF3E8FF,
            // Fuchsia
            0x46104A,
            0x701A75,
            0x86198F,
            0xA21CAF,
            0xC026D3,
            0xD946EF,
            0xE879F9,
            0xF0ABFC,
            0xF5D0FE,
            0xFAE8FF,
            // Pink
            0x5E1131,
            0x831843,
            0x9D174D,
            0xBE185D,
            0xDB2777,
            0xEC4899,
            0xF472B6,
            0xF9A8D4,
            0xFBCFE8,
            0xFCE7F3,
            // Rose
            0x550B22,
            0x881337,
            0x9F1239,
            0xBE123C,
            0xE11D48,
            0xF43F5E,
            0xFB7185,
            0xFDA4AF,
            0xFECDD3,
            0xFFE4E6,
            // Blue Gray
            0x090E1A,
            0x0F172A,
            0x1E293B,
            0x334155,
            0x475569,
            0x64748B,
            0x94A3B8,
            0xCBD5E1,
            0xE2E8F0,
            0xF1F5F9,
            // True Gray
            0x000000,
            0x171717,
            0x262626,
            0x404040,
            0x525252,
            0x737373,
            0xA3A3A3,
            0xD4D4D4,
            0xE5E5E5,
            0xFFFFFF
        };
    }

    public class ColorCollection : List<Color>
    {
        public ColorCollection() : base() { }
        public ColorCollection(IEnumerable<Color> colors) : base(colors) { }
    }
}
