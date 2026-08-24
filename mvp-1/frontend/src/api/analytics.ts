import api from './client'
import type { FinancialForecast, TaxFormKey, ZusStageKey } from '../types/forecast'

export const getFinancialForecast = (params?: {
  year?: number
  taxForm?: TaxFormKey
  zusStage?: ZusStageKey
  vatRate?: number
  includeForecast?: boolean
  ipBoxEnabled?: boolean
  ipQualifyingPercent?: number
  // Niezapisany podgląd korekt prognozy "miesiąc:kwota,…" (np. "7:15000,8:16000").
  revenueOverrides?: string
  costOverrides?: string
}) => api.get<FinancialForecast>('/analytics/forecast', { params }).then(r => r.data)

export type ForecastOverrideItem = { month: number; revenue?: number | null; cost?: number | null }

// Zapis trwałych korekt prognozy dla roku.
//  - save: upsert przekazanych miesięcy (miesiąc bez kwot usuwa korektę),
//  - clear: wyzerowanie wszystkich 12 miesięcy,
//  - reset: usunięcie korekt (powrót do prognozy automatycznej / carry-over).
export const saveForecastOverrides = (
  year: number,
  body: { action: 'save' | 'clear' | 'reset'; overrides?: ForecastOverrideItem[] },
) => api.put(`/analytics/forecast/${year}/overrides`, body).then(r => r.data)
