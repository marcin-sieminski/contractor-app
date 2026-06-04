export type ChatRole = 'user' | 'assistant'
export type AiProvider = 'ollama' | 'claude'

export interface ToolCallSummary {
  name: string
  args: unknown
  result: unknown
}

export interface ChatMessage {
  id: string
  role: ChatRole
  content: string
  toolCalls?: ToolCallSummary[]
}

export interface ChatRequest {
  messages: { role: ChatRole; content: string }[]
  model?: string
  provider?: AiProvider
}

export interface ChatResponse {
  content: string
  toolCalls: ToolCallSummary[]
  model: string
  toolsSupported: boolean
}

export interface AiModel {
  name: string
  size: number
  provider: AiProvider
}

export type OllamaModel = AiModel
