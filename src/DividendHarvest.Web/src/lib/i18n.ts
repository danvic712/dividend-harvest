import { createContext, createElement, useContext, useMemo, useState, type ReactNode } from "react"

import enCommon from "../../../../locales/en-US/common.json"
import enBudget from "../../../../locales/en-US/budget.json"
import enDividendStrategy from "../../../../locales/en-US/dividend-strategy.json"
import enPortfolio from "../../../../locales/en-US/portfolio.json"
import enSetup from "../../../../locales/en-US/setup.json"
import enStocks from "../../../../locales/en-US/stocks.json"
import zhCommon from "../../../../locales/zh-CN/common.json"
import zhBudget from "../../../../locales/zh-CN/budget.json"
import zhDividendStrategy from "../../../../locales/zh-CN/dividend-strategy.json"
import zhPortfolio from "../../../../locales/zh-CN/portfolio.json"
import zhSetup from "../../../../locales/zh-CN/setup.json"
import zhStocks from "../../../../locales/zh-CN/stocks.json"

export type Locale = "zh-CN" | "en-US"

type LocaleMessages = {
  common: typeof zhCommon
  budget: {
    ui: typeof zhBudget.ui
  }
  dividendStrategy: {
    ui: typeof zhDividendStrategy.ui
  }
  portfolio: {
    ui: typeof zhPortfolio.ui
  }
  setup: {
    ui: typeof zhSetup.ui
  }
  stocks: {
    ui: typeof zhStocks.ui
  }
}

const catalogs: Record<Locale, LocaleMessages> = {
  "zh-CN": {
    common: zhCommon,
    budget: { ui: zhBudget.ui },
    dividendStrategy: { ui: zhDividendStrategy.ui },
    portfolio: { ui: zhPortfolio.ui },
    setup: { ui: zhSetup.ui },
    stocks: { ui: zhStocks.ui },
  },
  "en-US": {
    common: enCommon,
    budget: { ui: enBudget.ui },
    dividendStrategy: { ui: enDividendStrategy.ui },
    portfolio: { ui: enPortfolio.ui },
    setup: { ui: enSetup.ui },
    stocks: { ui: enStocks.ui },
  },
}

export type OverviewCopy = LocaleMessages["dividendStrategy"]["ui"]["overview"]

type LocaleContextValue = {
  locale: Locale
  messages: LocaleMessages
  setLocale: (locale: Locale) => void
}

const LocaleContext = createContext<LocaleContextValue | undefined>(undefined)

function isLocale(value: string | null): value is Locale {
  return value === "zh-CN" || value === "en-US"
}

function getInitialLocale(): Locale {
  const storedLocale = typeof window !== "undefined" ? window.localStorage.getItem("dividend-harvest-locale") : null
  return isLocale(storedLocale) ? storedLocale : "zh-CN"
}

export function LocaleProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>(getInitialLocale)
  const messages = catalogs[locale]

  function setLocale(nextLocale: Locale) {
    window.localStorage.setItem("dividend-harvest-locale", nextLocale)
    setLocaleState(nextLocale)
  }

  const value = useMemo(() => ({ locale, messages, setLocale }), [locale, messages])

  return createElement(LocaleContext.Provider, { value }, children)
}

export function useLocale() {
  const context = useContext(LocaleContext)
  if (!context) {
    throw new Error("useLocale must be used within a LocaleProvider")
  }

  return context
}

export function interpolate(template: string, values: Record<string, string | number>) {
  return template.replace(/\{(\w+)\}/g, (_, key: string) => String(values[key] ?? `{${key}}`))
}

export const copy = catalogs["zh-CN"].common.ui
