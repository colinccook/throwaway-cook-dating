interface MatchListItemProps {
  matchId: string;
  name: string;
  lastMessage?: string;
  onClick?: () => void;
}

export default function MatchListItem({ matchId, name, lastMessage, onClick }: MatchListItemProps) {
  return (
    <div className="match-list-item" data-match-id={matchId} onClick={onClick}>
      <strong>{name}</strong>
      {lastMessage && <p>{lastMessage}</p>}
    </div>
  );
}
