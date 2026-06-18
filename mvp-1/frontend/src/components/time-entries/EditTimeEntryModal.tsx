import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { updateTimeEntry } from '../../api/timeEntries'
import { getProjects } from '../../api/clients'
import { useClientNameById } from '../../hooks/useClientNameById'
import { shortClientName } from '../../lib/clientName'
import { format } from 'date-fns'
import type { TimeEntry } from '../../types/timeEntry'

interface Props {
  entry: TimeEntry
  onClose: () => void
}

export function EditTimeEntryModal({ entry, onClose }: Props) {
  const qc = useQueryClient()
  const { data: projects = [] } = useQuery({ queryKey: ['projects'], queryFn: () => getProjects() })
  const clientNameById = useClientNameById()

  const startDate = new Date(entry.startedAt)
  const stopDate = entry.stoppedAt ? new Date(entry.stoppedAt) : new Date()

  const [form, setForm] = useState({
    projectId: entry.projectId,
    date: format(startDate, 'yyyy-MM-dd'),
    startTime: format(startDate, 'HH:mm'),
    endTime: format(stopDate, 'HH:mm'),
    description: entry.description,
  })

  const mutation = useMutation({
    mutationFn: () => {
      const startedAt = new Date(`${form.date}T${form.startTime}:00`).toISOString()
      const stoppedAt = new Date(`${form.date}T${form.endTime}:00`).toISOString()
      return updateTimeEntry(entry.id, { projectId: form.projectId, startedAt, stoppedAt, description: form.description })
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['timeEntries'] })
      onClose()
    }
  })

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl p-6 w-full max-w-md">
        <h2 className="text-lg font-semibold mb-4 text-gray-900 dark:text-gray-100">Edytuj wpis</h2>
        <div className="space-y-3">
          <select
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
          <input
            type="date"
            value={form.date}
            onChange={e => setForm(f => ({ ...f, date: e.target.value }))}
            className="w-full border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
          />
          <div className="flex gap-2">
            <input
              type="time"
              value={form.startTime}
              onChange={e => setForm(f => ({ ...f, startTime: e.target.value }))}
              className="flex-1 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
            />
            <span className="self-center text-gray-400 dark:text-gray-500">—</span>
            <input
              type="time"
              value={form.endTime}
              onChange={e => setForm(f => ({ ...f, endTime: e.target.value }))}
              className="flex-1 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
            />
          </div>
          <input
            type="text"
            placeholder="Opis (opcjonalnie)"
            value={form.description}
            onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
            className="w-full border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500"
          />
          {mutation.isError && (
            <p className="text-red-600 dark:text-red-400 text-xs">{(mutation.error as Error)?.message ?? 'Błąd zapisu.'}</p>
          )}
        </div>
        <div className="flex gap-2 mt-4 justify-end">
          <button onClick={onClose} className="px-4 py-2 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg">
            Anuluj
          </button>
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
