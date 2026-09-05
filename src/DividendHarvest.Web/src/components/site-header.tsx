import { Languages, Monitor, Moon, Settings2, Sun, SunMoon } from "lucide-react"
import { useEffect, useRef, useState } from "react"

import { useTheme, type Theme } from "@/components/theme-provider"
import { Button } from "@/components/ui/button"
import { useLocale } from "@/lib/i18n"
import { cn } from "@/lib/utils"
import { siteNavigation } from "@/components/site-navigation"

type SiteHeaderProps = {
  currentPath: string
  onNavigate: (path: string) => void
}

export function SiteHeader({ currentPath, onNavigate }: SiteHeaderProps) {
  const { theme, setTheme } = useTheme()
  const { locale, setLocale, messages } = useLocale()
  const [preferencesOpen, setPreferencesOpen] = useState(false)
  const preferencesRef = useRef<HTMLDivElement>(null)
  const themeOptions = [
    { value: "light" as Theme, label: messages.common.ui.theme.light, icon: Sun },
    { value: "dark" as Theme, label: messages.common.ui.theme.dark, icon: Moon },
    { value: "system" as Theme, label: messages.common.ui.theme.system, icon: Monitor },
  ]

  useEffect(() => {
    if (!preferencesOpen) {
      return undefined
    }

    const handlePointerDown = (event: PointerEvent) => {
      if (preferencesRef.current && !preferencesRef.current.contains(event.target as Node)) {
        setPreferencesOpen(false)
      }
    }
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setPreferencesOpen(false)
      }
    }

    document.addEventListener("pointerdown", handlePointerDown)
    document.addEventListener("keydown", handleKeyDown)
    return () => {
      document.removeEventListener("pointerdown", handlePointerDown)
      document.removeEventListener("keydown", handleKeyDown)
    }
  }, [preferencesOpen])

  const handleMobileLocaleChange = (value: typeof locale) => {
    setLocale(value)
    setPreferencesOpen(false)
  }

  return (
    <>
      <header className="topbar d-header">
        <Button variant="ghost" className="brand-lockup" type="button" onClick={() => onNavigate("/overview")} aria-label={messages.common.ui.backToOverview}>
          <span className="brand-mark">D</span>
          <span className="brand-copy">
            <span className="brand-name">{messages.common.ui.brandName}</span>
            <span className="brand-caption">{messages.common.ui.brandCaption}</span>
          </span>
        </Button>

        <nav className="main-nav" aria-label={messages.common.ui.mainNavigation}>
          {siteNavigation.map(({ path, key, icon: Icon }) => (
            <Button
              key={path}
              variant="ghost"
              className={cn("nav-item", currentPath === path && "nav-item-active")}
              type="button"
              onClick={() => onNavigate(path)}
              aria-current={currentPath === path ? "page" : undefined}
            >
              <Icon data-icon="inline-start" />
              {messages.common.ui.nav[key]}
            </Button>
          ))}
        </nav>

        <div className="topbar-actions d-header-actions">
          <div className="theme-control" aria-label={messages.common.ui.theme.label}>
            <SunMoon className="preference-icon" aria-hidden="true" />
            {themeOptions.map((option) => { const Icon = option.icon; return <Button key={option.value} variant="ghost" size="sm" type="button" aria-pressed={theme === option.value} className={theme === option.value ? "theme-control-active" : ""} onClick={() => setTheme(option.value)}><Icon className="theme-option-icon" aria-hidden="true" /><span className="theme-option-label">{option.label}</span></Button> })}
          </div>
          <div className="locale-control" aria-label={messages.common.ui.language.label}><Languages size={14} /><select value={locale} onChange={(event) => setLocale(event.target.value as typeof locale)} aria-label={messages.common.ui.language.select}><option value="zh-CN">{messages.common.ui.language.zhCN}</option><option value="en-US">{messages.common.ui.language.enUS}</option></select></div>
          <div className="mobile-preferences" ref={preferencesRef}>
            <Button
              size="icon"
              variant="ghost"
              className="mobile-preferences-trigger"
              type="button"
              aria-label={`${messages.common.ui.theme.label} / ${messages.common.ui.language.label}`}
              aria-expanded={preferencesOpen}
              aria-controls="mobile-preferences-panel"
              onClick={() => setPreferencesOpen((open) => !open)}
            >
              <SunMoon />
            </Button>
            {preferencesOpen ? (
              <div id="mobile-preferences-panel" className="mobile-preferences-panel" role="dialog" aria-label={`${messages.common.ui.theme.label} / ${messages.common.ui.language.label}`}>
                <div className="mobile-preferences-section">
                  <span>{messages.common.ui.theme.label}</span>
                  <div className="mobile-theme-options">
                    {themeOptions.map((option) => {
                      const Icon = option.icon
                      return (
                        <Button
                          key={option.value}
                          variant="ghost"
                          size="sm"
                          type="button"
                          aria-pressed={theme === option.value}
                          className={cn("mobile-theme-option", theme === option.value && "mobile-theme-option-active")}
                          onClick={() => {
                            setTheme(option.value)
                            setPreferencesOpen(false)
                          }}
                        >
                          <Icon aria-hidden="true" />
                          <span>{option.label}</span>
                        </Button>
                      )
                    })}
                  </div>
                </div>
                <div className="mobile-preferences-section">
                  <span>{messages.common.ui.language.label}</span>
                  <label className="mobile-preferences-locale">
                    <Languages size={15} aria-hidden="true" />
                    <select value={locale} onChange={(event) => handleMobileLocaleChange(event.target.value as typeof locale)} aria-label={messages.common.ui.language.select}>
                      <option value="zh-CN">{messages.common.ui.language.zhCN}</option>
                      <option value="en-US">{messages.common.ui.language.enUS}</option>
                    </select>
                  </label>
                </div>
              </div>
            ) : null}
          </div>
          <Button size="icon" variant="ghost" className="header-settings-button" aria-label={messages.common.ui.settings} onClick={() => onNavigate("/settings")}>
            <Settings2 />
          </Button>
        </div>
      </header>

      <nav className="mobile-nav" aria-label={messages.common.ui.mobileNavigation}>
        {siteNavigation.map(({ path, key, icon: Icon }) => (
          <Button key={path} variant="ghost" className={cn("mobile-nav-item", currentPath === path && "mobile-nav-item-active")} type="button" onClick={() => onNavigate(path)} aria-current={currentPath === path ? "page" : undefined}>
            <Icon />
            <span>{messages.common.ui.nav[key]}</span>
          </Button>
        ))}
      </nav>
    </>
  )
}
