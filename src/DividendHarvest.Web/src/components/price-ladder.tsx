import type { StockAnalysisResult } from "@/lib/api-types"
import { getPriceZones } from "@/lib/price-zones"
import { displayStockName } from "@/lib/stock-display"
import { formatMoney } from "@/lib/utils"

export function PriceLadder({ analysis, compact = false }: { analysis: StockAnalysisResult; compact?: boolean }) {
  const zones = getPriceZones(analysis)
  const validPrices = [analysis.strongBuyPrice, analysis.accumulatePrice, analysis.partialTrimPrice, analysis.aggressiveTrimPrice]

  if (validPrices.some((price) => price === null || price <= 0)) {
    return <div className={`d-ladder-unavailable ${compact ? "d-ladder-unavailable-compact" : ""}`}><span>价格阶梯等待资料</span><small>完整同步 TTM 实际股息和行情后，系统会计算五档参考区间。</small></div>
  }

  const [strongBuy, accumulate, partialTrim, aggressiveTrim] = validPrices as number[]
  const lowerStep = Math.max(accumulate - strongBuy, strongBuy * 0.08)
  const upperStep = Math.max(aggressiveTrim - partialTrim, aggressiveTrim * 0.08)
  const lower = Math.max(0.01, strongBuy - lowerStep)
  const upper = aggressiveTrim + upperStep
  const boundaries = [lower, strongBuy, accumulate, partialTrim, aggressiveTrim, upper]
  const markerPosition = analysis.closePrice === null ? 50 : Math.max(2, Math.min(98, ((analysis.closePrice - lower) / (upper - lower)) * 100))

  return (
    <div className={`price-ladder ${compact ? "price-ladder-compact" : ""}`}>
      <div className="ladder-axis" aria-hidden="true">
        {boundaries.map((value) => <span key={value}>{formatMoney(value)}</span>)}
      </div>
      <div className="ladder-track" aria-label={`${displayStockName(analysis)}价格阶梯`}>
        {zones.map((zone, index) => <div className={`ladder-zone ${zone.className}`} key={zone.code} style={{ flex: boundaries[index + 1] - boundaries[index] }}><span>{zone.label}</span></div>)}
        <div className="current-price-marker" style={{ left: `${markerPosition}%` }}><span>{formatMoney(analysis.closePrice)}</span><i /></div>
      </div>
      {!compact && <div className="ladder-legend">{zones.map((zone, index) => <div className="legend-item" key={zone.code}><span className={`legend-swatch ${zone.className}`} /><span>{zone.label}</span><small>{formatMoney(boundaries[index])}—{formatMoney(boundaries[index + 1])}</small></div>)}</div>}
    </div>
  )
}

export function PriceZoneBoard({ analysis }: { analysis: StockAnalysisResult }) {
  return <div className="d-zone-grid">{getPriceZones(analysis).map((zone) => <div className={`d-zone-card ${zone.className} ${analysis.priceZoneCode === zone.code ? "d-zone-card-active" : ""}`} key={zone.code}><span>{zone.label}</span><strong>{formatMoney(zone.price)}</strong><small>{zone.copy}</small></div>)}</div>
}
