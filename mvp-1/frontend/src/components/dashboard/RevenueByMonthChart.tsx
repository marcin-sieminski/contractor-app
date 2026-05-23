import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid, Cell } from 'recharts'
import { format, subMonths, startOfMonth } from 'date-fns'
import { pl } from 'date-fns/locale'
import type { RevenueDataPoint } from '../../types/revenueData'

interface Props {
  data: RevenueDataPoint[]
  onMonthClick: (monthKey: string) => void
  selectedMonth: string | null
}

export function RevenueByMonthChart({ data, onMonthClick, selectedMonth }: Props) {
  const now = new Date()
  const months = Array.from({ length: 12 }, (_, i) => {
    const d = startOfMonth(subMonths(now, 11 - i))
    return { key: format(d, 'yyyy-MM'), label: format(d, 'MMM yy', { locale: pl }), value: 0 }
  })

  for (const point of data) {
    const month = months.find(m => m.key === point.monthKey)
    if (month) month.value += point.valuePLN
  }

  const chartData = months.map(m => ({ ...m, value: Math.round(m.value) }))
  const hasData = chartData.some(d => d.value > 0)

  if (!hasData) {
    return (
      <div className="bg-white border border-gray-200 rounded-xl p-4">
        <div className="text-sm font-semibold text-gray-700 mb-4">Przychód miesięczny (12 mies.)</div>
        <div className="text-gray-400 text-sm text-center py-8">Brak danych</div>
      </div>
    )
  }

  const fmt = (v: number) => v.toLocaleString('pl-PL', { maximumFractionDigits: 0 })

  return (
    <div className="bg-white border border-gray-200 rounded-xl p-4">
      <div className="flex items-center justify-between mb-4">
        <span className="text-sm font-semibold text-gray-700">Przychód miesięczny (12 mies.)</span>
        <span className="text-xs text-gray-400">kliknij słupek aby zobaczyć szczegóły</span>
      </div>
      <ResponsiveContainer width="100%" height={260}>
        <BarChart data={chartData} margin={{ top: 4, right: 8, left: 8, bottom: 0 }} style={{ cursor: 'pointer' }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" vertical={false} />
          <XAxis dataKey="label" tick={{ fontSize: 11, fill: '#6b7280' }} axisLine={false} tickLine={false} />
          <YAxis tickFormatter={fmt} tick={{ fontSize: 11, fill: '#6b7280' }} axisLine={false} tickLine={false} width={70} />
          <Tooltip
            formatter={(value) => [fmt(Number(value ?? 0)) + ' PLN', 'Przychód']}
            contentStyle={{ fontSize: 12, borderRadius: 8 }}
            cursor={{ fill: '#eff6ff' }}
          />
          <Bar
            dataKey="value"
            radius={[4, 4, 0, 0]}
            maxBarSize={48}
            onClick={(barData) => { if (barData?.payload?.key) onMonthClick(barData.payload.key as string) }}
          >
            {chartData.map(entry => (
              <Cell key={entry.key} fill={entry.key === selectedMonth ? '#1d4ed8' : '#3b82f6'} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}
