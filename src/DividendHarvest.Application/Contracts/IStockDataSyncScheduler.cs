namespace DividendHarvest.Application.Contracts;

public interface IStockDataSyncScheduler
{
    bool TrySchedule();
}
