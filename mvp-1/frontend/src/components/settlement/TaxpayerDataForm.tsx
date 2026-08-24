import { useState } from 'react'
import { ChevronDown, ChevronRight, UserRound } from 'lucide-react'
import type { TaxpayerData } from '../../types/settlement'

const inputCls =
  'border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 ' +
  'text-gray-900 dark:text-gray-100 disabled:opacity-60 disabled:cursor-not-allowed'

interface Props {
  taxpayer: TaxpayerData
  disabled: boolean
  onChange: (next: TaxpayerData) => void
}

/** Dane identyfikacyjne podatnika — wymagane do zatwierdzenia, XML i PDF. Zwijane. */
export function TaxpayerDataForm({ taxpayer, disabled, onChange }: Props) {
  const isComplete = Boolean(taxpayer.nip && taxpayer.firstName && taxpayer.lastName && taxpayer.taxOfficeCode)
  const [open, setOpen] = useState(!isComplete)
  const set = (patch: Partial<TaxpayerData>) => onChange({ ...taxpayer, ...patch })

  return (
    <section className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl">
      <button
        type="button"
        onClick={() => setOpen(o => !o)}
        className="w-full flex items-center gap-2 px-4 py-3 text-sm font-semibold text-gray-700 dark:text-gray-200"
      >
        {open ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
        <UserRound size={16} className="text-blue-600" />
        Dane podatnika
        {!isComplete && (
          <span className="ml-2 text-[11px] font-normal text-amber-600 dark:text-amber-400">
            wymagane do zatwierdzenia i eksportu
          </span>
        )}
      </button>

      {open && (
        <div className="px-4 pb-4 grid sm:grid-cols-3 gap-3">
          <Field label="NIP" value={taxpayer.nip} disabled={disabled}
            placeholder="0000000000" onChange={v => set({ nip: v })} />
          <Field label="Imię" value={taxpayer.firstName} disabled={disabled}
            onChange={v => set({ firstName: v })} />
          <Field label="Nazwisko" value={taxpayer.lastName} disabled={disabled}
            onChange={v => set({ lastName: v })} />
          <label className="flex flex-col text-xs font-medium text-gray-500 dark:text-gray-400 gap-1">
            Data urodzenia
            <input
              type="date"
              value={taxpayer.birthDate ?? ''}
              disabled={disabled}
              onChange={e => set({ birthDate: e.target.value || null })}
              className={inputCls}
            />
          </label>
          <Field label="Województwo" value={taxpayer.voivodeship} disabled={disabled}
            placeholder="np. mazowieckie" onChange={v => set({ voivodeship: v })} />
          <Field label="Powiat" value={taxpayer.county} disabled={disabled}
            onChange={v => set({ county: v })} />
          <Field label="Gmina" value={taxpayer.commune} disabled={disabled}
            onChange={v => set({ commune: v })} />
          <Field label="Ulica" value={taxpayer.street} disabled={disabled}
            onChange={v => set({ street: v })} />
          <div className="grid grid-cols-2 gap-3">
            <Field label="Nr domu" value={taxpayer.buildingNumber} disabled={disabled}
              onChange={v => set({ buildingNumber: v })} />
            <Field label="Nr lokalu" value={taxpayer.apartmentNumber} disabled={disabled}
              onChange={v => set({ apartmentNumber: v })} />
          </div>
          <Field label="Kod pocztowy" value={taxpayer.postalCode} disabled={disabled}
            placeholder="00-000" onChange={v => set({ postalCode: v })} />
          <Field label="Miejscowość" value={taxpayer.city} disabled={disabled}
            onChange={v => set({ city: v })} />
          <Field label="Kod urzędu skarbowego" value={taxpayer.taxOfficeCode} disabled={disabled}
            placeholder="np. 1471" onChange={v => set({ taxOfficeCode: v })} />
        </div>
      )}
    </section>
  )
}

function Field({ label, value, disabled, placeholder, onChange }: {
  label: string
  value?: string | null
  disabled: boolean
  placeholder?: string
  onChange: (value: string) => void
}) {
  return (
    <label className="flex flex-col text-xs font-medium text-gray-500 dark:text-gray-400 gap-1">
      {label}
      <input
        type="text"
        value={value ?? ''}
        placeholder={placeholder}
        disabled={disabled}
        onChange={e => onChange(e.target.value)}
        className={inputCls}
      />
    </label>
  )
}
