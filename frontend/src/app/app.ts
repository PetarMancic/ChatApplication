import { Component, AfterViewInit, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ChatService } from './chat.service';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-root',
  imports: [FormsModule, DatePipe],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements AfterViewInit {
  private readonly chat = inject(ChatService);
  protected readonly auth = inject(AuthService);

  protected readonly title = signal('frontend');
  protected readonly messages = this.chat.messages;
  protected draft = '';

  constructor() {
    effect(() => {
      if (this.auth.isAuthenticated()) {
        this.chat.start().catch(err => console.error('SignalR connection failed:', err));
      }
    });
  }

  ngAfterViewInit(): void {
    if (!this.auth.isAuthenticated()) {
      this.auth.initGoogleButton('google-signin-button');
    }
  }

  async send(): Promise<void> {
    if (!this.draft.trim()) {
      return;
    }
    await this.chat.sendMessage(this.draft);
    this.draft = '';
  }
}
