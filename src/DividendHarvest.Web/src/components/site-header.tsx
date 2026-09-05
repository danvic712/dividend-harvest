import { Languages, Monitor, Moon, Settings2, Sun, SunMoon } from "lucide-react"
import { useState } from "react"

import { useTheme, type Theme } from "@/components/theme-provider"
import { Button } from "@/components/ui/button"
import { Popover, PopoverContent, PopoverDescription, PopoverHeader, PopoverTitle, PopoverTrigger } from "@/components/ui/popover"
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
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
  const themeOptions = [
    { value: "light" as Theme, label: messages.common.ui.theme.light, icon: Sun },
    { value: "dark" as Theme, label: messages.common.ui.theme.dark, icon: Moon },
    { value: "system" as Theme, label: messages.common.ui.theme.system, icon: Monitor },
  ]

  const handleLocaleChange = (value: string | null) => {
    if (value !== "zh-CN" && value !== "en-US") return
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
          <div className="theme-control" role="group" aria-label={messages.common.ui.theme.label}>
            <SunMoon className="preference-icon" aria-hidden="true" />
            {themeOptions.map((option) => { const Icon = option.icon; return <Button key={option.value} variant="ghost" size="sm" type="button" aria-pressed={theme === option.value} className={cn(theme === option.value && "theme-control-active")} onClick={() => setTheme(option.value)}><Icon data-icon="inline-start" className="theme-option-icon" aria-hidden="true" /><span className="theme-option-label">{option.label}</span></Button> })}
          </div>
          <div className="locale-control" aria-label={messages.common.ui.language.label}>
            <Select value={locale} onValueChange={handleLocaleChange}>
              <SelectTrigger size="sm" className="locale-select-trigger" aria-label={messages.common.ui.language.select}>
                <Languages className="locale-select-leading" aria-hidden="true" />
                <SelectValue>{locale === "zh-CN" ? messages.common.ui.language.zhCN : messages.common.ui.language.enUS}</SelectValue>
              </SelectTrigger>
              <SelectContent align="end"><SelectGroup><SelectItem value="zh-CN">{messages.common.ui.language.zhCN}</SelectItem><SelectItem value="en-US">{messages.common.ui.language.enUS}</SelectItem></SelectGroup></SelectContent>
            </Select>
          </div>
          <div className="mobile-preferences">
            <Popover open={preferencesOpen} onOpenChange={setPreferencesOpen}>
              <PopoverTrigger render={<Button size="icon" variant="ghost" className="mobile-preferences-trigger" aria-label={`${messages.common.ui.theme.label} / ${messages.common.ui.language.label}`}><SunMoon aria-hidden="true" /></Button>} />
              <PopoverContent align="end" className="mobile-preferences-panel">
                <PopoverHeader className="mobile-preferences-header">
                  <PopoverTitle>{messages.common.ui.preferences.label}</PopoverTitle>
                  <PopoverDescription>{messages.common.ui.preferences.description}</PopoverDescription>
                </PopoverHeader>
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
                          }}
                        >
                          <Icon data-icon="inline-start" aria-hidden="true" />
                          <span>{option.label}</span>
                        </Button>
                      )
                    })}
                  </div>
                </div>
                <div className="mobile-preferences-section">
                  <span>{messages.common.ui.language.label}</span>
                  <Select value={locale} onValueChange={handleLocaleChange}>
                    <SelectTrigger size="sm" className="mobile-preferences-locale" aria-label={messages.common.ui.language.select}>
                      <Languages className="locale-select-leading" aria-hidden="true" />
                      <SelectValue>{locale === "zh-CN" ? messages.common.ui.language.zhCN : messages.common.ui.language.enUS}</SelectValue>
                    </SelectTrigger>
                    <SelectContent align="end"><SelectGroup><SelectItem value="zh-CN">{messages.common.ui.language.zhCN}</SelectItem><SelectItem value="en-US">{messages.common.ui.language.enUS}</SelectItem></SelectGroup></SelectContent>
                  </Select>
                </div>
              </PopoverContent>
            </Popover>
          </div>
          <Button size="icon" variant="ghost" className="header-settings-button" aria-label={messages.common.ui.settings} onClick={() => onNavigate("/settings")}>
            <Settings2 data-icon="inline-start" />
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
