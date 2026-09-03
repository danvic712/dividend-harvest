import { AlertCircle, Inbox, LoaderCircle } from "lucide-react"

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Empty, EmptyDescription, EmptyTitle } from "@/components/ui/empty"
import { Button } from "@/components/ui/button"

export function LoadingState({ label = "正在读取数据…" }: { label?: string }) {
  return <div className="loading-state"><LoaderCircle className="spin" size={18} /><span>{label}</span></div>
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
