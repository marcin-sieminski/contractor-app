import { useEffect, useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { Clock, Users, FileText, FileCheck, FileSpreadsheet, LayoutDashboard, LogOut, Receipt, MessageCircle, TrendingUp, Settings, Landmark, BarChart2, CalendarDays, Waves, Globe, Cpu, Menu } from 'lucide-react'
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

const navLinkClass = ({ isActive }: { isActive: boolean }) =>
  `flex items-center gap-3 px-3 min-h-11 py-2 rounded-lg text-sm font-medium transition-colors ${
    isActive
      ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400'
      : 'text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800'
  }`

/** Zawartość paska bocznego — współdzielona przez stały sidebar (desktop)
 *  i wysuwaną szufladę (mobile). `onNavigate` zamyka szufladę po wyborze. */
function SidebarContent({ onNavigate }: { onNavigate?: () => void }) {
  const { user, logout } = useAuth()

  return (
    <>
      <div className="font-bold text-xl text-blue-600 px-2">ContractorHub</div>
      <div className="px-2 mb-4 mt-1 text-xs text-gray-500 dark:text-gray-400 truncate">{user?.displayName || user?.email}</div>
      <nav aria-label="Główna nawigacja" className="flex flex-col gap-1">
        {nav.map(({ to, icon: Icon, label }) => (
          <NavLink key={to} to={to} end={to === '/'} onClick={onNavigate} className={navLinkClass}>
            <Icon size={18} />
            {label}
          </NavLink>
        ))}
      </nav>

      <div className="mt-auto pt-4 border-t border-gray-200 dark:border-gray-700">
        <NavLink to="/settings" onClick={onNavigate} className={({ isActive }) => `${navLinkClass({ isActive })} mb-1`}>
          <Settings size={18} />
          Ustawienia
        </NavLink>
        <button
          onClick={() => { onNavigate?.(); logout() }}
          className="flex items-center gap-3 px-3 min-h-11 py-2 rounded-lg text-sm font-medium text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 w-full transition-colors"
        >
          <LogOut size={18} />
          Wyloguj
        </button>
      </div>
    </>
  )
}

export function AppShell() {
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const location = useLocation()

  // Zamknij szufladę przy zmianie trasy.
  useEffect(() => { setMobileNavOpen(false) }, [location.pathname])

  // Zablokuj przewijanie tła i obsłuż Escape, gdy szuflada jest otwarta.
  useEffect(() => {
    if (!mobileNavOpen) return
    const prevOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') setMobileNavOpen(false) }
    document.addEventListener('keydown', onKey)
    return () => {
      document.body.style.overflow = prevOverflow
      document.removeEventListener('keydown', onKey)
    }
  }, [mobileNavOpen])

  return (
    <div className="flex h-screen bg-gray-50 dark:bg-gray-950">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:fixed focus:top-3 focus:left-3 focus:z-50 focus:px-4 focus:py-2 focus:rounded-lg focus:bg-blue-600 focus:text-white focus:shadow-lg"
      >
        Przejdź do treści
      </a>

      {/* Sidebar — stały na desktopie (md+), ukryty na telefonie */}
      <aside className="hidden md:flex w-56 shrink-0 bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-700 flex-col p-4 gap-1">
        <SidebarContent />
      </aside>

      {/* Szuflada mobilna + nakładka */}
      {mobileNavOpen && (
        <div className="md:hidden fixed inset-0 z-40" role="dialog" aria-modal="true" aria-label="Menu nawigacji">
          <div
            className="absolute inset-0 bg-black/50"
            onClick={() => setMobileNavOpen(false)}
            aria-hidden="true"
          />
          <aside className="absolute inset-y-0 left-0 w-72 max-w-[85%] bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-700 flex flex-col p-4 gap-1 shadow-xl overflow-y-auto">
            <SidebarContent onNavigate={() => setMobileNavOpen(false)} />
          </aside>
        </div>
      )}

      <main id="main" tabIndex={-1} className="flex-1 min-w-0 overflow-auto flex flex-col focus:outline-none">
        {/* Górny pasek mobilny z hamburgerem — tylko na telefonie */}
        <header className="md:hidden flex items-center gap-3 px-2 h-14 bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-700 shrink-0">
          <button
            onClick={() => setMobileNavOpen(true)}
            aria-label="Otwórz menu"
            aria-expanded={mobileNavOpen}
            className="flex items-center justify-center h-11 w-11 rounded-lg text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
          >
            <Menu size={22} />
          </button>
          <span className="font-bold text-lg text-blue-600">ContractorHub</span>
        </header>

        <GlobalTimerBar />
        <div className="flex-1 overflow-auto">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
