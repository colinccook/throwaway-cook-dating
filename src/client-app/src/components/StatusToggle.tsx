interface StatusToggleProps {
  isOnline: boolean;
  onToggle?: (online: boolean) => void;
}

export default function StatusToggle({ isOnline, onToggle }: StatusToggleProps) {
  return (
    <button
      className={`status-toggle ${isOnline ? 'online' : 'offline'}`}
      onClick={() => onToggle?.(!isOnline)}
    >
      {isOnline ? '🟢 Online' : '⚫ Offline'}
    </button>
  );
}
