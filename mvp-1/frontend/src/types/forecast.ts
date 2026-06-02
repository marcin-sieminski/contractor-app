export type TaxFormKey = 'liniowy' | 'ryczalt' | 'skala'
export type ZusStageKey = 'pelny' | 'preferencyjny' | 'ulga_na_start'

export const TAX_FORM_LABELS: Record<TaxFormKey, string> = {
  liniowy: 'Liniowy 19%',
  ryczalt: 'Ryczałt 12%',
  skala: 'Skala 12%/32%',
}

export const ZUS_STAGE_LABELS: Record<ZusStageKey, string> = {
  pelny: 'Pełny ZUS',
  preferencyjny: 'Preferencyjny',
  ulga_na_start: 'Ulga na start',
}

export interface MonthForecast {
  month: number
  monthName: string
  isActual: boolean
  revenue: number
  costs: number
  income: number
  incomeTax: number
  zusSocial: number
  zusHealth: number
  vatOutput: number
  vatInput: number
  vatPayable: number
  totalObligations: number
  netCashFlow: number
}

export interface ForecastTotals {
  revenue: number
  costs: number
  income: number
  incomeTax: number
  zusSocial: number
  zusHealth: number
  vatPayable: number
  totalObligations: number
  netCashFlow: number
}

export interface FinancialForecast {
  year: number
  taxForm: string
  zusStage: string
  monthsWithData: number
  avgMonthlyRevenue: number
  avgMonthlyCosts: number
  ytd: ForecastTotals
  fullYear: ForecastTotals
  months: MonthForecast[]
  assumptions: string[]
}
