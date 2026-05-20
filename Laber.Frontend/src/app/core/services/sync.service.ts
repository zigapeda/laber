import { Injectable } from '@angular/core';
import { BehaviorSubject, interval, switchMap, tap } from 'rxjs';
import { MessageDto } from '../models/laber.models';
import { LaberApiService } from './laber-api.service';
import { SettingsService } from './settings.service';

@Injectable({ providedIn: 'root' })
export class SyncService {
  private readonly messagesSubject = new BehaviorSubject<MessageDto[]>([]);
  readonly messages$ = this.messagesSubject.asObservable();

  constructor(
    private readonly api: LaberApiService,
    private readonly settings: SettingsService
  ) {}

  startPolling(intervalMs = 15000): void {
    interval(intervalMs)
      .pipe(
        switchMap(() => this.api.getAllMessages(this.settings.getLastMessageId())),
        tap((messages) => this.mergeMessages(messages))
      )
      .subscribe();
  }

  refresh(): void {
    this.api.getAllMessages(this.settings.getLastMessageId()).subscribe((messages) => {
      this.mergeMessages(messages);
    });
  }

  private mergeMessages(incoming: MessageDto[]): void {
    if (!incoming.length) {
      return;
    }

    const current = [...this.messagesSubject.value];
    const known = new Set(current.map((m) => m.id));
    for (const message of incoming) {
      if (!known.has(message.id)) {
        current.push(message);
      }
    }

    current.sort((a, b) => a.id - b.id);
    this.messagesSubject.next(current);

    const maxId = current[current.length - 1]?.id ?? 0;
    if (maxId > 0) {
      this.settings.setLastMessageId(maxId);
    }
  }
}
