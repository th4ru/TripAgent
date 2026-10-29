export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  text: string;
  createdAt: Date;
}

export interface ChatRequest {
  message: string;
  sessionId: string;
}

export interface ChatResponse {
  reply: string;
  sessionId?: string;
}
