import type { HubConnection } from '@microsoft/signalr';

interface MatchingHubState {
  connection: HubConnection | null;
  isConnected: boolean;
}

export function useMatchingHub(): MatchingHubState {
  return {
    connection: null,
    isConnected: false,
  };
}
