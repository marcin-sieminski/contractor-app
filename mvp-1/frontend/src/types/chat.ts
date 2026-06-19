export type ChatRole = 'user' | 'assistant'
export type AiProvider = 'ollama' | 'claude'

export interface ToolCallSummary {
  name: string
  args: unknown
  result: unknown
}

export interface PendingAction {
  tool: string
  actionDescription: string
  args: unknown
}

export interface ChatMessage {
  id: string
  role: ChatRole
  content: string
  toolCalls?: ToolCallSummary[]
  pendingAction?: PendingAction
}

export interface ChatRequest {
  messages: { role: ChatRole; content: string }[]
  model?: string
  provider?: AiProvider
  conversationId?: string | null
}

export interface ChatResponse {
  content: string
  toolCalls: ToolCallSummary[]
  model: string
  toolsSupported: boolean
  pendingAction?: PendingAction | null
  conversationId?: string | null
}

export interface ConfirmActionRequest {
  messages: { role: ChatRole; content: string }[]
  action: { tool: string; args: unknown }
  model?: string
  provider?: AiProvider
  conversationId?: string | null
}

export interface ConversationSummary {
  id: string
  title: string
  createdAt: string
  updatedAt: string
  messageCount: number
}

export interface ConversationMessageItem {
  role: ChatRole
  content: string
  toolCalls?: ToolCallSummary[] | null
  createdAt: string
}

export interface ConversationDetail {
  id: string
  title: string
  createdAt: string
  updatedAt: string
  messages: ConversationMessageItem[]
}

export interface AiModel {
  name: string
  size: number
  provider: AiProvider
}

export type OllamaModel = AiModel
