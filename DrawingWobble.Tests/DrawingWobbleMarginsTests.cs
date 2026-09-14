using Vortice;
using static DrawingWobble.DrawingWobbleCustomEffect.EffectImpl;

namespace DrawingWobble.Tests;

public sealed class DrawingWobbleMarginsTests
{
    const int MaxInputPixel = 4096;

    [Theory]
    [InlineData(-3f, 0)]
    [InlineData(0f, 0)]
    [InlineData(0.01f, 3)]
    [InlineData(1f, 3)]
    [InlineData(2f, 4)]
    [InlineData(2.5f, 5)]
    [InlineData(MaxInputPixel - 2f, MaxInputPixel)]
    [InlineData(MaxInputPixel, MaxInputPixel)]
    [InlineData(float.MaxValue, MaxInputPixel)]
    public void TheOutputMarginRoundsTheAmplitudeUpAndAddsTwoPixels(float amplitude, int expected)
        => Assert.Equal(expected, Margins.Output(amplitude));

    [Theory]
    [InlineData(3f, 12f, 1f, 14)]
    [InlineData(12f, 3f, 1f, 14)]
    [InlineData(3f, 12f, 0.01f, 14)]
    [InlineData(3f, 12f, 0f, 5)]
    [InlineData(3f, 0f, 1f, 5)]
    [InlineData(0f, 12f, 1f, 0)]
    [InlineData(3f, -1f, 1f, 5)]
    [InlineData(3f, 12f, -1f, 5)]
    public void TheInputMarginCoversTheRingOnlyWhileItIsSampled(float amplitude, float edgeRadius, float edgeFocus, int expected)
        => Assert.Equal(expected, Margins.Input(amplitude, edgeRadius, edgeFocus));

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public void InflatingByNothingReturnsTheSameRectangle(int margin)
    {
        var rect = new RawRect(1, 2, 3, 4);

        Assert.Equal(rect, Margins.Inflate(rect, margin));
    }

    [Fact]
    public void InflatingGrowsEverySide()
    {
        Assert.Equal(new RawRect(-2, 7, 33, 44), Margins.Inflate(new RawRect(1, 10, 30, 41), 3));
    }

    [Fact]
    public void InflatingSaturatesAtTheIntegerLimits()
    {
        var inflated = Margins.Inflate(new RawRect(int.MinValue + 1, int.MinValue, int.MaxValue - 1, int.MaxValue), 5);

        Assert.Equal(new RawRect(int.MinValue, int.MinValue, int.MaxValue, int.MaxValue), inflated);
    }
}
