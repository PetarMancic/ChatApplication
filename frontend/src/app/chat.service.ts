import { Injectable, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { AuthService } from './auth.service';

export interface ChatMessage {
  channelId: string;
  user: string;
  senderEmail: string | null;
  message: string;
  timestamp: Date;
}

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly auth = inject(AuthService);
  private hubConnection: HubConnection;
  private activeChannelId: string | null = null;

  readonly messages = signal<ChatMessage[]>([]);

  constructor() {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl('https://localhost:7129/hubs/chat', {
        accessTokenFactory: () => this.auth.getToken() ?? ''
      })
      .configureLogging(LogLevel.Information)
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveMessage', (channelId: string, user: string, senderEmail: string | null, message: string, timestamp: string) => {
      if (channelId !== this.activeChannelId) {
        return;
      }
      this.messages.update(current => [...current, { channelId, user, senderEmail, message, timestamp: new Date(timestamp) }]);
    });

    this.hubConnection.on('ReceiveHistory', (channelId: string, history: { channelId: string; user: string; senderEmail: string | null; message: string; timestamp: string }[]) => {
      if (channelId !== this.activeChannelId) {
        return;
      }
      this.messages.set(history.map(h => ({ ...h, timestamp: new Date(h.timestamp) })));
    });

    this.hubConnection.onreconnected(() => {
      if (this.activeChannelId) {
        this.hubConnection.invoke('JoinChannel', this.activeChannelId);
      }
    });
  }

  async start(): Promise<void> {
    await this.hubConnection.start();
  }

  async disconnect(): Promise<void> {
    this.activeChannelId = null;
    this.messages.set([]);
    await this.hubConnection.stop();
  }

  async joinChannel(channelId: string): Promise<void> {
    this.activeChannelId = channelId;
    this.messages.set([]);
    await this.hubConnection.invoke('JoinChannel', channelId);
  }

  async leaveChannel(channelId: string): Promise<void> {
    if (this.activeChannelId === channelId) {
      this.activeChannelId = null;
    }
    await this.hubConnection.invoke('LeaveChannel', channelId);
  }

  async sendMessage(channelId: string, message: string): Promise<void> {
    await this.hubConnection.invoke('SendMessage', channelId, message);
  }
}
