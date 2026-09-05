import type { StockAnalysisResult } from "@/lib/api-types"

export type PriceZone = {
  code: string
  label: string
  price: number | null
  copy: string
  className: string
}

type PriceZoneLabels = Record<string, { label: string; copy: string }>

export function getPriceZones(analysis: Pick<StockAnalysisResult, "strongBuyPrice" | "accumulatePrice" | "closePrice" | "partialTrimPrice" | "aggressiveTrimPrice">, labels?: PriceZoneLabels): PriceZone[] {
  return [
    { code: "strong_buy", label: labels?.strongBuy?.label ?? "重点买入", price: analysis.strongBuyPrice, copy: labels?.strongBuy?.copy ?? "估值和收益率同时有吸引力，可买入较大一部分。", className: "zone-buy" },
    { code: "accumulate", label: labels?.accumulate?.label ?? "分批加仓", price: analysis.accumulatePrice, copy: labels?.accumulate?.copy ?? "接近目标区间，建议买入小部分，保留现金。", className: "zone-accumulate" },
    { code: "hold", label: labels?.hold?.label ?? "观察持有", price: analysis.closePrice, copy: labels?.hold?.copy ?? "已有仓位可继续持有，新资金暂缓。", className: "zone-hold" },
    { code: "partial_trim", label: labels?.partialTrim?.label ?? "减仓候选", price: analysis.partialTrimPrice, copy: labels?.partialTrim?.copy ?? "收益率偏低，仅在偏离目标较多时考虑减仓。", className: "zone-trim" },
    { code: "aggressive_trim", label: labels?.aggressiveTrim?.label ?? "激进减仓", price: analysis.aggressiveTrimPrice, copy: labels?.aggressiveTrim?.copy ?? "只在仓位与规则都允许时考虑。", className: "zone-aggressive" },
  ]
}

export function currentPriceZoneLabel(analysis: Pick<StockAnalysisResult, "priceZoneCode">, labels?: Record<string, string>) {
  return labels?.[analysis.priceZoneCode ?? ""] ?? ({
    strong_buy: "重点买入",
    accumulate: "分批加仓",
    hold: "观察持有",
    partial_trim: "减仓候选",
    aggressive_trim: "激进减仓",
  }[analysis.priceZoneCode ?? ""] ?? "待确认")
}
