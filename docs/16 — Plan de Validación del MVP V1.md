📘 16 — Plan de Validación del MVP V1

Producto: Hunter CRM AI
Empresa objetivo inicial: Difrani
Versión: MVP V1
Horizonte: Hasta octubre de 2026
Objetivo: Validar que el sistema puede generar oportunidades comerciales y ventas reales mediante prospección automatizada, manteniendo control sobre calidad, costos y riesgos.

1. Objetivo del MVP

La V1 debe responder una pregunta:

¿Podemos utilizar automatización para conseguir nuevos clientes y generar ventas reales para Difrani de forma repetible y rentable?

No buscamos validar solamente:

Cantidad de mensajes

Sino:

Prospectos
    ↓
Contactados
    ↓
Respuestas
    ↓
Interesados
    ↓
Leads
    ↓
Ventas
    ↓
Margen
2. Hipótesis Principal

La hipótesis inicial será:

Si Difrani puede contactar de manera automatizada una cantidad significativa de potenciales compradores, detectar automáticamente quién demuestra interés y derivar esos contactos a un vendedor humano, entonces podrá aumentar la cantidad de oportunidades comerciales sin aumentar proporcionalmente la carga operativa del equipo.

3. Hipótesis Secundarias

Se validará si:

H1 — La prospección genera respuestas
Prospectos contactados
        ↓
Respuestas
H2 — El mensaje genera interés
Respuestas
        ↓
Interesados
H3 — El interés se convierte en oportunidades
Interesados
        ↓
Leads
H4 — El humano puede cerrar las oportunidades
Leads
        ↓
Ventas
H5 — El modelo es rentable
Margen generado
        >
Costo de adquisición
4. Hipótesis Operativa

El sistema debe permitir que:

BOT
1000 prospectos
        ↓
Detecta interesados
        ↓
HUMANO
Atiende únicamente oportunidades

En lugar de:

HUMANO
Buscar 1000 prospectos
        ↓
Contactar 1000
        ↓
Leer 1000 respuestas
        ↓
Detectar interesados

El MVP debe demostrar que esta reducción de trabajo repetitivo tiene valor económico.

5. Fase 0 — Preparación

Antes de iniciar cualquier campaña:

[ ] API funcionando
[ ] n8n configurado
[ ] Base de datos funcionando
[ ] Prospectos importados
[ ] Deduplicación funcionando
[ ] Suppression List funcionando
[ ] Opt-out funcionando
[ ] Campañas funcionando
[ ] Mensajes configurados
[ ] Clasificación IA funcionando
[ ] Creación de Leads funcionando
[ ] Notificaciones funcionando
[ ] Dashboard funcionando
[ ] Kill Switch funcionando

No comenzar la validación comercial si los puntos críticos no están funcionando.

6. Fase 1 — Prueba Técnica
Objetivo

Validar el funcionamiento completo del flujo.

Cantidad:

10-20 prospectos

No se busca obtener conclusiones comerciales.

Se busca comprobar:

Prospecto
↓
Mensaje
↓
Respuesta
↓
IA
↓
Interés
↓
Lead
↓
Humano
7. Criterios de Éxito Fase 1

Debe cumplirse:

100%
de mensajes correctamente procesados

Y:

0
duplicaciones críticas

Además:

0
opt-outs ignorados

La clasificación debe poder revisarse manualmente.

8. Fase 2 — Validación Comercial Inicial

Cantidad:

50-100 prospectos

Objetivo:

Validar mensaje
Validar segmento
Validar respuesta

Se recomienda utilizar:

1 segmento
1 zona
1 mensaje principal

Esto evita mezclar demasiadas variables.

9. Selección del Primer Segmento

La primera prueba debe enfocarse en un segmento comercial relevante para Difrani.

Ejemplo:

Casas de Repuestos

o:

Distribuidores

La selección definitiva debe depender de la disponibilidad real de prospectos y de la capacidad comercial de Difrani para atenderlos.

10. Fase 3 — Validación de Conversión

Cantidad:

100-250 prospectos

Objetivo:

Medir
↓
Respuesta
↓
Interés
↓
Lead
↓
Venta

Aquí comienza la validación comercial real.

Se deben registrar:

Prospectos
Mensajes
Respuestas
Interesados
Leads
Cotizaciones
Ventas
Ingresos
11. Fase 4 — Escalamiento Controlado

Cantidad:

250-500 prospectos

Objetivo:

Demostrar repetibilidad

Se podrá probar:

Segmento A
vs
Segmento B

o:

Mensaje A
vs
Mensaje B

Pero no cambiar simultáneamente:

Segmento
Zona
Mensaje
Canal

porque dificultaría saber qué causó el resultado.

12. Fase 5 — Prueba de Volumen

Cantidad:

500-1000 prospectos

Objetivo:

Validar capacidad operativa

Se analizará:

Carga del sistema
Carga de n8n
Capacidad de la API
Capacidad humana
Cantidad de Leads
Costo
Conversión

El límite real no será únicamente técnico.

También será comercial.

Si:

1000 mensajes
↓
100 Leads

el equipo debe poder atender esos 100 Leads.

13. Capacidad Humana

Antes de aumentar volumen:

Leads generados
≤
Capacidad de atención

Si:

1000 contactos
↓
200 Leads

pero el equipo solo puede atender:

50 Leads

no se debe aumentar el volumen.

La automatización no debe generar más oportunidades de las que el equipo puede gestionar correctamente.

14. Escalamiento Progresivo

El esquema recomendado:

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

Cada etapa debe mantenerse durante un período suficiente para obtener datos.

No avanzar automáticamente únicamente porque:

"El sistema funciona técnicamente."

Debe funcionar comercialmente.

15. Regla de Escalamiento

Se puede avanzar si:

Sistema estable
+
Opt-Out controlado
+
Sin errores críticos
+
Leads atendibles
+
Conversión aceptable
+
Costo controlado
16. Regla de Pausa

Pausar la campaña si aparece:

Aumento significativo de errores

o:

Aumento de bloqueos

o:

Aumento de opt-outs

o:

Costos inesperados

o:

Problemas con el proveedor

o:

Equipo humano saturado
17. Regla de Abandono

Una campaña puede considerarse fallida si después de iteraciones razonables:

No genera respuestas

o:

No genera Leads

o:

Genera Leads pero ninguna venta

o:

El costo por venta supera el margen disponible

En ese caso:

NO aumentar volumen.

Primero:

Analizar
↓
Modificar
↓
Volver a probar
18. Variables a Testear

Las principales variables serán:

Segmento
Zona
Mensaje
Propuesta de valor
Horario
Día
19. Test A/B de Mensajes

Ejemplo:

Mensaje A
Hola, ¿cómo va?

Soy [NOMBRE] de Difrani.

Estamos contactando comercios del rubro automotor porque trabajamos con distribución de repuestos.

¿Actualmente compran repuestos para reventa?
Mensaje B
Hola, ¿cómo va?

Te contacto desde Difrani.

Estamos incorporando nuevos clientes comerciales y quería saber si trabajan con repuestos automotores.

Si te interesa, te paso información.

Se medirán:

Respuesta
Interés
Leads
Ventas

No únicamente respuestas.

20. Métrica de Ganador

El mejor mensaje será el que genere:

Mayor cantidad de ventas

considerando:

Costo
+
Volumen
+
Calidad

No necesariamente el que genere más respuestas.

21. Prueba de Segmentos

Ejemplo:

Campaña A
Casas de Repuestos

Campaña B
Distribuidores

Comparar:

Tasa de respuesta
Tasa de interés
Leads
Ventas
Ticket
Costo por venta
22. Prueba Geográfica

Ejemplo:

Zona Oeste

contra:

CABA

contra:

Interior

La distancia no debe determinar automáticamente si un prospecto es bueno o malo.

Debe medirse:

Conversión
+
Ticket
+
Costo logístico
+
Recurrencia
23. Modelo de Evaluación

Cada campaña deberá tener:

Campaign Score

Propuesta inicial:

Ventas
40%

Conversión
25%

Costo por venta
20%

Calidad de Leads
10%

Opt-Out / Riesgo
5%

Este score puede modificarse posteriormente.

24. Objetivos de Validación

La V1 debe responder:

Pregunta 1

¿Podemos conseguir prospectos?

SÍ / NO
Pregunta 2

¿Podemos contactarlos?

SÍ / NO
Pregunta 3

¿Responden?

SÍ / NO
Pregunta 4

¿Podemos detectar automáticamente interés?

SÍ / NO
Pregunta 5

¿El humano puede cerrar?

SÍ / NO
Pregunta 6

¿Es rentable?

SÍ / NO
25. Experimento Base

El primer experimento comercial será:

CAMPAÑA #001

Empresa:
Difrani

Segmento:
[Definir]

Zona:
[Definir]

Prospectos:
100

Mensaje:
Versión A

Canal:
[Definir]

Objetivo:
Generar Leads
26. Registro del Experimento

Cada experimento debe registrar:

ExperimentId
CampaignId
Segment
Zone
MessageVersion
StartDate
EndDate
Prospects
Messages
Responses
Interested
Leads
Quotes
Sales
Revenue
Cost
27. Ejemplo de Resultado
CAMPAÑA #001

Prospectos:
1000

Mensajes:
1000

Respuestas:
80

Interesados:
30

Leads:
30

Cotizaciones:
20

Ventas:
8

Ingresos:
$2.000.000

Costo:
$100.000

Resultado:

Costo por venta:
$12.500

Conversión Lead → Venta:
26,6%

Conversión Prospecto → Venta:
0,8%
28. Análisis del Resultado

No concluir inmediatamente:

"0,8% es bueno."

Hay que analizar:

¿Cuánto margen dejaron las 8 ventas?

Ejemplo:

Ingresos:
$2.000.000

Margen:
$500.000

Costo:
$100.000

Resultado:

Margen generado:
$500.000

Costo adquisición:
$100.000

Campaña potencialmente rentable.

29. Prueba de Repetibilidad

Una sola campaña no valida el modelo.

Se recomienda:

Campaña 1
↓
Campaña 2
↓
Campaña 3

Si los resultados se mantienen:

Modelo repetible

Si los resultados varían mucho:

Investigar
30. Validación de Capacidad

El sistema debe probar:

100 prospectos
250 prospectos
500 prospectos
1000 prospectos

Y medir:

Tiempo de procesamiento
Errores
Fallos
Costos
31. Validación del Equipo Comercial

También se debe medir:

Leads recibidos
Leads atendidos
Tiempo promedio de respuesta
Ventas

Una campaña puede ser técnicamente exitosa pero comercialmente inviable si:

Lead → humano

tarda demasiado.

32. Objetivo de Tiempo de Respuesta

Objetivo inicial:

< 5 minutos

Ideal:

< 2 minutos

El Dashboard debe mostrar:

🔴 Leads sin atender
33. Costo de Validación

Durante la V1, el objetivo no será maximizar rentabilidad inmediatamente.

Será:

Aprender

Por eso se debe aceptar un costo inicial de experimentación.

Pero debe existir un límite:

Presupuesto máximo de prueba

Ejemplo:

$X

Una vez alcanzado:

Pausar
↓
Analizar
↓
Decidir
34. Registro de Aprendizajes

Cada campaña debe finalizar con:

¿Qué funcionó?
¿Qué no funcionó?
¿Qué segmento respondió?
¿Qué mensaje funcionó?
¿Qué objeciones aparecieron?
¿Qué productos interesaron?
¿Por qué se perdieron ventas?

Esto debe convertirse en conocimiento reutilizable.

35. Biblioteca de Mensajes

El sistema debería conservar:

Mensaje A
Mensaje B
Mensaje C

Con sus resultados:

Respuesta
Interés
Lead
Venta

Con el tiempo:

Mensaje ganador

podrá convertirse en plantilla oficial.

36. Biblioteca de Objeciones

Registrar respuestas como:

"Ya tengo proveedor"
"Mandame lista de precios"
"No compro por WhatsApp"
"¿Qué marcas trabajan?"
"¿Hacen envíos?"

Estas respuestas podrán alimentar futuras automatizaciones.

37. Etapa de Cierre de V1

Antes de octubre se debe poder responder:

¿Cuántos prospectos podemos generar?
¿Cuántos responden?
¿Cuántos se convierten en Leads?
¿Cuántos compran?
¿Cuánto cuesta conseguir una venta?
¿Cuánto margen genera?
¿Podemos escalar?
38. Criterio para Pasar a V2

La V2 debe comenzar cuando exista evidencia de:

✅ Prospectos suficientes
✅ Flujo estable
✅ Leads reales
✅ Ventas reales
✅ Costos conocidos
✅ Conversión conocida
✅ Proceso humano validado

Si la V1 no genera ventas:

NO escalar tecnología.

Primero:

Revisar oferta
Revisar segmento
Revisar mensaje
Revisar canal
39. Decisión Post-Octubre

Al llegar octubre de 2026 se deben considerar tres escenarios.

Escenario A — Funciona
Ventas
+
Rentabilidad
+
Escalabilidad

Acción:

→ Pasar a V2
→ Optimizar costos
→ Implementar arquitectura completa
→ Escalar
Escenario B — Genera Leads pero no ventas
Prospección funciona
Cierre no funciona

Acción:

→ Optimizar proceso comercial
→ Mejorar seguimiento
→ Mejorar oferta
Escenario C — No genera interés
Baja respuesta
+
Bajo interés

Acción:

→ Revisar segmento
→ Revisar propuesta
→ Revisar mensaje
→ Revisar canal
40. Resultado Final Esperado

Al finalizar la V1 deberíamos tener algo similar a:

                    HUNTER V1
                        │
                        ▼
              DATOS DE PROSPECTOS
                        │
                        ▼
                  PROSPECCIÓN
                        │
                        ▼
                   MENSAJES
                        │
                        ▼
                 IA CLASIFICA
                        │
                        ▼
                      LEAD
                        │
                        ▼
                    HUMANO
                        │
                        ▼
                     VENTA
                        │
                        ▼
                  MÉTRICAS
                        │
                        ▼
                 DECISIÓN V2
41. Criterio Final de Validación

El MVP V1 será exitoso si demuestra:

Hunter puede convertir automatización de prospección en oportunidades comerciales reales y ventas medibles, con una intervención humana mínima pero suficiente para cerrar el negocio.

El éxito no se define por:

1000 mensajes enviados

sino por:

1000 mensajes
        ↓
X respuestas
        ↓
X Leads
        ↓
X ventas
        ↓
$X margen
        ↓
$X costo
        ↓
Rentabilidad positiva