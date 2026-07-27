📘 11 — Diseño del Frontend MVP V1

Producto: Hunter CRM AI
Versión: MVP V1
Objetivo: Crear un panel comercial simple, rápido y orientado a la acción.
Principio: El usuario debe poder pasar de prospecto → campaña → respuesta → Lead → venta con la menor cantidad de pasos posible.

1. Objetivo del Frontend

El frontend no debe intentar convertirse en un CRM empresarial completo.

En la V1 debe resolver principalmente:

┌─────────────────────┐
│ CONSEGUIR PROSPECTOS│
└──────────┬──────────┘
           ↓
┌─────────────────────┐
│ CONTACTAR           │
└──────────┬──────────┘
           ↓
┌─────────────────────┐
│ DETECTAR INTERÉS    │
└──────────┬──────────┘
           ↓
┌─────────────────────┐
│ RECIBIR LEAD        │
└──────────┬──────────┘
           ↓
┌─────────────────────┐
│ CERRAR VENTA        │
└─────────────────────┘

La interfaz debe priorizar:

Nuevos Leads.
Prospectos.
Campañas.
Seguimiento comercial.
Métricas.
2. Stack Recomendado

Para mantener coherencia con el perfil técnico del proyecto:

React
TypeScript
Vite
Tailwind CSS
React Query
Zustand
Axios
React Router

Arquitectura:

Frontend
    ↓
Axios
    ↓
ASP.NET Core API
    ↓
PostgreSQL

n8n no será consumido directamente desde el frontend.

3. Estructura Principal

El panel tendrá:

Dashboard
├── Prospectos
├── Campañas
├── Leads
├── Mensajes
└── Configuración

La navegación principal:

┌─────────────────────────────────────────┐
│ Hunter CRM AI                           │
├──────────────┬──────────────────────────┤
│              │                          │
│ Dashboard    │                          │
│ Prospectos   │      CONTENIDO           │
│ Campañas     │                          │
│ Leads        │                          │
│ Mensajes     │                          │
│              │                          │
│ Configuración│                          │
└──────────────┴──────────────────────────┘
4. Dashboard

Será la pantalla principal.

Objetivo

Responder rápidamente:

¿Qué está pasando con mi prospección?

KPIs principales
┌──────────────┐ ┌──────────────┐
│ Prospectos   │ │ Contactados  │
│ 5.000        │ │ 1.200        │
└──────────────┘ └──────────────┘

┌──────────────┐ ┌──────────────┐
│ Respuestas   │ │ Interesados  │
│ 150          │ │ 45           │
└──────────────┘ └──────────────┘

┌──────────────┐ ┌──────────────┐
│ Leads        │ │ Ventas       │
│ 45           │ │ 12           │
└──────────────┘ └──────────────┘
Embudo
Prospectos
   5.000
     ↓
Contactados
   1.200
     ↓
Respuestas
    150
     ↓
Interesados
     45
     ↓
Leads
     45
     ↓
Ventas
     12
Actividad reciente

Mostrar:

Últimos Leads.
Últimas respuestas.
Campañas activas.
Errores de envío.

Ejemplo:

🟢 Nuevo Lead
Repuestos López
"Sí, pasame información"

🟢 Nuevo Lead
Distribuidora Norte
"¿Qué productos manejan?"

🔴 Error
Campaña Distribuidores Zona Oeste
12 mensajes fallidos
5. Pantalla Prospectos
Objetivo

Gestionar la base de prospectos.

Tabla:

┌────────────────┬────────────┬────────────┬──────────┐
│ Empresa        │ Tipo       │ Ubicación  │ Estado   │
├────────────────┼────────────┼────────────┼──────────┤
│ Repuestos A    │ Repuestos  │ Moreno     │ Ready    │
│ Distribuidora B│ Distrib.   │ Merlo      │ Contact. │
│ Taller C       │ Taller     │ Ituzaingó  │ Interest │
└────────────────┴────────────┴────────────┴──────────┘
Filtros
Buscar
Tipo
Ciudad
Provincia
Estado
Fuente
Tag
Score
Acciones
+ Nuevo Prospecto
Importar
Exportar
Agregar a campaña
6. Detalle del Prospecto

Al seleccionar un prospecto:

Repuestos López

Tipo:
Casa de repuestos

Ubicación:
Moreno, Buenos Aires

Contacto:
WhatsApp
+54...

Fuente:
Google Places

Estado:
INTERESTED
Información comercial
Campañas
Mensajes
Interacciones
Leads
Actividades
Timeline
10:00
Prospecto agregado

10:05
Agregado a campaña

10:10
Mensaje enviado

10:15
Respuesta recibida

10:15
Interés detectado

10:15
Lead creado
7. Pantalla Campañas
Objetivo

Controlar las campañas de prospección.

Vista:

Campañas

┌────────────────────────┬───────────┬──────────┐
│ Nombre                 │ Estado    │ Enviados │
├────────────────────────┼───────────┼──────────┤
│ Distribuidores Oeste   │ Running   │ 350      │
│ Casas de Repuestos     │ Paused    │ 120      │
│ Talleres               │ Completed │ 1.000    │
└────────────────────────┴───────────┴──────────┘
Crear campaña

Flujo:

1. Nombre
        ↓
2. Segmentación
        ↓
3. Seleccionar prospectos
        ↓
4. Seleccionar plantilla
        ↓
5. Revisar
        ↓
6. Iniciar
8. Detalle de Campaña

Mostrar:

Campaña:
Distribuidores Zona Oeste

Estado:
RUNNING

Prospectos:
1.000

Enviados:
700

Respuestas:
80

Interesados:
25

Leads:
25
Acciones
Pausar
Reanudar
Cancelar
Ver prospectos
Ver métricas
9. Pantalla Leads

Esta será una de las pantallas más importantes.

El objetivo es que el vendedor pueda identificar rápidamente a quién debe contactar ahora.

Vista tipo Kanban
┌────────────┐ ┌────────────┐ ┌────────────┐
│ NUEVO      │ │ EN PROCESO │ │ GANADO     │
├────────────┤ ├────────────┤ ├────────────┤
│ Empresa A  │ │ Empresa C  │ │ Empresa E  │
│ Empresa B  │ │ Empresa D  │ │ Empresa F  │
└────────────┘ └────────────┘ └────────────┘

Estados:

NEW
IN_PROGRESS
WON
LOST
10. Detalle del Lead

Al abrir un Lead:

┌─────────────────────────────────────────┐
│ Repuestos López                         │
│                                         │
│ 🟢 Lead nuevo                           │
│                                         │
│ WhatsApp                                │
│ +54 9 11...                             │
│                                         │
│ Campaña                                  │
│ Distribuidores Zona Oeste               │
│                                         │
│ Interés detectado: 96%                  │
└─────────────────────────────────────────┘
Acciones principales

El CTA principal debe ser:

ABRIR WHATSAPP

Luego:

Marcar en proceso
Enviar cotización
Agregar nota
Marcar ganado
Marcar perdido
11. Principio de Human Handoff

Cuando un Lead llegue:

🟢 NUEVO LEAD

Repuestos López

"Sí, pasame información."

El vendedor debe poder:

[ ABRIR WHATSAPP ]

con un solo clic.

La V1 no necesita construir un chat completo dentro del CRM.

El cierre se realizará en el canal habitual del vendedor.

12. Pantalla Mensajes

La V1 mostrará el historial.

Prospecto
    ↓
Conversación
    ↓
Mensajes

Ejemplo:

BOT

Hola, ¿cómo va?
Estamos ampliando nuestra red comercial.
¿Trabajan con distribución?


PROSPECTO

Sí, pasame información.


SISTEMA

🟢 INTERESTED
Confidence: 96%

La conversación será principalmente de consulta.

El cierre se realizará fuera del sistema.

13. Configuración
Organización
Nombre
Email
Teléfono
Zona horaria
Usuarios
Nombre
Email
Rol
Estado
Plantillas
Nombre
Canal
Mensaje
Estado
Integraciones
Proveedor de mensajes
IA
Fuentes de prospectos
n8n
14. Notificaciones

El MVP debe notificar eventos importantes:

🟢 Nuevo Lead
🔴 Error de campaña
⚠️ Campaña pausada
🟡 Respuesta sin clasificar

La primera versión puede utilizar:

Toast
Badge
Notificación dentro del Dashboard

Posteriormente:

Email
WhatsApp interno
Telegram
Push
15. Mobile Responsive

Aunque el sistema será principalmente desktop, el panel de Leads debe funcionar correctamente en móvil.

Prioridad:

Desktop
⭐⭐⭐⭐⭐

Tablet
⭐⭐⭐⭐

Mobile
⭐⭐⭐⭐

El vendedor debería poder:

Recibir Lead
    ↓
Abrir panel
    ↓
Ver información
    ↓
Abrir WhatsApp

desde su teléfono.

16. Componentes Reutilizables

Crear componentes:

Button
Input
Select
Modal
Drawer
Table
Pagination
Badge
StatusBadge
Card
KpiCard
Toast
EmptyState
LoadingState
ConfirmDialog

Componentes comerciales:

ProspectCard
LeadCard
CampaignCard
Timeline
MessageBubble
PipelineColumn
17. Estado Global

Zustand:

authStore
organizationStore
uiStore

React Query:

prospects
campaigns
leads
messages
dashboard

La información del servidor no debe mantenerse innecesariamente en Zustand.

18. Estructura de Rutas
/login

/register

/app
    /dashboard

    /prospects
    /prospects/:id

    /campaigns
    /campaigns/new
    /campaigns/:id

    /leads
    /leads/:id

    /messages

    /settings
        /organization
        /users
        /templates
        /integrations
19. Flujo Principal del Usuario
LOGIN
  ↓
DASHBOARD
  ↓
Ver nuevo Lead
  ↓
Abrir Lead
  ↓
Leer mensaje
  ↓
Abrir WhatsApp
  ↓
Hablar con cliente
  ↓
Cotizar
  ↓
Cerrar venta
  ↓
Volver a Hunter
  ↓
Marcar WON

Este flujo debe poder completarse en segundos.

20. MVP Frontend

La V1 debe incluir obligatoriamente:

✅ Login
✅ Dashboard
✅ Prospectos
✅ Detalle Prospecto
✅ Campañas
✅ Detalle Campaña
✅ Leads
✅ Kanban Leads
✅ Detalle Lead
✅ Timeline
✅ Mensajes
✅ Configuración básica

Puede quedar fuera:

❌ Chat omnicanal
❌ CRM avanzado
❌ Automatizaciones visuales
❌ Constructor de workflows
❌ Analytics avanzado
❌ App móvil nativa
❌ IA conversacional avanzada
21. Pantalla Más Importante

La pantalla de mayor prioridad será:

LEADS

La segunda:

DASHBOARD

La tercera:

PROSPECTOS

El motivo es que el objetivo del sistema no es administrar datos.

El objetivo es:

Convertir prospectos en oportunidades y oportunidades en ventas.

Por eso la experiencia del vendedor debe estar centrada en el Lead.

22. Arquitectura UX Final
              DASHBOARD
                  │
        ┌─────────┴─────────┐
        │                   │
        ▼                   ▼
    PROSPECTOS           CAMPAÑAS
        │                   │
        └─────────┬─────────┘
                  ▼
              MENSAJES
                  │
                  ▼
              RESPUESTAS
                  │
                  ▼
                LEADS
                  │
                  ▼
               HUMANO
                  │
                  ▼
                VENTA
23. Criterio de Aceptación

El frontend V1 será considerado terminado cuando un usuario pueda:

1. Iniciar sesión.

2. Ver el estado general del sistema.

3. Consultar prospectos.

4. Crear una campaña.

5. Ver campañas activas.

6. Ver nuevos Leads.

7. Abrir un Lead.

8. Entender por qué fue creado.

9. Acceder al contacto.

10. Abrir WhatsApp.

11. Gestionar la oportunidad.

12. Marcar la venta como ganada o perdida.