import { useState, useId } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createManualEntry } from '../../api/timeEntries'
import { getProjects } from '../../api/clients'
import { useClientNameById } from '../../hooks/useClientNameById'
import { shortClientName } from '../../lib/clientName'
import { useDialogClose } from '../../hooks/useDialogClose'
import { format } from 'date-fns'

interface Props { onClose: () => void }

export function ManualEntryForm({ onClose }: Props) {
  const titleId = useId()
  useDialogClose(onClose)
  const qc = useQueryClient()
  const { data: projects = [] } = useQuery({ queryKey: ['projects'], queryFn: () => getProjects() })
  const clientNameById = useClientNameById()
  const today = format(new Date(), 'yyyy-MM-dd')
  const [form, setForm] = useState({ projectId: '', date: today, startTime: '09:00', endTime: '17:00', description: '' })

  const mutation = useMutation({
    mutationFn: () => {
      const startedAt = new Date(`${form.date}T${form.startTime}:00`).toISOString()
      const stoppedAt = new Date(`${form.date}T${form.endTime}:00`).toISOString()
      return createManualEntry({ projectId: form.projectId, startedAt, stoppedAt, description: form.description })
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['timeEntries'] })
      onClose()
    }
  })

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div role="dialog" aria-modal="true" aria-labelledby={titleId} className="bg-white dark:bg-gray-800 rounded-xl shadow-xl p-6 w-full max-w-md">
        <h2 id={titleId} className="text-lg font-semibold mb-4 text-gray-900 dark:text-gray-100">Dodaj wpis ręcznie</h2>
        <div className="space-y-3">
          <select
            aria-label="Projekt"
            value={form.projectId}
            onChange={e => setForm(f => ({ ...f, projectId: e.target.value }))}
            className="w-full border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
          >
            <option value="">Projekt...</option>
            {projects.map(p => {
              const client = clientNameById.get(p.clientId)
              return (
                <option key={p.id} value={p.id}>
                  {client ? `${p.name} (${shortClientName(client)})` : p.name}
                </option>
              )
            })}
          </select>
          <input type="date" aria-label="Data" value={form.date}
            onChange={e => setForm(f => ({ ...f, date: e.target.value }))}
            className="w-full border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100" />
          <div className="flex gap-2">
            <input type="time" aria-label="Godzina rozpoczęcia" value={form.startTime}
              onChange={e => setForm(f => ({ ...f, startTime: e.target.value }))}
              className="flex-1 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100" />
            <span aria-hidden="true" className="self-center text-gray-400 dark:text-gray-500">—</span>
            <input type="time" aria-label="Godzina zakończenia" value={form.endTime}
              onChange={e => setForm(f => ({ ...f, endTime: e.target.value }))}
              className="flex-1 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100" />
          </div>
          <input type="text" aria-label="Opis" placeholder="Opis (opcjonalnie)" value={form.description}
            onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
            className="w-full border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500" />
        </div>
        <div className="flex gap-2 mt-4 justify-end">
          <button onClick={onClose} className="px-4 py-2 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg">Anuluj</button>
          <button
            onClick={() => mutation.mutate()}
            disabled={!form.projectId || mutation.isPending}
            className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
          >
            {mutation.isPending ? 'Zapisywanie...' : 'Zapisz'}
          </button>
        </div>
      </div>
    </div>
  )
}
