# Chat + Video — Proyecto Final Servicios Multimedia

Proyecto Unity que combina:
- **Chat de texto vía TCP** 
- **Streaming de video por frames vía UDP** 

Fork base: Chat-TCP-UDP-base (COKII5/Chat-TCP-UDP-base).

## Decisión de protocolo
Ver [`docs/Fase0-decision-protocolo.md`](docs/Fase0-decision-protocolo.md):
UDP para video (prioriza tiempo real sobre confiabilidad), TCP para chat
(prioriza integridad y orden sobre velocidad).


## Estructura
Assets/Chat_TCP_UDP/
  Scripts/
    Interface/   -> IChatConnection, IClient, IServer (contratos comunes)
    TCP/         -> TCPClient.cs, TCPServer.cs + UI/ (chat)
    UDP/         -> UDPClient.cs, UDPServer.cs + UI/ (demo UDP genérico)
    VIdeo/       -> UdpVideoClient.cs, UdpVideoServer.cs,
                    VideoSender.cs (cámara -> JPEG -> UDP),
                    VideoReceiver.cs (UDP -> ConcurrentQueue -> textura)
  Scenes/
    TCP/  Tcp_Client.unity, Tcp_Server.unity
    UDP/  Udp_Client.unity, Udp_Server.unity
    Video/ Video_Client.unity, Video_Server.unity
    
## Concurrencia
- **Chat (TCP):** `async/await` sobre `NetworkStream`. Unity captura el
  `SynchronizationContext`, así que las continuaciones (`OnMessageReceived`,
  etc.) vuelven al hilo principal — seguro tocar UI directo.
- **Video (UDP):** sockets crudos (`BeginReceive`/`EndReceive`) corren en
  hilos del thread pool. Los frames se pasan al hilo principal por una
  `ConcurrentQueue<byte[]>` y se consumen en `Update()` — mismo patrón visto
  en los ejercicios de concurrencia de la materia (productor en hilo
  secundario, consumidor en el hilo principal de Unity).
- Ambos canales son independientes: uno saturado no bloquea al otro.

## Cómo correr
1. Abrir el proyecto en Unity 6.
2. activar primero la instancia "servidor" que tiene (chat + video), luego la
   instancia "cliente" (ej. build + Editor, o dos builds en la misma red).
3. Chat: conectar con la IP del servidor y puerto "5555".
4. Video: el receptor se conecta al puerto "5000"; espera el comentario en consola del handshake emisor antes de empezar a recibir frames.

## Bugs encontrados y corregidos durante el desarrollo
- `VideoSender` nunca iniciaba el envío (`sending` quedaba en `false`).
- Framerate de envío subido de 10 fps a un `targetFps` configurable (30 por
  defecto).
- Condición de carrera en `UdpVideoServer`: podía intentar enviar frames
  antes de recibir el handshake del cliente.
- `VideoReceiver` no toleraba un frame UDP corrupto/incompleto.
- Los sockets UDP (`UdpVideoServer`/`UdpVideoClient`) nunca se cerraban al
  destruir el objeto — causaba fuga de puertos entre corridas.
