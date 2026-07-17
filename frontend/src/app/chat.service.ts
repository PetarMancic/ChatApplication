import { Injectable, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { AuthService } from './auth.service';

export type MessageStatus = 'sending' | 'sent' | 'failed';

export interface ChatMessage {
  id: string | null;
  channelId: string;
  user: string;
  senderEmail: string | null;
  clientMessageId: string | null;
  message: string;
  timestamp: Date;
  status: MessageStatus;
}

interface ServerMessage {
  id: string;
  channelId: string;
  user: string;
  senderEmail: string | null;
  clientMessageId: string | null;
  message: string;
  timestamp: string;
}

interface ServerReadState {
  userId: string;
  channelId: string;
  lastReadMessageId: string;
}

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly auth = inject(AuthService);
  private hubConnection: HubConnection;
  private activeChannelId: string | null = null;

  readonly messages = signal<ChatMessage[]>([]);
  readonly onlineUsers = signal<ReadonlySet<string>>(new Set());
  readonly typingUsers = signal<ReadonlyMap<string, string>>(new Map());
  readonly readStates = signal<ReadonlyMap<string, string>>(new Map());

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

    this.hubConnection.on('ReceiveMessage', (m: ServerMessage) => {
      if (m.channelId !== this.activeChannelId) {
        return;
      }
      this.messages.update(current => this.mergeServerMessage(current, m));
    });

    this.hubConnection.on('ReceiveHistory', (channelId: string, history: ServerMessage[]) => {
      if (channelId !== this.activeChannelId) {
        return;
      }
      this.messages.set(history.map(h => this.fromServer(h)));
    });

    this.hubConnection.on('ReceiveReadStates', (channelId: string, states: ServerReadState[]) => {
      if (channelId !== this.activeChannelId) {
        return;
      }
      this.readStates.set(new Map(states.map(s => [s.userId, s.lastReadMessageId])));
    });

    this.hubConnection.on('ReadStateChanged', (channelId: string, userId: string, messageId: string) => {
      if (channelId !== this.activeChannelId) {
        return;
      }
      this.readStates.update(current => new Map(current).set(userId, messageId));
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

    // Gap recovery: re-enter the group, then append only what we missed — never wipe the list
    this.hubConnection.onreconnected(async () => {
      const channelId = this.activeChannelId;
      if (!channelId) {
        return;
      }
      const lastId = this.lastKnownMessageId();
      try {
        const gap: ServerMessage[] = await this.hubConnection.invoke('RejoinChannel', channelId, lastId);
        if (channelId !== this.activeChannelId) {
          return;
        }
        this.messages.update(current =>
          gap.reduce((acc, m) => this.mergeServerMessage(acc, m), current));
      } catch (err) {
        console.error('Rejoin after reconnect failed:', err);
      }
    });
  }

  private fromServer(m: ServerMessage): ChatMessage {
    return { ...m, timestamp: new Date(m.timestamp), status: 'sent' };
  }

  /** Reconcile an optimistic bubble by clientMessageId, drop known ids, else append. */
  private mergeServerMessage(current: ChatMessage[], m: ServerMessage): ChatMessage[] {
    if (m.clientMessageId && current.some(x => x.clientMessageId === m.clientMessageId)) {
      return current.map(x => x.clientMessageId === m.clientMessageId
        ? { ...x, id: m.id, timestamp: new Date(m.timestamp), status: 'sent' as const }
        : x);
    }
    if (current.some(x => x.id === m.id)) {
      return current;
    }
    return [...current, this.fromServer(m)];
  }

  private lastKnownMessageId(): string | null {
    const withId = this.messages().filter(m => m.id !== null);
    return withId.length ? withId[withId.length - 1].id : null;
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
    this.readStates.set(new Map());
    await this.hubConnection.stop();
  }

  async joinChannel(channelId: string): Promise<void> {
    this.activeChannelId = channelId;
    this.messages.set([]);
    this.readStates.set(new Map());
    this.clearTypingState();
    await this.hubConnection.invoke('JoinChannel', channelId);
  }

  async leaveChannel(channelId: string): Promise<void> {
    if (this.activeChannelId === channelId) {
      this.activeChannelId = null;
    }
    await this.hubConnection.invoke('LeaveChannel', channelId);
  }

  /** Optimistic send: the bubble renders immediately; the hub invoke's return value is the ACK. */
  async sendMessage(channelId: string, message: string): Promise<void> {
    const clientMessageId = crypto.randomUUID();
    const optimistic: ChatMessage = {
      id: null,
      channelId,
      user: this.auth.userName() ?? '',
      senderEmail: this.auth.userEmail(),
      clientMessageId,
      message,
      timestamp: new Date(),
      status: 'sending',
    };
    this.messages.update(current => [...current, optimistic]);
    await this.deliver(channelId, message, clientMessageId);
  }

  /** Safe because the server dedups on (channelId, clientMessageId). */
  async retryMessage(msg: ChatMessage): Promise<void> {
    if (!msg.clientMessageId || msg.status !== 'failed') {
      return;
    }
    this.setStatus(msg.clientMessageId, 'sending');
    await this.deliver(msg.channelId, msg.message, msg.clientMessageId);
  }

  private async deliver(channelId: string, message: string, clientMessageId: string): Promise<void> {
    try {
      const saved: ServerMessage = await this.hubConnection.invoke('SendMessage', channelId, message, clientMessageId);
      this.messages.update(current => current.map(m => m.clientMessageId === clientMessageId
        ? { ...m, id: saved.id, timestamp: new Date(saved.timestamp), status: 'sent' as const }
        : m));
    } catch {
      this.setStatus(clientMessageId, 'failed');
    }
  }

  private setStatus(clientMessageId: string, status: MessageStatus): void {
    this.messages.update(current => current.map(m =>
      m.clientMessageId === clientMessageId ? { ...m, status } : m));
  }

  markRead(channelId: string, messageId: string): void {
    this.hubConnection.invoke('MarkRead', channelId, messageId).catch(() => {});
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
