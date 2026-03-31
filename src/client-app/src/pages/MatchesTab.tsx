import { useNavigate } from 'react-router-dom';
import { useConversationHub } from '../hooks/useConversationHub';
import MatchListItem from '../components/MatchListItem';

export default function MatchesTab() {
  const { conversations, isLoading } = useConversationHub();
  const navigate = useNavigate();

  return (
    <div className="page matches-page">
      <h1>Matches</h1>

      {isLoading && <p className="matches-status">Loading conversations…</p>}

      {!isLoading && conversations.length === 0 && (
        <div className="matches-empty">
          <p>No matches yet — keep swiping!</p>
        </div>
      )}

      {!isLoading &&
        conversations.map((c) => (
          <MatchListItem
            key={c.conversationId}
            conversationId={c.conversationId}
            otherUserId={c.otherUserId}
            lastMessage={c.lastMessage}
            lastMessageAt={c.lastMessageAt}
            onClick={() => navigate(`/chat/${c.conversationId}`)}
          />
        ))}
    </div>
  );
}
