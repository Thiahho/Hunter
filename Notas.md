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
