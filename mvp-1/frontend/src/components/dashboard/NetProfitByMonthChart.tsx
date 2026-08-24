import { ComposedChart, Bar, Line, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid, Legend } from 'recharts'
import { format, subMonths, startOfMonth } from 'date-fns'
import { pl } from 'date-fns/locale'
import { useTheme } from '../../context/ThemeContext'
import type { RevenueDataPoint } from '../../types/revenueData'
import type { Expense } from '../../types/expense'

interface Props {
  revenuePoints: RevenueDataPoint[]
  expenses: Expense[]
}

export function NetProfitByMonthChart({ revenuePoints, expenses }: Props) {
  const { resolvedTheme } = useTheme()
  const dark = resolvedTheme === 'dark'

  const now = new Date()
  const months = Array.from({ length: 12 }, (_, i) => {
    const d = startOfMonth(subMonths(now, 11 - i))
    return { key: format(d, 'yyyy-MM'), label: format(d, 'MMM yy', { locale: pl }), revenue: 0, costs: 0 }
  })

  for (const p of revenuePoints) {
    const month = months.find(m => m.key === p.monthKey)
    if (month) month.revenue += p.valuePLN
  }
  for (const e of expenses) {
    const month = months.find(m => m.key === e.date.slice(0, 7))
    if (month) month.costs += e.amountPLN
  }

  const chartData = months.map(m => ({
    ...m,
    revenue: Math.round(m.revenue),
    costs: Math.round(m.costs),
    net: Math.round(m.revenue - m.costs),
  }))
  const hasData = chartData.some(d => d.revenue > 0 || d.costs > 0)
  const fmt = (v: number) => v.toLocaleString('pl-PL', { maximumFractionDigits: 0 })

  const tickColor = dark ? '#9ca3af' : '#6b7280'
  const gridColor = dark ? '#374151' : '#f0f0f0'
  const tooltipStyle = dark
    ? { fontSize: 12, borderRadius: 8, backgroundColor: '#1f2937', border: '1px solid #374151', color: '#f3f4f6' }
    : { fontSize: 12, borderRadius: 8 }

  const labelFor = (key: string) =>
    key === 'revenue' ? 'Przychód' : key === 'costs' ? 'Koszty' : 'Zysk netto'

  if (!hasData) {
    return (
      <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
        <div className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-4">Zysk netto (12 mies.)</div>
        <div className="text-gray-400 dark:text-gray-500 text-sm text-center py-8">Brak danych</div>
      </div>
    )
  }

  return (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
      <div className="flex items-center justify-between mb-4">
        <span className="text-sm font-semibold text-gray-700 dark:text-gray-200">Zysk netto (12 mies.)</span>
        <span className="text-xs text-gray-400 dark:text-gray-500">przychód − koszty</span>
      </div>
      <ResponsiveContainer width="100%" height={260}>
        <ComposedChart data={chartData} margin={{ top: 4, right: 8, left: 8, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke={gridColor} vertical={false} />
          <XAxis dataKey="label" tick={{ fontSize: 11, fill: tickColor }} axisLine={false} tickLine={false} />
          <YAxis tickFormatter={fmt} tick={{ fontSize: 11, fill: tickColor }} axisLine={false} tickLine={false} width={70} />
          <Tooltip
            formatter={(value, name, item) => {
              const v = Number(value ?? 0)
              if (name === 'net') {
                const rev = Number(item?.payload?.revenue ?? 0)
                const margin = rev > 0 ? ` (${Math.round((v / rev) * 100)}% marży)` : ''
                return [fmt(v) + ' PLN' + margin, labelFor(String(name))]
              }
              return [fmt(v) + ' PLN', labelFor(String(name))]
            }}
            contentStyle={tooltipStyle}
            cursor={{ fill: dark ? 'rgba(148,163,184,0.1)' : '#f8fafc' }}
          />
          <Legend formatter={(value) => labelFor(String(value))} wrapperStyle={{ fontSize: 11 }} />
          <Bar dataKey="revenue" fill="#22c55e" radius={[4, 4, 0, 0]} maxBarSize={28} />
          <Bar dataKey="costs" fill="#ef4444" radius={[4, 4, 0, 0]} maxBarSize={28} />
          <Line type="monotone" dataKey="net" stroke={dark ? '#60a5fa' : '#2563eb'} strokeWidth={2} dot={{ r: 3 }} />
        </ComposedChart>
      </ResponsiveContainer>
    </div>
  )
}
