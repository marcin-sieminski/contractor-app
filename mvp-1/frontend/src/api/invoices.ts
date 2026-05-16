import api from './client'
import type { Invoice } from '../types/invoice'

export const getInvoices = (clientId?: string) =>
  api.get<Invoice[]>('/invoices', { params: clientId ? { clientId } : undefined }).then(r => r.data)

export const getInvoiceById = (id: string) =>
  api.get<Invoice>(`/invoices/${id}`).then(r => r.data)

export const generateInvoice = (data: {
  clientId: string
  timeEntryIds: string[]
  issueDate: string
  vatTreatment: number
  currency: number
  paymentDays?: number
}) => api.post<Invoice>('/invoices/generate', data).then(r => r.data)

export const updateInvoice = (data: {
  invoiceId: string
  issueDate: string
  serviceDate: string
  dueDate: string
  vatTreatment: number
  currency: number
  lineItems: { lineNumber: number; description: string; quantity: number; unit: string; unitPrice: number }[]
}) => api.put<Invoice>(`/invoices/${data.invoiceId}`, data).then(r => r.data)

export const submitToKsef = (id: string) =>
  api.post<Invoice>(`/invoices/${id}/submit-ksef`).then(r => r.data)

export const getInvoiceXml = (id: string) =>
  api.get<string>(`/invoices/${id}/xml`, { responseType: 'text' }).then(r => r.data)
