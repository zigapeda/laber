import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { MessageDto } from '../models/laber.models';
import { LaberSseService } from './laber-sse.service';
import { SettingsService } from './settings.service';

@Injectable({ providedIn: 'root' })
export class SyncService implements OnDestroy {
  private readonly messagesSubject = new BehaviorSubject<MessageDto[]>([]);
  readonly messages$ = this.messagesSubject.asObservable();

  private disconnectSse?: () => void;

  constructor(
    private readonly sse: LaberSseService,
    private readonly settings: SettingsService
  ) {}

  connect(): void {
    if (this.disconnectSse) {
      return;
    }

    this.disconnectSse = this.sse.connectMessages((message) => this.mergeMessage(message));
  }

  ngOnDestroy(): void {
    this.disconnect();
  }

  disconnect(): void {
    this.disconnectSse?.();
    this.disconnectSse = undefined;
  }

  private mergeMessage(message: MessageDto): void {
    const current = [...this.messagesSubject.value];
    if (current.some((m) => m.id === message.id)) {
      return;
    }

    current.push(message);
    current.sort((a, b) => a.id - b.id);
    this.messagesSubject.next(current);

    if (message.id > this.settings.getLastMessageId()) {
      this.settings.setLastMessageId(message.id);
    }
  }
}
