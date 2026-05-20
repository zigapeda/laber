import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ChannelDto, ConnectionStatusDto } from '../core/models/laber.models';
import { LaberApiService } from '../core/services/laber-api.service';
import { SettingsService } from '../core/services/settings.service';
import { SyncService } from '../core/services/sync.service';

@Component({
  selector: 'app-channels',
  templateUrl: './channels.page.html',
  styleUrls: ['./channels.page.scss'],
  standalone: false,
})
export class ChannelsPage implements OnInit {
  channels: ChannelDto[] = [];
  status: ConnectionStatusDto | null = null;
  loading = false;

  constructor(
    private readonly api: LaberApiService,
    private readonly settings: SettingsService,
    private readonly sync: SyncService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.sync.startPolling();
    this.refresh();
  }

  refresh(event?: { target: { complete: () => void } }): void {
    this.loading = true;
    const sinceId = this.settings.getLastMessageId();

    this.api.getStatus().subscribe({
      next: (status) => (this.status = status),
      error: () => (this.status = null),
    });

    this.api.getChannels(sinceId).subscribe({
      next: (channels) => {
        this.channels = channels;
        this.loading = false;
        event?.target.complete();
      },
      error: () => {
        this.loading = false;
        event?.target.complete();
      },
    });

    this.sync.refresh();
  }

  openChannel(channel: ChannelDto): void {
    const name = channel.name.replace(/^#/, '');
    void this.router.navigate(['/tabs/channels', name]);
  }
}
