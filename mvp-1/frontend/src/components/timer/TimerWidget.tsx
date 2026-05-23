import { useState } from 'react'
import { Play } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import { useTimer } from '../../hooks/useTimer'
import { getProjects } from '../../api/clients'

export function TimerWidget() {
  const { isRunning, isPaused, start, isStarting } = useTimer()
  const { data: projects = [] } = useQuery({ queryKey: ['projects'], queryFn: () => getProjects() })
  const [projectId, setProjectId] = useState('')
  const [description, setDescription] = useState('')

  if (isRunning || isPaused) return null

  const handleStart = () => {
    if (!projectId) return
    start({ projectId, description })
    setDescription('')
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
