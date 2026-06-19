import { useId } from 'react'
import { X } from 'lucide-react'
import { format } from 'date-fns'
import { pl } from 'date-fns/locale'
import { useDialogClose } from '../../hooks/useDialogClose'
import type { Expense } from '../../types/expense'
import { EXPENSE_CATEGORY_LABELS } from '../../types/expense'

interface Props {
  categoryLabel: string
  expenses: Expense[]
  onClose: () => void
}

const fmt = (v: number) => v.toLocaleString('pl-PL', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

export function ExpenseCategoryDetailModal({ categoryLabel, expenses, onClose }: Props) {
  const titleId = useId()
  useDialogClose(onClose)
  const catExpenses = expenses
    .filter(e => (EXPENSE_CATEGORY_LABELS[e.category] ?? e.category) === categoryLabel)
    .sort((a, b) => b.date.localeCompare(a.date))

  const totalPLN = catExpenses.reduce((acc, e) => acc + e.amountPLN, 0)

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="bg-white rounded-xl shadow-xl w-full max-w-2xl max-h-[80vh] flex flex-col"
        onClick={e => e.stopPropagation()}
      >
        <div className="flex items-center justify-between px-5 py-4 border-b border-gray-100">
          <div>
            <h2 id={titleId} className="text-base font-semibold">{categoryLabel}</h2>
            <p className="text-xs text-gray-500 mt-0.5">
              {catExpenses.length} {catExpenses.length === 1 ? 'wydatek' : 'wydatków'} · łącznie{' '}
              <span className="font-medium text-red-600">{fmt(totalPLN)} PLN</span>
            </p>
          </div>
          <button onClick={onClose} aria-label="Zamknij" className="text-gray-500 hover:text-gray-700 p-1">
            <X size={18} aria-hidden="true" />
          </button>
        </div>

        <div className="overflow-y-auto flex-1 px-5 py-3">
          {catExpenses.length === 0 ? (
            <div className="text-gray-500 text-sm text-center py-10">Brak wydatków w tej kategorii.</div>
          ) : (
            <div className="divide-y divide-gray-50 border border-gray-100 rounded-lg overflow-hidden">
              {catExpenses.map(expense => (
                <div key={expense.id} className="flex items-center gap-3 px-3 py-2.5 bg-white hover:bg-gray-50">
                  <span className="text-xs text-gray-500 w-16 shrink-0">
                    {format(new Date(expense.date), 'd MMM yy', { locale: pl })}
                  </span>
                  <span className="flex-1 text-sm text-gray-700 truncate">
                    {expense.description}
                    {expense.receiptNumber && (
                      <span className="ml-1.5 text-xs text-gray-500">#{expense.receiptNumber}</span>
                    )}
                  </span>
                  <div className="flex items-center gap-2 shrink-0">
                    {expense.isVatDeductible && (
                      <span className="text-xs bg-green-50 text-green-700 px-1.5 py-0.5 rounded">VAT</span>
                    )}
                    <div className="text-right">
                      {expense.currency !== 'PLN' && (
                        <div className="text-xs text-gray-500">
                          {fmt(expense.amount)} {expense.currency}
                        </div>
                      )}
                      <div className="font-mono text-sm text-gray-700">{fmt(expense.amountPLN)} PLN</div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
