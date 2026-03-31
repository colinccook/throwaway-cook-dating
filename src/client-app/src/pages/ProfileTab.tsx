import { useEffect, useState } from 'react';
import { useAuth } from '../hooks/useAuth';
import * as api from '../services/api';

export default function ProfileTab() {
  const { signOut } = useAuth();

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const [lookingStatus, setLookingStatus] = useState<string>('NotLooking');
  const [displayName, setDisplayName] = useState('');
  const [bio, setBio] = useState('');
  const [dateOfBirth, setDateOfBirth] = useState('');
  const [gender, setGender] = useState('Male');
  const [preferredGender, setPreferredGender] = useState('');
  const [minAge, setMinAge] = useState(18);
  const [maxAge, setMaxAge] = useState(99);
  const [maxDistanceKm, setMaxDistanceKm] = useState(50);

  useEffect(() => {
    api.getProfile()
      .then((p) => {
        setLookingStatus(p.lookingStatus);
        setDisplayName(p.displayName);
        setBio(p.bio);
        setDateOfBirth(p.dateOfBirth);
        setGender(p.gender);
        setPreferredGender(p.preferredGender ?? '');
        setMinAge(p.minAge);
        setMaxAge(p.maxAge);
        setMaxDistanceKm(p.maxDistanceKm);
      })
      .catch(() => setMessage({ type: 'error', text: 'Failed to load profile' }))
      .finally(() => setLoading(false));
  }, []);

  const isActive = lookingStatus === 'ActivelyLooking';

  async function handleToggle() {
    const next = isActive ? 'NotLooking' : 'ActivelyLooking';
    try {
      await api.setLookingStatus(next);
      setLookingStatus(next);
    } catch {
      setMessage({ type: 'error', text: 'Failed to update status' });
    }
  }

  async function handleSave(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setMessage(null);
    try {
      await api.updateProfile({
        displayName,
        bio,
        dateOfBirth,
        gender,
        preferredGender: preferredGender || undefined,
        minAge,
        maxAge,
        maxDistanceKm,
      });
      setMessage({ type: 'success', text: 'Profile saved!' });
    } catch {
      setMessage({ type: 'error', text: 'Failed to save profile' });
    } finally {
      setSaving(false);
    }
  }

  if (loading) {
    return <div className="page"><p>Loading profile…</p></div>;
  }

  return (
    <div className="page profile-page">
      <h1>Profile</h1>

      {/* Looking Status Toggle */}
      <button
        className={`looking-toggle ${isActive ? 'active' : ''}`}
        onClick={handleToggle}
        type="button"
      >
        {isActive ? '🟢 Actively Looking' : '⚪ Not Looking'}
      </button>

      {/* Profile Form */}
      <form className="profile-form" onSubmit={handleSave}>
        <label>
          Display Name
          <input type="text" value={displayName} onChange={(e) => setDisplayName(e.target.value)} required />
        </label>

        <label>
          Bio
          <textarea rows={3} value={bio} onChange={(e) => setBio(e.target.value)} />
        </label>

        <label>
          Date of Birth
          <input type="date" value={dateOfBirth} onChange={(e) => setDateOfBirth(e.target.value)} required />
        </label>

        <label>
          Gender
          <select value={gender} onChange={(e) => setGender(e.target.value)}>
            <option value="Male">Male</option>
            <option value="Female">Female</option>
            <option value="NonBinary">Non-Binary</option>
          </select>
        </label>

        <label>
          Preferred Gender
          <select value={preferredGender} onChange={(e) => setPreferredGender(e.target.value)}>
            <option value="">Any</option>
            <option value="Male">Male</option>
            <option value="Female">Female</option>
            <option value="NonBinary">Non-Binary</option>
          </select>
        </label>

        <div className="profile-form-row">
          <label>
            Min Age
            <input type="number" min={18} max={99} value={minAge} onChange={(e) => setMinAge(Number(e.target.value))} />
          </label>
          <label>
            Max Age
            <input type="number" min={18} max={99} value={maxAge} onChange={(e) => setMaxAge(Number(e.target.value))} />
          </label>
        </div>

        <label>
          Max Distance (km)
          <input type="number" min={1} value={maxDistanceKm} onChange={(e) => setMaxDistanceKm(Number(e.target.value))} />
        </label>

        {message && (
          <p className={`profile-message ${message.type}`}>{message.text}</p>
        )}

        <button type="submit" className="profile-save-btn" disabled={saving}>
          {saving ? 'Saving…' : 'Save Changes'}
        </button>
      </form>

      <button className="profile-signout-btn" type="button" onClick={signOut}>
        Sign Out
      </button>
    </div>
  );
}
