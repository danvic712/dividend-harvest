import { ArrowUpRight, RefreshCw } from "lucide-react"
import { useCallback, useEffect, useState } from "react"

import { AppShell, PageTitle, SectionHeading } from "@/components/app-shell"
import { ErrorState, LoadingState } from "@/components/async-state"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { getApiErrorMessage } from "@/lib/api-errors"
import { displayStockName } from "@/lib/stock-display"
import { formatDateTime, formatMoney, stockKey, today } from "@/lib/utils"
import type { BudgetSummary, RecordCashLedgerEntryRequest, StockWatchlistItem } from "@/lib/api-types"
import { getBudget, recordBudgetEntry } from "@/features/budget/budget.api"
import { getStocks } from "@/features/stocks/stocks.api"
import "./budget.css"

type EntryType = "budget_deposit" | "dividend_received" | "buy" | "sell" | "fee" | "cash_adjustment"

const entryTypes: Array<{ code: EntryType; label: string; direction: "inflow" | "outflow"; needsStock: boolean }> = [
  { code: "budget_deposit", label: "预算入金", direction: "inflow", needsStock: false },
  { code: "dividend_received", label: "实际收到股息", direction: "inflow", needsStock: true },
  { code: "buy", label: "买入支出", direction: "outflow", needsStock: true },
  { code: "sell", label: "卖出回款", direction: "inflow", needsStock: true },
  { code: "fee", label: "交易费用", direction: "outflow", needsStock: false },
  { code: "cash_adjustment", label: "现金调整", direction: "inflow", needsStock: false },
]

export function BudgetPage({ onNavigate }: { onNavigate: (path: string) => void }) {
  const [summary, setSummary] = useState<BudgetSummary | null>(null)
  const [stocks, setStocks] = useState<StockWatchlistItem[]>([])
  const [entryType, setEntryType] = useState<EntryType>("budget_deposit")
  const [entryDate, setEntryDate] = useState(today())
  const [amount, setAmount] = useState("")
  const [selectedStock, setSelectedStock] = useState("")
  const [sourceRecordId, setSourceRecordId] = useState("")
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const [budget, watchlist] = await Promise.all([getBudget(), getStocks()])
      setSummary(budget)
      setStocks(watchlist)
    } catch (loadError) {
      setError(getApiErrorMessage(loadError, "预算资料暂时无法读取。"))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => { void load() }, 0)
    return () => window.clearTimeout(timeoutId)
  }, [load])
  const selectedType = entryTypes.find((item) => item.code === entryType) ?? entryTypes[0]

  async function submit() {
    setError(null)
    setMessage(null)
    if (!amount || Number(amount) <= 0 || (selectedType.needsStock && !selectedStock)) {
      setError(selectedType.needsStock ? "请填写金额并选择关联股票。" : "请填写大于零的金额。")
      return
    }
    const stock = stocks.find((item) => stockKey(item) === selectedStock)
    const request: RecordCashLedgerEntryRequest = { entryDate, entryTypeCode: entryType, cashDirectionCode: selectedType.direction, cashAmount: Number(amount), securityCode: stock?.securityCode ?? null, exchangeCode: stock?.exchangeCode ?? null, sourceRecordId: sourceRecordId.trim() || null }
    setSubmitting(true)
    try {
      await recordBudgetEntry(request)
      setAmount("")
      setSourceRecordId("")
      setMessage("现金流水已记录，余额将在重新读取后更新。")
      await load()
    } catch (submitError) {
      setError(getApiErrorMessage(submitError, "现金流水记录失败，请检查填写内容。"))
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) return <div className="page-wrap"><LoadingState label="正在读取预算…" /></div>
  if (error && !summary) return <div className="page-wrap"><ErrorState message={error} onRetry={() => void load()} /></div>
  if (!summary) return null

  return (
    <AppShell currentPath="/budget" onNavigate={onNavigate} lastUpdated={formatDateTime(summary.computedAt)} dataState="unknown">
      <PageTitle eyebrow="BUDGET / CASH LEDGER" title="预算先落地，动作才有边界。" description="把入金、实际股息和交易现金流记录下来，后端会在组合建议中使用同一套现金口径。" actions={<Button variant="outline" onClick={() => void load()}><RefreshCw data-icon="inline-start" />刷新余额</Button>} />
      {error && <Alert variant="destructive" style={{ marginBottom: 16 }}><AlertTitle>记录提醒</AlertTitle><AlertDescription>{error}</AlertDescription></Alert>}
      {message && <Alert style={{ marginBottom: 16 }}><AlertTitle>已完成</AlertTitle><AlertDescription>{message}</AlertDescription></Alert>}
      <div className="budget-layout">
        <section className="surface budget-form-card"><SectionHeading label="NEW LEDGER ENTRY" title="记录一笔现金流水" description="买入、卖出和实际收到股息需要关联股票；预算入金和费用不需要。" /><div className="form-grid" style={{ marginTop: 22 }}><div className="field"><label htmlFor="entry-date">发生日期</label><Input id="entry-date" type="date" value={entryDate} onChange={(event) => setEntryDate(event.target.value)} /></div><div className="field"><label htmlFor="entry-type">流水类型</label><select id="entry-type" value={entryType} onChange={(event) => setEntryType(event.target.value as EntryType)}>{entryTypes.map((item) => <option key={item.code} value={item.code}>{item.label}</option>)}</select></div><div className="field"><label htmlFor="entry-amount">金额</label><Input id="entry-amount" type="number" min="0.01" step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} placeholder="0.00" /></div><div className="field"><label htmlFor="entry-direction">现金方向</label><Input id="entry-direction" value={selectedType.direction === "inflow" ? "流入" : "流出"} readOnly /></div>{selectedType.needsStock && <div className="field form-grid-wide"><label htmlFor="entry-stock">关联股票</label><select id="entry-stock" value={selectedStock} onChange={(event) => setSelectedStock(event.target.value)}><option value="">请选择股票</option>{stocks.map((stock) => <option key={stockKey(stock)} value={stockKey(stock)}>{stock.securityCode} · {stock.exchangeCode} · {displayStockName(stock)}</option>)}</select></div>}<div className="field form-grid-wide"><label htmlFor="source-record">来源记录标识（可选）</label><Input id="source-record" value={sourceRecordId} onChange={(event) => setSourceRecordId(event.target.value)} placeholder="例如券商流水号或个人笔记编号" /></div><div className="form-actions"><Button size="lg" onClick={submit} disabled={submitting}>{submitting ? "记录中…" : "保存现金流水"}</Button></div></div></section>
        <section className="surface budget-summary-card"><SectionHeading label="CASH POSITION" title="当前现金状态" description={`共记录 ${summary.entryCount} 笔现金流水。`} /><div className="cash-balance"><span>可用现金余额</span><strong>{formatMoney(summary.cashBalanceAmount)}</strong></div><div className="budget-mini-grid"><div className="budget-mini"><span>累计流入</span><strong className="positive-reference">{formatMoney(summary.totalInflowAmount)}</strong></div><div className="budget-mini"><span>累计流出</span><strong className="negative-reference">{formatMoney(summary.totalOutflowAmount)}</strong></div></div><div className="note-box"><ArrowUpRight size={14} style={{ verticalAlign: "-3px", marginRight: 5 }} />现金余额只代表已记录流水；建议金额还会受到单股预算和现金保留比例限制。</div><p className="muted-copy" style={{ marginTop: 18 }}>想记录实际交易结果？可以去 <button className="text-link" type="button" onClick={() => onNavigate("/portfolio")}>交易记录</button> 页面。</p></section>
      </div>
    </AppShell>
  )
}
