import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface ChannelSummary {
  id: string;
  name: string;
  type: 'public' | 'private' | 'dm';
  displayName: string;
}

export interface UserSummary {
  id: string;
  name: string;
  email: string;
}

const API_BASE = 'https://localhost:7129';

@Injectable({ providedIn: 'root' })
export class ChannelsService {
  private readonly http = inject(HttpClient);

  readonly myChannels = signal<ChannelSummary[]>([]);
  readonly publicChannels = signal<ChannelSummary[]>([]);
  readonly selectedChannelId = signal<string | null>(null);

  reset(): void {
    this.myChannels.set([]);
    this.publicChannels.set([]);
    this.selectedChannelId.set(null);
  }

  async loadMyChannels(): Promise<void> {
    const channels = await firstValueFrom(
      this.http.get<ChannelSummary[]>(`${API_BASE}/channels`)
    );
    this.myChannels.set(channels);
  }

  async loadPublicChannels(): Promise<void> {
    const channels = await firstValueFrom(
      this.http.get<ChannelSummary[]>(`${API_BASE}/channels/public`)
    );
    this.publicChannels.set(channels);
  }

  async createChannel(name: string, isPrivate: boolean): Promise<ChannelSummary> {
    return firstValueFrom(
      this.http.post<ChannelSummary>(`${API_BASE}/channels`, { name, isPrivate })
    );
  }

  async joinPublic(channelId: string): Promise<ChannelSummary> {
    return firstValueFrom(
      this.http.post<ChannelSummary>(`${API_BASE}/channels/${channelId}/join`, {})
    );
  }

  async addMember(channelId: string, userId: string): Promise<void> {
    await firstValueFrom(
      this.http.post(`${API_BASE}/channels/${channelId}/members`, { userId })
    );
  }

  async startDm(otherUserId: string): Promise<ChannelSummary> {
    return firstValueFrom(
      this.http.post<ChannelSummary>(`${API_BASE}/channels/dm`, { otherUserId })
    );
  }

  async searchUsers(q: string): Promise<UserSummary[]> {
    if (!q.trim()) {
      return [];
    }
    return firstValueFrom(
      this.http.get<UserSummary[]>(`${API_BASE}/users/search`, { params: { q } })
    );
  }
}
