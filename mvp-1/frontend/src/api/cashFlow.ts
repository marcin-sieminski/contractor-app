import api from './client'
import type { CashFlowForecast } from '../types/cashFlow'
import type { TaxFormKey, ZusStageKey } from '../types/forecast'

export const getCashFlowForecast = (
  startingBalance: number,
  taxForm: TaxFormKey,
  zusStage: ZusStageKey,
) =>
  api
    .get<CashFlowForecast>('/cash-flow', {
      params: { startingBalance, taxForm, zusStage },
    })
    .then(r => r.data)
