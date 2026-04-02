type KeycloakInitOptions = Record<string, unknown>;
type KeycloakLoginOptions = Record<string, unknown>;
type KeycloakLogoutOptions = Record<string, unknown>;
type KeycloakProfileLike = {
  username?: string;
  email?: string;
};

export default class KeycloakMock {
  token?: string = 'fake-token';
  subject?: string = 'test-sub';
  tokenParsed?: Record<string, unknown> = {
    preferred_username: 'patient1',
    email: 'patient1@example.local',
    realm_access: { roles: ['patient'] }
  };

  constructor(_config?: Record<string, unknown>) {}

  async init(_options?: KeycloakInitOptions): Promise<boolean> {
    return true;
  }

  async login(_options?: KeycloakLoginOptions): Promise<void> {
    return;
  }

  async logout(_options?: KeycloakLogoutOptions): Promise<void> {
    return;
  }

  async updateToken(_minValiditySeconds?: number): Promise<boolean> {
    return true;
  }

  async loadUserProfile(): Promise<KeycloakProfileLike> {
    return {
      username: 'patient1',
      email: 'patient1@example.local'
    };
  }
}