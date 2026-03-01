import { inject } from '@angular/core';
import { ConfigService } from './config.service';
import { AuthService } from './auth.service';

export function appInitializerFactory() {
  const cfg = inject(ConfigService);
  const auth = inject(AuthService);

  return async () => {
    await cfg.load();
    await auth.init();
  };
}
