using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ChipGauge.App.Controls;

/// <summary>
/// A lightweight, allocation-frugal area chart that renders a sensor's rolling
/// value series — the scrolling performance-graph look from Task Manager. Bind
/// <see cref="Values"/> to a <see cref="ViewModels.SensorViewModel.Values"/> array
/// and it redraws whenever the series changes.
/// </summary>
public sealed class LiveChart : Control
{
    /// <summary>The value series to plot, oldest first.</summary>
    public static readonly StyledProperty<IReadOnlyList<float>?> ValuesProperty =
        AvaloniaProperty.Register<LiveChart, IReadOnlyList<float>?>(nameof(Values));

    /// <summary>Line/stroke color.</summary>
    public static readonly StyledProperty<IBrush> StrokeProperty =
        AvaloniaProperty.Register<LiveChart, IBrush>(nameof(Stroke), Brushes.DeepSkyBlue);

    /// <summary>When true the vertical axis is fixed to 0–100; otherwise it auto-scales.</summary>
    public static readonly StyledProperty<bool> IsPercentageProperty =
        AvaloniaProperty.Register<LiveChart, bool>(nameof(IsPercentage));

    static LiveChart() =>
        AffectsRender<LiveChart>(ValuesProperty, StrokeProperty, IsPercentageProperty, BoundsProperty);

    /// <inheritdoc cref="ValuesProperty" />
    public IReadOnlyList<float>? Values
    {
        get => GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    /// <inheritdoc cref="StrokeProperty" />
    public IBrush Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    /// <inheritdoc cref="IsPercentageProperty" />
    public bool IsPercentage
    {
        get => GetValue(IsPercentageProperty);
        set => SetValue(IsPercentageProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        Rect bounds = new(Bounds.Size);
        if (bounds.Width <= 1 || bounds.Height <= 1)
        {
            return;
        }

        DrawGrid(context, bounds);

        IReadOnlyList<float>? values = Values;
        if (values is null || values.Count < 2)
        {
            return;
        }

        (double min, double max) = ResolveRange(values);
        double range = max - min;
        if (range <= double.Epsilon)
        {
            range = 1;
        }

        int count = values.Count;
        double stepX = bounds.Width / (count - 1);

        Point PointAt(int i)
        {
            double x = i * stepX;
            double norm = (values[i] - min) / range;
            double y = bounds.Height - (norm * bounds.Height);
            return new Point(x, Math.Clamp(y, 0, bounds.Height));
        }

        // Filled area under the line.
        var area = new StreamGeometry();
        using (StreamGeometryContext ctx = area.Open())
        {
            ctx.BeginFigure(new Point(0, bounds.Height), isFilled: true);
            for (var i = 0; i < count; i++)
            {
                ctx.LineTo(PointAt(i));
            }

            ctx.LineTo(new Point(bounds.Width, bounds.Height));
            ctx.EndFigure(isClosed: true);
        }

        Color accent = (Stroke as ISolidColorBrush)?.Color ?? Colors.DeepSkyBlue;
        var fill = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(150, accent.R, accent.G, accent.B), 0),
                new GradientStop(Color.FromArgb(10, accent.R, accent.G, accent.B), 1),
            },
        };
        context.DrawGeometry(fill, null, area);

        // The line itself.
        var line = new StreamGeometry();
        using (StreamGeometryContext ctx = line.Open())
        {
            ctx.BeginFigure(PointAt(0), isFilled: false);
            for (var i = 1; i < count; i++)
            {
                ctx.LineTo(PointAt(i));
            }

            ctx.EndFigure(isClosed: false);
        }

        var pen = new Pen(Stroke, 1.6) { LineJoin = PenLineJoin.Round, LineCap = PenLineCap.Round };
        context.DrawGeometry(null, pen, line);
    }

    private (double Min, double Max) ResolveRange(IReadOnlyList<float> values)
    {
        if (IsPercentage)
        {
            return (0, 100);
        }

        double min = double.MaxValue;
        double max = double.MinValue;
        foreach (float v in values)
        {
            min = Math.Min(min, v);
            max = Math.Max(max, v);
        }

        // Pad by 8% so the line never hugs the edges; keep zero as a floor.
        double pad = Math.Max((max - min) * 0.08, 0.5);
        return (Math.Min(min - pad, min), max + pad);
    }

    private static void DrawGrid(DrawingContext context, Rect bounds)
    {
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(28, 128, 128, 128)), 1);
        for (var i = 1; i < 4; i++)
        {
            double y = bounds.Height * i / 4;
            context.DrawLine(gridPen, new Point(0, y), new Point(bounds.Width, y));
        }
    }
}
