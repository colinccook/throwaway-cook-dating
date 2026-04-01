import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { API_BASE_URL } from '../services/api';

interface TenantConfig {
  tenantId: string;
  tenantName: string;
}

const TenantContext = createContext<TenantConfig>({ tenantId: '', tenantName: '' });

// eslint-disable-next-line react-refresh/only-export-components
export function useTenant() {
  return useContext(TenantContext);
}

export function TenantProvider({ children }: { children: ReactNode }) {
  const [config, setConfig] = useState<TenantConfig>({ tenantId: '', tenantName: '' });

  useEffect(() => {
    fetch(`${API_BASE_URL}/config`)
      .then(res => res.json())
      .then(setConfig)
      .catch(() => setConfig({ tenantId: 'cook-dating', tenantName: 'Cook Dating' }));
  }, []);

  return (
    <TenantContext.Provider value={config}>
      {children}
    </TenantContext.Provider>
  );
}
