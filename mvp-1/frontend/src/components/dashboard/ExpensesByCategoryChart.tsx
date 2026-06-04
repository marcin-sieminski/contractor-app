import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts'
import { useTheme } from '../../context/ThemeContext'
import type { Expense } from '../../types/expense'
import { EXPENSE_CATEGORY_LABELS } from '../../types/expense'

const COLORS = ['#ef4444', '#f97316', '#f59e0b', '#eab308', '#84cc16', '#10b981', '#06b6d4', '#3b82f6', '#8b5cf6', '#ec4899']

interface Props {
  data: Expense[]
  onCategoryClick: (categoryLabel: string) => void
  selectedCategory: string | null
}

export function ExpensesByCategoryChart({ data, onCategoryClick, selectedCategory }: Props) {
  const { resolvedTheme } = useTheme()
  const dark = resolvedTheme === 'dark'

  const byCategory = data.reduce<Record<string, number>>((acc, e) => {
    const label = EXPENSE_CATEGORY_LABELS[e.category] ?? e.category
    acc[label] = (acc[label] ?? 0) + e.amountPLN
    return acc
  }, {})

  const chartData = Object.entries(byCategory)
    .map(([name, value]) => ({ name, value: Math.round(value) }))
    .filter(d => d.value > 0)
    .sort((a, b) => b.value - a.value)

  const fmt = (v: number) => v.toLocaleString('pl-PL', { maximumFractionDigits: 0 }) + ' PLN'
  const tooltipStyle = dark
    ? { fontSize: 12, borderRadius: 8, backgroundColor: '#1f2937', border: '1px solid #374151', color: '#f3f4f6' }
    : { fontSize: 12, borderRadius: 8 }
  const legendActiveColor = dark ? '#e5e7eb' : '#4b5563'
  const legendInactiveColor = dark ? '#6b7280' : '#9ca3af'

  if (chartData.length === 0) {
    return (
      <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
        <div className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-4">Wydatki wg kategorii</div>
        <div className="text-gray-400 dark:text-gray-500 text-sm text-center py-8">Brak danych</div>
      </div>
    )
  }

  return (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
      <div className="flex items-center justify-between mb-4">
        <span className="text-sm font-semibold text-gray-700 dark:text-gray-200">Wydatki wg kategorii</span>
        <span className="text-xs text-gray-400 dark:text-gray-500">kliknij wycinek aby zobaczyć szczegóły</span>
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
            onClick={(sliceData) => { if (sliceData?.name) onCategoryClick(sliceData.name as string) }}
            style={{ cursor: 'pointer' }}
          >
            {chartData.map((entry, i) => (
              <Cell
                key={i}
                fill={COLORS[i % COLORS.length]}
                opacity={selectedCategory && selectedCategory !== entry.name ? 0.4 : 1}
                stroke={selectedCategory === entry.name ? (dark ? '#e2e8f0' : '#1e293b') : 'none'}
                strokeWidth={selectedCategory === entry.name ? 2 : 0}
              />
            ))}
          </Pie>
          <Tooltip formatter={(value) => [fmt(Number(value ?? 0)), 'Wydatek']} contentStyle={tooltipStyle} />
          <Legend
            formatter={(value) => (
              <span
                className="text-xs cursor-pointer"
                style={{ color: selectedCategory && selectedCategory !== value ? legendInactiveColor : legendActiveColor }}
                onClick={() => onCategoryClick(value)}
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
