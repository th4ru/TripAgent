import { AfterViewChecked, Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ChatMessage } from '../../core/models/chat';
import { AuthService } from '../../core/services/auth.service';
import { ChatService } from '../../core/services/chat.service';

const SESSION_KEY = 'trip-planner-chat-session';

@Component({
  selector: 'app-chatbot',
  imports: [FormsModule],
  templateUrl: './chatbot.html',
  styleUrl: './chatbot.scss'
})
export class Chatbot implements AfterViewChecked {
  private readonly chatService = inject(ChatService);
  private readonly auth = inject(AuthService);
  private readonly messagesContainer = viewChild<ElementRef<HTMLDivElement>>('messagesContainer');

  readonly suggestions = [
    'Plan a 5-day trip to Ella',
    'Weekend getaway to Kandy under 50,000 LKR',
    'Best time to visit Sigiriya'
  ];

  readonly messages = signal<ChatMessage[]>([
    {
      id: crypto.randomUUID(),
      role: 'assistant',
      text: 'Hi! I am your AI travel assistant. Tell me where you want to go and I will help you plan the trip.',
      createdAt: new Date()
    }
  ]);

  readonly sending = signal(false);
  readonly error = signal<string | null>(null);
  readonly userLabel = this.auth.displayName();

  draft = '';

  private readonly sessionId = this.loadSessionId();
  private shouldScroll = false;

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

    this.chatService.send(message, this.sessionId).subscribe({
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

  private append(message: Pick<ChatMessage, 'role' | 'text'>): void {
    this.messages.update((messages) => [
      ...messages,
      { id: crypto.randomUUID(), createdAt: new Date(), ...message }
    ]);
  }

  private loadSessionId(): string {
    const userId = this.auth.user()?.id ?? 'anonymous';
    const key = `${SESSION_KEY}:${userId}`;

    try {
      const existing = localStorage.getItem(key);
      if (existing) {
        return existing;
      }

      const created = crypto.randomUUID();
      localStorage.setItem(key, created);
      return created;
    } catch {
      return crypto.randomUUID();
    }
  }
}
