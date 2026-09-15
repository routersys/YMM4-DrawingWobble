namespace DrawingWobble.Harness;

internal static class HarnessCases
{
    public static IEnumerable<(string Name, DrawingWobbleEffect Effect, IReadOnlyList<int> Frames)> All()
    {
        yield return ("default", Create(), [0]);
        yield return ("default-frame-4", Create(), [4]);
        yield return ("default-frames-0-8", Create(), Enumerable.Range(0, 9).ToArray());
        yield return ("amplitude-0", Create(effect => effect.Amplitude.Values[0].Value = 0), [0]);
        yield return ("amplitude-20", Create(effect => effect.Amplitude.Values[0].Value = 20), [0]);
        yield return ("scale-8", Create(effect => effect.Scale.Values[0].Value = 8), [0]);
        yield return ("scale-400", Create(effect => effect.Scale.Values[0].Value = 400), [0]);
        yield return ("hold-1-frame-2", Create(effect => effect.HoldFrames = 1), [2]);
        yield return ("hold-1-frames-0-3", Create(effect => effect.HoldFrames = 1), [0, 1, 2, 3]);
        yield return ("seed-42", Create(effect => effect.Seed = 42), [0]);
        yield return ("edge-radius-64", Create(effect => effect.EdgeRadius.Values[0].Value = 64), [0]);
        yield return ("edge-focus-0", Create(effect => effect.EdgeFocus.Values[0].Value = 0), [0]);
        yield return ("edge-focus-50", Create(effect => effect.EdgeFocus.Values[0].Value = 50), [0]);
    }

    public static IEnumerable<(string Name, Func<DrawingWobbleEffect> Create, Action<DrawingWobbleEffect> Change, int Frame)> Transitions()
    {
        yield return ("amplitude-2-to-20", () => Create(), effect => effect.Amplitude.Values[0].Value = 20, 0);
        yield return ("amplitude-2-to-0", () => Create(), effect => effect.Amplitude.Values[0].Value = 0, 0);
        yield return ("amplitude-0-to-2", () => Create(effect => effect.Amplitude.Values[0].Value = 0), effect => effect.Amplitude.Values[0].Value = 2, 0);
        yield return ("scale-60-to-8", () => Create(), effect => effect.Scale.Values[0].Value = 8, 0);
        yield return ("hold-3-to-1-frame-4", () => Create(), effect => effect.HoldFrames = 1, 4);
        yield return ("seed-0-to-42", () => Create(), effect => effect.Seed = 42, 0);
        yield return ("edge-radius-12-to-64", () => Create(), effect => effect.EdgeRadius.Values[0].Value = 64, 0);
        yield return ("edge-focus-100-to-0", () => Create(), effect => effect.EdgeFocus.Values[0].Value = 0, 0);
        yield return ("edge-focus-0-to-100", () => Create(effect => effect.EdgeFocus.Values[0].Value = 0), effect => effect.EdgeFocus.Values[0].Value = 100, 0);
    }

    public static IEnumerable<(string Name, DrawingWobbleEffect Effect)> Benchmarks()
    {
        yield return ("default", Create());
        yield return ("edge-focus-0", Create(effect => effect.EdgeFocus.Values[0].Value = 0));
        yield return ("edge-radius-64", Create(effect => effect.EdgeRadius.Values[0].Value = 64));
        yield return ("amplitude-20", Create(effect => effect.Amplitude.Values[0].Value = 20));
    }

    static DrawingWobbleEffect Create(Action<DrawingWobbleEffect>? configure = null)
    {
        var effect = new DrawingWobbleEffect();
        configure?.Invoke(effect);
        return effect;
    }
}
