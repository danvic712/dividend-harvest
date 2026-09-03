import { apiClient } from "@/lib/api-client"
import type { BudgetSummary, CashLedgerEntryResult, RecordCashLedgerEntryRequest } from "@/lib/api-types"

export async function getBudget() {
  const response = await apiClient.get<BudgetSummary>("/budgets/summary")
  return response.data
}

export async function recordBudgetEntry(request: RecordCashLedgerEntryRequest) {
  const response = await apiClient.post<CashLedgerEntryResult>("/budgets/entries", request)
  return response.data
}
