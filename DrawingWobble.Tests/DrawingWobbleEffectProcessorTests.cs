using System.Globalization;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Json;
using YukkuriMovieMaker.Player.Video;

namespace DrawingWobble.Tests;

[Collection("Direct2D")]
public sealed class DrawingWobbleEffectProcessorTests
{
    const int Width = 64;
    const int Height = 32;
    const int Length = 30;

    static readonly Bgra Red = Bgra.Opaque(0, 0, 255);
    static readonly Bgra Blue = Bgra.Opaque(255, 0, 0);

    static Bgra TwoHalves(int x, int y) => x < Width / 2 ? Red : Blue;

    static Bgra Gradient(int x, int y) => Bgra.Opaque((byte)(x * 4), (byte)(x * 4), (byte)(x * 4));

    static DrawingWobbleEffect Uniform(double amplitude, int holdFrames = 3)
    {
        var effect = new DrawingWobbleEffect { HoldFrames = holdFrames };
        effect.Amplitude.Values[0].Value = amplitude;
        effect.EdgeFocus.Values[0].Value = 0d;
        return effect;
    }

    static Animation Linear(double from, double to)
        => Json.LoadFromText<Animation>(string.Create(CultureInfo.InvariantCulture, $$"""{"AnimationType":"直線移動","Values":[{"Value":{{from}}},{"Value":{{to}}}]}"""))!;

    static Rendering RenderFrame(IGraphicsDevicesAndContext devices, IVideoEffectProcessor processor, int frame)
    {
        processor.Update(EffectDescriptions.At(frame, Length));
        return Rendering.Capture(devices, processor.Output);
    }

    [Fact]
    public void TheProcessorHandsTheDrawDescriptionBackUnchanged()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, TwoHalves);
        using var processor = Uniform(3d).CreateVideoEffect(context);
        processor.SetInput(source.Bitmap);
        var description = EffectDescriptions.At(0, Length);

        var draw = processor.Update(description);

        Assert.Same(description.DrawDescription, draw);
    }

    [Fact]
    public void WithoutAmplitudeTheProcessorPassesTheImageThrough()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, TwoHalves);
        using var processor = Uniform(0d).CreateVideoEffect(context);
        processor.SetInput(source.Bitmap);

        var rendering = RenderFrame(context, processor, 0);

        Assert.Equal((0, 0, Width, Height), (rendering.Left, rendering.Top, rendering.Width, rendering.Height));
        Assert.All(rendering.Coordinates(), point => Assert.Equal(source[point.X, point.Y], rendering[point.X, point.Y]));
    }

    [Fact]
    public void TheWobbleHoldsForTheConfiguredFramesThenMoves()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, TwoHalves);
        using var processor = Uniform(3d, holdFrames: 3).CreateVideoEffect(context);
        processor.SetInput(source.Bitmap);

        var frames = Enumerable.Range(0, 7).Select(frame => RenderFrame(context, processor, frame)).ToArray();

        Assert.True(frames[0].SamePixelsAs(frames[1]));
        Assert.True(frames[1].SamePixelsAs(frames[2]));
        Assert.False(frames[2].SamePixelsAs(frames[3]));
        Assert.True(frames[3].SamePixelsAs(frames[4]));
        Assert.True(frames[4].SamePixelsAs(frames[5]));
        Assert.False(frames[5].SamePixelsAs(frames[6]));
    }

    [Fact]
    public void AHoldOfOneFrameMovesEveryFrame()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, TwoHalves);
        using var processor = Uniform(3d, holdFrames: 1).CreateVideoEffect(context);
        processor.SetInput(source.Bitmap);

        var first = RenderFrame(context, processor, 0);
        var second = RenderFrame(context, processor, 1);

        Assert.False(first.SamePixelsAs(second));
    }

    [Fact]
    public void ReturningToAFrameReproducesItExactly()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, TwoHalves);
        using var processor = Uniform(3d, holdFrames: 2).CreateVideoEffect(context);
        processor.SetInput(source.Bitmap);

        var first = RenderFrame(context, processor, 4);
        RenderFrame(context, processor, 9);
        var again = RenderFrame(context, processor, 5);

        Assert.True(first.SamePixelsAs(again));
    }

    [Fact]
    public void TheSeedChangesTheSequence()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, TwoHalves);
        var effect = Uniform(3d);
        using var processor = effect.CreateVideoEffect(context);
        processor.SetInput(source.Bitmap);

        var first = RenderFrame(context, processor, 0);
        effect.Seed = 1;
        var second = RenderFrame(context, processor, 0);

        Assert.False(first.SamePixelsAs(second));
    }

    [Fact]
    public void AnimatedAmplitudeIsReadAtEachFrame()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, TwoHalves);
        var effect = Uniform(0d);
        effect.Amplitude.CopyFrom(Linear(0d, 4d));
        using var processor = effect.CreateVideoEffect(context);
        processor.SetInput(source.Bitmap);

        var start = RenderFrame(context, processor, 0);
        var end = RenderFrame(context, processor, Length - 1);

        Assert.Equal((0, 0, Width, Height), (start.Left, start.Top, start.Width, start.Height));
        Assert.True(end.Left < 0 && end.Top < 0);
    }

    [Fact]
    public void TheProcessorFeedsTheEffectExactlyTheParameters()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, Gradient);
        var effect = new DrawingWobbleEffect { HoldFrames = 4, Seed = 11 };
        effect.Amplitude.Values[0].Value = 3.5d;
        effect.Scale.Values[0].Value = 17d;
        effect.EdgeRadius.Values[0].Value = 5d;
        effect.EdgeFocus.Values[0].Value = 35d;
        using var processor = effect.CreateVideoEffect(context);
        processor.SetInput(source.Bitmap);
        using var direct = new DrawingWobbleCustomEffect(context);
        direct.SetInput(0, source.Bitmap, true);
        direct.Amplitude = 3.5f;
        direct.Scale = 17f;
        direct.EdgeRadius = 5f;
        direct.EdgeFocus = 0.35f;
        direct.HoldIndex = 2;
        direct.Seed = 11;
        using var expected = direct.Output;

        var rendering = RenderFrame(context, processor, 9);

        Assert.True(rendering.SamePixelsAs(Rendering.Capture(context, expected)));
    }

    [Fact]
    public void ChangedSettingsReachTheNextFrame()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, TwoHalves);
        var effect = Uniform(3d);
        using var processor = effect.CreateVideoEffect(context);
        processor.SetInput(source.Bitmap);

        var wobbling = RenderFrame(context, processor, 0);
        effect.Amplitude.Values[0].Value = 0d;
        var still = RenderFrame(context, processor, 0);

        Assert.False(wobbling.SamePixelsAs(still));
        Assert.All(still.Coordinates(), point => Assert.Equal(source[point.X, point.Y], still[point.X, point.Y]));
    }

    public static readonly TheoryData<string, Action<DrawingWobbleEffect>> LaterChanges = new()
    {
        { nameof(DrawingWobbleEffect.Scale), effect => effect.Scale.Values[0].Value = 6d },
        { nameof(DrawingWobbleEffect.EdgeRadius), effect => effect.EdgeRadius.Values[0].Value = 8d },
        { nameof(DrawingWobbleEffect.EdgeFocus), effect => effect.EdgeFocus.Values[0].Value = 0d },
        { nameof(DrawingWobbleEffect.HoldFrames), effect => effect.HoldFrames = 1 },
    };

    [Theory]
    [MemberData(nameof(LaterChanges))]
    public void EverySettingChangedAfterTheFirstFrameReachesTheEffect(string setting, Action<DrawingWobbleEffect> change)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = new SourceImage(context, Width, Height, Gradient);
        var effect = new DrawingWobbleEffect { HoldFrames = 3 };
        effect.Amplitude.Values[0].Value = 4d;
        effect.EdgeRadius.Values[0].Value = 2d;
        effect.EdgeFocus.Values[0].Value = 100d;
        using var processor = effect.CreateVideoEffect(context);
        processor.SetInput(source.Bitmap);

        var before = RenderFrame(context, processor, 5);
        change(effect);
        var after = RenderFrame(context, processor, 5);

        Assert.False(before.SamePixelsAs(after), setting);
    }

    [Fact]
    public void AFailureWhileUpdatingIsNotSwallowed()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var processor = Uniform(3d).CreateVideoEffect(context);

        Assert.ThrowsAny<Exception>(() => processor.Update(null!));
    }

    [Fact]
    public void AFailureWhileCreatingTheProcessorIsNotSwallowed()
    {
        var effect = Uniform(3d);

        Assert.ThrowsAny<Exception>(() => effect.CreateVideoEffect(null!));
    }
}
