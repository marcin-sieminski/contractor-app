import { NavLink, Outlet } from 'react-router-dom'
import { Clock, Users, FileText, LayoutDashboard, LogOut, Receipt, MessageCircle, TrendingUp } from 'lucide-react'
import { useAuth } from '../../context/AuthContext'
import { GlobalTimerBar } from '../timer/GlobalTimerBar'

const nav = [
  { to: '/', icon: LayoutDashboard, label: 'Dashboard' },
  { to: '/time', icon: Clock, label: 'Czas pracy' },
  { to: '/clients', icon: Users, label: 'Klienci' },
  { to: '/invoices', icon: FileText, label: 'Faktury' },
  { to: '/expenses', icon: Receipt, label: 'Wydatki' },
  { to: '/forecast', icon: TrendingUp, label: 'Analiza finansowa' },
  { to: '/chat', icon: MessageCircle, label: 'Asystent AI' }
]

export function AppShell() {
  const { user, logout } = useAuth()

  return (
    <div className="flex h-screen">
      <aside className="w-56 bg-white border-r border-gray-200 flex flex-col p-4 gap-1">
        <div className="font-bold text-xl text-blue-600 mb-6 px-2">ContractorHub</div>
        {nav.map(({ to, icon: Icon, label }) => (
          <NavLink
            key={to}
            to={to}
            end={to === '/'}
            className={({ isActive }) =>
              `flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                isActive ? 'bg-blue-50 text-blue-700' : 'text-gray-600 hover:bg-gray-100'
              }`
            }
          >
            <Icon size={18} />
            {label}
          </NavLink>
        ))}

        <div className="mt-auto pt-4 border-t border-gray-200">
          <div className="px-2 mb-2 text-xs text-gray-400 truncate">{user?.email}</div>
          <button
            onClick={logout}
            className="flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium text-gray-600 hover:bg-gray-100 w-full transition-colors"
          >
            <LogOut size={18} />
            Wyloguj
          </button>
        </div>
      </aside>
      <main className="flex-1 overflow-auto flex flex-col">
        <GlobalTimerBar />
        <div className="flex-1 overflow-auto">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
