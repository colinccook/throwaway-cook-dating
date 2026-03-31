import { useState, useEffect, useRef, useCallback } from 'react';
import type { HubConnection } from '@microsoft/signalr';
import { createHubConnection } from '../services/signalr';

export interface CandidateDto {
  userId: string;
  displayName: string;
  gender: string;
}

export interface MatchDto {
  matchId: string;
  otherUserId: string;
  otherDisplayName: string;
  matchedAt: string;
}

interface MatchingHubState {
  candidates: CandidateDto[];
  currentMatch: MatchDto | null;
  isConnected: boolean;
  isLoading: boolean;
  swipe: (targetUserId: string, direction: 'Left' | 'Right') => void;
  dismissMatch: () => void;
  refreshCandidates: () => void;
}

export function useMatchingHub(): MatchingHubState {
  const [candidates, setCandidates] = useState<CandidateDto[]>([]);
  const [currentMatch, setCurrentMatch] = useState<MatchDto | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const connectionRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    const hubUrl = `${window.location.protocol}//${window.location.host}/hubs/matching`;
    const connection = createHubConnection(hubUrl);
    connectionRef.current = connection;

    connection.on('ReceiveCandidates', (incoming: CandidateDto[]) => {
      setCandidates(incoming);
      setIsLoading(false);
    });

    connection.on('MatchFound', (match: MatchDto) => {
      setCurrentMatch(match);
    });

    connection.onclose(() => setIsConnected(false));
    connection.onreconnected(() => setIsConnected(true));

    connection
      .start()
      .then(() => {
        setIsConnected(true);
        return connection.invoke('GetCandidates');
      })
      .catch((err) => {
        console.error('MatchingHub connection error:', err);
        setIsLoading(false);
      });

    return () => {
      connection.stop();
    };
  }, []);

  const swipe = useCallback(
    (targetUserId: string, direction: 'Left' | 'Right') => {
      connectionRef.current?.invoke('Swipe', { targetUserId, direction });
    },
    [],
  );

  const dismissMatch = useCallback(() => {
    setCurrentMatch(null);
  }, []);

  const refreshCandidates = useCallback(() => {
    setIsLoading(true);
    connectionRef.current?.invoke('GetCandidates');
  }, []);

  return {
    candidates,
    currentMatch,
    isConnected,
    isLoading,
    swipe,
    dismissMatch,
    refreshCandidates,
  };
}
