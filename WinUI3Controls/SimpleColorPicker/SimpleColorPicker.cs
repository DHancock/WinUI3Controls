using Microsoft.UI.Input;
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
using Windows.UI.Core;

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
        private Border? selected;

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
                    flyout.Opening += Flyout_Opening;
                    flyout.Opened += Flyout_Opened; 
                    flyout.Closed += Flyout_Closed;

                    grid = root;
                    grid.IsTabStop = true;
                    grid.PreviewKeyDown += Grid_PreviewKeyDown;
                    grid.PreviewKeyUp += Grid_PreviewKeyUp;
                    grid.PointerExited += Grid_PointerExited;

                    // changing the constraint value after the flyout has been shown will throw an exception
                    flyout.ShouldConstrainToRootBounds = ShouldConstrainToRootBounds;

                    if (FlyoutPresenterStyle is not null)
                        flyout.FlyoutPresenterStyle = FlyoutPresenterStyle;
                    
                    if (IsFlyoutOpen)
                        Loaded += SimpleColorPicker_Loaded;
                }
            }
        }

        private void Flyout_Opening(object? sender, object e)
        {
            if (grid?.Children.Count == 0)
            {
                if (IsCustomPalette)
                    CreateCustomPaletteGrid();
                else
                    CreateDefaultPaletteGrid();
            }

            IsFlyoutOpen = true;
        }

        private void Flyout_Opened(object? sender, object e)
        {
            AttemptSelectCurrentColor();
            FlyoutOpened?.Invoke(this, true);
        }

        private void Flyout_Closed(object? sender, object e)
        {
            if (selected is not null)
            {
                ResetZoom(selected);
                selected = null;
            }

            IsFlyoutOpen = false;
            FlyoutClosed?.Invoke(this, false);
        }

        private void PickButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
        {
            // The flyout is being opened via the keyboard or a click on the indicator part of the split button.
            // When clicking the down arrow, this code isn't called.
            IsFlyoutOpen = true;
        }

        private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (selected is not null)
                ResetZoom(selected);

            e.Handled = true;
        }

        private void Grid_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if ((e.Key == VirtualKey.Tab) || (e.Key == VirtualKey.Up) || (e.Key == VirtualKey.Down) || (e.Key == VirtualKey.Left) || (e.Key == VirtualKey.Right))
            {
                if ((DateTime.UtcNow - lastKeyRepeat) > TimeSpan.FromMilliseconds(100)) // throttle changes
                {
                    lastKeyRepeat = DateTime.UtcNow;

                    Border? newSelection = null;
                    VirtualKey key = e.Key;

                    if (key == VirtualKey.Tab)
                    {
                        bool shift = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

                        if (shift)
                        {
                            if (selected is null)
                                newSelection = grid?.Children[grid.Children.Count - 1] as Border;
                            else
                                key = VirtualKey.Left;
                        }
                        else
                        {
                            if (selected is null)
                                newSelection = grid?.Children[0] as Border;
                            else
                                key = VirtualKey.Right;
                        }
                    }
                    else if ((key == VirtualKey.Right) && (FlowDirection == FlowDirection.RightToLeft))
                    {
                        key =  VirtualKey.Left;
                    }
                    else if ((key == VirtualKey.Left) && (FlowDirection == FlowDirection.RightToLeft))
                    {
                        key = VirtualKey.Right;
                    }

                    if ((newSelection is null) && (selected is not null))
                    {
                        Pos pos = (Pos)selected.Tag;
                        Pos newPos;

                        switch (key)
                        {
                            case VirtualKey.Up: newPos = MoveUp(pos); break;
                            case VirtualKey.Down: newPos = MoveDown(pos); break;
                            case VirtualKey.Left: newPos = MoveLeft(pos); break;
                            case VirtualKey.Right: newPos = MoveRight(pos); break;
                            default: Debug.Fail(key.ToString()); return;
                        }

                        newSelection = grid?.Children.FirstOrDefault(x => (Pos)((Border)x).Tag == newPos) as Border;
                        Debug.Assert(newSelection is not null);
                    }

                    if (newSelection is not null)
                    {
                        if (selected is not null)
                            ResetZoom(selected);

                        selected = newSelection;
                        ZoomOut(newSelection);
                    }
                }
            }

            e.Handled = true;
        }

        private void Grid_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if ((e.Key == VirtualKey.Space) || (e.Key == VirtualKey.Enter))
            {
                IsFlyoutOpen = false;

                if ((selected is not null) && (selected.Scale != Vector3.One))
                    SetPickedColor(selected);
            }

            e.Handled = true;
        }

        private static void SimpleColorPicker_Loaded(object sender, RoutedEventArgs e)
        {
            SimpleColorPicker picker = (SimpleColorPicker)sender;
            SetFlyoutOpenState(picker, picker.IsFlyoutOpen);
        }

        private bool IsCustomPalette => (Palette is not null) && Palette.Any() && (CellsPerColumn > 0);

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
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
            get => (double)GetValue(IndicatorWidthProperty);
            set => SetValue(IndicatorWidthProperty, value);
        }

        public static readonly DependencyProperty IndicatorWidthProperty =
            DependencyProperty.Register(nameof(IndicatorWidth),
                typeof(double),
                typeof(SimpleColorPicker),
                new PropertyMetadata(35.0));

        public bool IsFlyoutOpen
        {
            get => (bool)GetValue(IsFlyoutOpenProperty);
            set => SetValue(IsFlyoutOpenProperty, value);
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
            if (picker.IsLoaded && (picker.pickButton is not null) && (picker.pickButton.Flyout is not null))
            {
                FlyoutBase flyout = picker.pickButton.Flyout;

                if (toOpen)
                {
                    Debug.Assert(flyout.IsOpen == false);

                    if (!flyout.IsOpen)
                    {
                        if (picker.FlowDirection == FlowDirection.LeftToRight)
                            flyout.ShowAt(picker.pickButton, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft });
                        else
                            flyout.ShowAt(picker.pickButton, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.BottomEdgeAlignedRight });
                    }
                }
                else if (flyout.IsOpen)
                    flyout.Hide();
            }
        }

        public double ZoomFactor
        {
            get => (double)GetValue(ZoomFactorProperty);
            set => SetValue(ZoomFactorProperty, value);
        }

        public static readonly DependencyProperty ZoomFactorProperty =
            DependencyProperty.Register(nameof(ZoomFactor),
                typeof(double),
                typeof(SimpleColorPicker),
                new PropertyMetadata(1.5));

        public Orientation PaletteOrientation
        {
            get => (Orientation)GetValue(PaletteOrientationProperty);
            set => SetValue(PaletteOrientationProperty, value);
        }

        public static readonly DependencyProperty PaletteOrientationProperty =
            DependencyProperty.Register(nameof(PaletteOrientation),
                typeof(Orientation),
                typeof(SimpleColorPicker),
                new PropertyMetadata(Orientation.Horizontal));

        public bool IsMiniPalette
        {
            get => (bool)GetValue(IsMiniPaletteProperty);
            set => SetValue(IsMiniPaletteProperty, value);
        }

        public static readonly DependencyProperty IsMiniPaletteProperty =
            DependencyProperty.Register(nameof(IsMiniPalette),
                typeof(bool),
                typeof(SimpleColorPicker),
                new PropertyMetadata(false));

        public IEnumerable<Color> Palette
        {
            get => (IEnumerable<Color>)GetValue(PaletteProperty);
            set => SetValue(PaletteProperty, value);
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
            get => (int)GetValue(CellsPerColumnProperty);
            set => SetValue(CellsPerColumnProperty, value);
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
            get => (bool)GetValue(ShouldConstrainToRootBoundsProperty);
            set => SetValue(ShouldConstrainToRootBoundsProperty, value);
        }

        public static readonly DependencyProperty ShouldConstrainToRootBoundsProperty =
            DependencyProperty.Register(nameof(ShouldConstrainToRootBounds),
                typeof(bool),
                typeof(SimpleColorPicker),
                new PropertyMetadata(false));

        public enum InitialSelection { None, First, ExactMatch, ClosestMatch }

        public InitialSelection InitialSelectionMode
        {
            get => (InitialSelection)GetValue(InitialSelectionModeProperty);
            set => SetValue(InitialSelectionModeProperty, value);
        }

        public static readonly DependencyProperty InitialSelectionModeProperty =
            DependencyProperty.Register(nameof(InitialSelectionMode),
                typeof(InitialSelection),
                typeof(SimpleColorPicker),
                new PropertyMetadata(InitialSelection.None));

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

                    grid.Children.Add(CreateBorder(x, y, sRGB[colorIndex]));
                }
            }
        }

        private Border CreateBorder(int x, int y, Color color)
        {
            Border border = new Border();

            border.Tag = new Pos(x, y);
            border.Background = new SolidColorBrush(color);
            border.ScaleTransition = new Vector3Transition();
            border.ScaleTransition.Duration = TimeSpan.FromMilliseconds(200);
            border.PointerEntered += Border_PointerEntered;
            border.PointerReleased += Border_PointReleased;

            if (CellStyle is not null)
                border.Style = CellStyle;

            Grid.SetRow(border, y);
            Grid.SetColumn(border, x);

            return border;
        }

        private void AttemptSelectCurrentColor()
        {
            Debug.Assert(grid is not null);
            Debug.Assert(selected is null);

            if (InitialSelectionMode == InitialSelection.None)
                return;

            if (InitialSelectionMode == InitialSelection.First)
            {
                if (grid.Children.Count > 0)
                    selected = grid.Children[0] as Border;
            }
            else
            {
                static double PerceivedColorDifference(Color a, Color b)
                {
                    static double GrayScale(Color c) => (c.R * 0.30) + (c.G * 0.59) + (c.B * 0.11);
                    return Math.Abs(GrayScale(a) - GrayScale(b));
                }

                double minDifference = double.MaxValue;
                Border? exactMatch = null;
                Border? closestMatch = null;

                foreach (UIElement child in grid.Children)
                {
                    if (child is Border border)
                    {
                        double difference = PerceivedColorDifference(Color, ((SolidColorBrush)border.Background).Color);

                        if (difference < 0.00001)
                        {
                            exactMatch = border;
                            break;
                        }
                        else if (difference < minDifference)
                        {
                            closestMatch = border;
                            minDifference = difference;
                        }
                    }
                }

                if (exactMatch is not null)
                    selected = exactMatch;
                else if (InitialSelectionMode == InitialSelection.ClosestMatch)
                    selected = closestMatch;
            }

            if (selected is not null)
                ZoomOut(selected);
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
                
        private void ZoomOut(Border border)
        {
            border.CenterPoint = new Vector3((float)(border.ActualWidth / 2.0), (float)(border.ActualHeight / 2.0), 1.0f);
            Canvas.SetZIndex(border, 1);
            border.Scale = new Vector3((float)ZoomFactor, (float)ZoomFactor, 1.0f);
        }

        private static void ResetZoom(Border border)
        {
            Canvas.SetZIndex(border, 0);
            border.Scale = Vector3.One;
        }

        private void Border_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.IsGenerated)
                return;

            Border border = (Border)sender;

            if (selected is not null)
                ResetZoom(selected);

            selected = border;
            ZoomOut(border);
        }

        private void Border_PointReleased(object sender, PointerRoutedEventArgs e)
        {
            SetPickedColor((Border)sender);
            ResetZoom((Border)sender);
            IsFlyoutOpen = false;
        }

        private void SetPickedColor(Border border)
        {
            Color newColor = ((SolidColorBrush)border.Background).Color;

            if (newColor != Color)
                Color = newColor;
        }

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


        private static readonly int[] sMiniPaletteColumnOffsets = { 0, 20, 40, 60, 80, 100, 120, 140, 150, 180 };

        private static readonly Color[] sRGB =
        {
            // Red
            new() { A = 0xFF, R = 0x5F,  G = 0x16, B = 0x16 },
            new() { A = 0xFF, R = 0x7F,  G = 0x1D, B = 0x1D },
            new() { A = 0xFF, R = 0x99,  G = 0x1B, B = 0x1B },
            new() { A = 0xFF, R = 0xB9,  G = 0x1C, B = 0x1C },
            new() { A = 0xFF, R = 0xEF,  G = 0x10, B = 0x10 },
            new() { A = 0xFF, R = 0xEF,  G = 0x34, B = 0x34 },
            new() { A = 0xFF, R = 0xF8,  G = 0x71, B = 0x71 },
            new() { A = 0xFF, R = 0xFC,  G = 0xA5, B = 0xA5 },
            new() { A = 0xFF, R = 0xFE,  G = 0xCA, B = 0xCA },
            new() { A = 0xFF, R = 0xFE,  G = 0xE2, B = 0xE2 },
            // Orange
            new() { A = 0xFF, R = 0x58,  G = 0x20, B = 0x0D },
            new() { A = 0xFF, R = 0x7C,  G = 0x2D, B = 0x12 },
            new() { A = 0xFF, R = 0x9A,  G = 0x34, B = 0x12 },
            new() { A = 0xFF, R = 0xC2,  G = 0x41, B = 0x0C },
            new() { A = 0xFF, R = 0xEA,  G = 0x58, B = 0x0C },
            new() { A = 0xFF, R = 0xF9,  G = 0x73, B = 0x16 },
            new() { A = 0xFF, R = 0xFB,  G = 0x92, B = 0x3C },
            new() { A = 0xFF, R = 0xFD,  G = 0xBA, B = 0x74 },
            new() { A = 0xFF, R = 0xFE,  G = 0xD7, B = 0xAA },
            new() { A = 0xFF, R = 0xFF,  G = 0xED, B = 0xD5 },
            // Amber
            new() { A = 0xFF, R = 0x5A,  G = 0x27, B = 0x0B },
            new() { A = 0xFF, R = 0x78,  G = 0x35, B = 0x0F },
            new() { A = 0xFF, R = 0x92,  G = 0x40, B = 0x0E },
            new() { A = 0xFF, R = 0xB4,  G = 0x53, B = 0x09 },
            new() { A = 0xFF, R = 0xD9,  G = 0x77, B = 0x06 },
            new() { A = 0xFF, R = 0xF5,  G = 0x9E, B = 0x0B },
            new() { A = 0xFF, R = 0xFB,  G = 0xBF, B = 0x24 },
            new() { A = 0xFF, R = 0xFC,  G = 0xD3, B = 0x4D },
            new() { A = 0xFF, R = 0xFD,  G = 0xE6, B = 0x8A },
            new() { A = 0xFF, R = 0xFE,  G = 0xF3, B = 0xC7 },
            // Yellow
            new() { A = 0xFF, R = 0x53,  G = 0x2E, B = 0x0D },
            new() { A = 0xFF, R = 0x71,  G = 0x3F, B = 0x12 },
            new() { A = 0xFF, R = 0x85,  G = 0x4D, B = 0x0E },
            new() { A = 0xFF, R = 0xA1,  G = 0x62, B = 0x07 },
            new() { A = 0xFF, R = 0xCA,  G = 0x8A, B = 0x04 },
            new() { A = 0xFF, R = 0xEA,  G = 0xB3, B = 0x08 },
            new() { A = 0xFF, R = 0xFA,  G = 0xCC, B = 0x15 },
            new() { A = 0xFF, R = 0xFD,  G = 0xE0, B = 0x47 },
            new() { A = 0xFF, R = 0xFE,  G = 0xF0, B = 0x8A },
            new() { A = 0xFF, R = 0xFE,  G = 0xF9, B = 0xC3 },
            // Lime
            new() { A = 0xFF, R = 0x23,  G = 0x35, B = 0x0C },
            new() { A = 0xFF, R = 0x36,  G = 0x53, B = 0x14 },
            new() { A = 0xFF, R = 0x3F,  G = 0x62, B = 0x12 },
            new() { A = 0xFF, R = 0x4D,  G = 0x7C, B = 0x0F },
            new() { A = 0xFF, R = 0x65,  G = 0xA3, B = 0x0D },
            new() { A = 0xFF, R = 0x84,  G = 0xCC, B = 0x16 },
            new() { A = 0xFF, R = 0xA3,  G = 0xE6, B = 0x35 },
            new() { A = 0xFF, R = 0xBE,  G = 0xF2, B = 0x64 },
            new() { A = 0xFF, R = 0xD9,  G = 0xF9, B = 0x9D },
            new() { A = 0xFF, R = 0xEC,  G = 0xFC, B = 0xCB },
            // Green
            new() { A = 0xFF, R = 0x0E,  G = 0x3D, B = 0x20 },
            new() { A = 0xFF, R = 0x14,  G = 0x53, B = 0x2D },
            new() { A = 0xFF, R = 0x16,  G = 0x65, B = 0x34 },
            new() { A = 0xFF, R = 0x15,  G = 0x80, B = 0x3D },
            new() { A = 0xFF, R = 0x16,  G = 0xA3, B = 0x4A },
            new() { A = 0xFF, R = 0x22,  G = 0xC5, B = 0x5E },
            new() { A = 0xFF, R = 0x4A,  G = 0xDE, B = 0x80 },
            new() { A = 0xFF, R = 0x86,  G = 0xEF, B = 0xAC },
            new() { A = 0xFF, R = 0xBB,  G = 0xF7, B = 0xD0 },
            new() { A = 0xFF, R = 0xDC,  G = 0xFC, B = 0xE7 },
            // Emerald
            new() { A = 0xFF, R = 0x04,  G = 0x3D, B = 0x2E },
            new() { A = 0xFF, R = 0x06,  G = 0x4E, B = 0x3B },
            new() { A = 0xFF, R = 0x06,  G = 0x5F, B = 0x46 },
            new() { A = 0xFF, R = 0x04,  G = 0x78, B = 0x57 },
            new() { A = 0xFF, R = 0x05,  G = 0x96, B = 0x69 },
            new() { A = 0xFF, R = 0x10,  G = 0xB9, B = 0x81 },
            new() { A = 0xFF, R = 0x34,  G = 0xD3, B = 0x99 },
            new() { A = 0xFF, R = 0x6E,  G = 0xE7, B = 0xB7 },
            new() { A = 0xFF, R = 0xA7,  G = 0xF3, B = 0xD0 },
            new() { A = 0xFF, R = 0xD1,  G = 0xFA, B = 0xE5 },
            // Teal
            new() { A = 0xFF, R = 0x0F,  G = 0x3D, B = 0x39 },
            new() { A = 0xFF, R = 0x13,  G = 0x4E, B = 0x4A },
            new() { A = 0xFF, R = 0x11,  G = 0x5E, B = 0x59 },
            new() { A = 0xFF, R = 0x0F,  G = 0x76, B = 0x6E },
            new() { A = 0xFF, R = 0x0D,  G = 0x94, B = 0x88 },
            new() { A = 0xFF, R = 0x14,  G = 0xB8, B = 0xA6 },
            new() { A = 0xFF, R = 0x2D,  G = 0xD4, B = 0xBF },
            new() { A = 0xFF, R = 0x5E,  G = 0xEA, B = 0xD4 },
            new() { A = 0xFF, R = 0x99,  G = 0xF6, B = 0xE4 },
            new() { A = 0xFF, R = 0xCC,  G = 0xFB, B = 0xF1 },
            // Cyan
            new() { A = 0xFF, R = 0x12,  G = 0x41, B = 0x53 },
            new() { A = 0xFF, R = 0x16,  G = 0x4E, B = 0x63 },
            new() { A = 0xFF, R = 0x15,  G = 0x5E, B = 0x75 },
            new() { A = 0xFF, R = 0x0E,  G = 0x74, B = 0x90 },
            new() { A = 0xFF, R = 0x08,  G = 0x91, B = 0xB2 },
            new() { A = 0xFF, R = 0x06,  G = 0xB6, B = 0xD4 },
            new() { A = 0xFF, R = 0x22,  G = 0xD3, B = 0xEE },
            new() { A = 0xFF, R = 0x67,  G = 0xE8, B = 0xF9 },
            new() { A = 0xFF, R = 0xA5,  G = 0xF3, B = 0xFC },
            new() { A = 0xFF, R = 0xCF,  G = 0xFA, B = 0xFE },
            // Light Blue
            new() { A = 0xFF, R = 0x0A,  G = 0x3D, B = 0x5B },
            new() { A = 0xFF, R = 0x0C,  G = 0x4A, B = 0x6E },
            new() { A = 0xFF, R = 0x07,  G = 0x59, B = 0x85 },
            new() { A = 0xFF, R = 0x03,  G = 0x69, B = 0xA1 },
            new() { A = 0xFF, R = 0x02,  G = 0x84, B = 0xC7 },
            new() { A = 0xFF, R = 0x0E,  G = 0xA5, B = 0xE9 },
            new() { A = 0xFF, R = 0x38,  G = 0xBD, B = 0xF8 },
            new() { A = 0xFF, R = 0x7D,  G = 0xD3, B = 0xFC },
            new() { A = 0xFF, R = 0xBA,  G = 0xE6, B = 0xFD },
            new() { A = 0xFF, R = 0xE0,  G = 0xF2, B = 0xFE },
            // Blue
            new() { A = 0xFF, R = 0x15,  G = 0x29, B = 0x60 },
            new() { A = 0xFF, R = 0x1E,  G = 0x3A, B = 0x8A },
            new() { A = 0xFF, R = 0x1E,  G = 0x40, B = 0xAF },
            new() { A = 0xFF, R = 0x1D,  G = 0x4E, B = 0xD8 },
            new() { A = 0xFF, R = 0x25,  G = 0x63, B = 0xEB },
            new() { A = 0xFF, R = 0x3B,  G = 0x82, B = 0xF6 },
            new() { A = 0xFF, R = 0x60,  G = 0xA5, B = 0xFA },
            new() { A = 0xFF, R = 0x93,  G = 0xC5, B = 0xFD },
            new() { A = 0xFF, R = 0xBF,  G = 0xDB, B = 0xFE },
            new() { A = 0xFF, R = 0xDB,  G = 0xEA, B = 0xFE },
            // Indigo
            new() { A = 0xFF, R = 0x22,  G = 0x20, B = 0x59 },
            new() { A = 0xFF, R = 0x31,  G = 0x2E, B = 0x81 },
            new() { A = 0xFF, R = 0x37,  G = 0x30, B = 0xA3 },
            new() { A = 0xFF, R = 0x43,  G = 0x38, B = 0xCA },
            new() { A = 0xFF, R = 0x4F,  G = 0x46, B = 0xE5 },
            new() { A = 0xFF, R = 0x63,  G = 0x66, B = 0xF1 },
            new() { A = 0xFF, R = 0x81,  G = 0x8C, B = 0xF8 },
            new() { A = 0xFF, R = 0xA5,  G = 0xB4, B = 0xFC },
            new() { A = 0xFF, R = 0xC7,  G = 0xD2, B = 0xFE },
            new() { A = 0xFF, R = 0xE0,  G = 0xE7, B = 0xFF },
            // Violet
            new() { A = 0xFF, R = 0x31,  G = 0x13, B = 0x61 },
            new() { A = 0xFF, R = 0x4C,  G = 0x1D, B = 0x95 },
            new() { A = 0xFF, R = 0x5B,  G = 0x21, B = 0xB6 },
            new() { A = 0xFF, R = 0x6D,  G = 0x28, B = 0xD9 },
            new() { A = 0xFF, R = 0x7C,  G = 0x3A, B = 0xED },
            new() { A = 0xFF, R = 0x8B,  G = 0x5C, B = 0xF6 },
            new() { A = 0xFF, R = 0xA7,  G = 0x8B, B = 0xFA },
            new() { A = 0xFF, R = 0xC4,  G = 0xB5, B = 0xFD },
            new() { A = 0xFF, R = 0xDD,  G = 0xD6, B = 0xFE },
            new() { A = 0xFF, R = 0xED,  G = 0xE9, B = 0xFE },
            // Purple
            new() { A = 0xFF, R = 0x36,  G = 0x11, B = 0x54 },
            new() { A = 0xFF, R = 0x58,  G = 0x1C, B = 0x87 },
            new() { A = 0xFF, R = 0x6B,  G = 0x21, B = 0xA8 },
            new() { A = 0xFF, R = 0x7E,  G = 0x22, B = 0xCE },
            new() { A = 0xFF, R = 0x93,  G = 0x33, B = 0xEA },
            new() { A = 0xFF, R = 0xA8,  G = 0x55, B = 0xF7 },
            new() { A = 0xFF, R = 0xC0,  G = 0x84, B = 0xFC },
            new() { A = 0xFF, R = 0xD8,  G = 0xB4, B = 0xFE },
            new() { A = 0xFF, R = 0xE9,  G = 0xD5, B = 0xFF },
            new() { A = 0xFF, R = 0xF3,  G = 0xE8, B = 0xFF },
            // Fuchsia
            new() { A = 0xFF, R = 0x46,  G = 0x10, B = 0x4A },
            new() { A = 0xFF, R = 0x70,  G = 0x1A, B = 0x75 },
            new() { A = 0xFF, R = 0x86,  G = 0x19, B = 0x8F },
            new() { A = 0xFF, R = 0xA2,  G = 0x1C, B = 0xAF },
            new() { A = 0xFF, R = 0xC0,  G = 0x26, B = 0xD3 },
            new() { A = 0xFF, R = 0xD9,  G = 0x46, B = 0xEF },
            new() { A = 0xFF, R = 0xE8,  G = 0x79, B = 0xF9 },
            new() { A = 0xFF, R = 0xF0,  G = 0xAB, B = 0xFC },
            new() { A = 0xFF, R = 0xF5,  G = 0xD0, B = 0xFE },
            new() { A = 0xFF, R = 0xFA,  G = 0xE8, B = 0xFF },
            // Pink
            new() { A = 0xFF, R = 0x5E,  G = 0x11, B = 0x31 },
            new() { A = 0xFF, R = 0x83,  G = 0x18, B = 0x43 },
            new() { A = 0xFF, R = 0x9D,  G = 0x17, B = 0x4D },
            new() { A = 0xFF, R = 0xBE,  G = 0x18, B = 0x5D },
            new() { A = 0xFF, R = 0xDB,  G = 0x27, B = 0x77 },
            new() { A = 0xFF, R = 0xEC,  G = 0x48, B = 0x99 },
            new() { A = 0xFF, R = 0xF4,  G = 0x72, B = 0xB6 },
            new() { A = 0xFF, R = 0xF9,  G = 0xA8, B = 0xD4 },
            new() { A = 0xFF, R = 0xFB,  G = 0xCF, B = 0xE8 },
            new() { A = 0xFF, R = 0xFC,  G = 0xE7, B = 0xF3 },
            // Rose
            new() { A = 0xFF, R = 0x55,  G = 0x0B, B = 0x22 },
            new() { A = 0xFF, R = 0x88,  G = 0x13, B = 0x37 },
            new() { A = 0xFF, R = 0x9F,  G = 0x12, B = 0x39 },
            new() { A = 0xFF, R = 0xBE,  G = 0x12, B = 0x3C },
            new() { A = 0xFF, R = 0xE1,  G = 0x1D, B = 0x48 },
            new() { A = 0xFF, R = 0xF4,  G = 0x3F, B = 0x5E },
            new() { A = 0xFF, R = 0xFB,  G = 0x71, B = 0x85 },
            new() { A = 0xFF, R = 0xFD,  G = 0xA4, B = 0xAF },
            new() { A = 0xFF, R = 0xFE,  G = 0xCD, B = 0xD3 },
            new() { A = 0xFF, R = 0xFF,  G = 0xE4, B = 0xE6 },
            // Blue Gray
            new() { A = 0xFF, R = 0x09,  G = 0x0E, B = 0x1A },
            new() { A = 0xFF, R = 0x0F,  G = 0x17, B = 0x2A },
            new() { A = 0xFF, R = 0x1E,  G = 0x29, B = 0x3B },
            new() { A = 0xFF, R = 0x33,  G = 0x41, B = 0x55 },
            new() { A = 0xFF, R = 0x47,  G = 0x55, B = 0x69 },
            new() { A = 0xFF, R = 0x64,  G = 0x74, B = 0x8B },
            new() { A = 0xFF, R = 0x94,  G = 0xA3, B = 0xB8 },
            new() { A = 0xFF, R = 0xCB,  G = 0xD5, B = 0xE1 },
            new() { A = 0xFF, R = 0xE2,  G = 0xE8, B = 0xF0 },
            new() { A = 0xFF, R = 0xF1,  G = 0xF5, B = 0xF9 },
            // Gray
            new() { A = 0xFF, R = 0x00,  G = 0x00, B = 0x00 },
            new() { A = 0xFF, R = 0x17,  G = 0x17, B = 0x17 },
            new() { A = 0xFF, R = 0x26,  G = 0x26, B = 0x26 },
            new() { A = 0xFF, R = 0x40,  G = 0x40, B = 0x40 },
            new() { A = 0xFF, R = 0x52,  G = 0x52, B = 0x52 },
            new() { A = 0xFF, R = 0x73,  G = 0x73, B = 0x73 },
            new() { A = 0xFF, R = 0xA3,  G = 0xA3, B = 0xA3 },
            new() { A = 0xFF, R = 0xD4,  G = 0xD4, B = 0xD4 },
            new() { A = 0xFF, R = 0xE5,  G = 0xE5, B = 0xE5 },
            new() { A = 0xFF, R = 0xFF,  G = 0xFF, B = 0xFF }
        };
    }

    public class ColorCollection : List<Color>
    {
        public ColorCollection() : base() { }
        public ColorCollection(IEnumerable<Color> colors) : base(colors) { }
    }
}
