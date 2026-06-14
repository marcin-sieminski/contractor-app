import api from './client'
import type { TaxObligations, TaxPaymentType } from '../types/taxObligations'
import type { TaxFormKey, ZusStageKey } from '../types/forecast'

export const getTaxObligations = (
  year: number,
  taxForm: TaxFormKey,
  zusStage: ZusStageKey,
) => api.get<TaxObligations>(`/tax-obligations/${year}`, { params: { taxForm, zusStage } }).then(r => r.data)

export const recordTaxPayment = (
  year: number,
  month: number,
  body: { type: TaxPaymentType; amount: number; paidAt: string | null; notes: string | null },
) => api.post<{ id: string }>(`/tax-obligations/${year}/${month}/payments`, body).then(r => r.data)

export const deleteTaxPayment = (id: string) =>
  api.delete(`/tax-obligations/payments/${id}`)
