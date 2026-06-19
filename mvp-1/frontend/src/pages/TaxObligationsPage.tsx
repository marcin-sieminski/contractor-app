import { useState, useId } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { AlertTriangle, CheckCircle2, Clock, Plus, Trash2, XCircle } from 'lucide-react'
import { deleteTaxPayment, getTaxObligations, recordTaxPayment } from '../api/taxObligations'
import { TAX_FORM_LABELS, ZUS_STAGE_LABELS } from '../types/forecast'
import type { TaxFormKey, ZusStageKey } from '../types/forecast'
import type { MonthObligation, TaxPaymentType } from '../types/taxObligations'

const TAX_FORMS = Object.keys(TAX_FORM_LABELS) as TaxFormKey[]
const ZUS_STAGES = Object.keys(ZUS_STAGE_LABELS) as ZusStageKey[]

const fmt = (v: number) =>
  v.toLocaleString('pl-PL', { maximumFractionDigits: 0 }) + ' PLN'

const selectCls =
  'mt-1 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm ' +
  'text-gray-900 dark:text-gray-100 bg-white dark:bg-gray-800'

const PAYMENT_TYPE_LABELS: Record<TaxPaymentType, string> = {
  PIT: 'Zaliczka PIT',
  ZusSocial: 'ZUS społeczny',
  ZusHealth: 'ZUS zdrowotny',
  VAT: 'VAT',
}

const STATUS_CONFIG = {
  future: { label: 'Przyszły', icon: Clock, cls: 'text-gray-400 dark:text-gray-500', rowCls: '' },
  paid: { label: 'Zapłacone', icon: CheckCircle2, cls: 'text-green-600 dark:text-green-400', rowCls: 'bg-green-50/30 dark:bg-green-900/10' },
  partial: { label: 'Częściowo', icon: AlertTriangle, cls: 'text-yellow-600 dark:text-yellow-400', rowCls: 'bg-yellow-50/30 dark:bg-yellow-900/10' },
  due: { label: 'Do zapłaty', icon: AlertTriangle, cls: 'text-blue-600 dark:text-blue-400', rowCls: 'bg-blue-50/20 dark:bg-blue-900/10' },
  overdue: { label: 'Zaległe', icon: XCircle, cls: 'text-red-600 dark:text-red-400', rowCls: 'bg-red-50/30 dark:bg-red-900/10' },
} as const

interface AddPaymentFormProps {
  year: number
  month: number
  monthName: string
  onClose: () => void
}

function AddPaymentForm({ year, month, monthName, onClose }: AddPaymentFormProps) {
  const qc = useQueryClient()
  const baseId = useId()
  const [type, setType] = useState<TaxPaymentType>('ZusSocial')
  const [amount, setAmount] = useState('')
  const [paidAt, setPaidAt] = useState(new Date().toISOString().slice(0, 10))
  const [notes, setNotes] = useState('')

  const mutation = useMutation({
    mutationFn: () =>
      recordTaxPayment(year, month, {
        type,
        amount: parseFloat(amount),
        paidAt: paidAt || null,
        notes: notes || null,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tax-obligations'] })
      onClose()
    },
  })

  return (
    <div className="mt-3 p-3 bg-gray-50 dark:bg-gray-800/60 rounded-lg border border-gray-200 dark:border-gray-700">
      <p className="text-xs font-semibold text-gray-500 dark:text-gray-400 mb-2 uppercase tracking-wide">
        Dodaj wpłatę za {monthName}
      </p>
      <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
        <div>
          <label htmlFor={`${baseId}-type`} className="text-xs text-gray-500 dark:text-gray-400">Rodzaj</label>
          <select
            id={`${baseId}-type`}
            className="mt-0.5 w-full border border-gray-300 dark:border-gray-600 rounded px-2 py-1.5 text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
            value={type}
            onChange={e => setType(e.target.value as TaxPaymentType)}
          >
            {(Object.keys(PAYMENT_TYPE_LABELS) as TaxPaymentType[]).map(t => (
              <option key={t} value={t}>{PAYMENT_TYPE_LABELS[t]}</option>
            ))}
          </select>
        </div>
        <div>
          <label htmlFor={`${baseId}-amount`} className="text-xs text-gray-500 dark:text-gray-400">Kwota (PLN)</label>
          <input
            id={`${baseId}-amount`}
            type="number"
            min="0"
            step="0.01"
            value={amount}
            onChange={e => setAmount(e.target.value)}
            placeholder="np. 1803"
            className="mt-0.5 w-full border border-gray-300 dark:border-gray-600 rounded px-2 py-1.5 text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
          />
        </div>
        <div>
          <label htmlFor={`${baseId}-date`} className="text-xs text-gray-500 dark:text-gray-400">Data wpłaty</label>
          <input
            id={`${baseId}-date`}
            type="date"
            value={paidAt}
            onChange={e => setPaidAt(e.target.value)}
            className="mt-0.5 w-full border border-gray-300 dark:border-gray-600 rounded px-2 py-1.5 text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
          />
        </div>
        <div>
          <label htmlFor={`${baseId}-notes`} className="text-xs text-gray-500 dark:text-gray-400">Uwagi</label>
          <input
            id={`${baseId}-notes`}
            type="text"
            value={notes}
            onChange={e => setNotes(e.target.value)}
            placeholder="opcjonalnie"
            className="mt-0.5 w-full border border-gray-300 dark:border-gray-600 rounded px-2 py-1.5 text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
          />
        </div>
      </div>
      <div className="flex gap-2 mt-2">
        <button
          onClick={() => mutation.mutate()}
          disabled={!amount || parseFloat(amount) <= 0 || mutation.isPending}
          className="text-sm px-3 py-1.5 rounded bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50 transition-colors"
        >
          {mutation.isPending ? 'Zapisuję…' : 'Zapisz wpłatę'}
        </button>
        <button
          onClick={onClose}
          className="text-sm px-3 py-1.5 rounded border border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
        >
          Anuluj
        </button>
      </div>
    </div>
  )
}

interface MonthRowProps {
  month: MonthObligation
  year: number
}

function MonthRow({ month, year }: MonthRowProps) {
  const qc = useQueryClient()
  const [expanded, setExpanded] = useState(false)
  const [addingPayment, setAddingPayment] = useState(false)

  const cfg = STATUS_CONFIG[month.status]
  const StatusIcon = cfg.icon
  const remaining = month.totalDue - month.totalPaid

  const deleteMutation = useMutation({
    mutationFn: deleteTaxPayment,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['tax-obligations'] }),
  })

  return (
    <div className={`border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden ${cfg.rowCls}`}>
      <button
        onClick={() => setExpanded(e => !e)}
        className="w-full text-left px-4 py-3 flex items-center gap-3 hover:bg-black/5 dark:hover:bg-white/5 transition-colors"
      >
        <StatusIcon size={16} className={cfg.cls} />
        <span className="font-medium text-gray-800 dark:text-gray-200 w-24 capitalize">
          {month.monthName}
        </span>
        <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${cfg.cls} bg-current/10`}>
          {cfg.label}
        </span>
        <div className="flex-1 hidden sm:flex items-center gap-6 justify-end text-sm text-gray-500 dark:text-gray-400">
          <span>PIT: <b className="text-gray-800 dark:text-gray-200">{fmt(month.pitDue)}</b></span>
          <span>ZUS: <b className="text-gray-800 dark:text-gray-200">{fmt(month.zusSocialDue + month.zusHealthDue)}</b></span>
          <span>VAT: <b className="text-gray-800 dark:text-gray-200">{fmt(month.vatDue)}</b></span>
          <span className="border-l border-gray-300 dark:border-gray-600 pl-4">
            Łącznie: <b className="text-gray-900 dark:text-gray-100">{fmt(month.totalDue)}</b>
          </span>
          {month.totalPaid > 0 && (
            <span className="text-green-700 dark:text-green-400">
              Zapł.: <b>{fmt(month.totalPaid)}</b>
            </span>
          )}
          {remaining > 0 && month.status !== 'future' && (
            <span className="text-red-600 dark:text-red-400">
              Pozostało: <b>{fmt(remaining)}</b>
            </span>
          )}
        </div>
        <span className="text-gray-400 ml-2">{expanded ? '▲' : '▼'}</span>
      </button>

      {expanded && (
        <div className="px-4 pb-4 border-t border-gray-200 dark:border-gray-700">
          {/* Tabela zobowiązań */}
          <div className="mt-3 grid grid-cols-2 sm:grid-cols-4 gap-3 text-sm">
            {[
              { label: 'Zaliczka PIT', due: month.pitDue, paid: month.pitPaid, dueDate: month.pitDueDate },
              { label: 'ZUS społeczny', due: month.zusSocialDue, paid: month.zusSocialPaid, dueDate: month.zusDueDate },
              { label: 'ZUS zdrowotny', due: month.zusHealthDue, paid: month.zusHealthPaid, dueDate: month.zusDueDate },
              { label: 'VAT', due: month.vatDue, paid: month.vatPaid, dueDate: month.vatDueDate },
            ].map(({ label, due, paid, dueDate }) => (
              <div key={label} className="bg-white dark:bg-gray-800/50 rounded-lg p-3 border border-gray-100 dark:border-gray-700">
                <div className="text-xs text-gray-500 dark:text-gray-400">{label}</div>
                <div className="font-semibold text-gray-900 dark:text-gray-100">{fmt(due)}</div>
                <div className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">termin: {dueDate}</div>
                {paid > 0 && (
                  <div className="text-xs text-green-600 dark:text-green-400 mt-1">zapł.: {fmt(paid)}</div>
                )}
              </div>
            ))}
          </div>

          {/* Zarejestrowane wpłaty */}
          {month.payments.length > 0 && (
            <div className="mt-3">
              <p className="text-xs font-semibold text-gray-500 dark:text-gray-400 mb-1.5 uppercase tracking-wide">
                Zarejestrowane wpłaty
              </p>
              <div className="space-y-1">
                {month.payments.map(p => (
                  <div
                    key={p.id}
                    className="flex items-center gap-3 text-sm bg-white dark:bg-gray-800/50 rounded px-3 py-2 border border-gray-100 dark:border-gray-700"
                  >
                    <span className="text-gray-500 dark:text-gray-400 w-28">{PAYMENT_TYPE_LABELS[p.type]}</span>
                    <span className="font-medium text-gray-900 dark:text-gray-100">{fmt(p.amount)}</span>
                    {p.paidAt && <span className="text-gray-500 dark:text-gray-400 text-xs">{p.paidAt}</span>}
                    {p.notes && <span className="text-gray-500 dark:text-gray-400 text-xs italic truncate">{p.notes}</span>}
                    <button
                      onClick={() => deleteMutation.mutate(p.id)}
                      disabled={deleteMutation.isPending}
                      className="ml-auto text-red-500 hover:text-red-600 transition-colors disabled:opacity-40"
                      aria-label="Usuń wpłatę"
                      title="Usuń wpłatę"
                    >
                      <Trash2 size={14} aria-hidden="true" />
                    </button>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Formularz dodawania wpłaty */}
          {addingPayment ? (
            <AddPaymentForm
              year={year}
              month={month.month}
              monthName={month.monthName}
              onClose={() => setAddingPayment(false)}
            />
          ) : (
            month.status !== 'future' && (
              <button
                onClick={() => setAddingPayment(true)}
                className="mt-3 flex items-center gap-1.5 text-sm text-blue-600 dark:text-blue-400 hover:underline"
              >
                <Plus size={14} />
                Dodaj wpłatę
              </button>
            )
          )}
        </div>
      )}
    </div>
  )
}

export function TaxObligationsPage() {
  const currentYear = new Date().getFullYear()
  const filterId = useId()
  const [year, setYear] = useState(currentYear)
  const [taxForm, setTaxForm] = useState<TaxFormKey>('liniowy')
  const [zusStage, setZusStage] = useState<ZusStageKey>('pelny')

  const { data, isLoading, isError } = useQuery({
    queryKey: ['tax-obligations', year, taxForm, zusStage],
    queryFn: () => getTaxObligations(year, taxForm, zusStage),
  })

  const overdue = data?.months.filter(m => m.status === 'overdue') ?? []
  const due = data?.months.filter(m => m.status === 'due') ?? []

  return (
    <div className="p-6 max-w-7xl mx-auto">
      {/* Nagłówek */}
      <div className="flex flex-wrap items-end gap-4 mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">
            Zobowiązania podatkowe
          </h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
            Naliczone ZUS, PIT i VAT vs. faktyczne wpłaty
          </p>
        </div>
        <div className="flex flex-wrap gap-3 ml-auto">
          <div className="flex flex-col">
            <label htmlFor={`${filterId}-year`} className="text-xs text-gray-500 dark:text-gray-400">Rok</label>
            <select
              id={`${filterId}-year`}
              className={selectCls}
              value={year}
              onChange={e => setYear(Number(e.target.value))}
            >
              {[currentYear - 1, currentYear, currentYear + 1].map(y => (
                <option key={y} value={y}>{y}</option>
              ))}
            </select>
          </div>
          <div className="flex flex-col">
            <label htmlFor={`${filterId}-form`} className="text-xs text-gray-500 dark:text-gray-400">Forma</label>
            <select
              id={`${filterId}-form`}
              className={selectCls}
              value={taxForm}
              onChange={e => setTaxForm(e.target.value as TaxFormKey)}
            >
              {TAX_FORMS.map(f => (
                <option key={f} value={f}>{TAX_FORM_LABELS[f]}</option>
              ))}
            </select>
          </div>
          <div className="flex flex-col">
            <label htmlFor={`${filterId}-zus`} className="text-xs text-gray-500 dark:text-gray-400">Etap ZUS</label>
            <select
              id={`${filterId}-zus`}
              className={selectCls}
              value={zusStage}
              onChange={e => setZusStage(e.target.value as ZusStageKey)}
            >
              {ZUS_STAGES.map(s => (
                <option key={s} value={s}>{ZUS_STAGE_LABELS[s]}</option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Alerty */}
      {overdue.length > 0 && (
        <div className="mb-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg flex items-start gap-2">
          <XCircle size={16} className="text-red-600 dark:text-red-400 mt-0.5 shrink-0" />
          <p className="text-sm text-red-700 dark:text-red-300">
            <b>Zaległości:</b> {overdue.map(m => m.monthName).join(', ')} — termin minął, brak pełnej wpłaty.
          </p>
        </div>
      )}
      {due.length > 0 && (
        <div className="mb-4 p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg flex items-start gap-2">
          <AlertTriangle size={16} className="text-blue-600 dark:text-blue-400 mt-0.5 shrink-0" />
          <p className="text-sm text-blue-700 dark:text-blue-300">
            <b>Do zapłaty:</b> {due.map(m => m.monthName).join(', ')}.
          </p>
        </div>
      )}

      {/* Podsumowanie roku */}
      {data && (
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 mb-6">
          {[
            { label: 'Łącznie naliczone', value: data.summary.totalDue, cls: 'text-gray-900 dark:text-gray-100' },
            { label: 'Łącznie zapłacone', value: data.summary.totalPaid, cls: 'text-green-700 dark:text-green-400' },
            { label: 'Pozostało', value: data.summary.totalRemaining, cls: data.summary.totalRemaining > 0 ? 'text-red-600 dark:text-red-400' : 'text-green-700 dark:text-green-400' },
            { label: 'PIT w roku', value: data.summary.totalPitDue, cls: 'text-gray-700 dark:text-gray-300' },
          ].map(({ label, value, cls }) => (
            <div key={label} className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
              <div className="text-xs text-gray-500 dark:text-gray-400">{label}</div>
              <div className={`text-xl font-bold mt-1 ${cls}`}>{fmt(value)}</div>
            </div>
          ))}
        </div>
      )}

      {/* Miesięczna lista */}
      {isLoading && (
        <div className="text-center py-12 text-gray-500 dark:text-gray-400">Ładowanie…</div>
      )}
      {isError && (
        <div role="alert" className="text-center py-12 text-red-500">Błąd ładowania danych.</div>
      )}
      {data && (
        <div className="space-y-2">
          {data.months.map(m => (
            <MonthRow key={m.month} month={m} year={year} />
          ))}
        </div>
      )}
    </div>
  )
}
