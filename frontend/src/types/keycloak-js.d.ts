declare module 'keycloak-js' {
  export interface KeycloakProfile {
    username?: string;
    email?: string;
    [key: string]: unknown;
  }

  export interface KeycloakTokenParsed {
    preferred_username?: string;
    email?: string;
    realm_access?: {
      roles?: string[];
    };
    [key: string]: unknown;
  }

  export interface KeycloakInitOptions {
    [key: string]: unknown;
  }

  export interface KeycloakLoginOptions {
    [key: string]: unknown;
  }

  export interface KeycloakLogoutOptions {
    [key: string]: unknown;
  }

  export default class Keycloak {
    token?: string;
    subject?: string;
    tokenParsed?: KeycloakTokenParsed;

    constructor(config?: Record<string, unknown>);

    init(options?: KeycloakInitOptions): Promise<boolean>;
    login(options?: KeycloakLoginOptions): Promise<void>;
    logout(options?: KeycloakLogoutOptions): Promise<void>;
    updateToken(minValiditySeconds?: number): Promise<boolean>;
    loadUserProfile(): Promise<KeycloakProfile>;
  }
}