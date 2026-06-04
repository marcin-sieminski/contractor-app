import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createExpense, updateExpense } from '../../api/expenses'
import { EXPENSE_CATEGORY_LABELS } from '../../types/expense'
import type { Expense, ExpenseCategory } from '../../types/expense'
import { format } from 'date-fns'

const CATEGORIES = Object.keys(EXPENSE_CATEGORY_LABELS) as ExpenseCategory[]
const CURRENCIES = ['PLN', 'EUR', 'USD', 'GBP', 'CHF']

interface Props {
  expense?: Expense
  onClose: () => void
}

interface FormState {
  date: string
  category: ExpenseCategory
  description: string
  amount: string
  currency: string
  exchangeRate: string
  isVatDeductible: boolean
  receiptNumber: string
}

const inputCls = "border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"

export function ExpenseForm({ expense, onClose }: Props) {
  const qc = useQueryClient()
  const isEdit = !!expense

  const [form, setForm] = useState<FormState>({
    date: expense?.date ?? format(new Date(), 'yyyy-MM-dd'),
    category: expense?.category ?? 'Other',
    description: expense?.description ?? '',
    amount: expense?.amount.toString() ?? '',
    currency: expense?.currency ?? 'PLN',
    exchangeRate: expense?.exchangeRate?.toString() ?? '',
    isVatDeductible: expense?.isVatDeductible ?? false,
    receiptNumber: expense?.receiptNumber ?? '',
  })

  const set = <K extends keyof FormState>(k: K, v: FormState[K]) =>
    setForm(f => ({ ...f, [k]: v }))

  const payload = () => ({
    date: form.date,
    category: form.category,
    description: form.description,
    amount: parseFloat(form.amount) || 0,
    currency: form.currency,
    exchangeRate: form.currency !== 'PLN' && form.exchangeRate ? parseFloat(form.exchangeRate) : undefined,
    isVatDeductible: form.isVatDeductible,
    receiptNumber: form.receiptNumber || undefined,
  })

  const mutation = useMutation({
    mutationFn: () => isEdit
      ? updateExpense(expense!.id, payload())
      : createExpense(payload()),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['expenses'] })
      onClose()
    },
  })

  const isValid = form.description.trim() && parseFloat(form.amount) > 0 &&
    (form.currency === 'PLN' || parseFloat(form.exchangeRate) > 0)

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-md">
        <h2 className="text-lg font-semibold px-6 pt-5 pb-4 text-gray-900 dark:text-gray-100">
          {isEdit ? 'Edytuj wydatek' : 'Dodaj wydatek'}
        </h2>

        <div className="px-6 space-y-3 pb-4">
          <div className="grid grid-cols-2 gap-3">
            <input
              type="date"
              value={form.date}
              onChange={e => set('date', e.target.value)}
              className={inputCls}
            />
            <select
              value={form.category}
              onChange={e => set('category', e.target.value as ExpenseCategory)}
              className={inputCls}
            >
              {CATEGORIES.map(c => (
                <option key={c} value={c}>{EXPENSE_CATEGORY_LABELS[c]}</option>
              ))}
            </select>
          </div>

          <input
            type="text"
            placeholder="Opis *"
            value={form.description}
            onChange={e => set('description', e.target.value)}
            className={'w-full ' + inputCls}
          />

          <div className="grid grid-cols-2 gap-3">
            <input
              type="number"
              placeholder="Kwota *"
              min="0"
              step="0.01"
              value={form.amount}
              onChange={e => set('amount', e.target.value)}
              className={inputCls}
            />
            <select
              value={form.currency}
              onChange={e => set('currency', e.target.value)}
              className={inputCls}
            >
              {CURRENCIES.map(c => <option key={c} value={c}>{c}</option>)}
            </select>
          </div>

          {form.currency !== 'PLN' && (
            <input
              type="number"
              placeholder={`Kurs ${form.currency}/PLN *`}
              min="0"
              step="0.0001"
              value={form.exchangeRate}
              onChange={e => set('exchangeRate', e.target.value)}
              className={'w-full ' + inputCls}
            />
          )}

          <input
            type="text"
            placeholder="Nr faktury / paragonu (opcjonalnie)"
            value={form.receiptNumber}
            onChange={e => set('receiptNumber', e.target.value)}
            className={'w-full ' + inputCls}
          />

          <label className="flex items-center gap-2 text-sm cursor-pointer">
            <input
              type="checkbox"
              checked={form.isVatDeductible}
              onChange={e => set('isVatDeductible', e.target.checked)}
              className="rounded"
            />
            <span className="text-gray-700 dark:text-gray-200">Koszt z odliczeniem VAT</span>
          </label>

          {mutation.isError && (
            <p className="text-red-600 dark:text-red-400 text-xs">{(mutation.error as Error)?.message ?? 'Błąd zapisu.'}</p>
          )}
        </div>

        <div className="flex gap-2 px-6 pb-5 justify-end">
          <button onClick={onClose} className="px-4 py-2 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg">
            Anuluj
          </button>
          <button
            onClick={() => mutation.mutate()}
            disabled={!isValid || mutation.isPending}
            className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
          >
            {mutation.isPending ? 'Zapisywanie...' : 'Zapisz'}
          </button>
        </div>
      </div>
    </div>
  )
}
