import { useState, useId } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createClient, updateClient } from '../../api/clients'
import { useNipLookup } from '../../hooks/useNipLookup'
import { useDialogClose } from '../../hooks/useDialogClose'
import type { Client, CompanyLookupResult } from '../../types/client'

interface Props { onClose: () => void; client?: Client }

const inputCls = "w-full border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"

export function ClientForm({ onClose, client }: Props) {
  const titleId = useId()
  const nipId = useId()
  useDialogClose(onClose)
  const qc = useQueryClient()
  const isEdit = !!client
  const [form, setForm] = useState({
    name: client?.name ?? '', nip: client?.nip ?? '', regon: client?.regon ?? '',
    street: client?.street ?? '', city: client?.city ?? '', postalCode: client?.postalCode ?? '',
    country: client?.country ?? 'PL', isEuVatPayer: client?.isEuVatPayer ?? false
  })
  const [verified, setVerified] = useState(client?.isVerified ?? false)

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
    mutationFn: () => isEdit ? updateClient(client!.id, { ...form }) : createClient({ ...form }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['clients'] }); onClose() }
  })

  const set = (k: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
    setForm(f => ({ ...f, [k]: e.target.type === 'checkbox' ? (e.target as HTMLInputElement).checked : e.target.value }))

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-0 sm:p-4">
      <div role="dialog" aria-modal="true" aria-labelledby={titleId} className="bg-white dark:bg-gray-800 shadow-xl p-6 w-full h-full sm:h-auto max-w-none sm:max-w-lg max-h-none sm:max-h-[90vh] rounded-none sm:rounded-xl overflow-y-auto">
        <h2 id={titleId} className="text-lg font-semibold mb-4 text-gray-900 dark:text-gray-100">{isEdit ? 'Edytuj klienta' : 'Dodaj klienta'}</h2>
        <div className="space-y-3">
          <div>
            <label htmlFor={nipId} className="text-xs text-gray-500 dark:text-gray-400 mb-1 block">NIP</label>
            <div className="relative">
              <input id={nipId} value={form.nip} onChange={set('nip')} placeholder="0000000000"
                className={inputCls + ' pr-24'} />
              <span aria-live="polite" className="absolute right-3 top-2 text-xs">
                {nipLoading && <span className="text-blue-500">Sprawdzam...</span>}
                {verified && !nipLoading && <span className="text-green-600 dark:text-green-400">✓ Zweryfikowany</span>}
                {nipError && <span className="text-red-500">{nipError}</span>}
              </span>
            </div>
          </div>
          <input aria-label="Nazwa firmy" value={form.name} onChange={set('name')} placeholder="Nazwa firmy" className={inputCls} />
          <input aria-label="Ulica i numer" value={form.street} onChange={set('street')} placeholder="Ulica i numer" className={inputCls} />
          <div className="flex gap-2">
            <input aria-label="Kod pocztowy" value={form.postalCode} onChange={set('postalCode')} placeholder="Kod pocztowy"
              className="w-32 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500" />
            <input aria-label="Miasto" value={form.city} onChange={set('city')} placeholder="Miasto"
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
