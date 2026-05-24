import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import { getExpenses } from '../api/expenses'
import { ExpenseList } from '../components/expenses/ExpenseList'
import { ExpenseForm } from '../components/expenses/ExpenseForm'
import { EXPENSE_CATEGORY_LABELS } from '../types/expense'
import type { ExpenseCategory } from '../types/expense'

const CATEGORIES = Object.keys(EXPENSE_CATEGORY_LABELS) as ExpenseCategory[]

export function ExpensesPage() {
  const [adding, setAdding] = useState(false)
  const [categoryFilter, setCategoryFilter] = useState('')

  const { data: expenses = [], isLoading } = useQuery({
    queryKey: ['expenses'],
    queryFn: () => getExpenses(),
  })

  const filtered = categoryFilter
    ? expenses.filter(e => e.category === categoryFilter)
    : expenses

  const totalPLN = filtered.reduce((acc, e) => acc + e.amountPLN, 0)
  const vatDeductiblePLN = filtered.filter(e => e.isVatDeductible).reduce((acc, e) => acc + e.amountPLN, 0)

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Wydatki</h1>
        <button
          onClick={() => setAdding(true)}
          className="bg-blue-600 text-white hover:bg-blue-700 px-4 py-2 rounded-lg text-sm font-medium flex items-center gap-2"
        >
          <Plus size={16} /> Dodaj wydatek
        </button>
      </div>

      {/* KPI */}
      <div className="grid grid-cols-3 gap-4 mb-6">
        <div className="bg-white border border-gray-200 rounded-xl p-4">
          <div className="text-gray-500 text-sm mb-1">Łączne koszty</div>
          <div className="text-2xl font-bold text-red-500">
            {totalPLN.toLocaleString('pl-PL', { maximumFractionDigits: 2 })} PLN
          </div>
          <div className="text-xs text-gray-400 mt-1">{filtered.length} pozycji</div>
        </div>
        <div className="bg-white border border-gray-200 rounded-xl p-4">
          <div className="text-gray-500 text-sm mb-1">Z odliczeniem VAT</div>
          <div className="text-2xl font-bold text-green-600">
            {vatDeductiblePLN.toLocaleString('pl-PL', { maximumFractionDigits: 2 })} PLN
          </div>
          <div className="text-xs text-gray-400 mt-1">
            {filtered.filter(e => e.isVatDeductible).length} pozycji
          </div>
        </div>
        <div className="bg-white border border-gray-200 rounded-xl p-4">
          <div className="text-gray-500 text-sm mb-1">Bez odliczenia VAT</div>
          <div className="text-2xl font-bold text-gray-700">
            {(totalPLN - vatDeductiblePLN).toLocaleString('pl-PL', { maximumFractionDigits: 2 })} PLN
          </div>
          <div className="text-xs text-gray-400 mt-1">
            {filtered.filter(e => !e.isVatDeductible).length} pozycji
          </div>
        </div>
      </div>

      {/* Filtry */}
      <div className="flex items-center gap-3 mb-4">
        <select
          value={categoryFilter}
          onChange={e => setCategoryFilter(e.target.value)}
          className="border border-gray-300 rounded-lg px-3 py-2 text-sm"
        >
          <option value="">Wszystkie kategorie</option>
          {CATEGORIES.map(c => (
            <option key={c} value={c}>{EXPENSE_CATEGORY_LABELS[c]}</option>
          ))}
        </select>
        {categoryFilter && (
          <button onClick={() => setCategoryFilter('')} className="text-xs text-gray-400 hover:text-gray-600">
            Wyczyść filtr
          </button>
        )}
      </div>

      {/* Lista */}
      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
        {isLoading
          ? <div className="text-gray-400 text-sm p-6 text-center">Ładowanie...</div>
          : <ExpenseList expenses={filtered} />
        }
      </div>

      {adding && <ExpenseForm onClose={() => setAdding(false)} />}
    </div>
  )
}
