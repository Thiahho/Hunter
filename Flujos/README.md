# Flujos n8n — DIFRANI | Hunter CRM AI

## 01 — Prospect Discovery (Google Places)

`workflows/01-prospect-discovery-google-places.json`

Busca negocios en Google Places según una consulta de texto y los importa como
prospectos en Hunter, reusando el mismo pipeline de deduplicación y preview/confirm
que la importación por CSV.

### Flujo

```
Ejecutar manualmente ─┐
                       ├─> Parámetros de búsqueda ─> Login Hunter API ─> ¿Login OK?
Programado (opcional)─┘                                                   │      │
                                                                          sí     no ─> Error de autenticación (corta ejecución)
                                                                           │
                                                                           v
                                                          Importar desde Google Places ─> ¿Import válido?
                                                                                            │      │
                                                                                           sí     no ─> Sin resultados válidos (corta ejecución)
                                                                                            │
                                                                                            v
                                                                          Confirmar importación ─> Importación completada
```

Sin ramas colgantes: cada camino de error termina en un nodo `Stop and Error`
explícito (queda visible en el historial de ejecuciones de n8n), y el camino
feliz siempre termina en un único nodo final.

### Instalación

1. En n8n: **Workflows → Import from File** y seleccionar el `.json`.
2. Configurar las variables de entorno del **servidor de n8n** (no del workflow):
   - `HUNTER_API_BASE_URL` — ej. `https://api.tudominio.com` (sin barra final)
   - `HUNTER_API_EMAIL` — email de una cuenta de servicio dentro de Hunter
   - `HUNTER_API_PASSWORD` — contraseña de esa cuenta
3. Editar el nodo **"Parámetros de búsqueda"** antes de cada ejecución manual
   (campo `query`), o activar el trigger **"Programado (opcional)"** si se
   quiere correr automáticamente todos los días.

### Seguridad

- **La API key de Google Places nunca pasa por n8n.** Vive únicamente en el
  `appsettings` del backend de Hunter. n8n solo llama a nuestro propio endpoint
  autenticado (`POST /api/v1/imports/google-places`), que internamente habla
  con Google — así evitamos tener el mismo secreto duplicado en dos sistemas.
- Las credenciales de Hunter (`HUNTER_API_EMAIL` / `HUNTER_API_PASSWORD`) se
  leen de variables de entorno del servidor de n8n vía `$env`, **nunca quedan
  escritas en el JSON del workflow** — es seguro versionar/compartir este archivo.
- Usar una cuenta de servicio con rol **Manager** (no **Owner**) para el login
  de n8n — principio de mínimo privilegio.
- El token JWT se pide de nuevo en cada ejecución (expira a los 15 minutos) en
  vez de guardarse entre corridas.
- `maxResults` queda siempre acotado a un máximo de 20 dentro del propio
  workflow (mismo límite que aplica la API) para controlar el costo de cada
  búsqueda contra Google Places.

### Validaciones

- **¿Login OK?** — corta la ejecución con un error explícito si la
  autenticación contra Hunter falla, en vez de seguir con un token vacío.
- **¿Import válido?** — verifica que la respuesta haya sido exitosa *y* que
  haya al menos un registro válido antes de confirmar. Si la búsqueda no trajo
  resultados utilizables, el workflow corta con un mensaje descriptivo
  (incluye cuántos duplicados/inválidos hubo) en vez de confirmar un batch vacío.
- La deduplicación en sí (mismo contacto ya existente en la organización) la
  resuelve el backend, no el workflow — así queda consistente con la
  importación manual por CSV.
