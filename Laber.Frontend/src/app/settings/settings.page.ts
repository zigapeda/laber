import { Component, OnInit } from '@angular/core';
import { ConnectionStatusDto, ThemeMode } from '../core/models/laber.models';
import { LaberApiService } from '../core/services/laber-api.service';
import { SettingsService } from '../core/services/settings.service';
import { ThemeService } from '../core/services/theme.service';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.page.html',
  styleUrls: ['./settings.page.scss'],
  standalone: false,
})
export class SettingsPage implements OnInit {
  apiBaseUrl = '';
  themeMode: ThemeMode = 'system';
  status: ConnectionStatusDto | null = null;

  constructor(
    private readonly settings: SettingsService,
    private readonly theme: ThemeService,
    private readonly api: LaberApiService
  ) {}

  ngOnInit(): void {
    this.apiBaseUrl = this.settings.getApiBaseUrl();
    this.themeMode = this.theme.getMode();
    this.loadStatus();
  }

  saveApiUrl(): void {
    this.settings.setApiBaseUrl(this.apiBaseUrl);
    this.loadStatus();
  }

  onThemeChange(event: CustomEvent): void {
    this.themeMode = event.detail.value as ThemeMode;
    this.theme.setMode(this.themeMode);
  }

  loadStatus(): void {
    this.api.getStatus().subscribe({
      next: (status) => (this.status = status),
      error: () => (this.status = null),
    });
  }
}
