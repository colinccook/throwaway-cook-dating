interface SwipeCardProps {
  name: string;
  age: number;
  imageUrl?: string;
  onSwipeLeft?: () => void;
  onSwipeRight?: () => void;
}

export default function SwipeCard({ name, age, imageUrl, onSwipeLeft, onSwipeRight }: SwipeCardProps) {
  return (
    <div className="swipe-card">
      {imageUrl && <img src={imageUrl} alt={name} />}
      <h2>{name}, {age}</h2>
      <div className="swipe-actions">
        <button onClick={onSwipeLeft}>✕</button>
        <button onClick={onSwipeRight}>♥</button>
      </div>
    </div>
  );
}
