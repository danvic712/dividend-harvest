import { Skeleton } from "@/components/ui/skeleton"

export function StockDetailSkeleton({ label }: { label: string }) {
  return (
    <div className="stock-detail-skeleton" role="status" aria-busy="true" aria-label={label}>
      <div className="stock-detail-skeleton-header">
        <div className="stock-detail-skeleton-identity">
          <Skeleton className="stock-detail-skeleton-avatar" />
          <div className="stock-detail-skeleton-copy">
            <Skeleton className="stock-detail-skeleton-name" />
            <Skeleton className="stock-detail-skeleton-code" />
          </div>
        </div>
        <Skeleton className="stock-detail-skeleton-badge" />
      </div>

      <div className="stock-detail-skeleton-metrics">
        {Array.from({ length: 6 }, (_, index) => <Skeleton className="stock-detail-skeleton-metric" key={index} />)}
      </div>

      <Skeleton className="stock-detail-skeleton-divider" />
      <div className="stock-detail-skeleton-section">
        <div className="stock-detail-skeleton-section-copy">
          <Skeleton className="stock-detail-skeleton-kicker" />
          <Skeleton className="stock-detail-skeleton-title" />
          <Skeleton className="stock-detail-skeleton-description" />
        </div>
        <Skeleton className="stock-detail-skeleton-price" />
      </div>
      <Skeleton className="stock-detail-skeleton-ladder" />
    </div>
  )
}
