import { useState, useId } from 'react'
import { Monitor, Moon, Sun, Contrast, Sparkles } from 'lucide-react'
import { useTheme, type Theme } from '../context/ThemeContext'
import { useAuth } from '../context/AuthContext'
import { profileApi } from '../api/profile'

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
  { value: 'modern', label: 'Nowoczesny', icon: Sparkles, description: 'Ciepłe, jasne barwy' },
  { value: 'system', label: 'Systemowy',  icon: Monitor,  description: 'Dopasuj do ustawień systemu' },
]

export function SettingsPage() {
  const { theme, setTheme } = useTheme()
  const { user, updateDisplayName } = useAuth()

  const [displayName, setDisplayName] = useState(user?.displayName ?? '')
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const displayNameId = useId()

  async function handleSaveProfile(e: React.FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    try {
      const result = await profileApi.updateProfile(displayName.trim() || null)
      updateDisplayName(result.displayName)
      setSaved(true)
      setTimeout(() => setSaved(false), 2000)
    } catch {
      setError('Nie udało się zapisać zmian.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="p-6 max-w-2xl">
      <h1 className="text-2xl font-bold mb-6 text-gray-900 dark:text-gray-100">Ustawienia</h1>

      <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-6 mb-6">
        <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-1">Profil</h2>
        <p className="text-xs text-gray-500 dark:text-gray-400 mb-4">Nazwa wyświetlana w pasku nawigacji zamiast adresu e-mail</p>
        <form onSubmit={handleSaveProfile} className="space-y-4">
          <div>
            <label htmlFor={displayNameId} className="block text-xs font-medium text-gray-600 dark:text-gray-300 mb-1">
              Nazwa użytkownika
            </label>
            <input
              id={displayNameId}
              type="text"
              autoComplete="name"
              value={displayName}
              onChange={e => setDisplayName(e.target.value)}
              placeholder={user?.email ?? ''}
              maxLength={80}
              className="w-full px-3 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">
              Zostaw puste, aby wyświetlać adres e-mail
            </p>
          </div>
          {error && <p role="alert" className="text-xs text-red-500">{error}</p>}
          <button
            type="submit"
            disabled={saving}
            className="px-4 py-2 text-sm font-medium rounded-lg bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white transition-colors"
          >
            {saving ? 'Zapisywanie…' : saved ? 'Zapisano ✓' : 'Zapisz'}
          </button>
        </form>
      </div>

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
                <span className={`text-xs text-center leading-tight ${isActive ? 'text-blue-600/80 dark:text-blue-400/80' : 'text-gray-500 dark:text-gray-400'}`}>
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
