export type SetupStatus = {
  isComplete: boolean
  missingRequirements: string[]
}

export type SetupStockRequest = {
  securityCode: string
  exchangeCode: string
  initialHolding: InitialHoldingInput | null
}

export type SetupRequest = {
  portfolioName: string
  stocks: SetupStockRequest[]
}

export type InitialHoldingInput = {
  heldShares: number
  coreShares: number
  targetShares: number
  averageCostPerShare: number
}

export type SetupResult = {
  portfolioId: string
  portfolioName: string
  stocks: Array<{
    securityCode: string
    exchangeCode: string
    securityName: string
  }>
}

export type StockHoldingSnapshot = {
  heldShares: number
  coreShares: number
  targetShares: number
  averageCostPerShare: number
}

export type StockWatchlistItem = {
  securityId: string
  securityCode: string
  exchangeCode: string
  securityName: string
  marketCode: string
  currencyCode: string
  sectorCode?: string | null
  holding?: StockHoldingSnapshot | null
}

export type StockAnalysisResult = {
  securityCode: string
  exchangeCode: string
  securityName: string
  modelStatusCode: string
  dividendReliabilityCode: string
  closePrice: number | null
  modelDividendPerShare: number | null
  dividendModeCode: string | null
  dividendYield: number | null
  strongBuyPrice: number | null
  accumulatePrice: number | null
  partialTrimPrice: number | null
  aggressiveTrimPrice: number | null
  observedPriceZoneCode: string | null
  priceZoneCode: string | null
  priceZoneConfirmed: boolean
  recommendationCode: string
  heldShares: number
  coreShares: number
  satelliteShares: number
  dataAsOfDate: string | null
  modelParameterSetId: string | null
  computedAt: string
  explanation: string
  securityId: string
}

export type StockRecommendationResult = {
  analysis: StockAnalysisResult
  suggestedBuyShares: number
  suggestedSellShares: number
  suggestedTradeAmount: number
  estimatedTransactionFeeAmount: number
}

export type PortfolioRecommendationResult = {
  portfolioId: string
  startingAvailableBudgetAmount: number
  remainingAvailableBudgetAmount: number
  totalSuggestedTradeAmount: number
  estimatedTransactionFeeAmount: number
  stocks: StockRecommendationResult[]
  computedAt: string
}

export type StockModelParameterSet = {
  modelParameterSetId: string
  securityCode: string
  exchangeCode: string
  modelVersion: string
  strongBuyYieldThreshold: number
  accumulationYieldThreshold: number
  partialTrimYieldThreshold: number
  aggressiveTrimYieldThreshold: number
  strongBuyBudgetRatio: number
  accumulateBudgetRatio: number
  partialTrimRatio: number
  aggressiveTrimRatio: number
  maxSecurityWeight: number
  maxSectorWeight: number
  cashReserveRatio: number
  maxSingleTradeAmount: number
  maxPeriodBudgetAmount: number
  transactionFeeRatio: number
  minimumTransactionFeeAmount: number
  tradingLotSize: number
  effectiveFromDate: string
}

export type SaveStockModelParametersRequest = Omit<
  StockModelParameterSet,
  "modelParameterSetId"
>

export type BudgetSummary = {
  portfolioId: string
  portfolioName: string
  totalInflowAmount: number
  totalOutflowAmount: number
  cashBalanceAmount: number
  entryCount: number
  computedAt: string
}

export type RecordCashLedgerEntryRequest = {
  entryDate: string
  entryTypeCode: string
  cashDirectionCode: string
  cashAmount: number
  securityCode: string | null
  exchangeCode: string | null
  sourceRecordId: string | null
}

export type CashLedgerEntryResult = RecordCashLedgerEntryRequest & {
  cashLedgerEntryId: string
  portfolioId: string
}

export type RecordPortfolioTradeRequest = {
  securityCode: string
  exchangeCode: string
  tradeDate: string
  tradeDirectionCode: "buy" | "sell"
  shareQuantity: number
  pricePerShare: number
  transactionFeeAmount: number
  sourceRecordId: string | null
}

export type PortfolioTradeResult = RecordPortfolioTradeRequest & {
  portfolioTradeId: string
  portfolioId: string
  heldShares: number
  coreShares: number
  targetShares: number
  averageCostPerShare: number
  tradePrincipalAmount: number
}

export type StockDataSyncRunResult = {
  attemptedStockCount: number
  fullyCompletedStockCount: number
  partiallyFailedStockCount: number
  failures: StockDataSyncFailure[]
  completedAt: string
}

export type StockDataSyncFailure = {
  securityCode: string
  exchangeCode: string
  dataKind: string
  errorCode: string
  parameters: Record<string, unknown>
}
