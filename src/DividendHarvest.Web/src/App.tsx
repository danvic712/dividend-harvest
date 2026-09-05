import { useCallback, useEffect, useRef, useState } from "react"

import { ErrorState, LoadingState } from "@/components/async-state"
import { ScrollToTop } from "@/components/scroll-to-top"
import { ThemeProvider } from "@/components/theme-provider"
import { getApiErrorMessage } from "@/lib/api-errors"
import { LocaleProvider, useLocale } from "@/lib/i18n"
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
  return <LocaleProvider><AppContent /></LocaleProvider>
}

function AppContent() {
  const { locale, messages } = useLocale()
  const setupErrorRef = useRef(messages.common.application_error_unknown.detail)
  const [path, setPath] = useState(currentPath)
  const [portfolioStockKey, setPortfolioStockKey] = useState(() => {
    const query = new URLSearchParams(window.location.search)
    const queryStockKey = query.get("stock") ?? (query.get("code") && query.get("exchange") ? `${query.get("code")}:${query.get("exchange")}` : null)
    return queryStockKey ?? window.sessionStorage.getItem("dividend-harvest-portfolio-stock") ?? ""
  })
  const [setupStatus, setSetupStatus] = useState<SetupStatus | null>(null)
  const [setupNotice, setSetupNotice] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const navigate = useCallback((nextPath: string) => {
    const [pathname] = nextPath.split("?")
    window.history.pushState({}, "", nextPath)
    setPath(pathname || "/overview")
  }, [])

  useEffect(() => {
    document.title = messages.common.ui.pageTitle
    document.documentElement.lang = locale
  }, [locale, messages.common.ui.pageTitle])

  useEffect(() => {
    setupErrorRef.current = messages.common.application_error_unknown.detail
  }, [messages.common.application_error_unknown.detail])

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
      setError(getApiErrorMessage(statusError, setupErrorRef.current))
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
    if (path === "/setup") return <SetupPage onComplete={(result: SetupResult) => { setSetupStatus({ isComplete: true, missingRequirements: [] }); setSetupNotice(result.stockDataSyncScheduled ? messages.common.ui.states.setupCompleteSync : messages.common.ui.states.setupCompleteDeferred); navigate("/overview") }} />
    if (path === "/stocks") return <StocksPage onNavigate={navigate} />
    if (path === "/budget") return <BudgetPage onNavigate={navigate} />
    if (path === "/portfolio") return <PortfolioPage onNavigate={navigate} selectedStockKey={portfolioStockKey} onSelectedStockKeyChange={setPortfolioStockKey} />
    if (path === "/settings") return <SettingsPage onNavigate={navigate} />
    return <RecommendationsPage onNavigate={navigate} notice={setupNotice} />
  }

  return (
    <ThemeProvider>
      {loading ? <div className="page-wrap"><LoadingState label={messages.common.ui.states.connecting} /></div> : error ? <div className="page-wrap"><ErrorState message={error} onRetry={() => void checkSetup()} /></div> : setupStatus?.isComplete || path === "/setup" ? renderPage() : <div className="page-wrap"><LoadingState label={messages.common.ui.states.preparingSetup} /></div>}
      <ScrollToTop />
    </ThemeProvider>
  )
}
