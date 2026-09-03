namespace DividendHarvest.Application.Exceptions;

public static class ApplicationErrorCodes
{
    public const string SetupValidationFailed = "setup_validation_failed";
    public const string SetupAlreadyCompleted = "setup_already_completed";
    public const string SetupNotCompleted = "setup_not_completed";
    public const string ModelParameterValidationFailed = "model_parameter_validation_failed";
    public const string ModelParameterVersionAlreadyExists = "model_parameter_version_already_exists";
    public const string StockAnalysisValidationFailed = "stock_analysis_validation_failed";
    public const string StockDataSyncValidationFailed = "stock_data_sync_validation_failed";
    public const string StockDataProviderUnavailable = "stock_data_provider_unavailable";
    public const string StockDataUnavailable = "stock_data_unavailable";
    public const string StockMarketDataUnavailable = "stock_market_data_unavailable";
    public const string StockDividendDataUnavailable = "stock_dividend_data_unavailable";
    public const string StockFinancialDataUnavailable = "stock_financial_data_unavailable";
    public const string StockNotConfigured = "stock_not_configured";
    public const string BudgetValidationFailed = "budget_validation_failed";
    public const string CashLedgerEntryConflict = "cash_ledger_entry_conflict";
    public const string PortfolioTradeValidationFailed = "portfolio_trade_validation_failed";
    public const string PortfolioPositionMissingForTrade = "portfolio_position_missing_for_trade";
    public const string PortfolioTradeConflict = "portfolio_trade_conflict";
    public const string Unknown = "application_error_unknown";

    public static IReadOnlyList<string> All { get; } =
    [
        SetupValidationFailed,
        SetupAlreadyCompleted,
        SetupNotCompleted,
        ModelParameterValidationFailed,
        ModelParameterVersionAlreadyExists,
        StockAnalysisValidationFailed,
        StockDataSyncValidationFailed,
        StockDataProviderUnavailable,
        StockDataUnavailable,
        StockMarketDataUnavailable,
        StockDividendDataUnavailable,
        StockFinancialDataUnavailable,
        StockNotConfigured,
        BudgetValidationFailed,
        CashLedgerEntryConflict,
        PortfolioTradeValidationFailed,
        PortfolioPositionMissingForTrade,
        PortfolioTradeConflict
    ];

    public static IReadOnlyList<string> ExpectedStockSyncFailures { get; } =
    [
        StockMarketDataUnavailable,
        StockDividendDataUnavailable,
        StockFinancialDataUnavailable,
        StockDataSyncValidationFailed,
        StockNotConfigured
    ];
}
