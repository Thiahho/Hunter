📘 13 — Seguridad, Cumplimiento y Gestión de Riesgos — MVP V1

Producto: DIFRANI | Hunter CRM AI
Versión: MVP V1
Objetivo: Definir las medidas técnicas y operativas necesarias para que el sistema pueda realizar prospección automatizada de forma controlada, segura y trazable.

1. Objetivo

Hunter manejará:

Datos de empresas.
Teléfonos.
WhatsApp.
Emails cuando estén disponibles.
Historial de interacciones.
Respuestas.
Leads.
Información comercial.

Por lo tanto, el sistema debe aplicar seguridad desde el inicio.

El principio general será:

OBTENER DATOS
      ↓
VALIDAR
      ↓
CONTACTAR
      ↓
REGISTRAR
      ↓
RESPETAR OPT-OUT
      ↓
AUDITAR
2. Principios Fundamentales

La V1 debe cumplir cinco principios:

1. Minimización

Guardar únicamente los datos necesarios.

2. Trazabilidad

Saber:

Quién
Qué
Cuándo
Cómo
Desde dónde

realizó cada acción.

3. Control

El sistema debe poder detener:

Campaña
Prospecto
Número
Proveedor
Workflow
4. Exclusión

Un contacto que solicite no recibir más mensajes no debe volver a ser contactado.

5. Seguridad

Ningún usuario debe acceder a datos de otra organización.

3. Multi-Tenancy

La V1 utilizará una arquitectura multi-tenant.

Todas las entidades comerciales deberán estar asociadas a:

OrganizationId

Ejemplo:

Organization A
    ├── Prospectos
    ├── Campañas
    ├── Leads
    └── Mensajes

Organization B
    ├── Prospectos
    ├── Campañas
    ├── Leads
    └── Mensajes

Nunca:

Usuario A
    ↓
Datos Organización B
4. Aislamiento de Datos

Se implementará:

JWT
  ↓
UserId
  ↓
OrganizationId
  ↓
Query Filter
  ↓
Datos permitidos

En EF Core:

OrganizationId == CurrentOrganizationId

Las entidades críticas deberán utilizar filtros globales cuando corresponda.

5. Roles

La V1 tendrá inicialmente:

ADMIN
MANAGER
SELLER
ADMIN

Puede:

Configurar organización.
Administrar usuarios.
Ver campañas.
Ver prospectos.
Ver métricas.
MANAGER

Puede:

Crear campañas.
Gestionar prospectos.
Asignar Leads.
Ver métricas.
SELLER

Puede:

Ver Leads asignados.
Gestionar oportunidades.
Registrar actividades.
Marcar ventas.
6. Autenticación

Se utilizará:

JWT Access Token
+
Refresh Token

Buenas prácticas:

Expiración corta del Access Token.
Refresh Token rotativo.
Revocación.
Contraseñas hasheadas.
No almacenar contraseñas en texto plano.
7. Protección de Contraseñas

Las contraseñas nunca deben almacenarse directamente.

Utilizar:

Password Hash
+
Salt

Preferentemente mediante una solución probada como:

ASP.NET Core Identity

o una implementación equivalente.

8. API Keys

Las integraciones externas no deben utilizar credenciales hardcodeadas.

Incorrecto:

appsettings.json
API_KEY=123456

Correcto:

Environment Variables
Secret Manager
Docker Secrets

En producción:

Secret
   ↓
Environment
   ↓
Application
9. n8n

n8n tendrá acceso limitado.

No debe tener acceso directo a:

PostgreSQL

La comunicación será:

n8n
   ↓
API
   ↓
Business Logic
   ↓
Database

Esto permite controlar:

Autorización.
Validaciones.
Idempotencia.
Logs.
10. Service Account

n8n debe utilizar una identidad técnica.

Ejemplo:

hunter-n8n-service

No utilizar:

admin@empresa.com

como credencial técnica.

11. Webhooks

Los webhooks públicos representan un riesgo.

Deben validarse mediante:

Firma
+
Secret
+
Timestamp

Cuando el proveedor lo permita.

Flujo:

Proveedor
    ↓
Webhook
    ↓
Validar firma
    ↓
Validar timestamp
    ↓
Validar evento
    ↓
Procesar
12. Idempotencia

Todos los eventos externos deben ser idempotentes.

Ejemplo:

ExternalMessageId

Si llega dos veces:

MSG-123
MSG-123

Debe procesarse:

1 vez

No:

2 veces

Esto es crítico para evitar:

Leads duplicados.
Mensajes duplicados.
Actividades duplicadas.
13. Lista Global de Exclusión

Hunter debe tener una entidad:

GlobalSuppressionList

La lista debe bloquear:

Teléfono
Email
WhatsApp

cuando el contacto solicite no ser contactado.

Ejemplo:

+5491112345678

queda bloqueado.

14. STOP / NO QUIERO RECIBIR MÁS

El sistema debe detectar mensajes como:

STOP
BAJA
NO ME INTERESA
NO QUIERO RECIBIR MÁS
BORRAME
NO CONTACTAR

La detección puede combinar:

Reglas
+
IA

La regla crítica:

Si el usuario solicita explícitamente no recibir más mensajes, la exclusión debe ejecutarse automáticamente.

15. Flujo de Opt-Out
Prospecto
    ↓
"Por favor no me contacten más"
    ↓
n8n
    ↓
Detección STOP
    ↓
ASP.NET API
    ↓
GlobalSuppressionList
    ↓
Bloqueado

Después:

Nueva campaña
    ↓
Filtrar prospectos
    ↓
Excluir SuppressionList

Resultado:

NO CONTACTAR
16. Regla de Supresión

Antes de cualquier envío:

Prospect
    ↓
¿Está en SuppressionList?
      │
   ┌──┴──┐
   │     │
  SI    NO
   │     │
   ▼     ▼
 STOP   SEND

Esta validación debe ejecutarse antes de entregar el mensaje al proveedor.

17. Baja Permanente

La exclusión será global para la organización.

Si una persona dice:

NO QUIERO RECIBIR MÁS

no podrá volver a recibir mensajes de:

Campaña A
Campaña B
Campaña C

aunque vuelva a aparecer en una fuente externa.

18. Reingreso de Prospectos

Si un prospecto bloqueado vuelve a ser importado:

Google Places
     ↓
Import
     ↓
Prospect existente
     ↓
SuppressionList = TRUE
     ↓
NO CONTACTAR

Nunca se debe eliminar automáticamente la exclusión.

19. Consentimiento y Base Legal

La V1 debe diferenciar:

Dato público

de:

Autorización para recibir comunicaciones

Encontrar públicamente un teléfono no implica necesariamente autorización ilimitada para enviar comunicaciones comerciales.

Por eso el sistema debe mantener:

Source
SourceUrl
CollectedAt
ContactMethod

y registrar la base de procedencia.

La estrategia comercial y legal concreta deberá revisarse según el país y el canal utilizado.

20. Registro de Fuente

Cada prospecto debe registrar:

SourceType
SourceUrl
ExternalId
CollectedAt

Ejemplo:

{
  "sourceType": "GOOGLE_PLACES",
  "sourceUrl": "...",
  "externalId": "ChIJ...",
  "collectedAt": "2026-07-27T10:00:00Z"
}

Esto permite saber de dónde provino el dato.

21. Scraping

El sistema debe evitar:

Scraping de cuentas privadas
Extracción de datos protegidos
Bypass de CAPTCHA
Bypass de autenticación
Evasión de controles anti-bot

Se priorizarán:

APIs oficiales
Datos públicos
Fuentes autorizadas
Directorios permitidos
22. Riesgo de WhatsApp

El principal riesgo comercial de la V1 es:

Mensajería masiva
        ↓
Baja calidad
        ↓
Reportes
        ↓
Bloqueos
        ↓
Pérdida del número

Por eso el sistema debe controlar:

Volumen
Calidad
Tasa de respuesta
Tasa de bloqueo
Tasa de reportes
23. Escalamiento Progresivo

Nunca comenzar directamente con:

1000 mensajes/día

Recomendación:

Día 1
10-20

Día 2
20-50

Día 3
50-100

Día 4+
Incrementar según resultados

El aumento debe depender de:

Entrega
Respuesta
Bloqueos
Reportes
24. Control de Velocidad

El sistema debe incorporar:

Rate Limiting

Ejemplo:

Campaign
    ↓
Batch
    ↓
Delay
    ↓
Next Batch

No:

1000 requests
en 2 segundos

El objetivo es reducir errores y permitir controlar el volumen.

25. Kill Switch

Debe existir un mecanismo para detener inmediatamente todas las campañas.

Ejemplo:

🚨 DETENER TODAS LAS CAMPAÑAS

Al activarlo:

Campaigns
    ↓
PAUSED

También debe poder detenerse:

n8n workflows
26. Suspensión Automática

Si se detecta una anomalía:

Error rate > límite

o:

Block rate > límite

el sistema puede:

PAUSAR CAMPAÑA

Ejemplo:

100 mensajes
↓
30 fallidos
↓
Campaña pausada
27. Logs

Registrar:

Login
Logout
Importación
Creación de campaña
Inicio campaña
Pausa
Mensaje enviado
Mensaje recibido
Opt-out
Creación Lead
Cambio de estado

Ejemplo:

2026-07-27
User: Juan
Action: CAMPAIGN_PAUSED
Campaign: UUID
28. Auditoría

Se recomienda una tabla:

AuditLogs

Campos:

Id
OrganizationId
UserId
Action
Entity
EntityId
Metadata
IpAddress
UserAgent
CreatedAt
29. Protección de Datos

Los datos sensibles deben limitarse.

No almacenar innecesariamente:

DNI
Datos bancarios
Contraseñas
Información personal no relacionada

El sistema debe guardar únicamente información relevante para la actividad comercial.

30. Cifrado
En tránsito
HTTPS / TLS
En reposo

Dependerá de:

Proveedor VPS
Base de datos
Storage

Las credenciales deben estar cifradas o protegidas mediante secretos.

31. Backup

Se recomienda:

Backup diario

Con:

Retención
Verificación
Restauración periódica

Un backup que nunca se prueba no debe considerarse confiable.

Debe realizarse periódicamente:

Backup
↓
Restore Test
↓
Verificar integridad
32. Disaster Recovery

V1:

Base de datos
     ↓
Backup externo
     ↓
Nuevo VPS
     ↓
Restore
     ↓
Reactivar servicios

Debe documentarse:

Qué hacer
Quién lo hace
Dónde está el backup
Cómo restaurar
33. Riesgo de Pérdida de Datos

Riesgo:

VPS falla

Mitigación:

Backup externo
+
Backup diario
34. Riesgo de Duplicación

Riesgo:

Mismo prospecto
varias veces

Mitigación:

Unique Constraints
+
Phone Normalization
+
ExternalId
+
Deduplication
35. Riesgo de Mensaje Duplicado

Riesgo:

n8n procesa workflow
dos veces

Mitigación:

ExternalMessageId
+
Idempotency Key
36. Riesgo de IA Incorrecta

Ejemplo:

"Bueno, después veo"

IA:

INTERESTED

Para reducir errores:

Confidence

Ejemplo:

>= 0.85

crear Lead automáticamente.

< 0.85

marcar:

UNCLEAR

y revisión humana.

37. Riesgo de Falsos Positivos
"Sí, pasame información"

Correcto:

INTERESTED

Pero:

"Sí, pero no me interesa"

Debe clasificarse:

NOT_INTERESTED

Por eso el sistema debe conservar:

Mensaje original
+
Clasificación IA
+
Confidence
38. Riesgo de Alucinación

La IA no debe inventar:

Precios
Stock
Marcas
Descuentos
Condiciones comerciales

En V1:

IA
↓
Clasificar

No:

IA
↓
Vender
39. Riesgo Comercial

El sistema puede generar:

Muchos Leads

pero pocos cierres.

Por eso se debe medir:

Lead
   ↓
Contacto humano
   ↓
Cotización
   ↓
Venta

El sistema no se considerará exitoso solamente por generar Leads.

40. Riesgo de Calidad de Prospectos

Una campaña puede generar:

1000 prospectos

pero:

800 incorrectos

Por eso se debe controlar:

Validación
Teléfono
Categoría
Ubicación
Duplicados
41. Riesgo de Escalar Demasiado Rápido

La V1 debe evitar:

10
↓
1000

La estrategia:

10
↓
50
↓
100
↓
250
↓
500
↓
1000

Cada etapa debe validarse.

42. Matriz de Riesgos
Riesgo	Impacto	Probabilidad	Mitigación
Bloqueo de WhatsApp	Alto	Medio	Escalamiento gradual
Mensajes duplicados	Medio	Medio	Idempotencia
Datos duplicados	Medio	Alto	Deduplicación
IA incorrecta	Medio	Medio	Confidence
Fuga de datos	Alto	Bajo	Multi-tenancy
Caída VPS	Alto	Bajo	Backups
Leads de baja calidad	Alto	Alto	Validación
Costos superiores	Alto	Medio	Cost Tracking
Error n8n	Medio	Medio	Logs
Opt-out ignorado	Alto	Bajo	Suppression List
API externa caída	Medio	Medio	Retry + fallback
Scraping bloqueado	Medio	Medio	APIs oficiales
43. Arquitectura de Seguridad
                     INTERNET
                         │
                         ▼
                      HTTPS
                         │
                         ▼
                       NGINX
                         │
              ┌──────────┴──────────┐
              ▼                     ▼
          Frontend                  API
                                      │
                              Authentication
                                      │
                              Authorization
                                      │
                              Organization
                                      │
                              Business Rules
                                      │
                              PostgreSQL

n8n:

n8n
 ↓
API Key / Service Account
 ↓
ASP.NET API
44. Checklist de Seguridad V1
[ ] HTTPS
[ ] JWT
[ ] Refresh Tokens
[ ] Password Hash
[ ] Multi-Tenancy
[ ] RBAC
[ ] API Keys protegidas
[ ] Secrets fuera del código
[ ] Webhook validation
[ ] Idempotencia
[ ] Rate limiting
[ ] Audit logs
[ ] Backup
[ ] Restore test
[ ] Suppression List
[ ] Opt-out automático
[ ] Kill Switch
[ ] Logs
45. Regla Comercial Fundamental

Hunter nunca debe priorizar:

Cantidad de mensajes

por encima de:

Calidad
+
Relevancia
+
Conversión
+
Rentabilidad

El objetivo real es:

PROSPECTO CALIFICADO
        ↓
CONTACTO
        ↓
INTERÉS
        ↓
LEAD
        ↓
VENTA
46. Criterio de Aceptación

La V1 no podrá considerarse lista para producción hasta que:

✅ Exista autenticación
✅ Exista aislamiento multi-tenant
✅ Exista lista de exclusión
✅ STOP funcione
✅ Los webhooks estén protegidos
✅ Exista idempotencia
✅ Existan backups
✅ Exista Kill Switch
✅ Existan logs
✅ Se puedan pausar campañas
✅ Se pueda rastrear el origen del prospecto