import { Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export interface ChatMessage {
  user: string;
  message: string;
  timestamp: Date;
}

@Injectable({ providedIn: 'root' })
export class ChatService {
  private hubConnection: HubConnection;

  readonly messages = signal<ChatMessage[]>([]);

  constructor() {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl('https://localhost:7129/hubs/chat')
      .configureLogging(LogLevel.Information)
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveMessage', (user: string, message: string, timestamp: string) => {
      console.log('ovde je receive message user:', user, 'message:', message, new Date(timestamp));
      this.messages.update(current => [...current, { user, message, timestamp: new Date(timestamp) }]);
    });

    this.hubConnection.on('ReceiveHistory', (history: { user: string; message: string; timestamp: string }[]) => {
      this.messages.set(history.map(h => ({ ...h, timestamp: new Date(h.timestamp) })));
    });
  }

  async start(): Promise<void> {
    await this.hubConnection.start();
  }

  async sendMessage(user: string, message: string): Promise<void> {
    console.log("ovde je send message  message user:", user, 'message:', message);
    await this.hubConnection.invoke('SendMessage', user, message);
  }
}
