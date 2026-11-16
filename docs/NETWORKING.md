# KaMoon — rede (Mirror) e o que o jogo oferece em rede

> Estado: camada de rede da partida implementada e **compilada no Editor sem erros**
> (`Assembly-CSharp.dll` reconstruída, Console com 0 erros).
> **Play Mode com duas instâncias e hardware físico continuam por executar** nesta entrega
> (ver secção 7, "Como testar").

## 1. Arquitetura

Modelo **cliente-servidor** sobre Mirror 96.0.1 (transporte KCP, porta `7777`):

- **Host (`StartHost`)**: corre servidor + cliente no mesmo processo. É a autoridade da partida.
- **Cliente (`StartClient`)**: liga-se por `kcp://ip:7777`, envia pedidos e aplica o estado recebido.
- `NetworkManager` em `StartMenu` (`dontDestroyOnLoad`), `maxConnections = 2`, `playerPrefab = LobbyPlayer`,
  sem autenticador e sem cifra (rede local de confiança).

A rede está dividida em três camadas:

| Camada | Ficheiros | Responsabilidade |
| --- | --- | --- |
| Lobby | `Scripts/Network/LobbyPlayer.cs`, `LobbyRules.cs`, `LobbySession.cs`, `LobbyDiscovery.cs`, `Scripts/ModeMenu.cs` | Criar a partida, anunciar-se na LAN, listar salas encontradas, entrar com um clique, escolha de Sala 1/Sala 2, pronto, validação no servidor, arranque da partida. |
| Partida | `Scripts/Network/NetMatch.cs`, `NetMessages.cs` | Autoridade do relógio, energia e puzzles; poses dos avatares; remoção de objetos. |
| Guarda | `Scripts/Network/MatchGuard.cs` | Termina a partida e volta ao menu quando um dos jogadores sai. |

`NetMatch` é instalado em runtime no objeto do `NetworkManager`
(`[RuntimeInitializeOnLoadMethod]`), pelo que não é preciso reeditar cenas nem prefabs.
Não é um `NetworkBehaviour` nem usa `NetworkIdentity`: a replicação é feita com
mensagens Mirror registadas estaticamente, o que evita depender de `sceneId`/`assetId`
gerados pelo Editor.

### Lobby por descoberta em LAN

Quem cria a partida deixa de escrever o próprio IP e quem entra deixa de o copiar:

1. **Criar partida** faz `StartHost()` e `LobbyDiscovery.Anunciar()`, que liga o
   `NetworkDiscovery` do Mirror (difusão UDP na porta **47777**) a responder a pedidos.
2. O cliente abre o lobby, `LobbyDiscovery.Procurar()` começa a difundir pedidos e cada
   resposta (`ServerResponse.uri`, ex.: `kcp://192.168.1.10:7777`) aparece como um botão na
   lista **SALAS NA REDE** (até 2 salas visíveis).
3. Clicar no botão chama `ModeMenu.EntrarEm(uri)` → `StartClient(uri)`; a procura pára.
4. As escolhas de nome passaram a não ser pedidas: usa-se o nome da máquina
   (`LobbySession.Nome`), e os campos antigos de Nome/IP ficam ocultos.

O `NetworkDiscovery` é criado em runtime no objeto do `NetworkManager`, com o transporte KCP
atribuído e um *handshake* fixo do KaMoon, pelo que não é preciso editar cenas nem prefabs.
O botão antigo **Entrar** (com IP) continua exposto em `ModeMenu.Entrar()` apenas como
compatibilidade e entra na primeira sala encontrada.

## 2. Quem manda no quê

| Estado | Autoridade | Como chega ao cliente |
| --- | --- | --- |
| Relógio da partida | Servidor (`NetMatch`) | `EstadoPartidaMessage.tempo` (10 Hz) |
| Bateria de cada jogador | Servidor | `bateriaUm` / `bateriaDois` |
| Lanterna ativa (uma só) | Servidor | `lanternaDono` (0 = nenhuma, 1, 2) |
| Código do keypad | Servidor compara com `PuzzleKeypad.codigoCorreto` | `keypadResolvido` + `derrota` |
| Cabo certo do CRT | Servidor compara com `PuzzleCabosTV.indiceCaboCorreto` | `caboResolvido` + `derrota` |
| Cabos físicos (`PlayerInteraction`) | Servidor valida a tag e manda remover | `ObjetoRemovidoMessage` |
| Itens apanhados | Servidor remove o objeto em todos os clientes | `ObjetoRemovidoMessage` |
| Posição/rotação do avatar | Cliente dono envia; servidor reencaminha | `PoseJogadorMessage` → `PoseRemotaMessage` (20 Hz, canal *unreliable*) |
| Fim de partida | Servidor | `derrota` → `GameOverManager.GameOver()` em todos os clientes |

O cliente **nunca** decide o resultado de um módulo: `PuzzleKeypad.Submeter`,
`PuzzleCabosTV.CortarCabo`, `PlayerInteraction`, `Hotbar` (lanterna/bateria/itens) e
`GestorLanternas` passam a delegar em `NetMatch.Pedir*` quando `NetMatch.EmPartida` é verdadeiro.
No modo local (`GameSession.multiplayer == false`) todo o comportamento original fica intacto.

Fluxo de uma ação, exemplo keypad:

1. O cliente escreve o código e chama `NetMatch.PedirKeypad(codigo)`.
2. O servidor resolve a sala de quem enviou (`LobbyPlayer.SalaPorConexao`) e compara com o código correto.
3. O servidor marca `keypadResolvido` ou `derrota` e difunde novo `EstadoPartidaMessage`.
4. Todos os clientes atualizam o display e, em caso de falha, disparam o fim de partida.

## 3. Mensagens (`NetMessages.cs`)

| Mensagem | Direção | Campos |
| --- | --- | --- |
| `PoseJogadorMessage` | cliente → servidor | `posicao`, `yaw`, `pitch`, `velocidade`, `aFalar` |
| `PoseRemotaMessage` | servidor → clientes | `sala`, `posicao`, `yaw`, `pitch`, `velocidade` |
| `AcaoJogadorMessage` | cliente → servidor | `acao` (`NetAcao`), `indice`, `texto` |
| `ObjetoRemovidoMessage` | servidor → clientes | `nome` do GameObject |
| `EstadoPartidaMessage` | servidor → clientes | `tempo`, baterias, `lanternaDono`, `keypadResolvido`, `caboResolvido`, `derrota` |

Os serializadores são gerados pelo Weaver do Mirror para `struct ... : NetworkMessage`.

## 4. Ligação do avatar remoto

`SplitScreenManager`, em rede, mantém apenas a câmara da sala escolhida em ecrã inteiro e
neutraliza o avatar remoto: câmaras e `AudioListener` desligados, `CapsuleMovement` e
`PlayerInteraction` desativados e `Rigidbody.isKinematic = true`. O `NetMatch` aplica então
a pose recebida a esse avatar (interpolação exponencial) — é isto que faz o outro jogador
aparecer a andar e a olhar na direção correta do lado de lá do vidro.

## 5. O que um jogo em rede oferece aqui

Para além dos requisitos mínimos, esta arquitetura já permite:

- **Cooperação real à distância**: cada jogador vê o outro a mover-se e a apontar a lanterna.
- **Estado único da partida**: relógio, energia e módulos deixam de divergir entre máquinas.
- **Autoridade no servidor**: um cliente não consegue marcar um puzzle como resolvido nem adivinhar o código localmente.
- **Entrada/saída com regra**: `MatchGuard` termina a partida e devolve ambos ao menu quando um jogador cai.
- **Reutilização do mesmo código**: o modo local continua a funcionar sem servidor.
- **Extensões naturais**: relay/NAT punch (o transporte KCP já suporta *port forwarding*), mais do que dois
  jogadores (`maxConnections`), voz sobre rede, espectador e reconexão.

## 6. Limites conhecidos

- **Sem voz em rede**: `LocalVoiceChat` continua a ser captação + auto-escuta local.
- **Movimento autoritativo no cliente**: o servidor reencaminha poses; não há validação anti-cheat de velocidade/colisão.
- **Só 2 jogadores**: limite do design assimétrico (P1 pistas / P2 desarmamento), definido em `LobbyRules.MaxPlayers`.
- **Lobby em LAN por difusão UDP** (porta 47777): não há *matchmaking* pela Internet, relay/NAT punch nem lista de servidores fora da rede local.
- **Sem autenticação** nem cifra do tráfego.
- **Bateria partilhada (GDD)** não foi alterada: mantém-se a bateria por jogador do protótipo,
  agora simulada no servidor. A migração para uma carga central é uma decisão de design separada.
- **Sem UI de vitória**: só existe o fluxo de derrota (`GameOverManager`).

## 7. Como testar (procedimento, não resultado)

1. Abrir `Assets/Scenes/StartMenu.unity`, entrar em Play Mode.
2. **Jogar → Multiplayer coop → Criar sala**; escolher **Sala 1** e **Pronto**.
3. Numa segunda instância (build ou segundo Editor com o mesmo projeto **não** é permitido — usar build),
   **Entrar em sala** com o IP mostrado ao anfitrião; escolher **Sala 2** e **Pronto**.
4. **Iniciar** no anfitrião: ambos carregam `SampleScene`, cada um vê a sua sala em ecrã inteiro.
5. Verificar: relógio igual nos dois; lanterna a passar de um para o outro; bateria a descer no servidor;
   keypad correto resolve nos dois; cabo errado termina a partida nos dois; avatar remoto a mover-se.
6. Sair de um dos clientes: o outro deve voltar ao menu com o aviso de partida terminada.
