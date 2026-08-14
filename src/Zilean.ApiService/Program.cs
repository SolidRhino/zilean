var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddConfigurationFiles();

var zileanConfiguration = builder.Configuration.GetZileanConfiguration();

builder.AddOtlpServiceDefaults();

builder.Services
    .AddConfiguration(zileanConfiguration)
    .AddSwaggerSupport()
    .AddSchedulingSupport()
    .AddShellExecutionService()
    .RegisterSyncJobs(zileanConfiguration)
    .AddZileanDataServices(zileanConfiguration)
    .AddApiKeyAuthentication()
    .AddStartupHostedServices()
    .AddDashboardSupport(zileanConfiguration);

var app = builder.Build();

app.UseZileanRequired(zileanConfiguration);
app.MapZileanEndpoints(zileanConfiguration);
app.Services.SetupScheduling(zileanConfiguration);

app.Run();

// Make Program accessible to WebApplicationFactory in test project
/// <summary>
/// The entry-point partial class for the ApiService, made accessible to <c>WebApplicationFactory</c> in the test project.
/// </summary>
public partial class Program;