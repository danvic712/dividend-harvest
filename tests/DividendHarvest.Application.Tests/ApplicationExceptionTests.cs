using DividendHarvest.Application.Exceptions;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class ApplicationExceptionTests
{
    [Fact]
    public void Generic_error_factory_preserves_a_stable_code_and_safe_parameters()
    {
        var exception = ApplicationErrors.WithSecurity(
            ApplicationErrorCodes.StockNotConfigured,
            "000001");

        Assert.IsType<ApplicationErrorException>(exception);
        Assert.Equal("stock_not_configured", exception.ErrorCode);
        Assert.Equal("000001", exception.Parameters["securityCode"]);
    }

    [Fact]
    public void Validation_exceptions_share_one_validation_seam_and_stable_error_codes()
    {
        ApplicationValidationException[] exceptions =
        [
            ApplicationErrors.Validation(ApplicationErrorCodes.SetupValidationFailed, "invalid"),
            ApplicationErrors.Validation(ApplicationErrorCodes.ModelParameterValidationFailed, "invalid"),
            ApplicationErrors.Validation(ApplicationErrorCodes.StockAnalysisValidationFailed, "invalid"),
            ApplicationErrors.Validation(ApplicationErrorCodes.StockDataSyncValidationFailed, "invalid"),
            ApplicationErrors.Validation(ApplicationErrorCodes.BudgetValidationFailed, "invalid"),
            ApplicationErrors.Validation(ApplicationErrorCodes.PortfolioTradeValidationFailed, "invalid")
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
        var exception = ApplicationErrors.WithSecurityReference(
            ApplicationErrorCodes.StockNotConfigured,
            "000001",
            "SZSE");

        Assert.Equal("stock_not_configured", exception.ErrorCode);
    }

    [Fact]
    public void Source_record_conflicts_have_stable_conflict_error_codes()
    {
        Assert.Equal(
            "cash_ledger_entry_conflict",
            ApplicationErrors.WithSourceRecord(
                ApplicationErrorCodes.CashLedgerEntryConflict,
                "cash-1").ErrorCode);
        Assert.Equal(
            "portfolio_trade_conflict",
            ApplicationErrors.WithSourceRecord(
                ApplicationErrorCodes.PortfolioTradeConflict,
                "trade-1").ErrorCode);
    }
}
