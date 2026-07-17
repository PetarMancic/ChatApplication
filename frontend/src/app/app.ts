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
  }

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
    await this.chat.sendMessage(channelId, this.draft);
    this.draft = '';
  }
}
