import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getClients } from '../api/clients'

// Mapa clientId → nazwa klienta, zbudowana z cache'owanego zapytania ['clients'].
export function useClientNameById() {
  const { data: clients = [] } = useQuery({ queryKey: ['clients'], queryFn: getClients })
  return useMemo(() => {
    const map = new Map<string, string>()
    for (const c of clients) map.set(c.id, c.name)
    return map
  }, [clients])
}
