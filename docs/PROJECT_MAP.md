# KaMoon — mapa do projeto

> Snapshot de 2026-11-03, investigado pelo GPT-6 Luna e revisto pelo agente principal. Descreve fontes e serialização em disco, incluindo trabalho ainda não commitado. Não constitui certificação de funcionamento em runtime.

> Atualização posterior: StartMenu foi guardada com painel de opções, composição fullscreen e briefing. Foi acrescentado um painel de modo e um lobby local sem rede: Jogar abre o painel de modo, Local segue para o briefing em split-screen e Multiplayer abre o lobby (Criar sala → escolher Sala 1/Sala 2 → Iniciar → briefing). Em multiplayer a partida mostra apenas a câmara da sala escolhida em ecrã inteiro; Entrar em sala está desativado. O fluxo de abertura/fecho foi exercitado por comando em Play Mode; validações visuais, inputs físicos e persistência continuam pendentes. As referências ao painel ausente neste inventário descrevem o snapshot anterior.

## Como ler este mapa

- **Implementado**: o comportamento existe no código; não garante que o componente esteja ligado numa cena.
- **Serializado**: existe uma referência num asset/cena guardado em disco; não garante execução correta.
- **Observado no Editor**: estado consultado por MCP, sem implicar teste funcional.
- **Pretendido pelo GDD**: requisito de design, descrito em [GAME_DESIGN.md](GAME_DESIGN.md); não implica que esteja implementado ou testado.
- Nomes e caminhos são literais. Diferenças entre nome do ficheiro e classe devem ser preservadas até avaliar as referências.

Guia de execução e verificações: [DEVELOPMENT.md](DEVELOPMENT.md). Regras para agentes: [AGENTS.md](../AGENTS.md).

## 1. Visão geral

O código implementa um jogo Unity com dois jogadores locais, câmaras em split-screen, interação com puzzles, inventário/lanternas/baterias, temporizador e derrota por explosão. A voz usa microfone e reprodução locais; não foi demonstrado transporte de rede.

O GDD final identifica o jogo como **KaMoon!**, da **LAURA Games**, com dois astronautas numa estação lunar em 2087: P1 é analista de pistas, P2 executa o desarmamento. Exige informação dividida, uma só lanterna ativa e energia central partilhada entre luz e rádio. A descrição detalhada do alvo, regras, números e ambiguidades está em [GAME_DESIGN.md](GAME_DESIGN.md); o inventário abaixo continua a descrever o snapshot real, mesmo quando diverge desse alvo.

A entrada configurada em `ProjectSettings/EditorBuildSettings.asset` é:

```text
StartMenu (índice 0)
  └─ MainMenu.ChangeScene(): índice atual + 1
       └─ SampleScene (índice 1)
```

`MainMenu.RestartGame()` recarrega o índice atual. Reordenar as cenas de build altera o destino de `ChangeScene()`; não há escolha explícita por nome nesse método.

### GDD versus implementação encontrada

| Requisito pretendido | Fonte(s) existente(s) / estado auditado | Diferença ou verificação necessária |
| --- | --- | --- |
| P1 pistas, P2 bomba; salas isoladas por vidro | CapsuleMovement, PlayerInteraction, LeitorPistaArmario, prefabs Player/Player2 e Sala Principal. | Verificar restrições de espaço/interação e distribuição de informação; dois players não provam assimetria cumprida. PlayerInteraction ativo em P1 merece revisão face ao papel pretendido, sem assumir intenção pelo nome. |
| Uma lanterna, com períodos em que ambas estão apagadas | GestorLanternas já comuta P1/P2 em exclusão. | Não confundir com bateria central completa; validar input para desligar, consumo e bloqueio de recuperação. |
| Carga central 100%, taxas 1,5/3,0/4,5 e recarga 2,0; recuperação 15% | Duas Battery por jogador; defaults de 120 s por sistema, Recharge manual; Hotbar/voz consultam Battery. | Modelo de energia diverge. Não renomear as duas Battery como se já fossem uma Shared Power Cell nem tratar Recharge como recarga passiva. |
| Rádio PTT V/LT e bandpass | LocalVoiceChat usa V/RB/fallback B, microfone/reprodução locais e lowpass opcional. | Binding e filtro diferem; lowpass não é bandpass. A política de transmissão simultânea precisa confirmação. |
| Módulo A CRT com quatro cabos/coloridos e solução por serial | PuzzleCabosTV, pistas; há também CableRandomizer/FormRandomizer com 3–5 cabos e regras por forma. | São caminhos existentes distintos. Não usar automaticamente a regra por formas como solução do CRT por serial, nem apagar o módulo antigo sem decisão. |
| Keypad errado: −20 s + bloqueio | PuzzleKeypad chama GameOver no erro. TimerScript não oferece a política pretendida neste fluxo. | Penalização divergente; duração do bloqueio e tabela de pistas ainda não especificadas. O código 4281 é default de implementação, não solução final do GDD. |
| Vitória por todos os módulos ativos | Puzzles têm estados locais de resolvido; fluxo auditado demonstra derrota/restart, não agregação global de vitória. | Falta comprovar/implementar conclusão global, desativação da bomba e painel de estatísticas. |
| Menu → briefing → partida; sucesso → estatísticas → menu | MainMenu muda diretamente para a cena seguinte; restart recarrega a atual. | Briefing e percurso de vitória não demonstrados no snapshot. O GDD não exige novas cenas para cada ecrã. |
| HUD central de energia partilhada e relógio MM:SS | Battery usa barras OnGUI por jogador; TimerScript escreve em dois TMP. | Apresentação atual não equivale a HUD central partilhado. |
| Puzzles fullscreen com look bloqueado | Painéis e navegadores manuais já existem. | Não foi demonstrado bloqueio/restauro global da rotação ao abrir/fechar; verificar antes de considerar cumprido. |
| Spotlights 60°/20 m, sombra dinâmica | GestorLanternas referencia Lights. | Valores URP e luzes efetivas precisam de validação nos assets/Editor; a referência à Light não prova os parâmetros. |
| Pulso acelera abaixo de 60 s; explosão com camera shake | TimerScript/ExplosaoBomba fornecem tempo e parte do fluxo de derrota. | Áudio dinâmico e shake com os critérios do GDD não foram demonstrados pela auditoria. |
| Expansão C: sincronização <1 s | Não há implementação correspondente demonstrada. | Explicitamente futura, não ativar como requisito da partida atual sem decisão. |
| PC Windows 64-bit / DirectX 11 e mínimos do GDD | Unity/URP instalados; backend Windows não demonstrado pelo mapa de settings. | Build alvo e desempenho precisam de validação; os mínimos declarados não são medições. |

Estas diferenças são contexto para tarefas futuras, não autorização para modificar gameplay nesta atualização documental. “Não demonstrado” não equivale a prova de inexistência em cada asset; usar o mapa e confirmar o sistema afetado antes de implementar.

### Onde procurar por tipo de alteração

| Caminho | Conteúdo e papel |
| --- | --- |
| `Assets/` | Assets importados pelo Unity: cenas, scripts, prefabs, modelos, materiais, fontes e dados. `.meta` identifica cada asset por GUID. |
| `Packages/manifest.json` | Dependências diretas do projeto e versões pedidas. |
| `Packages/packages-lock.json` | Resolução de packages, dependências transitivas e origens; preservar coerência com o manifest. |
| `ProjectSettings/ProjectVersion.txt` | Versão/revisão do Editor a usar. |
| `ProjectSettings/ProjectSettings.asset` | Player Settings: produto, companhia, input handling, opções e valores por plataforma. |
| `ProjectSettings/EditorBuildSettings.asset` | Cenas habilitadas e ordem global de build, além de referências de configuração. Um perfil pode introduzir diferenças; confirmar o ativo. |
| `ProjectSettings/GraphicsSettings.asset` | Configuração gráfica global e referência ao render pipeline. |
| `ProjectSettings/QualitySettings.asset` | Presets de qualidade, defaults por plataforma e pipelines associados. O menu de opções já não expõe estes presets. |
| `ProjectSettings/TagManager.asset` | Tags e layers do projeto; relevantes para Player, WrongCable e CorrectCable usados pelos sistemas de interação. |
| `ProjectSettings/AudioManager.asset` | Configuração de áudio do Unity; não confundir com preferências de volume guardadas em PlayerPrefs. |
| `ProjectSettings/TimeManager.asset` | Passos/limites de tempo da simulação; o gameplay também altera `Time.timeScale` em runtime. |
| `ProjectSettings/InputManager.asset` | Configuração de input legado; a presença deste ficheiro não prova que seja o caminho principal do código. |
| `ProjectSettings/ProjectAuditorSettings.asset` | Configuração da ferramenta de auditoria do projeto; não é resultado de testes de gameplay. |
| `.gitignore` | Exclusões de artefactos Unity/build e setup local OpenCode. |
| `.vsconfig`, `.vscode/`, `KaMoon.slnx` | Integração com IDE/editor. Não substituir a configuração do Unity por edições da solução. |
| `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj` | Projetos C# gerados no ambiente local, ignorados pelo Git; não são a lista de fontes autoritativa. |
| `Library/`, `Temp/`, `Logs/`, `UserSettings/` | Estado gerado/local do Editor. Não confundir `Library/` da raiz com `Assets/Library/`. |
| `UpgradeLog.htm` | Artefacto local de upgrade, não documentação funcional do jogo. |
| `AGENTS.md`, `docs/` | Instruções portáveis e documentação: GAME_DESIGN para o alvo, PROJECT_MAP para o código/integração, DEVELOPMENT para execução/aceitação. |
| `.opencode/`, `opencode.jsonc` | Skills, plugins, estado e MCP locais; ignorados pelo Git. |

O mapa descreve código próprio individualmente e assets binários/terceiros por família. Não atribui uma responsabilidade de gameplay a cada `.meta`, textura ou variante de rocket.

## 2. Cenas

| Caminho | Papel demonstrável |
| --- | --- |
| `Assets/Scenes/StartMenu.unity` | Primeira cena habilitada na build; cena alvo do builder dos menus. |
| `Assets/Scenes/SampleScene.unity` | Segunda cena habilitada; destino de Jogar quando executado a partir de StartMenu. |
| `Assets/Scenes/David2.unity` | Cena adicional, fora da lista de build consultada. Finalidade/autoria não dedutível apenas do nome. |
| `Assets/Scenes/Goncalo.unity` | Cena adicional, fora da lista de build consultada. |
| `Assets/Scenes/Samuel.unity` | Cena adicional, fora da lista de build consultada. |
| `Assets/KaMoon_Room.unity` | Cena adicional fora de `Assets/Scenes`, não listada nessa build. |
| `Assets/BTM_Assets/BTM_Rockets_Missiles_Bombs/Scenes/DemoScene_RMB.unity` | Demonstração incluída no asset de terceiros. |
| `Assets/room/Cosmic_Retro_Station_Props_FREE/Demo_Scene/Cosmic_Retro_Station_Props_1_FREE.unity` | Demonstração incluída no asset de ambiente. |

Não apagar ou promover uma cena a principal com base no nome. Referências entre prefabs e cenas precisam ser verificadas antes de reorganizar assets.

## 3. Inventário dos scripts próprios

Todos os caminhos desta secção são relativos a `Assets/Scripts/`. O inventário inclui o código ainda não commitado no momento da pesquisa.

### 3.1 Menu principal e opções

| Ficheiro / tipo | Responsabilidade e implementação |
| --- | --- |
| `MainMenu.cs` — `MainMenu` | `OnEnable` liberta o cursor e tenta selecionar `firstSelectedButton`; `Start` repete a seleção para acomodar a inicialização do EventSystem. `ChangeScene()` guarda preferências, repõe `Time.timeScale` e carrega a cena seguinte. `OpenOptions()` mostra `optionsMenuPanel` e desativa o objeto do menu principal. `RestartGame()` recarrega a cena atual. `QuitApplication()` guarda preferências e sai da aplicação; no Editor termina Play Mode. |
| `OptionsMenu.cs` — `OptionsMenu` | Liga listeners em `Awake` e remove-os em `OnDestroy`; `OnEnable` preenche os controlos sem disparar callbacks e seleciona o dropdown do idioma. Aplica som/ecrã guardados antes da primeira cena por `RuntimeInitializeOnLoadMethod`. Deduplica resoluções e inclui o tamanho atual da janela como fallback. `FechaOpcoes()` esconde o painel, reativa o menu principal e repõe o foco no botão Opções. Guarda preferências em `OnDisable`. |
| `MenuLocalization.cs` — `MenuTextKey`, `MenuLocalization` | Dez chaves de texto para os menus em Português/Inglês. Guarda `MenuLanguage`, limita o índice e publica `LanguageChanged`. Limpa o evento estático em `SubsystemRegistration`. API: `SetLanguage`, `GetText`, `CurrentLanguage`, `LanguageNames`. Não é tradução automática de todos os textos do jogo. |
| `MenuLocalizedText.cs` — `MenuLocalizedText` | Exige TextMeshProUGUI; subscreve o evento de idioma em `OnEnable`, cancela em `OnDisable` e atualiza o texto. `SetKey()` define a chave de tradução e atualiza imediatamente. |
| `Navegaçao_Menus.cs` — `NavegadorMenuComando` | Navegação própria numa lista de botões com índice circular, orientação horizontal/vertical e indicador visual. Confirma invocando `Button.onClick`; cancelar invoca o botão de saída ou desativa o objeto. Lê teclado/gamepad diretamente, independentemente do EventSystem. |
| `Navegador_keypad.cs` — `NavegadorKeypad` | Navegação própria numa grelha de três colunas, com indicador e botão de saída. Não assumir que partilha o foco do EventSystem. |
| `Editor/OptionsMenuBuilder.cs` — `OptionsMenuBuilder` | Utilitário de Editor acessível em **Tools → KaMoon → Build Options Menu**. Exige StartMenu ativa, fora de Play Mode, e botões existentes ligados a `OpenMode`/`ChangeScene` e `QuitApplication`. Cria o painel (idioma, som, ecrã inteiro, resolução, voltar), o botão Options e liga o foco de regresso; agrupa Undo, marca a cena alterada e não a guarda automaticamente. |

Dependências principais: `UnityEngine.UI`, TextMeshPro, EventSystem e Input System UI. Os navegadores manuais e o EventSystem não devem processar a mesma confirmação sem avaliação do risco de dupla execução.

### 3.2 Jogadores, câmaras e interação

| Ficheiro / tipo | Responsabilidade e implementação |
| --- | --- |
| `SplitScreenManager.cs` — `SplitScreenManager` | Em `Awake`, resolve as câmaras por referências/nomes de objetos (`Player`/`Main Camera`, `Player2`/`CameraP2`), aplica viewports horizontal/vertical e desativa AudioListeners excedentes. Apresenta labels/divisor por `OnGUI`. `SetLayout()` permite alterar a disposição. Não constitui, por si só, um sistema de join/pareamento de dispositivos. |
| `CapsuleMovement.cs` — `CapsuleMovement` | Requer Rigidbody. Lê movimento e look em `Update`; aplica movimento relativo à câmara em `FixedUpdate`, atualiza Animator e trata chão/queda por raycast. Usa teclado, rato e gamepad diretamente; não demonstra consumo do asset de actions por PlayerInput. |
| `FollowCamera.cs` — `FollowCamera` | Em `LateUpdate`, segue o `target` serializado com offset, suavização e altura de look. A referência ao target faz parte da configuração da cena/prefab. |
| `PlayerInteraction.cs` — `PlayerInteraction` | Raycast a partir de `InteractorSource`, com indicador visual e interação por E. Para `WrongCable`, chama GameOver; para `CorrectCable`, apenas escreve log nesse caminho. Destrói o objeto do collider atingido nos dois casos. A existência de `CorrectCable` não demonstra um fluxo de vitória. |

### 3.3 Inventário, lanternas e energia

| Ficheiro / tipo | Responsabilidade e implementação |
| --- | --- |
| `Hotbar/Hotbar.cs` — `Hotbar` | Inicializa/normaliza slots e cria Canvas uGUI em runtime. Processa seleção/uso por teclado ou comando, conforme flags. API: `PlayerNumber`, `SelectSlot`, `SetItem`, `AddItem`, `HasItem`, `RemoveItem`, `UseSelected`. IDs especiais `flashlight` e `battery` acionam comportamentos próprios. Procura Battery/GestorLanternas na cena. A UI gerada usa `UnityEngine.UI.Text`, não TMP. Constrói também o crosshair do jogador (ponto central claro com contorno escuro): duas metades em split-screen local, ecrã inteiro no multiplayer. |
| `Hotbar/HotbarItem.cs` — `HotbarItem` | ScriptableObject de dados: `displayName`, `icon`, `itemId`. Pode ser criado pelo menu **KaMoon → Hotbar Item**. O item de inventário não é o mesmo objeto que o pickup físico. |
| `Hotbar/HotbarPickup.cs` — `HotbarPickup` | Requer Collider; `Reset` configura trigger. A Hotbar do jogador tenta recolher o item visado com E/A; o pickup valida jogador e item e só desaparece quando `AddItem` aceita a recolha. |

Atualização: `Assets/Objects_Prefab/AA battery.prefab` usa `HotbarPickup` e o item `Assets/Hotbar/Battery.asset`; `SampleScene` contém uma bateria perto de cada jogador. Olhar para ela a até 3 m e pressionar E/A ocupa um slot; usá-la recarrega apenas a bateria individual do respetivo jogador a 100%, consumindo o item quando a carga não está cheia. Esta decisão para a tarefa mantém o protótipo individual e diverge da energia central partilhada do GDD.
| `Hotbar/Battery.cs` — `Battery` | `Awake` define carga inicial 0/1 e encontra gestor/voz; `Update` drena carga conforme lanterna e fala ativas. Defaults de fonte: `lanternSeconds=120`, `voiceSeconds=120`, `startFull=true`, jogador 1. As taxas somam: ambos ativos gastam a carga em cerca de 60 s com esses defaults. Ao esgotar, apaga as luzes do jogador; a voz consulta `IsDepleted` para cortar transmissão. Expõe `Charge`, `IsDepleted`, `PlayerNumber`, `Recharge()`. Recharge repõe carga, mas não religa a luz. `OnGUI` apresenta a barra. |
| `GestorLanternas.cs` — `GestorLanternas` | Gere `lanternaP1`, `lanternaP2` e `p1ComecaLigado=true`. `Start` ativa uma e desativa outra quando as referências são válidas. F pede a lanterna P1; buttonNorth/Y pede P2. `AtivarLanterna(int)` exige as duas referências, procura Battery e bloqueia jogador sem carga; para 1/2, liga a luz desse jogador e desliga a do outro. É chamado por `Hotbar.UseFlashlight`; a política atual é uma luz ativa de cada vez. |

### 3.4 Puzzles e pistas

| Ficheiro / tipo | Responsabilidade e implementação |
| --- | --- |
| `Puzzle_Keypad.cs` — `PuzzleKeypad` | Esconde painel em `Start`, abre por proximidade + E/buttonSouth e fecha por Esc/buttonEast. API: `InserirDigito`, `Limpar`, `Submeter`, `AbrirKeypad`, `FecharKeypad`. Código inicial no fonte: `4281`, quatro dígitos. Sucesso marca resolvido e agenda fecho; erro chama GameOver. O Inspector pode substituir o valor inicial. |
| `PuzzleCabos/Puzzle_cabosTV.cs` — `PuzzleCabosTV` | Esconde painel e associa listeners aos botões em `Start`. Abre por proximidade ao jogador configurado e input global; o índice correto é serializado. Escolha correta resolve/fecha após atraso; escolha errada chama GameOver. |
| `CableRandomizer.cs` — `CableRandomizer` | `Start` chama `currentForm.RandomizeForm()`, sorteia os cabos e atribui tags segundo a tabela abaixo. `RandomizeCables()` recolhe filhos imediatos, desativa-os, sorteia entre 3 e 5 (limitado à quantidade disponível), embaralha por Fisher–Yates, ativa N e destrói os restantes; retorna N. `SetCableTags()` marca ativos como `WrongCable` e o escolhido como `CorrectCable` por índice 1-based. É destrutivo para os objetos de runtime não selecionados. |
| `CableModuleControl.cs` — `CableModuleControl` | Componente anexado ao prefab CableModule, mas funcionalmente vazio: `Start` e `Update` não fazem nada; não tem campos nem API de controlo implementada. Não confundir o nome com uma máquina de estados existente. |
| `FormRandomizer.cs` — `FormRandomizer` | `Start` vazio. `RandomizeForm()` sorteia um filho, ativa só esse e devolve índice+1. Sem guarda para zero filhos; os filhos do prefab constituem os dados do sorteio. É chamado explicitamente por CableRandomizer. |
| `Pista_Armario.cs` — `LeitorPistaArmario` | Interação por proximidade para mostrar/esconder nota ou painel. API `AbrirNota`/`FecharNota` e desenho de gizmo. O jogador associado é uma referência configurável, não inferível só do nome do campo. |
| `interruptorCumutator.cs` — `InterruptorComutador` | Deteta Player por trigger e alterna duas luzes com uma tecla; expõe `AlternarLuz()`. Usa referências às luzes e ao renderer. |

Regra do cabo correto em `CableRandomizer.Start` (índices de forma e cabo começam em 1):

| Cabos ativos | Forma 1 | Forma 2 | Forma 3 |
| --- | ---: | ---: | ---: |
| 3 | 2 | 3 | 3 |
| 4 | 4 | 1 | 2 |
| 5 | 2 | 5 | 5 |

A lógica espera os casos acima. O log em `SetCableTags()` acede a três nomes, por isso a seleção de menos de três cabos não é um caso seguro só porque o sorteio o permite. As tags precisam existir em `TagManager`; preencher `currentForm` e fornecer filhos válidos é parte da configuração do prefab.

### 3.5 Tempo, derrota e explosão

| Ficheiro / tipo | Responsabilidade e implementação |
| --- | --- |
| `TimerScript.cs` — `TimerScript` | Inicializa `currentTime` a partir de `startTime` em `Start`; decrementa com `Time.deltaTime` em `Update` e escreve dois TMP com minutos/segundos arredondados para baixo. Default de fonte: 60 s. A condição de fim é tratada no frame seguinte ao cruzamento de zero, podendo aparecer um valor negativo por um frame; depois fixa `00:00`, marca `isGameOver` e chama GameOver uma vez nesse caminho. As referências TMP/gestor são obrigatórias no código atual. |
| `GameOverManager.cs` — `GameOverManager` | `GameOver()` pede `Bomba.Detonar()` e inicia coroutine; após 3 s mostra `GameOverUI`, pausa o tempo e liberta o cursor. Não há proteção demonstrada contra chamadas repetidas. A espera é escalada (`WaitForSeconds`). |
| `Explosão_bomba.cs` — `ExplosaoBomba` | `Detonar()` inicia a sequência visual/áudio da explosão usando partículas, luz, modelo e AudioSource referenciados. Chamado pelo GameOverManager. |

Fluxo implementado no código:

```text
Timer a zero / erro num puzzle / WrongCable
  → GameOverManager.GameOver()
  → ExplosaoBomba.Detonar()
  → espera escalada de 3 s
  → GameOverUI + Time.timeScale = 0 + cursor livre
```

Não há neste fluxo um sistema de vitória demonstrado, embora seja exigido pelo GDD. A regra atual de erro fatal do keypad também diverge do GDD (−20 s e bloqueio). O callback persistente `MainMenu.RestartGame` existe em SampleScene; o alvo está ligado em disco, o que não prova que toda a derrota/reinício tenha sido exercitada em runtime.

### 3.6 Voz local

`Voice/LocalVoiceChat.cs` contém `LocalVoiceChat`, requer AudioSource e cria InputActions em runtime. Implementa push-to-talk local, captura pelo microfone, reprodução/monitorização, filtro lowpass opcional, diagnóstico por `OnGUI` e recuperação após mudança de dispositivo. Expõe `TalkingPlayer`; a bateria pode impedir transmissão.

P1 usa V; P2 usa RB e fallback B, conforme a implementação de bindings. Não existe transporte de rede demonstrado: não documentar como chat online. O volume global do menu também pode afetar a reprodução de voz pelo AudioListener.

## 4. Scripts fora de Assets/Scripts

| Caminho | Papel e limites |
| --- | --- |
| `Interruptor.cs` | Ficheiro na raiz, integralmente comentado, fora da árvore normal de importação Unity. Contém uma versão conceptual do interruptor; não é fonte ativa do jogo e não deve ser apagado sem confirmação. |
| `Assets/TutorialInfo/Readme.cs` | ScriptableObject de dados do tutorial/template, com estilos UI Toolkit, título, ícone e secções. |
| `Assets/TutorialInfo/Editor/ReadmeEditor.cs` | CustomEditor que apresenta o Readme, seleciona o tutorial e oferece remoção dos assets mediante diálogo. Não é ferramenta de gameplay. |
| `Assets/BTM_Assets/BTM_Rockets_Missiles_Bombs/Scripts/RocketRotator.cs` | Classe `Benjathemaker.RocketRotator`: roda um objeto em `Update` com vetor serializado; pertence ao asset de terceiros. |

Diferenças ficheiro/classe a preservar: `Navegaçao_Menus`/`NavegadorMenuComando`, `Navegador_keypad`/`NavegadorKeypad`, `Explosão_bomba`/`ExplosaoBomba`, `Puzzle_Keypad`/`PuzzleKeypad`, `Puzzle_cabosTV`/`PuzzleCabosTV`, `Pista_Armario`/`LeitorPistaArmario`, `interruptorCumutator`/`InterruptorComutador`.

## 5. Opções e persistência

| Chave PlayerPrefs | Significado / fallback |
| --- | --- |
| `MenuLanguage` | Índice PT/EN; 0 (Português) por defeito, limitado ao intervalo suportado. |
| `MasterVolume` | Volume linear 0..1; default 1; mostrado como percentagem. |
| `MasterMuted` | Silenciar por volume do AudioListener; default falso; mantém o valor de MasterVolume. |
| `Fullscreen` | Preferência de ecrã inteiro; fallback para o estado atual do ecrã. |
| `ResolutionWidth`, `ResolutionHeight` | Dimensões guardadas; procura correspondência na lista disponível. |
| `ResolutionIndex` | Compatibilidade com a versão anterior baseada em índice. |

Abrir opções preenche controlos com métodos sem notificação para não tratar o carregamento como interação. A inicialização global restaura definições mesmo sem abrir o painel. As opções incluem idioma, volume geral/mute, ecrã inteiro e resolução; qualidade gráfica e reduzir movimento foram retirados a pedido. Uma chave `QualityLevel` antiga pode permanecer no registo do utilizador, mas já não é lida nem aplicada.

## 6. Integração guardada das cenas de build

### StartMenu

- Objeto `MainMenu`: GameObject fileID `348065467`, componente MainMenu `348065469`.
- `Button` (componente Button `1097090102`) chama `MainMenu.ChangeScene` no componente `348065469`.
- `Button (1)` (componente Button `835407540`) chama `MainMenu.QuitApplication` no mesmo alvo.
- O MainMenu guardado não tem `optionsMenuPanel`/`firstSelectedButton` atribuídos; sem dados serializados, são referências nulas.
- **Não existe `OptionsMenuPanel` nem componente OptionsMenu/MenuLocalizedText guardado nesta cena no snapshot analisado.** O código das opções existe, mas a geração e gravação da UI ainda são necessárias.
- Um EventSystem e um InputSystemUIInputModule; não foi encontrada referência PlayerInput ou StandaloneInputModule nessa cena.

### SampleScene

- Há componentes próprios diretos de MainMenu, GameOverManager, ExplosaoBomba, TimerScript, PlayerInteraction, LeitorPistaArmario, Battery e Hotbar.
- O botão do objeto `GameOver` (GO `1758232837`, Button `1758232839`) chama `MainMenu.RestartGame` no componente `1332752252`, anexado ao objeto **`Panel`** (`1332752248`). O nome do tipo MainMenu não é o nome desse GameObject.
- Parte dos outros sistemas entra por instâncias de prefabs, não por componentes m_Script diretamente no ficheiro da cena.
- Um EventSystem e um InputSystemUIInputModule; não foi encontrada referência direta PlayerInput ou StandaloneInputModule.

As contagens são do YAML em disco; não pressupõem a quantidade total de componentes herdados de prefabs nem a validade de todas as referências internas.

### Referências críticas e estados em SampleScene

| Componente / objeto | Configuração guardada | Consequência / limite |
| --- | --- | --- |
| TimerScript `1049075574` / `Suporte_Timer` | `startTime=60`; dois TMP atribuídos; **`gameOver` nulo**. | A chamada a GameOver ao expirar o timer não tem destino atribuído no snapshot. O código não faz null-check nessa chamada. |
| PlayerInteraction `1724678369` / `Player` | Ativo; origem não nula, alcance 5 e Dot atribuído; **`gameOver` nulo**. | No ramo WrongCable, a chamada sobre null impede a conclusão normal desse caminho; não afirmar que a derrota por cabo funciona. |
| PlayerInteraction `265295956` / `Player2` | **Componente desativado**; origem não nula, alcance 5, mesmo Dot; `gameOver` nulo. | Não executa Update enquanto desativado; referências existentes não significam interação de P2 ativa. |
| Battery `1724678367` / `Player` | Jogador 1, 120 s de lanterna e 120 s de voz, carga inicial cheia; `flashlight` nula. | Pode consultar o gestor de lanternas como fallback; luz nula isoladamente não demonstra falha. |
| Battery `265295954` / `Player2` | Jogador 2, mesmos tempos/carga; `flashlight` nula. | Há duas Battery, uma por jogador. |
| Hotbar `1724678368` / `Player` | Jogador 1, quatro slots, teclado ligado, gamepad desligado; componente ativo. | Ativo desde a tarefa da hotbar; constrói a barra e o crosshair de P1. |
| Hotbar `265295955` / `Player2` | Jogador 2, quatro slots, teclado desligado, gamepad ligado; componente ativo. | Há duas Hotbar; ambas têm um item referenciado no slot 0, não inventários vazios por omissão. |
| GameOverManager `303886131` / `GameOverManager` | Bomba aponta ao componente ExplosaoBomba da instância Sala Principal; GameOverUI aponta a `Panel`. | Referências não nulas, resolvidas por fileID e fonte prefab. `Panel` começa inativo e contém também MainMenu. |
| SplitScreenManager / instância prefab | `layout=1` (Vertical), cameraP1/P2 nulas no prefab; labels PLAYER 1/PLAYER 2. | `Awake.ResolveCameras()` precisa procurar câmaras por nome; Player/Main Camera e Player2/CameraP2 existem nas fontes dos prefabs. |

Os players têm GameObjects `stripped` na cena: nomes foram resolvidos usando `m_CorrespondingSourceObject` com GUID/fileID dos prefabs Player/Player2. A presença de uma referência não nula não prova que o ponto de interação ou a câmara corresponda ao design pretendido. Não foi feita essa validação funcional.

## 7. Inputs: configuração versus consumidores

`Assets/InputSystem_Actions.inputactions` contém dois maps, `Player` e `UI`, e schemes `Keyboard&Mouse`, `Gamepad`, `Touch`, `Joystick`, `XR`.

| Map | Actions |
| --- | --- |
| `Player` | `Move`, `Look`, `Attack`, `Interact` (Hold), `Crouch`, `Jump`, `Previous`, `Next`, `Sprint`. |
| `UI` | `Navigate`, `Submit`, `Cancel`, `Point`, `Click`, `RightClick`, `MiddleClick`, `ScrollWheel`, `TrackedDevicePosition`, `TrackedDeviceOrientation`. |

Principais bindings declarados no asset: Move por leftStick/WASD/setas; Look por rightStick/pointer delta; Attack por buttonWest/clique esquerdo/Enter; Interact por E/buttonNorth; Jump por Space/buttonSouth; Previous/Next por 1/2 e dpad left/right; Sprint por Shift/leftStickPress; Crouch por C/buttonEast.

**Isto não é o mapa garantido dos comandos do gameplay.** Os scripts próprios analisados leem `Keyboard.current`, `Gamepad.current` e `Mouse.current` diretamente; LocalVoiceChat cria actions próprias. Não foi demonstrado consumo desse asset pelos controladores de jogador.

| Sistema implementado | Leitura usada |
| --- | --- |
| Movimento/look | Teclado/rato e sticks diretamente em CapsuleMovement. |
| Lanternas | F para P1 e buttonNorth/Y para P2 em GestorLanternas. |
| Hotbar | Teclas de slots 1-8 e G para usar; D-pad esquerdo/direito e R3 para usar, segundo flags/configuração. F/Y ficam para a lanterna. |
| Puzzles/painéis | E/buttonSouth para abrir e Esc/buttonEast para fechar nos scripts correspondentes; verificar diferenças entre componentes. |
| Voz | V para P1; RB e fallback B para P2 nas InputActions criadas por código. |
| Menus | EventSystem/InputSystemUIInputModule e, noutros contextos, navegadores manuais que invocam Button.onClick. |

Os módulos UI das cenas de build referenciam o asset de GUID `ca9f5fa95ffab41fb9a615ab714db018`, diferente do GUID `052faaac586de48259a63d0c4782560b` de `InputSystem_Actions.inputactions` referenciado nas configurações. Não substituir um pelo outro sem verificar actions/referências.

## 8. Prefabs e assets

Prefabs próprios principais em `Assets/PreFab_Sala/`: CableModule, Gestor_lanternas, LocalVoice, SplitScreenManager, Player, Player2, Sala Principal e Puzzles. Os nomes completos e referências devem ser consultados nos assets antes de mover/renomear.

| Prefab/sistema | Scripts/relações demonstradas |
| --- | --- |
| `CableModule.prefab` | CableModuleControl, CableRandomizer e FormRandomizer. Instâncias de cena verificadas em David2 e SampleScene. |
| `Gestor_lanternas.prefab` | GestorLanternas; instâncias verificadas em SampleScene e Samuel. |
| `LocalVoice.prefab` | LocalVoiceChat; instâncias verificadas em David2, SampleScene, Goncalo e Samuel. |
| `SplitScreenManager.prefab` | SplitScreenManager; instâncias verificadas nessas quatro cenas. |
| `Player.prefab`, `Player2.prefab` | CapsuleMovement; incluídos por instâncias em cenas, apesar da ausência de m_Script direto do movimento nelas. |
| `Sala Principal.prefab` | Contém componentes de ExplosaoBomba, LeitorPistaArmario e PuzzleKeypad. Há também referências diretas a alguns desses scripts em cenas. |
| `Puzzles.prefab` | Inclui os navegadores manuais NavegadorKeypad e NavegadorMenuComando; alcançável por SampleScene e Samuel. |

### Referências por script, diretas e via prefab

Na tabela seguinte, nomes de cena sem caminho são de `Assets/Scenes/`; `KaMoon_Room` é `Assets/KaMoon_Room.unity`. A inclusão via prefab foi seguida recursivamente por `m_SourcePrefab`. Ela descreve composição serializada, não garante que todos os componentes herdados permaneçam ativos ou sem overrides.

| Script(s) em Assets/Scripts | Cenas alcançadas | Origem principal |
| --- | --- | --- |
| `MainMenu.cs` | StartMenu, SampleScene | Componentes diretos. |
| `CableModuleControl.cs`, `CableRandomizer.cs`, `FormRandomizer.cs` | SampleScene, David2 | CableModule.prefab. |
| `CapsuleMovement.cs` | SampleScene, David2, Goncalo, Samuel | Player.prefab e Player2.prefab. |
| `Explosão_bomba.cs` | SampleScene, David2, Samuel, KaMoon_Room | Sala Principal.prefab; também componente direto em SampleScene/David2. |
| `GameOverManager.cs` | SampleScene | Componente direto. |
| `GestorLanternas.cs` | SampleScene, Samuel | Gestor_lanternas.prefab. |
| `Navegador_keypad.cs`, `Navegaçao_Menus.cs` | SampleScene, Samuel | Puzzles.prefab. |
| `Pista_Armario.cs` | SampleScene, David2, Samuel, KaMoon_Room | Sala Principal.prefab; também componente direto em SampleScene. |
| `PlayerInteraction.cs` | SampleScene, David2 | Componentes diretos; avaliar estados enabled de cada jogador. |
| `Puzzle_Keypad.cs` | SampleScene, David2, Samuel, KaMoon_Room | Sala Principal.prefab; também componente direto em Samuel. |
| `SplitScreenManager.cs` | SampleScene, David2, Goncalo, Samuel | SplitScreenManager.prefab. |
| `TimerScript.cs` | SampleScene, David2 | Componentes diretos. |
| `Hotbar/Battery.cs`, `Hotbar/Hotbar.cs` | SampleScene, Goncalo | Componentes diretos/adicionados às instâncias Player/Player2. |
| `PuzzleCabos/Puzzle_cabosTV.cs` | SampleScene, David2, Samuel, KaMoon_Room | Sala Principal.prefab. |
| `Voice/LocalVoiceChat.cs` | SampleScene, David2, Goncalo, Samuel | LocalVoice.prefab. |
| `FollowCamera.cs`, `interruptorCumutator.cs`, `MenuLocalization.cs`, `MenuLocalizedText.cs`, `OptionsMenu.cs`, `Hotbar/HotbarItem.cs`, `Hotbar/HotbarPickup.cs`, `Editor/OptionsMenuBuilder.cs` | Sem m_Script encontrado nas cenas/prefabs do levantamento. | Não é declaração de código morto: há tipos estáticos, Editor, ScriptableObjects e componentes gerados dinamicamente. |

Foram cruzados os 27 scripts próprios com seis cenas próprias e o grafo de 304 prefabs. Há 51 pares script/cena alcançados no CSV. Prefabs distintos alcançáveis, sem contar múltiplas instâncias: StartMenu 0; SampleScene 16; David2 14; Goncalo 4; Samuel 15; KaMoon_Room 9.

O agrupamento de diretórios evita tratar centenas de modelos/prefabs terceiros como lógica própria:

| Diretório | Conteúdo / cuidados |
| --- | --- |
| `Assets/Animations/` | Animações, controller, modelos e materiais associados. |
| `Assets/BTM_Assets/` | Rockets/missiles/bombs de terceiros, muitas variantes de prefab, modelos, materiais e demo. |
| `Assets/Hotbar/` | Assets de dados Battery/Flashlight, distintos do código em `Assets/Scripts/Hotbar/`. |
| `Assets/Library/` | Conteúdo importado dentro de Assets; **não é a pasta gerada `Library/` da raiz**. Não apagar por nome. |
| `Assets/Materials/`, `Assets/Models/`, `Assets/Objects/` | Materiais/texturas e modelos OBJ/MTL/FBX. |
| `Assets/PreFab_Sala/` | Prefabs de gameplay/ambiente da sala e jogadores. |
| `Assets/room/` | Pacote de ambiente Cosmic Retro Station e demo de terceiros. |
| `Assets/Scenes/` | Cenas principais e adicionais do projeto. |
| `Assets/Scripts/` | Código próprio, incluindo subpastas Hotbar, Voice, PuzzleCabos e Editor. |
| `Assets/Settings/` | Assets de renderização/URP. |
| `Assets/TextMesh Pro/` | Fontes, resources, shaders e definições TMP; conteúdo de suporte, não scripts de jogo. |
| `Assets/TutorialInfo/` | Readme/editor/estilos/layout do template. |

## 9. Fontes, limites e manutenção

O inventário reconciliado encontrou **1 032 ficheiros em Assets**, incluindo `.meta`, subpastas e eventuais ficheiros ocultos (nenhum oculto devolvido nesta contagem). São **27 scripts próprios** sob `Assets/Scripts`, **3 scripts adicionais** em assets/tutorial e **1 ficheiro C# comentado na raiz**, fora de Assets. Há **8 cenas** e **304 prefabs**.

Contagens por extensão em Assets: 541 `.meta`, 304 `.prefab`, 52 `.fbx`, 30 `.cs`, 27 `.mat`, 18 `.shader`, 16 `.asset`, 12 `.png`, 8 `.unity`, 4 `.cginc`, 4 `.shadergraph`, 3 `.txt`, 3 `.uss`, 2 `.anim`; um de cada `.controller`, `.hlsl`, `.inputactions`, `.mtl`, `.obj`, `.pdf`, `.ttf`, `.wlt`. Soma = 1 032. Os 58 ficheiros em Assets/Scripts incluem 27 C# e 31 metas; não se deve assumir um par simples por script, pois há metas de pastas.

Na inspeção MCP de 2026-11-03: StartMenu era a única cena carregada e ativa, sem alterações não guardadas, fora de Play Mode e sem compilação em curso; foi encontrado um EventSystem ativo. Não foram executados testes funcionais.

Ausência de uma ocorrência YAML direta não prova que um script seja inútil: pode ser criado em runtime, usado via prefab ou referenciado por reflexão. Por exemplo, MenuLocalization é estático, HotbarItem é ScriptableObject e OptionsMenuBuilder é código de Editor: não é esperado que todos tenham m_Script em cenas/prefabs.

Evidências de implementação para os sistemas complementados: `CableModuleControl.cs:3–15`, `CableRandomizer.cs:11–125`, `FormRandomizer.cs:3–19`, `GestorLanternas.cs:4–71`, `Hotbar/Battery.cs:9–134`, `TimerScript.cs:5–42` (todos sob `Assets/Scripts/`; linhas do snapshot, sujeitas a alterações).

A pesquisa detalhada e CSVs estão localmente em `.opencode/research/`, ignorados pelo Git; este documento mantém a informação essencial sem exigir esses ficheiros noutra máquina. O método combinou leitura de fontes, extração de GUIDs `.meta`, divisão de YAML por blocos/fileID, referências m_Script, callbacks persistentes e fecho recursivo m_SourcePrefab. Não avalia todas as propriedades/overrides do Unity como um importador real faria.

Foram revistas as referências críticas descritas acima; não foi validado cada campo de cada prefab nem a correspondência funcional dos InteractorSource. Não foram executados compilação do projeto, testes, Play Mode ou build nesta auditoria. A consulta MCP executou somente código temporário de leitura de estado do Editor.
