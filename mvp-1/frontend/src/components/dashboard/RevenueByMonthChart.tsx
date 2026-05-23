import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from 'recharts'
import { format, subMonths, startOfMonth } from 'date-fns'
import { pl } from 'date-fns/locale'
import type { Invoice } from '../../types/invoice'

function toPlnGross(inv: Invoice): number {
  if (inv.currency === 'PLN') return inv.totalGross
  if (inv.exchangeRate) return inv.totalGross * inv.exchangeRate
  return 0
}

interface Props {
  invoices: Invoice[]
}

export function RevenueByMonthChart({ invoices }: Props) {
  const accepted = invoices.filter(i => i.status === 'Accepted')

  const now = new Date()
  const months = Array.from({ length: 12 }, (_, i) => {
    const d = startOfMonth(subMonths(now, 11 - i))
    return {
      key: format(d, 'yyyy-MM'),
      label: format(d, 'MMM yy', { locale: pl }),
      value: 0,
    }
  })

  for (const inv of accepted) {
    const key = inv.issueDate.slice(0, 7)
    const month = months.find(m => m.key === key)
    if (month) month.value += toPlnGross(inv)
  }

  const data = months.map(m => ({ ...m, value: Math.round(m.value) }))
  const hasData = data.some(d => d.value > 0)

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
      <div className="text-sm font-semibold text-gray-700 mb-4">Przychód miesięczny (12 mies.)</div>
      <ResponsiveContainer width="100%" height={260}>
        <BarChart data={data} margin={{ top: 4, right: 8, left: 8, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" vertical={false} />
          <XAxis
            dataKey="label"
            tick={{ fontSize: 11, fill: '#6b7280' }}
            axisLine={false}
            tickLine={false}
          />
          <YAxis
            tickFormatter={fmt}
            tick={{ fontSize: 11, fill: '#6b7280' }}
            axisLine={false}
            tickLine={false}
            width={70}
          />
          <Tooltip
            formatter={(value) => [fmt(Number(value ?? 0)) + ' PLN', 'Przychód']}
            contentStyle={{ fontSize: 12, borderRadius: 8 }}
            cursor={{ fill: '#f3f4f6' }}
          />
          <Bar dataKey="value" fill="#3b82f6" radius={[4, 4, 0, 0]} maxBarSize={48} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}
