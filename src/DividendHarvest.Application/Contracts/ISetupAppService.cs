using DividendHarvest.Application.Dtos;

namespace DividendHarvest.Application.Contracts;

public interface ISetupAppService
{
    Task<SetupStatus> GetStatusAsync(CancellationToken cancellationToken);

    Task<SetupResult> InitializeAsync(
        SetupRequest request,
        CancellationToken cancellationToken);
}
