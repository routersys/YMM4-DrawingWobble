using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Json;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Plugin.Effects;
using YukkuriMovieMaker.Project;

namespace DrawingWobble.Tests;

public sealed class DrawingWobbleEffectTests
{
    static PropertyInfo Property(string name) => typeof(DrawingWobbleEffect).GetProperty(name)!;

    static T Attribute<T>(string property) where T : Attribute => Property(property).GetCustomAttribute<T>()!;

    [Theory]
    [InlineData(nameof(DrawingWobbleEffect.Amplitude), 2d, 0d, 4096d)]
    [InlineData(nameof(DrawingWobbleEffect.Scale), 60d, 1d, 4096d)]
    [InlineData(nameof(DrawingWobbleEffect.EdgeRadius), 12d, 0d, 4096d)]
    [InlineData(nameof(DrawingWobbleEffect.EdgeFocus), 100d, 0d, 100d)]
    public void AnimatedParametersStartFromTheirDefaultsWithinTheirRange(string name, double defaultValue, double minimum, double maximum)
    {
        var effect = new DrawingWobbleEffect();

        var animation = (Animation)Property(name).GetValue(effect)!;

        Assert.Equal(defaultValue, animation.DefaultValue);
        Assert.Equal(minimum, animation.MinValue);
        Assert.Equal(maximum, animation.MaxValue);
        Assert.Equal(defaultValue, animation.GetValue(0, 1, EffectDescriptions.Fps));
    }

    [Fact]
    public void HoldFramesAndSeedStartFromTheirDefaults()
    {
        var effect = new DrawingWobbleEffect();

        Assert.Equal(3, effect.HoldFrames);
        Assert.Equal(0, effect.Seed);
    }

    [Theory]
    [InlineData(int.MinValue, 1)]
    [InlineData(-1, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(12, 12)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void HoldFramesNeverDropBelowOne(int value, int expected)
    {
        var effect = new DrawingWobbleEffect { HoldFrames = 5 };

        effect.HoldFrames = value;

        Assert.Equal(expected, effect.HoldFrames);
        Assert.False(effect.HasErrors);
    }

    [Theory]
    [InlineData(int.MinValue, 0)]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(10000, 10000)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void SeedNeverDropsBelowZero(int value, int expected)
    {
        var effect = new DrawingWobbleEffect { Seed = 5 };

        effect.Seed = value;

        Assert.Equal(expected, effect.Seed);
        Assert.False(effect.HasErrors);
    }

    [Fact]
    public void ChangingHoldFramesOrSeedNotifiesTheEditor()
    {
        var effect = new DrawingWobbleEffect();
        var changed = new List<string?>();
        effect.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        effect.HoldFrames = 2;
        effect.Seed = 7;

        Assert.Equal([nameof(DrawingWobbleEffect.HoldFrames), nameof(DrawingWobbleEffect.Seed)], changed);
    }

    [Fact]
    public void AssigningAnUnchangedOrClampedValueDoesNotNotify()
    {
        var effect = new DrawingWobbleEffect { HoldFrames = 1 };
        var changed = new List<string?>();
        effect.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        effect.HoldFrames = 1;
        effect.HoldFrames = -5;
        effect.Seed = 0;
        effect.Seed = -1;

        Assert.Empty(changed);
    }

    [Fact]
    public void TheLabelIsTheLocalizedEffectName()
    {
        var effect = new DrawingWobbleEffect();

        Assert.Equal(Texts.DrawingWobble, effect.Label);
    }

    [Fact]
    public void TheFourNumericParametersReceiveTheAnimationParameters()
    {
        var effect = new DrawingWobbleEffect();

        effect.SetAnimationParameters(120, EffectDescriptions.Fps);

        Assert.All([effect.Amplitude, effect.Scale, effect.EdgeRadius, effect.EdgeFocus], animation => Assert.Equal(120, animation.Length));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void NoExoFilterIsWrittenForAviUtl(int keyFrameIndex)
    {
        var effect = new DrawingWobbleEffect();

        var description = new ExoOutputDescription(new VideoInfo(), string.Empty, new AviUtlDirectories(string.Empty, string.Empty));

        Assert.Empty(effect.CreateExoVideoFilters(keyFrameIndex, description));
    }

    [Fact]
    public void TheEffectIsRegisteredForAnimationAndFilteringWithoutAviUtlSupport()
    {
        var attribute = typeof(DrawingWobbleEffect).GetCustomAttribute<VideoEffectAttribute>()!;

        Assert.Equal(nameof(Texts.DrawingWobble), attribute.Name);
        Assert.Equal([VideoEffectCategories.Animation, VideoEffectCategories.Filtering], attribute.Categories);
        Assert.Equal([nameof(Texts.TagHandDrawn), nameof(Texts.TagWobble), nameof(Texts.TagBoil), nameof(Texts.TagSketch)], attribute.Keywords);
        Assert.False(attribute.IsAviUtlSupported);
        Assert.True(attribute.IsEffectItemSupported);
        Assert.Equal(typeof(Texts), attribute.ResourceType);
        Assert.Equal(Texts.DrawingWobble, attribute.GetName());
    }

    [Fact]
    public void TheAuthorIsDeclared()
    {
        var details = typeof(DrawingWobbleEffect).GetCustomAttribute<PluginDetailsAttribute>()!;

        Assert.Equal("routersys", details.AuthorName);
    }

    [Theory]
    [InlineData(nameof(DrawingWobbleEffect.Amplitude), nameof(Texts.WobbleGroup), nameof(Texts.Amplitude), nameof(Texts.AmplitudeDescription), 0)]
    [InlineData(nameof(DrawingWobbleEffect.Scale), nameof(Texts.WobbleGroup), nameof(Texts.Scale), nameof(Texts.ScaleDescription), 1)]
    [InlineData(nameof(DrawingWobbleEffect.HoldFrames), nameof(Texts.WobbleGroup), nameof(Texts.HoldFrames), nameof(Texts.HoldFramesDescription), 2)]
    [InlineData(nameof(DrawingWobbleEffect.Seed), nameof(Texts.WobbleGroup), nameof(Texts.Seed), nameof(Texts.SeedDescription), 3)]
    [InlineData(nameof(DrawingWobbleEffect.EdgeRadius), nameof(Texts.EdgeGroup), nameof(Texts.EdgeRadius), nameof(Texts.EdgeRadiusDescription), 10)]
    [InlineData(nameof(DrawingWobbleEffect.EdgeFocus), nameof(Texts.EdgeGroup), nameof(Texts.EdgeFocus), nameof(Texts.EdgeFocusDescription), 11)]
    public void EveryParameterIsDisplayedInItsGroupInOrder(string property, string group, string name, string description, int order)
    {
        var display = Attribute<DisplayAttribute>(property);

        Assert.Equal(group, display.GroupName);
        Assert.Equal(name, display.Name);
        Assert.Equal(description, display.Description);
        Assert.Equal(order, display.Order);
        Assert.Equal(typeof(Texts), display.ResourceType);
    }

    [Theory]
    [InlineData(nameof(DrawingWobbleEffect.Amplitude), "F1", "px", 0d, 20d)]
    [InlineData(nameof(DrawingWobbleEffect.Scale), "F1", "px", 8d, 400d)]
    [InlineData(nameof(DrawingWobbleEffect.EdgeRadius), "F1", "px", 0d, 64d)]
    [InlineData(nameof(DrawingWobbleEffect.EdgeFocus), "F1", "%", 0d, 100d)]
    public void AnimatedParametersAreEditedWithAnimationSliders(string property, string format, string unit, double minimum, double maximum)
    {
        var slider = Attribute<AnimationSliderAttribute>(property);

        Assert.Equal(format, slider.StringFormat);
        Assert.Equal(unit, slider.UnitText);
        Assert.Equal(minimum, slider.DefaultMin);
        Assert.Equal(maximum, slider.DefaultMax);
    }

    [Fact]
    public void HoldFramesAreEditedInFramesFromOneToTwelve()
    {
        var slider = Attribute<TextBoxSliderAttribute>(nameof(DrawingWobbleEffect.HoldFrames));
        var range = Attribute<RangeAttribute>(nameof(DrawingWobbleEffect.HoldFrames));

        Assert.Equal("F0", slider.StringFormat);
        Assert.Equal(nameof(Texts.FrameUnit), slider.UnitText);
        Assert.Equal(typeof(Texts), slider.ResourceType);
        Assert.Equal(1d, slider.DefaultMin);
        Assert.Equal(12d, slider.DefaultMax);
        Assert.Equal(1, range.Minimum);
        Assert.Equal(int.MaxValue, range.Maximum);
        Assert.Equal(3, Attribute<DefaultValueAttribute>(nameof(DrawingWobbleEffect.HoldFrames)).Value);
    }

    [Fact]
    public void TheSeedIsEditedWithoutAUnitFromZero()
    {
        var slider = Attribute<TextBoxSliderAttribute>(nameof(DrawingWobbleEffect.Seed));
        var range = Attribute<RangeAttribute>(nameof(DrawingWobbleEffect.Seed));

        Assert.Equal("F0", slider.StringFormat);
        Assert.Equal(string.Empty, slider.UnitText);
        Assert.Equal(0d, slider.DefaultMin);
        Assert.Equal(10000d, slider.DefaultMax);
        Assert.Equal(0, range.Minimum);
        Assert.Equal(int.MaxValue, range.Maximum);
        Assert.Equal(0, Attribute<DefaultValueAttribute>(nameof(DrawingWobbleEffect.Seed)).Value);
    }

    [Fact]
    public void EverySettingSurvivesAProjectRoundTrip()
    {
        var effect = new DrawingWobbleEffect { HoldFrames = 5, Seed = 42 };
        effect.Amplitude.Values[0].Value = 7.5d;
        effect.Scale.Values[0].Value = 120d;
        effect.EdgeRadius.Values[0].Value = 3d;
        effect.EdgeFocus.Values[0].Value = 40d;

        var clone = Json.GetClone(effect)!;

        Assert.NotSame(effect, clone);
        Assert.Equal(5, clone.HoldFrames);
        Assert.Equal(42, clone.Seed);
        Assert.Equal(7.5d, clone.Amplitude.GetValue(0, 1, EffectDescriptions.Fps));
        Assert.Equal(120d, clone.Scale.GetValue(0, 1, EffectDescriptions.Fps));
        Assert.Equal(3d, clone.EdgeRadius.GetValue(0, 1, EffectDescriptions.Fps));
        Assert.Equal(40d, clone.EdgeFocus.GetValue(0, 1, EffectDescriptions.Fps));
    }
}
