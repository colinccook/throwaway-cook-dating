interface User {
  id: string;
  email: string;
  name: string;
}

interface AuthState {
  user: User | null;
  signIn: (email: string, password: string) => Promise<void>;
  signUp: (email: string, password: string, name: string) => Promise<void>;
  signOut: () => void;
}

export function useAuth(): AuthState {
  return {
    user: null,
    signIn: async () => {},
    signUp: async () => {},
    signOut: () => {},
  };
}
