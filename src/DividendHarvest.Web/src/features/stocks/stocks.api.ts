import axios from "axios"

import { apiClient } from "@/lib/api-client"
import type { SaveStockModelParametersRequest, StockDataSyncRunResult, StockModelParameterSet, StockRecommendationResult, StockWatchlistItem } from "@/lib/api-types"

export async function getStocks() {
  const response = await apiClient.get<StockWatchlistItem[]>("/stocks")
  return response.data
}

export async function getStockAnalysis(securityCode: string, exchangeCode: string, signal?: AbortSignal) {
  const response = await apiClient.get<StockRecommendationResult>(`/stocks/${securityCode}/${exchangeCode}/analysis`, { signal })
  return response.data
}

export async function getModelParameters(securityCode: string, exchangeCode: string, signal?: AbortSignal) {
  try {
    const response = await apiClient.get<StockModelParameterSet | null>(`/stocks/${securityCode}/${exchangeCode}/model-parameters`, { signal })
    return response.data
  } catch (error) {
    if (axios.isAxiosError(error) && error.response?.status === 404) {
      return null
    }

    throw error
  }
}

export async function syncStocks() {
  const response = await apiClient.post<StockDataSyncRunResult>("/stocks/sync")
  return response.data
}

export async function saveModelParameters(request: SaveStockModelParametersRequest) {
  const response = await apiClient.post<StockModelParameterSet>("/stocks/model-parameters", request)
  return response.data
}
