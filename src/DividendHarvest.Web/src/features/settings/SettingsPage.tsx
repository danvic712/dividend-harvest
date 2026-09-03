import { Save } from "lucide-react"
import { useCallback, useEffect, useState } from "react"

import { AppShell, PageTitle } from "@/components/app-shell"
import { EmptyState, ErrorState, LoadingState } from "@/components/async-state"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { getApiErrorMessage } from "@/lib/api-errors"
import { stockKey, today } from "@/lib/utils"
import type { SaveStockModelParametersRequest, StockWatchlistItem } from "@/lib/api-types"
import { getSettingsParameters, getSettingsStocks, updateSettingsParameters } from "@/features/settings/settings.api"
import "./settings.css"

type NumericKey = Exclude<keyof SaveStockModelParametersRequest, "securityCode" | "exchangeCode" | "modelVersion" | "effectiveFromDate">
const thresholdFields: Array<{ key: NumericKey; label: string }> = [
  { key: "strongBuyYieldThreshold", label: "强买入收益率阈值" },
  { key: "accumulationYieldThreshold", label: "分批加仓收益率阈值" },
  { key: "partialTrimYieldThreshold", label: "减仓候选收益率阈值" },
  { key: "aggressiveTrimYieldThreshold", label: "激进减仓收益率阈值" },
]
const ratioFields: Array<{ key: NumericKey; label: string }> = [
  { key: "strongBuyBudgetRatio", label: "强买入预算比例" },
  { key: "accumulateBudgetRatio", label: "分批加仓预算比例" },
  { key: "partialTrimRatio", label: "减仓候选比例" },
  { key: "aggressiveTrimRatio", label: "激进减仓比例" },
  { key: "maxSecurityWeight", label: "单只股票最大权重" },
  { key: "maxSectorWeight", label: "单一行业最大权重" },
  { key: "cashReserveRatio", label: "现金保留比例" },
  { key: "transactionFeeRatio", label: "交易费用比例" },
]
const limitFields: Array<{ key: NumericKey; label: string; step: string }> = [
  { key: "maxSingleTradeAmount", label: "单次交易金额上限", step: "0.01" },
  { key: "maxPeriodBudgetAmount", label: "单期预算上限", step: "0.01" },
  { key: "minimumTransactionFeeAmount", label: "最低交易费用", step: "0.01" },
  { key: "tradingLotSize", label: "交易单位（股）", step: "1" },
]

export function SettingsPage({ onNavigate }: { onNavigate: (path: string) => void }) {
  const query = new URLSearchParams(window.location.search)
  const initialKey = query.get("stock") ?? (query.get("code") && query.get("exchange") ? `${query.get("code")}:${query.get("exchange")}` : null)
  const [stocks, setStocks] = useState<StockWatchlistItem[]>([])
  const [selectedKey, setSelectedKey] = useState(initialKey ?? "")
  const [parameters, setParameters] = useState<SaveStockModelParametersRequest | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  const loadStocks = useCallback(async () => {
    try {
      const list = await getSettingsStocks()
      setStocks(list)
      setSelectedKey((current) => current && list.some((stock) => stockKey(stock) === current) ? current : list[0] ? stockKey(list[0]) : "")
    } catch (loadError) {
      setError(getApiErrorMessage(loadError, "股票列表暂时无法读取。"))
    } finally {
      setLoading(false)
    }
  }, [])

  const loadParameters = useCallback(async (signal?: AbortSignal) => {
    const stock = stocks.find((item) => stockKey(item) === selectedKey)
    if (!stock) return
    setParameters(null)
    setMessage(null)
    try {
      const result = await getSettingsParameters(stock.securityCode, stock.exchangeCode, signal)
      if (result && !signal?.aborted) {
        setParameters(result)
      }
    } catch (loadError) {
      if (!signal?.aborted) {
        setError(getApiErrorMessage(loadError, "模型参数暂时无法读取。"))
      }
    }
  }, [selectedKey, stocks])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => { void loadStocks() }, 0)
    return () => window.clearTimeout(timeoutId)
  }, [loadStocks])
  useEffect(() => {
    const controller = new AbortController()
    const timeoutId = window.setTimeout(() => { void loadParameters(controller.signal) }, 0)
    return () => {
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [loadParameters])

  function updateValue(key: keyof SaveStockModelParametersRequest, value: string) {
    setParameters((current) => current ? { ...current, [key]: key === "modelVersion" || key === "securityCode" || key === "exchangeCode" || key === "effectiveFromDate" ? value : Number(value) || 0 } : current)
  }

  async function save() {
    if (!parameters) return
    setSaving(true)
    setError(null)
    setMessage(null)
    try {
      await updateSettingsParameters(parameters)
      setMessage("模型参数已保存，下一次分析将读取新配置。")
    } catch (saveError) {
      setError(getApiErrorMessage(saveError, "模型参数保存失败，请检查阈值顺序和比例范围。"))
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <div className="page-wrap"><LoadingState label="正在读取模型设置…" /></div>
  if (error && !stocks.length) return <div className="page-wrap"><ErrorState message={error} onRetry={() => window.location.reload()} /></div>
  if (!stocks.length) return <div className="page-wrap"><EmptyState title="还没有股票资料" description="完成首次设置后，才能按股票配置模型参数。" action={<Button onClick={() => onNavigate("/setup")}>去建立组合</Button>} /></div>

  return (
    <AppShell currentPath="/settings" onNavigate={onNavigate}>
      <PageTitle eyebrow="SETTINGS / PER-STOCK MODEL" title="每只股票，自己的边界。" description="这些参数由后端验证并保存；比例使用 0 到 1 的小数表达，例如 0.25 表示 25%。" actions={<Button onClick={() => void save()} disabled={!parameters || saving}><Save data-icon="inline-start" />{saving ? "保存中…" : "保存参数"}</Button>} />
      {error && <Alert variant="destructive" style={{ marginBottom: 16 }}><AlertTitle>设置提醒</AlertTitle><AlertDescription>{error}</AlertDescription></Alert>}
      {message && <Alert style={{ marginBottom: 16 }}><AlertTitle>已完成</AlertTitle><AlertDescription>{message}</AlertDescription></Alert>}
      <section className="surface settings-card"><div className="settings-stock-tabs" role="tablist" aria-label="选择股票参数"><span className="eyebrow" style={{ alignSelf: "center", margin: "0 8px 0 0", whiteSpace: "nowrap" }}>SELECT STOCK</span>{stocks.map((stock) => <button key={stockKey(stock)} className={`settings-stock-tab ${selectedKey === stockKey(stock) ? "settings-stock-tab-active" : ""}`} type="button" onClick={() => setSelectedKey(stockKey(stock))} role="tab" aria-selected={selectedKey === stockKey(stock)}>{stock.securityCode} · {stock.exchangeCode} · {stock.securityName}</button>)}</div>{parameters ? <div className="form-grid"><div className="field"><label htmlFor="model-version">模型版本</label><Input id="model-version" value={parameters.modelVersion} onChange={(event) => updateValue("modelVersion", event.target.value)} /></div><div className="field"><label htmlFor="effective-date">生效日期</label><Input id="effective-date" type="date" value={parameters.effectiveFromDate || today()} onChange={(event) => updateValue("effectiveFromDate", event.target.value)} /></div><ParameterGroup title="收益率阈值" fields={thresholdFields} parameters={parameters} onChange={updateValue} percent /><ParameterGroup title="预算与仓位比例" fields={ratioFields} parameters={parameters} onChange={updateValue} percent /><ParameterGroup title="交易限制" fields={limitFields} parameters={parameters} onChange={updateValue} /></div> : <EmptyState title="这只股票还没有模型参数" description="请先通过后端初始化或导入模型参数；当前页面不会用默认值覆盖你的模型设置。" />}</section>
    </AppShell>
  )
}

function ParameterGroup({ title, fields, parameters, onChange, percent = false }: { title: string; fields: Array<{ key: NumericKey; label: string; step?: string }>; parameters: SaveStockModelParametersRequest; onChange: (key: keyof SaveStockModelParametersRequest, value: string) => void; percent?: boolean }) {
  return <div className="settings-group form-grid-wide"><h3>{title}</h3><div className="form-grid">{fields.map((field) => <div className="field" key={field.key}><label htmlFor={field.key}>{field.label}</label><Input id={field.key} type="number" min="0" max={percent ? "1" : undefined} step={field.step ?? "0.01"} value={String(parameters[field.key])} onChange={(event) => onChange(field.key, event.target.value)} /><span className="field-help">{percent ? "请输入 0 到 1 之间的小数。" : "由后端验证数值范围。"}</span></div>)}</div></div>
}
