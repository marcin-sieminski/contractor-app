import { useQueries } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid, Cell } from 'recharts'
import { getFinancialForecast } from '../../api/analytics'
import { TAX_FORM_LABELS } from '../../types/forecast'
import type { TaxFormKey } from '../../types/forecast'
import { useTheme } from '../../context/ThemeContext'

const FORMS: TaxFormKey[] = ['liniowy', 'ryczalt', 'skala']

export function TaxFormComparisonChart() {
  const { resolvedTheme } = useTheme()
  const dark = resolvedTheme === 'dark'
  const year = new Date().getFullYear()

  const results = useQueries({
    queries: FORMS.map(form => ({
      queryKey: ['forecast-compare', form, year],
      queryFn: () =>
        getFinancialForecast({ year, taxForm: form, zusStage: 'pelny', includeForecast: true }),
    })),
  })

  const isLoading = results.some(r => r.isLoading)
  const fmt = (v: number) => v.toLocaleString('pl-PL', { maximumFractionDigits: 0 })

  const tickColor = dark ? '#9ca3af' : '#6b7280'
  const gridColor = dark ? '#374151' : '#f0f0f0'
  const tooltipStyle = dark
    ? { fontSize: 12, borderRadius: 8, backgroundColor: '#1f2937', border: '1px solid #374151', color: '#f3f4f6' }
    : { fontSize: 12, borderRadius: 8 }

  const Card = ({ children }: { children: React.ReactNode }) => (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
      <div className="flex items-center justify-between mb-4">
        <span className="text-sm font-semibold text-gray-700 dark:text-gray-200">Porównanie form opodatkowania ({year})</span>
        <Link to="/forecast" className="text-xs text-blue-600 hover:underline">Szczegóły →</Link>
      </div>
      {children}
    </div>
  )

  if (isLoading) {
    return <Card><div className="text-gray-400 dark:text-gray-500 text-sm text-center py-8">Ładowanie…</div></Card>
  }

  const chartData = FORMS.map((form, i) => ({
    key: form,
    label: TAX_FORM_LABELS[form],
    value: Math.round(results[i].data?.fullYear.totalObligations ?? 0),
  }))
  const hasData = chartData.some(d => d.value > 0)

  if (!hasData) {
    return <Card><div className="text-gray-400 dark:text-gray-500 text-sm text-center py-8">Brak danych</div></Card>
  }

  const minValue = Math.min(...chartData.map(d => d.value))
  const maxValue = Math.max(...chartData.map(d => d.value))
  const savings = maxValue - minValue
  const cheapest = chartData.find(d => d.value === minValue)

  return (
    <Card>
      <ResponsiveContainer width="100%" height={220}>
        <BarChart data={chartData} margin={{ top: 4, right: 8, left: 8, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke={gridColor} vertical={false} />
          <XAxis dataKey="label" tick={{ fontSize: 11, fill: tickColor }} axisLine={false} tickLine={false} />
          <YAxis tickFormatter={fmt} tick={{ fontSize: 11, fill: tickColor }} axisLine={false} tickLine={false} width={70} />
          <Tooltip
            formatter={(value) => [fmt(Number(value ?? 0)) + ' PLN', 'Podatki + ZUS + VAT (rok)']}
            contentStyle={tooltipStyle}
            cursor={{ fill: dark ? 'rgba(148,163,184,0.1)' : '#f8fafc' }}
          />
          <Bar dataKey="value" radius={[4, 4, 0, 0]} maxBarSize={64}>
            {chartData.map(entry => (
              <Cell key={entry.key} fill={entry.value === minValue ? '#22c55e' : dark ? '#475569' : '#cbd5e1'} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
      {cheapest && savings > 0 && (
        <div className="text-xs text-gray-500 dark:text-gray-400 mt-2">
          Najtańsza forma: <span className="font-semibold text-green-600">{cheapest.label}</span> — oszczędzasz do{' '}
          <span className="font-semibold">{fmt(savings)} PLN</span>/rok względem najdroższej.
        </div>
      )}
    </Card>
  )
}
