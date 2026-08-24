import type { TaxFormKey, ZusStageKey } from './forecast'

export type SettlementStatus = 'draft' | 'final'

export interface SettlementLine {
  boxId: string
  label: string
  value: number
  description?: string | null
}

/** Wartości wyliczone z danych aplikacji — podpowiedzi do pól edytowalnych. */
export interface SettlementPrefill {
  revenue: number
  costs: number
  zusSocialTheoretical: number
  zusHealthTheoretical: number
  taxPrepaymentsTheoretical: number
}

export interface SettlementAdjustments {
  revenueOverride?: number | null
  costsOverride?: number | null
  zusSocialPaid: number
  zusHealthPaid: number
  taxPrepaymentsPaid: number
  ipBoxEnabled: boolean
  ipQualifyingPercent: number
  nexusCoefficient: number
}

export interface TaxpayerData {
  nip?: string | null
  firstName?: string | null
  lastName?: string | null
  birthDate?: string | null // yyyy-MM-dd
  street?: string | null
  buildingNumber?: string | null
  apartmentNumber?: string | null
  postalCode?: string | null
  city?: string | null
  taxOfficeCode?: string | null
  voivodeship?: string | null
  county?: string | null
  commune?: string | null
}

export interface SettlementResult {
  przychod: number
  koszty: number
  dochod: number
  strata: number
  skladkiSpoleczneOdliczone: number
  skladkaZdrowotnaOdliczona: number
  podstawaOpodatkowania: number
  podstawaIpBox: number
  podatekIpBox: number
  podatekPozaIpBox: number
  podatekNalezny: number
  zaliczkiWplacone: number
  doZaplaty: number
  nadplata: number
  efektywnaStawkaOdPrzychodu: number
}

export interface AnnualSettlement {
  year: number
  taxForm: TaxFormKey
  taxFormLabel: string
  formCode: string // PIT-36 | PIT-36L | PIT-28
  status: SettlementStatus
  isSaved: boolean
  finalizedAt?: string | null
  isYearParamsExact: boolean
  prefill: SettlementPrefill
  adjustments: SettlementAdjustments
  taxpayer: TaxpayerData
  result: SettlementResult
  lines: SettlementLine[]
  notes: string[]
  dataWarnings: string[]
}

export interface SettlementListItem {
  year: number
  taxForm: TaxFormKey
  taxFormLabel: string
  formCode: string
  status: SettlementStatus
  podatekNalezny?: number | null
  doZaplaty?: number | null
  nadplata?: number | null
  finalizedAt?: string | null
  updatedAt: string
}

export interface SaveSettlementBody {
  revenueOverride?: number | null
  costsOverride?: number | null
  zusSocialPaid: number
  zusHealthPaid: number
  taxPrepaymentsPaid: number
  ipBoxEnabled: boolean
  ipQualifyingPercent: number
  nexusCoefficient: number
  taxpayer?: TaxpayerData | null
}

export type { TaxFormKey, ZusStageKey }
