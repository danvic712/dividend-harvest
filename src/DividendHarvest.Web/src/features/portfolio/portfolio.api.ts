import { apiClient } from "@/lib/api-client"
import type { PortfolioTradeResult, RecordPortfolioTradeRequest, StockWatchlistItem } from "@/lib/api-types"

export async function getPortfolioStocks() {
  const response = await apiClient.get<StockWatchlistItem[]>("/stocks")
  return response.data
}

export async function recordPortfolioTrade(request: RecordPortfolioTradeRequest) {
  const response = await apiClient.post<PortfolioTradeResult>("/portfolio/trades", request)
  return response.data
}
