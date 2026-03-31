interface ChatBubbleProps {
  content: string;
  sentAt: string;
  isMine: boolean;
}

export default function ChatBubble({ content, sentAt, isMine }: ChatBubbleProps) {
  const time = new Date(sentAt).toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
  });

  return (
    <div className={`chat-bubble ${isMine ? 'mine' : 'theirs'}`}>
      <p className="chat-bubble-content">{content}</p>
      <span className="chat-bubble-time">{time}</span>
    </div>
  );
}
