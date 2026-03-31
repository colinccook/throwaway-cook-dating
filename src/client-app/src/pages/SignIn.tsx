import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, Navigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

export default function SignIn() {
  const { signIn, isAuthenticated } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  if (isAuthenticated) {
    return <Navigate to="/discover" replace />;
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await signIn(email, password);
      // Navigation handled by <Navigate> after state update triggers re-render
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Sign in failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="page">
      <h1>Sign In</h1>
      <form onSubmit={handleSubmit}>
        <div>
          <label htmlFor="email">Email</label>
          <input id="email" type="email" required value={email} onChange={e => setEmail(e.target.value)} />
        </div>
        <div>
          <label htmlFor="password">Password</label>
          <input id="password" type="password" required value={password} onChange={e => setPassword(e.target.value)} />
        </div>
        {error && <p style={{ color: 'red' }}>{error}</p>}
        <button type="submit" disabled={loading}>{loading ? 'Signing in…' : 'Sign In'}</button>
      </form>
      <p>Don't have an account? <Link to="/signup">Sign Up</Link></p>
    </div>
  );
}
