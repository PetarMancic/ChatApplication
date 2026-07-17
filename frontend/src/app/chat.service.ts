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
  readonly onlineUsers = signal<ReadonlySet<string>>(new Set());
  readonly typingUsers = signal<ReadonlyMap<string, string>>(new Map());

  private readonly typingTimeouts = new Map<string, ReturnType<typeof setTimeout>>();
  private lastTypingSentAt = 0;

  private static readonly TYPING_SEND_INTERVAL_MS = 2000;
  private static readonly TYPING_CLEAR_AFTER_MS = 3000;

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

    this.hubConnection.on('OnlineUsers', (userIds: string[]) => {
      this.onlineUsers.set(new Set(userIds));
    });

    this.hubConnection.on('UserOnline', (userId: string) => {
      this.onlineUsers.update(current => new Set(current).add(userId));
    });

    this.hubConnection.on('UserOffline', (userId: string) => {
      this.onlineUsers.update(current => {
        const next = new Set(current);
        next.delete(userId);
        return next;
      });
    });

    this.hubConnection.on('UserTyping', (channelId: string, userId: string, userName: string) => {
      if (channelId !== this.activeChannelId) {
        return;
      }
      this.typingUsers.update(current => new Map(current).set(userId, userName));

      // Receiver-side timeout: clear the indicator if no ping arrives in time
      clearTimeout(this.typingTimeouts.get(userId));
      this.typingTimeouts.set(userId, setTimeout(() => {
        this.typingTimeouts.delete(userId);
        this.typingUsers.update(current => {
          const next = new Map(current);
          next.delete(userId);
          return next;
        });
      }, ChatService.TYPING_CLEAR_AFTER_MS));
    });

    this.hubConnection.onreconnected(() => {
      if (this.activeChannelId) {
        this.hubConnection.invoke('JoinChannel', this.activeChannelId);
      }
    });
  }

  private clearTypingState(): void {
    for (const timeout of this.typingTimeouts.values()) {
      clearTimeout(timeout);
    }
    this.typingTimeouts.clear();
    this.typingUsers.set(new Map());
  }

  async start(): Promise<void> {
    await this.hubConnection.start();
  }

  async disconnect(): Promise<void> {
    this.activeChannelId = null;
    this.messages.set([]);
    this.clearTypingState();
    this.onlineUsers.set(new Set());
    await this.hubConnection.stop();
  }

  async joinChannel(channelId: string): Promise<void> {
    this.activeChannelId = channelId;
    this.messages.set([]);
    this.clearTypingState();
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

  /** Throttled: at most one Typing ping per interval while keys are being pressed. */
  sendTyping(channelId: string): void {
    const now = Date.now();
    if (now - this.lastTypingSentAt < ChatService.TYPING_SEND_INTERVAL_MS) {
      return;
    }
    this.lastTypingSentAt = now;
    this.hubConnection.invoke('Typing', channelId).catch(() => {});
  }
}
