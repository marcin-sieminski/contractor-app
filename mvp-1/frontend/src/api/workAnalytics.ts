import api from './client'
import type { WorkAnalytics } from '../types/workAnalytics'

export const getWorkAnalytics = (year: number) =>
  api.get<WorkAnalytics>(`/work-analytics/${year}`).then(r => r.data)
