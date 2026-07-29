using Zilean.ApiService.Features.Bootstrapping;

namespace Zilean.Tests.Tests;

public class ConfigurationValidationTests
{
    private static ZileanConfiguration CreateValidConfiguration() =>
        new()
        {
            Database =
            {
                ConnectionString = "Host=localhost;Database=zilean;Username=postgres;Password=postgres;"
            }
        };

    [Fact]
    public void validate_returns_no_errors_for_default_configuration()
    {
        var config = CreateValidConfiguration();

        var errors = config.Validate();

        errors.Should().BeEmpty(
            "because a default configuration with a valid connection string must be valid");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void validate_returns_error_when_max_filtered_results_is_zero_or_negative(int value)
    {
        var config = CreateValidConfiguration();
        config.Dmm.MaxFilteredResults = value;

        var errors = config.Validate();

        errors.Should().ContainSingle(
            e => e.Contains("Dmm.MaxFilteredResults must be greater than 0"),
            $"because MaxFilteredResults={value} must be rejected");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void validate_returns_error_when_minimum_score_match_out_of_range(double value)
    {
        var config = CreateValidConfiguration();
        config.Dmm.MinimumScoreMatch = value;

        var errors = config.Validate();

        errors.Should().ContainSingle(
            e => e.Contains("Dmm.MinimumScoreMatch must be between 0 and 1"),
            $"because MinimumScoreMatch={value} is outside the inclusive [0, 1] range");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void validate_returns_no_error_when_minimum_score_match_at_boundaries(double value)
    {
        var config = CreateValidConfiguration();
        config.Dmm.MinimumScoreMatch = value;

        var errors = config.Validate();

        errors.Should().NotContain(
            e => e.Contains("Dmm.MinimumScoreMatch must be between 0 and 1"),
            $"because MinimumScoreMatch={value} is at the inclusive boundary and must be valid");
    }

    [Fact]
    public void validate_returns_error_when_minimum_redownload_interval_is_negative()
    {
        var config = CreateValidConfiguration();
        config.Dmm.MinimumReDownloadIntervalMinutes = -1;

        var errors = config.Validate();

        errors.Should().ContainSingle(
            e => e.Contains("Dmm.MinimumReDownloadIntervalMinutes must be non-negative"),
            "because a negative re-download interval must be rejected");
    }

    [Theory]
    [InlineData("")]
    [InlineData("0 0 * *")]
    [InlineData("0 0 * * 0 6")]
    public void validate_returns_error_for_invalid_cron_schedule(string cron)
    {
        var config = CreateValidConfiguration();
        config.Dmm.ScrapeSchedule = cron;

        var errors = config.Validate();

        errors.Should().Contain(
            e => e.Contains("is not a valid cron expression"),
            $"because cron='{cron}' is not a valid 5-field cron expression");
    }

    [Fact]
    public void validate_returns_no_error_for_valid_5_field_cron_schedule()
    {
        var config = CreateValidConfiguration();
        config.Dmm.ScrapeSchedule = "0 * * * *";

        var errors = config.Validate();

        errors.Should().NotContain(
            e => e.Contains("Dmm.ScrapeSchedule") && e.Contains("is not a valid cron expression"),
            "because '0 * * * *' is a valid 5-field cron expression");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void validate_returns_error_when_batch_size_is_zero_or_negative(int value)
    {
        var config = CreateValidConfiguration();
        config.Parsing.BatchSize = value;

        var errors = config.Validate();

        errors.Should().ContainSingle(
            e => e.Contains("Parsing.BatchSize must be greater than 0"),
            $"because BatchSize={value} must be rejected");
    }

    [Fact]
    public void validate_returns_error_when_connection_string_is_empty()
    {
        var config = CreateValidConfiguration();
        config.Database.ConnectionString = "";

        var errors = config.Validate();

        errors.Should().Contain(
            e => e.Contains("Database.ConnectionString is empty"),
            "because an empty connection string must be rejected");
    }

    [Fact]
    public void validate_returns_multiple_errors_for_all_invalid_config()
    {
        var config = new ZileanConfiguration
        {
            Dmm =
            {
                MaxFilteredResults = 0,
                MinimumScoreMatch = -1,
                MinimumReDownloadIntervalMinutes = -5,
                ScrapeSchedule = "bad"
            },
            Ingestion =
            {
                ScrapeSchedule = "also bad"
            },
            Parsing =
            {
                BatchSize = 0
            },
            Database =
            {
                ConnectionString = ""
            }
        };

        var errors = config.Validate();

        // 8 rules broken: MaxFilteredResults, MinimumScoreMatch, MinimumReDownloadIntervalMinutes,
        // Dmm.ScrapeSchedule, Ingestion.ScrapeSchedule, BatchSize, ConnectionString — 7 distinct rules,
        // but the two invalid cron schedules each produce their own error, totaling 7 errors.
        errors.Should().HaveCount(7,
            "because 7 validation rules are violated: MaxFilteredResults, MinimumScoreMatch, " +
            "MinimumReDownloadIntervalMinutes, Dmm.ScrapeSchedule, Ingestion.ScrapeSchedule, BatchSize, ConnectionString");
        errors.Should().Contain(e => e.Contains("Dmm.MaxFilteredResults must be greater than 0"));
        errors.Should().Contain(e => e.Contains("Dmm.MinimumScoreMatch must be between 0 and 1"));
        errors.Should().Contain(e => e.Contains("Dmm.MinimumReDownloadIntervalMinutes must be non-negative"));
        errors.Should().Contain(e => e.Contains("Dmm.ScrapeSchedule") && e.Contains("is not a valid cron expression"));
        errors.Should().Contain(e => e.Contains("Ingestion.ScrapeSchedule") && e.Contains("is not a valid cron expression"));
        errors.Should().Contain(e => e.Contains("Parsing.BatchSize must be greater than 0"));
        errors.Should().Contain(e => e.Contains("Database.ConnectionString is empty"));
    }

    [Fact]
    public async Task startup_service_throws_when_configuration_is_invalid()
    {
        var config = new ZileanConfiguration
        {
            Dmm =
            {
                MaxFilteredResults = 0
            },
            Database =
            {
                ConnectionString = ""
            }
        };

        var serviceProvider = Substitute.For<IServiceProvider>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        var startupService = new StartupService(config, serviceProvider, loggerFactory);

        var act = () => startupService.StartingAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Zilean configuration is invalid*",
                "because StartupService must fail fast on invalid configuration before touching the database");
    }
}