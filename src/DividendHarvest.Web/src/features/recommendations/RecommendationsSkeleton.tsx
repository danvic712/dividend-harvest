import { BookOpen } from "lucide-react"

import { Skeleton } from "@/components/ui/skeleton"
import { useLocale } from "@/lib/i18n"

export function RecommendationsSkeleton({ label }: { label: string }) {
  const { messages } = useLocale()

  return (
    <div className="overview-skeleton" role="status" aria-busy="true" aria-label={label}>
      <div className="overview-skeleton-loading">
        <div className="overview-skeleton-loading-mark" aria-hidden="true">
          <BookOpen size={20} />
          <span />
        </div>
        <div className="overview-skeleton-loading-copy">
          <span>{messages.common.ui.brandName}</span>
          <strong>{label}</strong>
          <span className="overview-skeleton-loading-dots" aria-hidden="true"><i /><i /><i /></span>
        </div>
      </div>

      <section className="overview-skeleton-notebook">
        <Skeleton className="overview-skeleton-notebook-index" />
        <div className="overview-skeleton-notebook-copy">
          <Skeleton className="overview-skeleton-notebook-kicker" />
          <Skeleton className="overview-skeleton-notebook-title" />
          <Skeleton className="overview-skeleton-notebook-description" />
        </div>
        <div className="overview-skeleton-notebook-aside">
          <Skeleton className="overview-skeleton-notebook-status" />
          <div className="overview-skeleton-notebook-tools">
            <Skeleton className="overview-skeleton-button overview-skeleton-button-short" />
            <Skeleton className="overview-skeleton-button overview-skeleton-button-short" />
          </div>
        </div>
      </section>

      <section className="overview-skeleton-watchlist">
        <div className="overview-skeleton-heading">
          <Skeleton className="overview-skeleton-kicker" />
          <Skeleton className="overview-skeleton-title" />
        </div>
        <Skeleton className="overview-skeleton-description" />
        <div className="overview-skeleton-stocks">
          <Skeleton className="overview-skeleton-stock" />
          <Skeleton className="overview-skeleton-stock overview-skeleton-stock-optional" />
        </div>
      </section>

      <section className="overview-skeleton-hero">
        <div className="overview-skeleton-story">
          <Skeleton className="overview-skeleton-kicker" />
          <Skeleton className="overview-skeleton-identity" />
          <Skeleton className="overview-skeleton-hero-title" />
          <Skeleton className="overview-skeleton-copy" />
          <Skeleton className="overview-skeleton-copy overview-skeleton-copy-short" />
          <div className="overview-skeleton-action-row">
            <Skeleton className="overview-skeleton-action" />
            <Skeleton className="overview-skeleton-button overview-skeleton-button-primary" />
          </div>
        </div>
        <div className="overview-skeleton-signal">
          <Skeleton className="overview-skeleton-card-kicker" />
          <Skeleton className="overview-skeleton-card-title" />
          <Skeleton className="overview-skeleton-card-copy" />
          <div className="overview-skeleton-readouts"><Skeleton /><Skeleton /></div>
          <Skeleton className="overview-skeleton-reasons" />
          <Skeleton className="overview-skeleton-foot" />
        </div>
      </section>

      <section className="overview-skeleton-lower">
        <Skeleton className="overview-skeleton-lower-main" />
        <Skeleton className="overview-skeleton-lower-side" />
      </section>
    </div>
  )
}
