import { X } from 'lucide-react'
import { format } from 'date-fns'
import { pl } from 'date-fns/locale'
import type { TimeEntry } from '../../types/timeEntry'

interface Props {
  monthKey: string
  entries: TimeEntry[]
  onClose: () => void
}

function formatDuration(minutes: number | null) {
  if (!minutes) return '—'
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  return m > 0 ? `${h}h ${m}m` : `${h}h`
}

export function MonthDetailModal({ monthKey, entries, onClose }: Props) {
  const [year, month] = monthKey.split('-').map(Number)

  const monthEntries = entries.filter(e => {
    const d = new Date(e.startedAt)
    return d.getFullYear() === year && d.getMonth() + 1 === month && !e.isRunning && !e.isPaused
  })

  const totalMinutes = monthEntries.reduce((acc, e) => acc + (e.durationMinutes ?? 0), 0)

  const byProject = monthEntries.reduce<Record<string, TimeEntry[]>>((acc, e) => {
    const key = `${e.clientName} / ${e.projectName}`
    ;(acc[key] ??= []).push(e)
    return acc
  }, {})

  const monthLabel = format(new Date(year, month - 1, 1), 'LLLL yyyy', { locale: pl })

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-2xl max-h-[80vh] flex flex-col">
        <div className="flex items-center justify-between px-5 py-4 border-b border-gray-100">
          <div>
            <h2 className="text-base font-semibold capitalize">{monthLabel}</h2>
            <p className="text-xs text-gray-400 mt-0.5">
              {monthEntries.length} {monthEntries.length === 1 ? 'wpis' : 'wpisów'} · łącznie {formatDuration(totalMinutes)}
            </p>
          </div>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 p-1">
            <X size={18} />
          </button>
        </div>

        <div className="overflow-y-auto flex-1 px-5 py-3">
          {monthEntries.length === 0 ? (
            <div className="text-gray-400 text-sm text-center py-10">
              Brak wpisów czasu pracy w tym miesiącu.
            </div>
          ) : (
            Object.entries(byProject).map(([projectKey, projectEntries]) => {
              const projectMinutes = projectEntries.reduce((acc, e) => acc + (e.durationMinutes ?? 0), 0)
              return (
                <div key={projectKey} className="mb-5">
                  <div className="flex items-center justify-between mb-1.5">
                    <span className="text-xs font-semibold text-gray-500 uppercase tracking-wide">
                      {projectKey}
                    </span>
                    <span className="text-xs font-medium text-gray-500">
                      {formatDuration(projectMinutes)}
                    </span>
                  </div>
                  <div className="divide-y divide-gray-50 border border-gray-100 rounded-lg overflow-hidden">
                    {projectEntries
                      .sort((a, b) => new Date(a.startedAt).getTime() - new Date(b.startedAt).getTime())
                      .map(entry => (
                        <div key={entry.id} className="flex items-center gap-3 px-3 py-2.5 bg-white hover:bg-gray-50">
                          <span className="text-xs text-gray-400 w-16 shrink-0">
                            {format(new Date(entry.startedAt), 'd MMM', { locale: pl })}
                          </span>
                          <span className="flex-1 text-sm text-gray-700 truncate">
                            {entry.description || <span className="text-gray-400 italic">bez opisu</span>}
                          </span>
                          <span className="font-mono text-sm text-gray-500 shrink-0">
                            {formatDuration(entry.durationMinutes)}
                          </span>
                        </div>
                      ))}
                  </div>
                </div>
              )
            })
          )}
        </div>
      </div>
    </div>
  )
}
