import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getClients, createProject } from '../../api/clients'
import { CheckCircle, Plus, X } from 'lucide-react'

const CURRENCIES = ['PLN', 'EUR', 'USD', 'GBP', 'CHF']

function AddProjectInline({ clientId, onDone }: { clientId: string; onDone: () => void }) {
  const qc = useQueryClient()
  const [form, setForm] = useState({ name: '', hourlyRate: '', currency: 'PLN' })

  const mutation = useMutation({
    mutationFn: () => createProject({
      clientId,
      name: form.name,
      hourlyRate: parseFloat(form.hourlyRate),
      currency: form.currency
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['clients'] })
      qc.invalidateQueries({ queryKey: ['projects'] })
      onDone()
    }
  })

  return (
    <form
      className="flex items-center gap-2 mt-2 bg-blue-50 rounded-lg px-3 py-2"
      onSubmit={e => { e.preventDefault(); mutation.mutate() }}
    >
      <input
        autoFocus
        placeholder="Nazwa projektu"
        value={form.name}
        onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
        className="border border-gray-300 rounded px-2 py-1 text-xs w-40 focus:ring-2 focus:ring-blue-400 outline-none"
      />
      <input
        type="number"
        placeholder="Stawka/h"
        value={form.hourlyRate}
        onChange={e => setForm(f => ({ ...f, hourlyRate: e.target.value }))}
        className="border border-gray-300 rounded px-2 py-1 text-xs w-24 focus:ring-2 focus:ring-blue-400 outline-none"
        min="0"
        step="0.01"
      />
      <select
        value={form.currency}
        onChange={e => setForm(f => ({ ...f, currency: e.target.value }))}
        className="border border-gray-300 rounded px-2 py-1 text-xs focus:ring-2 focus:ring-blue-400 outline-none"
      >
        {CURRENCIES.map(c => <option key={c}>{c}</option>)}
      </select>
      <button
        type="submit"
        disabled={!form.name || !form.hourlyRate || mutation.isPending}
        className="bg-blue-600 text-white text-xs px-3 py-1 rounded hover:bg-blue-700 disabled:opacity-50"
      >
        {mutation.isPending ? '...' : 'Dodaj'}
      </button>
      <button type="button" onClick={onDone} className="text-gray-400 hover:text-gray-600">
        <X size={14} />
      </button>
    </form>
  )
}

export function ClientList() {
  const { data: clients = [], isLoading } = useQuery({ queryKey: ['clients'], queryFn: getClients })
  const [addingProjectFor, setAddingProjectFor] = useState<string | null>(null)

  if (isLoading) return <div className="text-gray-500 p-4 text-sm">Ładowanie...</div>
  if (clients.length === 0)
    return <div className="text-gray-400 text-sm p-4 text-center">Brak klientów. Dodaj pierwszego klienta.</div>

  return (
    <div className="divide-y divide-gray-100">
      {clients.map(c => (
        <div key={c.id} className="px-4 py-3 hover:bg-gray-50">
          <div className="flex items-center gap-2">
            <span className="font-medium">{c.name}</span>
            {c.isVerified && <CheckCircle size={14} className="text-green-500" />}
            {c.isEuVatPayer && (
              <span className="bg-purple-100 text-purple-700 text-xs px-2 py-0.5 rounded-full">VAT UE</span>
            )}
          </div>
          <div className="text-gray-500 text-xs mt-0.5">NIP: {c.nip} · {c.city}</div>

          <div className="flex gap-1 mt-1.5 flex-wrap items-center">
            {c.projects.map(p => (
              <span key={p.id} className="bg-gray-100 text-gray-600 text-xs px-2 py-0.5 rounded">
                {p.name} · {p.hourlyRate} {p.currency}/h
              </span>
            ))}
            {addingProjectFor !== c.id && (
              <button
                onClick={() => setAddingProjectFor(c.id)}
                className="flex items-center gap-1 text-xs text-blue-600 hover:text-blue-800 px-1.5 py-0.5 rounded hover:bg-blue-50"
              >
                <Plus size={12} /> projekt
              </button>
            )}
          </div>

          {addingProjectFor === c.id && (
            <AddProjectInline clientId={c.id} onDone={() => setAddingProjectFor(null)} />
          )}
        </div>
      ))}
    </div>
  )
}
