import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { AreaChart, Area, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid, ReferenceLine } from 'recharts'
import { format, parseISO } from 'date-fns'
import { pl } from 'date-fns/locale'
import { getCashFlowForecast } from '../../api/cashFlow'
import { useTheme } from '../../context/ThemeContext'

const fmt = (v: number) => v.toLocaleString('pl-PL', { maximumFractionDigits: 0 })

export function CashFlowMiniChart() {
  const { resolvedTheme } = useTheme()
  const dark = resolvedTheme === 'dark'

  const { data, isLoading } = useQuery({
    queryKey: ['cash-flow', 0, 'liniowy', 'pelny'],
    queryFn: () => getCashFlowForecast(0, 'liniowy', 'pelny'),
  })

  const tickColor = dark ? '#9ca3af' : '#6b7280'
  const gridColor = dark ? '#374151' : '#f0f0f0'
  const tooltipStyle = dark
    ? { fontSize: 12, borderRadius: 8, backgroundColor: '#1f2937', border: '1px solid #374151', color: '#f3f4f6' }
    : { fontSize: 12, borderRadius: 8 }

  const Card = ({ children, foot }: { children: React.ReactNode; foot?: React.ReactNode }) => (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
      <div className="flex items-center justify-between mb-4">
        <span className="text-sm font-semibold text-gray-700 dark:text-gray-200">Prognoza salda (90 dni)</span>
        <Link to="/cash-flow" className="text-xs text-blue-600 hover:underline">Szczegóły →</Link>
      </div>
      {children}
      {foot}
    </div>
  )

  if (isLoading) {
    return <Card><div className="text-gray-400 dark:text-gray-500 text-sm text-center py-8">Ładowanie…</div></Card>
  }

  const chartData = (data?.days ?? []).map(d => ({
    date: d.date,
    label: format(parseISO(d.date), 'd MMM', { locale: pl }),
    balance: Math.round(d.balance),
  }))

  if (chartData.length === 0) {
    return <Card><div className="text-gray-400 dark:text-gray-500 text-sm text-center py-8">Brak danych</div></Card>
  }

  const endBalance = Math.round(data?.projectedEndBalance ?? 0)
  const buffer = Math.round(data?.monthlyBufferRecommended ?? 0)

  return (
    <Card
      foot={
        <div className="text-xs text-gray-500 dark:text-gray-400 mt-2">
          Saldo końcowe: <span className={`font-semibold ${endBalance < 0 ? 'text-red-600' : 'text-gray-800 dark:text-gray-100'}`}>{fmt(endBalance)} PLN</span>
          {' · '}rekomendowany bufor: <span className="font-semibold">{fmt(buffer)} PLN</span>/mies.
        </div>
      }
    >
      <ResponsiveContainer width="100%" height={200}>
        <AreaChart data={chartData} margin={{ top: 4, right: 8, left: 8, bottom: 0 }}>
          <defs>
            <linearGradient id="cashflowFill" x1="0" y1="0" x2="0" y2="1">
              <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.35} />
              <stop offset="95%" stopColor="#3b82f6" stopOpacity={0} />
            </linearGradient>
          </defs>
          <CartesianGrid strokeDasharray="3 3" stroke={gridColor} vertical={false} />
          <XAxis dataKey="label" tick={{ fontSize: 10, fill: tickColor }} axisLine={false} tickLine={false} minTickGap={40} />
          <YAxis tickFormatter={fmt} tick={{ fontSize: 11, fill: tickColor }} axisLine={false} tickLine={false} width={70} />
          <ReferenceLine y={0} stroke={dark ? '#f87171' : '#ef4444'} strokeDasharray="4 4" />
          <Tooltip
            formatter={(value) => [fmt(Number(value ?? 0)) + ' PLN', 'Saldo']}
            contentStyle={tooltipStyle}
          />
          <Area type="monotone" dataKey="balance" stroke="#3b82f6" strokeWidth={2} fill="url(#cashflowFill)" />
        </AreaChart>
      </ResponsiveContainer>
    </Card>
  )
}
