import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ChannelDto,
  ConnectionStatusDto,
  MessageDto,
} from '../models/laber.models';
import { SettingsService } from './settings.service';

@Injectable({ providedIn: 'root' })
export class LaberApiService {
  constructor(
    private readonly http: HttpClient,
    private readonly settings: SettingsService
  ) {}

  getStatus(): Observable<ConnectionStatusDto> {
    return this.http.get<ConnectionStatusDto>(this.url('/api/status'));
  }

  getChannels(sinceId?: number): Observable<ChannelDto[]> {
    let params = new HttpParams();
    if (sinceId && sinceId > 0) {
      params = params.set('sinceId', String(sinceId));
    }

    return this.http.get<ChannelDto[]>(this.url('/api/channels'), { params });
  }

  getMessages(channel: string, afterId?: number, limit = 100): Observable<MessageDto[]> {
    let params = new HttpParams().set('limit', String(limit));
    if (afterId && afterId > 0) {
      params = params.set('afterId', String(afterId));
    }

    const encoded = encodeURIComponent(channel.replace(/^#/, ''));
    return this.http.get<MessageDto[]>(this.url(`/api/messages/${encoded}`), { params });
  }

  getAllMessages(afterId?: number, limit = 100): Observable<MessageDto[]> {
    let params = new HttpParams().set('limit', String(limit));
    if (afterId && afterId > 0) {
      params = params.set('afterId', String(afterId));
    }

    return this.http.get<MessageDto[]>(this.url('/api/messages'), { params });
  }

  private url(path: string): string {
    const base = this.settings.getApiBaseUrl();
    if (!base) {
      return path;
    }

    return `${base}${path}`;
  }
}
