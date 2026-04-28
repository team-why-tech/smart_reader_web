import { createContext, useContext, useState, useEffect, useCallback, type ReactNode } from 'react';
import * as authApi from '../api/auth';
import { setTokens, clearTokens, getAccessToken, getRefreshToken, decodeJwt, isTokenExpired } from '../utils/token';
import type { AuthUser, LoginDto, RegisterDto } from '../types';

interface AuthContextType {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (data: LoginDto) => Promise<void>;
  register: (data: RegisterDto) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const token = getAccessToken();
    if (token) {
      if (isTokenExpired(token)) {
        const refresh = getRefreshToken();
        if (refresh) {
          authApi
            .refreshToken({ refreshToken: refresh })
            .then(({ data: res }) => {
              if (res.success && res.data) {
                setTokens(res.data.accessToken, res.data.refreshToken);
                setUser(decodeJwt(res.data.accessToken));
              } else {
                clearTokens();
              }
            })
            .catch(() => clearTokens())
            .finally(() => setIsLoading(false));
          return;
        }
        clearTokens();
      } else {
        setUser(decodeJwt(token));
      }
    }
    setIsLoading(false);
  }, []);

  const login = useCallback(async (data: LoginDto) => {
    const { data: res } = await authApi.login(data);
    if (res.success && res.data) {
      setTokens(res.data.accessToken, res.data.refreshToken);
      setUser(decodeJwt(res.data.accessToken));
    } else {
      throw new Error(res.message ?? 'Login failed');
    }
  }, []);

  const register = useCallback(async (data: RegisterDto) => {
    const { data: res } = await authApi.register(data);
    if (res.success && res.data) {
      setTokens(res.data.accessToken, res.data.refreshToken);
      setUser(decodeJwt(res.data.accessToken));
    } else {
      throw new Error(res.message ?? 'Registration failed');
    }
  }, []);

  const logout = useCallback(() => {
    clearTokens();
    setUser(null);
  }, []);

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, isLoading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within AuthProvider');
  return context;
}
