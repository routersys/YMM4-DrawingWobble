using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Player.Video.Effects;

namespace DrawingWobble
{
    internal sealed class DrawingWobbleEffectProcessor(
        IGraphicsDevicesAndContext devices,
        DrawingWobbleEffect item) : VideoEffectProcessorBase(devices)
    {
        private readonly DrawingWobbleEffect _item = item;
        private DrawingWobbleCustomEffect? _effect;

        private bool _isFirst = true;
        private Parameters _parameters;

        public override DrawDescription Update(EffectDescription effectDescription)
        {
            try
            {
                return UpdateCore(effectDescription);
            }
            catch (Exception exception)
            {
                DrawingWobbleTelemetry.Report(exception);
                throw;
            }
        }

        private DrawDescription UpdateCore(EffectDescription effectDescription)
        {
            if (IsPassThroughEffect || _effect is null)
                return effectDescription.DrawDescription;

            var frame = effectDescription.ItemPosition.Frame;
            var length = effectDescription.ItemDuration.Frame;
            var fps = effectDescription.FPS;
            var holdFrames = Math.Max(_item.HoldFrames, 1);

            var parameters = new Parameters(
                (float)_item.Amplitude.GetValue(frame, length, fps),
                (float)_item.Scale.GetValue(frame, length, fps),
                (float)_item.EdgeRadius.GetValue(frame, length, fps),
                (float)(_item.EdgeFocus.GetValue(frame, length, fps) / 100.0),
                (int)Math.Floor(frame / (double)holdFrames),
                _item.Seed);

            if (_isFirst || _parameters.Amplitude != parameters.Amplitude)
                _effect.Amplitude = parameters.Amplitude;
            if (_isFirst || _parameters.Scale != parameters.Scale)
                _effect.Scale = parameters.Scale;
            if (_isFirst || _parameters.EdgeRadius != parameters.EdgeRadius)
                _effect.EdgeRadius = parameters.EdgeRadius;
            if (_isFirst || _parameters.EdgeFocus != parameters.EdgeFocus)
                _effect.EdgeFocus = parameters.EdgeFocus;
            if (_isFirst || _parameters.HoldIndex != parameters.HoldIndex)
                _effect.HoldIndex = parameters.HoldIndex;
            if (_isFirst || _parameters.Seed != parameters.Seed)
                _effect.Seed = parameters.Seed;

            _parameters = parameters;
            _isFirst = false;

            return effectDescription.DrawDescription;
        }

        protected override ID2D1Image? CreateEffect(IGraphicsDevicesAndContext devices)
        {
            _effect = new DrawingWobbleCustomEffect(devices);
            if (!_effect.IsEnabled)
            {
                _effect.Dispose();
                _effect = null;
                return null;
            }
            disposer.Collect(_effect);

            var output = _effect.Output;
            disposer.Collect(output);
            return output;
        }

        protected override void setInput(ID2D1Image? input)
        {
            _effect?.SetInput(0, input, true);
        }

        protected override void ClearEffectChain()
        {
            try
            {
                ClearEffectChainCore();
            }
            catch (Exception exception)
            {
                DrawingWobbleTelemetry.Report(exception);
                throw;
            }
        }

        private void ClearEffectChainCore()
        {
            _effect?.SetInput(0, null, true);
            _isFirst = true;
        }

        private readonly record struct Parameters(
            float Amplitude,
            float Scale,
            float EdgeRadius,
            float EdgeFocus,
            int HoldIndex,
            int Seed);
    }
}
