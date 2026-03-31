import { createContext, useContext, useState, useEffect, useCallback } from 'react';
import type { ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import * as api from '../services/api';
import type { SignUpData } from '../services/api';

interface User {
  id: string;
  email: string;
}

interface AuthContextValue {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  signIn: (email: string, password: string) => Promise<void>;
  signUp: (data: SignUpData) => Promise<void>;
  signOut: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    const savedToken = localStorage.getItem('auth_token');
    const savedUser = localStorage.getItem('auth_user');
    if (savedToken && savedUser) {
      setToken(savedToken);
      setUser(JSON.parse(savedUser) as User);
    }
    setIsLoading(false);
  }, []);

  const signIn = useCallback(async (email: string, password: string) => {
    const res = await api.signIn({ email, password });
    const u: User = { id: res.userId, email: res.email };
    localStorage.setItem('auth_token', res.token);
    localStorage.setItem('auth_user', JSON.stringify(u));
    setToken(res.token);
    setUser(u);
    navigate('/discover');
  }, [navigate]);

  const signUp = useCallback(async (data: SignUpData) => {
    const res = await api.signUp(data);
    const u: User = { id: res.userId, email: res.email };
    localStorage.setItem('auth_token', res.token);
    localStorage.setItem('auth_user', JSON.stringify(u));
    setToken(res.token);
    setUser(u);
    navigate('/profile');
  }, [navigate]);

  const signOut = useCallback(() => {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('auth_user');
    setToken(null);
    setUser(null);
    navigate('/signin');
  }, [navigate]);

  return (
    <AuthContext value={{
      user,
      token,
      isAuthenticated: !!token,
      isLoading,
      signIn,
      signUp,
      signOut,
    }}>
      {children}
    </AuthContext>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return ctx;
}
