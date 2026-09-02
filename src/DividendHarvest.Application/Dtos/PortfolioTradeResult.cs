namespace DividendHarvest.Application.Dtos;

public sealed record PortfolioTradeResult(
    Guid PortfolioTradeId,
    Guid PortfolioId,
    string SecurityCode,
    string ExchangeCode,
    DateOnly TradeDate,
    string TradeDirectionCode,
    int ShareQuantity,
    decimal PricePerShare,
    decimal TransactionFeeAmount,
    int HeldShares,
    int CoreShares,
    int TargetShares,
    decimal AverageCostPerShare,
    decimal TradePrincipalAmount);
