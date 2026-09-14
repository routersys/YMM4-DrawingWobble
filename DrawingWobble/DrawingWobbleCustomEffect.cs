using System.Numerics;
using System.Runtime.InteropServices;
using Vortice;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace DrawingWobble
{
    internal sealed class DrawingWobbleCustomEffect(IGraphicsDevicesAndContext devices) : D2D1CustomShaderEffectBase(Create<EffectImpl>(devices))
    {
        private enum PropertyIndex
        {
            Amplitude = 0,
            Scale,
            EdgeRadius,
            EdgeFocus,
            HoldIndex,
            Seed,
        }

        public float Amplitude { set => SetValue((int)PropertyIndex.Amplitude, value); }
        public float Scale { set => SetValue((int)PropertyIndex.Scale, value); }
        public float EdgeRadius { set => SetValue((int)PropertyIndex.EdgeRadius, value); }
        public float EdgeFocus { set => SetValue((int)PropertyIndex.EdgeFocus, value); }
        public int HoldIndex { set => SetValue((int)PropertyIndex.HoldIndex, value); }
        public int Seed { set => SetValue((int)PropertyIndex.Seed, value); }

        [CustomEffect(1)]
        private sealed class EffectImpl : D2D1CustomShaderEffectImplBase<EffectImpl>
        {
            private ConstantBuffer _cb;

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.Amplitude)]
            public float Amplitude { get => _cb.Amplitude; set { _cb.Amplitude = Math.Clamp(value, 0f, MaxInputPixel); UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.Scale)]
            public float Scale { get => _cb.Scale; set { _cb.Scale = Math.Max(value, 1f); UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.EdgeRadius)]
            public float EdgeRadius { get => _cb.EdgeRadius; set { _cb.EdgeRadius = Math.Clamp(value, 0f, MaxInputPixel); UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.EdgeFocus)]
            public float EdgeFocus { get => _cb.EdgeFocus; set { _cb.EdgeFocus = Math.Clamp(value, 0f, 1f); UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Int32, (int)PropertyIndex.HoldIndex)]
            public int HoldIndex { get => _cb.HoldIndex; set { _cb.HoldIndex = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Int32, (int)PropertyIndex.Seed)]
            public int Seed { get => _cb.Seed; set { _cb.Seed = value; UpdateConstants(); } }

            public EffectImpl() : base(ShaderResourceUri.Get("DrawingWobble"))
            {
                _cb.Scale = 1f;
            }

            protected override void UpdateConstants()
            {
                if (drawInformation is null)
                    return;

                try
                {
                    drawInformation.SetPixelShaderConstantBuffer(_cb);
                }
                catch (Exception exception)
                {
                    DrawingWobbleTelemetry.Report(exception);
                    throw;
                }
            }

            public override void MapInputRectsToOutputRect(
                RawRect[] inputRects,
                RawRect[] inputOpaqueSubRects,
                out RawRect outputRect,
                out RawRect outputOpaqueSubRect)
            {
                inputRect = ClampInputRect(inputRects[0]);
                if (inputRect.Right <= inputRect.Left || inputRect.Bottom <= inputRect.Top)
                {
                    outputRect = inputRect;
                    outputOpaqueSubRect = default;
                    return;
                }

                _cb.InputBounds = new Vector4(inputRect.Left, inputRect.Top, inputRect.Right, inputRect.Bottom);
                UpdateConstants();

                outputRect = Margins.Inflate(inputRect, Margins.Output(_cb.Amplitude));
                outputOpaqueSubRect = default;
            }

            public override void MapOutputRectToInputRects(RawRect outputRect, RawRect[] inputRects)
            {
                if (inputRects.Length == 0)
                    return;

                if (inputRect.Right <= inputRect.Left || inputRect.Bottom <= inputRect.Top)
                {
                    inputRects[0] = inputRect;
                    return;
                }

                inputRects[0] = Margins.Inflate(outputRect, Margins.Input(_cb.Amplitude, _cb.EdgeRadius, _cb.EdgeFocus));
            }

            public override RawRect MapInvalidRect(int inputIndex, RawRect invalidInputRect)
                => Margins.Inflate(invalidInputRect, Margins.Input(_cb.Amplitude, _cb.EdgeRadius, _cb.EdgeFocus));

            internal static class Margins
            {
                public static int Output(float amplitude) => Of(amplitude);

                public static int Input(float amplitude, float edgeRadius, float edgeFocus)
                    => Of(amplitude > 0f && edgeFocus > 0f && edgeRadius > 0f ? Math.Max(amplitude, edgeRadius) : amplitude);

                public static RawRect Inflate(RawRect rect, int margin)
                {
                    if (margin <= 0)
                        return rect;

                    return new RawRect(
                        Saturate((long)rect.Left - margin),
                        Saturate((long)rect.Top - margin),
                        Saturate((long)rect.Right + margin),
                        Saturate((long)rect.Bottom + margin));
                }

                private static int Of(float distance) => distance <= 0f ? 0 : (int)Math.Min(Math.Ceiling(distance) + 2.0, MaxInputPixel);

                private static int Saturate(long value) => (int)Math.Clamp(value, int.MinValue, int.MaxValue);
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct ConstantBuffer
            {
                public Vector4 InputBounds;
                public float Amplitude;
                public float Scale;
                public float EdgeRadius;
                public float EdgeFocus;
                public int HoldIndex;
                public int Seed;
                public float Pad0;
                public float Pad1;
            }
        }
    }
}
