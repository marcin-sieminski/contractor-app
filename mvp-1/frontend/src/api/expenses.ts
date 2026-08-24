import api from './client'
import type { Expense, OcrProvider, ScanReceiptResult } from '../types/expense'

interface ExpensePayload {
  date: string; category: string; description: string
  amount: number; currency: string; exchangeRate?: number
  isVatDeductible: boolean; receiptNumber?: string
  vendorName?: string; vendorNip?: string
  netAmount?: number; vatAmount?: number
  receiptId?: string
}

export const getExpenses = (params?: { from?: string; to?: string; category?: string }) =>
  api.get<Expense[]>('/expenses', { params }).then(r => r.data)

export const createExpense = (data: ExpensePayload) =>
  api.post<Expense>('/expenses', data).then(r => r.data)

export const updateExpense = (id: string, data: ExpensePayload) =>
  api.put<Expense>(`/expenses/${id}`, data).then(r => r.data)

export const deleteExpense = (id: string) =>
  api.delete(`/expenses/${id}`)

/** Wgrywa zdjęcie/skan paragonu i zwraca rozpoznane dane + id zapisanego skanu. */
export const scanReceipt = (file: File, provider: OcrProvider) => {
  const form = new FormData()
  form.append('file', file)
  form.append('provider', provider)
  return api.post<ScanReceiptResult>('/expenses/scan', form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  }).then(r => r.data)
}

/** Pobiera oryginalny plik skanu (do podglądu) jako Blob — wymaga nagłówka Bearer, stąd nie używamy bezpośrednio <img src>. */
export const getReceiptBlob = (expenseId: string) =>
  api.get(`/expenses/${expenseId}/receipt`, { responseType: 'blob' }).then(r => r.data as Blob)
