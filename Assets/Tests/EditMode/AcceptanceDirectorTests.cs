using NUnit.Framework;
using System;

public class AcceptanceDirectorTests
{
    [Test]
    public void ShouldRun_RequiresAcceptanceFlag()
    {
        Assert.IsFalse(AcceptanceDirector.ShouldRun(new[] { "HappyMatchGame" }));
        Assert.IsTrue(AcceptanceDirector.ShouldRun(new[] { "HappyMatchGame", "-happyMatchAcceptance" }));
    }

    [Test]
    public void OutputDirectory_UsesValueAfterFlag()
    {
        string result = AcceptanceDirector.OutputDirectory(new[]
        {
            "HappyMatchGame", "-happyMatchAcceptanceDir", "/tmp/hm-acceptance"
        });
        Assert.AreEqual("/tmp/hm-acceptance", result);
    }

    [Test]
    public void ShouldRun_AcceptsEnvironmentFlag()
    {
        string previous = Environment.GetEnvironmentVariable("HAPPY_MATCH_ACCEPTANCE");
        try
        {
            Environment.SetEnvironmentVariable("HAPPY_MATCH_ACCEPTANCE", "1");
            Assert.IsTrue(AcceptanceDirector.ShouldRun(new[] { "HappyMatchGame" }));
        }
        finally
        {
            Environment.SetEnvironmentVariable("HAPPY_MATCH_ACCEPTANCE", previous);
        }
    }

    [Test]
    public void OutputDirectory_AcceptsEnvironmentValue()
    {
        string previous = Environment.GetEnvironmentVariable("HAPPY_MATCH_ACCEPTANCE_DIR");
        try
        {
            Environment.SetEnvironmentVariable("HAPPY_MATCH_ACCEPTANCE_DIR", "/tmp/hm-env-acceptance");
            Assert.AreEqual("/tmp/hm-env-acceptance", AcceptanceDirector.OutputDirectory(new[] { "HappyMatchGame" }));
        }
        finally
        {
            Environment.SetEnvironmentVariable("HAPPY_MATCH_ACCEPTANCE_DIR", previous);
        }
    }
}
