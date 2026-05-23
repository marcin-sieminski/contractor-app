import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts'
import type { Invoice } from '../../types/invoice'

const COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#06b6d4', '#f97316', '#84cc16']

function toPlnGross(inv: Invoice): number {
  if (inv.currency === 'PLN') return inv.totalGross
  if (inv.exchangeRate) return inv.totalGross * inv.exchangeRate
  return 0
}

interface Props {
  invoices: Invoice[]
}

export function RevenueByClientChart({ invoices }: Props) {
  const accepted = invoices.filter(i => i.status === 'Accepted')

  const byClient = accepted.reduce<Record<string, number>>((acc, inv) => {
    const pln = toPlnGross(inv)
    acc[inv.clientName] = (acc[inv.clientName] ?? 0) + pln
    return acc
  }, {})

  const data = Object.entries(byClient)
    .map(([name, value]) => ({ name, value: Math.round(value) }))
    .filter(d => d.value > 0)
    .sort((a, b) => b.value - a.value)

  if (data.length === 0) {
    return (
      <div className="bg-white border border-gray-200 rounded-xl p-4">
        <div className="text-sm font-semibold text-gray-700 mb-4">Przychód wg klientów</div>
        <div className="text-gray-400 text-sm text-center py-8">Brak danych</div>
      </div>
    )
  }

  const fmt = (v: number) => v.toLocaleString('pl-PL', { maximumFractionDigits: 0 }) + ' PLN'

  return (
    <div className="bg-white border border-gray-200 rounded-xl p-4">
      <div className="text-sm font-semibold text-gray-700 mb-4">Przychód wg klientów</div>
      <ResponsiveContainer width="100%" height={260}>
        <PieChart>
          <Pie
            data={data}
            cx="50%"
            cy="50%"
            innerRadius={60}
            outerRadius={100}
            paddingAngle={2}
            dataKey="value"
          >
            {data.map((_, i) => (
              <Cell key={i} fill={COLORS[i % COLORS.length]} />
            ))}
          </Pie>
          <Tooltip
            formatter={(value) => [fmt(Number(value ?? 0)), 'Przychód']}
            contentStyle={{ fontSize: 12, borderRadius: 8 }}
          />
          <Legend
            formatter={(value) => <span className="text-xs text-gray-600">{value}</span>}
          />
        </PieChart>
      </ResponsiveContainer>
    </div>
  )
}
