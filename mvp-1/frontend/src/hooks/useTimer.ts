import { useState, useEffect, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getActiveTimer, startTimer, stopTimer } from '../api/timeEntries'

export function useTimer() {
  const qc = useQueryClient()
  const [elapsed, setElapsed] = useState(0)
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null)

  const { data: activeEntry } = useQuery({
    queryKey: ['activeTimer'],
    queryFn: getActiveTimer,
    refetchInterval: 30_000
  })

  useEffect(() => {
    if (activeEntry?.isRunning) {
      const start = new Date(activeEntry.startedAt).getTime()
      const tick = () => setElapsed(Math.floor((Date.now() - start) / 1000))
      tick()
      intervalRef.current = setInterval(tick, 1000)
    } else {
      setElapsed(0)
      if (intervalRef.current) clearInterval(intervalRef.current)
    }
    return () => { if (intervalRef.current) clearInterval(intervalRef.current) }
  }, [activeEntry?.id, activeEntry?.isRunning])

  const startMutation = useMutation({
    mutationFn: ({ projectId, description }: { projectId: string; description: string }) =>
      startTimer(projectId, description),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['activeTimer'] })
      qc.invalidateQueries({ queryKey: ['timeEntries'] })
    }
  })

  const stopMutation = useMutation({
    mutationFn: (id: string) => stopTimer(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['activeTimer'] })
      qc.invalidateQueries({ queryKey: ['timeEntries'] })
    }
  })

  return {
    activeEntry,
    elapsed,
    isRunning: !!activeEntry?.isRunning,
    start: startMutation.mutate,
    stop: () => activeEntry && stopMutation.mutate(activeEntry.id),
    isStarting: startMutation.isPending,
    isStopping: stopMutation.isPending
  }
}

export function formatElapsed(seconds: number) {
  const h = Math.floor(seconds / 3600)
  const m = Math.floor((seconds % 3600) / 60)
  const s = seconds % 60
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
}
