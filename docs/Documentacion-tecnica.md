# Documentación técnica — Chat TCP + Video UDP

Explicación detallada de cómo funciona cada parte del proyecto: protocolos,
flujo de datos, concurrencia, y cómo están armadas las escenas.

---

## 1. Arquitectura general

Dos canales de red totalmente independientes entre Cliente y Servidor:

```
CLIENTE                                   SERVIDOR
┌─────────────────────┐                  ┌─────────────────────┐
│ TCPClient            │◄──── TCP:5555 ──►│ TCPServer            │  (chat)
│ UI_TCPClient          │                  │ TCPServerUI           │
├─────────────────────┤                  ├─────────────────────┤
│ UdpVideoClient        │◄──── UDP:5000 ──│ UdpVideoServer         │  (video)
│ VideoReceiver          │                  │ VideoSender            │
└─────────────────────┘                  └─────────────────────┘
```

- El **chat** usa **TCP** (puerto 5555): orientado a conexión, confiable,
  entrega garantizada y en orden.
- El **video** usa **UDP** (puerto 5000): sin conexión real, sin garantía de
  entrega, prioriza baja latencia sobre confiabilidad.

Ambos corren en paralelo dentro del mismo proceso de Unity, en objetos
`MonoBehaviour` distintos, sin bloquearse entre sí (ver sección 4).

---

## 2. Chat por TCP — paso a paso

### Contratos (interfaces)
- `IChatConnection`: eventos comunes (`OnMessageReceived`, `OnConnected`,
  `OnDisconnected`) + `SendMessageAsync` + `Disconnect`.
- `IClient` (extiende `IChatConnection`): agrega `ConnectToServer(ip, port)`.
- `IServer` (extiende `IChatConnection`): agrega `StartServer(port)`.

Esto permite que la capa de UI (`UI_TCPClient`, `TCPServerUI`) hable con
`TCPClient`/`TCPServer` a través de una interfaz genérica, sin acoplarse a la
implementación concreta.

### `TCPServer.cs`
1. `StartServer(port)` crea un `TcpListener` y llama a
   `AcceptTcpClientAsync()` — espera (sin bloquear el hilo) a que un cliente
   se conecte.
2. Al conectar un cliente, dispara `OnConnected` y arranca `ReceiveLoop()`
   como una `Task` en paralelo (`_ = ReceiveLoop()` — "fire and forget").
3. `ReceiveLoop()` hace `await networkStream.ReadAsync(...)` en bucle: cada
   vez que llegan bytes, los convierte a string UTF-8 y dispara
   `OnMessageReceived`. Si `ReadAsync` devuelve 0 bytes, el cliente se
   desconectó — sale del bucle y llama a `Disconnect()`.
4. `SendMessageAsync(message)` escribe bytes al `NetworkStream` con
   `WriteAsync`.
5. `Disconnect()`/`OnDestroy()` cierran el stream y el socket, y notifican
   con `OnDisconnected`.

### `TCPClient.cs`
Simétrico al servidor: `ConnectToServer(ip, port)` usa
`TcpClient.ConnectAsync`, y el resto (`ReceiveLoop`, `SendMessageAsync`,
`Disconnect`) es igual en estructura.

### Capa de UI (`UI_TCPClient.cs` / `TCPServerUI.cs`)
- Se suscriben a los eventos (`OnMessageReceived`, `OnConnected`,
  `OnDisconnected`) en `Start()`.
- Los botones de la escena llaman a `ConnectClient()`/`StartServer()` y a
  `SendClientMessage()`/`SendServerMessage()`.
- Cada mensaje enviado o recibido se agrega al texto `chatDisplay`
  (`TextMeshProUGUI`) con el método `AppendToChat()`, que además hace
  auto-scroll al fondo si hay un `ScrollRect` asignado.

**¿Por qué es seguro tocar la UI directo desde `HandleMessageReceived`?**
Porque `async`/`await` sin `ConfigureAwait(false)` captura el
`SynchronizationContext` de Unity: la continuación después de cada `await`
vuelve al hilo principal automáticamente. No hace falta una `ConcurrentQueue`
para el chat (a diferencia del video, ver sección 3).

---

## 3. Video por UDP — paso a paso

### Servidor (`VideoSender.cs` + `UdpVideoServer.cs`)
1. `VideoSender.Start()` llama a `udpServer.StartUDPServer(5000)` y arranca
   a enviar (`StartSending()`).
2. `UdpVideoServer.StartUDPServer` abre un `UdpClient` en el puerto 5000 y
   queda escuchando el **handshake** del cliente con `BeginReceive` (API
   asíncrona basada en callbacks, corre en un hilo del thread pool, no en el
   hilo principal de Unity).
3. Hasta que no llega el handshake (`ReceiveHandshake` completa y pone
   `hasClient = true`), el servidor **no manda ningún frame** — esto evita
   mandar a una dirección inválida antes de saber la IP/puerto real del
   cliente (UDP no tiene "conexión", el servidor recién sabe a quién
   responder después de recibir algo de él).
4. `VideoSender.CaptureLoop()` (una `Coroutine`, corre en el hilo principal)
   cada `1/targetFps` segundos:
   - Lee los píxeles de la `WebCamTexture` (la cámara).
   - Los vuelca a un `Texture2D`.
   - Llama a `udpServer.SendImage(texture, jpegQuality)`, que comprime el
     frame a JPEG (`EncodeToJPG`) y lo manda como **un solo datagrama UDP**.
5. Cada frame es independiente: si un datagrama se pierde en la red, el
   frame se descarta y sigue con el próximo — no hay reintentos (esa es la
   naturaleza de UDP, y es aceptable para video en vivo).

### Cliente (`VideoReceiver.cs` + `UdpVideoClient.cs`)
1. `VideoReceiver.ConnectToServer()` llama a
   `udpClient.StartUDPClient(ip, 5000)`, que manda el handshake
   (`SendHandshake()`, un datagrama con el texto "Hi") y arranca a escuchar
   con `BeginReceive`.
2. Cada vez que llega un datagrama, `ReceiveImage` lo pasa a
   `OnImageReceived`, que en `VideoReceiver` lo mete en una
   **`ConcurrentQueue<byte[]>`** (`EnqueueFrame`).
3. En `Update()` (que sí corre en el hilo principal de Unity, una vez por
   frame de render), se hace `TryDequeue` y si hay un frame nuevo se
   reconstruye con `texture.LoadImage(bytes)` y se muestra en el
   `RawImage`.

**¿Por qué acá SÍ hace falta `ConcurrentQueue`?**
Porque los sockets UDP usan `BeginReceive`/`EndReceive`, que ejecutan el
callback en un **hilo del thread pool**, no en el hilo principal de Unity ni
bajo el `SynchronizationContext` de Unity. Tocar la API de Unity (crear
texturas, actualizar UI) desde ese hilo tiraría una `UnityException`. Por
eso el hilo secundario solo **encola bytes** (operación segura/thread-safe
con `ConcurrentQueue`) y el hilo principal los **desencola y procesa** en
`Update()`. Es el mismo patrón productor-consumidor visto en los ejercicios
de concurrencia de la materia.

**Tolerancia a frames corruptos:** si un datagrama llega incompleto/dañado
(posible con UDP), `texture.LoadImage()` devuelve `false` y simplemente se
descarta ese frame sin romper la app — se sigue esperando el próximo.

**Cierre de sockets:** `UdpVideoServer`/`UdpVideoClient` cierran el socket en
`OnDestroy()` (`CloseServer()`/`CloseClient()`), y los callbacks pendientes
de `BeginReceive` verifican `ObjectDisposedException` para no tirar error si
el objeto se destruye mientras había una recepción en curso.

---

## 4. Concurrencia: por qué chat y video no se bloquean entre sí

| Canal | Mecanismo de concurrencia | Hilo de ejecución |
|---|---|---|
| Chat (TCP) | `async`/`await` sobre `NetworkStream` | Continuaciones vuelven al hilo principal (SynchronizationContext de Unity) |
| Video — envío | `Coroutine` (`CaptureLoop`) | Hilo principal, pero solo hace trabajo liviano (captura + JPEG + `Send` no bloqueante) |
| Video — recepción | `BeginReceive`/`EndReceive` (callbacks) | Hilo del thread pool → pasa datos al hilo principal vía `ConcurrentQueue` |

Ninguno de los tres usa `Thread.Sleep` ni una llamada de red **bloqueante**
en el hilo principal. Por eso una ráfaga de frames de video no traba el
chat, y viceversa: cada canal avanza de forma independiente y solo se
sincronizan en el punto justo donde hace falta (la cola concurrente del
video, o el `SynchronizationContext` del chat).

---

## 5. Escenas: cómo están armadas

- **`Assets/Chat_TCP_UDP/Scenes/`**: escenas originales de demo separadas
  por protocolo (`TCP/`, `UDP/`, `Video/`) — quedan como referencia/pruebas
  individuales de cada canal por separado.
- **`Assets/Cliente.unity`**: escena final del lado cliente. Fusiona en un
  mismo `Canvas`/`System` los objetos de `Tcp_Client` (chat: `TCPClient`,
  `UI_TCPClient`, `ChatDisplay`, input, botón) y de `Video_Client` (video:
  `UdpVideoClient`, `VideoReceiver`, `RawImage`). Un solo botón dispara dos
  acciones (`OnClick()` con dos entradas): `UI_TCPClient.ConnectClient` +
  `VideoReceiver.ConnectToServer`.
- **`Assets/Servidor.unity`**: análogo del lado servidor, fusiona
  `Tcp_Server` (chat) y `Video_Server` (video). El botón de arranque dispara
  `TCPServerUI.StartServer` + `VideoSender.StartSending`.

Cada escena mantiene **una sola** `Main Camera` y **una sola**
`EventSystem` (tener más de una de cada una genera warnings y comportamiento
indefinido de input/renderizado en Unity).

---

## 6. Flujo completo de una sesión típica

1. Se abre `Servidor.unity` y se da Play → arranca a escuchar TCP (5555) y
   UDP (5000), y la cámara empieza a capturar (pero no manda nada hasta que
   haya cliente).
2. Se abre `Cliente.unity` (otra instancia/proceso) y se da Play → clic en
   "Connect": dispara `ConnectClient()` (TCP) y `ConnectToServer()` (UDP,
   manda el handshake).
3. Servidor recibe la conexión TCP → `OnConnected` → chat listo.
4. Servidor recibe el handshake UDP → `hasClient = true` → empieza a mandar
   frames.
5. De ahí en más, ambos canales corren en paralelo: mensajes de chat van y
   vienen por TCP, frames de video van del servidor al cliente por UDP, sin
   interferir entre sí.
6. Si el cliente se desconecta (cierra la app, se corta la red), el
   servidor lo detecta por TCP (`ReadAsync` devuelve 0 bytes) y dispara
   `OnDisconnected`. El video simplemente deja de tener quien lo mire (el
   servidor sigue mandando si `hasClient` sigue en `true`, ya que UDP no
   tiene una notificación de desconexión — limitación conocida, ver
   `docs/Fase7-plan-pruebas.md`).

---

## 7. Limitaciones conocidas

- **Reconexión sin reiniciar el proceso:** si se destruye y recrea el objeto
  cliente en la misma sesión de Play, la reconexión no siempre agarra
  correctamente (el estado de los sockets no se reinicializa del todo).
  Reiniciar el Play sí permite reconectar sin problema. No es crítico para
  el uso normal (cerrar y volver a abrir la app), pero queda documentado
  como mejora futura.
- **UDP no notifica desconexión:** a diferencia de TCP, el servidor de video
  no tiene forma nativa de saber si el cliente se fue — solo deja de recibir
  frames si el cliente para de escuchar. No afecta la funcionalidad, pero es
  una limitación inherente al protocolo, no un bug.
