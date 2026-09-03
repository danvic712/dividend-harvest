import axios from "axios"

import { apiClient } from "@/lib/api-client"
import type { SaveStockModelParametersRequest, StockModelParameterSet, StockWatchlistItem } from "@/lib/api-types"

export async function getSettingsStocks() {
  const response = await apiClient.get<StockWatchlistItem[]>("/stocks")
  return response.data
}

export async function getSettingsParameters(securityCode: string, exchangeCode: string, signal?: AbortSignal) {
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

export async function updateSettingsParameters(request: SaveStockModelParametersRequest) {
  const response = await apiClient.post<StockModelParameterSet>("/stocks/model-parameters", request)
  return response.data
}
