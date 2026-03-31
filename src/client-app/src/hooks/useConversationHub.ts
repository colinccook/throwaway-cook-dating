import { useState, useEffect, useRef, useCallback } from 'react';
import type { HubConnection } from '@microsoft/signalr';
import { createHubConnection } from '../services/signalr';

export interface ConversationSummary {
  conversationId: string;
  matchId: string;
  otherUserId: string;
  lastMessage?: string;
  lastMessageAt?: string;
}

export interface MessageDto {
  id: string;
  senderId: string;
  content: string;
  sentAt: string;
  isRead: boolean;
}

interface ConversationHubState {
  conversations: ConversationSummary[];
  messages: MessageDto[];
  isConnected: boolean;
  isLoading: boolean;
  joinConversation: (conversationId: string) => void;
  leaveConversation: (conversationId: string) => void;
  sendMessage: (conversationId: string, content: string) => void;
  markRead: (conversationId: string) => void;
  refreshConversations: () => void;
}

export function useConversationHub(): ConversationHubState {
  const [conversations, setConversations] = useState<ConversationSummary[]>([]);
  const [messages, setMessages] = useState<MessageDto[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const connectionRef = useRef<HubConnection | null>(null);
  const pendingJoinRef = useRef<string | null>(null);

  useEffect(() => {
    const hubUrl = `${window.location.protocol}//${window.location.host}/hubs/conversation`;
    const connection = createHubConnection(hubUrl);
    connectionRef.current = connection;

    connection.on('ReceiveConversations', (incoming: ConversationSummary[]) => {
      setConversations(incoming);
      setIsLoading(false);
    });

    connection.on('ReceiveMessages', (incoming: MessageDto[]) => {
      setMessages(incoming);
    });

    connection.on('ReceiveMessage', (message: MessageDto) => {
      setMessages((prev) => [...prev, message]);
    });

    connection.onclose(() => setIsConnected(false));
    connection.onreconnected(() => setIsConnected(true));

    connection
      .start()
      .then(() => {
        setIsConnected(true);
        // Process any pending join request
        if (pendingJoinRef.current) {
          connection.invoke('JoinConversation', pendingJoinRef.current);
          pendingJoinRef.current = null;
        }
        return connection.invoke('GetConversations');
      })
      .catch((err) => {
        console.error('ConversationHub connection error:', err);
        setIsLoading(false);
      });

    return () => {
      connection.stop();
    };
  }, []);

  const joinConversation = useCallback((conversationId: string) => {
    const conn = connectionRef.current;
    if (conn?.state === 'Connected') {
      conn.invoke('JoinConversation', conversationId);
    } else {
      pendingJoinRef.current = conversationId;
    }
  }, []);

  const leaveConversation = useCallback((conversationId: string) => {
    connectionRef.current?.invoke('LeaveConversation', conversationId);
  }, []);

  const sendMessage = useCallback((conversationId: string, content: string) => {
    connectionRef.current?.invoke('SendMessage', conversationId, content);
  }, []);

  const markRead = useCallback((conversationId: string) => {
    connectionRef.current?.invoke('MarkRead', conversationId);
  }, []);

  const refreshConversations = useCallback(() => {
    setIsLoading(true);
    connectionRef.current?.invoke('GetConversations');
  }, []);

  return {
    conversations,
    messages,
    isConnected,
    isLoading,
    joinConversation,
    leaveConversation,
    sendMessage,
    markRead,
    refreshConversations,
  };
}
