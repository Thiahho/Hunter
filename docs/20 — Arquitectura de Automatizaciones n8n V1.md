📘 20 — Arquitectura de Automatizaciones n8n V1

Producto: DIFRANI | Hunter CRM AI
Empresa inicial: Difrani
Versión: MVP V1
Motor de automatización: n8n
Backend: ASP.NET Core 8
Base de datos: PostgreSQL
Objetivo: Automatizar la prospección y el procesamiento de respuestas, dejando el cierre comercial en manos humanas.

1. Objetivo

n8n será el motor de orquestación de Hunter V1.

Su función será conectar:

                    HUNTER API
                        │
        ┌───────────────┼───────────────┐
        ▼               ▼               ▼
    PostgreSQL        IA            Mensajería
        │               │               │
        └───────────────┼───────────────┘
                        ▼
                     n8n

La regla principal será:

n8n automatiza el proceso, pero la lógica comercial crítica permanece en la API.

2. Responsabilidades de n8n

n8n será responsable de:

✓ Ejecutar procesos programados
✓ Orquestar campañas
✓ Consultar destinatarios
✓ Procesar colas
✓ Controlar tiempos
✓ Integrar proveedores
✓ Recibir Webhooks
✓ Enviar respuestas a la API
✓ Ejecutar clasificación IA
✓ Notificar vendedores
✓ Ejecutar seguimientos
✓ Registrar errores

La API será responsable de:

✓ Reglas de negocio
✓ Seguridad
✓ Multi-tenancy
✓ Persistencia
✓ Validación
✓ Creación de Leads
✓ Control de Suppression List
✓ Métricas
✓ Auditoría
3. Arquitectura General
┌──────────────────────┐
│       FRONTEND       │
│      React + TS      │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│      HUNTER API      │
│    ASP.NET Core 8    │
└──────┬───────────────┘
       │
       ├───────────────┐
       ▼               ▼
┌─────────────┐   ┌─────────────┐
│ PostgreSQL  │   │     n8n     │
└─────────────┘   └──────┬──────┘
                         │
                ┌────────┼────────┐
                ▼        ▼        ▼
             Mensajes    IA    Notificaciones
4. Workflow 01 — Preparación de Prospectos
Objetivo

Preparar los prospectos antes de enviarlos a una campaña.

Flujo
Schedule
   ↓
Consultar campañas READY
   ↓
Obtener destinatarios
   ↓
Validar datos
   ↓
Consultar Suppression List
   ↓
Descartar bloqueados
   ↓
Crear Queue
Nodo 1 — Schedule Trigger

Ejecutar cada:

1 minuto

La frecuencia será configurable.

Nodo 2 — Consultar Campañas
GET /api/v1/internal/campaigns/ready

Respuesta:

{
  "campaigns": [
    {
      "id": "uuid",
      "status": "READY"
    }
  ]
}
Nodo 3 — Obtener Destinatarios
GET /api/v1/internal/campaigns/{id}/recipients
Nodo 4 — Validación

Validar:

Teléfono
WhatsApp
Estado
Suppression
Campaña
Nodo 5 — Preparar Envío

Cada destinatario pasa a:

QUEUED
5. Workflow 02 — Envío de Mensajes
Objetivo

Enviar mensajes respetando límites configurados.

Queue
 ↓
Obtener siguiente lote
 ↓
Check Kill Switch
 ↓
Check Campaign Status
 ↓
Check Suppression
 ↓
Render Template
 ↓
Enviar
 ↓
Registrar resultado
 ↓
Esperar
 ↓
Siguiente
6. Control de Kill Switch

Antes de cada lote:

¿Kill Switch activo?

Si:

YES

Entonces:

STOP

Si:

NO

Continúa.

Esto permite detener todas las campañas sin apagar n8n.

7. Control de Campaña

Antes de enviar:

Campaign.Status

Si:

RUNNING

Continuar.

Si:

PAUSED
CANCELLED
COMPLETED

No enviar.

8. Check Suppression

Cada destinatario debe validarse nuevamente.

Prospect
   ↓
Suppression Check
   ↓
¿Bloqueado?

Si:

YES

Resultado:

STOPPED

Si:

NO

Continúa.

9. Render de Template

Ejemplo:

Hola {{business_name}}, somos Difrani...

Se transforma:

Hola Repuestos Oeste, somos Difrani...

Variables:

business_name
city
category
sender_name
10. Envío

n8n ejecutará:

HTTP Request

hacia el proveedor correspondiente.

Ejemplo conceptual:

n8n
 ↓
Messaging Provider
 ↓
Message ID
 ↓
Hunter API
11. Registro de Mensaje

Después del envío:

POST /api/v1/internal/messages/register

Datos:

{
  "campaignRecipientId": "uuid",
  "externalMessageId": "provider-id",
  "status": "SENT"
}
12. Error de Envío

Si falla:

Provider
 ↓
ERROR
 ↓
n8n Catch
 ↓
Registrar FAILED
 ↓
Guardar error

No se debe reintentar infinitamente.

13. Reintentos

Configuración inicial:

Intento 1
    ↓
Error
    ↓
Esperar
    ↓
Intento 2
    ↓
Error
    ↓
Esperar
    ↓
Intento 3
    ↓
FAILED

Máximo:

3 intentos
14. Rate Limit

Cada campaña puede definir:

MessagesPerMinute
MessagesPerHour
MessagesPerDay

n8n deberá consultar estos límites antes de procesar.

Ejemplo:

1000 mensajes diarios

No significa:

1000 instantáneos

sino:

Distribuidos durante el día
15. Workflow 03 — Control de Límites

Se recomienda separar la lógica de envío del control de velocidad.

Campaign
 ↓
Rate Limit Check
 ↓
¿Puede enviar?

Si:

YES

Enviar.

Si:

NO

Esperar.

16. Distribución del Envío

Ejemplo:

Objetivo:
1000 mensajes / día

Distribución:

08:00 → 100
09:00 → 100
10:00 → 100
11:00 → 100
12:00 → 100
13:00 → 100
14:00 → 100
15:00 → 100
16:00 → 100
17:00 → 100

La distribución final dependerá del proveedor y de la estrategia definida.

17. Workflow 04 — Recepción de Respuestas
Trigger
Webhook

Endpoint:

POST /webhooks/messaging/inbound

Flujo:

Webhook
 ↓
Validar Firma
 ↓
Validar Payload
 ↓
Identificar Contacto
 ↓
Identificar Prospecto
 ↓
Guardar Respuesta
 ↓
Clasificar IA
18. Validación de Webhook

Validar:

Firma
Provider
ExternalMessageId
Timestamp
Payload

Si falla:

400 / 401

No procesar.

19. Idempotencia

Antes de procesar:

¿ExternalMessageId existe?

Si:

YES

No procesar nuevamente.

Esto evita duplicados.

20. Identificación del Prospecto

n8n enviará:

Phone
WhatsApp
ExternalId

a:

GET /api/v1/internal/prospects/by-contact

Resultado:

Prospect encontrado

Si no existe:

Prospect no encontrado

Se deberá registrar para revisión.

21. Workflow 05 — Clasificación IA
Objetivo

Determinar la intención de la respuesta.

Respuesta
 ↓
IA
 ↓
Classification
 ↓
Confidence

Categorías:

INTERESTED
NOT_INTERESTED
QUESTION
UNCLEAR
STOP
22. Prompt de Clasificación

La IA recibirá información similar a:

Clasificá el siguiente mensaje:

"Sí, pasame información"

Opciones:
INTERESTED
NOT_INTERESTED
QUESTION
UNCLEAR
STOP

Respondé únicamente JSON.

Resultado esperado:

{
  "classification": "INTERESTED",
  "confidence": 0.98
}
23. Regla de Confianza

V1:

Confidence >= 0.80

Puede procesarse automáticamente.

Si:

Confidence < 0.80

Resultado:

UNCLEAR

y pasa a revisión.

24. Workflow 06 — Detección de Interés

Si:

INTERESTED

n8n ejecuta:

POST /internal/responses/{id}/process

La API realiza:

Update CampaignRecipient
        ↓
Update Prospect
        ↓
Create Lead
        ↓
Assign Seller
        ↓
Create Activity
25. Creación de Lead

Ejemplo:

Prospecto:
Repuestos Oeste

Respuesta:
"Sí, pasame información"

Clasificación:
INTERESTED

Lead:
NEW
26. Asignación de Lead

V1:

Round Robin

Ejemplo:

Juan
 ↓
Pedro
 ↓
Carlos
 ↓
Juan

Posteriormente:

Asignación por zona
Asignación por categoría
Asignación por disponibilidad
27. Workflow 07 — Notificación Humana

Cuando se crea un Lead:

Lead Created
 ↓
Notification

Canales posibles:

Telegram
WhatsApp interno
Email
Discord
Panel Web

Para V1 se recomienda:

Panel Web
+
Telegram
28. Notificación

Ejemplo:

🚨 NUEVO LEAD

Empresa:
Repuestos Oeste

Contacto:
+54 9 ...

Campaña:
Distribuidores Zona Oeste

Respuesta:
"Sí, pasame información."

Estado:
INTERESADO

Acción:
Contactar al cliente.
29. Human Handoff

El flujo termina en:

BOT
 ↓
DETECTA INTERÉS
 ↓
LEAD
 ↓
HUMANO

El bot V1:

NO cotiza
NO negocia
NO cierra
NO promete precios
NO ofrece descuentos

El humano toma el control.

30. Workflow 08 — Opt-Out

Si la IA detecta:

STOP

Flujo:

Response
 ↓
STOP
 ↓
Create Suppression
 ↓
Update Prospect
 ↓
STOP CAMPAIGN RECIPIENT
31. Ejemplos de STOP
No me contacten más
No quiero recibir mensajes
Borrame
No me escriban

La IA debe identificar intención de exclusión aunque el texto no contenga literalmente "STOP".

32. Workflow 09 — Preguntas

Si:

QUESTION

V1:

NO RESPONDER AUTOMÁTICAMENTE

Se crea:

Lead

o:

Human Review

Recomendación:

QUESTION
 ↓
Lead
 ↓
Humano

Esto evita que la IA invente información comercial.

33. Workflow 10 — No Interesado

Si:

NOT_INTERESTED

Entonces:

Actualizar CampaignRecipient

Estado:

NOT_INTERESTED

No crear Lead.

No insistir inmediatamente.

34. Workflow 11 — Respuesta Ambigua

Si:

UNCLEAR

No generar Lead automáticamente.

Registrar:

UNCLEAR

y permitir revisión manual.

35. Workflow 12 — Seguimientos

Los seguimientos serán responsabilidad humana en V1.

n8n puede enviar:

Recordatorio

al vendedor.

Ejemplo:

⏰ SEGUIMIENTO PENDIENTE

Lead:
Repuestos Oeste

Cliente interesado hace:
2 días

Acción:
Contactar.
36. Workflow 13 — Registro de Costos

Después de cada mensaje:

Message Sent
 ↓
Provider Cost
 ↓
POST /costs

Ejemplo:

{
  "type": "MESSAGING",
  "provider": "Provider",
  "referenceId": "message-id",
  "amount": 5,
  "currency": "ARS"
}
37. Costos IA

Después de cada clasificación:

AI Request
 ↓
Tokens
 ↓
Cost
 ↓
POST /costs

Esto permitirá calcular:

Costo total de IA
Costo por respuesta
Costo por Lead
38. Workflow 14 — Monitor de Errores

Se debe implementar:

Error Trigger
 ↓
Registrar Error
 ↓
Notificar

Información:

Workflow
Node
Error
Timestamp
ExecutionId
39. Notificación de Error

Ejemplo:

⚠️ ERROR HUNTER

Workflow:
Campaign Sender

Node:
Send Message

Error:
Provider Timeout

Execution:
123456
40. Workflow 15 — Kill Switch

Debe existir un workflow que consulte periódicamente:

GET /internal/system/status

Resultado:

{
  "killSwitch": true
}

Entonces:

STOP ALL

La ventaja es que no depende exclusivamente de cancelar manualmente cada workflow.

41. Estructura de Workflows

Recomendación:

n8n
│
├── 01-Campaign-Manager
│
├── 02-Message-Queue
│
├── 03-Message-Sender
│
├── 04-Message-Status
│
├── 05-Inbound-Messages
│
├── 06-AI-Classifier
│
├── 07-Lead-Creation
│
├── 08-Notifications
│
├── 09-Opt-Out
│
├── 10-Followups
│
├── 11-Cost-Tracking
│
├── 12-Error-Monitor
│
└── 13-Kill-Switch
42. Arquitectura de Comunicación
           HUNTER API
                │
       ┌────────┴────────┐
       ▼                 ▼
   PostgreSQL           n8n
                         │
          ┌──────────────┼───────────────┐
          ▼              ▼               ▼
      Messaging          IA        Notification
43. Regla de Comunicación

n8n nunca debe modificar directamente la base de datos.

Incorrecto:

n8n → PostgreSQL

Correcto:

n8n → API → PostgreSQL

Excepción posible:

Analytics

pero no para lógica comercial.

44. Variables de Entorno

n8n utilizará:

HUNTER_API_URL
HUNTER_API_KEY
AI_API_KEY
MESSAGING_API_KEY
NOTIFICATION_API_KEY

Nunca deben estar hardcodeadas.

45. Credenciales

Las credenciales deberán almacenarse mediante:

n8n Credentials

No dentro de:

Code Nodes

ni:

JSON

ni:

Variables visibles
46. Seguridad

Todos los Webhooks deben utilizar:

HTTPS

Además:

API Key
Signature Validation
Rate Limiting
IP Filtering

cuando el proveedor lo permita.

47. Backup

Debe existir backup de:

PostgreSQL
n8n Workflows
n8n Credentials
Environment Variables

Los workflows deben versionarse.

Recomendación:

Git

para:

n8n workflow JSON
48. Ambientes

Se recomienda:

Development
Staging
Production
Development
Datos de prueba
Proveedores sandbox
Staging
Pruebas integrales
Production
Datos reales
Clientes reales
Mensajes reales
49. Modo Test

Antes de lanzar una campaña:

TEST MODE

permitirá:

1 destinatario

o:

5 destinatarios

Esto permitirá verificar:

Template
Variables
Proveedor
Webhook
IA
Lead Creation

antes del envío masivo.

50. Flujo Completo V1
               PROSPECTOS
                    │
                    ▼
              HUNTER API
                    │
                    ▼
                CAMPAÑA
                    │
                    ▼
                   n8n
                    │
                    ▼
             RATE LIMIT CHECK
                    │
                    ▼
           SUPPRESSION CHECK
                    │
                    ▼
              SEND MESSAGE
                    │
                    ▼
              PROVEEDOR
                    │
                    ▼
             CLIENTE RESPONDE
                    │
                    ▼
                WEBHOOK
                    │
                    ▼
                HUNTER API
                    │
                    ▼
                    IA
                    │
       ┌────────────┼─────────────┐
       ▼            ▼             ▼
   INTERESTED    QUESTION       STOP
       │            │             │
       ▼            ▼             ▼
      LEAD        HUMANO       SUPPRESSION
       │
       ▼
   NOTIFICACIÓN
       │
       ▼
     HUMANO
       │
       ▼
    COTIZACIÓN
       │
       ▼
      VENTA
51. Criterio de Aceptación

Los workflows estarán completos cuando puedan ejecutar:

✓ Detectar campaña activa
✓ Obtener prospectos
✓ Validar destinatarios
✓ Respetar Suppression List
✓ Respetar Kill Switch
✓ Controlar velocidad
✓ Enviar mensajes
✓ Registrar mensajes
✓ Recibir respuestas
✓ Evitar duplicados
✓ Clasificar respuestas
✓ Detectar interés
✓ Crear Lead
✓ Asignar Lead
✓ Notificar humano
✓ Procesar STOP
✓ Registrar costos
✓ Manejar errores
✓ Registrar ejecuciones
52. Orden de Implementación

Para desarrollar la V1:

FASE 1
01 Campaign Manager
02 Message Queue
FASE 2
03 Message Sender
04 Message Status
FASE 3
05 Inbound Messages
06 AI Classifier
FASE 4
07 Lead Creation
08 Notifications
FASE 5
09 Opt-Out
10 Cost Tracking
FASE 6
11 Error Monitor
12 Kill Switch
53. Arquitectura Final V1
                         ┌───────────────┐
                         │    FRONTEND   │
                         │  React + TS   │
                         └───────┬───────┘
                                 │
                                 ▼
                         ┌───────────────┐
                         │   HUNTER API  │
                         │ ASP.NET Core 8│
                         └───────┬───────┘
                                 │
                    ┌────────────┴────────────┐
                    ▼                         ▼
             ┌─────────────┐            ┌───────────┐
             │ PostgreSQL  │            │    n8n    │
             └─────────────┘            └─────┬─────┘
                                               │
                        ┌──────────────────────┼──────────────────┐
                        ▼                      ▼                  ▼
                  Mensajería                  IA            Notificaciones
                        │
                        ▼
                     Cliente
                        │
                        ▼
                    Respuesta
                        │
                        ▼
                       n8n
                        │
                        ▼
                   Hunter API
                        │
                        ▼
                      Lead
                        │
                        ▼
                     Humano
                        │
                        ▼
                      Venta
54. Decisión Arquitectónica Clave

La V1 debe mantener una separación clara:

┌─────────────────────────────────────┐
│              HUNTER                 │
│                                     │
│  Datos                              │
│  Negocio                            │
│  Seguridad                          │
│  Leads                              │
│  Ventas                             │
│  Métricas                           │
└──────────────────┬──────────────────┘
                   │
                   ▼
┌─────────────────────────────────────┐
│               n8n                   │
│                                     │
│  Automatización                     │
│  Orquestación                       │
│  Integraciones                      │
│  Triggers                           │
└──────────────────┬──────────────────┘
                   │
                   ▼
┌─────────────────────────────────────┐
│           SERVICIOS EXTERNOS        │
│                                     │
│  Mensajería                         │
│  IA                                  │
│  Notificaciones                      │
└─────────────────────────────────────┘

Esto es especialmente importante porque, después de octubre, se podrá reemplazar progresivamente n8n por componentes propios si el volumen, los costos o la complejidad lo justifican, sin tener que rehacer el sistema completo.