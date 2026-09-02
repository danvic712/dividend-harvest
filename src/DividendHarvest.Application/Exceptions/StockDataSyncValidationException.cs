namespace DividendHarvest.Application.Exceptions;

public sealed class StockDataSyncValidationException(string message)
    : ApplicationValidationException("stock_data_sync_validation_failed", message);
