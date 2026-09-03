import { Check, ChevronRight } from "lucide-react"

import { cn, stockKey } from "@/lib/utils"
import { displayStockName, hasAnalysisData, recommendationLabel, recommendationTone } from "@/lib/stock-display"
import type { StockRecommendationResult, StockWatchlistItem } from "@/lib/api-types"

export function StockSelector({ stocks, selectedKey, onSelect, recommendations = [] }: { stocks: StockWatchlistItem[]; selectedKey: string | null; onSelect: (stock: StockWatchlistItem) => void; recommendations?: StockRecommendationResult[] }) {
  return (
    <section className="d-watchlist">
      <div className="d-watchlist-heading"><div><div className="d-kicker"><span>00</span><span>我的股票</span><i /></div><h2>今天先看哪一只？</h2></div><span>{stocks.length} 只关注标的 · 每只单独配置</span></div>
      <div className="stock-rail d-stock-rail" role="list" aria-label="股票列表">
      {stocks.map((stock) => {
        const selected = stockKey(stock) === selectedKey
        const recommendation = recommendations.find((item) => stockKey(item.analysis) === stockKey(stock))
        const statusCode = recommendation && hasAnalysisData(recommendation.analysis) ? recommendation.analysis.recommendationCode : null
        const statusLabel = statusCode ? recommendationLabel(statusCode) : "等待数据"
        const statusTone = recommendationTone(statusCode)
        const stockName = displayStockName(stock)
        const initials = stockName.slice(0, 2)
        return (
          <button key={`${stock.securityCode}-${stock.exchangeCode}`} className={cn("d-stock-tile", selected && "d-stock-tile-active")} type="button" onClick={() => onSelect(stock)} aria-pressed={selected}>
            <span className={cn("d-tile-avatar", `d-tile-${statusTone}`)}>{initials}</span>
            <span className="d-tile-name"><strong>{stockName}</strong><span>{stock.securityCode}.{stock.exchangeCode}</span></span>
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
