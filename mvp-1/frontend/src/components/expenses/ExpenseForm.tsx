import { useRef, useState, useId } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Sparkles, Cpu, ScanLine, Loader2, Paperclip, FileText } from 'lucide-react'
import { createExpense, updateExpense, scanReceipt } from '../../api/expenses'
import { useDialogClose } from '../../hooks/useDialogClose'
import { EXPENSE_CATEGORY_LABELS } from '../../types/expense'
import type { Expense, ExpenseCategory, OcrProvider, ReceiptExtraction } from '../../types/expense'
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
  vendorName: string
  vendorNip: string
  netAmount: string
  vatAmount: string
}

const inputCls = "border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"

export function ExpenseForm({ expense, onClose }: Props) {
  const titleId = useId()
  useDialogClose(onClose)
  const qc = useQueryClient()
  const isEdit = !!expense
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [form, setForm] = useState<FormState>({
    date: expense?.date ?? format(new Date(), 'yyyy-MM-dd'),
    category: expense?.category ?? 'Other',
    description: expense?.description ?? '',
    amount: expense?.amount.toString() ?? '',
    currency: expense?.currency ?? 'PLN',
    exchangeRate: expense?.exchangeRate?.toString() ?? '',
    isVatDeductible: expense?.isVatDeductible ?? false,
    receiptNumber: expense?.receiptNumber ?? '',
    vendorName: expense?.vendorName ?? '',
    vendorNip: expense?.vendorNip ?? '',
    netAmount: expense?.netAmount?.toString() ?? '',
    vatAmount: expense?.vatAmount?.toString() ?? '',
  })

  const [provider, setProvider] = useState<OcrProvider>('claude')
  const [receiptId, setReceiptId] = useState<string | undefined>(expense?.receiptId)
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [previewIsPdf, setPreviewIsPdf] = useState(false)
  const [lowConfidence, setLowConfidence] = useState(false)

  const set = <K extends keyof FormState>(k: K, v: FormState[K]) =>
    setForm(f => ({ ...f, [k]: v }))

  const applyExtraction = (x: ReceiptExtraction) => {
    setForm(f => ({
      ...f,
      date: x.date || f.date,
      description: f.description || x.vendorName || f.description,
      amount: x.grossAmount != null ? String(x.grossAmount) : f.amount,
      currency: x.currency || f.currency,
      category: x.category && (CATEGORIES as string[]).includes(x.category) ? x.category : f.category,
      isVatDeductible: x.isVatDeductible ?? f.isVatDeductible,
      receiptNumber: x.receiptNumber || f.receiptNumber,
      vendorName: x.vendorName || f.vendorName,
      vendorNip: x.vendorNip || f.vendorNip,
      netAmount: x.netAmount != null ? String(x.netAmount) : f.netAmount,
      vatAmount: x.vatAmount != null ? String(x.vatAmount) : f.vatAmount,
    }))
    setLowConfidence(x.categoryConfidence != null && x.categoryConfidence < 0.5)
  }

  const scanMutation = useMutation({
    mutationFn: (file: File) => scanReceipt(file, provider),
    onSuccess: (data) => {
      setReceiptId(data.receiptId)
      applyExtraction(data.extracted)
    },
  })

  const onFile = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    setPreviewIsPdf(file.type === 'application/pdf')
    setPreviewUrl(prev => { if (prev) URL.revokeObjectURL(prev); return URL.createObjectURL(file) })
    scanMutation.mutate(file)
    e.target.value = '' // pozwól ponownie wybrać ten sam plik
  }

  const payload = () => ({
    date: form.date,
    category: form.category,
    description: form.description,
    amount: parseFloat(form.amount) || 0,
    currency: form.currency,
    exchangeRate: form.currency !== 'PLN' && form.exchangeRate ? parseFloat(form.exchangeRate) : undefined,
    isVatDeductible: form.isVatDeductible,
    receiptNumber: form.receiptNumber || undefined,
    vendorName: form.vendorName || undefined,
    vendorNip: form.vendorNip || undefined,
    netAmount: form.netAmount ? parseFloat(form.netAmount) : undefined,
    vatAmount: form.vatAmount ? parseFloat(form.vatAmount) : undefined,
    receiptId: receiptId || undefined,
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

  const providerBtn = (p: OcrProvider, label: string, Icon: typeof Sparkles) => (
    <button
      type="button"
      onClick={() => setProvider(p)}
      className={`flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-medium transition ${
        provider === p
          ? 'bg-blue-600 text-white'
          : 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
      }`}
    >
      <Icon size={13} aria-hidden="true" /> {label}
    </button>
  )

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-0 sm:p-4">
      <div role="dialog" aria-modal="true" aria-labelledby={titleId} className="bg-white dark:bg-gray-800 shadow-xl w-full h-full sm:h-auto max-w-none sm:max-w-md max-h-none sm:max-h-[90vh] rounded-none sm:rounded-xl overflow-y-auto">
        <h2 id={titleId} className="text-lg font-semibold px-6 pt-5 pb-4 text-gray-900 dark:text-gray-100">
          {isEdit ? 'Edytuj wydatek' : 'Dodaj wydatek'}
        </h2>

        <div className="px-6 space-y-3 pb-4">
          {/* --- Skanowanie paragonu/faktury --- */}
          <div className="border border-dashed border-blue-300 dark:border-blue-700 rounded-lg p-3 bg-blue-50/50 dark:bg-blue-900/10">
            <div className="flex items-center justify-between mb-2">
              <span className="text-xs font-medium text-gray-600 dark:text-gray-300">Rozpoznaj ze zdjęcia</span>
              <div className="flex gap-1">
                {providerBtn('claude', 'Claude', Sparkles)}
                {providerBtn('ollama', 'Lokalnie', Cpu)}
              </div>
            </div>

            <input
              ref={fileInputRef}
              type="file"
              aria-label="Plik paragonu lub faktury"
              accept="image/jpeg,image/png,image/webp,application/pdf"
              onChange={onFile}
              className="hidden"
            />

            <div className="flex items-center gap-3">
              <button
                type="button"
                onClick={() => fileInputRef.current?.click()}
                disabled={scanMutation.isPending}
                className="flex items-center gap-2 px-3 py-2 text-sm bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-600 disabled:opacity-50 text-gray-700 dark:text-gray-200"
              >
                {scanMutation.isPending
                  ? <><Loader2 size={15} className="animate-spin" aria-hidden="true" /> Rozpoznaję…</>
                  : <><ScanLine size={15} aria-hidden="true" /> Skanuj paragon/fakturę</>}
              </button>

              {previewUrl && !previewIsPdf && (
                <img src={previewUrl} alt="podgląd" className="h-12 w-12 object-cover rounded border border-gray-200 dark:border-gray-600" />
              )}
              {previewUrl && previewIsPdf && (
                <span className="flex items-center gap-1.5 text-xs text-gray-600 dark:text-gray-300 border border-gray-200 dark:border-gray-600 rounded px-2 py-1.5">
                  <FileText size={14} className="text-red-500" aria-hidden="true" /> PDF
                </span>
              )}
              {!previewUrl && receiptId && (
                <span className="flex items-center gap-1 text-xs text-green-600 dark:text-green-400"><Paperclip size={12} aria-hidden="true" /> skan</span>
              )}
            </div>

            {scanMutation.isError && (
              <p role="alert" className="text-red-600 dark:text-red-400 text-xs mt-2">
                {((scanMutation.error as { response?: { data?: { message?: string } } })?.response?.data?.message)
                  ?? 'Nie udało się rozpoznać dokumentu. Uzupełnij dane ręcznie.'}
              </p>
            )}
            {scanMutation.isSuccess && (
              <p role="status" className="text-green-600 dark:text-green-400 text-xs mt-2">Rozpoznano — sprawdź i popraw dane przed zapisem.</p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <input
              type="date"
              aria-label="Data"
              value={form.date}
              onChange={e => set('date', e.target.value)}
              className={inputCls}
            />
            <select
              aria-label="Kategoria"
              value={form.category}
              onChange={e => { set('category', e.target.value as ExpenseCategory); setLowConfidence(false) }}
              className={inputCls + (lowConfidence ? ' ring-2 ring-amber-400' : '')}
              title={lowConfidence ? 'Niska pewność rozpoznania kategorii — sprawdź' : undefined}
            >
              {CATEGORIES.map(c => (
                <option key={c} value={c}>{EXPENSE_CATEGORY_LABELS[c]}</option>
              ))}
            </select>
          </div>

          <input
            type="text"
            aria-label="Opis"
            placeholder="Opis *"
            value={form.description}
            onChange={e => set('description', e.target.value)}
            className={'w-full ' + inputCls}
          />

          <div className="grid grid-cols-2 gap-3">
            <input
              type="text"
              aria-label="Sprzedawca"
              placeholder="Sprzedawca"
              value={form.vendorName}
              onChange={e => set('vendorName', e.target.value)}
              className={inputCls}
            />
            <input
              type="text"
              aria-label="NIP sprzedawcy"
              placeholder="NIP sprzedawcy"
              value={form.vendorNip}
              onChange={e => set('vendorNip', e.target.value)}
              className={inputCls}
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <input
              type="number"
              aria-label="Kwota brutto"
              placeholder="Kwota brutto *"
              min="0"
              step="0.01"
              value={form.amount}
              onChange={e => set('amount', e.target.value)}
              className={inputCls}
            />
            <select
              aria-label="Waluta"
              value={form.currency}
              onChange={e => set('currency', e.target.value)}
              className={inputCls}
            >
              {CURRENCIES.map(c => <option key={c} value={c}>{c}</option>)}
            </select>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <input
              type="number"
              aria-label="Kwota netto"
              placeholder="Netto"
              min="0"
              step="0.01"
              value={form.netAmount}
              onChange={e => set('netAmount', e.target.value)}
              className={inputCls}
            />
            <input
              type="number"
              aria-label="Kwota VAT"
              placeholder="VAT"
              min="0"
              step="0.01"
              value={form.vatAmount}
              onChange={e => set('vatAmount', e.target.value)}
              className={inputCls}
            />
          </div>

          {form.currency !== 'PLN' && (
            <input
              type="number"
              aria-label={`Kurs ${form.currency}/PLN`}
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
            aria-label="Nr faktury lub paragonu"
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
            <p role="alert" className="text-red-600 dark:text-red-400 text-xs">{(mutation.error as Error)?.message ?? 'Błąd zapisu.'}</p>
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
