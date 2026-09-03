using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Localization;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class ApplicationErrorCatalogTests
{
    private readonly IApplicationErrorCatalog catalog = new ApplicationErrorCatalog();
    private readonly IApplicationErrorLocalizer localizer;

    public ApplicationErrorCatalogTests()
    {
        localizer = new ApplicationErrorLocalizer(catalog);
    }

    [Fact]
    public void Resolve_loads_embedded_zh_cn_error_definitions()
    {
        var localized = localizer.Localize(
            ApplicationErrors.Simple(ApplicationErrorCodes.SetupAlreadyCompleted),
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
        var localized = localizer.Localize(
            ApplicationErrors.Simple(ApplicationErrorCodes.SetupAlreadyCompleted),
            "en-US");

        Assert.Contains("en-US", catalog.SupportedCultureNames);
        Assert.Equal("en-US", localized.CultureName);
        Assert.Equal("Setup already completed", localized.Title);
        Assert.Contains("cannot be initialized again", localized.Detail);
    }

    [Fact]
    public void Resolve_uses_the_highest_quality_supported_language()
    {
        var localized = localizer.Localize(
            ApplicationErrors.Simple(ApplicationErrorCodes.SetupAlreadyCompleted),
            "zh-CN;q=0.1,en-US;q=0.9");

        Assert.Equal("en-US", localized.CultureName);
    }

    [Fact]
    public void Resolve_returns_the_canonical_culture_name()
    {
        var localized = localizer.Localize(
            ApplicationErrors.Simple(ApplicationErrorCodes.SetupAlreadyCompleted),
            "en-us");

        Assert.Equal("en-US", localized.CultureName);
    }

    [Fact]
    public void Resolve_does_not_mix_chinese_validation_text_into_english_response()
    {
        var localized = localizer.Localize(
            ApplicationErrors.Validation(
                ApplicationErrorCodes.SetupValidationFailed,
                "投资组合名称必须为 1 到 100 个字符。"),
            "en-US");

        Assert.Contains("Please review", localized.Detail);
        Assert.DoesNotContain("投资组合名称", localized.Detail);
    }

    [Fact]
    public void Resolve_uses_default_locale_for_an_unsupported_accept_language()
    {
        var localized = localizer.Localize(
            ApplicationErrors.Simple(ApplicationErrorCodes.SetupNotCompleted),
            "fr-FR,fr;q=0.9");

        Assert.Equal(catalog.DefaultCultureName, localized.CultureName);
        Assert.Equal("zh-CN", localized.CultureName);
    }

    [Fact]
    public void Resolve_interpolates_exception_parameters_without_exposing_inner_errors()
    {
        var localized = localizer.Localize(
            ApplicationErrors.WithModelParameterVersion(
                ApplicationErrorCodes.ModelParameterVersionAlreadyExists,
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
        ApplicationExceptionBase[] exceptions = ApplicationErrorCodes.All
            .Select<string, ApplicationExceptionBase>(errorCode => errorCode switch
            {
                ApplicationErrorCodes.SetupValidationFailed
                    or ApplicationErrorCodes.ModelParameterValidationFailed
                    or ApplicationErrorCodes.StockAnalysisValidationFailed
                    or ApplicationErrorCodes.StockDataSyncValidationFailed
                    or ApplicationErrorCodes.BudgetValidationFailed
                    or ApplicationErrorCodes.PortfolioTradeValidationFailed
                    => ApplicationErrors.Validation(errorCode, "validation"),
                ApplicationErrorCodes.ModelParameterVersionAlreadyExists
                    => ApplicationErrors.WithModelParameterVersion(
                        errorCode,
                        "000001",
                        new DateOnly(2026, 9, 3)),
                ApplicationErrorCodes.StockDataUnavailable
                    or ApplicationErrorCodes.StockMarketDataUnavailable
                    or ApplicationErrorCodes.StockDividendDataUnavailable
                    or ApplicationErrorCodes.StockFinancialDataUnavailable
                    => ApplicationErrors.WithSecurity(errorCode, "000001"),
                ApplicationErrorCodes.StockNotConfigured
                    => ApplicationErrors.WithSecurityReference(errorCode, "000001", "SZSE"),
                ApplicationErrorCodes.CashLedgerEntryConflict
                    or ApplicationErrorCodes.PortfolioTradeConflict
                    => ApplicationErrors.WithSourceRecord(errorCode, "source-1"),
                _ => ApplicationErrors.Simple(errorCode)
            })
            .ToArray();

        foreach (var cultureName in catalog.SupportedCultureNames)
        {
            foreach (var exception in exceptions)
            {
                var localized = localizer.Localize(exception, cultureName);

                Assert.Equal(exception.ErrorCode, localized.ErrorCode);
                Assert.NotEmpty(localized.Title);
                Assert.NotEmpty(localized.Detail);
            }
        }
    }
}
