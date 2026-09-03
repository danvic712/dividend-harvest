import { ArrowDownRight, ArrowUpRight, RefreshCw } from "lucide-react"
import { useEffect, useState } from "react"

import { AppShell, PageTitle, SectionHeading } from "@/components/app-shell"
import { EmptyState, ErrorState, LoadingState } from "@/components/async-state"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { getApiErrorMessage } from "@/lib/api-errors"
import { displayStockName } from "@/lib/stock-display"
import { formatMoney, stockKey, today } from "@/lib/utils"
import type { PortfolioTradeResult, RecordPortfolioTradeRequest, StockWatchlistItem } from "@/lib/api-types"
import { getPortfolioStocks, recordPortfolioTrade } from "@/features/portfolio/portfolio.api"
import "./portfolio.css"

export function PortfolioPage({ onNavigate }: { onNavigate: (path: string) => void }) {
  const query = new URLSearchParams(window.location.search)
  const [stocks, setStocks] = useState<StockWatchlistItem[]>([])
  const initialKey = query.get("stock") ?? (query.get("code") && query.get("exchange") ? `${query.get("code")}:${query.get("exchange")}` : null)
  const [selectedKey, setSelectedKey] = useState(initialKey ?? "")
  const [direction, setDirection] = useState<"buy" | "sell">(query.get("direction") === "sell" ? "sell" : "buy")
  const [tradeDate, setTradeDate] = useState(today())
  const [quantity, setQuantity] = useState("")
  const [price, setPrice] = useState("")
  const [fee, setFee] = useState("0")
  const [sourceRecordId, setSourceRecordId] = useState("")
  const [result, setResult] = useState<PortfolioTradeResult | null>(null)
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    void getPortfolioStocks().then((watchlist) => {
      setStocks(watchlist)
      setSelectedKey((current) => current && watchlist.some((stock) => stockKey(stock) === current) ? current : watchlist[0] ? stockKey(watchlist[0]) : "")
    }).catch((loadError: unknown) => setError(getApiErrorMessage(loadError, "持仓股票暂时无法读取。"))).finally(() => setLoading(false))
  }, [])

  async function submit() {
    const stock = stocks.find((item) => stockKey(item) === selectedKey)
    setError(null)
    setResult(null)
    if (!stock || !quantity || Number(quantity) <= 0 || !price || Number(price) <= 0) {
      setError("请选择股票，并填写大于零的成交股数和成交价格。")
      return
    }
    const request: RecordPortfolioTradeRequest = { securityCode: stock.securityCode, exchangeCode: stock.exchangeCode, tradeDate, tradeDirectionCode: direction, shareQuantity: Number(quantity), pricePerShare: Number(price), transactionFeeAmount: Number(fee) || 0, sourceRecordId: sourceRecordId.trim() || null }
    setSubmitting(true)
    try {
      setResult(await recordPortfolioTrade(request))
      setQuantity("")
      setPrice("")
      setFee("0")
      setSourceRecordId("")
    } catch (submitError) {
      setError(getApiErrorMessage(submitError, "交易记录失败，请检查持仓和现金流水。"))
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) return <div className="page-wrap"><LoadingState label="正在读取持仓股票…" /></div>
  if (error && !stocks.length) return <div className="page-wrap"><ErrorState message={error} onRetry={() => window.location.reload()} /></div>
  if (!stocks.length) return <div className="page-wrap"><EmptyState title="还没有可记录的股票" description="完成首次设置后，才能为股票记录买入和卖出。" action={<Button onClick={() => onNavigate("/setup")}>去建立组合</Button>} /></div>

  return (
    <AppShell currentPath="/portfolio" onNavigate={onNavigate} dataState="unknown">
      <PageTitle eyebrow="PORTFOLIO / SIMULATION LEDGER" title="把决定，记成自己的数据。" description="记录实际发生过的模拟买入或卖出，系统会更新持仓、核心股数、目标股数和成本价。" actions={<Button variant="outline" onClick={() => window.location.reload()}><RefreshCw data-icon="inline-start" />重新读取</Button>} />
      {error && <Alert variant="destructive" style={{ marginBottom: 16 }}><AlertTitle>记录提醒</AlertTitle><AlertDescription>{error}</AlertDescription></Alert>}
      <div className="portfolio-layout">
        <section className="surface portfolio-form-card"><SectionHeading label="TRADE RECORD" title="记录一笔交易" description="这里只记录，不会发送到券商。卖出时后端会校验当前持仓股数。" /><div className="form-grid" style={{ marginTop: 22 }}><div className="field form-grid-wide"><label>交易方向</label><div className="trade-direction"><button className={`direction-button ${direction === "buy" ? "direction-button-active" : ""}`} type="button" onClick={() => setDirection("buy")} aria-pressed={direction === "buy"}><ArrowUpRight size={16} />模拟买入</button><button className={`direction-button ${direction === "sell" ? "direction-button-active" : ""}`} type="button" onClick={() => setDirection("sell")} aria-pressed={direction === "sell"}><ArrowDownRight size={16} />模拟卖出</button></div></div><div className="field form-grid-wide"><label htmlFor="trade-stock">股票</label><select id="trade-stock" value={selectedKey} onChange={(event) => setSelectedKey(event.target.value)}>{stocks.map((stock) => <option key={stockKey(stock)} value={stockKey(stock)}>{stock.securityCode} · {stock.exchangeCode} · {displayStockName(stock)}</option>)}</select></div><div className="field"><label htmlFor="trade-date">交易日期</label><Input id="trade-date" type="date" value={tradeDate} onChange={(event) => setTradeDate(event.target.value)} /></div><div className="field"><label htmlFor="trade-quantity">成交股数</label><Input id="trade-quantity" type="number" min="1" step="1" value={quantity} onChange={(event) => setQuantity(event.target.value)} placeholder="100" /></div><div className="field"><label htmlFor="trade-price">成交价格</label><Input id="trade-price" type="number" min="0.01" step="0.01" value={price} onChange={(event) => setPrice(event.target.value)} placeholder="0.00" /></div><div className="field"><label htmlFor="trade-fee">交易费用</label><Input id="trade-fee" type="number" min="0" step="0.01" value={fee} onChange={(event) => setFee(event.target.value)} /></div><div className="field form-grid-wide"><label htmlFor="trade-source">来源记录标识（可选）</label><Input id="trade-source" value={sourceRecordId} onChange={(event) => setSourceRecordId(event.target.value)} placeholder="例如个人交易记录编号" /></div><div className="form-actions"><Button size="lg" onClick={submit} disabled={submitting}>{submitting ? "保存中…" : "保存交易记录"}</Button></div></div></section>
        <section className="surface portfolio-result-card"><SectionHeading label="UPDATED POSITION" title="最新持仓结果" description="保存一笔交易后，这里展示后端返回的新状态。" />{result ? <div className="trade-result"><div className="trade-result-main"><span>本次交易本金</span><strong>{formatMoney(result.tradePrincipalAmount)}</strong></div><div className="result-list"><div className="data-line"><span>持仓股数</span><strong>{result.heldShares.toLocaleString("zh-CN")} 股</strong></div><div className="data-line"><span>核心股数</span><strong>{result.coreShares.toLocaleString("zh-CN")} 股</strong></div><div className="data-line"><span>目标股数</span><strong>{result.targetShares.toLocaleString("zh-CN")} 股</strong></div><div className="data-line"><span>当前成本价</span><strong>{formatMoney(result.averageCostPerShare)}</strong></div></div><Alert><AlertTitle>交易记录已保存</AlertTitle><AlertDescription>这笔记录已写入本地数据库，之后的组合建议会读取新的持仓状态。</AlertDescription></Alert></div> : <EmptyState title="等待一笔记录" description="完成表单后，后端会返回更新后的持仓快照。" />}</section>
      </div>
    </AppShell>
  )
}
