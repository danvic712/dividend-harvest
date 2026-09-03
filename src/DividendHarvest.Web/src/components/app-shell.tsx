import { BookOpen, CalendarClock, ChevronRight, CircleDollarSign, Languages, LayoutDashboard, Settings2, WalletCards } from "lucide-react"
import { useState } from "react"

import { useTheme, type Theme } from "@/components/theme-provider"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { copy } from "@/lib/i18n"
import { cn } from "@/lib/utils"

type AppShellProps = {
  children: React.ReactNode
  currentPath: string
  onNavigate: (path: string) => void
  lastUpdated?: string | null
  dataState?: "synced" | "pending" | "unknown"
}

const navigation = [
  { path: "/overview", label: copy.nav.overview, icon: LayoutDashboard },
  { path: "/stocks", label: copy.nav.stocks, icon: BookOpen },
  { path: "/budget", label: copy.nav.budget, icon: CircleDollarSign },
  { path: "/portfolio", label: copy.nav.portfolio, icon: WalletCards },
]

export function AppShell({ children, currentPath, onNavigate, lastUpdated, dataState = "unknown" }: AppShellProps) {
  const { theme, setTheme } = useTheme()
  const [locale, setLocale] = useState(() => localStorage.getItem("dividend-harvest-locale") ?? "zh-CN")
  const dataLabel = dataState === "synced" ? "FTShare 已同步" : dataState === "pending" ? "FTShare 后台同步中" : "FTShare · 按交易日更新"

  function updateLocale(nextLocale: string) {
    localStorage.setItem("dividend-harvest-locale", nextLocale)
    setLocale(nextLocale)
  }
  return (
    <div className="app-frame">
      <header className="topbar d-header">
        <button className="brand-lockup" type="button" onClick={() => onNavigate("/overview")} aria-label="返回今日决策">
          <span className="brand-mark">D</span>
          <span>
            <span className="brand-name">Dividend / Harvest</span>
            <span className="brand-caption">股息收割 · 长期积累</span>
          </span>
        </button>

        <div className="d-header-center"><span className="d-live-dot" />每日交易参考 <span>·</span> {lastUpdated ?? "按交易日更新"}</div>

        <nav className="main-nav" aria-label="主导航">
          {navigation.map(({ path, label, icon: Icon }) => (
            <button
              key={path}
              className={cn("nav-item", currentPath === path && "nav-item-active")}
              type="button"
              onClick={() => onNavigate(path)}
              aria-current={currentPath === path ? "page" : undefined}
            >
              <Icon data-icon="inline-start" />
              {label}
            </button>
          ))}
        </nav>

        <div className="topbar-actions d-header-actions">
          <div className={`data-stamp data-stamp-${dataState}`}><span className="data-dot" /><span>{dataLabel}</span><span className="data-divider">·</span><span>{lastUpdated ?? "每交易日"}</span></div>
          <div className="theme-control" aria-label="主题设置">
            {([{"value": "light", "label": "日间"}, {"value": "dark", "label": "夜间"}, {"value": "system", "label": "系统"}] as Array<{ value: Theme; label: string }>).map((option) => <button key={option.value} type="button" aria-pressed={theme === option.value} className={theme === option.value ? "theme-control-active" : ""} onClick={() => setTheme(option.value)}>{option.label}</button>)}
          </div>
          <div className="locale-control" aria-label="语言设置"><Languages size={14} /><select value={locale} onChange={(event) => updateLocale(event.target.value)} aria-label="选择语言"><option value="zh-CN">中文</option><option value="en-US">EN</option></select></div>
          <Button size="icon" variant="ghost" aria-label="打开设置" onClick={() => onNavigate("/settings")}>
            <Settings2 />
          </Button>
        </div>
      </header>

      <div className="mobile-nav" aria-label="移动端主导航">
        {navigation.map(({ path, label, icon: Icon }) => (
          <button key={path} className={cn("mobile-nav-item", currentPath === path && "mobile-nav-item-active")} type="button" onClick={() => onNavigate(path)}>
            <Icon />
            <span>{label}</span>
          </button>
        ))}
      </div>

      <main className="page-wrap">
        <div className="page-crumb"><span>{copy.appName}</span><ChevronRight size={13} /><span className="page-crumb-current">{navigation.find((item) => item.path === currentPath)?.label ?? "设置"}</span></div>
        {children}
      </main>

      <footer className="app-footer">
        <div><span className="footer-rule" />{copy.disclaimer}</div>
        <div className="footer-meta"><CalendarClock size={14} />{lastUpdated ? `数据更新时间 ${lastUpdated}` : "数据按交易日更新"}<Badge variant="outline">仅 A 股</Badge><Badge variant="outline">仅供研究</Badge></div>
      </footer>
    </div>
  )
}

export function PageTitle({ eyebrow, title, description, actions }: { eyebrow: string; title: string; description?: string; actions?: React.ReactNode }) {
  return (
    <div className="page-title-row">
      <div>
        <p className="eyebrow">{eyebrow}</p>
        <h1 className="page-title">{title}</h1>
        {description && <p className="page-description">{description}</p>}
      </div>
      {actions && <div className="page-actions">{actions}</div>}
    </div>
  )
}

export function SectionHeading({ label, title, description }: { label?: string; title: string; description?: string }) {
  return (
    <div className="section-heading">
      {label && <p className="eyebrow">{label}</p>}
      <h2>{title}</h2>
      {description && <p>{description}</p>}
    </div>
  )
}
