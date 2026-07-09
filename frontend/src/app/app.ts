import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ChatService } from './chat.service';

@Component({
  selector: 'app-root',
  imports: [FormsModule, DatePipe],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly chat = inject(ChatService);

  protected readonly title = signal('frontend');
  protected readonly messages = this.chat.messages;
  protected username = 'Guest' + Math.floor(Math.random() * 1000);
  protected draft = '';

  constructor() {
    this.chat.start().catch(err => console.error('SignalR connection failed:', err));
  }

  async send(): Promise<void> {
    if (!this.draft.trim()) {
      return;
    }
    await this.chat.sendMessage(this.username, this.draft);
    this.draft = '';
  }
}
