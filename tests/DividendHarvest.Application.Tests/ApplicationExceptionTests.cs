using DividendHarvest.Application.Exceptions;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class ApplicationExceptionTests
{
    [Fact]
    public void Validation_exceptions_share_one_validation_seam_and_stable_error_codes()
    {
        ApplicationValidationException[] exceptions =
        [
            new SetupValidationException("invalid"),
            new ModelParameterValidationException("invalid"),
            new StockAnalysisValidationException("invalid"),
            new StockDataSyncValidationException("invalid"),
            new BudgetValidationException("invalid"),
            new PortfolioTradeValidationException("invalid")
        ];

        Assert.All(exceptions, exception => Assert.NotEmpty(exception.ErrorCode));
        Assert.Equal(
            [
                "setup_validation_failed",
                "model_parameter_validation_failed",
                "stock_analysis_validation_failed",
                "stock_data_sync_validation_failed",
                "budget_validation_failed",
                "portfolio_trade_validation_failed"
            ],
            exceptions.Select(exception => exception.ErrorCode));
    }

    [Fact]
    public void Stock_not_configured_has_a_stable_not_found_error_code()
    {
        var exception = new StockNotConfiguredException("000001", "SZSE");

        Assert.Equal("stock_not_configured", exception.ErrorCode);
    }

    [Fact]
    public void Source_record_conflicts_have_stable_conflict_error_codes()
    {
        Assert.Equal(
            "cash_ledger_entry_conflict",
            new CashLedgerEntryConflictException("cash-1").ErrorCode);
        Assert.Equal(
            "portfolio_trade_conflict",
            new PortfolioTradeConflictException("trade-1").ErrorCode);
    }
}
