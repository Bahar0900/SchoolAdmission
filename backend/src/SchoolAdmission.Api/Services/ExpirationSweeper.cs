namespace SchoolAdmission.Api.Services;

public class ExpirationSweeper : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpirationSweeper> _logger;

    public ExpirationSweeper(IServiceProvider serviceProvider, ILogger<ExpirationSweeper> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Expiration Sweeper started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var admissionService = scope.ServiceProvider.GetRequiredService<IAdmissionService>();
                await admissionService.CleanExpiredHoldsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Expiration Sweeper.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
