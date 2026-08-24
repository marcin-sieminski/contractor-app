import { RotateCcw } from 'lucide-react'
import type { SettlementAdjustments, SettlementPrefill } from '../../types/settlement'

const fmt = (v: number) =>
  v.toLocaleString('pl-PL', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' zł'

const inputCls =
  'w-40 text-right tabular-nums border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm ' +
  'bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 disabled:opacity-60 disabled:cursor-not-allowed'

interface Props {
  prefill: SettlementPrefill
  adjustments: SettlementAdjustments
  disabled: boolean
  onChange: (next: SettlementAdjustments) => void
}

/**
 * Sekcje „Dane z aplikacji" (przychód/koszty z możliwością ręcznej korekty)
 * oraz „Składki i zaliczki zapłacone" (metoda kasowa, prefill z wartości teoretycznych).
 */
export function SettlementAdjustmentsForm({ prefill, adjustments, disabled, onChange }: Props) {
  const set = (patch: Partial<SettlementAdjustments>) => onChange({ ...adjustments, ...patch })

  const parseAmount = (raw: string): number => Math.max(0, Number(raw) || 0)
  const parseOverride = (raw: string): number | null =>
    raw.trim() === '' ? null : Math.max(0, Number(raw) || 0)

  return (
    <div className="space-y-4">
      <section className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
        <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-1">Dane z aplikacji</h2>
        <p className="text-xs text-gray-500 dark:text-gray-400 mb-4">
          Sumy roczne z faktur (bez szkiców) i wydatków. Pozostaw pole korekty puste, aby użyć danych z aplikacji.
        </p>
        <div className="grid sm:grid-cols-2 gap-4">
          <AmountField
            label="Przychód roczny"
            hint={`z faktur: ${fmt(prefill.revenue)}`}
            value={adjustments.revenueOverride}
            placeholder={prefill.revenue.toFixed(2)}
            disabled={disabled}
            onChange={raw => set({ revenueOverride: parseOverride(raw) })}
            onReset={adjustments.revenueOverride != null ? () => set({ revenueOverride: null }) : undefined}
          />
          <AmountField
            label="Koszty roczne"
            hint={`z wydatków: ${fmt(prefill.costs)}`}
            value={adjustments.costsOverride}
            placeholder={prefill.costs.toFixed(2)}
            disabled={disabled}
            onChange={raw => set({ costsOverride: parseOverride(raw) })}
            onReset={adjustments.costsOverride != null ? () => set({ costsOverride: null }) : undefined}
          />
        </div>
      </section>

      <section className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
        <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-1">
          Składki i zaliczki zapłacone w roku (metoda kasowa)
        </h2>
        <p className="text-xs text-gray-500 dark:text-gray-400 mb-4">
          W zeznaniu uwzględnia się kwoty faktycznie zapłacone w roku podatkowym — zweryfikuj z przelewami.
          Podpowiedzi wyliczono ze stawek dla wybranego roku i etapu ZUS.
        </p>
        <div className="grid sm:grid-cols-3 gap-4">
          <AmountField
            label="Składki społeczne ZUS"
            hint={`wyliczone: ${fmt(prefill.zusSocialTheoretical)}`}
            value={adjustments.zusSocialPaid}
            disabled={disabled}
            onChange={raw => set({ zusSocialPaid: parseAmount(raw) })}
            onReset={adjustments.zusSocialPaid !== prefill.zusSocialTheoretical
              ? () => set({ zusSocialPaid: prefill.zusSocialTheoretical })
              : undefined}
          />
          <AmountField
            label="Składka zdrowotna"
            hint={`wyliczona: ${fmt(prefill.zusHealthTheoretical)}`}
            value={adjustments.zusHealthPaid}
            disabled={disabled}
            onChange={raw => set({ zusHealthPaid: parseAmount(raw) })}
            onReset={adjustments.zusHealthPaid !== prefill.zusHealthTheoretical
              ? () => set({ zusHealthPaid: prefill.zusHealthTheoretical })
              : undefined}
          />
          <AmountField
            label="Zaliczki na podatek / ryczałt"
            hint={`wyliczone: ${fmt(prefill.taxPrepaymentsTheoretical)}`}
            value={adjustments.taxPrepaymentsPaid}
            disabled={disabled}
            onChange={raw => set({ taxPrepaymentsPaid: parseAmount(raw) })}
            onReset={adjustments.taxPrepaymentsPaid !== prefill.taxPrepaymentsTheoretical
              ? () => set({ taxPrepaymentsPaid: prefill.taxPrepaymentsTheoretical })
              : undefined}
          />
        </div>
      </section>
    </div>
  )
}

function AmountField({ label, hint, value, placeholder, disabled, onChange, onReset }: {
  label: string
  hint: string
  value: number | null | undefined
  placeholder?: string
  disabled: boolean
  onChange: (raw: string) => void
  onReset?: () => void
}) {
  return (
    <label className="flex flex-col text-xs font-medium text-gray-500 dark:text-gray-400 gap-1">
      <span className="flex items-center gap-2">
        {label}
        {onReset && !disabled && (
          <button
            type="button"
            onClick={onReset}
            className="inline-flex items-center gap-0.5 text-[11px] text-blue-600 dark:text-blue-400 hover:underline"
            title="Przywróć wartość wyliczoną"
          >
            <RotateCcw size={11} aria-hidden="true" /> przywróć wyliczone
          </button>
        )}
      </span>
      <input
        type="number"
        min={0}
        step="0.01"
        value={value ?? ''}
        placeholder={placeholder}
        disabled={disabled}
        onChange={e => onChange(e.target.value)}
        className={inputCls}
      />
      <span className="text-[11px] text-gray-500 dark:text-gray-400">{hint}</span>
    </label>
  )
}
