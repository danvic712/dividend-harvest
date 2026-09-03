import * as React from "react"

export type Theme = "light" | "dark" | "system"

type ThemeProviderProps = {
  children: React.ReactNode
  defaultTheme?: Theme
  storageKey?: string
}

type ThemeProviderState = {
  theme: Theme
  setTheme: (theme: Theme) => void
}

const ThemeProviderContext = React.createContext<ThemeProviderState | undefined>(undefined)
const mediaQuery = "(prefers-color-scheme: dark)"

function isTheme(value: string | null): value is Theme {
  return value === "light" || value === "dark" || value === "system"
}

function resolveTheme(theme: Theme) {
  return theme === "system"
    ? window.matchMedia(mediaQuery).matches
      ? "dark"
      : "light"
    : theme
}

export function ThemeProvider({ children, defaultTheme = "system", storageKey = "dividend-harvest-theme" }: ThemeProviderProps) {
  const [theme, setTheme] = React.useState<Theme>(() => {
    const stored = localStorage.getItem(storageKey)
    return isTheme(stored) ? stored : defaultTheme
  })

  React.useEffect(() => {
    const applyTheme = () => {
      document.documentElement.classList.remove("light", "dark")
      document.documentElement.classList.add(resolveTheme(theme))
    }

    applyTheme()
    if (theme !== "system") {
      return undefined
    }

    const query = window.matchMedia(mediaQuery)
    query.addEventListener("change", applyTheme)
    return () => query.removeEventListener("change", applyTheme)
  }, [theme])

  const updateTheme = React.useCallback((nextTheme: Theme) => {
    localStorage.setItem(storageKey, nextTheme)
    setTheme(nextTheme)
  }, [storageKey])

  return (
    <ThemeProviderContext.Provider value={{ theme, setTheme: updateTheme }}>
      {children}
    </ThemeProviderContext.Provider>
  )
}

export function useTheme() {
  const context = React.useContext(ThemeProviderContext)
  if (!context) {
    throw new Error("useTheme must be used within a ThemeProvider")
  }

  return context
}
