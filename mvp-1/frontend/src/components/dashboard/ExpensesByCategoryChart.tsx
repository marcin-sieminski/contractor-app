import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts'
import type { Expense } from '../../types/expense'
import { EXPENSE_CATEGORY_LABELS } from '../../types/expense'

const COLORS = ['#ef4444', '#f97316', '#f59e0b', '#eab308', '#84cc16', '#10b981', '#06b6d4', '#3b82f6', '#8b5cf6', '#ec4899']

interface Props {
  data: Expense[]
  onCategoryClick: (categoryLabel: string) => void
  selectedCategory: string | null
}

export function ExpensesByCategoryChart({ data, onCategoryClick, selectedCategory }: Props) {
  const byCategory = data.reduce<Record<string, number>>((acc, e) => {
    const label = EXPENSE_CATEGORY_LABELS[e.category] ?? e.category
    acc[label] = (acc[label] ?? 0) + e.amountPLN
    return acc
  }, {})

  const chartData = Object.entries(byCategory)
    .map(([name, value]) => ({ name, value: Math.round(value) }))
    .filter(d => d.value > 0)
    .sort((a, b) => b.value - a.value)

  if (chartData.length === 0) {
    return (
      <div className="bg-white border border-gray-200 rounded-xl p-4">
        <div className="text-sm font-semibold text-gray-700 mb-4">Wydatki wg kategorii</div>
        <div className="text-gray-400 text-sm text-center py-8">Brak danych</div>
      </div>
    )
  }

  const fmt = (v: number) => v.toLocaleString('pl-PL', { maximumFractionDigits: 0 }) + ' PLN'

  return (
    <div className="bg-white border border-gray-200 rounded-xl p-4">
      <div className="flex items-center justify-between mb-4">
        <span className="text-sm font-semibold text-gray-700">Wydatki wg kategorii</span>
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
            onClick={(sliceData) => { if (sliceData?.name) onCategoryClick(sliceData.name as string) }}
            style={{ cursor: 'pointer' }}
          >
            {chartData.map((entry, i) => (
              <Cell
                key={i}
                fill={COLORS[i % COLORS.length]}
                opacity={selectedCategory && selectedCategory !== entry.name ? 0.4 : 1}
                stroke={selectedCategory === entry.name ? '#1e293b' : 'none'}
                strokeWidth={selectedCategory === entry.name ? 2 : 0}
              />
            ))}
          </Pie>
          <Tooltip
            formatter={(value) => [fmt(Number(value ?? 0)), 'Wydatek']}
            contentStyle={{ fontSize: 12, borderRadius: 8 }}
          />
          <Legend
            formatter={(value) => (
              <span
                className="text-xs cursor-pointer"
                style={{ color: selectedCategory && selectedCategory !== value ? '#9ca3af' : '#4b5563' }}
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
