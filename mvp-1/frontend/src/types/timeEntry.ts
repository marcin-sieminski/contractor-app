export interface TimeEntry {
  id: string
  projectId: string
  projectName: string
  clientName: string
  startedAt: string
  stoppedAt: string | null
  durationMinutes: number | null
  description: string
  isInvoiced: boolean
  isRunning: boolean
  isPaused: boolean
  accumulatedSeconds: number
}
