import { HubConnectionBuilder } from '@microsoft/signalr';
import type { HubConnection } from '@microsoft/signalr';

export function createHubConnection(hubUrl: string): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(hubUrl, {
      accessTokenFactory: () => localStorage.getItem('auth_token') ?? '',
    })
    .withAutomaticReconnect()
    .build();
}
