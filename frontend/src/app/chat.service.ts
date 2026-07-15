import { Injectable, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { AuthService } from './auth.service';

export interface ChatMessage {
  user: string;
  message: string;
  timestamp: Date;
}

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly auth = inject(AuthService);
  private hubConnection: HubConnection;

  readonly messages = signal<ChatMessage[]>([]);

  constructor() {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl('https://localhost:7129/hubs/chat', {
        accessTokenFactory: () => this.auth.getToken() ?? ''
      })
      .configureLogging(LogLevel.Information)
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveMessage', (user: string, message: string, timestamp: string) => {
      this.messages.update(current => [...current, { user, message, timestamp: new Date(timestamp) }]);
    });

    this.hubConnection.on('ReceiveHistory', (history: { user: string; message: string; timestamp: string }[]) => {
      this.messages.set(history.map(h => ({ ...h, timestamp: new Date(h.timestamp) })));
    });
  }

  async start(): Promise<void> {
    await this.hubConnection.start();
  }

  async sendMessage(message: string): Promise<void> {
    await this.hubConnection.invoke('SendMessage', message);
  }
}
