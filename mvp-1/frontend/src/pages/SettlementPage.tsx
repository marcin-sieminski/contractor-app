import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AlertTriangle, Download, FileCheck, FileText, Lock, LockOpen, Save,
} from 'lucide-react'
import {
  downloadSettlementFile, finalizeSettlement, getSettlement, reopenSettlement, saveSettlement,
} from '../api/settlements'
import { SettlementAdjustmentsForm } from '../components/settlement/SettlementAdjustmentsForm'
import { SettlementResultTable } from '../components/settlement/SettlementResultTable'
import { SettlementStatusBadge } from '../components/settlement/SettlementStatusBadge'
import { TaxpayerDataForm } from '../components/settlement/TaxpayerDataForm'
import { TAX_FORM_LABELS, ZUS_STAGE_LABELS } from '../types/forecast'
import type { TaxFormKey, ZusStageKey } from '../types/forecast'
import type { SettlementAdjustments, TaxpayerData } from '../types/settlement'

const TAX_FORMS = Object.keys(TAX_FORM_LABELS) as TaxFormKey[]
const ZUS_STAGES = Object.keys(ZUS_STAGE_LABELS) as ZusStageKey[]

const selectCls =
  'mt-1 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm ' +
  'text-gray-900 dark:text-gray-100 bg-white dark:bg-gray-800'

const btnSecondary =
  'inline-flex items-center gap-1.5 text-sm px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 ' +
  'text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors disabled:opacity-50'

export function SettlementPage() {
  const currentYear = new Date().getFullYear()
  const qc = useQueryClient()
  const [year, setYear] = useState(currentYear - 1) // domyślnie rok, który właśnie się rozlicza
  const [taxForm, setTaxForm] = useState<TaxFormKey>('liniowy')
  const [zusStage, setZusStage] = useState<ZusStageKey>('pelny')

  // Lokalne, niezapisane edycje; null = pokazuj stan z serwera.
  const [draftAdjustments, setDraftAdjustments] = useState<SettlementAdjustments | null>(null)
  const [draftTaxpayer, setDraftTaxpayer] = useState<TaxpayerData | null>(null)
  const isDirty = draftAdjustments !== null || draftTaxpayer !== null

  // Zmiana roku/formy porzuca niezapisane zmiany.
  useEffect(() => {
    setDraftAdjustments(null)
    setDraftTaxpayer(null)
  }, [year, taxForm])

  const { data, isLoading, isError } = useQuery({
    queryKey: ['settlement', year, taxForm, zusStage],
    queryFn: () => getSettlement(year, taxForm, zusStage),
    placeholderData: prev => prev,
  })

  const isFinal = data?.status === 'final'
  const adjustments = draftAdjustments ?? data?.adjustments ?? null
  const taxpayer = draftTaxpayer ?? data?.taxpayer ?? null

  const onMutationSuccess = () => {
    setDraftAdjustments(null)
    setDraftTaxpayer(null)
    qc.invalidateQueries({ queryKey: ['settlement'] })
  }

  const saveMutation = useMutation({
    mutationFn: () =>
      saveSettlement(year, taxForm, { ...adjustments!, taxpayer }, zusStage),
    onSuccess: onMutationSuccess,
  })

  const finalizeMutation = useMutation({
    mutationFn: async () => {
      // Zatwierdzenie zapisuje najpierw bieżący stan formularza.
      await saveSettlement(year, taxForm, { ...adjustments!, taxpayer }, zusStage)
      return finalizeSettlement(year, taxForm, zusStage)
    },
    onSuccess: onMutationSuccess,
  })

  const reopenMutation = useMutation({
    mutationFn: () => reopenSettlement(year, taxForm, zusStage),
    onSuccess: onMutationSuccess,
  })

  const [downloading, setDownloading] = useState<'xml' | 'pdf' | null>(null)
  const [downloadError, setDownloadError] = useState<string | null>(null)
  const download = async (kind: 'xml' | 'pdf') => {
    setDownloading(kind)
    setDownloadError(null)
    try {
      await downloadSettlementFile(year, taxForm, kind)
    } catch (e: unknown) {
      // Błędy walidacji (np. brak danych podatnika) przychodzą jako blob z ProblemDetails.
      let message = `Nie udało się pobrać pliku ${kind.toUpperCase()}.`
      const response = (e as { response?: { data?: Blob } }).response
      if (response?.data instanceof Blob) {
        try {
          const detail = JSON.parse(await response.data.text())?.detail
          if (detail) message = detail
        } catch { /* zostaw komunikat ogólny */ }
      }
      setDownloadError(message)
    } finally {
      setDownloading(null)
    }
  }

  const mutationError =
    (saveMutation.error ?? finalizeMutation.error ?? reopenMutation.error) as
      | { response?: { data?: { detail?: string } } }
      | null
  const errorDetail = mutationError?.response?.data?.detail

  const busy = saveMutation.isPending || finalizeMutation.isPending || reopenMutation.isPending

  const confirmFinalize = () => {
    if (window.confirm(
      `Zatwierdzić rozliczenie ${data?.formCode} za ${year}?\n\n` +
      'Wynik zostanie zamrożony — późniejsze zmiany faktur i wydatków nie wpłyną na to rozliczenie. ' +
      'Edycja będzie wymagała cofnięcia do wersji roboczej.',
    )) {
      finalizeMutation.mutate()
    }
  }

  return (
    <div className="p-6 max-w-5xl">
      <div className="flex items-center justify-between mb-6 flex-wrap gap-3">
        <div className="flex items-center gap-2">
          <FileCheck className="text-blue-600" size={24} />
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Rozliczenie roczne</h1>
        </div>
        {data && <SettlementStatusBadge status={data.status} finalizedAt={data.finalizedAt} />}
      </div>

      <div className="flex flex-wrap items-end gap-3 mb-6">
        <label className="flex flex-col text-xs font-medium text-gray-500 dark:text-gray-400">
          Rok podatkowy
          <select value={year} onChange={e => setYear(Number(e.target.value))} className={selectCls}>
            {[currentYear - 2, currentYear - 1, currentYear].map(y => (
              <option key={y} value={y}>{y}</option>
            ))}
          </select>
        </label>
        <label className="flex flex-col text-xs font-medium text-gray-500 dark:text-gray-400">
          Forma opodatkowania
          <select
            value={taxForm}
            onChange={e => setTaxForm(e.target.value as TaxFormKey)}
            className={selectCls}
          >
            {TAX_FORMS.map(f => (
              <option key={f} value={f}>
                {TAX_FORM_LABELS[f]} ({f === 'ryczalt' ? 'PIT-28' : f === 'skala' ? 'PIT-36' : 'PIT-36L'})
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col text-xs font-medium text-gray-500 dark:text-gray-400">
          Etap ZUS (do podpowiedzi składek)
          <select
            value={zusStage}
            onChange={e => setZusStage(e.target.value as ZusStageKey)}
            className={selectCls}
          >
            {ZUS_STAGES.map(s => (
              <option key={s} value={s}>{ZUS_STAGE_LABELS[s]}</option>
            ))}
          </select>
        </label>
      </div>

      {isLoading && !data && (
        <div className="text-gray-400 dark:text-gray-500 text-sm py-12 text-center">Ładowanie…</div>
      )}
      {isError && (
        <div className="text-red-500 text-sm py-12 text-center">Nie udało się pobrać rozliczenia.</div>
      )}

      {data && adjustments && taxpayer && (
        <div className="space-y-4">
          {data.dataWarnings.length > 0 && (
            <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-xl p-4">
              {data.dataWarnings.map((w, i) => (
                <div key={i} className="flex items-start gap-2 text-sm text-amber-700 dark:text-amber-400">
                  <AlertTriangle size={15} className="mt-0.5 shrink-0" />
                  {w}
                </div>
              ))}
            </div>
          )}

          <SettlementAdjustmentsForm
            prefill={data.prefill}
            adjustments={adjustments}
            disabled={isFinal || busy}
            onChange={setDraftAdjustments}
          />

          {taxForm !== 'ryczalt' && (
            <section className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
              <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-1">IP Box (5%)</h2>
              <p className="text-xs text-gray-400 dark:text-gray-500 mb-3">
                Wymaga odrębnej ewidencji IP i załącznika PIT/IP. Solo-kontraktor bez zakupu praw IP → Nexus zwykle 1,0.
              </p>
              <div className="flex flex-wrap items-end gap-4">
                <label className="inline-flex items-center gap-2 text-sm text-gray-700 dark:text-gray-200">
                  <input
                    type="checkbox"
                    checked={adjustments.ipBoxEnabled}
                    disabled={isFinal || busy}
                    onChange={e => setDraftAdjustments({ ...adjustments, ipBoxEnabled: e.target.checked })}
                    className="rounded"
                  />
                  Rozliczam dochód z IP Box
                </label>
                {adjustments.ipBoxEnabled && (
                  <>
                    <label className="flex flex-col text-xs font-medium text-gray-500 dark:text-gray-400">
                      % dochodu kwalifikowanego
                      <input
                        type="number" min={0} max={100}
                        value={adjustments.ipQualifyingPercent}
                        disabled={isFinal || busy}
                        onChange={e => setDraftAdjustments({
                          ...adjustments,
                          ipQualifyingPercent: Math.min(100, Math.max(0, Number(e.target.value))),
                        })}
                        className={`${selectCls} w-28`}
                      />
                    </label>
                    <label className="flex flex-col text-xs font-medium text-gray-500 dark:text-gray-400">
                      Współczynnik Nexus (0–1)
                      <input
                        type="number" min={0} max={1} step={0.01}
                        value={adjustments.nexusCoefficient}
                        disabled={isFinal || busy}
                        onChange={e => setDraftAdjustments({
                          ...adjustments,
                          nexusCoefficient: Math.min(1, Math.max(0, Number(e.target.value))),
                        })}
                        className={`${selectCls} w-28`}
                      />
                    </label>
                  </>
                )}
              </div>
            </section>
          )}

          <TaxpayerDataForm taxpayer={taxpayer} disabled={isFinal || busy} onChange={setDraftTaxpayer} />

          {isDirty && !isFinal && (
            <div className="text-xs text-amber-600 dark:text-amber-400">
              Masz niezapisane zmiany — zapisz, aby przeliczyć wynik.
            </div>
          )}
          {errorDetail && <div className="text-sm text-red-600 dark:text-red-400">{errorDetail}</div>}
          {downloadError && <div className="text-sm text-red-600 dark:text-red-400">{downloadError}</div>}

          <div className="flex flex-wrap gap-2">
            {!isFinal ? (
              <>
                <button
                  onClick={() => saveMutation.mutate()}
                  disabled={busy}
                  className="inline-flex items-center gap-1.5 text-sm px-4 py-2 rounded-lg bg-blue-600 text-white hover:bg-blue-700 transition-colors disabled:opacity-50"
                >
                  <Save size={15} />
                  {saveMutation.isPending ? 'Zapisywanie…' : 'Zapisz i przelicz'}
                </button>
                <button onClick={confirmFinalize} disabled={busy} className={btnSecondary}>
                  <Lock size={15} />
                  {finalizeMutation.isPending ? 'Zatwierdzanie…' : 'Zatwierdź rozliczenie'}
                </button>
              </>
            ) : (
              <button onClick={() => reopenMutation.mutate()} disabled={busy} className={btnSecondary}>
                <LockOpen size={15} />
                {reopenMutation.isPending ? 'Cofanie…' : 'Cofnij do roboczej'}
              </button>
            )}
            <button onClick={() => download('xml')} disabled={downloading !== null} className={btnSecondary}>
              <FileText size={15} />
              {downloading === 'xml' ? 'Generowanie…' : 'Pobierz XML (e-Deklaracje)'}
            </button>
            <button onClick={() => download('pdf')} disabled={downloading !== null} className={btnSecondary}>
              <Download size={15} />
              {downloading === 'pdf' ? 'Generowanie…' : 'Pobierz PDF (wydruk)'}
            </button>
          </div>

          <p className="text-xs text-gray-400 dark:text-gray-500">
            Plik XML wgrasz w oficjalnym eFormularzu Ministerstwa Finansów
            (klient-eformularz.mf.gov.pl) i tam autoryzujesz wysyłkę. Zalecamy zatwierdzenie
            rozliczenia przed wysyłką.
          </p>

          <SettlementResultTable lines={data.lines} result={data.result} formCode={data.formCode} />

          <div className="bg-blue-50/50 dark:bg-blue-900/10 border border-blue-100 dark:border-blue-900 rounded-xl p-4">
            <div className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-2">
              Założenia i zastrzeżenia
            </div>
            <ul className="list-disc list-inside text-xs text-gray-600 dark:text-gray-400 space-y-1">
              {data.notes.map((n, i) => <li key={i}>{n}</li>)}
              <li>
                Dokument pomocniczy — nie zastępuje deklaracji podatkowej. Skonsultuj z księgową
                przed złożeniem zeznania.
              </li>
            </ul>
          </div>
        </div>
      )}
    </div>
  )
}
