import { Injectable, NgZone } from '@angular/core';
import { MessageDto } from '../models/laber.models';
import { SettingsService } from './settings.service';

@Injectable({ providedIn: 'root' })
export class LaberSseService {
  constructor(
    private readonly settings: SettingsService,
    private readonly zone: NgZone
  ) {}

  connectMessages(
    onMessage: (message: MessageDto) => void,
    channel?: string
  ): () => void {
    let params = '';
    if (channel) {
      const name = channel.replace(/^#/, '');
      params = `?channel=${encodeURIComponent(name)}`;
    }

    const source = new EventSource(this.url(`/api/messages/stream${params}`));

    source.onmessage = (event) => {
      const message = JSON.parse(event.data) as MessageDto;
      this.zone.run(() => onMessage(message));
    };

    return () => source.close();
  }

  private url(path: string): string {
    const base = this.settings.getApiBaseUrl();
    if (!base) {
      return path;
    }

    return `${base}${path}`;
  }
}
