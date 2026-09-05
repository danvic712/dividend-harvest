import { createContext, createElement, useContext, useMemo, useState, type ReactNode } from "react"

import enCommon from "../../../../locales/en-US/common.json"
import enDividendStrategy from "../../../../locales/en-US/dividend-strategy.json"
import zhCommon from "../../../../locales/zh-CN/common.json"
import zhDividendStrategy from "../../../../locales/zh-CN/dividend-strategy.json"

export type Locale = "zh-CN" | "en-US"

type LocaleMessages = {
  common: typeof zhCommon
  dividendStrategy: {
    ui: typeof zhDividendStrategy.ui
  }
}

const catalogs: Record<Locale, LocaleMessages> = {
  "zh-CN": {
    common: zhCommon,
    dividendStrategy: { ui: zhDividendStrategy.ui },
  },
  "en-US": {
    common: enCommon,
    dividendStrategy: { ui: enDividendStrategy.ui },
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
