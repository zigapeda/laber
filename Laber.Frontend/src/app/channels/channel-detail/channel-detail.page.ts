import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription, interval, switchMap } from 'rxjs';
import { MessageDto } from '../../core/models/laber.models';
import { LaberApiService } from '../../core/services/laber-api.service';

@Component({
  selector: 'app-channel-detail',
  templateUrl: './channel-detail.page.html',
  styleUrls: ['./channel-detail.page.scss'],
  standalone: false,
})
export class ChannelDetailPage implements OnInit, OnDestroy {
  channel = '';
  messages: MessageDto[] = [];
  loadingOlder = false;
  hasMoreHistory = true;

  private pollSub?: Subscription;
  private readonly pageSize = 100;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly api: LaberApiService
  ) {}

  ngOnInit(): void {
    this.channel = this.route.snapshot.paramMap.get('channel') ?? '';
    this.loadLatest();
    this.pollSub = interval(15000)
      .pipe(switchMap(() => this.api.getMessages(this.channelName(), { afterId: this.newestId(), limit: this.pageSize })))
      .subscribe({
        next: (incoming) => this.mergeNewer(incoming),
      });
  }

  ngOnDestroy(): void {
    this.pollSub?.unsubscribe();
  }

  loadLatest(event?: { target: { complete: () => void } }): void {
    this.api.getMessages(this.channelName(), { limit: this.pageSize }).subscribe({
      next: (messages) => {
        this.messages = messages;
        this.hasMoreHistory = messages.length >= this.pageSize;
        event?.target.complete();
      },
      error: () => event?.target.complete(),
    });
  }

  loadOlder(event: { target: { complete: () => void } }): void {
    const oldest = this.messages[0]?.id;
    if (!oldest || this.loadingOlder || !this.hasMoreHistory) {
      event.target.complete();
      return;
    }

    this.loadingOlder = true;
    this.api
      .getMessages(this.channelName(), { beforeId: oldest, limit: 50 })
      .subscribe({
        next: (older) => {
          if (!older.length) {
            this.hasMoreHistory = false;
          } else {
            const known = new Set(this.messages.map((m) => m.id));
            const prepend = older.filter((m) => !known.has(m.id));
            this.messages = [...prepend, ...this.messages];
            if (older.length < 50) {
              this.hasMoreHistory = false;
            }
          }
          this.loadingOlder = false;
          event.target.complete();
        },
        error: () => {
          this.loadingOlder = false;
          event.target.complete();
        },
      });
  }

  private mergeNewer(incoming: MessageDto[]): void {
    if (!incoming.length) {
      return;
    }

    const known = new Set(this.messages.map((m) => m.id));
    const added = incoming.filter((m) => !known.has(m.id));
    if (!added.length) {
      return;
    }

    this.messages = [...this.messages, ...added].sort((a, b) => a.id - b.id);
  }

  private channelName(): string {
    return this.channel.startsWith('#') ? this.channel : `#${this.channel}`;
  }

  private newestId(): number {
    return this.messages[this.messages.length - 1]?.id ?? 0;
  }
}
