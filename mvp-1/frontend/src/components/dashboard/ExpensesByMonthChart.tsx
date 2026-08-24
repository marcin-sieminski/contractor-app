import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid, Cell } from 'recharts'
import { format, subMonths, startOfMonth } from 'date-fns'
import { pl } from 'date-fns/locale'
import { useTheme } from '../../context/ThemeContext'
import type { Expense } from '../../types/expense'

interface Props {
  data: Expense[]
  onMonthClick: (monthKey: string) => void
  selectedMonth: string | null
}

export function ExpensesByMonthChart({ data, onMonthClick, selectedMonth }: Props) {
  const { resolvedTheme } = useTheme()
  const dark = resolvedTheme === 'dark'

  const now = new Date()
  const months = Array.from({ length: 12 }, (_, i) => {
    const d = startOfMonth(subMonths(now, 11 - i))
    return { key: format(d, 'yyyy-MM'), label: format(d, 'MMM yy', { locale: pl }), value: 0 }
  })

  for (const expense of data) {
    const month = months.find(m => m.key === expense.date.slice(0, 7))
    if (month) month.value += expense.amountPLN
  }

  const chartData = months.map(m => ({ ...m, value: Math.round(m.value) }))
  const hasData = chartData.some(d => d.value > 0)
  const fmt = (v: number) => v.toLocaleString('pl-PL', { maximumFractionDigits: 0 })

  const tickColor = dark ? '#9ca3af' : '#6b7280'
  const gridColor = dark ? '#374151' : '#f0f0f0'
  const tooltipStyle = dark
    ? { fontSize: 12, borderRadius: 8, backgroundColor: '#1f2937', border: '1px solid #374151', color: '#f3f4f6' }
    : { fontSize: 12, borderRadius: 8 }
  const cursorFill = dark ? 'rgba(239,68,68,0.15)' : '#fef2f2'

  if (!hasData) {
    return (
      <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
        <div className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-4">Wydatki miesięczne (12 mies.)</div>
        <div className="text-gray-400 dark:text-gray-500 text-sm text-center py-8">Brak danych</div>
      </div>
    )
  }

  return (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
      <div className="flex items-center justify-between mb-4">
        <span className="text-sm font-semibold text-gray-700 dark:text-gray-200">Wydatki miesięczne (12 mies.)</span>
        <span className="text-xs text-gray-400 dark:text-gray-500">kliknij słupek aby zobaczyć szczegóły</span>
      </div>
      <ResponsiveContainer width="100%" height={260}>
        <BarChart data={chartData} margin={{ top: 4, right: 8, left: 8, bottom: 0 }} style={{ cursor: 'pointer' }}>
          <CartesianGrid strokeDasharray="3 3" stroke={gridColor} vertical={false} />
          <XAxis dataKey="label" tick={{ fontSize: 11, fill: tickColor }} axisLine={false} tickLine={false} />
          <YAxis tickFormatter={fmt} tick={{ fontSize: 11, fill: tickColor }} axisLine={false} tickLine={false} width={70} />
          <Tooltip
            formatter={(value) => [fmt(Number(value ?? 0)) + ' PLN', 'Wydatki']}
            contentStyle={tooltipStyle}
            cursor={{ fill: cursorFill }}
          />
          <Bar
            dataKey="value"
            radius={[4, 4, 0, 0]}
            maxBarSize={48}
            onClick={(barData) => { if (barData?.payload?.key) onMonthClick(barData.payload.key as string) }}
          >
            {chartData.map(entry => (
              <Cell key={entry.key} fill={entry.key === selectedMonth ? '#b91c1c' : '#ef4444'} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}
