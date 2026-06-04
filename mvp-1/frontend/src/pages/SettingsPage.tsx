import { Monitor, Moon, Sun, Contrast } from 'lucide-react'
import { useTheme, type Theme } from '../context/ThemeContext'

interface ThemeOption {
  value: Theme
  label: string
  icon: typeof Sun
  description: string
}

const THEME_OPTIONS: ThemeOption[] = [
  { value: 'light',  label: 'Jasny',      icon: Sun,      description: 'Zawsze jasne tło' },
  { value: 'dark',   label: 'Ciemny',     icon: Moon,     description: 'Zawsze ciemne tło' },
  { value: 'gray',   label: 'Szary',      icon: Contrast, description: 'Odcienie szarości' },
  { value: 'system', label: 'Systemowy',  icon: Monitor,  description: 'Dopasuj do ustawień systemu' },
]

export function SettingsPage() {
  const { theme, setTheme } = useTheme()

  return (
    <div className="p-6 max-w-2xl">
      <h1 className="text-2xl font-bold mb-6 text-gray-900 dark:text-gray-100">Ustawienia</h1>

      <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-6">
        <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-1">Schemat kolorów</h2>
        <p className="text-xs text-gray-500 dark:text-gray-400 mb-4">Wybierz motyw interfejsu aplikacji</p>
        <div className="grid grid-cols-2 gap-3">
          {THEME_OPTIONS.map(opt => {
            const Icon = opt.icon
            const isActive = theme === opt.value
            return (
              <button
                key={opt.value}
                onClick={() => setTheme(opt.value)}
                className={`flex flex-col items-center gap-2 p-4 rounded-xl border-2 transition-colors ${
                  isActive
                    ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20'
                    : 'border-gray-200 dark:border-gray-600 hover:border-gray-300 dark:hover:border-gray-500 hover:bg-gray-50 dark:hover:bg-gray-700/50'
                }`}
              >
                <Icon
                  size={22}
                  className={isActive ? 'text-blue-600 dark:text-blue-400' : 'text-gray-500 dark:text-gray-400'}
                />
                <span className={`text-sm font-medium ${isActive ? 'text-blue-700 dark:text-blue-400' : 'text-gray-700 dark:text-gray-200'}`}>
                  {opt.label}
                </span>
                <span className={`text-xs text-center leading-tight ${isActive ? 'text-blue-600/80 dark:text-blue-400/80' : 'text-gray-400 dark:text-gray-500'}`}>
                  {opt.description}
                </span>
              </button>
            )
          })}
        </div>
      </div>
    </div>
  )
}
