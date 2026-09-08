# Fase 0 — Análisis y decisión de protocolo

## Decisión
- **Video (frames):** UDP.
- **Chat (texto):** TCP.

## Justificación técnica

### Por qué UDP para video
- El video es un flujo continuo de frames donde **la actualidad importa más que
  la completitud**: un frame perdido se reemplaza por el siguiente en
  fracciones de segundo, así que retransmitirlo no aporta valor (llegaría
  tarde y ya obsoleto).
- UDP no tiene control de flujo/congestión ni handshake, lo que reduce
  overhead y latencia — crítico para sostener ≥24 fps en tiempo real.
- El costo de UDP (posible pérdida, desorden, duplicación de paquetes) es
  tolerable en video: se ve como un parpadeo o frame saltado, no como un
  fallo funcional.
- Cada frame se serializa como JPEG independiente y se envía en un datagrama
  propio, así que la pérdida de un paquete no corrompe frames futuros.

### Por qué TCP para chat
- Los mensajes de chat son **discretos y deben llegar íntegros y en orden**:
  perder o desordenar un mensaje sí es un fallo funcional visible para el
  usuario.
- TCP garantiza entrega confiable, orden y control de flujo/congestión,
  a costa de mayor overhead — aceptable porque el chat es de bajo volumen
  (texto, esporádico) comparado con el flujo constante de video.
- Al ser orientado a conexión, TCP también permite detectar de forma más
  natural la conexión/desconexión del cliente (handshake y cierre reales),
  útil para reflejar estado de conexión en la UI.

## Consecuencia de diseño
Ambos protocolos corren en **hilos/threads independientes** (Fase 5) para que
la posible saturación de uno (ej. ráfaga de frames UDP) no bloquee al otro
(mensajes TCP), y viceversa.
