// Skraca nazwę klienta na podpowiedź w nawiasie: usuwa formę prawną (sp. z o.o., S.A. itd.) i przycina długie nazwy.
export function shortClientName(name: string): string {
  const cleaned = name
    .replace(/[\s,]+(sp\.?\s*z\s*o\.?\s*o\.?|s\.?\s*k\.?\s*a\.?|sp\.?\s*k\.?|sp\.?\s*j\.?|s\.?\s*a\.?)\.?\s*$/i, '')
    .trim() || name.trim()
  return cleaned.length > 16 ? cleaned.slice(0, 15).trimEnd() + '…' : cleaned
}
