namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("stock_data_sync_validation_failed")]
public sealed class StockDataSyncValidationException(string message)
    : ApplicationValidationException(message);
