using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace DrawingWobble
{
    [VideoEffect(nameof(Texts.DrawingWobble), [VideoEffectCategories.Animation, VideoEffectCategories.Filtering], [nameof(Texts.TagHandDrawn), nameof(Texts.TagWobble), nameof(Texts.TagBoil), nameof(Texts.TagSketch)], IsAviUtlSupported = false, ResourceType = typeof(Texts))]
    public sealed class DrawingWobbleEffect : VideoEffectBase
    {
        public DrawingWobbleEffect()
        {
            DrawingWobbleTelemetry.EnsureStartedOnce();
        }

        public override string Label => Texts.DrawingWobble;

        [Display(GroupName = nameof(Texts.WobbleGroup), Name = nameof(Texts.Amplitude), Description = nameof(Texts.AmplitudeDescription), Order = 0, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "px", 0, 20)]
        public Animation Amplitude { get; } = new Animation(2, 0, 4096);

        [Display(GroupName = nameof(Texts.WobbleGroup), Name = nameof(Texts.Scale), Description = nameof(Texts.ScaleDescription), Order = 1, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "px", 8, 400)]
        public Animation Scale { get; } = new Animation(60, 1, 4096);

        [Display(GroupName = nameof(Texts.WobbleGroup), Name = nameof(Texts.HoldFrames), Description = nameof(Texts.HoldFramesDescription), Order = 2, ResourceType = typeof(Texts))]
        [Range(1, int.MaxValue)]
        [DefaultValue(3)]
        [TextBoxSlider("F0", nameof(Texts.FrameUnit), 1, 12, ResourceType = typeof(Texts))]
        public int HoldFrames
        {
            get => _holdFrames;
            set => Set(ref _holdFrames, Math.Max(value, 1));
        }
        private int _holdFrames = 3;

        [Display(GroupName = nameof(Texts.WobbleGroup), Name = nameof(Texts.Seed), Description = nameof(Texts.SeedDescription), Order = 3, ResourceType = typeof(Texts))]
        [Range(0, int.MaxValue)]
        [DefaultValue(0)]
        [TextBoxSlider("F0", "", 0, 10000)]
        public int Seed
        {
            get => _seed;
            set => Set(ref _seed, Math.Max(value, 0));
        }
        private int _seed;

        [Display(GroupName = nameof(Texts.EdgeGroup), Name = nameof(Texts.EdgeRadius), Description = nameof(Texts.EdgeRadiusDescription), Order = 10, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "px", 0, 64)]
        public Animation EdgeRadius { get; } = new Animation(12, 0, 4096);

        [Display(GroupName = nameof(Texts.EdgeGroup), Name = nameof(Texts.EdgeFocus), Description = nameof(Texts.EdgeFocusDescription), Order = 11, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation EdgeFocus { get; } = new Animation(100, 0, 100);

        private IAnimatable[]? _animatables;

        public override IEnumerable<string> CreateExoVideoFilters(
            int keyFrameIndex,
            ExoOutputDescription exoOutputDescription) => [];

        public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices)
        {
            try
            {
                return new DrawingWobbleEffectProcessor(devices, this);
            }
            catch (Exception exception)
            {
                DrawingWobbleTelemetry.Report(exception);
                throw;
            }
        }

        protected override IEnumerable<IAnimatable> GetAnimatables()
            => _animatables ??= [Amplitude, Scale, EdgeRadius, EdgeFocus];
    }
}
