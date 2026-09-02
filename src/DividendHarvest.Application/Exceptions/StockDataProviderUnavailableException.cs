namespace DividendHarvest.Application.Exceptions;

public sealed class StockDataProviderUnavailableException(string message, Exception innerException)
    : ApplicationExceptionBase("stock_data_provider_unavailable", message, innerException);
