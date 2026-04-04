using System;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

using WinRT;

namespace AssyntSoftware.WinUI3Controls
{
    public partial class GroupBox : ContentControl
    {
        private ContentPresenter? headingPresenter;
        private ContentPresenter? childPresenter;
        private Path? borderPath;

        public GroupBox()
        {
            this.DefaultStyleKey = typeof(GroupBox);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            try
            {
                headingPresenter = GetTemplateChild("PART_HeadingPresenter").As<ContentPresenter>();
                childPresenter = GetTemplateChild("PART_ChildPresenter").As<ContentPresenter>();
                borderPath = GetTemplateChild("PART_BorderPath").As<Path>();

                // offset the heading presenter from the control edge
                headingPresenter.Margin = new Thickness(HeadingMargin, 0, 0, 0);

                // reuse Control properties to define the group border
                RegisterPropertyChangedCallback(CornerRadiusProperty, (s, d) => ((GroupBox)s).BorderPropertyChanged());
                RegisterPropertyChangedCallback(BorderThicknessProperty, (s, d) => ((GroupBox)s).BorderPropertyChanged());

                headingPresenter.SizeChanged += (s, e) => BorderPropertyChanged();

                SizeChanged += (s, e) => ((GroupBox)s).RedrawBorder();

                Loaded += (s, e) => ((GroupBox)s).RedrawBorder();

                // initialise
                BorderPropertyChanged();
            }
            catch (InvalidCastException)
            {
            }
        }

        private void RedrawBorder()
        {
            if (IsLoaded)
                CreateBorderRoundedRect();
        }

        private void BorderPropertyChanged()
        {
            if (childPresenter is null || borderPath is null)
                return;

            Thickness newPadding = CalculateContentPresenterPadding();

            if (childPresenter.Padding != newPadding)
                childPresenter.Padding = newPadding;

            // a non uniform border thickness isn't supported
            if (borderPath.StrokeThickness != BorderThickness.Left)
                borderPath.StrokeThickness = BorderThickness.Left;

            // it's difficult to tell if changing the child presenter padding would
            // cause a size changed event, so always redraw the border here
            RedrawBorder();
        }


        private Thickness CalculateContentPresenterPadding()
        {
            static double Max(double a, double b, double c) => Math.Max(Math.Max(a, b), c);

            double halfStrokeThickness = BorderThickness.Left / 2;
            double headingHeight = (headingPresenter is null) ? 0.0 : headingPresenter.ActualHeight;
            double headingBaseLineRatio = Math.Clamp(HeadingBaseLineRatio, 0.0, 1.0);

            // if "borderOffset" is positive, the top border line extends below the top of the content presenter
            double borderOffset = -(headingHeight - ((headingHeight * headingBaseLineRatio) + halfStrokeThickness));
            double cornerAdjustment = Math.Max(CornerRadius.TopLeft, CornerRadius.TopRight) - BorderThickness.Left;
            double topPadding = cornerAdjustment + borderOffset;

            if (topPadding < borderOffset)  // top padding cannot be less that the bottom of the border 
                topPadding = borderOffset;

            if (topPadding < 0) // the content cannot be outside of the content presenter (even if that's a valid operation)
                topPadding = 0;

            // a non uniform corner radius is unlikely, but possible
            // a non uniform border thickness isn't supported
            return new Thickness(Max(CornerRadius.TopLeft, CornerRadius.BottomLeft, BorderThickness.Left),
                                topPadding,
                                Max(CornerRadius.TopRight, CornerRadius.BottomRight, BorderThickness.Left),
                                Max(CornerRadius.BottomLeft, CornerRadius.BottomRight, BorderThickness.Left));
        }


        // string.Empty is used to ensure that the Heading content presenter's content can't be null when set or omitted in xaml.
        // Otherwise an argument exception would be thrown by WinUi's layout code, which can't be caught.
        // While it's likely to be a WinUi bug in WAS1.8, this work around is for convenience.

        public static readonly DependencyProperty HeadingProperty =
            DependencyProperty.Register(nameof(Heading),
                typeof(object),
                typeof(GroupBox),
                new PropertyMetadata(string.Empty));

        public object Heading
        {
            get { return GetValue(HeadingProperty); }
            set { SetValue(HeadingProperty, value ?? string.Empty); }
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
                new PropertyMetadata(0.61, (d, e) => ((GroupBox)d).BorderPropertyChanged()));

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
                new PropertyMetadata(16.0, HeadingMarginPropertyChanged));

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

            if (gb.headingPresenter is not null)
            {
                gb.headingPresenter.Margin = new Thickness((double)e.NewValue, 0, 0, 0);
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
            if (headingPresenter is null || borderPath is null)
                return;

            static LineSegment LineTo(float x, float y) => new LineSegment() { Point = new Point(x, y), };
            static ArcSegment ArcTo(Point end, float radius) => new ArcSegment() { Point = end, RotationAngle = 90.0, IsLargeArc = false, Size = new Size(radius, radius), SweepDirection = SweepDirection.Clockwise };

            PathFigure figure = new PathFigure() { IsClosed = false, IsFilled = false, };

            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(figure);

            float textLHS = (float)(HeadingMargin - BorderEndPadding);
            float textRHS = (float)(HeadingMargin + headingPresenter.ActualWidth + BorderStartPadding);

            float halfStrokeThickness = (float)(borderPath.StrokeThickness * 0.5);
            float headingCenter = (float)(headingPresenter.ActualHeight * Math.Clamp(HeadingBaseLineRatio, 0.0, 1.0));

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
            borderPath.Data = pathGeometry;
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new GroupBoxAutomationPeer(this);
        }
    }

    public partial class GroupBoxAutomationPeer : FrameworkElementAutomationPeer
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
