import api from './client'
import type { IpBoxProgressDto } from '../types/ipBox'

export const getIpBoxProgress = (year: number) =>
  api.get<IpBoxProgressDto>(`/ip-box/${year}`).then(r => r.data)

export const setProjectIpStatus = (projectId: string, isIpProject: boolean) =>
  api.put(`/ip-box/projects/${projectId}/ip-status`, { isIpProject })

export const setTimeEntryIpWork = (
  timeEntryId: string,
  isIpWork: boolean,
  ipWorkDescription: string | null,
) => api.put(`/ip-box/entries/${timeEntryId}/ip-work`, { isIpWork, ipWorkDescription })

export const exportIpBoxCsvUrl = (year: number, month?: number) => {
  const token = localStorage.getItem('auth_token')
  const base = `/api/v1/ip-box/${year}/export-csv`
  const params = month ? `?month=${month}` : ''
  return { url: base + params, token }
}
