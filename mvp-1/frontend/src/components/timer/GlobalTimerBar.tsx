import { useNavigate } from 'react-router-dom'
import { Pause, Play, Square } from 'lucide-react'
import { useTimer, formatElapsed } from '../../hooks/useTimer'

export function GlobalTimerBar() {
  const { activeEntry, elapsed, isRunning, isPaused, pause, resume, stop, isPausing, isResuming, isStopping } = useTimer()
  const navigate = useNavigate()

  if (!activeEntry || (!isRunning && !isPaused)) return null

  const isActive = isRunning

  return (
    <div
      className={`flex items-center gap-3 px-4 py-2 text-white text-sm ${isActive ? 'bg-blue-600' : 'bg-amber-500'}`}
    >
      <button
        onClick={() => navigate('/time')}
        className="flex items-center gap-3 flex-1 min-w-0 text-left hover:opacity-80 transition-opacity"
      >
        <span className="font-mono font-bold text-base tabular-nums">{formatElapsed(elapsed)}</span>
        <span className={`text-xs font-medium px-1.5 py-0.5 rounded ${isActive ? 'bg-blue-500' : 'bg-amber-600'}`}>
          {isActive ? 'LIVE' : 'PAUZA'}
        </span>
        <span className="font-medium truncate">{activeEntry.projectName}</span>
        <span className={`hidden sm:inline truncate ${isActive ? 'text-blue-200' : 'text-amber-100'}`}>
          · {activeEntry.clientName}
        </span>
      </button>

      <div className="flex items-center gap-1 shrink-0">
        {isActive ? (
          <button
            onClick={e => { e.stopPropagation(); pause() }}
            disabled={isPausing}
            title="Wstrzymaj"
            className={`p-1.5 rounded hover:bg-blue-500 disabled:opacity-50 transition-colors`}
          >
            <Pause size={14} />
          </button>
        ) : (
          <button
            onClick={e => { e.stopPropagation(); resume() }}
            disabled={isResuming}
            title="Wznów"
            className="p-1.5 rounded hover:bg-amber-600 disabled:opacity-50 transition-colors"
          >
            <Play size={14} />
          </button>
        )}
        <button
          onClick={e => { e.stopPropagation(); stop() }}
          disabled={isStopping}
          title="Zatrzymaj i zapisz"
          className={`p-1.5 rounded disabled:opacity-50 transition-colors ${isActive ? 'hover:bg-blue-500' : 'hover:bg-amber-600'}`}
        >
          <Square size={14} />
        </button>
      </div>
    </div>
  )
}
