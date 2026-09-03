using DividendHarvest.Application.Contracts;

namespace DividendHarvest.Infrastructure.Exceptions;

public sealed class FtShareProviderException(Exception innerException)
    : Exception(null, innerException), IStockDataProviderFailure;
