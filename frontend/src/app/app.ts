import { Component, AfterViewInit, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ChatMessage, ChatService } from './chat.service';
import { AuthService } from './auth.service';
import { ChannelsService, UserSummary } from './channels.service';

@Component({
  selector: 'app-root',
  imports: [FormsModule, DatePipe],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements AfterViewInit {
  private readonly chat = inject(ChatService);
  protected readonly auth = inject(AuthService);
  protected readonly channels = inject(ChannelsService);

  protected readonly messages = this.chat.messages;
  protected readonly connected = signal(false);
  protected readonly showBrowse = signal(false);
  protected readonly dmResults = signal<UserSummary[]>([]);

  protected draft = '';
  protected newChannelName = '';
  protected newChannelPrivate = false;
  protected dmQuery = '';

  protected isMine(m: ChatMessage): boolean {
    return !!m.senderEmail && m.senderEmail === this.auth.userEmail();
  }

  protected isOnline(userId: string | null | undefined): boolean {
    return !!userId && this.chat.onlineUsers().has(userId);
  }

  protected onDraftInput(): void {
    const channelId = this.channels.selectedChannelId();
    if (channelId && this.draft.trim()) {
      this.chat.sendTyping(channelId);
    }
  }

  protected readonly typingText = computed(() => {
    const names = [...this.chat.typingUsers().values()];
    switch (names.length) {
      case 0: return '';
      case 1: return `${names[0]} kuca…`;
      case 2: return `${names[0]} i ${names[1]} kucaju…`;
      default: return `${names.length} osoba kuca…`;
    }
  });

  protected readonly selectedChannel = computed(() =>
    this.channels.myChannels().find(c => c.id === this.channels.selectedChannelId()) ?? null
  );

  constructor() {
    effect(() => {
      if (this.auth.isAuthenticated() && !this.connected()) {
        this.chat.start()
          .then(async () => {
            this.connected.set(true);
            await this.channels.loadMyChannels();
          })
          .catch(err => console.error('SignalR connection failed:', err));
      }
    });

    // Session ended (token expired or 401) — tear down and show the login card again
    effect(() => {
      if (!this.auth.isAuthenticated() && this.connected()) {
        this.connected.set(false);
        this.showBrowse.set(false);
        this.dmResults.set([]);
        this.channels.reset();
        this.chat.disconnect().catch(() => {});
        // Login card renders after this effect; init the Google button once it's in the DOM
        setTimeout(() => this.auth.initGoogleButton('google-signin-button'));
      }
    });

    // Read pointer: advances only while the channel is open AND the tab is focused
    effect(() => {
      this.chat.messages();
      this.channels.selectedChannelId();
      this.tryMarkRead();
    });
    window.addEventListener('focus', () => this.tryMarkRead());
    document.addEventListener('visibilitychange', () => {
      if (!document.hidden) {
        this.tryMarkRead();
      }
    });
  }

  private readonly lastMarked = new Map<string, string>();

  private tryMarkRead(): void {
    const channelId = this.channels.selectedChannelId();
    if (!channelId || !document.hasFocus()) {
      return;
    }
    const withId = this.chat.messages().filter(m => m.id !== null);
    const lastId = withId.length ? withId[withId.length - 1].id! : null;
    if (!lastId) {
      return;
    }
    const previous = this.lastMarked.get(channelId);
    if (previous !== undefined && previous >= lastId) {
      return;
    }
    this.lastMarked.set(channelId, lastId);
    this.chat.markRead(channelId, lastId);
  }

  protected retry(m: ChatMessage): void {
    this.chat.retryMessage(m);
  }

  /** In a DM: the id of my newest message the other person has read — shows "Viđeno". */
  protected readonly lastSeenOwnMessageId = computed(() => {
    const sel = this.selectedChannel();
    if (!sel || sel.type !== 'dm' || !sel.otherUserId) {
      return null;
    }
    const pointer = this.chat.readStates().get(sel.otherUserId);
    if (!pointer) {
      return null;
    }
    const seenOwn = this.chat.messages()
      .filter(m => this.isMine(m) && m.id !== null && m.id <= pointer);
    return seenOwn.length ? seenOwn[seenOwn.length - 1].id : null;
  });

  ngAfterViewInit(): void {
    if (!this.auth.isAuthenticated()) {
      this.auth.initGoogleButton('google-signin-button');
    }
  }

  async selectChannel(id: string): Promise<void> {
    if (!this.connected() || this.channels.selectedChannelId() === id) {
      return;
    }
    const previous = this.channels.selectedChannelId();
    if (previous) {
      await this.chat.leaveChannel(previous);
    }
    this.channels.selectedChannelId.set(id);
    await this.chat.joinChannel(id);
  }

  async createChannel(): Promise<void> {
    console.log("kliknuo sam!")
    const name = this.newChannelName.trim();
    if (!name) {
      return;
    }
    const created = await this.channels.createChannel(name, this.newChannelPrivate);
    this.newChannelName = '';
    this.newChannelPrivate = false;
    await this.channels.loadMyChannels();
    await this.selectChannel(created.id);
  }

  async toggleBrowse(): Promise<void> {
    this.showBrowse.update(v => !v);
    if (this.showBrowse()) {
      await this.channels.loadPublicChannels();
    }
  }

  async joinPublic(id: string): Promise<void> {
    await this.channels.joinPublic(id);
    await this.channels.loadMyChannels();
    await this.channels.loadPublicChannels();
    await this.selectChannel(id);
  }

  async searchDm(): Promise<void> {
    this.dmResults.set(await this.channels.searchUsers(this.dmQuery));
  }

  async startDm(userId: string): Promise<void> {
    const dm = await this.channels.startDm(userId);
    this.dmQuery = '';
    this.dmResults.set([]);
    await this.channels.loadMyChannels();
    await this.selectChannel(dm.id);
  }

  async addMember(userId: string): Promise<void> {
    const selected = this.selectedChannel();
    if (!selected || selected.type !== 'private') {
      return;
    }
    await this.channels.addMember(selected.id, userId);
    this.dmQuery = '';
    this.dmResults.set([]);
  }

  async send(): Promise<void> {
    const channelId = this.channels.selectedChannelId();
    if (!channelId || !this.draft.trim()) {
      return;
    }
    const text = this.draft;
    this.draft = '';
    await this.chat.sendMessage(channelId, text);
  }
}
