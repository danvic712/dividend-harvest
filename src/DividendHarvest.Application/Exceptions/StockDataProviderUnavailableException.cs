namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("stock_data_provider_unavailable")]
public sealed class StockDataProviderUnavailableException(Exception innerException)
    : ApplicationExceptionBase("stock_data_provider_unavailable", innerException);
