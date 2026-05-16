export interface Project {
  id: string
  clientId: string
  name: string
  hourlyRate: number
  currency: string
  isActive: boolean
}

export interface Client {
  id: string
  name: string
  nip: string
  regon?: string
  street?: string
  city?: string
  postalCode?: string
  country: string
  isVerified: boolean
  isEuVatPayer: boolean
  projects: Project[]
}

export interface CompanyLookupResult {
  name: string
  nip: string
  regon?: string
  street?: string
  city?: string
  postalCode?: string
  country: string
}
