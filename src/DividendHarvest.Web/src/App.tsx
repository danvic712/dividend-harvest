import { useCallback, useEffect, useState } from "react"

import { ErrorState, LoadingState } from "@/components/async-state"
import { ThemeProvider } from "@/components/theme-provider"
import { getApiErrorMessage } from "@/lib/api-errors"
import { getSetupStatus } from "@/features/setup/setup.api"
import type { SetupResult, SetupStatus } from "@/lib/api-types"
import { SetupPage } from "@/features/setup/SetupPage"
import { RecommendationsPage } from "@/features/recommendations/RecommendationsPage"
import { StocksPage } from "@/features/stocks/StocksPage"
import { BudgetPage } from "@/features/budget/BudgetPage"
import { PortfolioPage } from "@/features/portfolio/PortfolioPage"
import { SettingsPage } from "@/features/settings/SettingsPage"

function currentPath() {
  return window.location.pathname
}

export default function App() {
  const [path, setPath] = useState(currentPath)
  const [setupStatus, setSetupStatus] = useState<SetupStatus | null>(null)
  const [setupNotice, setSetupNotice] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const navigate = useCallback((nextPath: string) => {
    const [pathname] = nextPath.split("?")
    window.history.pushState({}, "", nextPath)
    setPath(pathname || "/overview")
  }, [])

  const checkSetup = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const status = await getSetupStatus()
      setSetupStatus(status)
      if (!status.isComplete && currentPath() !== "/setup") {
        navigate("/setup")
      }
      if (status.isComplete && currentPath() === "/setup") {
        navigate("/overview")
      }
    } catch (statusError) {
      setError(getApiErrorMessage(statusError, "无法连接到后端服务，请确认应用正在运行。"))
    } finally {
      setLoading(false)
    }
  }, [navigate])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => { void checkSetup() }, 0)
    const handlePopState = () => setPath(currentPath())
    window.addEventListener("popstate", handlePopState)
    return () => {
      window.clearTimeout(timeoutId)
      window.removeEventListener("popstate", handlePopState)
    }
  }, [checkSetup])

  function renderPage() {
    if (path === "/setup") return <SetupPage onComplete={(result: SetupResult) => { setSetupStatus({ isComplete: true, missingRequirements: [] }); setSetupNotice(result.stockDataSyncScheduled ? "组合已建立，股票资料正在后台同步。" : "组合已建立；本次后台同步未入队，系统会在下一次手动或交易日同步时重试。"); navigate("/overview") }} />
    if (path === "/stocks") return <StocksPage onNavigate={navigate} />
    if (path === "/budget") return <BudgetPage onNavigate={navigate} />
    if (path === "/portfolio") return <PortfolioPage onNavigate={navigate} />
    if (path === "/settings") return <SettingsPage onNavigate={navigate} />
    return <RecommendationsPage onNavigate={navigate} notice={setupNotice} />
  }

  return <ThemeProvider>{loading ? <div className="page-wrap"><LoadingState label="正在连接本地组合…" /></div> : error ? <div className="page-wrap"><ErrorState message={error} onRetry={() => void checkSetup()} /></div> : setupStatus?.isComplete || path === "/setup" ? renderPage() : <div className="page-wrap"><LoadingState label="正在准备首次设置…" /></div>}</ThemeProvider>
}
