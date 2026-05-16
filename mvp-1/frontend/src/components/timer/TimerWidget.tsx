import { useState } from 'react'
import { Play, Square } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import { useTimer, formatElapsed } from '../../hooks/useTimer'
import { getProjects } from '../../api/clients'

export function TimerWidget() {
  const { activeEntry, elapsed, isRunning, start, stop, isStarting, isStopping } = useTimer()
  const { data: projects = [] } = useQuery({ queryKey: ['projects'], queryFn: () => getProjects() })
  const [projectId, setProjectId] = useState('')
  const [description, setDescription] = useState('')

  const handleStart = () => {
    if (!projectId) return
    start({ projectId, description })
    setDescription('')
  }

  if (isRunning && activeEntry) {
    return (
      <div className="bg-blue-600 text-white rounded-xl p-4 flex items-center gap-4">
        <div className="font-mono text-3xl font-bold">{formatElapsed(elapsed)}</div>
        <div className="flex-1">
          <div className="font-medium">{activeEntry.projectName}</div>
          <div className="text-blue-200 text-sm">{activeEntry.clientName}</div>
          {activeEntry.description && <div className="text-blue-100 text-sm mt-1">{activeEntry.description}</div>}
        </div>
        <button
          onClick={stop}
          disabled={isStopping}
          className="bg-white text-blue-600 hover:bg-blue-50 px-4 py-2 rounded-lg font-medium flex items-center gap-2 disabled:opacity-50"
        >
          <Square size={16} /> Zatrzymaj
        </button>
      </div>
    )
  }

  return (
    <div className="bg-white border border-gray-200 rounded-xl p-4 flex items-center gap-3">
      <select
        value={projectId}
        onChange={e => setProjectId(e.target.value)}
        className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 outline-none"
      >
        <option value="">Wybierz projekt...</option>
        {projects.map(p => (
          <option key={p.id} value={p.id}>{p.name}</option>
        ))}
      </select>
      <input
        type="text"
        placeholder="Opis (opcjonalnie)"
        value={description}
        onChange={e => setDescription(e.target.value)}
        onKeyDown={e => e.key === 'Enter' && handleStart()}
        className="flex-1 border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 outline-none"
      />
      <button
        onClick={handleStart}
        disabled={!projectId || isStarting}
        className="bg-blue-600 text-white hover:bg-blue-700 px-4 py-2 rounded-lg font-medium flex items-center gap-2 disabled:opacity-50"
      >
        <Play size={16} /> Start
      </button>
    </div>
  )
}
