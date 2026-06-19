import api from './client'
import type {
  AiModel, AiProvider, ChatRequest, ChatResponse, ConfirmActionRequest, PendingAction, ToolCallSummary,
} from '../types/chat'

export const sendChatMessage = (req: ChatRequest, signal?: AbortSignal) =>
  api.post<ChatResponse>('/ai/chat', req, { signal }).then(r => r.data)

export const confirmAction = (req: ConfirmActionRequest, signal?: AbortSignal) =>
  api.post<ChatResponse>('/ai/chat/confirm', req, { signal }).then(r => r.data)

export const getModels = () =>
  api.get<{ models: AiModel[]; defaultModel: string; defaultProvider: AiProvider }>('/ai/models').then(r => r.data)

export interface StreamDone {
  conversationId?: string | null
  model: string
  toolsSupported: boolean
  toolCalls: ToolCallSummary[]
  pendingAction?: PendingAction | null
}

export interface StreamCallbacks {
  onDelta: (text: string) => void
  onTool: (name: string) => void
  onPendingAction: (action: PendingAction) => void
  onDone: (data: StreamDone) => void
  onError: (message: string) => void
}

/** Strumieniowy czat przez SSE (fetch + ReadableStream). Parsuje zdarzenia delta/tool/pending_action/done/error. */
export async function streamChatMessage(req: ChatRequest, cb: StreamCallbacks, signal?: AbortSignal): Promise<void> {
  const token = localStorage.getItem('auth_token')
  const res = await fetch('/api/v1/ai/chat/stream', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify(req),
    signal,
  })

  if (res.status === 401) {
    localStorage.removeItem('auth_token')
    localStorage.removeItem('auth_email')
    window.location.href = '/login'
    return
  }
  if (!res.ok || !res.body) {
    cb.onError(`Błąd serwera (HTTP ${res.status}).`)
    return
  }

  const reader = res.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''

  while (true) {
    const { done, value } = await reader.read()
    if (done) break
    buffer += decoder.decode(value, { stream: true })

    let sep: number
    while ((sep = buffer.indexOf('\n\n')) !== -1) {
      const rawEvent = buffer.slice(0, sep)
      buffer = buffer.slice(sep + 2)
      dispatchSseEvent(rawEvent, cb)
    }
  }
}

function dispatchSseEvent(raw: string, cb: StreamCallbacks) {
  const eventMatch = raw.match(/^event: (.*)$/m)
  const dataMatch = raw.match(/^data: (.*)$/m)
  if (!eventMatch || !dataMatch) return

  const event = eventMatch[1].trim()
  let data: any
  try { data = JSON.parse(dataMatch[1]) } catch { return }

  switch (event) {
    case 'delta': cb.onDelta(data.text ?? ''); break
    case 'tool': cb.onTool(data.name ?? ''); break
    case 'pending_action': cb.onPendingAction(data); break
    case 'done': cb.onDone(data); break
    case 'error': cb.onError(data.error ?? 'Błąd asystenta AI.'); break
  }
}
