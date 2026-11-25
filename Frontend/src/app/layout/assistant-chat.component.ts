import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { AssistantApi, AssistantMessage } from '../core/api.services';

/**
 * Asistente con IA disponible en todas las pantallas. Las sugerencias y las
 * respuestas dependen del rol: el servidor solo entrega al modelo los datos
 * que el usuario autenticado puede ver.
 */
@Component({
  selector: 'app-assistant-chat',
  standalone: false,
  templateUrl: './assistant-chat.component.html',
  styleUrl: './assistant-chat.component.scss',
})
export class AssistantChatComponent implements OnInit {
  @ViewChild('scroll') private scroll?: ElementRef<HTMLElement>;

  open = false;
  enabled = false;
  thinking = false;
  draft = '';
  suggestions: string[] = [];
  messages: AssistantMessage[] = [];

  constructor(private api: AssistantApi) {}

  ngOnInit(): void {
    this.api.info().subscribe((info) => {
      this.enabled = info.enabled;
      this.suggestions = info.suggestions;
    });
  }

  toggle(): void {
    this.open = !this.open;
    if (this.open) this.scrollToEnd();
  }

  send(text = this.draft): void {
    const message = text.trim();
    if (!message || this.thinking || !this.enabled) return;

    const history = [...this.messages];
    this.messages.push({ role: 'user', text: message });
    this.draft = '';
    this.thinking = true;
    this.scrollToEnd();

    this.api.chat(message, history).subscribe({
      next: (response) => {
        this.messages.push({ role: 'assistant', text: response.reply });
        this.thinking = false;
        this.scrollToEnd();
      },
      error: () => {
        // El interceptor ya muestra el error; se retira la pregunta para poder reintentar.
        this.messages.pop();
        this.draft = message;
        this.thinking = false;
      },
    });
  }

  clear(): void {
    this.messages = [];
  }

  private scrollToEnd(): void {
    setTimeout(() => this.scroll?.nativeElement.scrollTo({ top: this.scroll.nativeElement.scrollHeight }));
  }
}
