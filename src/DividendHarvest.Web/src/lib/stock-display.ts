import type { StockAnalysisResult, StockWatchlistItem } from "@/lib/api-types"

export type SignalTone = "strong" | "add" | "hold" | "reduce"

type StockLike = Pick<StockWatchlistItem, "securityCode"> & { securityName?: string | null }

export function displayStockName(stock: StockLike) {
  const name = stock.securityName?.trim()
  return name || `待同步 ${stock.securityCode}`
}

export function exchangeLabel(exchangeCode: string) {
  return ({ SSE: "沪市", SZSE: "深市", BSE: "北市" } as Record<string, string>)[exchangeCode] ?? exchangeCode
}

export function recommendationTone(code: string | null | undefined): SignalTone {
  const normalized = code?.toLowerCase() ?? ""
  if (normalized.includes("trim")) return "reduce"
  if (normalized === "strong_buy") return "strong"
  if (normalized === "accumulate") return "add"
  return "hold"
}

export function recommendationLabel(code: string | null | undefined) {
  const normalized = code?.toLowerCase() ?? ""
  if (normalized === "strong_buy") return "强买入"
  if (normalized === "accumulate") return "分批加仓"
  if (normalized === "partial_trim") return "减仓候选"
  if (normalized === "aggressive_trim") return "激进减仓"
  if (normalized === "re_evaluate") return "需要复核"
  if (normalized === "no_action" || normalized === "hold") return "暂不操作"
  return "等待数据"
}

export function recommendationHeadline(code: string | null | undefined) {
  const normalized = code?.toLowerCase() ?? ""
  if (normalized === "strong_buy") return { lead: "可以", accent: "试买" }
  if (normalized === "accumulate") return { lead: "可以", accent: "加仓" }
  if (normalized.includes("trim")) return { lead: "考虑", accent: "减仓" }
  if (normalized === "re_evaluate") return { lead: "需要", accent: "复核" }
  if (normalized === "no_action") return { lead: "暂不", accent: "动作" }
  return { lead: "先", accent: "观察" }
}

export function recommendationSignalTitle(code: string | null | undefined) {
  const normalized = code?.toLowerCase() ?? ""
  if (normalized === "strong_buy") return "价格进入试探区间"
  if (normalized === "accumulate") return "收益率回到可接受区间"
  if (normalized.includes("trim")) return "收益率偏低，先保护仓位"
  if (normalized === "re_evaluate") return "资料需要重新确认"
  if (normalized === "no_action") return "当前没有需要执行的动作"
  return "等待收益率重新抬升"
}

export function hasAnalysisData(analysis: Pick<StockAnalysisResult, "modelStatusCode" | "closePrice" | "modelDividendPerShare" | "priceZoneConfirmed"> | null | undefined) {
  return Boolean(
    analysis &&
    analysis.modelStatusCode.toLowerCase() !== "unavailable" &&
    analysis.closePrice !== null &&
    analysis.modelDividendPerShare !== null &&
    analysis.priceZoneConfirmed,
  )
}

export function analysisDisplayName(analysis: Pick<StockAnalysisResult, "securityName" | "securityCode">) {
  return displayStockName(analysis)
}
