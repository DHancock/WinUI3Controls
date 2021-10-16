using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

#nullable enable

namespace AssyntSoftware.WinUI3Controls
{
    [TemplatePart(Name = "PART_BorderPath", Type = typeof(Path))]
    [TemplatePart(Name = "PART_HeadingPresenter", Type = typeof(ContentPresenter))]
    [TemplatePart(Name = "PART_ChildPresenter", Type = typeof(ContentPresenter))]
    public sealed partial class GroupBox : ContentControl
    {
        private ContentPresenter? HeadingPresenter { get; set; }
        private ContentPresenter? ChildPresenter { get; set; }
        private Path? BorderPath { get; set; }


        public GroupBox()
        {
            this.DefaultStyleKey = typeof(GroupBox);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            HeadingPresenter = GetTemplateChild("PART_HeadingPresenter") as ContentPresenter;
            ChildPresenter = GetTemplateChild("PART_ChildPresenter") as ContentPresenter;
            BorderPath = GetTemplateChild("PART_BorderPath") as Path;

            if (HeadingPresenter is null || ChildPresenter is null || BorderPath is null)
                return;

            // offset the heading presenter from the control edge
            HeadingPresenter.Margin = new Thickness(HeadingMargin, 0, 0, 0);

            // the padding is dependent on the corner radius and border thickness
            ChildPresenter.Padding = CalculateContentPresenterPadding();

            // a non uniform border thickness isn't supported
            BorderPath.StrokeThickness = BorderThickness.Left;

            // reuse Control properties to define the group border
            RegisterPropertyChangedCallback(CornerRadiusProperty, BorderPropertyChanged);
            RegisterPropertyChangedCallback(BorderThicknessProperty, BorderPropertyChanged);

            Loaded += (s, e) =>
            {
                HeadingPresenter.SizeChanged += (s, e) => RedrawBorder();
                SizeChanged += (s, e) => RedrawBorder();

                RedrawBorder(); // first draw
            };
        }

        private void RedrawBorder()
        {
            if (IsLoaded)
                CreateBorderRoundedRect();
        }

        private void BorderPropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (ChildPresenter is null || BorderPath is null)
                return;

            Thickness newPadding = CalculateContentPresenterPadding();

            if (ChildPresenter.Padding != newPadding)
                ChildPresenter.Padding = newPadding;

            // a non uniform border thickness isn't supported
            BorderPath.StrokeThickness = BorderThickness.Left;

            // it's difficult to tell if changing the child presenter padding would
            // cause a size changed event, so always redraw the border here
            RedrawBorder();
        }

        private Thickness CalculateContentPresenterPadding()
        {
            static double Max(double a, double b, double c) => Math.Max(Math.Max(a, b), c);

            // a non uniform corner radius is unlikely, but possible
            // a non uniform border thickness isn't supported
            return new Thickness(Max(CornerRadius.TopLeft, CornerRadius.BottomLeft, BorderThickness.Left),
                                    Max(CornerRadius.TopLeft, CornerRadius.TopRight, BorderThickness.Left),
                                    Max(CornerRadius.TopRight, CornerRadius.BottomRight, BorderThickness.Left),
                                    Max(CornerRadius.BottomLeft, CornerRadius.BottomRight, BorderThickness.Left));
        }


        public static readonly DependencyProperty HeadingProperty =
            DependencyProperty.Register(nameof(Heading),
            typeof(object),
            typeof(GroupBox),
            new PropertyMetadata(null));

        public object Heading
        {
            get { return GetValue(HeadingProperty); }
            set { SetValue(HeadingProperty, value); }
        }


        public static readonly DependencyProperty HeadingTemplateProperty =
            DependencyProperty.Register(nameof(HeadingTemplate),
                typeof(DataTemplate),
                typeof(GroupBox),
                new PropertyMetadata(null));

        public object HeadingTemplate
        {
            get { return (DataTemplate)GetValue(HeadingTemplateProperty); }
            set { SetValue(HeadingTemplateProperty, value); }
        }


        public static readonly DependencyProperty HeadingTemplateSelectorProperty =
            DependencyProperty.Register(nameof(HeadingTemplateSelector),
                typeof(DataTemplateSelector),
                typeof(GroupBox),
                new PropertyMetadata(null));

        public object HeadingTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(HeadingTemplateSelectorProperty); }
            set { SetValue(HeadingTemplateSelectorProperty, value); }
        }

        public static readonly DependencyProperty HeadingBaseLineRatioProperty =
            DependencyProperty.Register(nameof(HeadingBaseLineRatio),
                typeof(double),
                typeof(GroupBox),
                new PropertyMetadata(0.61, (d, e) => ((GroupBox)d).RedrawBorder()));

        /// <summary>
        /// How far down the heading the border line is drawn.
        /// If 0.0, it'll be at the top of the content. 
        /// If 1.0, it would be drawn at the bottom.
        /// </summary>
        public double HeadingBaseLineRatio
        {
            get { return (double)GetValue(HeadingBaseLineRatioProperty); }
            set { SetValue(HeadingBaseLineRatioProperty, value); }
        }

        public static readonly DependencyProperty HeadingMarginProperty =
            DependencyProperty.Register(nameof(HeadingMargin),
                typeof(double),
                typeof(GroupBox),
                new PropertyMetadata(12.0, HeadingMarginPropertyChanged));

        /// <summary>
        /// The offset from this control's edge to the start of the heading presenter.
        /// </summary>
        public double HeadingMargin
        {
            get { return (double)GetValue(HeadingMarginProperty); }
            set { SetValue(HeadingMarginProperty, value); }
        }

        private static void HeadingMarginPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GroupBox gb = (GroupBox)d;

            if (gb.HeadingPresenter is not null)
            {
                gb.HeadingPresenter.Margin = new Thickness((double)e.NewValue, 0, 0, 0);
                gb.RedrawBorder();
            }
        }

        public static readonly DependencyProperty BorderEndPaddingProperty =
            DependencyProperty.Register(nameof(BorderEndPadding),
                typeof(double),
                typeof(GroupBox),
                new PropertyMetadata(3.0, (d, e) => ((GroupBox)d).RedrawBorder()));

        /// <summary>
        /// Padding between the end of the border and the start of the heading.
        /// This affects the border, changes won't cause a new measure pass.
        /// </summary>
        public double BorderEndPadding
        {
            get { return (double)GetValue(BorderEndPaddingProperty); }
            set { SetValue(BorderEndPaddingProperty, value); }
        }

        public static readonly DependencyProperty BorderStartPaddingProperty =
            DependencyProperty.Register(nameof(BorderStartPadding),
                typeof(double),
                typeof(GroupBox),
                new PropertyMetadata(4.0, (d, e) => ((GroupBox)d).RedrawBorder()));

        /// <summary>
        /// Padding between the start of the border and the end of the heading.
        /// This affects the border, changes won't cause a new measure pass.
        /// </summary>
        public double BorderStartPadding
        {
            get { return (double)GetValue(BorderStartPaddingProperty); }
            set { SetValue(BorderStartPaddingProperty, value); }
        }

        private void CreateBorderRoundedRect()
        {
            if (HeadingPresenter is null || BorderPath is null)
                return;

            static LineSegment LineTo(float x, float y) => new LineSegment() { Point = new Point(x, y), };
            static ArcSegment ArcTo(Point end, float radius) => new ArcSegment() { Point = end, RotationAngle = 90.0, IsLargeArc = false, Size = new Size(radius, radius), SweepDirection = SweepDirection.Clockwise };

            PathFigure figure = new PathFigure()
            {
                IsClosed = false,
                IsFilled = false,
            };

            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(figure);

            float textLHS = (float)(HeadingMargin - BorderEndPadding);
            float textRHS = (float)(HeadingMargin + HeadingPresenter.ActualWidth + BorderStartPadding);

            float halfStrokeThickness = (float)(BorderPath.StrokeThickness * 0.5);
            float headingCenter = (float)(HeadingPresenter.ActualHeight * Math.Clamp(HeadingBaseLineRatio, 0.0, 1.0));

            // right hand side of text
            float radius = (float)CornerRadius.TopRight;
            float xArcStart = ActualSize.X - (radius + halfStrokeThickness);

            if (textRHS < xArcStart) // check the first line is required, otherwise start at the arc
            {
                figure.StartPoint = new Point(textRHS, headingCenter);
                figure.Segments.Add(LineTo(xArcStart, headingCenter));
            }
            else
                figure.StartPoint = new Point(xArcStart, headingCenter);

            if (radius > 0) // top right corner
            {
                Point arcEnd = new Point(ActualSize.X - halfStrokeThickness, headingCenter + radius);
                figure.Segments.Add(ArcTo(arcEnd, radius));
            }

            radius = (float)CornerRadius.BottomRight;
            figure.Segments.Add(LineTo(ActualSize.X - halfStrokeThickness, ActualSize.Y - (radius + halfStrokeThickness)));

            if (radius > 0) // bottom right corner
            {
                Point arcEnd = new Point(ActualSize.X - (radius + halfStrokeThickness), ActualSize.Y - halfStrokeThickness);
                figure.Segments.Add(ArcTo(arcEnd, radius));
            }

            radius = (float)CornerRadius.BottomLeft;
            figure.Segments.Add(LineTo(radius + halfStrokeThickness, ActualSize.Y - halfStrokeThickness));

            if (radius > 0) // bottom left corner
            {
                Point arcEnd = new Point(halfStrokeThickness, ActualSize.Y - (radius + halfStrokeThickness));
                figure.Segments.Add(ArcTo(arcEnd, radius));
            }

            radius = (float)CornerRadius.TopLeft;
            figure.Segments.Add(LineTo(halfStrokeThickness, headingCenter + radius));

            if (radius > 0) // top left corner
            {
                Point arcEnd = new Point(radius + halfStrokeThickness, headingCenter);
                figure.Segments.Add(ArcTo(arcEnd, radius));
            }

            // check if the last line is required, the arc may be too large
            if (radius + halfStrokeThickness < textLHS)
                figure.Segments.Add(LineTo(textLHS, headingCenter));

            // add the new path geometry in to the visual tree
            BorderPath.Data = pathGeometry;
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new GroupBoxAutomationPeer(this);
        }
    }

    public class GroupBoxAutomationPeer : FrameworkElementAutomationPeer
    {
        public GroupBoxAutomationPeer(GroupBox control) : base(control)
        {
        }

        protected override string GetClassNameCore()
        {
            return "GroupBox";
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        protected override string GetNameCore()
        {
            if (((GroupBox)Owner).Heading is string str)
                return str;

            return base.GetNameCore();
        }
    }
}
