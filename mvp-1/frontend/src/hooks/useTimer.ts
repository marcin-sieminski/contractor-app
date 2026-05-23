import { useState, useEffect, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getActiveTimer, startTimer, stopTimer, pauseTimer, resumeTimer } from '../api/timeEntries'

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
      const startedAt = new Date(activeEntry.startedAt).getTime()
      const accumulated = activeEntry.accumulatedSeconds ?? 0
      const tick = () => setElapsed(accumulated + Math.floor((Date.now() - startedAt) / 1000))
      tick()
      intervalRef.current = setInterval(tick, 1000)
    } else if (activeEntry?.isPaused) {
      setElapsed(activeEntry.accumulatedSeconds ?? 0)
      if (intervalRef.current) clearInterval(intervalRef.current)
    } else {
      setElapsed(0)
      if (intervalRef.current) clearInterval(intervalRef.current)
    }
    return () => { if (intervalRef.current) clearInterval(intervalRef.current) }
  }, [activeEntry?.id, activeEntry?.isRunning, activeEntry?.isPaused, activeEntry?.accumulatedSeconds])

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: ['activeTimer'] })
    qc.invalidateQueries({ queryKey: ['timeEntries'] })
  }

  const startMutation = useMutation({
    mutationFn: ({ projectId, description }: { projectId: string; description: string }) =>
      startTimer(projectId, description),
    onSuccess: invalidate
  })

  const stopMutation = useMutation({
    mutationFn: (id: string) => stopTimer(id),
    onSuccess: invalidate
  })

  const pauseMutation = useMutation({
    mutationFn: (id: string) => pauseTimer(id),
    onSuccess: invalidate
  })

  const resumeMutation = useMutation({
    mutationFn: (id: string) => resumeTimer(id),
    onSuccess: invalidate
  })

  return {
    activeEntry,
    elapsed,
    isRunning: !!activeEntry?.isRunning,
    isPaused: !!activeEntry?.isPaused,
    start: startMutation.mutate,
    stop: () => activeEntry && stopMutation.mutate(activeEntry.id),
    pause: () => activeEntry && pauseMutation.mutate(activeEntry.id),
    resume: () => activeEntry && resumeMutation.mutate(activeEntry.id),
    isStarting: startMutation.isPending,
    isStopping: stopMutation.isPending,
    isPausing: pauseMutation.isPending,
    isResuming: resumeMutation.isPending,
  }
}

export function formatElapsed(seconds: number) {
  const h = Math.floor(seconds / 3600)
  const m = Math.floor((seconds % 3600) / 60)
  const s = seconds % 60
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
}
