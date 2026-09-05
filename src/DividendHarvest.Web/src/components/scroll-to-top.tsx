import { ArrowUp } from "lucide-react"
import { useEffect, useState } from "react"

import { Button } from "@/components/ui/button"
import { useLocale } from "@/lib/i18n"

const visibilityThreshold = 360

export function ScrollToTop() {
  const { messages } = useLocale()
  const [visible, setVisible] = useState(false)

  useEffect(() => {
    const updateVisibility = () => setVisible(window.scrollY > visibilityThreshold)

    updateVisibility()
    window.addEventListener("scroll", updateVisibility, { passive: true })
    return () => window.removeEventListener("scroll", updateVisibility)
  }, [])

  const scrollToTop = () => {
    const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches
    window.scrollTo({ top: 0, behavior: prefersReducedMotion ? "auto" : "smooth" })
  }

  return (
    <Button
      variant="ghost"
      size="icon"
      type="button"
      className={`scroll-to-top${visible ? " scroll-to-top-visible" : ""}`}
      aria-label={messages.common.ui.scrollToTop}
      aria-hidden={!visible}
      tabIndex={visible ? 0 : -1}
      onClick={scrollToTop}
    >
      <ArrowUp aria-hidden="true" />
    </Button>
  )
}
