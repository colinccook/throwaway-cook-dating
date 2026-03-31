import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

export default function SignUp() {
  const { signUp, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [dateOfBirth, setDateOfBirth] = useState('');
  const [gender, setGender] = useState('Male');
  const [preferredGender, setPreferredGender] = useState('');
  const [minAge, setMinAge] = useState(18);
  const [maxAge, setMaxAge] = useState(99);
  const [maxDistanceKm, setMaxDistanceKm] = useState(50);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  if (isAuthenticated) {
    return <Navigate to="/profile" replace />;
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await signUp({
        email,
        password,
        displayName,
        dateOfBirth,
        gender,
        preferredGender: preferredGender || undefined,
        minAge,
        maxAge,
        maxDistanceKm,
      });
      navigate('/profile', { replace: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Sign up failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="page">
      <h1>Sign Up</h1>
      <form onSubmit={handleSubmit}>
        <div>
          <label htmlFor="email">Email</label>
          <input id="email" type="email" required value={email} onChange={e => setEmail(e.target.value)} />
        </div>
        <div>
          <label htmlFor="password">Password</label>
          <input id="password" type="password" required value={password} onChange={e => setPassword(e.target.value)} />
        </div>
        <div>
          <label htmlFor="displayName">Display Name</label>
          <input id="displayName" type="text" required value={displayName} onChange={e => setDisplayName(e.target.value)} />
        </div>
        <div>
          <label htmlFor="dateOfBirth">Date of Birth</label>
          <input id="dateOfBirth" type="date" required value={dateOfBirth} onChange={e => setDateOfBirth(e.target.value)} />
        </div>
        <div>
          <label htmlFor="gender">Gender</label>
          <select id="gender" value={gender} onChange={e => setGender(e.target.value)}>
            <option value="Male">Male</option>
            <option value="Female">Female</option>
            <option value="NonBinary">Non-Binary</option>
          </select>
        </div>
        <div>
          <label htmlFor="preferredGender">Preferred Gender (optional)</label>
          <select id="preferredGender" value={preferredGender} onChange={e => setPreferredGender(e.target.value)}>
            <option value="">No preference</option>
            <option value="Male">Male</option>
            <option value="Female">Female</option>
            <option value="NonBinary">Non-Binary</option>
          </select>
        </div>
        <div>
          <label htmlFor="minAge">Min Age</label>
          <input id="minAge" type="number" required min={18} value={minAge} onChange={e => setMinAge(Number(e.target.value))} />
        </div>
        <div>
          <label htmlFor="maxAge">Max Age</label>
          <input id="maxAge" type="number" required min={18} value={maxAge} onChange={e => setMaxAge(Number(e.target.value))} />
        </div>
        <div>
          <label htmlFor="maxDistanceKm">Max Distance (km)</label>
          <input id="maxDistanceKm" type="number" required min={1} value={maxDistanceKm} onChange={e => setMaxDistanceKm(Number(e.target.value))} />
        </div>
        {error && <p style={{ color: 'red' }}>{error}</p>}
        <button type="submit" disabled={loading}>{loading ? 'Creating account…' : 'Sign Up'}</button>
      </form>
      <p>Already have an account? <Link to="/signin">Sign In</Link></p>
    </div>
  );
}
