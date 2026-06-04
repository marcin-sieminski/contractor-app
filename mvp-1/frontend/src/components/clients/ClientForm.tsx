import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createClient } from '../../api/clients'
import { useNipLookup } from '../../hooks/useNipLookup'
import type { CompanyLookupResult } from '../../types/client'

interface Props { onClose: () => void }

const inputCls = "w-full border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"

export function ClientForm({ onClose }: Props) {
  const qc = useQueryClient()
  const [form, setForm] = useState({
    name: '', nip: '', regon: '', street: '', city: '', postalCode: '', country: 'PL', isEuVatPayer: false
  })
  const [verified, setVerified] = useState(false)

  const handleLookupResult = (r: CompanyLookupResult) => {
    setForm(f => ({
      ...f,
      name: r.name || f.name,
      regon: r.regon || f.regon,
      street: r.street || f.street,
      city: r.city || f.city,
      postalCode: r.postalCode || f.postalCode,
      country: r.country || f.country
    }))
    setVerified(true)
  }

  const { loading: nipLoading, error: nipError } = useNipLookup(form.nip, handleLookupResult)

  const mutation = useMutation({
    mutationFn: () => createClient({ ...form }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['clients'] }); onClose() }
  })

  const set = (k: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
    setForm(f => ({ ...f, [k]: e.target.type === 'checkbox' ? (e.target as HTMLInputElement).checked : e.target.value }))

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl p-6 w-full max-w-lg">
        <h2 className="text-lg font-semibold mb-4 text-gray-900 dark:text-gray-100">Dodaj klienta</h2>
        <div className="space-y-3">
          <div>
            <label className="text-xs text-gray-500 dark:text-gray-400 mb-1 block">NIP</label>
            <div className="relative">
              <input value={form.nip} onChange={set('nip')} placeholder="0000000000"
                className={inputCls + ' pr-24'} />
              <span className="absolute right-3 top-2 text-xs">
                {nipLoading && <span className="text-blue-500">Sprawdzam...</span>}
                {verified && !nipLoading && <span className="text-green-600 dark:text-green-400">✓ Zweryfikowany</span>}
                {nipError && <span className="text-red-500">{nipError}</span>}
              </span>
            </div>
          </div>
          <input value={form.name} onChange={set('name')} placeholder="Nazwa firmy" className={inputCls} />
          <input value={form.street} onChange={set('street')} placeholder="Ulica i numer" className={inputCls} />
          <div className="flex gap-2">
            <input value={form.postalCode} onChange={set('postalCode')} placeholder="Kod pocztowy"
              className="w-32 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500" />
            <input value={form.city} onChange={set('city')} placeholder="Miasto"
              className="flex-1 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div className="flex items-center gap-2">
            <input type="checkbox" id="euVat" checked={form.isEuVatPayer}
              onChange={set('isEuVatPayer')} className="rounded" />
            <label htmlFor="euVat" className="text-sm text-gray-600 dark:text-gray-300">Podatnik VAT UE (odwrotne obciążenie)</label>
          </div>
        </div>
        <div className="flex gap-2 mt-4 justify-end">
          <button onClick={onClose} className="px-4 py-2 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg">Anuluj</button>
          <button onClick={() => mutation.mutate()} disabled={!form.name || !form.nip || mutation.isPending}
            className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50">
            {mutation.isPending ? 'Zapisywanie...' : 'Zapisz'}
          </button>
        </div>
      </div>
    </div>
  )
}
