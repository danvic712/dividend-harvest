namespace DividendHarvest.Application.Exceptions;

public sealed class StockDataProviderUnavailableException : ApplicationExceptionBase
{
    public StockDataProviderUnavailableException(string message, Exception innerException)
        : base("stock_data_provider_unavailable", innerException)
    {
        _ = message;
    }
}
