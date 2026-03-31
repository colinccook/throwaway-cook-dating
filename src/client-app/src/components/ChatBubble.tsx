interface ChatBubbleProps {
  message: string;
  isMine: boolean;
  timestamp?: string;
}

export default function ChatBubble({ message, isMine, timestamp }: ChatBubbleProps) {
  return (
    <div className={`chat-bubble ${isMine ? 'mine' : 'theirs'}`}>
      <p>{message}</p>
      {timestamp && <span className="timestamp">{timestamp}</span>}
    </div>
  );
}
