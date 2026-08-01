# Notas

## Meta WhatsApp Cloud API — datos a setear en `appsettings`

Sección `WhatsAppCloudApi` en `backend/src/Hunter.Api/appsettings.Development.json` (o el
`appsettings.{Environment}.json` que corresponda). Ver
`backend/src/Hunter.Infrastructure/Messaging/WhatsAppCloudApiOptions.cs`.

| Campo en Hunter | De dónde sale en Meta |
|---|---|
| `PhoneNumberId` | Meta for Developers → tu app → WhatsApp → API Setup (es un ID numérico, **no** el número de teléfono) |
| `AccessToken` | Token **permanente** de un System User (Business Settings → System Users), con permisos `whatsapp_business_messaging` / `whatsapp_business_management` — el token temporal de 24hs que aparece por defecto en "API Setup" no sirve para producción |
| `AppSecret` | Meta for Developers → tu app → Settings → Basic. Valida la firma HMAC (`X-Hub-Signature-256`) de los webhooks entrantes |
| `WebhookVerifyToken` | Lo inventás vos; va en `appsettings` **y** en Meta al dar de alta el webhook (mismo valor en los dos lados) |
| `OrganizationId` | El ID de la organización en Hunter — el payload de Meta no dice a qué org pertenece el mensaje, V1 asume un solo número = una sola org |
| `TemplateName` | Opcional al inicio — solo hace falta para **iniciar** conversaciones en frío. Para responder dentro de la ventana de 24hs de un mensaje entrante no se necesita |

Bloqueante a tener en cuenta: Meta necesita pegarle a una URL pública HTTPS
(`https://tu-dominio/api/v1/webhooks/messaging/whatsapp`) — no le puede hablar a `localhost`.
Mientras el backend no esté deployado, hace falta un túnel (Cloudflare Tunnel / ngrok) para
probar el webhook real.

## Deploy — Vercel (frontend) + Render (backend)

- **Frontend → Vercel.** SPA con Vite (`frontend/package.json`: React 19 + Vite + Tailwind).
  Deploy automático desde GitHub, CDN global, free tier generoso.
- **Backend → Render**, no Vercel. Vercel es serverless/edge, no corre un proceso ASP.NET Core
  de larga duración. Render sí soporta "Web Service" desde el `Dockerfile` que ya está en
  `backend/`, más Postgres administrado. De paso resuelve la URL pública HTTPS que necesitan
  tanto n8n Cloud como el webhook de Meta.

**Trade-off:** el free tier de Render duerme el servicio sin tráfico y tarda ~30-50s en
despertar (cold start) — problemático para un webhook de Meta que espera respuesta rápida. Para
producción real con WhatsApp conviene el plan pago más chico (~$7/mes) para que quede siempre
despierto.

## Estado al cortar (30/31-jul-2026) — qué queda para mañana

### Hecho hoy
- **WhatsApp Cloud API real conectado y probado** con número de pruebas de Meta (+1 555 677 5179,
  `PhoneNumberId: 1266621949861306`) y tu número personal como destinatario de prueba.
- **Bug real encontrado y corregido:** los números argentinos se mandaban a Meta con el "9" de
  celular (`5491122692061`) y Meta los rechaza así — hay que sacarlo (`541122692061`) en el campo
  `to`. Corregido en `WhatsAppCloudApiMessageProvider.ToMetaWhatsAppFormat`, con test.
- **Confirmado empíricamente:** texto libre NUNCA entrega a alguien que no te escribió antes
  (sin ventana de 24hs) — hace falta sí o sí una plantilla aprobada por Meta para el primer
  contacto en frío. Es exactamente el "PASO 1" del flujo que pediste.
- **Plantilla real enviada a revisión en Meta** (Tauro Parts, cuerpo con `{{1}}` nombre y `{{2}}`
  URL del catálogo) — quedó pendiente de aprobación.
- **Nuevo: crear prospecto manual desde el frontend** (`/app/prospects/new`) — para cargar
  números de prueba sin tocar los prospectos reales de Google Places.
- **Nuevo: botón "Enviar mensaje de prueba"** en el detalle de un prospecto — dispara un envío
  directo (sin pasar por Campaign), respeta Kill Switch y lista de supresión.
- **Nuevo: webhook de estado de mensajes de Meta** (`sent`/`delivered`/`read`/`failed`) — antes
  no teníamos forma de saber si un mensaje realmente llegó. Ahora `Message` guarda
  `deliveredAt`/`readAt`/`failedAt`/`failureReason` y se expone en `GET /api/v1/messages`.
- Suite de tests: 63/63 pasando.
- ngrok instalado y autenticado (`ngrok config add-authtoken` ya corrido) — **el túnel todavía
  no se levantó**, quedó ahí cuando cortamos.

### Estado al retomar (31-jul-2026, sesión siguiente)
- **Túnel de ngrok levantado**: backend corriendo en `localhost:5226`, túnel público en
  `https://6346-149-78-52-180.ngrok-free.app` (plan free — la URL cambia cada vez que se
  reinicia el túnel).
- Handshake de verificación del webhook probado de punta a punta contra la URL de ngrok
  (`GET .../api/v1/webhooks/messaging/whatsapp?hub.mode=subscribe&hub.verify_token=...&hub.challenge=...`
  devuelve el challenge correctamente) usando el `WebhookVerifyToken` de
  `appsettings.Development.json` (`dev-only-verify-token-change-me`).
- **Pendiente:** dar de alta el webhook en Meta for Developers con esa URL (todavía no
  confirmado que Meta lo haya registrado del lado de ellos).

### Para seguir, en orden
1. Dar de alta el webhook en Meta for Developers (WhatsApp → Configuration → Webhook):
   callback URL `https://6346-149-78-52-180.ngrok-free.app/api/v1/webhooks/messaging/whatsapp`,
   verify token `dev-only-verify-token-change-me`, suscribir field `messages`.
2. Probar el ciclo completo en vivo: responder al mensaje de bienvenida desde tu celular → ver
   que llega el webhook → se clasifica con la IA → se crea el Lead → aparece en el Kanban del
   frontend.
3. Revisar si ya aprobaron la plantilla de Tauro Parts. Si está aprobada: pasar el nombre exacto
   para cargarlo en `WhatsAppCloudApi:TemplateName` (+ `TemplateLanguage`) y volver a probar el
   envío de bienvenida real de punta a punta.
4. Definir qué dispara el mensaje de bienvenida automáticamente (hoy es 100% manual: o el botón
   de mensaje de prueba, o crear una `Campaign` a mano vía API — no hay pantalla en el frontend
   para campañas todavía).
5. Nada de esta sesión está commiteado a git — sigue todo en el working directory.
