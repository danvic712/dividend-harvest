import { Check, ChevronRight } from "lucide-react"

import { cn, stockKey } from "@/lib/utils"
import type { StockRecommendationResult, StockWatchlistItem } from "@/lib/api-types"

export function StockSelector({ stocks, selectedKey, onSelect, recommendations = [] }: { stocks: StockWatchlistItem[]; selectedKey: string | null; onSelect: (stock: StockWatchlistItem) => void; recommendations?: StockRecommendationResult[] }) {
  return (
    <section className="d-watchlist">
      <div className="d-watchlist-heading"><div><div className="d-kicker"><span>00</span><span>我的股票</span><i /></div><h2>今天先看哪一只？</h2></div><span>{stocks.length} 只关注标的 · 每只单独配置</span></div>
      <div className="stock-rail d-stock-rail" role="list" aria-label="股票列表">
      {stocks.map((stock) => {
        const selected = stockKey(stock) === selectedKey
        const recommendation = recommendations.find((item) => stockKey(item.analysis) === stockKey(stock))
        const status = recommendation?.analysis.recommendationCode.toLowerCase() ?? "missing"
        const statusLabel = status === "missing" ? "暂无数据" : status.includes("trim") ? "减仓候选" : status === "strong_buy" ? "强买入" : status === "accumulate" ? "分批加仓" : status === "re_evaluate" ? "需要复核" : status === "no_action" || status === "hold" ? "暂不操作" : "等待数据"
        const statusTone = status.includes("trim") ? "reduce" : status === "strong_buy" ? "strong" : status === "accumulate" ? "add" : "hold"
        const initials = stock.securityName.slice(0, 2)
        return (
          <button key={`${stock.securityCode}-${stock.exchangeCode}`} className={cn("d-stock-tile", selected && "d-stock-tile-active")} type="button" onClick={() => onSelect(stock)} aria-pressed={selected}>
            <span className={cn("d-tile-avatar", `d-tile-${statusTone}`)}>{initials}</span>
            <span className="d-tile-name"><strong>{stock.securityName}</strong><span>{stock.securityCode}.{stock.exchangeCode}</span></span>
            <span className="d-tile-quote"><strong>{recommendation?.analysis.closePrice === null || recommendation?.analysis.closePrice === undefined ? "—" : `¥${recommendation.analysis.closePrice.toFixed(2)}`}</strong><span>{stock.sectorCode ?? "行业未设置"}</span></span>
            <span className={`signal-badge signal-${statusTone}`}>{statusLabel}</span>
            {selected ? <Check className="d-tile-arrow" size={15} /> : <ChevronRight className="d-tile-arrow" size={15} />}
          </button>
        )
      })}
      </div>
    </section>
  )
}
