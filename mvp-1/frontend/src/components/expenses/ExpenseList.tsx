import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Pencil, Trash2, CheckCircle } from 'lucide-react'
import { deleteExpense } from '../../api/expenses'
import { ExpenseForm } from './ExpenseForm'
import { EXPENSE_CATEGORY_LABELS } from '../../types/expense'
import type { Expense } from '../../types/expense'
import { format } from 'date-fns'
import { pl } from 'date-fns/locale'

interface Props {
  expenses: Expense[]
}

function groupByMonth(expenses: Expense[]) {
  const map = new Map<string, Expense[]>()
  for (const e of expenses) {
    const key = e.date.slice(0, 7)
    if (!map.has(key)) map.set(key, [])
    map.get(key)!.push(e)
  }
  return map
}

export function ExpenseList({ expenses }: Props) {
  const qc = useQueryClient()
  const [editing, setEditing] = useState<Expense | null>(null)

  const deleteMutation = useMutation({
    mutationFn: deleteExpense,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['expenses'] }),
  })

  if (expenses.length === 0)
    return <div className="text-gray-400 dark:text-gray-500 text-sm text-center py-10">Brak wydatków. Dodaj pierwszy koszt.</div>

  const grouped = groupByMonth(expenses)

  return (
    <>
      <div className="divide-y divide-gray-100 dark:divide-gray-700">
        {[...grouped.entries()].map(([monthKey, items]) => {
          const monthTotal = items.reduce((acc, e) => acc + e.amountPLN, 0)
          const [y, m] = monthKey.split('-').map(Number)
          const monthLabel = format(new Date(y, m - 1, 1), 'LLLL yyyy', { locale: pl })

          return (
            <div key={monthKey}>
              <div className="px-4 py-2 bg-gray-50 dark:bg-gray-900 flex justify-between items-center">
                <span className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide capitalize">
                  {monthLabel}
                </span>
                <span className="text-xs font-semibold text-gray-500 dark:text-gray-400">
                  {monthTotal.toLocaleString('pl-PL', { maximumFractionDigits: 2 })} PLN
                </span>
              </div>
              {items.map(expense => (
                <div key={expense.id} className="flex items-center px-4 py-3 hover:bg-gray-50 dark:hover:bg-gray-700/50 gap-3">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="font-medium text-sm truncate text-gray-900 dark:text-gray-100">{expense.description}</span>
                      <span className="text-xs bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 px-2 py-0.5 rounded-full shrink-0">
                        {EXPENSE_CATEGORY_LABELS[expense.category]}
                      </span>
                      {expense.isVatDeductible && (
                        <span className="text-xs flex items-center gap-1 text-green-600 dark:text-green-400 shrink-0">
                          <CheckCircle size={11} /> VAT
                        </span>
                      )}
                    </div>
                    <div className="text-xs text-gray-400 dark:text-gray-500 mt-0.5 flex gap-2">
                      <span>{format(new Date(expense.date), 'd MMM', { locale: pl })}</span>
                      {expense.receiptNumber && <span>· {expense.receiptNumber}</span>}
                    </div>
                  </div>

                  <div className="text-right shrink-0">
                    <div className="font-mono text-sm font-medium text-gray-800 dark:text-gray-200">
                      {expense.amount.toLocaleString('pl-PL', { maximumFractionDigits: 2 })} {expense.currency}
                    </div>
                    {expense.currency !== 'PLN' && (
                      <div className="text-xs text-gray-400 dark:text-gray-500">
                        ≈ {expense.amountPLN.toLocaleString('pl-PL', { maximumFractionDigits: 2 })} PLN
                      </div>
                    )}
                  </div>

                  <div className="flex items-center gap-1 shrink-0">
                    <button
                      onClick={() => setEditing(expense)}
                      className="text-gray-400 hover:text-blue-500 p-1"
                      title="Edytuj"
                    >
                      <Pencil size={14} />
                    </button>
                    <button
                      onClick={() => deleteMutation.mutate(expense.id)}
                      className="text-gray-400 hover:text-red-500 p-1"
                      title="Usuń"
                    >
                      <Trash2 size={14} />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )
        })}
      </div>

      {editing && <ExpenseForm expense={editing} onClose={() => setEditing(null)} />}
    </>
  )
}
