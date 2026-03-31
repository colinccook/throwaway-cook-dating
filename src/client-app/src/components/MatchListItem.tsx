interface MatchListItemProps {
  conversationId: string;
  otherUserId: string;
  lastMessage?: string;
  lastMessageAt?: string;
  onClick: () => void;
}

export default function MatchListItem({
  otherUserId,
  lastMessage,
  lastMessageAt,
  onClick,
}: MatchListItemProps) {
  const timeLabel = lastMessageAt
    ? new Date(lastMessageAt).toLocaleDateString([], {
        month: 'short',
        day: 'numeric',
      })
    : '';

  return (
    <div className="match-list-item" onClick={onClick}>
      <div className="match-list-item-avatar">👤</div>
      <div className="match-list-item-info">
        <div className="match-list-item-header">
          <strong className="match-list-item-name">{otherUserId}</strong>
          {timeLabel && <span className="match-list-item-time">{timeLabel}</span>}
        </div>
        <p className="match-list-item-preview">
          {lastMessage || 'No messages yet — say hi!'}
        </p>
      </div>
    </div>
  );
}
