using System.Diagnostics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PracticalOtel.xUnit.OpenTelemetry;
using Xunit.Abstractions;

[assembly: TestFramework("todo_odd.Tests.OtelTestFramework", "tests")]

namespace todo_odd.Tests;

public class OtelTestFramework : TracedTestFramework
{
    public static readonly List<Activity> CollectedSpans = [];
    public OtelTestFramework(IMessageSink messageSink) : base(messageSink)
    {
        traceProviderSetup = tpb => {
            tpb
                .ConfigureResource(resource => resource.AddService("Unit-Tests"))
                .AddSource("UnitTests")
                .AddSource(ActivityHelper.Source.Name)
                .AddInMemoryExporter(CollectedSpans)
                .AddAspNetCoreInstrumentation()
                .AddOtlpExporter();
        };
    }
}