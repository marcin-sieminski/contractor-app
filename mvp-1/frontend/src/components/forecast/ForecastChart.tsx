import {
  ComposedChart, Bar, Line, XAxis, YAxis, Tooltip, Legend,
  ResponsiveContainer, CartesianGrid, ReferenceArea, Cell,
} from 'recharts'
import type { MonthForecast } from '../../types/forecast'

interface Props {
  months: MonthForecast[]
}

const fmt = (v: number) => v.toLocaleString('pl-PL', { maximumFractionDigits: 0 })

export function ForecastChart({ months }: Props) {
  const data = months.map(m => ({
    label: m.monthName.slice(0, 3),
    isActual: m.isActual,
    Przychód: m.revenue,
    Obciążenia: m.totalObligations,
    'Przepływ netto': m.netCashFlow,
  }))

  // Zakres prognozy (pierwszy miesiąc, który nie jest rzeczywisty).
  const firstForecast = months.find(m => !m.isActual)
  const lastMonth = months[months.length - 1]
  const forecastFrom = firstForecast ? firstForecast.monthName.slice(0, 3) : null
  const forecastTo = lastMonth ? lastMonth.monthName.slice(0, 3) : null

  return (
    <div className="bg-white border border-gray-200 rounded-xl p-4">
      <div className="flex items-center justify-between mb-4">
        <span className="text-sm font-semibold text-gray-700">
          Przychód, obciążenia i przepływ netto (12 mies.)
        </span>
        <span className="text-xs text-gray-400">jaśniejszy obszar = prognoza</span>
      </div>
      <ResponsiveContainer width="100%" height={320}>
        <ComposedChart data={data} margin={{ top: 4, right: 8, left: 8, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" vertical={false} />
          {forecastFrom && forecastTo && (
            <ReferenceArea x1={forecastFrom} x2={forecastTo} fill="#f8fafc" fillOpacity={0.8} />
          )}
          <XAxis dataKey="label" tick={{ fontSize: 11, fill: '#6b7280' }} axisLine={false} tickLine={false} />
          <YAxis tickFormatter={fmt} tick={{ fontSize: 11, fill: '#6b7280' }} axisLine={false} tickLine={false} width={70} />
          <Tooltip
            formatter={(value, name) => [fmt(Number(value ?? 0)) + ' PLN', name as string]}
            contentStyle={{ fontSize: 12, borderRadius: 8 }}
          />
          <Legend wrapperStyle={{ fontSize: 12 }} />
          <Bar dataKey="Przychód" radius={[4, 4, 0, 0]} maxBarSize={28}>
            {data.map((d, i) => (
              <Cell key={i} fill={d.isActual ? '#3b82f6' : '#93c5fd'} />
            ))}
          </Bar>
          <Bar dataKey="Obciążenia" radius={[4, 4, 0, 0]} maxBarSize={28}>
            {data.map((d, i) => (
              <Cell key={i} fill={d.isActual ? '#ef4444' : '#fca5a5'} />
            ))}
          </Bar>
          <Line
            type="monotone"
            dataKey="Przepływ netto"
            stroke="#16a34a"
            strokeWidth={2}
            dot={{ r: 3 }}
          />
        </ComposedChart>
      </ResponsiveContainer>
    </div>
  )
}
