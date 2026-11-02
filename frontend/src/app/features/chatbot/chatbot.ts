import { DatePipe } from '@angular/common';
import {
  AfterViewChecked,
  Component,
  ElementRef,
  HostListener,
  inject,
  signal,
  viewChild
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ChatMessage, ConversationSummary } from '../../core/models/chat';
import { AuthService } from '../../core/services/auth.service';
import { ChatHistoryService } from '../../core/services/chat-history.service';
import { ChatService } from '../../core/services/chat.service';
import { AgenticBackdrop } from '../../shared/components/agentic-backdrop/agentic-backdrop';

const SESSION_KEY = 'trip-planner-chat-session';

@Component({
  selector: 'app-chatbot',
  imports: [FormsModule, DatePipe, AgenticBackdrop],
  templateUrl: './chatbot.html',
  styleUrl: './chatbot.scss'
})
export class Chatbot implements AfterViewChecked {
  private readonly chatService = inject(ChatService);
  private readonly history = inject(ChatHistoryService);
  private readonly auth = inject(AuthService);
  private readonly messagesContainer = viewChild<ElementRef<HTMLDivElement>>('messagesContainer');

  readonly suggestions = [
    'Plan a 5-day trip to Ella',
    'Weekend getaway to Kandy under 50,000 LKR',
    'Best time to visit Sigiriya'
  ];

  readonly messages = signal<ChatMessage[]>([createWelcomeMessage()]);
  readonly sending = signal(false);
  readonly error = signal<string | null>(null);
  readonly userLabel = this.auth.displayName();

  readonly sessionId = signal(this.loadSessionId());
  readonly historyOpen = signal(false);
  readonly historyItems = signal<ConversationSummary[]>([]);

  draft = '';

  private shouldScroll = false;

  constructor() {
    // Restore the conversation that was open when the page was last left.
    const restored = this.history.load(this.sessionId());
    if (restored.length > 0) {
      this.messages.set(restored);
      this.shouldScroll = true;
    }
  }

  ngAfterViewChecked(): void {
    if (!this.shouldScroll) {
      return;
    }

    this.shouldScroll = false;
    const container = this.messagesContainer()?.nativeElement;
    if (container) {
      container.scrollTop = container.scrollHeight;
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeHistory();
  }

  useSuggestion(text: string): void {
    this.draft = text;
    this.send();
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  newChat(): void {
    if (this.sending()) {
      return;
    }

    const sessionId = crypto.randomUUID();
    this.sessionId.set(sessionId);
    this.persistSessionId(sessionId);

    this.messages.set([createWelcomeMessage()]);
    this.error.set(null);
    this.draft = '';
    this.shouldScroll = true;
    this.closeHistory();
  }

  send(): void {
    const message = this.draft.trim();
    if (!message || this.sending()) {
      return;
    }

    this.error.set(null);
    this.draft = '';
    this.append({ role: 'user', text: message });
    this.sending.set(true);
    this.shouldScroll = true;

    this.chatService.send(message, this.sessionId()).subscribe({
      next: (response) => {
        this.append({
          role: 'assistant',
          text: response.reply?.trim() || 'I did not receive a response from the travel agent.'
        });
        this.sending.set(false);
        this.shouldScroll = true;
      },
      error: (err) => {
        this.sending.set(false);

        if (err?.status === 401) {
          this.error.set('Your session has expired. Please sign in again.');
          return;
        }

        this.draft = message;
        this.error.set(
          err?.error?.error ?? 'Could not reach the AI assistant. Please make sure the backend is running.'
        );
      }
    });
  }

  openHistory(): void {
    this.historyItems.set(this.history.list());
    this.historyOpen.set(true);
  }

  closeHistory(): void {
    this.historyOpen.set(false);
  }

  loadConversation(id: string): void {
    if (this.sending()) {
      return;
    }

    const messages = this.history.load(id);
    if (messages.length === 0) {
      return;
    }

    this.sessionId.set(id);
    this.persistSessionId(id);
    this.messages.set(messages);
    this.error.set(null);
    this.draft = '';
    this.shouldScroll = true;
    this.closeHistory();
  }

  deleteConversation(id: string, event: Event): void {
    event.stopPropagation();
    if (this.sending()) {
      return;
    }

    this.history.remove(id);
    this.historyItems.set(this.history.list());

    if (id === this.sessionId()) {
      this.newChat();
    }
  }

  clearHistory(): void {
    if (this.sending()) {
      return;
    }

    this.history.clear();
    this.historyItems.set([]);

    // Start a fresh thread so the cleared messages cannot be saved again.
    const sessionId = crypto.randomUUID();
    this.sessionId.set(sessionId);
    this.persistSessionId(sessionId);
    this.messages.set([createWelcomeMessage()]);
    this.error.set(null);
    this.draft = '';
    this.shouldScroll = true;
  }

  private append(message: Pick<ChatMessage, 'role' | 'text'>): void {
    this.messages.update((messages) => [
      ...messages,
      { id: crypto.randomUUID(), createdAt: new Date(), ...message }
    ]);
    this.history.save(this.sessionId(), this.messages());
  }

  private sessionStorageKey(): string {
    return `${SESSION_KEY}:${this.auth.user()?.id ?? 'anonymous'}`;
  }

  private loadSessionId(): string {
    try {
      const existing = localStorage.getItem(this.sessionStorageKey());
      if (existing) {
        return existing;
      }
    } catch {
      // Storage unavailable; fall through to an in-memory id.
    }

    const created = crypto.randomUUID();
    this.persistSessionId(created);
    return created;
  }

  private persistSessionId(sessionId: string): void {
    try {
      localStorage.setItem(this.sessionStorageKey(), sessionId);
    } catch {
      // Ignore storage failures.
    }
  }
}

function createWelcomeMessage(): ChatMessage {
  return {
    id: crypto.randomUUID(),
    role: 'assistant',
    text: 'Hi! I am your AI travel assistant. Tell me where you want to go and I will help you plan the trip.',
    createdAt: new Date()
  };
}
