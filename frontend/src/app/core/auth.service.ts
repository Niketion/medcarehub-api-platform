import { Injectable, signal } from '@angular/core';
import Keycloak, { KeycloakProfile } from 'keycloak-js';
import { ConfigService } from './config.service';

type ProfileLite = {
  sub?: string;
  preferred_username?: string;
  email?: string;
};

@Injectable({ providedIn: 'root' })
export class AuthService {
  private kc?: Keycloak;

  private _ready = signal(false);
  private _authenticated = signal(false);
  private _profile = signal<ProfileLite | null>(null);
  private _roles = signal<string[]>([]);

  constructor(private cfg: ConfigService) {}

  async init(): Promise<void> {
    const c = this.cfg.required;

    this.kc = new (Keycloak as any)({
      url: c.keycloak.url,
      realm: c.keycloak.realm,
      clientId: c.keycloak.clientId
    });

    const authenticated = await this.kc!.init({
      onLoad: 'check-sso',
      pkceMethod: 'S256',
      checkLoginIframe: false,
    });

    this._authenticated.set(!!authenticated);

    if (authenticated) {
      await this.loadProfile();
      this.extractRoles();
    }

    this._ready.set(true);
  }

  isReady(): boolean { return this._ready(); }
  isAuthenticated(): boolean { return this._authenticated(); }
  profile(): ProfileLite | null { return this._profile(); }
  roles(): string[] { return this._roles(); }

  hasRole(role: string): boolean {
    return this._roles().some(r => r.toLowerCase() === role.toLowerCase());
  }

  login(): void {
    this.kc?.login({ redirectUri: window.location.origin + '/dashboard' });
  }

  logout(): void {
    this.kc?.logout({ redirectUri: window.location.origin + '/' });
  }

  async getToken(minValiditySeconds = 30): Promise<string | null> {
    if (!this.kc || !this._authenticated()) return null;

    try {
      await this.kc.updateToken(minValiditySeconds);
      return this.kc.token ?? null;
    } catch {
      this._authenticated.set(false);
      this._profile.set(null);
      this._roles.set([]);
      return null;
    }
  }

  private async loadProfile(): Promise<void> {
    try {
      const p: KeycloakProfile = await this.kc!.loadUserProfile();
      this._profile.set({
        sub: this.kc!.subject,
        preferred_username: (p as any).username ?? undefined,
        email: p.email ?? undefined
      });
    } catch {
      this._profile.set({
        sub: this.kc!.subject,
        preferred_username: this.kc!.tokenParsed?.['preferred_username'],
        email: this.kc!.tokenParsed?.['email']
      });
    }
  }

  private extractRoles(): void {
    const token = this.kc?.token;
    if (!token) { this._roles.set([]); return; }
    this._roles.set(extractRealmRolesFromJwt(token));
  }
}

function extractRealmRolesFromJwt(jwt: string): string[] {
  const parts = jwt.split('.');
  if (parts.length < 2) return [];
  try {
    const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));
    const roles = payload?.realm_access?.roles;
    return Array.isArray(roles) ? roles : [];
  } catch {
    return [];
  }
}
