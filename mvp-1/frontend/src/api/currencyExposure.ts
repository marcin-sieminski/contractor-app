import api from './client'
import type { CurrencyExposure } from '../types/currencyExposure'

export const getCurrencyExposure = (year: number) =>
  api.get<CurrencyExposure>(`/currency-exposure/${year}`).then(r => r.data)
