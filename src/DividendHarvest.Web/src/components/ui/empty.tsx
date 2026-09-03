import * as React from "react"

import { cn } from "@/lib/utils"

function Empty({ className, ...props }: React.ComponentProps<"div">) {
  return <div data-slot="empty" className={cn("flex min-h-48 flex-col items-center justify-center gap-3 rounded-xl border border-dashed border-border p-6 text-center", className)} {...props} />
}

function EmptyTitle({ className, ...props }: React.ComponentProps<"div">) {
  return <div data-slot="empty-title" className={cn("font-heading text-base font-medium", className)} {...props} />
}

function EmptyDescription({ className, ...props }: React.ComponentProps<"p">) {
  return <p data-slot="empty-description" className={cn("max-w-md text-sm leading-6 text-muted-foreground", className)} {...props} />
}

export { Empty, EmptyTitle, EmptyDescription }
