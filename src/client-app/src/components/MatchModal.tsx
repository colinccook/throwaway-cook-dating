interface MatchModalProps {
  matchId: string;
  otherDisplayName: string;
  onDismiss: () => void;
  onChat: () => void;
}

export default function MatchModal({ otherDisplayName, onDismiss, onChat }: MatchModalProps) {
  return (
    <div className="match-modal-overlay">
      <div className="match-modal-content">
        <div className="match-modal-emoji">🎉</div>
        <h2>It's a Match!</h2>
        <p>You matched with <strong>{otherDisplayName}</strong></p>
        <div className="match-modal-actions">
          <button className="match-modal-btn match-modal-btn-dismiss" onClick={onDismiss}>
            Keep Swiping
          </button>
          <button className="match-modal-btn match-modal-btn-chat" onClick={onChat}>
            Chat Now
          </button>
        </div>
      </div>
    </div>
  );
}
