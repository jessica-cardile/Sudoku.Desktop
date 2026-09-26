using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace Sudoku.UI
{
    // A Canvas that fills itself with randomly placed stars, each twinkling on its own
    // compositor-driven opacity (and, for sparkles, scale) animation.
    public sealed class TwinklingStarField : Canvas
    {
        private const int DotCount = 70;
        private const int SparkleCount = 22;
        private const double EdgeInset = 10;

        private static readonly Color[] StarColors =
        {
            Color.FromArgb(255, 255, 255, 255),
            Color.FromArgb(255, 255, 244, 200),
            Color.FromArgb(255, 215, 200, 255),
            Color.FromArgb(255, 200, 220, 255),
        };

        private readonly List<(FrameworkElement Star, double RelX, double RelY)> _stars = new();

        public TwinklingStarField()
        {
            IsHitTestVisible = false;
            SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width <= 0 || e.NewSize.Height <= 0)
            {
                return;
            }

            if (_stars.Count == 0)
            {
                CreateStars();
            }

            PositionStars(e.NewSize);
        }

        private void CreateStars()
        {
            // Fixed seed keeps the sky looking the same every time the card is opened.
            var rng = new Random(42);

            for (int i = 0; i < DotCount + SparkleCount; i++)
            {
                bool isSparkle = i >= DotCount;
                var brush = new SolidColorBrush(StarColors[rng.Next(StarColors.Length)]);

                FrameworkElement star = isSparkle
                    ? CreateSparkle(7 + rng.NextDouble() * 6, brush)
                    : CreateDot(1.5 + rng.NextDouble() * 2, brush);

                Children.Add(star);
                _stars.Add((star, rng.NextDouble(), rng.NextDouble()));
                StartTwinkle(star, rng, scaleToo: isSparkle);
            }
        }

        private static Ellipse CreateDot(double diameter, Brush fill) =>
            new() { Width = diameter, Height = diameter, Fill = fill, IsHitTestVisible = false };

        // Four-pointed star: 8 vertices alternating between the outer and inner radius.
        private static Polygon CreateSparkle(double size, Brush fill)
        {
            double center = size / 2;
            double innerRadius = center * 0.22;
            var points = new PointCollection();

            for (int i = 0; i < 8; i++)
            {
                double radius = i % 2 == 0 ? center : innerRadius;
                double angle = (-90 + i * 45) * Math.PI / 180;
                points.Add(new Point(center + radius * Math.Cos(angle), center + radius * Math.Sin(angle)));
            }

            return new Polygon { Width = size, Height = size, Points = points, Fill = fill, IsHitTestVisible = false };
        }

        private void PositionStars(Size size)
        {
            double usableWidth = Math.Max(0, size.Width - 2 * EdgeInset);
            double usableHeight = Math.Max(0, size.Height - 2 * EdgeInset);

            foreach (var (star, relX, relY) in _stars)
            {
                SetLeft(star, EdgeInset + relX * usableWidth - star.Width / 2);
                SetTop(star, EdgeInset + relY * usableHeight - star.Height / 2);
            }
        }

        private static void StartTwinkle(FrameworkElement star, Random rng, bool scaleToo)
        {
            Visual visual = ElementCompositionPreview.GetElementVisual(star);
            Compositor compositor = visual.Compositor;

            float minOpacity = (float)(0.1 + rng.NextDouble() * 0.2);
            float maxOpacity = (float)(0.7 + rng.NextDouble() * 0.3);
            TimeSpan duration = TimeSpan.FromMilliseconds(1200 + rng.Next(0, 2800));
            TimeSpan delay = TimeSpan.FromMilliseconds(rng.Next(0, 3000));

            // Held at its dim value while the animation waits out its random start delay.
            star.Opacity = minOpacity;

            CubicBezierEasingFunction easing =
                compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0f), new Vector2(0.6f, 1f));

            ScalarKeyFrameAnimation opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f, minOpacity);
            opacity.InsertKeyFrame(1f, maxOpacity, easing);
            ConfigureLooping(opacity, duration, delay);
            visual.StartAnimation("Opacity", opacity);

            if (scaleToo)
            {
                visual.CenterPoint = new Vector3((float)(star.Width / 2), (float)(star.Height / 2), 0);

                Vector3KeyFrameAnimation scale = compositor.CreateVector3KeyFrameAnimation();
                scale.InsertKeyFrame(0f, new Vector3(0.55f, 0.55f, 1f));
                scale.InsertKeyFrame(1f, Vector3.One, easing);
                ConfigureLooping(scale, duration, delay);
                visual.StartAnimation("Scale", scale);
            }
        }

        private static void ConfigureLooping(KeyFrameAnimation animation, TimeSpan duration, TimeSpan delay)
        {
            animation.Duration = duration;
            animation.Direction = AnimationDirection.Alternate;
            animation.IterationBehavior = AnimationIterationBehavior.Forever;
            animation.DelayTime = delay;
        }
    }
}
