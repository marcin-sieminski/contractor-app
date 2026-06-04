import { useEffect, useRef, useState, type FormEvent, type KeyboardEvent } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Send, Wrench, Bot, User, ChevronDown, AlertTriangle, X } from 'lucide-react'
import { sendChatMessage, getModels } from '../api/chat'
import type { AiProvider, ChatMessage } from '../types/chat'

function uid() {
  return Math.random().toString(36).slice(2) + Date.now().toString(36)
}

function modelLabel(name: string): string {
  if (name === 'claude-haiku-4-5-20251001') return 'Claude Haiku 3.5'
  if (name === 'claude-sonnet-4-6') return 'Claude Sonnet 4.6'
  if (name === 'claude-opus-4-8') return 'Claude Opus 4'
  const n = name.toLowerCase()
  if (n.includes('bielik')) return 'Bielik 11B v2.3 (Polski)'
  if (n.includes('qwen2.5') && n.includes('14b')) return 'Qwen2.5 14B'
  if (n.includes('qwen2.5') && n.includes('7b')) return 'Qwen2.5 7B'
  return name
}

function formatSize(bytes: number): string {
  return (bytes / 1e9).toFixed(1) + ' GB'
}

export function ChatPage() {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [input, setInput] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [selectedModel, setSelectedModel] = useState<string>('')
  const [selectedProvider, setSelectedProvider] = useState<AiProvider>('ollama')
  const [toolsSupported, setToolsSupported] = useState<boolean>(true)
  const abortRef = useRef<AbortController | null>(null)
  const scrollRef = useRef<HTMLDivElement>(null)

  const { data: modelsData } = useQuery({
    queryKey: ['ai-models'],
    queryFn: getModels,
    staleTime: 60_000,
  })
  const allModels = modelsData?.models ?? []
  const defaultModel = modelsData?.defaultModel
  const defaultProvider = modelsData?.defaultProvider ?? 'ollama'

  const providerModels = allModels.filter(m => m.provider === selectedProvider)

  useEffect(() => {
    if (allModels.length === 0 || selectedModel) return
    const preferred = defaultModel && allModels.find(m => m.name === defaultModel && m.provider === defaultProvider)
    const first = allModels.find(m => m.provider === defaultProvider) ?? allModels[0]
    setSelectedModel(preferred ? preferred.name : first.name)
    setSelectedProvider(defaultProvider)
  }, [allModels, defaultModel, defaultProvider, selectedModel])

  function switchProvider(p: AiProvider) {
    setSelectedProvider(p)
    const first = allModels.find(m => m.provider === p)
    if (first) setSelectedModel(first.name)
    setToolsSupported(true)
  }

  const mutation = useMutation({
    mutationFn: ({ req, signal }: { req: Parameters<typeof sendChatMessage>[0]; signal: AbortSignal }) =>
      sendChatMessage(req, signal),
    onSuccess: (data) => {
      setToolsSupported(data.toolsSupported)
      setMessages(prev => [
        ...prev,
        { id: uid(), role: 'assistant', content: data.content, toolCalls: data.toolCalls }
      ])
    },
    onError: (err: any) => {
      if (err?.code === 'ERR_CANCELED' || err?.name === 'CanceledError' || err?.name === 'AbortError') return
      const base = err?.response?.data?.error ?? err?.message ?? 'Nie udało się uzyskać odpowiedzi.'
      const detail = err?.response?.data?.detail
      setError(detail ? `${base}\n\nSzczegóły: ${detail}` : base)
    }
  })

  function handleCancel() {
    abortRef.current?.abort()
    abortRef.current = null
  }

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: 'smooth' })
  }, [messages, mutation.isPending])

  function handleSend(e?: FormEvent) {
    e?.preventDefault()
    const text = input.trim()
    if (!text || mutation.isPending) return

    setError(null)
    const newUser: ChatMessage = { id: uid(), role: 'user', content: text }
    const next = [...messages, newUser]
    setMessages(next)
    setInput('')

    const controller = new AbortController()
    abortRef.current = controller

    mutation.mutate({
      req: {
        messages: next.map(m => ({ role: m.role, content: m.content })),
        model: selectedModel || undefined,
        provider: selectedProvider,
      },
      signal: controller.signal,
    })
  }

  function handleKeyDown(e: KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  const currentModel = allModels.find(m => m.name === selectedModel)

  return (
    <div className="p-6 flex flex-col h-[calc(100vh-4rem)]">
      <div className="mb-4 flex items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold">Asystent AI</h1>
          <p className="text-xs text-gray-500 mt-1">
            {selectedProvider === 'claude'
              ? 'Claude API (Anthropic) z dostępem do Twoich danych.'
              : 'Lokalny model (Ollama) z dostępem do Twoich danych.'}
          </p>
        </div>

        {allModels.length > 0 && (
          <div className="flex flex-col items-end gap-2">
            <div className="flex rounded-lg border border-gray-300 overflow-hidden text-xs font-medium">
              <button
                onClick={() => switchProvider('ollama')}
                disabled={mutation.isPending}
                className={`px-3 py-1.5 transition-colors ${
                  selectedProvider === 'ollama'
                    ? 'bg-blue-600 text-white'
                    : 'bg-white text-gray-600 hover:bg-gray-50'
                } disabled:opacity-50`}
              >
                Lokalne (Ollama)
              </button>
              <button
                onClick={() => switchProvider('claude')}
                disabled={mutation.isPending}
                className={`px-3 py-1.5 border-l border-gray-300 transition-colors ${
                  selectedProvider === 'claude'
                    ? 'bg-blue-600 text-white'
                    : 'bg-white text-gray-600 hover:bg-gray-50'
                } disabled:opacity-50`}
              >
                Claude API
              </button>
            </div>

            {providerModels.length > 0 && (
              <div className="flex flex-col items-end gap-1">
                <label className="text-xs text-gray-500 font-medium">Model</label>
                <div className="relative">
                  <select
                    value={selectedModel}
                    onChange={e => { setSelectedModel(e.target.value); setToolsSupported(true) }}
                    disabled={mutation.isPending}
                    className="appearance-none bg-white border border-gray-300 rounded-lg pl-3 pr-8 py-2 text-sm font-medium text-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500 cursor-pointer disabled:opacity-50"
                  >
                    {providerModels.map(m => (
                      <option key={m.name} value={m.name}>
                        {modelLabel(m.name)}
                      </option>
                    ))}
                  </select>
                  <ChevronDown size={14} className="absolute right-2.5 top-1/2 -translate-y-1/2 text-gray-400 pointer-events-none" />
                </div>
                {currentModel && currentModel.size > 0 && (
                  <span className="text-[10px] text-gray-400">{formatSize(currentModel.size)}</span>
                )}
              </div>
            )}
          </div>
        )}
      </div>

      {!toolsSupported && selectedProvider === 'ollama' && messages.length > 0 && (
        <div className="flex items-center gap-2 bg-amber-50 border border-amber-200 text-amber-800 rounded-lg px-3 py-2 text-xs mb-3">
          <AlertTriangle size={14} className="shrink-0" />
          Ten model nie obsługuje narzędzi — odpowiedzi nie korzystają z Twoich danych (faktur, czasu, finansów).
          Wybierz Qwen2.5 lub przełącz na Claude API.
        </div>
      )}

      <div ref={scrollRef} className="flex-1 overflow-y-auto space-y-3 pr-2">
        {messages.length === 0 && (
          <div className="bg-gray-50 border border-gray-200 rounded-xl p-4 text-sm text-gray-600">
            <div className="font-medium text-gray-800 mb-2">Zapytaj asystenta o:</div>
            <ul className="space-y-1 list-disc list-inside">
              <li>"Ile zarobiłem w tym roku?"</li>
              <li>"Porównaj formy podatkowe przy 25000 PLN miesięcznie"</li>
              <li>"Czy IP Box mi się opłaca przy 30000 PLN miesięcznie?"</li>
              <li>"Jakie mam zbliżające się terminy podatkowe?"</li>
              <li>"Ilu mam aktywnych klientów i ile godzin w tym miesiącu?"</li>
            </ul>
          </div>
        )}

        {messages.map(m => (
          <MessageBubble key={m.id} message={m} />
        ))}

        {mutation.isPending && (
          <div className="mr-auto max-w-[80%] bg-gray-100 rounded-2xl px-4 py-2 flex items-center gap-2 text-sm text-gray-600">
            <Bot size={16} />
            <span className="inline-flex gap-1">
              <span className="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce [animation-delay:-0.3s]"></span>
              <span className="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce [animation-delay:-0.15s]"></span>
              <span className="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce"></span>
            </span>
            {selectedModel && (
              <span className="text-[10px] text-gray-400 ml-1">{modelLabel(selectedModel)}</span>
            )}
          </div>
        )}

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 rounded-xl px-4 py-2 text-sm">
            {error}
          </div>
        )}
      </div>

      <form onSubmit={handleSend} className="flex gap-2 mt-4">
        <textarea
          value={input}
          onChange={e => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          rows={2}
          disabled={mutation.isPending}
          placeholder="Wpisz pytanie... (Enter = wyślij, Shift+Enter = nowa linia)"
          className="flex-1 resize-none border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        {mutation.isPending ? (
          <button
            type="button"
            onClick={handleCancel}
            className="bg-red-500 text-white hover:bg-red-600 px-4 py-2 rounded-lg font-medium flex items-center gap-2 self-end"
          >
            <X size={16} /> Anuluj
          </button>
        ) : (
          <button
            type="submit"
            disabled={!input.trim()}
            className="bg-blue-600 text-white hover:bg-blue-700 disabled:bg-gray-300 disabled:cursor-not-allowed px-4 py-2 rounded-lg font-medium flex items-center gap-2 self-end"
          >
            <Send size={16} /> Wyślij
          </button>
        )}
      </form>
    </div>
  )
}

const TOOL_LABELS: Record<string, string> = {
  get_financial_summary: 'Finanse',
  get_invoices: 'Faktury',
  get_clients: 'Klienci',
  get_expenses: 'Wydatki',
  get_time_entries: 'Czas pracy',
  get_time_summary: 'Podsumowanie czasu',
  get_upcoming_deadlines: 'Terminy',
  get_active_timer: 'Timer',
  start_timer: 'Start timera',
  stop_timer: 'Stop timera',
  calculate_zus: 'Kalkulator ZUS',
  calculate_ip_box: 'IP Box',
  compare_tax_forms: 'Formy podatkowe',
  forecast_annual_tax: 'Prognoza podatku',
  lookup_company_by_nip: 'Wyszukiwanie NIP',
}

function toolLabel(name: string) {
  return TOOL_LABELS[name] ?? name
}

function MessageBubble({ message }: { message: ChatMessage }) {
  if (message.role === 'user') {
    return (
      <div className="ml-auto max-w-[80%] bg-blue-600 text-white rounded-2xl px-4 py-2 whitespace-pre-wrap break-words">
        <div className="flex items-center gap-1 text-xs opacity-80 mb-1">
          <User size={12} /> Ty
        </div>
        {message.content}
      </div>
    )
  }

  const hasTools = message.toolCalls && message.toolCalls.length > 0

  return (
    <div className="mr-auto max-w-[80%] bg-gray-100 rounded-2xl px-4 py-2">
      <div className="flex items-center gap-1 text-xs text-gray-500 mb-1">
        <Bot size={12} /> Asystent
      </div>
      <div className="whitespace-pre-wrap break-words text-gray-900">
        {message.content || <span className="italic text-gray-500">(brak odpowiedzi)</span>}
      </div>

      {hasTools && (
        <div className="mt-2 pt-2 border-t border-gray-200">
          <div className="flex flex-wrap items-center gap-1.5">
            <span className="text-[11px] text-gray-400 flex items-center gap-1">
              <Wrench size={11} /> Użyto:
            </span>
            {message.toolCalls!.map((tc, i) => (
              <span
                key={i}
                className="inline-flex items-center gap-1 bg-blue-50 text-blue-700 border border-blue-200 rounded-full px-2 py-0.5 text-[11px] font-medium"
              >
                {toolLabel(tc.name)}
              </span>
            ))}
          </div>

          <details className="mt-2 text-xs">
            <summary className="cursor-pointer text-gray-400 hover:text-gray-600 select-none">
              Szczegóły wywołań
            </summary>
            <div className="mt-2 space-y-2">
              {message.toolCalls!.map((tc, i) => (
                <div key={i} className="bg-white border border-gray-200 rounded-lg p-2 font-mono">
                  <div className="font-semibold text-blue-700">{tc.name}</div>
                  <div className="text-gray-500 mt-1">args:</div>
                  <pre className="text-[10px] overflow-x-auto">{JSON.stringify(tc.args, null, 2)}</pre>
                  <div className="text-gray-500 mt-1">result:</div>
                  <pre className="text-[10px] overflow-x-auto max-h-40">{JSON.stringify(tc.result, null, 2)}</pre>
                </div>
              ))}
            </div>
          </details>
        </div>
      )}
    </div>
  )
}
