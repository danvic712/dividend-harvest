import { AlertCircle, BookOpen, Inbox } from "lucide-react"

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Empty, EmptyDescription, EmptyTitle } from "@/components/ui/empty"
import { Button } from "@/components/ui/button"
import { useLocale } from "@/lib/i18n"

export function LoadingState({ label }: { label?: string }) {
  const { messages } = useLocale()
  const loadingLabel = label ?? messages.common.ui.states.readingData

  return (
    <div className="loading-state" role="status" aria-live="polite" aria-busy="true">
      <div className="loading-state-card">
        <div className="loading-state-mark" aria-hidden="true">
          <BookOpen size={21} />
          <span />
        </div>
        <div className="loading-state-copy">
          <span className="loading-state-kicker">{messages.common.ui.brandName}</span>
          <span className="loading-state-label">{loadingLabel}</span>
          <span className="loading-state-dots" aria-hidden="true"><i /><i /><i /></span>
        </div>
      </div>
      <div className="loading-state-progress" aria-hidden="true"><span /></div>
    </div>
  )
}

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <Alert variant="destructive">
      <AlertCircle size={18} />
      <AlertTitle>数据读取失败</AlertTitle>
      <AlertDescription>{message}</AlertDescription>
      {onRetry && <Button className="alert-retry" size="sm" variant="outline" onClick={onRetry}>重新读取</Button>}
    </Alert>
  )
}

export function EmptyState({ title, description, action }: { title: string; description: string; action?: React.ReactNode }) {
  return <Empty><Inbox size={22} className="empty-icon" /><EmptyTitle>{title}</EmptyTitle><EmptyDescription>{description}</EmptyDescription>{action}</Empty>
}
