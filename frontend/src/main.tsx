import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import './index.css'
import App from './App.tsx'
import { useAuthStore } from './store/authStore'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
})

// La caché de React Query no depende del usuario (las keys son ['prospects', ...], sin
// organización): al cerrar sesión y entrar con otra cuenta en la misma pestaña, la primera
// pantalla mostraba los datos de la sesión anterior. Se vacía cada vez que cambia el usuario.
useAuthStore.subscribe((state, prev) => {
  if (state.user?.id !== prev.user?.id || state.user?.organizationId !== prev.user?.organizationId) {
    queryClient.clear()
  }
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
)
