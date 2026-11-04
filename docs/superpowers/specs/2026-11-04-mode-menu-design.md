# Design — Menu de modo (Local / Multiplayer coop)

Data: 2026-11-04. Pedido do utilizador: depois de Jogar, escolher entre **Local (split-screen)** e **Multiplayer coop**. Nesta entrega o multiplayer é apenas navegável e local: criar uma sala que só contém o jogador deste PC e entrar na partida sozinho. **Sem rede, netcode ou lógica multiplayer.**

## Fluxo

```
Jogar → ModePanel ─┬─ Local (split-screen) ─────────────→ Briefing → Partida
                   └─ Multiplayer coop → LobbyPanel ─┬─ [Criar sala] → sala local com 1 jogador
                                                     │                → [Iniciar] → Briefing → Partida (sozinho)
                                                     └─ [Entrar em sala] desativado
```

## Painéis

- **ModePanel** (clone do BriefingPanel): título, descrição curta, botões **Local (split-screen)** e **Multiplayer coop**, **Voltar**. Foco inicial no Local. ESC/b → menu principal.
- **LobbyPanel** (clone do BriefingPanel), dois estados:
  - **Inicial**: "Criar sala" ativo; "Entrar em sala" desativado (visual apagado, com nota de indisponível); "Voltar" → ModePanel.
  - **Sala criada**: texto "SALA LOCAL · 1 jogador (tu)"; **Iniciar** → `OpenBriefing()`; "Criar sala" oculto; "Entrar" continua desativado; "Voltar" → ModePanel. O estado reinicia ao reabrir.

## Código

- `Assets/Scripts/ModeMenu.cs` (novo): `AbrirLobby()`, `CriarSala()`, `Iniciar()`, `Voltar()`; guarda referências aos painéis e ao `MainMenu`.
- `Assets/Scripts/MainMenu.cs`: campos `modePanel`/`modeFirstButton`; `Jogar` passa a `OpenMode()`; `OpenBriefing()` mantém-se.
- `Assets/Scripts/MenuLocalization.cs`: chaves novas no fim de `MenuTextKey` (PT/EN), reutilizando `Back`/`MissionStart` onde couber.
- Cena `StartMenu`: painéis criados por clone do briefing com o estilo existente (Rajdhani, #040A0E, botões 270×60), `MenuPanelTransition`, `MenuCancel`, `Reduzir movimento`.

## Non-goals

- Rede, Netcode/Relay, sincronização, matchmaking real.
- Alterar gameplay, cenas de jogo, `ProjectSettings` ou aplicar pareamento de dispositivos.
- Escolha de controlos por jogador (revertida de propósito).

## Adenda — escolha de sala e vista única (2026-11-04)

Pedido posterior: sozinho em multiplayer não deve ver o split-screen e deve poder escolher a sala. O âmbito alarga-se apenas a isto:

- Lobby, depois de **Criar sala**: escolha **Sala 1 · Pistas** / **Sala 2 · Desarmar**; **Iniciar** fica bloqueado até haver escolha e "Entrar em sala" desaparece.
- `GameSession` (estático) transporta `multiplayer`/`room` do menu para a partida e repõe-se no arranque do Play Mode.
- `SplitScreenManager`, em multiplayer: desativa o jogador da outra sala, deixa a câmara escolhida em `Rect(0,0,1,1)` e garante um `AudioListener` nessa câmara. Local mantém o split-screen original.
- `Hotbar`: não constrói a barra do jogador inativo; com um só ecrã ocupa a largura toda.

A escolha de sala não altera puzzles, soluções, temporizações nem introduz rede.

## Verificação

1. Compilação e Console sem erros.
2. Jogar → ModePanel; Local → Briefing → Partida; Multiplayer → Lobby → Criar → Iniciar → Briefing → Partida.
3. "Entrar em sala" não clicável; "Criar" desaparece após criar; Voltar em cada painel; ESC em cada painel.
4. PT/EN em todos os textos novos.
5. Captura visual dos dois painéis coerente com os restantes.
6. Multiplayer: Iniciar bloqueado até escolher sala; Sala 1 → só P1 ativo; Sala 2 → só P2 ativo; ambos em ecrã inteiro e com um AudioListener.
7. Local continua com as duas câmaras em metades.
