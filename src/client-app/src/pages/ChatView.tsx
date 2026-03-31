import { useParams } from 'react-router-dom';

export default function ChatView() {
  const { matchId } = useParams<{ matchId: string }>();

  return (
    <div className="page">
      <h1>Chat</h1>
      <p>Conversation with match {matchId}</p>
    </div>
  );
}
