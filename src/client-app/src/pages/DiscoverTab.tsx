import { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMatchingHub } from '../hooks/useMatchingHub';
import SwipeCard from '../components/SwipeCard';
import MatchModal from '../components/MatchModal';

export default function DiscoverTab() {
  const {
    candidates,
    currentMatch,
    isLoading,
    swipe,
    dismissMatch,
    refreshCandidates,
  } = useMatchingHub();
  const navigate = useNavigate();

  const currentCandidate = candidates[0] ?? null;

  const handleSwipe = useCallback(
    (direction: 'Left' | 'Right') => {
      if (!currentCandidate) return;
      swipe(currentCandidate.userId, direction);
    },
    [currentCandidate, swipe],
  );

  return (
    <div className="page discover-page">
      <h1>Discover</h1>

      {isLoading && <p className="discover-status">Loading candidates…</p>}

      {!isLoading && !currentCandidate && (
        <div className="discover-empty">
          <p>No more candidates</p>
          <button className="discover-refresh-btn" onClick={refreshCandidates}>
            Refresh
          </button>
        </div>
      )}

      {!isLoading && currentCandidate && (
        <SwipeCard
          candidate={currentCandidate}
          onSwipeLeft={() => handleSwipe('Left')}
          onSwipeRight={() => handleSwipe('Right')}
        />
      )}

      {currentMatch && (
        <MatchModal
          matchId={currentMatch.matchId}
          otherDisplayName={currentMatch.otherDisplayName}
          onDismiss={dismissMatch}
          onChat={() => {
            dismissMatch();
            navigate('/matches');
          }}
        />
      )}
    </div>
  );
}
