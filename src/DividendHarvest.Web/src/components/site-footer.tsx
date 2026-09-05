import { CalendarClock } from "lucide-react"

import { Badge } from "@/components/ui/badge"
import { interpolate, useLocale } from "@/lib/i18n"

type SiteFooterProps = {
  lastUpdated?: string | null
  dataState: "synced" | "pending" | "unknown"
}

export function SiteFooter({ lastUpdated, dataState }: SiteFooterProps) {
  const { messages } = useLocale()
  const dataLabel = dataState === "synced" ? messages.common.ui.data.synced : dataState === "pending" ? messages.common.ui.data.pending : messages.common.ui.data.unknown
  const dataDate = lastUpdated ?? messages.common.ui.data.tradingDay

  return (
    <footer className="app-footer">
      <div><span className="footer-rule" />{messages.common.ui.disclaimer}</div>
      <div className="footer-meta">
        <div className={`footer-data-summary footer-data-summary-${dataState}`} role="status" aria-live="polite" aria-label={`${messages.common.ui.data.referenceTitle}，${dataLabel}，${dataDate}`}>
          <span className="footer-data-dot" aria-hidden="true" />
          <span className="footer-data-title">{messages.common.ui.data.referenceTitle}</span>
          <span className="footer-data-state">{dataLabel}</span>
          <span className="footer-data-date"><CalendarClock size={13} aria-hidden="true" />{lastUpdated ? interpolate(messages.common.ui.data.updatedAt, { updatedAt: lastUpdated }) : dataDate}</span>
        </div>
        <Badge variant="outline">{messages.common.ui.aShareOnly}</Badge>
        <Badge variant="outline">{messages.common.ui.researchOnly}</Badge>
      </div>
    </footer>
  )
}
