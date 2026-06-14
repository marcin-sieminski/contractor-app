export interface ProjectProfitability {
  projectId: string
  projectName: string
  revenuePln: number
  billableHours: number
  effectiveRatePlnPerHour: number
}

export interface ClientProfitability {
  clientId: string
  clientName: string
  clientNip: string
  revenuePln: number
  billableHours: number
  effectiveRatePlnPerHour: number
  revenueSharePercent: number
  invoiceCount: number
  projects: ProjectProfitability[]
}

export interface Profitability {
  year: number
  period: string
  totalRevenuePln: number
  totalBillableHours: number
  overallEffectiveRate: number
  hasConcentrationRisk: boolean
  concentrationWarning: string | null
  clients: ClientProfitability[]
}
