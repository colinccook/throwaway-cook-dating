import type { HubConnection } from '@microsoft/signalr';

interface ConversationHubState {
  connection: HubConnection | null;
  isConnected: boolean;
}

export function useConversationHub(): ConversationHubState {
  return {
    connection: null,
    isConnected: false,
  };
}
