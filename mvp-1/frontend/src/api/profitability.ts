import api from './client'
import type { Profitability } from '../types/profitability'

export const getClientProfitability = (year: number, month?: number) =>
  api
    .get<Profitability>(`/profitability/${year}`, { params: month ? { month } : undefined })
    .then(r => r.data)
