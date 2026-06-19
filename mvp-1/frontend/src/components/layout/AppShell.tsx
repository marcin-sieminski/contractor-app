import { NavLink, Outlet } from 'react-router-dom'
import { Clock, Users, FileText, FileCheck, FileSpreadsheet, LayoutDashboard, LogOut, Receipt, MessageCircle, TrendingUp, Settings, Landmark, BarChart2, CalendarDays, Waves, Globe, Cpu } from 'lucide-react'
import { useAuth } from '../../context/AuthContext'
import { GlobalTimerBar } from '../timer/GlobalTimerBar'

const nav = [
  { to: '/', icon: LayoutDashboard, label: 'Dashboard' },
  { to: '/time', icon: Clock, label: 'Czas pracy' },
  { to: '/clients', icon: Users, label: 'Klienci' },
  { to: '/invoices', icon: FileText, label: 'Faktury' },
  { to: '/expenses', icon: Receipt, label: 'Wydatki' },
  { to: '/forecast', icon: TrendingUp, label: 'Analiza finansowa' },
  { to: '/settlement', icon: FileCheck, label: 'Rozliczenie roczne' },
  { to: '/profitability', icon: BarChart2, label: 'Rentowność' },
  { to: '/work-analytics', icon: CalendarDays, label: 'Analiza czasu pracy' },
  { to: '/cash-flow', icon: Waves, label: 'Cash Flow' },
  { to: '/currency-exposure', icon: Globe, label: 'Waluty' },
  { to: '/ip-box', icon: Cpu, label: 'IP Box' },
  { to: '/tax-obligations', icon: Landmark, label: 'Zobowiązania' },
  { to: '/documents', icon: FileSpreadsheet, label: 'Dokumenty' },
  { to: '/chat', icon: MessageCircle, label: 'Asystent AI' },
]

export function AppShell() {
  const { user, logout } = useAuth()

  return (
    <div className="flex h-screen bg-gray-50 dark:bg-gray-950">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:fixed focus:top-3 focus:left-3 focus:z-50 focus:px-4 focus:py-2 focus:rounded-lg focus:bg-blue-600 focus:text-white focus:shadow-lg"
      >
        Przejdź do treści
      </a>
      <aside className="w-56 bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-700 flex flex-col p-4 gap-1">
        <div className="font-bold text-xl text-blue-600 px-2">ContractorHub</div>
        <div className="px-2 mb-4 mt-1 text-xs text-gray-500 dark:text-gray-400 truncate">{user?.displayName || user?.email}</div>
        <nav aria-label="Główna nawigacja" className="flex flex-col gap-1">
          {nav.map(({ to, icon: Icon, label }) => (
            <NavLink
              key={to}
              to={to}
              end={to === '/'}
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400'
                    : 'text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800'
                }`
              }
            >
              <Icon size={18} />
              {label}
            </NavLink>
          ))}
        </nav>

        <div className="mt-auto pt-4 border-t border-gray-200 dark:border-gray-700">
          <NavLink
            to="/settings"
            className={({ isActive }) =>
              `flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors mb-1 ${
                isActive
                  ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400'
                  : 'text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800'
              }`
            }
          >
            <Settings size={18} />
            Ustawienia
          </NavLink>
          <button
            onClick={logout}
            className="flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 w-full transition-colors"
          >
            <LogOut size={18} />
            Wyloguj
          </button>
        </div>
      </aside>
      <main id="main" tabIndex={-1} className="flex-1 overflow-auto flex flex-col focus:outline-none">
        <GlobalTimerBar />
        <div className="flex-1 overflow-auto">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
