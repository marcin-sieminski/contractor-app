import api from './client'
import type { TimeEntry } from '../types/timeEntry'

export const getTimeEntries = (params?: { from?: string; to?: string; projectId?: string; includeInvoiced?: boolean }) =>
  api.get<TimeEntry[]>('/time-entries', { params }).then(r => r.data)

export const getActiveTimer = () =>
  api.get<TimeEntry | null>('/time-entries/active').then(r => r.data)

export const startTimer = (projectId: string, description: string) =>
  api.post<TimeEntry>('/time-entries/start', { projectId, description }).then(r => r.data)

export const stopTimer = (id: string) =>
  api.post<TimeEntry>(`/time-entries/${id}/stop`).then(r => r.data)

export const createManualEntry = (data: { projectId: string; startedAt: string; stoppedAt: string; description: string }) =>
  api.post<TimeEntry>('/time-entries/manual', data).then(r => r.data)

export const updateTimeEntry = (id: string, data: { projectId: string; startedAt: string; stoppedAt: string; description: string }) =>
  api.put<TimeEntry>(`/time-entries/${id}`, data).then(r => r.data)

export const deleteTimeEntry = (id: string) =>
  api.delete(`/time-entries/${id}`)
