export interface AppConfig {
  apiBaseUrl: string;
  keycloak: {
    url: string;
    realm: string;
    clientId: string;
  };
}
