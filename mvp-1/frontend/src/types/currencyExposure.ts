export interface CurrencyBreakdown {
  currency: string
  totalNetForeign: number
  totalNetPln: number
  sharePercent: number
  invoiceCount: number
  avgExchangeRate: number
  minExchangeRate: number
  maxExchangeRate: number
}

export interface MonthlyFxData {
  month: number
  monthName: string
  plnRevenue: number
  eurRevenuePln: number
  usdRevenuePln: number
  gbpRevenuePln: number
  chfRevenuePln: number
  totalRevenuePln: number
  eurRate: number | null
  usdRate: number | null
  gbpRate: number | null
}

export interface SensitivityRow {
  changePercent: number
  eurImpact: number
  usdImpact: number
  gbpImpact: number
  chfImpact: number
  totalImpact: number
  adjustedTotalRevenue: number
}

export interface CurrencyExposure {
  year: number
  totalRevenuePln: number
  foreignRevenuePln: number
  plnOnlyRevenuePln: number
  foreignSharePercent: number
  breakdown: CurrencyBreakdown[]
  monthlyTrend: MonthlyFxData[]
  sensitivity: SensitivityRow[]
}
