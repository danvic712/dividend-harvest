using DividendHarvest.Application.Dtos;

namespace DividendHarvest.Application.Contracts;

public interface IPortfolioTradeAppService
{
    Task<PortfolioTradeResult> RecordAsync(
        RecordPortfolioTradeRequest request,
        CancellationToken cancellationToken);
}
