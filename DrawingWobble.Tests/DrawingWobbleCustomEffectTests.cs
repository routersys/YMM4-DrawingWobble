using System.Numerics;
using Vortice.Direct2D1;
using Vortice.Direct2D1.Effects;
using YukkuriMovieMaker.Commons;
using static DrawingWobble.DrawingWobbleCustomEffect;

namespace DrawingWobble.Tests;

[Collection("Direct2D")]
public sealed class DrawingWobbleCustomEffectTests
{
    const float MaxInputPixel = 4096f;

    static float Read(DrawingWobbleCustomEffect effect, PropertyIndex index) => effect.GetFloatValue((int)index);

    [Fact]
    public void TheEffectIsEnabledOnceCreated()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var effect = new DrawingWobbleCustomEffect(context);

        Assert.True(effect.IsEnabled);
    }

    [Fact]
    public void ThePropertiesStartFromTheirDefaults()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var effect = new DrawingWobbleCustomEffect(context);

        Assert.Equal(0f, Read(effect, PropertyIndex.Amplitude));
        Assert.Equal(1f, Read(effect, PropertyIndex.Scale));
        Assert.Equal(0f, Read(effect, PropertyIndex.EdgeRadius));
        Assert.Equal(0f, Read(effect, PropertyIndex.EdgeFocus));
        Assert.Equal(0, effect.GetIntValue((int)PropertyIndex.HoldIndex));
        Assert.Equal(0, effect.GetIntValue((int)PropertyIndex.Seed));
    }

    [Theory]
    [InlineData(-1f, 0f)]
    [InlineData(0f, 0f)]
    [InlineData(2.5f, 2.5f)]
    [InlineData(MaxInputPixel, MaxInputPixel)]
    [InlineData(MaxInputPixel + 1f, MaxInputPixel)]
    [InlineData(float.MaxValue, MaxInputPixel)]
    public void AmplitudeIsClampedToTheInputPixelRange(float value, float expected)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var effect = new DrawingWobbleCustomEffect(context);

        effect.Amplitude = value;

        Assert.Equal(expected, Read(effect, PropertyIndex.Amplitude));
    }

    [Theory]
    [InlineData(-1f, 0f)]
    [InlineData(12f, 12f)]
    [InlineData(MaxInputPixel + 1f, MaxInputPixel)]
    public void EdgeRadiusIsClampedToTheInputPixelRange(float value, float expected)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var effect = new DrawingWobbleCustomEffect(context);

        effect.EdgeRadius = value;

        Assert.Equal(expected, Read(effect, PropertyIndex.EdgeRadius));
    }

    [Theory]
    [InlineData(-5f, 1f)]
    [InlineData(0f, 1f)]
    [InlineData(0.5f, 1f)]
    [InlineData(1f, 1f)]
    [InlineData(60f, 60f)]
    [InlineData(float.MaxValue, float.MaxValue)]
    public void ScaleNeverDropsBelowOnePixel(float value, float expected)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var effect = new DrawingWobbleCustomEffect(context);

        effect.Scale = value;

        Assert.Equal(expected, Read(effect, PropertyIndex.Scale));
    }

    [Theory]
    [InlineData(-0.5f, 0f)]
    [InlineData(0f, 0f)]
    [InlineData(0.25f, 0.25f)]
    [InlineData(1f, 1f)]
    [InlineData(1.5f, 1f)]
    public void EdgeFocusIsClampedToTheUnitRange(float value, float expected)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var effect = new DrawingWobbleCustomEffect(context);

        effect.EdgeFocus = value;

        Assert.Equal(expected, Read(effect, PropertyIndex.EdgeFocus));
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(int.MaxValue)]
    public void HoldIndexAndSeedAreStoredWithoutClamping(int value)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var effect = new DrawingWobbleCustomEffect(context);

        effect.HoldIndex = value;
        effect.Seed = value;

        Assert.Equal(value, effect.GetIntValue((int)PropertyIndex.HoldIndex));
        Assert.Equal(value, effect.GetIntValue((int)PropertyIndex.Seed));
    }

    [Fact]
    public void WithoutAmplitudeTheOutputBoundsEqualTheInput()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = SourceImage.Solid(context, 40, 24, Bgra.Opaque(10, 20, 30));
        using var effect = new DrawingWobbleCustomEffect(context);
        effect.SetInput(0, source.Bitmap, true);
        effect.EdgeRadius = 64f;
        effect.EdgeFocus = 1f;
        using var output = effect.Output;

        var bounds = context.DeviceContext.GetImageLocalBounds(output);

        Assert.Equal(0f, bounds.Left);
        Assert.Equal(0f, bounds.Top);
        Assert.Equal(40f, bounds.Right);
        Assert.Equal(24f, bounds.Bottom);
    }

    [Theory]
    [InlineData(0.5f, 3f)]
    [InlineData(1f, 3f)]
    [InlineData(2f, 4f)]
    [InlineData(2.01f, 5f)]
    [InlineData(20f, 22f)]
    public void TheOutputBoundsGrowByTheRoundedUpAmplitudePlusTwoPixels(float amplitude, float margin)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = SourceImage.Solid(context, 40, 24, Bgra.Opaque(10, 20, 30));
        using var effect = new DrawingWobbleCustomEffect(context);
        effect.SetInput(0, source.Bitmap, true);
        effect.Amplitude = amplitude;
        using var output = effect.Output;

        var bounds = context.DeviceContext.GetImageLocalBounds(output);

        Assert.Equal(-margin, bounds.Left);
        Assert.Equal(-margin, bounds.Top);
        Assert.Equal(40f + margin, bounds.Right);
        Assert.Equal(24f + margin, bounds.Bottom);
    }

    [Fact]
    public void TheOutputMarginIgnoresTheEdgeRadius()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = SourceImage.Solid(context, 40, 24, Bgra.Opaque(10, 20, 30));
        using var effect = new DrawingWobbleCustomEffect(context);
        effect.SetInput(0, source.Bitmap, true);
        effect.Amplitude = 2f;
        effect.EdgeRadius = 64f;
        effect.EdgeFocus = 1f;
        using var output = effect.Output;

        var bounds = context.DeviceContext.GetImageLocalBounds(output);

        Assert.Equal(-4f, bounds.Left);
        Assert.Equal(44f, bounds.Right);
    }

    [Theory]
    [InlineData(10f, 5f, 10f, 20f)]
    [InlineData(10f, 5f, 30f, 5f)]
    [InlineData(10f, 5f, 10f, 5f)]
    public void AnEmptyInputStaysEmpty(float left, float top, float right, float bottom)
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = SourceImage.Solid(context, 40, 24, Bgra.Opaque(10, 20, 30));
        using var crop = new Crop(context.DeviceContext) { Rectangle = new Vector4(left, top, right, bottom) };
        crop.SetInput(0, source.Bitmap, true);
        using var cropped = crop.Output;
        using var effect = new DrawingWobbleCustomEffect(context);
        effect.SetInput(0, cropped, true);
        effect.Amplitude = 3f;
        using var output = effect.Output;

        var bounds = context.DeviceContext.GetImageLocalBounds(output);

        Assert.True(bounds.Right <= bounds.Left || bounds.Bottom <= bounds.Top, $"{bounds}");
    }

    [Fact]
    public void TheOutputMarginIsCappedAtTheInputPixelLimit()
    {
        using var devices = new GraphicsDevices();
        using var context = devices.CreateContext();
        using var source = SourceImage.Solid(context, 8, 8, Bgra.Opaque(10, 20, 30));
        using var effect = new DrawingWobbleCustomEffect(context);
        effect.SetInput(0, source.Bitmap, true);
        effect.Amplitude = MaxInputPixel;
        using var output = effect.Output;

        var bounds = context.DeviceContext.GetImageLocalBounds(output);

        Assert.Equal(-MaxInputPixel, bounds.Left);
        Assert.Equal(8f + MaxInputPixel, bounds.Right);
    }
}
