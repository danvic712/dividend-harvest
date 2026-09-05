import { ChevronRight } from "lucide-react"

import { SiteFooter } from "@/components/site-footer"
import { SiteHeader } from "@/components/site-header"
import { findSiteNavigationItem } from "@/components/site-navigation"
import { useLocale } from "@/lib/i18n"
import { cn } from "@/lib/utils"

type PageFrameProps = {
  children: React.ReactNode
  currentPath: string
  onNavigate: (path: string) => void
  lastUpdated?: string | null
  dataState?: "synced" | "pending" | "unknown"
}

export function PageFrame({ children, currentPath, onNavigate, lastUpdated, dataState = "unknown" }: PageFrameProps) {
  const { messages } = useLocale()
  const pageLabel = findSiteNavigationItem(currentPath)

  return (
    <div className={cn("app-frame", currentPath === "/overview" && "app-frame-overview")}>
      <SiteHeader currentPath={currentPath} onNavigate={onNavigate} />
      <main className="page-wrap">
        {currentPath !== "/overview" && <div className="page-crumb"><span>{messages.common.ui.appName}</span><ChevronRight size={13} /><span className="page-crumb-current">{pageLabel ? messages.common.ui.nav[pageLabel.key] : messages.common.ui.nav.settings}</span></div>}
        {children}
      </main>
      <SiteFooter lastUpdated={lastUpdated} dataState={dataState} />
    </div>
  )
}
