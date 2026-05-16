import { useState, useEffect, useRef } from 'react'
import { lookupNip } from '../api/clients'
import type { CompanyLookupResult } from '../types/client'

export function useNipLookup(nip: string, onResult: (r: CompanyLookupResult) => void) {
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    const digits = nip.replace(/\D/g, '')
    if (digits.length !== 10) { setError(null); return }

    if (timerRef.current) clearTimeout(timerRef.current)
    timerRef.current = setTimeout(async () => {
      setLoading(true)
      setError(null)
      try {
        const result = await lookupNip(digits)
        onResult(result)
      } catch {
        setError('NIP nie znaleziony w CEIDG/GUS')
      } finally {
        setLoading(false)
      }
    }, 500)

    return () => { if (timerRef.current) clearTimeout(timerRef.current) }
  }, [nip])

  return { loading, error }
}
