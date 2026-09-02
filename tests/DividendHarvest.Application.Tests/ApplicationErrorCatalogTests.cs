using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Localization;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class ApplicationErrorCatalogTests
{
    private readonly IApplicationErrorCatalog catalog = new ApplicationErrorCatalog();

    [Fact]
    public void Resolve_loads_embedded_zh_cn_error_definitions()
    {
        var localized = catalog.Resolve(
            new SetupAlreadyCompletedException(),
            "zh-CN");

        Assert.Contains("zh-CN", catalog.SupportedCultureNames);
        Assert.Equal("zh-CN", localized.CultureName);
        Assert.Equal("setup_already_completed", localized.ErrorCode);
        Assert.Equal(409, localized.StatusCode);
        Assert.Equal("系统已经完成建账", localized.Title);
        Assert.Contains("不能重复初始化", localized.Detail);
    }

    [Fact]
    public void Resolve_supports_another_embedded_locale()
    {
        var localized = catalog.Resolve(
            new SetupAlreadyCompletedException(),
            "en-US");

        Assert.Contains("en-US", catalog.SupportedCultureNames);
        Assert.Equal("en-US", localized.CultureName);
        Assert.Equal("Setup already completed", localized.Title);
        Assert.Contains("cannot be initialized again", localized.Detail);
    }

    [Fact]
    public void Resolve_uses_the_highest_quality_supported_language()
    {
        var localized = catalog.Resolve(
            new SetupAlreadyCompletedException(),
            "zh-CN;q=0.1,en-US;q=0.9");

        Assert.Equal("en-US", localized.CultureName);
    }

    [Fact]
    public void Resolve_does_not_mix_chinese_validation_text_into_english_response()
    {
        var localized = catalog.Resolve(
            new SetupValidationException("投资组合名称必须为 1 到 100 个字符。"),
            "en-US");

        Assert.Contains("Please review", localized.Detail);
        Assert.DoesNotContain("投资组合名称", localized.Detail);
    }

    [Fact]
    public void Resolve_uses_default_locale_for_an_unsupported_accept_language()
    {
        var localized = catalog.Resolve(
            new SetupNotCompletedException(),
            "fr-FR,fr;q=0.9");

        Assert.Equal(catalog.DefaultCultureName, localized.CultureName);
        Assert.Equal("zh-CN", localized.CultureName);
    }

    [Fact]
    public void Resolve_interpolates_exception_parameters_without_exposing_inner_errors()
    {
        var localized = catalog.Resolve(
            new ModelParameterVersionAlreadyExistsException(
                "000001",
                new DateOnly(2026, 9, 3)),
            "en-US");

        Assert.Contains("000001", localized.Detail);
        Assert.Contains("2026-09-03", localized.Detail);
        Assert.DoesNotContain("System.", localized.Detail);
    }

    [Fact]
    public void Every_application_exception_has_a_definition_in_each_supported_locale()
    {
        ApplicationExceptionBase[] exceptions =
        [
            new SetupValidationException("validation"),
            new SetupAlreadyCompletedException(),
            new SetupNotCompletedException(),
            new ModelParameterValidationException("validation"),
            new ModelParameterVersionAlreadyExistsException("000001", new DateOnly(2026, 9, 3)),
            new StockAnalysisValidationException("validation"),
            new StockDataSyncValidationException("validation"),
            new StockDataProviderUnavailableException(new TimeoutException()),
            new StockDataUnavailableException("000001"),
            new StockMarketDataUnavailableException("000001"),
            new StockDividendDataUnavailableException("000001"),
            new StockFinancialDataUnavailableException("000001"),
            new StockNotConfiguredException("000001", "SZSE"),
            new BudgetValidationException("validation"),
            new CashLedgerEntryConflictException("cash-1"),
            new PortfolioTradeValidationException("validation"),
            new PortfolioTradeConflictException("trade-1")
        ];

        foreach (var cultureName in catalog.SupportedCultureNames)
        {
            foreach (var exception in exceptions)
            {
                var localized = catalog.Resolve(exception, cultureName);

                Assert.Equal(exception.ErrorCode, localized.ErrorCode);
                Assert.NotEmpty(localized.Title);
                Assert.NotEmpty(localized.Detail);
            }
        }
    }
}
