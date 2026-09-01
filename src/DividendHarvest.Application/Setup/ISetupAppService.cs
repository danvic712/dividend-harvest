namespace DividendHarvest.Application.Setup;

public interface ISetupAppService
{
    Task<SetupStatus> GetStatusAsync(CancellationToken cancellationToken);

    Task<SetupResult> InitializeAsync(
        SetupRequest request,
        CancellationToken cancellationToken);
}
