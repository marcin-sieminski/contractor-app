import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getInvoiceById, updateInvoice } from '../../api/invoices'
import { X, Plus, Trash2 } from 'lucide-react'

interface Props {
  invoiceId: string
  onClose: () => void
  onSaved: () => void
}

const VAT_OPTIONS = [
  { value: 0, label: 'Krajowy 23% VAT' },
  { value: 1, label: 'Odwrotne obciążenie (UE)' },
  { value: 2, label: 'Poza UE (np.)' },
  { value: 3, label: 'Zwolniony (ZW)' }
]

const CURRENCY_OPTIONS = [
  { value: 0, label: 'PLN' },
  { value: 1, label: 'EUR' },
  { value: 2, label: 'USD' },
  { value: 3, label: 'GBP' },
  { value: 4, label: 'CHF' }
]

const VAT_LABEL: Record<string, string> = {
  Domestic23: 'Krajowy 23% VAT',
  ReverseCharge: 'Odwrotne obciążenie (UE)',
  OutsideEU: 'Poza UE (np.)',
  Exempt: 'Zwolniony (ZW)'
}

const VAT_TO_INDEX: Record<string, number> = {
  Domestic23: 0, ReverseCharge: 1, OutsideEU: 2, Exempt: 3
}

const CURRENCY_TO_INDEX: Record<string, number> = {
  PLN: 0, EUR: 1, USD: 2, GBP: 3, CHF: 4
}

interface LineItemForm {
  lineNumber: number
  description: string
  quantity: string
  unit: string
  unitPrice: string
}

export function InvoiceEditModal({ invoiceId, onClose, onSaved }: Props) {
  const qc = useQueryClient()
  const { data: invoice, isLoading } = useQuery({
    queryKey: ['invoice', invoiceId],
    queryFn: () => getInvoiceById(invoiceId)
  })

  const [issueDate, setIssueDate] = useState('')
  const [serviceDate, setServiceDate] = useState('')
  const [dueDate, setDueDate] = useState('')
  const [vatTreatment, setVatTreatment] = useState(0)
  const [currency, setCurrency] = useState(0)
  const [lines, setLines] = useState<LineItemForm[]>([])
  const [initialized, setInitialized] = useState(false)

  // Initialize form once invoice loads
  if (invoice && !initialized) {
    setIssueDate(invoice.issueDate)
    setServiceDate(invoice.serviceDate)
    setDueDate(invoice.dueDate)
    setVatTreatment(VAT_TO_INDEX[invoice.vatTreatment] ?? 0)
    setCurrency(CURRENCY_TO_INDEX[invoice.currency] ?? 0)
    setLines(invoice.lineItems.map(l => ({
      lineNumber: l.lineNumber,
      description: l.description,
      quantity: String(l.quantity),
      unit: l.unit,
      unitPrice: String(l.unitPrice)
    })))
    setInitialized(true)
  }

  const vatRate = vatTreatment === 0 ? 0.23 : 0

  const mutation = useMutation({
    mutationFn: () => updateInvoice({
      invoiceId,
      issueDate,
      serviceDate,
      dueDate,
      vatTreatment,
      currency,
      lineItems: lines.map((l, i) => ({
        lineNumber: i + 1,
        description: l.description,
        quantity: parseFloat(l.quantity) || 0,
        unit: l.unit,
        unitPrice: parseFloat(l.unitPrice) || 0
      }))
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['invoices'] })
      qc.invalidateQueries({ queryKey: ['invoice', invoiceId] })
      onSaved()
    }
  })

  const addLine = () => setLines(ls => [
    ...ls,
    { lineNumber: ls.length + 1, description: '', quantity: '1', unit: 'godz.', unitPrice: '' }
  ])

  const removeLine = (idx: number) =>
    setLines(ls => ls.filter((_, i) => i !== idx).map((l, i) => ({ ...l, lineNumber: i + 1 })))

  const updateLine = (idx: number, field: keyof LineItemForm, value: string) =>
    setLines(ls => ls.map((l, i) => i === idx ? { ...l, [field]: value } : l))

  const previewNet = (l: LineItemForm) => {
    const q = parseFloat(l.quantity) || 0
    const p = parseFloat(l.unitPrice) || 0
    return q * p
  }

  const totalNet = lines.reduce((acc, l) => acc + previewNet(l), 0)
  const totalGross = totalNet * (1 + vatRate)

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-xl shadow-2xl w-full max-w-2xl max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
          <h2 className="text-lg font-semibold">
            Edytuj fakturę {invoice?.invoiceNumber}
          </h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 p-1">
            <X size={20} />
          </button>
        </div>

        <div className="overflow-y-auto flex-1 px-6 py-4 space-y-5">
          {isLoading && <div className="text-gray-400 text-center py-8">Ładowanie...</div>}

          {initialized && (
            <>
              {/* Dates */}
              <div className="grid grid-cols-3 gap-3">
                <div>
                  <label className="text-xs text-gray-500 mb-1 block">Data wystawienia</label>
                  <input type="date" value={issueDate} onChange={e => setIssueDate(e.target.value)}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-400 outline-none" />
                </div>
                <div>
                  <label className="text-xs text-gray-500 mb-1 block">Data sprzedaży</label>
                  <input type="date" value={serviceDate} onChange={e => setServiceDate(e.target.value)}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-400 outline-none" />
                </div>
                <div>
                  <label className="text-xs text-gray-500 mb-1 block">Termin płatności</label>
                  <input type="date" value={dueDate} onChange={e => setDueDate(e.target.value)}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-400 outline-none" />
                </div>
              </div>

              {/* VAT & Currency */}
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-gray-500 mb-1 block">Traktowanie VAT</label>
                  <select value={vatTreatment} onChange={e => setVatTreatment(Number(e.target.value))}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-400 outline-none">
                    {VAT_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
                  </select>
                </div>
                <div>
                  <label className="text-xs text-gray-500 mb-1 block">Waluta</label>
                  <select value={currency} onChange={e => setCurrency(Number(e.target.value))}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-400 outline-none">
                    {CURRENCY_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
                  </select>
                </div>
              </div>

              {/* Line items */}
              <div>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-xs font-semibold text-gray-500 uppercase tracking-wide">Pozycje</span>
                  <button onClick={addLine}
                    className="flex items-center gap-1 text-xs text-blue-600 hover:text-blue-800 px-2 py-1 rounded hover:bg-blue-50">
                    <Plus size={12} /> Dodaj pozycję
                  </button>
                </div>

                <div className="space-y-2">
                  {lines.map((l, idx) => (
                    <div key={idx} className="bg-gray-50 rounded-lg p-3 space-y-2">
                      <div className="flex gap-2">
                        <input
                          placeholder="Opis usługi"
                          value={l.description}
                          onChange={e => updateLine(idx, 'description', e.target.value)}
                          className="flex-1 border border-gray-300 rounded px-2 py-1.5 text-sm focus:ring-2 focus:ring-blue-400 outline-none"
                        />
                        {lines.length > 1 && (
                          <button onClick={() => removeLine(idx)} className="text-gray-400 hover:text-red-500 flex-shrink-0">
                            <Trash2 size={14} />
                          </button>
                        )}
                      </div>
                      <div className="flex gap-2 items-center">
                        <div className="flex-1">
                          <label className="text-xs text-gray-400">Ilość</label>
                          <input type="number" value={l.quantity} min="0" step="0.01"
                            onChange={e => updateLine(idx, 'quantity', e.target.value)}
                            className="w-full border border-gray-300 rounded px-2 py-1.5 text-sm font-mono focus:ring-2 focus:ring-blue-400 outline-none" />
                        </div>
                        <div className="w-20">
                          <label className="text-xs text-gray-400">Jedn.</label>
                          <input value={l.unit} onChange={e => updateLine(idx, 'unit', e.target.value)}
                            className="w-full border border-gray-300 rounded px-2 py-1.5 text-sm focus:ring-2 focus:ring-blue-400 outline-none" />
                        </div>
                        <div className="flex-1">
                          <label className="text-xs text-gray-400">Cena / jedn.</label>
                          <input type="number" value={l.unitPrice} min="0" step="0.01"
                            onChange={e => updateLine(idx, 'unitPrice', e.target.value)}
                            className="w-full border border-gray-300 rounded px-2 py-1.5 text-sm font-mono focus:ring-2 focus:ring-blue-400 outline-none" />
                        </div>
                        <div className="flex-1 text-right">
                          <label className="text-xs text-gray-400">Netto</label>
                          <div className="text-sm font-mono font-medium pt-1.5">
                            {previewNet(l).toFixed(2)}
                          </div>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>

                {/* Summary */}
                <div className="mt-3 bg-blue-50 rounded-lg px-4 py-2 flex justify-between text-sm">
                  <span className="text-gray-600">
                    Netto: <span className="font-mono font-medium">{totalNet.toFixed(2)}</span>
                    {vatRate > 0 && <span className="text-gray-400 ml-2 text-xs">(+ {(totalNet * vatRate).toFixed(2)} VAT)</span>}
                  </span>
                  <span className="font-semibold text-blue-800">
                    Brutto: <span className="font-mono">{totalGross.toFixed(2)} {CURRENCY_OPTIONS[currency]?.label}</span>
                  </span>
                </div>
              </div>
            </>
          )}
        </div>

        {/* Footer */}
        <div className="flex gap-2 justify-end px-6 py-4 border-t border-gray-100">
          <button onClick={onClose} className="px-4 py-2 text-sm text-gray-600 hover:bg-gray-100 rounded-lg">
            Anuluj
          </button>
          <button
            onClick={() => mutation.mutate()}
            disabled={!initialized || lines.length === 0 || mutation.isPending}
            className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
          >
            {mutation.isPending ? 'Zapisywanie...' : 'Zapisz zmiany'}
          </button>
        </div>
      </div>
    </div>
  )
}
