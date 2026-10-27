using NainLegacyModernizer.ParityCli;
using Xunit;

namespace NainLegacyModernizer.Tests;

public sealed class ParityCliOptionsTests
{
    private static readonly string[] Required = ["--legacy", "http://legacy.local", "--modern", "http://modern.local", "--paths", "/a,/b"];

    [Fact]
    public void TryParse_RequiredArguments_UsesDefaults()
    {
        var ok = CliOptions.TryParse(Required, out var options, out var error);

        Assert.True(ok, error);
        Assert.NotNull(options);
        Assert.Equal(new Uri("http://legacy.local"), options.Legacy);
        Assert.Equal(new Uri("http://modern.local"), options.Modern);
        Assert.Equal(["/a", "/b"], options.Paths);
        Assert.Equal("parity-report.html", options.OutputPath);
        Assert.Null(options.JsonPath);
        Assert.Empty(options.Ignore);
        Assert.Empty(options.Map);
    }

    [Fact]
    public void TryParse_OptionalArguments_AreRead()
    {
        string[] args = [.. Required, "--ignore", "source,generatedAt", "--map", "OrderID=id", "--map", "CustomerRef=customerReference", "--out", "r.html", "--json", "r.json"];

        Assert.True(CliOptions.TryParse(args, out var options, out var error), error);

        Assert.Equal(["generatedAt", "source"], options!.Ignore.Order(StringComparer.Ordinal));
        Assert.Equal("id", options.Map["OrderID"]);
        Assert.Equal("customerReference", options.Map["CustomerRef"]);
        Assert.Equal("r.html", options.OutputPath);
        Assert.Equal("r.json", options.JsonPath);
    }

    [Fact]
    public void TryParse_PathsFile_ReadsOnePathPerLineSkippingBlanksAndComments()
    {
        var file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, "/a\n\n# comment\n  /b  \n");
            string[] args = ["--legacy", "http://l", "--modern", "http://m", "--paths", file];

            Assert.True(CliOptions.TryParse(args, out var options, out var error), error);

            Assert.Equal(["/a", "/b"], options!.Paths);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Theory]
    [InlineData("--legacy")]
    [InlineData("--modern")]
    [InlineData("--paths")]
    public void TryParse_MissingRequiredArgument_Fails(string missing)
    {
        var args = new List<string>(Required);
        var index = args.IndexOf(missing);
        args.RemoveRange(index, 2);

        Assert.False(CliOptions.TryParse([.. args], out _, out var error));
        Assert.Contains(missing, error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--legacy", "not a url")]
    [InlineData("--legacy", "ftp://legacy.local")]
    [InlineData("--map", "no-equals-sign")]
    public void TryParse_InvalidValue_Fails(string option, string value)
    {
        string[] args = [.. Required, option, value];
        var withoutDuplicate = option == "--legacy" ? ["--modern", "http://m", "--paths", "/a", option, value] : args;

        Assert.False(CliOptions.TryParse(withoutDuplicate, out _, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void TryParse_UnknownOption_Fails()
    {
        Assert.False(CliOptions.TryParse([.. Required, "--bogus", "x"], out _, out var error));
        Assert.Contains("--bogus", error, StringComparison.Ordinal);
    }

    [Fact]
    public void TryParse_OptionWithoutValue_Fails()
    {
        Assert.False(CliOptions.TryParse([.. Required, "--out"], out _, out var error));
        Assert.Contains("--out", error, StringComparison.Ordinal);
    }

    [Fact]
    public void TryParse_PathsMustStartWithASlash()
    {
        Assert.False(CliOptions.TryParse(["--legacy", "http://l", "--modern", "http://m", "--paths", "api/orders"], out _, out var error));
        Assert.Contains("/", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    public void ExitCode_FollowsReadiness(bool ready, int expected)
    {
        Assert.Equal(expected, CliOptions.ExitCodeFor(ready));
    }
}
