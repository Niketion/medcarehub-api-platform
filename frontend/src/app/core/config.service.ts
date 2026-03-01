import { Injectable, signal } from '@angular/core';
import { AppConfig } from './app-config';

@Injectable({ providedIn: 'root' })
export class ConfigService {
  private _config = signal<AppConfig | null>(null);
  config = this._config.asReadonly();

  async load(): Promise<void> {
    const res = await fetch('/assets/config.json', { cache: 'no-store' });
    if (!res.ok) throw new Error('Cannot load /assets/config.json');
    this._config.set(await res.json() as AppConfig);
  }

  get required(): AppConfig {
    const c = this._config();
    if (!c) throw new Error('Config not loaded');
    return c;
  }
}
