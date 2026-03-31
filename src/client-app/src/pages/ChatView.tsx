import { useEffect, useRef, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useConversationHub } from '../hooks/useConversationHub';
import { useAuth } from '../hooks/useAuth';
import ChatBubble from '../components/ChatBubble';

export default function ChatView() {
  const { conversationId } = useParams<{ conversationId: string }>();
  const { messages, joinConversation, leaveConversation, sendMessage, markRead } =
    useConversationHub();
  const { user } = useAuth();
  const navigate = useNavigate();
  const [input, setInput] = useState('');
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (conversationId) {
      joinConversation(conversationId);
      markRead(conversationId);
    }
    return () => {
      if (conversationId) {
        leaveConversation(conversationId);
      }
    };
  }, [conversationId, joinConversation, leaveConversation, markRead]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSend = () => {
    const trimmed = input.trim();
    if (!trimmed || !conversationId) return;
    sendMessage(conversationId, trimmed);
    setInput('');
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  return (
    <div className="page chat-view">
      <div className="chat-header">
        <button className="chat-back-btn" onClick={() => navigate('/matches')}>
          ← Back
        </button>
        <span className="chat-header-title">Chat</span>
      </div>

      <div className="messages-container">
        {messages.map((msg) => (
          <ChatBubble
            key={msg.id}
            content={msg.content}
            sentAt={msg.sentAt}
            isMine={msg.senderId === user?.id}
          />
        ))}
        <div ref={messagesEndRef} />
      </div>

      <div className="chat-input">
        <input
          type="text"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Type a message…"
        />
        <button onClick={handleSend} disabled={!input.trim()}>
          Send
        </button>
      </div>
    </div>
  );
}
