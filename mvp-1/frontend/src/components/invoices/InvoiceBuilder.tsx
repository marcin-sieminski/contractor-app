import { useState, useId } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getClients } from '../../api/clients'
import { getTimeEntries } from '../../api/timeEntries'
import { generateInvoice, submitToKsef } from '../../api/invoices'
import { useDialogClose } from '../../hooks/useDialogClose'
import { format } from 'date-fns'

interface Props { onClose: () => void }

const VAT_TREATMENT = [
  { value: 0, label: 'Krajowy 23% VAT' },
  { value: 1, label: 'Odwrotne obciążenie (EU)' },
  { value: 2, label: 'Poza UE (np.)' }
]

const CURRENCIES = [
  { value: 0, label: 'PLN' },
  { value: 1, label: 'EUR' },
  { value: 2, label: 'USD' },
  { value: 3, label: 'GBP' }
]

export function InvoiceBuilder({ onClose }: Props) {
  const fieldId = useId()
  useDialogClose(onClose)
  const qc = useQueryClient()
  const [step, setStep] = useState(1)
  const [clientId, setClientId] = useState('')
  const [selectedEntries, setSelectedEntries] = useState<Set<string>>(new Set())
  const [vatTreatment, setVatTreatment] = useState(0)
  const [currency, setCurrency] = useState(0)
  const [issueDate, setIssueDate] = useState(format(new Date(), 'yyyy-MM-dd'))
  const [generatedInvoiceId, setGeneratedInvoiceId] = useState<string | null>(null)

  const { data: clients = [] } = useQuery({ queryKey: ['clients'], queryFn: getClients })
  const { data: entries = [] } = useQuery({
    queryKey: ['timeEntries', clientId],
    queryFn: () => clientId
      ? getTimeEntries({ includeInvoiced: false })
          .then(es => es.filter(e =>
            clients.find(c => c.id === clientId)?.projects.some(p => p.id === e.projectId) ?? false
          ))
      : Promise.resolve([]),
    enabled: !!clientId
  })

  const generateMutation = useMutation({
    mutationFn: () => generateInvoice({
      clientId,
      timeEntryIds: [...selectedEntries],
      issueDate,
      vatTreatment,
      currency
    }),
    onSuccess: (inv) => {
      qc.invalidateQueries({ queryKey: ['invoices'] })
      qc.invalidateQueries({ queryKey: ['timeEntries'] })
      setGeneratedInvoiceId(inv.id)
      setStep(4)
    }
  })

  const submitMutation = useMutation({
    mutationFn: () => submitToKsef(generatedInvoiceId!),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['invoices'] })
      onClose()
    }
  })

  const toggleEntry = (id: string) => {
    setSelectedEntries(s => {
      const next = new Set(s)
      next.has(id) ? next.delete(id) : next.add(id)
      return next
    })
  }

  const selectedHours = entries
    .filter(e => selectedEntries.has(e.id))
    .reduce((acc, e) => acc + (e.durationMinutes ?? 0) / 60, 0)

  return (
    <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50 p-0 sm:p-4">
      <div role="dialog" aria-modal="true" aria-label="Kreator faktury" className="bg-white shadow-xl p-6 w-full h-full sm:h-auto max-w-none sm:max-w-lg max-h-none sm:max-h-[90vh] rounded-none sm:rounded-xl overflow-y-auto">
        <div className="flex items-center gap-2 mb-6">
          {[1,2,3,4].map(s => (
            <div key={s} className={`w-2 h-2 rounded-full ${s <= step ? 'bg-blue-600' : 'bg-gray-200'}`} />
          ))}
          <span className="text-sm text-gray-500 ml-2">Krok {step} z 4</span>
        </div>

        {step === 1 && (
          <div>
            <h2 className="text-lg font-semibold mb-4">Wybierz klienta</h2>
            <div className="space-y-2">
              {clients.map(c => (
                <button key={c.id} onClick={() => setClientId(c.id)}
                  className={`w-full text-left px-4 py-3 rounded-lg border transition-colors ${clientId === c.id ? 'border-blue-500 bg-blue-50' : 'border-gray-200 hover:border-gray-300'}`}>
                  <div className="font-medium">{c.name}</div>
                  <div className="text-gray-500 text-xs">NIP: {c.nip}</div>
                </button>
              ))}
            </div>
          </div>
        )}

        {step === 2 && (
          <div>
            <h2 className="text-lg font-semibold mb-4">Wybierz wpisy czasu</h2>
            {entries.length === 0
              ? <div className="text-gray-500 text-sm text-center py-8">Brak niezafakturowanych wpisów dla tego klienta.</div>
              : <div className="space-y-1">
                  {entries.map(e => (
                    <label key={e.id} className="flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-gray-50 cursor-pointer">
                      <input type="checkbox" checked={selectedEntries.has(e.id)} onChange={() => toggleEntry(e.id)} className="rounded" />
                      <div className="flex-1">
                        <div className="text-sm font-medium">{e.projectName}</div>
                        <div className="text-xs text-gray-500">{e.description || '—'} · {format(new Date(e.startedAt), 'dd.MM.yyyy')}</div>
                      </div>
                      <div className="font-mono text-sm">{((e.durationMinutes ?? 0) / 60).toFixed(2)}h</div>
                    </label>
                  ))}
                </div>
            }
            {selectedEntries.size > 0 && (
              <div className="mt-3 bg-blue-50 rounded-lg px-4 py-2 text-sm text-blue-700">
                Wybrano: {selectedEntries.size} wpisów · {selectedHours.toFixed(2)}h
              </div>
            )}
          </div>
        )}

        {step === 3 && (
          <div>
            <h2 className="text-lg font-semibold mb-4">Opcje faktury</h2>
            <div className="space-y-4">
              <div>
                <label htmlFor={`${fieldId}-issue`} className="text-xs text-gray-500 mb-1 block">Data wystawienia</label>
                <input id={`${fieldId}-issue`} type="date" value={issueDate} onChange={e => setIssueDate(e.target.value)}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
              </div>
              <div>
                <label htmlFor={`${fieldId}-vat`} className="text-xs text-gray-500 mb-1 block">Traktowanie VAT</label>
                <select id={`${fieldId}-vat`} value={vatTreatment} onChange={e => setVatTreatment(Number(e.target.value))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm">
                  {VAT_TREATMENT.map(v => <option key={v.value} value={v.value}>{v.label}</option>)}
                </select>
              </div>
              <div>
                <label htmlFor={`${fieldId}-currency`} className="text-xs text-gray-500 mb-1 block">Waluta</label>
                <select id={`${fieldId}-currency`} value={currency} onChange={e => setCurrency(Number(e.target.value))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm">
                  {CURRENCIES.map(c => <option key={c.value} value={c.value}>{c.label}</option>)}
                </select>
              </div>
              {currency !== 0 && (
                <div className="bg-yellow-50 border border-yellow-200 rounded-lg px-3 py-2 text-xs text-yellow-800">
                  Kurs NBP zostanie pobrany automatycznie z dnia roboczego przed datą wystawienia.
                </div>
              )}
            </div>
          </div>
        )}

        {step === 4 && (
          <div className="text-center py-4">
            <div className="text-4xl mb-3">✅</div>
            <h2 className="text-lg font-semibold mb-2">Faktura wygenerowana!</h2>
            <p className="text-gray-500 text-sm mb-4">Możesz teraz wysłać fakturę do KSeF (sandbox testowy) lub zamknąć to okno.</p>
            <button
              onClick={() => submitMutation.mutate()}
              disabled={submitMutation.isPending}
              className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50 w-full mb-2"
            >
              {submitMutation.isPending ? 'Wysyłanie...' : '📤 Wyślij do KSeF'}
            </button>
            {submitMutation.isSuccess && (
              <div role="status" className="text-green-600 text-sm">✓ Wysłano do KSeF pomyślnie!</div>
            )}
            {submitMutation.isError && (
              <div role="alert" className="text-red-500 text-sm">Błąd KSeF – sprawdź szczegóły na liście faktur.</div>
            )}
          </div>
        )}

        <div className="flex gap-2 mt-6">
          {step > 1 && step < 4 && (
            <button onClick={() => setStep(s => s - 1)} className="px-4 py-2 text-sm text-gray-600 hover:bg-gray-100 rounded-lg">Wstecz</button>
          )}
          <div className="flex-1" />
          <button onClick={onClose} className="px-4 py-2 text-sm text-gray-600 hover:bg-gray-100 rounded-lg">Zamknij</button>
          {step === 1 && (
            <button onClick={() => setStep(2)} disabled={!clientId}
              className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50">Dalej</button>
          )}
          {step === 2 && (
            <button onClick={() => setStep(3)} disabled={selectedEntries.size === 0}
              className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50">Dalej</button>
          )}
          {step === 3 && (
            <button onClick={() => generateMutation.mutate()} disabled={generateMutation.isPending}
              className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50">
              {generateMutation.isPending ? 'Generowanie...' : 'Generuj fakturę'}
            </button>
          )}
        </div>
      </div>
    </div>
  )
}
