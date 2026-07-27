📘 15 — KPIs y Modelo de Medición V1

Producto: Hunter CRM AI
Versión: MVP V1
Objetivo: Medir el rendimiento real del sistema desde la captación del prospecto hasta la venta cerrada, permitiendo determinar si la automatización genera un retorno económico positivo.

1. Objetivo del sistema de métricas

Hunter no debe medir únicamente:

¿Cuántos mensajes enviamos?

Debe responder:

¿Cuántos prospectos conseguimos?
¿Cuántos pudimos contactar?
¿Cuántos respondieron?
¿Cuántos mostraron interés?
¿Cuántos se convirtieron en Leads?
¿Cuántos compraron?
¿Cuánto costó conseguir esas ventas?
¿Cuánto dinero generaron?

El modelo completo será:

PROSPECCIÓN
      ↓
CONTACTO
      ↓
RESPUESTA
      ↓
INTERÉS
      ↓
LEAD
      ↓
COTIZACIÓN
      ↓
VENTA
      ↓
INGRESOS
      ↓
RENTABILIDAD
2. Principio de medición

Cada etapa debe poder relacionarse con la siguiente.

Prospecto
    ↓
CampaignRecipient
    ↓
Message
    ↓
Response
    ↓
Lead
    ↓
Quote
    ↓
Sale

Esto permitirá calcular la conversión de cada etapa.

3. Embudo Comercial

El Dashboard principal deberá representar:

┌─────────────────────────────┐
│ PROSPECTOS                  │
│ 10.000                      │
└──────────────┬──────────────┘
               ↓
┌─────────────────────────────┐
│ CONTACTADOS                 │
│ 8.000                       │
└──────────────┬──────────────┘
               ↓
┌─────────────────────────────┐
│ RESPUESTAS                  │
│ 800                         │
└──────────────┬──────────────┘
               ↓
┌─────────────────────────────┐
│ INTERESADOS                 │
│ 200                         │
└──────────────┬──────────────┘
               ↓
┌─────────────────────────────┐
│ LEADS                       │
│ 200                         │
└──────────────┬──────────────┘
               ↓
┌─────────────────────────────┐
│ COTIZACIONES                │
│ 150                         │
└──────────────┬──────────────┘
               ↓
┌─────────────────────────────┐
│ VENTAS                      │
│ 50                          │
└─────────────────────────────┘
4. KPIs de Prospección
4.1 Prospectos encontrados

Cantidad total de prospectos obtenidos.

Prospectos Encontrados

Ejemplo:

5.000
4.2 Prospectos válidos

Prospectos que superaron las validaciones.

Prospectos Válidos

Fórmula:

Prospectos encontrados
-
Inválidos
-
Duplicados
4.3 Tasa de validación
Prospectos válidos
/
Prospectos encontrados
× 100

Ejemplo:

4.000 / 5.000 × 100
= 80%
4.4 Prospectos contactables

Cantidad que posee un canal válido para contacto.

Prospectos Contactables
5. KPIs de Contacto
5.1 Mensajes enviados

Cantidad total de mensajes enviados.

Messages Sent
5.2 Mensajes entregados

Cantidad confirmada como entregada por el proveedor.

Messages Delivered
5.3 Mensajes fallidos
Messages Failed
5.4 Tasa de entrega
Mensajes entregados
/
Mensajes enviados
× 100
5.5 Tasa de error
Mensajes fallidos
/
Mensajes enviados
× 100
6. KPIs de Engagement
6.1 Respuestas

Cantidad de prospectos que respondieron.

Responses
6.2 Tasa de respuesta
Respuestas
/
Mensajes entregados
× 100

Ejemplo:

200 respuestas
/
5.000 entregados
× 100

= 4%
6.3 Interesados

Cantidad de prospectos clasificados como:

INTERESTED
6.4 Tasa de interés
Interesados
/
Respuestas
× 100
7. KPIs de Leads
7.1 Leads generados

Cantidad de Leads creados automáticamente.

Total Leads
7.2 Tasa de conversión a Lead
Leads
/
Mensajes entregados
× 100

También:

Leads
/
Respuestas
× 100

Ambas métricas deben existir.

8. KPIs de Gestión Humana
8.1 Tiempo de primera respuesta

Tiempo desde:

Lead creado

hasta:

Primer contacto humano

Ejemplo:

Lead creado: 10:00
Humano responde: 10:04

Tiempo:
4 minutos
8.2 Tiempo promedio de respuesta
Σ Tiempo de respuesta
/
Cantidad de Leads atendidos
8.3 Leads atendidos
Leads con contacto humano
8.4 Leads sin atender
Leads creados
-
Leads atendidos

Este KPI debe aparecer destacado.

9. KPIs de Cotización
9.1 Cotizaciones enviadas
Total Quotes
9.2 Tasa de cotización
Cotizaciones
/
Leads
× 100
9.3 Tiempo hasta cotización

Tiempo entre:

Lead creado

y:

Cotización enviada
10. KPIs de Ventas
10.1 Ventas cerradas
Total Sales
10.2 Tasa de cierre
Ventas
/
Leads
× 100
10.3 Conversión de cotización
Ventas
/
Cotizaciones
× 100
10.4 Conversión total
Ventas
/
Prospectos contactados
× 100

Esta será una de las métricas más importantes.

11. Ingresos

Cada venta debe registrar:

SaleAmount

El Dashboard debe mostrar:

Ingresos Totales

Ejemplo:

$10.500.000 ARS
12. Ticket Promedio
Ingresos
/
Cantidad de ventas

Ejemplo:

$10.000.000
/
50 ventas

= $200.000
13. Ventas por Segmento

Medir:

Distribuidores
Casas de Repuestos
Talleres
Revendedores
Otros

Ejemplo:

Distribuidores
Ventas: 20

Casas de Repuestos
Ventas: 15

Talleres
Ventas: 10

Otros
Ventas: 5

Esto permitirá detectar dónde funciona mejor la estrategia.

14. Ventas por Ubicación

Medir:

Provincia
Ciudad
Zona

Ejemplo:

Moreno
15 ventas

Merlo
12 ventas

Ituzaingó
10 ventas

Morón
8 ventas
15. Ventas por Campaña

Cada venta debe poder atribuirse a:

CampaignId

Ejemplo:

Campaña A
1000 contactos
50 Leads
15 ventas

Campaña B
1000 contactos
30 Leads
5 ventas

Resultado:

Campaña A > Campaña B
16. Costos

Los costos deben dividirse.

COSTOS FIJOS
    ↓
Hosting
Infraestructura

COSTOS VARIABLES
    ↓
Mensajería
IA
Prospección

COSTOS OPERATIVOS
    ↓
Tiempo humano
17. Costo de Mensajería

Cada mensaje debe guardar:

Provider
MessageType
Cost
Currency

Ejemplo:

Mensaje
Costo:
$X

Esto permite calcular:

Costo total de mensajería
18. Costo de IA

Registrar:

Modelo
Tokens Input
Tokens Output
Costo

Esto permite:

Costo total IA
19. Costo de Prospección

Si la fuente tiene costo:

Costo de fuente

Ejemplo:

Google Places

Se debe registrar:

API Cost
20. Costo por Prospecto
Costo total de prospección
/
Prospectos obtenidos
21. Costo por Contacto
Costo total de campañas
/
Mensajes enviados
22. Costo por Lead
Costo total de campaña
/
Leads generados

Ejemplo:

$100.000
/
100 Leads

= $1.000 por Lead
23. Costo por Venta

Esta será la métrica económica más importante.

Costo total
/
Ventas cerradas

Ejemplo:

$100.000
/
10 ventas

= $10.000 por venta
24. CAC

El Customer Acquisition Cost será:

CAC
=
Costo total de adquisición
/
Nuevos clientes

Debe diferenciarse:

CAC Lead

de:

CAC Cliente
25. ROAS

Si se considera únicamente inversión publicitaria:

ROAS
=
Ingresos atribuidos
/
Inversión publicitaria

En Hunter será más útil medir:

Revenue / Total Acquisition Cost
26. ROI
ROI
=
(Valor generado - Costo)
/
Costo
× 100

Ejemplo:

Ingresos:
$1.000.000

Costo:
$200.000

ROI:

(1.000.000 - 200.000)
/
200.000
× 100

= 400%
27. Rentabilidad

La métrica más importante no es necesariamente facturación.

Se debe buscar:

Margen generado
-
Costo de adquisición

Idealmente:

Margen bruto
>
Costo de adquisición
28. Métricas de Calidad

Además del volumen, medir:

Tasa de interés
Tasa de Lead
Tasa de cierre
Ticket promedio

Una campaña con:

10.000 mensajes

puede ser peor que:

1.000 mensajes

si la segunda genera más ventas rentables.

29. Métrica de Opt-Out

Registrar:

Opt-Out Rate

Fórmula:

Opt-Out
/
Mensajes entregados
× 100

Esta métrica debe observarse especialmente.

Un aumento puede indicar:

Mensaje poco relevante
Segmentación incorrecta
Frecuencia excesiva
Mala calidad de prospectos
30. Métrica de Bloqueos

Registrar:

Block Rate

Fórmula:

Bloqueos
/
Mensajes entregados
× 100

Un aumento debe generar una alerta.

31. Métrica de Calidad de Campaña

Podemos crear:

Campaign Quality Score

Basado en:

Respuesta
+
Interés
+
Conversión
-
Opt-Out
-
Errores

En V1 puede ser una métrica experimental.

32. Ranking de Campañas

El sistema podrá ordenar campañas por:

Ventas
Conversión
Costo por venta
ROI

Ejemplo:

1. Distribuidores Oeste
2. Casas de Repuestos
3. Talleres
4. Gomerías
33. Ranking de Segmentos

Ejemplo:

Segmento              Ventas    Conversión

Distribuidores           20        5%
Casas Repuestos          15        4%
Talleres                 10        2%
Gomerías                  5        1%
34. Métrica de Recurrencia

Una vez que el sistema tenga historial:

Cliente
   ↓
Compra 1
   ↓
Compra 2
   ↓
Compra 3

Se medirá:

Repeat Customer Rate

Fórmula:

Clientes con >1 compra
/
Clientes totales
× 100

Esta métrica será especialmente importante para Difrani.

35. Valor del Cliente

Posteriormente se podrá calcular:

Customer Lifetime Value

Pero en V1 será una métrica inicial.

Se puede estimar:

Ticket promedio
×
Cantidad promedio de compras
36. Métricas del Vendedor

Por vendedor:

Leads recibidos
Leads atendidos
Tiempo respuesta
Cotizaciones
Ventas
Ingresos
Ticket promedio
Conversión

Ejemplo:

Vendedor A
50 Leads
20 ventas
40% cierre

Vendedor B
50 Leads
10 ventas
20% cierre

Esto permite detectar oportunidades de capacitación.

37. Dashboard Ejecutivo

El Dashboard deberá mostrar:

┌────────────────────────────────────────┐
│ PROSPECTOS        10.000               │
│ CONTACTADOS        8.000               │
│ RESPUESTAS           800               │
│ LEADS                 200              │
│ VENTAS                 50              │
│ INGRESOS        $10.000.000            │
└────────────────────────────────────────┘

Debajo:

Conversión
Costo por Lead
Costo por Venta
Ticket Promedio
ROI
38. Dashboard Comercial

Debe priorizar:

Nuevos Leads
Leads sin atender
Seguimientos
Ventas
39. Dashboard de Campañas

Debe mostrar:

Campaña
Prospectos
Enviados
Entregados
Respuestas
Interesados
Leads
Ventas
Costo
ROI
40. Dashboard de Costos

Debe mostrar:

Mensajería
IA
Prospección
Infraestructura
Total

Y:

Costo / Prospecto
Costo / Mensaje
Costo / Lead
Costo / Venta
41. Modelo de Atribución

La V1 utilizará:

First Campaign Attribution

El Lead conservará:

FirstCampaignId

La venta quedará asociada al Lead.

Ejemplo:

Campaña
    ↓
Prospecto
    ↓
Lead
    ↓
Venta

Esto permite atribuir la venta.

42. Problema del Cierre Fuera del Sistema

Si el humano cierra la venta por WhatsApp o teléfono, Hunter no podrá detectarla automáticamente.

Por eso será obligatorio:

Lead
 ↓
Marcar WON
 ↓
Ingresar monto

Esto debe ser extremadamente simple.

43. Datos Mínimos de una Venta
LeadId
CampaignId
SellerId
Amount
Currency
Date

Opcional:

Margin
ProductCategory
44. Métricas Mínimas V1

Obligatorias:

Prospectos encontrados
Prospectos válidos
Mensajes enviados
Mensajes entregados
Respuestas
Interesados
Leads
Cotizaciones
Ventas
Ingresos
Costo mensajería
Costo IA
Costo prospectos
Costo por Lead
Costo por Venta
Conversión
45. Métricas V2

Quedan para después:

CLV avanzado
Scoring predictivo
Lead Quality Score avanzado
Predicción de compra
Recurrencia predictiva
Optimización automática
Attribution Multi-Touch
46. Métrica Principal del Proyecto

La métrica North Star de Hunter será:

Ventas incrementales generadas por cada unidad de costo de prospección.

Una forma práctica de medirla:

Ingresos atribuibles
/
Costo total de adquisición

Pero para la toma de decisiones comerciales será aún más importante:

Margen generado
/
Costo de adquisición
47. Objetivo de Rentabilidad

La lógica será:

COSTO
   ↓
MENSAJES
   ↓
LEADS
   ↓
VENTAS
   ↓
MARGEN

El sistema debe determinar:

¿Cada $1 invertido genera más de $1 de margen?

Si:

Margen / Costo > 1

la campaña tiene potencial de ser rentable.

48. Modelo de Medición Completo
                    PROSPECTOS
                         │
                         ▼
                    CONTACTADOS
                         │
                         ▼
                      ENTREGA
                         │
                         ▼
                     RESPUESTA
                         │
                         ▼
                      INTERÉS
                         │
                         ▼
                        LEAD
                         │
                         ▼
                     COTIZACIÓN
                         │
                         ▼
                       VENTA
                         │
                         ▼
                       MARGEN
                         │
                         ▼
                  RENTABILIDAD REAL
49. Criterio de Éxito

Hunter V1 será considerado comercialmente validado si demuestra que:

1. Puede generar prospectos constantemente.
2. Puede contactar prospectos de forma controlada.
3. Puede detectar interés.
4. Puede generar Leads.
5. Los humanos pueden cerrar ventas.
6. Las ventas pueden atribuirse a campañas.
7. Los costos pueden medirse.
8. El costo por venta es conocido.
9. El margen generado supera el costo de adquisición.
50. Objetivo de la V1

La V1 no busca demostrar:

"Podemos enviar 1000 mensajes."

Busca demostrar:

"Podemos generar ventas de manera sistemática,
medible y rentable utilizando automatización."

Ese es el verdadero criterio para decidir si Hunter debe escalar después de octubre.