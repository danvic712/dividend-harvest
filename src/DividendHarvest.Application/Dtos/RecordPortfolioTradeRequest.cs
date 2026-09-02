namespace DividendHarvest.Application.Dtos;

public sealed record RecordPortfolioTradeRequest(
    string SecurityCode,
    string ExchangeCode,
    DateOnly TradeDate,
    string TradeDirectionCode,
    int ShareQuantity,
    decimal PricePerShare,
    decimal TransactionFeeAmount,
    string? SourceRecordId);
