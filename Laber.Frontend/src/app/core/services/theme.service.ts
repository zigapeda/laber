import { Injectable } from '@angular/core';
import { ThemeMode } from '../models/laber.models';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly storageKey = 'laber-theme';

  getMode(): ThemeMode {
    const stored = localStorage.getItem(this.storageKey) as ThemeMode | null;
    return stored ?? 'system';
  }

  setMode(mode: ThemeMode): void {
    localStorage.setItem(this.storageKey, mode);
    this.apply(mode);
  }

  apply(mode: ThemeMode = this.getMode()): void {
    const body = document.body;
    body.classList.remove('dark', 'theme-light', 'theme-dark');

    if (mode === 'dark') {
      body.classList.add('dark', 'theme-dark');
      return;
    }

    if (mode === 'light') {
      body.classList.add('theme-light');
      return;
    }

    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    if (prefersDark) {
      body.classList.add('dark', 'theme-dark');
    }
  }

  watchSystemChanges(): void {
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
      if (this.getMode() === 'system') {
        this.apply('system');
      }
    });
  }
}
