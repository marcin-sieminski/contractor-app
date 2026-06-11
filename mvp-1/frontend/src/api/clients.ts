import api from './client'
import type { Client, CompanyLookupResult } from '../types/client'

export const getClients = () => api.get<Client[]>('/clients').then(r => r.data)

export const createClient = (data: Omit<Client, 'id' | 'isVerified' | 'projects'>) =>
  api.post<Client>('/clients', data).then(r => r.data)

export const updateClient = (id: string, data: Omit<Client, 'id' | 'isVerified' | 'projects'>) =>
  api.put<Client>(`/clients/${id}`, data).then(r => r.data)

export const lookupNip = (nip: string) =>
  api.get<CompanyLookupResult>(`/clients/lookup?nip=${nip}`).then(r => r.data)

export const getProjects = (clientId?: string) =>
  api.get<import('../types/client').Project[]>(`/projects${clientId ? `?clientId=${clientId}` : ''}`).then(r => r.data)

export const createProject = (data: { clientId: string; name: string; hourlyRate: number; currency: string }) =>
  api.post<import('../types/client').Project>('/projects', data).then(r => r.data)
