import { useEffect } from 'react'

/**
 * Zamyka dialog po naciśnięciu klawisza Escape. Zmiana czysto addytywna —
 * nie wpływa na istniejące sposoby zamykania (przyciski „Anuluj”/„X”/tło).
 * Listener jest rejestrowany na `document` i sprzątany przy odmontowaniu okna.
 */
export function useDialogClose(onClose: () => void) {
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', handler)
    return () => document.removeEventListener('keydown', handler)
  }, [onClose])
}
