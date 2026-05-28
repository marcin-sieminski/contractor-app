export type ChatRole = 'user' | 'assistant'

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
}

export interface ChatResponse {
  content: string
  toolCalls: ToolCallSummary[]
  model: string
  toolsSupported: boolean
}

export interface OllamaModel {
  name: string
  size: number
}
