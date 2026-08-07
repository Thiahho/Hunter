import { useEffect, useState } from 'react';

interface LiveRefreshLabelProps {
  intervalSeconds: number;
  // Timestamp (ms, tipo Date.now()) del último fetch real — usar dataUpdatedAt de react-query.
  // Si hay más de una query compartiendo el label, pasar la más antigua (Math.min) para que la
  // cuenta regresiva nunca muestre más tiempo del que realmente falta.
  lastUpdatedAt: number;
}

// Cuenta regresiva en vivo hasta el próximo refetch automático (refetchInterval de react-query).
// Se resetea sola cuando lastUpdatedAt cambia, así que siempre queda en sync con el fetch real:
// no es un timer aparte que se pueda desincronizar.
export function LiveRefreshLabel({ intervalSeconds, lastUpdatedAt }: LiveRefreshLabelProps) {
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, []);

  const elapsedSeconds = Math.floor((now - lastUpdatedAt) / 1000);
  const secondsLeft = Math.min(intervalSeconds, Math.max(0, intervalSeconds - elapsedSeconds));

  return <span className="text-xs text-slate-400">Se actualiza en {secondsLeft}s</span>;
}
