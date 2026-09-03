import { apiClient } from "@/lib/api-client"
import type { BudgetSummary, PortfolioRecommendationResult, StockWatchlistItem } from "@/lib/api-types"

export async function getRecommendations() {
  const response = await apiClient.get<PortfolioRecommendationResult>("/recommendations")
  return response.data
}

export async function getWatchlist() {
  const response = await apiClient.get<StockWatchlistItem[]>("/stocks")
  return response.data
}

export async function getBudgetSummary() {
  const response = await apiClient.get<BudgetSummary>("/budgets/summary")
  return response.data
}

export async function createRecommendationSnapshot() {
  const response = await apiClient.post("/recommendations/snapshots")
  return response.data
}
