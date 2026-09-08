# Fase 7 — Plan de pruebas end-to-end

Ejecutar con dos instancias (dos builds o un build + el Editor) en la misma
red/localhost. Marcar cada caso al validarlo manualmente.

## 1. Chat TCP
- [ ] Servidor arranca en el puerto configurado (5555) sin error.
- [ ] Cliente se conecta con la IP/puerto correctos → evento `OnConnected`
      dispara y aparece "Conectado al servidor." en el chat.
- [ ] Mensaje enviado desde el cliente aparece en pantalla del cliente
      ("Yo: ...") Y llega al servidor ("Cliente: ...").
- [ ] Mensaje enviado desde el servidor llega al cliente ("Servidor: ...").
- [ ] Enviar con el campo de texto vacío no hace nada (validación existente).
- [ ] Cerrar el cliente (o matar el proceso) → servidor detecta
      desconexión ("Cliente desconectado.") sin crashear.
- [ ] Reconectar un cliente nuevo después de una desconexión funciona.

## 2. Video UDP
- [ ] Servidor (emisor) arranca cámara y comienza a enviar frames tras
      recibir el handshake ("Hi") del cliente.
- [ ] Cliente (receptor) muestra el video en el `RawImage` en tiempo real.
- [ ] Framerate percibido ≥ 24 fps aprox (ajustar `targetFps`/`jpegQuality`
      si hay lag).
- [ ] Simular pérdida de paquetes (ej. cortar red un instante) → el video
      recupera solo, sin excepción ni congelarse en un frame corrupto.
- [ ] Cerrar la escena/objeto de video → el puerto UDP se libera (se puede
      volver a arrancar sin "address already in use").

## 3. Concurrencia (chat + video simultáneos)
- [ ] Con video corriendo, enviar mensajes de chat no introduce lag visible
      en el video ni viceversa (confirma que no se bloquean entre sí).
- [ ] Cerrar solo el chat (o solo el video) no afecta al otro canal.

## 4. Errores esperados a verificar que NO rompan la app
- [ ] Intentar enviar mensaje de chat sin estar conectado → log, no excepción.
- [ ] Arrancar el video receptor antes que el emisor esté arriba → no
      crashea, simplemente no hay imagen hasta que el emisor conecta.
