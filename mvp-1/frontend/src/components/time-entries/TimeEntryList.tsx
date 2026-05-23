import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Pencil, Trash2 } from 'lucide-react'
import { getTimeEntries, deleteTimeEntry } from '../../api/timeEntries'
import { EditTimeEntryModal } from './EditTimeEntryModal'
import { format } from 'date-fns'
import type { TimeEntry } from '../../types/timeEntry'

function formatDuration(minutes: number | null) {
  if (!minutes) return '—'
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  return `${h}h ${m}m`
}

function groupByDate(entries: TimeEntry[]) {
  const map = new Map<string, TimeEntry[]>()
  for (const e of entries) {
    const key = format(new Date(e.startedAt), 'yyyy-MM-dd')
    if (!map.has(key)) map.set(key, [])
    map.get(key)!.push(e)
  }
  return map
}

export function TimeEntryList() {
  const qc = useQueryClient()
  const [editingEntry, setEditingEntry] = useState<TimeEntry | null>(null)

  const { data: entries = [], isLoading } = useQuery({
    queryKey: ['timeEntries'],
    queryFn: () => getTimeEntries()
  })

  const deleteMutation = useMutation({
    mutationFn: deleteTimeEntry,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['timeEntries'] })
  })

  if (isLoading) return <div className="text-gray-500 text-sm p-4">Ładowanie...</div>

  const grouped = groupByDate(entries.filter(e => !e.isRunning))

  if (grouped.size === 0)
    return <div className="text-gray-400 text-sm p-4 text-center">Brak wpisów. Uruchom timer lub dodaj ręcznie.</div>

  return (
    <>
      <div className="divide-y divide-gray-100">
        {[...grouped.entries()].map(([date, dayEntries]) => (
          <div key={date}>
            <div className="px-4 py-2 bg-gray-50 text-xs font-semibold text-gray-500 uppercase tracking-wide">
              {format(new Date(date), 'EEEE, d MMMM yyyy')}
              <span className="ml-2 text-gray-400">
                {formatDuration(dayEntries.reduce((acc, e) => acc + (e.durationMinutes ?? 0), 0))}
              </span>
            </div>
            {dayEntries.map(entry => (
              <div key={entry.id} className="flex items-center px-4 py-3 hover:bg-gray-50 gap-3">
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <span className="font-medium text-sm">{entry.projectName}</span>
                    <span className="text-gray-400 text-xs">·</span>
                    <span className="text-gray-500 text-xs">{entry.clientName}</span>
                    {entry.isInvoiced && (
                      <span className="bg-green-100 text-green-700 text-xs px-2 py-0.5 rounded-full">Zafakturowane</span>
                    )}
                  </div>
                  {entry.description && <div className="text-gray-500 text-xs mt-0.5">{entry.description}</div>}
                </div>
                <div className="font-mono text-sm text-gray-600 w-16 text-right">
                  {formatDuration(entry.durationMinutes)}
                </div>
                {!entry.isInvoiced && (
                  <div className="flex items-center gap-1">
                    <button
                      onClick={() => setEditingEntry(entry)}
                      className="text-gray-400 hover:text-blue-500 p-1"
                      title="Edytuj"
                    >
                      <Pencil size={14} />
                    </button>
                    <button
                      onClick={() => deleteMutation.mutate(entry.id)}
                      className="text-gray-400 hover:text-red-500 p-1"
                      title="Usuń"
                    >
                      <Trash2 size={14} />
                    </button>
                  </div>
                )}
              </div>
            ))}
          </div>
        ))}
      </div>
      {editingEntry && (
        <EditTimeEntryModal entry={editingEntry} onClose={() => setEditingEntry(null)} />
      )}
    </>
  )
}
