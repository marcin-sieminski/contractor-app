import api from './client'
import type { Expense } from '../types/expense'

export const getExpenses = (params?: { from?: string; to?: string; category?: string }) =>
  api.get<Expense[]>('/expenses', { params }).then(r => r.data)

export const createExpense = (data: {
  date: string; category: string; description: string
  amount: number; currency: string; exchangeRate?: number
  isVatDeductible: boolean; receiptNumber?: string
}) => api.post<Expense>('/expenses', data).then(r => r.data)

export const updateExpense = (id: string, data: {
  date: string; category: string; description: string
  amount: number; currency: string; exchangeRate?: number
  isVatDeductible: boolean; receiptNumber?: string
}) => api.put<Expense>(`/expenses/${id}`, data).then(r => r.data)

export const deleteExpense = (id: string) =>
  api.delete(`/expenses/${id}`)
