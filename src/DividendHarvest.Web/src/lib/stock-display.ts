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

export function recommendationLabel(code: string | null | undefined, labels?: Record<string, string>) {
  const normalized = code?.toLowerCase() ?? ""
  const labelKey = ({
    strong_buy: "strongBuy",
    accumulate: "accumulate",
    partial_trim: "partialTrim",
    aggressive_trim: "aggressiveTrim",
    re_evaluate: "reEvaluate",
    no_action: "hold",
    hold: "hold",
  } as Record<string, string>)[normalized] ?? "unknown"
  return labels?.[labelKey] ?? ({
    strongBuy: "强买入",
    accumulate: "分批加仓",
    partialTrim: "减仓候选",
    aggressiveTrim: "激进减仓",
    reEvaluate: "需要复核",
    hold: "暂不操作",
    unknown: "等待数据",
  } as Record<string, string>)[labelKey]
}

type HeadlineLabels = Record<string, { lead: string; accent: string }>

export function recommendationHeadline(code: string | null | undefined, labels?: HeadlineLabels) {
  const normalized = code?.toLowerCase() ?? ""
  const labelKey = normalized === "strong_buy" ? "strongBuy" : normalized === "accumulate" ? "accumulate" : normalized.includes("trim") ? "trim" : normalized === "re_evaluate" ? "reEvaluate" : normalized === "no_action" ? "noAction" : "default"
  return labels?.[labelKey] ?? ({
    strongBuy: { lead: "可以", accent: "试买" },
    accumulate: { lead: "可以", accent: "加仓" },
    trim: { lead: "考虑", accent: "减仓" },
    reEvaluate: { lead: "需要", accent: "复核" },
    noAction: { lead: "暂不", accent: "动作" },
    default: { lead: "先", accent: "观察" },
  } as HeadlineLabels)[labelKey]
}

export function recommendationSignalTitle(code: string | null | undefined, labels?: Record<string, string>) {
  const normalized = code?.toLowerCase() ?? ""
  const labelKey = normalized === "strong_buy" ? "strongBuy" : normalized === "accumulate" ? "accumulate" : normalized.includes("trim") ? "trim" : normalized === "re_evaluate" ? "reEvaluate" : normalized === "no_action" ? "noAction" : "default"
  return labels?.[labelKey] ?? ({
    strongBuy: "价格进入试探区间",
    accumulate: "收益率回到可接受区间",
    trim: "收益率偏低，先保护仓位",
    reEvaluate: "资料需要重新确认",
    noAction: "当前没有需要执行的动作",
    default: "等待收益率重新抬升",
  } as Record<string, string>)[labelKey]
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
