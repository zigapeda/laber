import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MessageDto } from '../../core/models/laber.models';
import { LaberApiService } from '../../core/services/laber-api.service';

@Component({
  selector: 'app-channel-detail',
  templateUrl: './channel-detail.page.html',
  styleUrls: ['./channel-detail.page.scss'],
  standalone: false,
})
export class ChannelDetailPage implements OnInit {
  channel = '';
  messages: MessageDto[] = [];

  constructor(
    private readonly route: ActivatedRoute,
    private readonly api: LaberApiService
  ) {}

  ngOnInit(): void {
    this.channel = this.route.snapshot.paramMap.get('channel') ?? '';
    this.loadMessages();
  }

  loadMessages(event?: { target: { complete: () => void } }): void {
    const channelName = this.channel.startsWith('#') ? this.channel : `#${this.channel}`;
    this.api.getMessages(channelName).subscribe({
      next: (messages) => {
        this.messages = messages;
        event?.target.complete();
      },
      error: () => event?.target.complete(),
    });
  }
}
