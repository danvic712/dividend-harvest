using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Validators;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class SetupRequestValidatorTests
{
    [Fact]
    public async Task ValidateAsync_rejects_core_shares_above_held_shares()
    {
        var validator = CreateValidator();
        var request = new SetupRequest(
            "长期股息组合",
            [new SetupStockRequest(
                "000001",
                "SZSE",
                new InitialHoldingInput(100, 101, 120, 10m))]);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == "Stocks[0].InitialHolding.CoreShares"
            && error.ErrorMessage == "核心仓数量不能为负数或超过持股数量。");
    }

    [Fact]
    public async Task ValidateAsync_rejects_invalid_stock_reference()
    {
        var validator = CreateValidator();
        var request = new SetupRequest(
            "长期股息组合",
            [new SetupStockRequest("123", "NYSE", null)]);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == "Stocks[0].SecurityCode"
            && error.ErrorMessage == "A 股股票代码必须是 6 位数字。");
        Assert.Contains(result.Errors, error =>
            error.PropertyName == "Stocks[0].ExchangeCode"
            && error.ErrorMessage == "交易所必须是 SSE、SZSE 或 BSE。");
    }

    [Fact]
    public async Task ValidateAsync_rejects_empty_portfolio_name_and_stock_list()
    {
        var validator = CreateValidator();
        var request = new SetupRequest("   ", []);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == "PortfolioName"
            && error.ErrorMessage == "投资组合名称必须为 1 到 100 个字符。");
        Assert.Contains(result.Errors, error =>
            error.PropertyName == "Stocks"
            && error.ErrorMessage == "至少需要配置一只 A 股股票。");
    }

    [Fact]
    public async Task ValidateAsync_rejects_negative_initial_holding_values()
    {
        var validator = CreateValidator();
        var request = new SetupRequest(
            "长期股息组合",
            [new SetupStockRequest(
                "000001",
                "SZSE",
                new InitialHoldingInput(-1, -2, -3, -1m))]);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == "Stocks[0].InitialHolding.HeldShares"
            && error.ErrorMessage == "持股数量不能为负数。");
        Assert.Contains(result.Errors, error =>
            error.PropertyName == "Stocks[0].InitialHolding.CoreShares"
            && error.ErrorMessage == "核心仓数量不能为负数或超过持股数量。");
        Assert.Contains(result.Errors, error =>
            error.PropertyName == "Stocks[0].InitialHolding.TargetShares"
            && error.ErrorMessage == "目标股数不能为负数。");
        Assert.Contains(result.Errors, error =>
            error.PropertyName == "Stocks[0].InitialHolding.AverageCostPerShare"
            && error.ErrorMessage == "平均成本不能为负数。");
    }

    [Fact]
    public async Task ValidateAsync_rejects_duplicate_normalized_stock_references()
    {
        var validator = CreateValidator();
        var request = new SetupRequest(
            "长期股息组合",
            [
                new SetupStockRequest("000001", "szse", null),
                new SetupStockRequest(" 000001 ", "SZSE", null)
            ]);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == "Stocks"
            && error.ErrorMessage == "不能重复配置同一只股票。");
    }

    [Fact]
    public async Task ValidateAsync_rejects_null_stock_items()
    {
        var validator = CreateValidator();
        var request = new SetupRequest(
            "长期股息组合",
            [null!]);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == "Stocks[0]"
            && error.ErrorMessage == "股票配置不能为空。");
    }

    [Fact]
    public async Task ValidateAsync_rejects_null_stock_collection()
    {
        var validator = CreateValidator();
        var request = new SetupRequest("长期股息组合", null!);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == "Stocks"
            && error.ErrorMessage == "至少需要配置一只 A 股股票。");
    }

    private static SetupRequestValidator CreateValidator()
        => new(new SetupStockRequestValidator(new InitialHoldingInputValidator()));
}
