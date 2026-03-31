import type { CandidateDto } from '../hooks/useMatchingHub';

interface SwipeCardProps {
  candidate: CandidateDto;
  onSwipeLeft: () => void;
  onSwipeRight: () => void;
}

function avatarEmoji(gender: string): string {
  switch (gender.toLowerCase()) {
    case 'male':
      return '👨';
    case 'female':
      return '👩';
    default:
      return '👤';
  }
}

export default function SwipeCard({ candidate, onSwipeLeft, onSwipeRight }: SwipeCardProps) {
  return (
    <div className="swipe-card">
      <div className="swipe-card-avatar">{avatarEmoji(candidate.gender)}</div>
      <h2 className="swipe-card-name">{candidate.displayName}</h2>
      <p className="swipe-card-gender">{candidate.gender}</p>
      <div className="swipe-actions">
        <button className="swipe-btn swipe-btn-pass" onClick={onSwipeLeft}>
          ✕ Pass
        </button>
        <button className="swipe-btn swipe-btn-like" onClick={onSwipeRight}>
          ♥ Like
        </button>
      </div>
    </div>
  );
}
