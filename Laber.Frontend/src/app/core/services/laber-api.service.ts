import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ChannelDto,
  ConnectionEventDto,
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

  getMessages(
    channel: string,
    options: { afterId?: number; beforeId?: number; limit?: number } = {}
  ): Observable<MessageDto[]> {
    let params = new HttpParams().set('limit', String(options.limit ?? 100));
    if (options.afterId && options.afterId > 0) {
      params = params.set('afterId', String(options.afterId));
    }
    if (options.beforeId && options.beforeId > 0) {
      params = params.set('beforeId', String(options.beforeId));
    }

    const encoded = encodeURIComponent(channel.replace(/^#/, ''));
    return this.http.get<MessageDto[]>(this.url(`/api/messages/${encoded}`), { params });
  }

  getEvents(): Observable<ConnectionEventDto[]> {
    return this.http.get<ConnectionEventDto[]>(this.url('/api/events'));
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
