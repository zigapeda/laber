import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly apiKey = 'laber-api-base';
  private readonly lastMessageIdKey = 'laber-last-message-id';

  getApiBaseUrl(): string {
    return localStorage.getItem(this.apiKey) ?? environment.apiBaseUrl;
  }

  setApiBaseUrl(url: string): void {
    localStorage.setItem(this.apiKey, url.replace(/\/$/, ''));
  }

  getLastMessageId(): number {
    return Number(localStorage.getItem(this.lastMessageIdKey) ?? '0');
  }

  setLastMessageId(id: number): void {
    localStorage.setItem(this.lastMessageIdKey, String(id));
  }
}
