using System.Windows;
using Telemetry;

namespace DrawingWobble.Tests;

public sealed class HostIntegrationTests
{
    [Fact]
    public void OutsideAWpfApplicationNoTelemetryIsStartedOrSent()
    {
        Assert.Null(Application.Current);

        DrawingWobbleTelemetry.EnsureStartedOnce();
        DrawingWobbleTelemetry.Report(new InvalidOperationException());

        Assert.Null(ProcessState.Read("DrainClaimed"));
        Assert.Null(ProcessState.Read("SentCount"));
    }

    [Fact]
    public void OutsideAWpfApplicationTheEffectCanStillBeCreated()
    {
        Assert.Null(Application.Current);

        var effect = new DrawingWobbleEffect();

        Assert.Equal(Texts.DrawingWobble, effect.Label);
        Assert.Null(ProcessState.Read("DrainClaimed"));
    }
}
