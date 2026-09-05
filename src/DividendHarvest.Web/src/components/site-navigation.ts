import type { LucideIcon } from "lucide-react"
import { BookOpen, CircleDollarSign, LayoutDashboard, WalletCards } from "lucide-react"

export type SiteNavigationKey = "overview" | "stocks" | "budget" | "portfolio"

export type SiteNavigationItem = {
  path: string
  key: SiteNavigationKey
  icon: LucideIcon
}

export const siteNavigation: readonly SiteNavigationItem[] = [
  { path: "/overview", key: "overview", icon: LayoutDashboard },
  { path: "/stocks", key: "stocks", icon: BookOpen },
  { path: "/budget", key: "budget", icon: CircleDollarSign },
  { path: "/portfolio", key: "portfolio", icon: WalletCards },
]

export function findSiteNavigationItem(path: string) {
  return siteNavigation.find((item) => item.path === path)
}
