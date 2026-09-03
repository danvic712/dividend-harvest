import { Plus, Trash2 } from "lucide-react"
import { useState } from "react"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { getApiErrorMessage } from "@/lib/api-errors"
import { initializeSetup } from "@/features/setup/setup.api"
import type { SetupResult, SetupStockRequest } from "@/lib/api-types"
import { copy } from "@/lib/i18n"
import "./setup.css"

function newStock(): SetupStockRequest {
  return { securityCode: "", exchangeCode: "SSE", initialHolding: null }
}

export function SetupPage({ onComplete }: { onComplete: (result: SetupResult) => void }) {
  const [portfolioName, setPortfolioName] = useState("我的股息组合")
  const [stocks, setStocks] = useState<SetupStockRequest[]>([newStock()])
  const [withHolding, setWithHolding] = useState<boolean[]>([false])
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  function updateStock(index: number, update: Partial<SetupStockRequest>) {
    setStocks((current) => current.map((stock, itemIndex) => itemIndex === index ? { ...stock, ...update } : stock))
  }

  function toggleHolding(index: number, checked: boolean) {
    setWithHolding((current) => current.map((value, itemIndex) => itemIndex === index ? checked : value))
    updateStock(index, { initialHolding: checked ? { heldShares: 0, coreShares: 0, targetShares: 0, averageCostPerShare: 0 } : null })
  }

  function updateHolding(index: number, key: "heldShares" | "coreShares" | "targetShares" | "averageCostPerShare", value: string) {
    const stock = stocks[index]
    if (!stock?.initialHolding) return
    updateStock(index, { initialHolding: { ...stock.initialHolding, [key]: Number(value) || 0 } })
  }

  function addStock() {
    setStocks((current) => [...current, newStock()])
    setWithHolding((current) => [...current, false])
  }

  function removeStock(index: number) {
    if (stocks.length === 1) return
    setStocks((current) => current.filter((_, itemIndex) => itemIndex !== index))
    setWithHolding((current) => current.filter((_, itemIndex) => itemIndex !== index))
  }

  async function submit() {
    setError(null)
    if (!portfolioName.trim() || stocks.some((stock) => !/^\d{6}$/.test(stock.securityCode.trim()))) {
      setError("请填写组合名称，并为每只股票填写 6 位 A 股代码。")
      return
    }

    setSubmitting(true)
    try {
      const result = await initializeSetup({ portfolioName: portfolioName.trim(), stocks: stocks.map((stock) => ({ ...stock, securityCode: stock.securityCode.trim(), exchangeCode: stock.exchangeCode.trim().toUpperCase() })) })
      onComplete(result)
    } catch (submitError) {
      setError(getApiErrorMessage(submitError, "初始化失败，请确认股票代码和交易所后重试。"))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="page-wrap">
      <div className="setup-page">
        <section className="setup-intro">
          <p className="eyebrow">FIRST RUN / PERSONAL WORKSPACE</p>
          <h1 className="setup-title">先把你的<br />组合放进来。</h1>
          <p>输入需要跟踪的 A 股股票。系统会从 FTShare 同步股票资料、价格、股息和财务快照，再根据每只股票的模型参数给出可解释的交易参考。</p>
          <div className="setup-callout"><span>01</span><span>{copy.disclaimer}</span></div>
        </section>

        <section className="surface setup-card">
          <div className="section-heading"><p className="eyebrow">WORKSPACE SETUP</p><h2>建立基础数据</h2><p>持仓信息可以先跳过，稍后在交易记录中补充。</p></div>
          {error && <Alert variant="destructive"><AlertTitle>无法保存设置</AlertTitle><AlertDescription>{error}</AlertDescription></Alert>}
          <div className="form-grid" style={{ marginTop: 22 }}>
            <div className="field form-grid-wide"><label htmlFor="portfolio-name">组合名称</label><Input id="portfolio-name" value={portfolioName} onChange={(event) => setPortfolioName(event.target.value)} placeholder="例如：长期股息组合" /></div>
            <div className="setup-stock-list">
              {stocks.map((stock, index) => (
                <div className="setup-stock-row" key={index}>
                  <div className="field"><label htmlFor={`stock-code-${index}`}>股票代码</label><Input id={`stock-code-${index}`} value={stock.securityCode} maxLength={6} inputMode="numeric" onChange={(event) => updateStock(index, { securityCode: event.target.value.replace(/\D/g, "") })} placeholder="600000" /></div>
                  <div className="field"><label htmlFor={`exchange-${index}`}>交易所</label><select id={`exchange-${index}`} value={stock.exchangeCode} onChange={(event) => updateStock(index, { exchangeCode: event.target.value })}><option value="SSE">SSE · 上海</option><option value="SZSE">SZSE · 深圳</option><option value="BSE">BSE · 北京</option></select></div>
                  <Button className="remove-stock" size="icon" variant="ghost" aria-label={`移除第 ${index + 1} 只股票`} disabled={stocks.length === 1} onClick={() => removeStock(index)}><Trash2 /></Button>
                  <label className="holding-toggle"><input type="checkbox" checked={withHolding[index] ?? false} onChange={(event) => toggleHolding(index, event.target.checked)} />已持有这只股票</label>
                  {stock.initialHolding && <div className="holding-fields form-grid-wide"><div className="field"><label htmlFor={`held-shares-${index}`}>持仓股数</label><Input id={`held-shares-${index}`} type="number" min="0" step="1" value={stock.initialHolding.heldShares} onChange={(event) => updateHolding(index, "heldShares", event.target.value)} /></div><div className="field"><label htmlFor={`core-shares-${index}`}>核心股数</label><Input id={`core-shares-${index}`} type="number" min="0" step="1" value={stock.initialHolding.coreShares} onChange={(event) => updateHolding(index, "coreShares", event.target.value)} /></div><div className="field"><label htmlFor={`target-shares-${index}`}>目标股数</label><Input id={`target-shares-${index}`} type="number" min="0" step="1" value={stock.initialHolding.targetShares} onChange={(event) => updateHolding(index, "targetShares", event.target.value)} /></div><div className="field"><label htmlFor={`average-cost-${index}`}>当前成本价</label><Input id={`average-cost-${index}`} type="number" min="0" step="0.01" value={stock.initialHolding.averageCostPerShare} onChange={(event) => updateHolding(index, "averageCostPerShare", event.target.value)} /></div></div>}
                </div>
              ))}
            </div>
            <Button className="form-grid-wide" variant="outline" onClick={addStock}><Plus data-icon="inline-start" />继续添加股票</Button>
            <div className="form-actions"><Button size="lg" onClick={submit} disabled={submitting}>{submitting ? "正在建立…" : "建立我的组合"}</Button><span className="form-message">组合会先立即建立；股票名称、行情和股息资料将在后台同步，不会阻塞进入系统。</span></div>
          </div>
          <p className="setup-footnote">股票代码仅支持 6 位数字；交易所用于区分同代码证券。该页面不会接收或保存 FTShare 密钥。</p>
        </section>
      </div>
    </main>
  )
}
