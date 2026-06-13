import api from './client'
import type {
  AnnualSettlement,
  SaveSettlementBody,
  SettlementListItem,
  TaxFormKey,
  ZusStageKey,
} from '../types/settlement'

export const listSettlements = () =>
  api.get<SettlementListItem[]>('/settlements').then(r => r.data)

export const getSettlement = (year: number, taxForm: TaxFormKey, zusStage?: ZusStageKey) =>
  api
    .get<AnnualSettlement>(`/settlements/${year}/${taxForm}`, { params: { zusStage } })
    .then(r => r.data)

export const saveSettlement = (
  year: number,
  taxForm: TaxFormKey,
  body: SaveSettlementBody,
  zusStage?: ZusStageKey,
) =>
  api
    .put<AnnualSettlement>(`/settlements/${year}/${taxForm}`, body, { params: { zusStage } })
    .then(r => r.data)

export const finalizeSettlement = (year: number, taxForm: TaxFormKey, zusStage?: ZusStageKey) =>
  api
    .post<AnnualSettlement>(`/settlements/${year}/${taxForm}/finalize`, null, { params: { zusStage } })
    .then(r => r.data)

export const reopenSettlement = (year: number, taxForm: TaxFormKey, zusStage?: ZusStageKey) =>
  api
    .post<AnnualSettlement>(`/settlements/${year}/${taxForm}/reopen`, null, { params: { zusStage } })
    .then(r => r.data)

/** Pobiera XML deklaracji lub PDF wydruku i zapisuje jako plik w przeglądarce. */
export const downloadSettlementFile = async (
  year: number,
  taxForm: TaxFormKey,
  kind: 'xml' | 'pdf',
) => {
  const response = await api.get(`/settlements/${year}/${taxForm}/${kind}`, {
    responseType: 'blob',
  })
  const disposition: string = response.headers['content-disposition'] ?? ''
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition)
  const fileName = match?.[1] ? decodeURIComponent(match[1]) : `rozliczenie_${year}.${kind}`

  const url = URL.createObjectURL(response.data as Blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  document.body.appendChild(anchor)
  anchor.click()
  anchor.remove()
  URL.revokeObjectURL(url)
}
