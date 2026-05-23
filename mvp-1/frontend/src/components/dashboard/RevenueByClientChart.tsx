import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts'
import type { RevenueDataPoint } from '../../types/revenueData'

const COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#06b6d4', '#f97316', '#84cc16']

interface Props {
  data: RevenueDataPoint[]
  onClientClick: (clientName: string) => void
  selectedClient: string | null
}

export function RevenueByClientChart({ data, onClientClick, selectedClient }: Props) {
  const byClient = data.reduce<Record<string, number>>((acc, p) => {
    acc[p.clientName] = (acc[p.clientName] ?? 0) + p.valuePLN
    return acc
  }, {})

  const chartData = Object.entries(byClient)
    .map(([name, value]) => ({ name, value: Math.round(value) }))
    .filter(d => d.value > 0)
    .sort((a, b) => b.value - a.value)

  if (chartData.length === 0) {
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
      <div className="flex items-center justify-between mb-4">
        <span className="text-sm font-semibold text-gray-700">Przychód wg klientów</span>
        <span className="text-xs text-gray-400">kliknij wycinek aby zobaczyć szczegóły</span>
      </div>
      <ResponsiveContainer width="100%" height={260}>
        <PieChart>
          <Pie
            data={chartData}
            cx="50%"
            cy="50%"
            innerRadius={60}
            outerRadius={100}
            paddingAngle={2}
            dataKey="value"
            onClick={(sliceData) => { if (sliceData?.name) onClientClick(sliceData.name as string) }}
            style={{ cursor: 'pointer' }}
          >
            {chartData.map((entry, i) => (
              <Cell
                key={i}
                fill={COLORS[i % COLORS.length]}
                opacity={selectedClient && selectedClient !== entry.name ? 0.4 : 1}
                stroke={selectedClient === entry.name ? '#1e293b' : 'none'}
                strokeWidth={selectedClient === entry.name ? 2 : 0}
              />
            ))}
          </Pie>
          <Tooltip
            formatter={(value) => [fmt(Number(value ?? 0)), 'Przychód']}
            contentStyle={{ fontSize: 12, borderRadius: 8 }}
          />
          <Legend
            formatter={(value) => (
              <span
                className="text-xs cursor-pointer"
                style={{ color: selectedClient && selectedClient !== value ? '#9ca3af' : '#4b5563' }}
                onClick={() => onClientClick(value)}
              >
                {value}
              </span>
            )}
          />
        </PieChart>
      </ResponsiveContainer>
    </div>
  )
}
