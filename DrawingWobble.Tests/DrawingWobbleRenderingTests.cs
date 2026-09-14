using System.Numerics;
using Vortice.Direct2D1;
using Vortice.Direct2D1.Effects;
using YukkuriMovieMaker.Commons;

namespace DrawingWobble.Tests;

[Collection("Direct2D")]
public sealed class DrawingWobbleRenderingTests
{
    const int Width = 64;
    const int Height = 32;

    static readonly Bgra Red = Bgra.Opaque(0, 0, 255);
    static readonly Bgra Blue = Bgra.Opaque(255, 0, 0);

    static Bgra TwoHalves(int x, int y) => x < Width / 2 ? Red : Blue;

    static Bgra Gradient(int x, int y) => Bgra.Opaque((byte)(x * 4), (byte)(x * 4), (byte)(x * 4));

    static Bgra OpaqueLeftHalf(int x, int y) => x < Width / 2 ? Bgra.Opaque(0, 0, 0) : Bgra.Transparent;

    static Rendering Render(IGraphicsDevicesAndContext devices, SourceImage source, Action<DrawingWobbleCustomEffect> configure)
    {
        using var effect = new DrawingWobbleCustomEffect(devices);
        effect.SetInput(0, source.Bitmap, true);
        configure(effect);
        using var output = effect.Output;
        return Rendering.Capture(devices, output);
    }

    static void AssertSameAsSource(Rendering rendering, SourceImage source)
    {
        Assert.Equal((0, 0, source.Width, source.Height), (rendering.Left, rendering.Top, rendering.Width, rendering.Height));
        Assert.All(rendering.Coordinates(), point => Assert.Equal(source[point.X, point.Y], rendering[point.X, point.Y]));
    }

    static double DistanceToBorder(SourceImage source, int x, int y)
        => Math.Min(Math.Min(x, source.Width - 1 - x), Math.Min(y, source.Height - 1 - y));

    static bool WithinRounding(Bgra expected, Bgra actual)
        => Math.Abs(expected.Blue - actual.Blue) <= 1 && Math.Abs(expected.Green - actual.Green) <= 1 && Math.Abs(expected.Red - actual.Red) <= 1 && Math.Abs(expected.Alpha - actual.Alpha) <= 1;

    static long Difference(Rendering rendering, SourceImage source)
    {
        var total = 0L;
        foreach (var (x, y) in rendering.Coordinates())
        {
            var expected = source.Contains(x, y) ? source[x, y] : Bgra.Transparent;
            var actual = rendering[x, y];
            total += Math.Abs(expected.Blue - actual.Blue) + Math.Abs(expected.Green - actual.Green) + Math.Abs(expected.Red - actual.Red) + Math.Abs(expected.Alpha - actual.Alpha);
        }

        return total;
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(1f, 0f)]
    [InlineData(1f, 8f)]
    public void WithoutAmplitudeTheImagePassesThroughUntouched(float edgeFocus, float edgeRadius)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, Gradient);

        var rendering = Render(context, source, effect =>
        {
            effect.EdgeFocus = edgeFocus;
            effect.EdgeRadius = edgeRadius;
            effect.HoldIndex = 3;
            effect.Seed = 9;
        });

        AssertSameAsSource(rendering, source);
    }

    [Fact]
    public void TheSameSettingsAlwaysProduceTheSamePixels()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, TwoHalves);
        void Configure(DrawingWobbleCustomEffect effect)
        {
            effect.Amplitude = 3f;
            effect.Scale = 12f;
            effect.HoldIndex = 2;
            effect.Seed = 5;
        }

        var first = Render(context, source, Configure);
        var second = Render(context, source, Configure);

        Assert.True(first.SamePixelsAs(second));
    }

    [Fact]
    public void ADifferentHoldIndexMovesTheImageDifferently()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, TwoHalves);

        var held = Render(context, source, effect => { effect.Amplitude = 3f; effect.HoldIndex = 0; });
        var next = Render(context, source, effect => { effect.Amplitude = 3f; effect.HoldIndex = 1; });

        Assert.False(held.SamePixelsAs(next));
    }

    [Fact]
    public void ADifferentSeedMovesTheImageDifferently()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, TwoHalves);

        var first = Render(context, source, effect => { effect.Amplitude = 3f; effect.Seed = 0; });
        var second = Render(context, source, effect => { effect.Amplitude = 3f; effect.Seed = 1; });

        Assert.False(first.SamePixelsAs(second));
    }

    [Fact]
    public void ADifferentScaleMovesTheImageDifferently()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, TwoHalves);

        var fine = Render(context, source, effect => { effect.Amplitude = 3f; effect.Scale = 6f; });
        var coarse = Render(context, source, effect => { effect.Amplitude = 3f; effect.Scale = 60f; });

        Assert.False(fine.SamePixelsAs(coarse));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(2.5f)]
    [InlineData(6f)]
    public void NoPixelIsPulledFromFartherThanTheAmplitude(float amplitude)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, TwoHalves);
        var reach = amplitude + 1d;

        var rendering = Render(context, source, effect => effect.Amplitude = amplitude);

        foreach (var (x, y) in rendering.Coordinates())
        {
            var distanceToSeam = Math.Abs(x + 0.5d - Width / 2d);
            if (source.Contains(x, y) && distanceToSeam > reach && DistanceToBorder(source, x, y) > reach)
                Assert.Equal(source[x, y], rendering[x, y]);
        }
    }

    [Fact]
    public void TheSeamActuallyWobbles()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, TwoHalves);

        var rendering = Render(context, source, effect => effect.Amplitude = 3f);

        Assert.Contains(rendering.Coordinates(), point => source.Contains(point.X, point.Y) && rendering[point.X, point.Y] != source[point.X, point.Y]);
    }

    [Theory]
    [InlineData(2f)]
    [InlineData(5f)]
    public void PixelsBeyondTheReachOfTheAmplitudeStayTransparent(float amplitude)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = SourceImage.Solid(context, Width, Height, Blue);
        var reach = amplitude + 1d;

        var rendering = Render(context, source, effect => effect.Amplitude = amplitude);

        Assert.True(rendering.Left < 0 && rendering.Top < 0);
        foreach (var (x, y) in rendering.Coordinates())
        {
            if (-DistanceToBorder(source, x, y) > reach)
                Assert.Equal(Bgra.Transparent, rendering[x, y]);
        }
    }

    [Fact]
    public void TheImageSpillsOverItsOriginalEdges()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = SourceImage.Solid(context, Width, Height, Blue);

        var rendering = Render(context, source, effect => effect.Amplitude = 3f);

        Assert.Contains(rendering.Coordinates(), point => !source.Contains(point.X, point.Y) && rendering[point.X, point.Y].Alpha > 0);
    }

    [Fact]
    public void TheInteriorOfAFlatImageStaysStillWhenTheWobbleFollowsTheEdges()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = SourceImage.Solid(context, Width, Height, Blue);
        const float edgeRadius = 6f;

        var rendering = Render(context, source, effect =>
        {
            effect.Amplitude = 4f;
            effect.EdgeRadius = edgeRadius;
            effect.EdgeFocus = 1f;
        });

        foreach (var (x, y) in rendering.Coordinates())
        {
            if (source.Contains(x, y) && DistanceToBorder(source, x, y) > edgeRadius + 1d)
                Assert.Equal(Blue.Premultiplied(), rendering[x, y]);
        }
    }

    [Fact]
    public void TheEdgeFocusScalesDownTheWobbleOfFlatAreas()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, Gradient);
        Action<DrawingWobbleCustomEffect> With(float focus) => effect =>
        {
            effect.Amplitude = 4f;
            effect.EdgeRadius = 2f;
            effect.EdgeFocus = focus;
        };

        var none = Difference(Render(context, source, With(0f)), source);
        var half = Difference(Render(context, source, With(0.5f)), source);
        var full = Difference(Render(context, source, With(1f)), source);

        Assert.True(none > half, $"none={none} half={half}");
        Assert.True(half > full, $"half={half} full={full}");
    }

    [Fact]
    public void AZeroEdgeRadiusWobblesTheWholeImageEvenlyDespiteTheFocus()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, Gradient);

        var uniform = Render(context, source, effect => { effect.Amplitude = 4f; effect.EdgeFocus = 0f; });
        var focused = Render(context, source, effect => { effect.Amplitude = 4f; effect.EdgeFocus = 1f; effect.EdgeRadius = 0f; });

        Assert.True(uniform.SamePixelsAs(focused));
    }

    [Fact]
    public void AnAlphaEdgeCountsAsAContour()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, OpaqueLeftHalf);
        const float amplitude = 3f;
        const float edgeRadius = 4f;

        var rendering = Render(context, source, effect =>
        {
            effect.Amplitude = amplitude;
            effect.EdgeRadius = edgeRadius;
            effect.EdgeFocus = 1f;
        });

        var nearSeam = rendering.Coordinates().Where(point => Math.Abs(point.X + 0.5d - Width / 2d) <= edgeRadius && DistanceToBorder(source, point.X, point.Y) > edgeRadius + amplitude + 1d);
        Assert.Contains(nearSeam, point => rendering[point.X, point.Y] != source[point.X, point.Y]);
        foreach (var (x, y) in rendering.Coordinates())
        {
            if (source.Contains(x, y) && Math.Abs(x + 0.5d - Width / 2d) > edgeRadius + amplitude + 1d && DistanceToBorder(source, x, y) > edgeRadius + amplitude + 1d)
                Assert.Equal(source[x, y], rendering[x, y]);
        }
    }

    [Theory]
    [InlineData(100, 50)]
    [InlineData(-37, 21)]
    public void TheWobbleTravelsWithTheImage(int dx, int dy)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, TwoHalves);
        using var moved = new AffineTransform2D(context.DeviceContext)
        {
            InterPolationMode = AffineTransform2DInterpolationMode.NearestNeighbor,
            BorderMode = BorderMode.Hard,
            TransformMatrix = Matrix3x2.CreateTranslation(dx, dy),
        };
        moved.SetInput(0, source.Bitmap, true);
        using var movedOutput = moved.Output;
        void Configure(DrawingWobbleCustomEffect effect)
        {
            effect.Amplitude = 3f;
            effect.EdgeRadius = 4f;
            effect.EdgeFocus = 0.5f;
        }

        var inPlace = Render(context, source, Configure);
        using var effect = new DrawingWobbleCustomEffect(context);
        effect.SetInput(0, movedOutput, true);
        Configure(effect);
        using var output = effect.Output;
        var travelled = Rendering.Capture(context, output);

        Assert.Equal((inPlace.Left + dx, inPlace.Top + dy, inPlace.Width, inPlace.Height), (travelled.Left, travelled.Top, travelled.Width, travelled.Height));
        Assert.All(inPlace.Coordinates(), point => Assert.True(WithinRounding(inPlace[point.X, point.Y], travelled[point.X + dx, point.Y + dy]), $"({point.X}, {point.Y})"));
    }

    [Fact]
    public void SemiTransparentPixelsKeepTheirPremultipliedValues()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        var color = new Bgra(40, 90, 200, 128);
        using var source = SourceImage.Solid(context, Width, Height, color);
        const float amplitude = 3f;

        var rendering = Render(context, source, effect => effect.Amplitude = amplitude);

        foreach (var (x, y) in rendering.Coordinates())
        {
            if (source.Contains(x, y) && DistanceToBorder(source, x, y) > amplitude + 1d)
                Assert.Equal(color.Premultiplied(), rendering[x, y]);
        }
    }
}
