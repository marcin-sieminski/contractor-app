import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { differenceInCalendarDays, parseISO, format } from 'date-fns'
import { pl } from 'date-fns/locale'
import { getTaxObligations } from '../../api/taxObligations'
import type { MonthObligation } from '../../types/taxObligations'

const fmt = (v: number) => v.toLocaleString('pl-PL', { maximumFractionDigits: 0 }) + ' PLN'

// Najbliższy termin dla miesiąca (ZUS/PIT 20., VAT 25.) z niezerową pozostałością.
function earliestDueDate(m: MonthObligation): string {
  return [m.zusDueDate, m.pitDueDate, m.vatDueDate].sort((a, b) => a.localeCompare(b))[0]
}

export function UpcomingObligationsWidget() {
  const year = new Date().getFullYear()
  const { data, isLoading } = useQuery({
    queryKey: ['tax-obligations', year, 'liniowy', 'pelny'],
    queryFn: () => getTaxObligations(year, 'liniowy', 'pelny'),
  })

  const Card = ({ children }: { children: React.ReactNode }) => (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
      <div className="flex items-center justify-between mb-3">
        <span className="text-sm font-semibold text-gray-700 dark:text-gray-200">Nadchodzące zobowiązania</span>
        <Link to="/tax-obligations" className="text-xs text-blue-600 hover:underline">Wszystkie →</Link>
      </div>
      {children}
    </div>
  )

  if (isLoading) {
    return <Card><div className="text-gray-400 dark:text-gray-500 text-sm text-center py-8">Ładowanie…</div></Card>
  }

  const today = new Date()
  const upcoming = (data?.months ?? [])
    .map(m => ({ m, remaining: m.totalDue - m.totalPaid, due: earliestDueDate(m) }))
    .filter(x => x.remaining > 0.5 && (x.m.status === 'due' || x.m.status === 'overdue' || x.m.status === 'partial' || x.m.status === 'future'))
    .sort((a, b) => a.due.localeCompare(b.due))
    .slice(0, 5)

  if (upcoming.length === 0) {
    return <Card><div className="text-gray-400 dark:text-gray-500 text-sm text-center py-8">Brak otwartych zobowiązań 🎉</div></Card>
  }

  return (
    <Card>
      <div className="divide-y divide-gray-50 dark:divide-gray-700/50">
        {upcoming.map(({ m, remaining, due }) => {
          const days = differenceInCalendarDays(parseISO(due), today)
          const overdue = m.status === 'overdue' || days < 0
          return (
            <div key={m.month} className="flex items-center justify-between py-2">
              <div>
                <div className="text-sm text-gray-800 dark:text-gray-100">{m.monthName}</div>
                <div className={`text-xs ${overdue ? 'text-red-600' : 'text-gray-400 dark:text-gray-500'}`}>
                  termin {format(parseISO(due), 'd MMM', { locale: pl })}
                  {overdue ? ` · po terminie (${Math.abs(days)} dni)` : ` · za ${days} dni`}
                </div>
              </div>
              <div className={`text-sm font-semibold ${overdue ? 'text-red-600' : 'text-gray-800 dark:text-gray-100'}`}>
                {fmt(remaining)}
              </div>
            </div>
          )
        })}
      </div>
    </Card>
  )
}
