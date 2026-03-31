import { useState, useCallback } from 'react';
import type { ReactNode } from 'react';
import * as api from '../services/api';
import type { SignUpData } from '../services/api';
import { AuthContext } from './AuthContext';
import type { User } from './AuthContext';

function loadSavedAuth(): { user: User | null; token: string | null } {
  const savedToken = localStorage.getItem('auth_token');
  const savedUser = localStorage.getItem('auth_user');
  if (savedToken && savedUser) {
    return { user: JSON.parse(savedUser) as User, token: savedToken };
  }
  return { user: null, token: null };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [savedAuth] = useState(loadSavedAuth);
  const [user, setUser] = useState<User | null>(savedAuth.user);
  const [token, setToken] = useState<string | null>(savedAuth.token);

  const signIn = useCallback(async (email: string, password: string) => {
    const res = await api.signIn({ email, password });
    const u: User = { id: res.userId, email: res.email };
    localStorage.setItem('auth_token', res.accessToken);
    localStorage.setItem('auth_user', JSON.stringify(u));
    setToken(res.accessToken);
    setUser(u);
  }, []);

  const signUp = useCallback(async (data: SignUpData) => {
    const res = await api.signUp(data);
    const u: User = { id: res.userId, email: res.email };
    localStorage.setItem('auth_token', res.accessToken);
    localStorage.setItem('auth_user', JSON.stringify(u));
    setToken(res.accessToken);
    setUser(u);
  }, []);

  const signOut = useCallback(() => {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('auth_user');
    setToken(null);
    setUser(null);
  }, []);

  return (
    <AuthContext value={{
      user,
      token,
      isAuthenticated: !!token,
      isLoading: false,
      signIn,
      signUp,
      signOut,
    }}>
      {children}
    </AuthContext>
  );
}
