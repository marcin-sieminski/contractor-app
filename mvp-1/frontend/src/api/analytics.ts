import api from './client'
import type { FinancialForecast, TaxFormKey, ZusStageKey } from '../types/forecast'

export const getFinancialForecast = (params?: {
  year?: number
  taxForm?: TaxFormKey
  zusStage?: ZusStageKey
  vatRate?: number
}) => api.get<FinancialForecast>('/analytics/forecast', { params }).then(r => r.data)
