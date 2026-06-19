import { useEffect, useRef, useState, type FormEvent, type KeyboardEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Send, Wrench, Bot, User, ChevronDown, AlertTriangle, X, Plus, Trash2, MessageSquare } from 'lucide-react'
import { streamChatMessage, confirmAction, getModels } from '../api/chat'
import { listConversations, getConversation, deleteConversation } from '../api/conversations'
import type { AiProvider, ChatMessage, ConfirmActionRequest, ConversationSummary } from '../types/chat'

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

// Przykładowe pytania na ekranie startowym — odzwierciedlają możliwości
// zaimplementowanych narzędzi MCP (analizy + akcje na danych użytkownika).
const SUGGESTION_GROUPS: { title: string; items: string[] }[] = [
  {
    title: 'Finanse i podatki',
    items: [
      'Jak stoję finansowo w tym roku?',
      'Ile zostanie mi na rękę po podatkach?',
      'Która forma opodatkowania mi się opłaca — ryczałt, liniowy czy skala?',
      'Czy opłaca mi się IP Box?',
    ],
  },
  {
    title: 'Płynność, ZUS i terminy',
    items: [
      'Czy starczy mi na najbliższy ZUS i podatki?',
      'Jakie mam nadchodzące terminy ZUS, VAT i PIT?',
      'Ile zalegam z podatkami i ZUS?',
      'Co wymaga teraz mojej uwagi?',
    ],
  },
  {
    title: 'Klienci, praca i waluty',
    items: [
      'Który klient jest dla mnie najbardziej opłacalny?',
      'Ile mam niezafakturowanych godzin?',
      'Jak duże jest moje ryzyko walutowe przy EUR/USD?',
    ],
  },
  {
    title: 'Asystent może też wykonać',
    items: [
      'Wystaw fakturę dla klienta za poprzedni miesiąc',
      'Sprawdź kontrahenta po numerze NIP',
      'Uruchom licznik czasu dla projektu',
    ],
  },
]

export function ChatPage() {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [input, setInput] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [selectedModel, setSelectedModel] = useState<string>('')
  const [selectedProvider, setSelectedProvider] = useState<AiProvider>('ollama')
  const [toolsSupported, setToolsSupported] = useState<boolean>(true)
  const [confirmingId, setConfirmingId] = useState<string | null>(null)
  const [conversationId, setConversationId] = useState<string | null>(null)
  const [isStreaming, setIsStreaming] = useState(false)
  const abortRef = useRef<AbortController | null>(null)
  const scrollRef = useRef<HTMLDivElement>(null)
  const textareaRef = useRef<HTMLTextAreaElement>(null)
  const queryClient = useQueryClient()

  const { data: conversations = [] } = useQuery({
    queryKey: ['conversations'],
    queryFn: listConversations,
    staleTime: 10_000,
  })

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

  const confirmMutation = useMutation({
    mutationFn: ({ req, signal }: { req: ConfirmActionRequest; messageId: string; signal: AbortSignal }) =>
      confirmAction(req, signal),
    onSuccess: (data, vars) => {
      setMessages(prev => [
        ...prev.map(m => (m.id === vars.messageId ? { ...m, pendingAction: undefined } : m)),
        { id: uid(), role: 'assistant', content: data.content, toolCalls: data.toolCalls },
      ])
      if (data.conversationId) setConversationId(data.conversationId)
      queryClient.invalidateQueries({ queryKey: ['conversations'] })
      setConfirmingId(null)
    },
    onError: (err: any) => {
      setConfirmingId(null)
      if (err?.code === 'ERR_CANCELED' || err?.name === 'CanceledError' || err?.name === 'AbortError') return
      const base = err?.response?.data?.error ?? err?.message ?? 'Nie udało się wykonać akcji.'
      setError(base)
    },
  })

  function handleConfirm(message: ChatMessage) {
    if (!message.pendingAction || confirmMutation.isPending) return
    setError(null)
    setConfirmingId(message.id)
    const controller = new AbortController()
    abortRef.current = controller
    confirmMutation.mutate({
      req: {
        messages: messages
          .filter(m => m.id !== message.id)
          .map(m => ({ role: m.role, content: m.content })),
        action: { tool: message.pendingAction.tool, args: message.pendingAction.args },
        model: selectedModel || undefined,
        provider: selectedProvider,
        conversationId: conversationId ?? undefined,
      },
      messageId: message.id,
      signal: controller.signal,
    })
  }

  function handleCancelPending(message: ChatMessage) {
    setMessages(prev => [
      ...prev.map(m => (m.id === message.id ? { ...m, pendingAction: undefined } : m)),
      { id: uid(), role: 'assistant', content: 'Anulowano — akcja nie została wykonana.' },
    ])
  }

  function handleNewChat() {
    handleCancel()
    setMessages([])
    setConversationId(null)
    setError(null)
  }

  async function handleSelectConversation(id: string) {
    if (id === conversationId) return
    handleCancel()
    setError(null)
    try {
      const detail = await getConversation(id)
      setMessages(detail.messages.map(m => ({
        id: uid(),
        role: m.role,
        content: m.content,
        toolCalls: m.toolCalls ?? undefined,
      })))
      setConversationId(detail.id)
    } catch {
      setError('Nie udało się wczytać rozmowy.')
    }
  }

  async function handleDeleteConversation(id: string) {
    try {
      await deleteConversation(id)
      if (id === conversationId) handleNewChat()
      queryClient.invalidateQueries({ queryKey: ['conversations'] })
    } catch {
      setError('Nie udało się usunąć rozmowy.')
    }
  }

  function handleCancel() {
    abortRef.current?.abort()
    abortRef.current = null
  }

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: 'smooth' })
  }, [messages, isStreaming, confirmMutation.isPending])

  async function handleSend(e?: FormEvent) {
    e?.preventDefault()
    const text = input.trim()
    if (!text || isStreaming) return

    setError(null)
    const newUser: ChatMessage = { id: uid(), role: 'user', content: text }
    const assistantId = uid()
    const next = [...messages, newUser]
    setMessages([...next, { id: assistantId, role: 'assistant', content: '' }])
    setInput('')

    const controller = new AbortController()
    abortRef.current = controller
    setIsStreaming(true)

    try {
      await streamChatMessage(
        {
          messages: next.map(m => ({ role: m.role, content: m.content })),
          model: selectedModel || undefined,
          provider: selectedProvider,
          conversationId: conversationId ?? undefined,
        },
        {
          onDelta: t =>
            setMessages(prev => prev.map(m => (m.id === assistantId ? { ...m, content: m.content + t } : m))),
          onTool: name =>
            setMessages(prev => prev.map(m =>
              m.id === assistantId
                ? { ...m, toolCalls: [...(m.toolCalls ?? []), { name, args: null, result: null }] }
                : m)),
          onPendingAction: action =>
            setMessages(prev => prev.map(m => (m.id === assistantId ? { ...m, pendingAction: action } : m))),
          onDone: d => {
            setToolsSupported(d.toolsSupported)
            if (d.conversationId) setConversationId(d.conversationId)
            if (d.toolCalls && d.toolCalls.length > 0)
              setMessages(prev => prev.map(m => (m.id === assistantId ? { ...m, toolCalls: d.toolCalls } : m)))
            queryClient.invalidateQueries({ queryKey: ['conversations'] })
          },
          onError: msg => setError(msg),
        },
        controller.signal,
      )
    } catch (err: any) {
      if (err?.name !== 'AbortError') setError(err?.message ?? 'Nie udało się uzyskać odpowiedzi.')
    } finally {
      setIsStreaming(false)
      abortRef.current = null
    }
  }

  function handleKeyDown(e: KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  function applySuggestion(text: string) {
    setInput(text)
    requestAnimationFrame(() => textareaRef.current?.focus())
  }

  const currentModel = allModels.find(m => m.name === selectedModel)

  return (
    <div className="flex h-[calc(100vh-4rem)]">
      <ConversationSidebar
        conversations={conversations}
        activeId={conversationId}
        onSelect={handleSelectConversation}
        onNew={handleNewChat}
        onDelete={handleDeleteConversation}
      />
      <div className="flex-1 p-4 md:p-6 flex flex-col min-w-0">
      <div className="mb-4 flex items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Asystent AI</h1>
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
            {selectedProvider === 'claude'
              ? 'Claude API (Anthropic) z dostępem do Twoich danych.'
              : 'Lokalny model (Ollama) z dostępem do Twoich danych.'}
          </p>
        </div>

        {allModels.length > 0 && (
          <div className="flex flex-col items-end gap-2">
            <div className="flex rounded-lg border border-gray-300 dark:border-gray-600 overflow-hidden text-xs font-medium">
              <button
                onClick={() => switchProvider('ollama')}
                disabled={isStreaming}
                className={`px-3 py-1.5 transition-colors ${
                  selectedProvider === 'ollama'
                    ? 'bg-blue-600 text-white'
                    : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'
                } disabled:opacity-50`}
              >
                Lokalne (Ollama)
              </button>
              <button
                onClick={() => switchProvider('claude')}
                disabled={isStreaming}
                className={`px-3 py-1.5 border-l border-gray-300 dark:border-gray-600 transition-colors ${
                  selectedProvider === 'claude'
                    ? 'bg-blue-600 text-white'
                    : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'
                } disabled:opacity-50`}
              >
                Claude API
              </button>
            </div>

            {providerModels.length > 0 && (
              <div className="flex flex-col items-end gap-1">
                <label className="text-xs text-gray-500 dark:text-gray-400 font-medium">Model</label>
                <div className="relative">
                  <select
                    aria-label="Model"
                    value={selectedModel}
                    onChange={e => { setSelectedModel(e.target.value); setToolsSupported(true) }}
                    disabled={isStreaming}
                    className="appearance-none bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg pl-3 pr-8 py-2 text-sm font-medium text-gray-800 dark:text-gray-200 focus:outline-none focus:ring-2 focus:ring-blue-500 cursor-pointer disabled:opacity-50"
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
                  <span className="text-[10px] text-gray-500 dark:text-gray-400">{formatSize(currentModel.size)}</span>
                )}
              </div>
            )}
          </div>
        )}
      </div>

      {!toolsSupported && selectedProvider === 'ollama' && messages.length > 0 && (
        <div className="flex items-center gap-2 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 text-amber-800 dark:text-amber-400 rounded-lg px-3 py-2 text-xs mb-3">
          <AlertTriangle size={14} className="shrink-0" />
          Ten model nie obsługuje narzędzi — odpowiedzi nie korzystają z Twoich danych (faktur, czasu, finansów).
          Wybierz Qwen2.5 lub przełącz na Claude API.
        </div>
      )}

      <div ref={scrollRef} className="flex-1 overflow-y-auto space-y-3 pr-2">
        {messages.length === 0 && (
          <div className="bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4 text-sm text-gray-600 dark:text-gray-300">
            <div className="font-medium text-gray-800 dark:text-gray-200 mb-1">
              Zapytaj asystenta — ma dostęp do Twoich danych i może działać w aplikacji
            </div>
            <p className="text-xs text-gray-500 dark:text-gray-400 mb-3">
              Kliknij przykład, aby wstawić go do pola, albo wpisz własne pytanie.
            </p>
            <div className="space-y-3">
              {SUGGESTION_GROUPS.map(group => (
                <div key={group.title}>
                  <div className="text-[11px] font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400 mb-1.5">
                    {group.title}
                  </div>
                  <div className="flex flex-wrap gap-1.5">
                    {group.items.map(q => (
                      <button
                        key={q}
                        type="button"
                        onClick={() => applySuggestion(q)}
                        className="text-left bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 hover:border-blue-400 dark:hover:border-blue-500 hover:bg-blue-50 dark:hover:bg-blue-900/20 text-gray-700 dark:text-gray-300 rounded-full px-3 py-1.5 text-xs transition-colors"
                      >
                        {q}
                      </button>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {messages.map((m, i) => (
          <MessageBubble
            key={m.id}
            message={m}
            onConfirm={handleConfirm}
            onCancel={handleCancelPending}
            confirming={confirmMutation.isPending && confirmingId === m.id}
            streaming={isStreaming && i === messages.length - 1 && m.role === 'assistant'}
          />
        ))}

        {error && (
          <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 rounded-xl px-4 py-2 text-sm">
            {error}
          </div>
        )}
      </div>

      <form onSubmit={handleSend} className="flex gap-2 mt-4">
        <textarea
          ref={textareaRef}
          aria-label="Wiadomość do asystenta"
          value={input}
          onChange={e => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          rows={2}
          disabled={isStreaming}
          placeholder="Wpisz pytanie... (Enter = wyślij, Shift+Enter = nowa linia)"
          className="flex-1 resize-none border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        {isStreaming ? (
          <button
            type="button"
            onClick={handleCancel}
            className="bg-red-500 text-white hover:bg-red-600 px-4 py-2 rounded-lg font-medium flex items-center gap-2 self-end"
          >
            <X size={16} aria-hidden="true" /> Anuluj
          </button>
        ) : (
          <button
            type="submit"
            disabled={!input.trim()}
            className="bg-blue-600 text-white hover:bg-blue-700 disabled:bg-gray-300 dark:disabled:bg-gray-700 disabled:cursor-not-allowed px-4 py-2 rounded-lg font-medium flex items-center gap-2 self-end"
          >
            <Send size={16} aria-hidden="true" /> Wyślij
          </button>
        )}
      </form>
      </div>
    </div>
  )
}

function ConversationSidebar({ conversations, activeId, onSelect, onNew, onDelete }: {
  conversations: ConversationSummary[]
  activeId: string | null
  onSelect: (id: string) => void
  onNew: () => void
  onDelete: (id: string) => void
}) {
  return (
    <aside className="hidden md:flex w-64 shrink-0 border-r border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900 flex-col">
      <div className="p-3">
        <button
          onClick={onNew}
          className="w-full flex items-center justify-center gap-2 bg-blue-600 text-white hover:bg-blue-700 rounded-lg px-3 py-2 text-sm font-medium"
        >
          <Plus size={16} /> Nowa rozmowa
        </button>
      </div>
      <div className="flex-1 overflow-y-auto px-2 pb-3 space-y-1">
        {conversations.length === 0 && (
          <p className="text-xs text-gray-500 dark:text-gray-400 px-2 py-4 text-center">Brak zapisanych rozmów.</p>
        )}
        {conversations.map(c => (
          <div
            key={c.id}
            onClick={() => onSelect(c.id)}
            className={`group flex items-center gap-2 rounded-lg px-2 py-2 cursor-pointer text-sm ${
              c.id === activeId
                ? 'bg-blue-100 dark:bg-blue-900/40 text-blue-800 dark:text-blue-200'
                : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800'
            }`}
          >
            <MessageSquare size={14} className="shrink-0 opacity-60" aria-hidden="true" />
            <span className="flex-1 truncate" title={c.title}>{c.title}</span>
            <button
              onClick={e => { e.stopPropagation(); onDelete(c.id) }}
              aria-label="Usuń rozmowę"
              className="opacity-0 group-hover:opacity-100 focus:opacity-100 text-gray-500 hover:text-red-500 transition-opacity"
              title="Usuń rozmowę"
            >
              <Trash2 size={14} aria-hidden="true" />
            </button>
          </div>
        ))}
      </div>
    </aside>
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
  get_financial_forecast: 'Prognoza finansowa',
  get_cash_flow_forecast: 'Cash flow',
  get_currency_exposure: 'Ekspozycja walutowa',
  get_client_profitability: 'Rentowność klientów',
  get_work_analytics: 'Analityka pracy',
  get_tax_obligations: 'Zobowiązania',
  get_ip_box_progress: 'IP Box (postęp)',
  get_financial_document: 'Dokumenty finansowe',
  list_annual_settlements: 'Rozliczenia roczne',
  get_annual_settlement: 'Rozliczenie roczne',
  list_projects: 'Projekty',
  get_invoice_by_id: 'Faktura (szczegóły)',
  get_financial_health_check: 'Kondycja finansowa',
  get_action_items: 'Co wymaga uwagi',
  get_tax_optimization_advice: 'Optymalizacja podatków',
  pause_timer: 'Pauza timera',
  resume_timer: 'Wznów timer',
  create_manual_time_entry: 'Wpis czasu',
  update_time_entry: 'Edycja czasu',
  delete_time_entry: 'Usuń wpis czasu',
  create_client: 'Nowy klient',
  update_client: 'Edycja klienta',
  create_expense: 'Nowy wydatek',
  update_expense: 'Edycja wydatku',
  delete_expense: 'Usuń wydatek',
  scan_receipt: 'Skan paragonu',
  record_tax_payment: 'Rejestracja wpłaty',
  set_time_entry_ip_work: 'Oznacz pracę IP',
  set_project_ip_status: 'Projekt IP',
  generate_invoice: 'Wystaw fakturę',
  submit_invoice_to_ksef: 'Wyślij do KSeF',
  finalize_settlement: 'Zatwierdź rozliczenie',
}

function toolLabel(name: string) {
  return TOOL_LABELS[name] ?? name
}

function MessageBubble({ message, onConfirm, onCancel, confirming, streaming }: {
  message: ChatMessage
  onConfirm?: (m: ChatMessage) => void
  onCancel?: (m: ChatMessage) => void
  confirming?: boolean
  streaming?: boolean
}) {
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
    <div className="mr-auto max-w-[80%] bg-gray-100 dark:bg-gray-700 rounded-2xl px-4 py-2">
      <div className="flex items-center gap-1 text-xs text-gray-500 dark:text-gray-400 mb-1">
        <Bot size={12} /> Asystent
      </div>
      <div className="whitespace-pre-wrap break-words text-gray-900 dark:text-gray-100">
        {message.content}
        {streaming && (
          <span className="inline-block w-1.5 h-4 ml-0.5 align-middle bg-gray-400 dark:bg-gray-500 animate-pulse" />
        )}
        {!message.content && !streaming && (
          <span className="italic text-gray-500 dark:text-gray-400">(brak odpowiedzi)</span>
        )}
      </div>

      {hasTools && (
        <div className="mt-2 pt-2 border-t border-gray-200 dark:border-gray-600">
          <div className="flex flex-wrap items-center gap-1.5">
            <span className="text-[11px] text-gray-500 dark:text-gray-400 flex items-center gap-1">
              <Wrench size={11} /> Użyto:
            </span>
            {message.toolCalls!.map((tc, i) => (
              <span
                key={i}
                className="inline-flex items-center gap-1 bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 border border-blue-200 dark:border-blue-800 rounded-full px-2 py-0.5 text-[11px] font-medium"
              >
                {toolLabel(tc.name)}
              </span>
            ))}
          </div>

          <details className="mt-2 text-xs">
            <summary className="cursor-pointer text-gray-500 dark:text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 select-none">
              Szczegóły wywołań
            </summary>
            <div className="mt-2 space-y-2">
              {message.toolCalls!.map((tc, i) => (
                <div key={i} className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 rounded-lg p-2 font-mono">
                  <div className="font-semibold text-blue-700 dark:text-blue-400">{tc.name}</div>
                  <div className="text-gray-500 dark:text-gray-400 mt-1">args:</div>
                  <pre className="text-[10px] overflow-x-auto text-gray-700 dark:text-gray-300">{JSON.stringify(tc.args, null, 2)}</pre>
                  <div className="text-gray-500 dark:text-gray-400 mt-1">result:</div>
                  <pre className="text-[10px] overflow-x-auto max-h-40 text-gray-700 dark:text-gray-300">{JSON.stringify(tc.result, null, 2)}</pre>
                </div>
              ))}
            </div>
          </details>
        </div>
      )}

      {message.pendingAction && (
        <div className="mt-2 pt-2 border-t border-amber-200 dark:border-amber-800">
          <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-3">
            <div className="flex items-center gap-1.5 text-amber-800 dark:text-amber-400 text-xs font-semibold mb-1">
              <AlertTriangle size={13} /> Wymaga potwierdzenia
            </div>
            <div className="text-sm text-gray-800 dark:text-gray-200">{message.pendingAction.actionDescription}</div>
            <div className="text-[11px] text-gray-500 dark:text-gray-400 mt-1">
              Narzędzie: <span className="font-mono">{toolLabel(message.pendingAction.tool)}</span>
            </div>
            <details className="mt-1 text-xs">
              <summary className="cursor-pointer text-gray-500 dark:text-gray-400 select-none">Parametry</summary>
              <pre className="text-[10px] overflow-x-auto mt-1 text-gray-700 dark:text-gray-300">{JSON.stringify(message.pendingAction.args, null, 2)}</pre>
            </details>
            <div className="flex gap-2 mt-2">
              <button
                onClick={() => onConfirm?.(message)}
                disabled={confirming}
                className="bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50 px-3 py-1.5 rounded-lg text-xs font-medium"
              >
                {confirming ? 'Wykonywanie…' : 'Wykonaj'}
              </button>
              <button
                onClick={() => onCancel?.(message)}
                disabled={confirming}
                className="bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50 px-3 py-1.5 rounded-lg text-xs font-medium"
              >
                Anuluj
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
