# KaMoon — desenvolvimento e verificação

> Snapshot de 2026-11-03. Resulta da investigação do GPT-6 Luna, cruzamento de referências e revisão do agente principal. Os procedimentos abaixo são instruções; não são registo de testes aprovados ou de uma build executada.

Regras para agentes: [AGENTS.md](../AGENTS.md). Sistemas e ficheiros: [PROJECT_MAP.md](PROJECT_MAP.md).

Design alvo: [GAME_DESIGN.md](GAME_DESIGN.md), síntese do GDD final fornecido posteriormente à auditoria. **Procedimentos do protótipo não são critérios de conformidade com o GDD.** Quando houver diferença, reportar requisito, implementação e resultado do teste separadamente.

## 1. Ambiente

- Abrir com **Unity 6000.6.2f1**, conforme `ProjectSettings/ProjectVersion.txt`.
- O código usa Unity, URP, uGUI/TextMeshPro e Input System. O Input Handling consultado é **Both**; não migrar para um só backend sem avaliar os consumidores.
- As dependências são declaradas em `Packages/manifest.json` e resolvidas em `Packages/packages-lock.json`. Não atualizar pacotes como efeito secundário de uma tarefa de menus/documentação.
- Configuração específica de render/qualidade em `ProjectSettings/GraphicsSettings.asset`, `ProjectSettings/QualitySettings.asset` e assets em `Assets/Settings/`.
- Presets e valores do Inspector podem substituir defaults do código; consultar a cena/prefab que se pretende executar.

### Dependências relevantes no snapshot

| Package | Versão declarada | Papel |
| --- | --- | --- |
| `com.unity.inputsystem` | `1.20.0` | Input direto nos scripts e módulo UI. |
| `com.unity.render-pipelines.universal` | `17.6.0` | URP, com assets/configuração de render no projeto. |
| `com.unity.ugui` | `2.6.0` | UI Canvas, Selectables e infraestrutura de texto/UI usada no código. |
| `com.unity.test-framework` | `1.8.0` | Infraestrutura instalada; não há testes próprios encontrados. |
| `com.unity.ai.assistant` | `2.19.0-pre.2` | Assistant e bridge MCP oficial; pacote pré-release. |
| `com.unity.ai.navigation` | `2.0.14` | Instalado; não concluir uso no gameplay só pela dependência. |
| `com.unity.visualscripting` | `1.9.12` | Instalado; não demonstra que o fluxo principal use grafos visuais. |
| `com.unity.timeline` | `6.6.0` | Instalado; não demonstra por si só uso de cutscenes. |

As versões efetivas/resolução transitiva devem ser verificadas no lockfile antes de mudar dependências.

## 2. Abrir e executar

1. Adicionar a raiz do projeto no Unity Hub e selecionar a versão indicada.
2. Aguardar a resolução dos packages, importação e compilação. Verificar a Console sem apagar as mensagens existentes por rotina.
3. Abrir `Assets/Scenes/StartMenu.unity` para testar a entrada do jogo.
4. Verificar **File → Build Profiles** e a lista de cenas antes de testar Jogar: o `EditorBuildSettings.asset` consultado contém StartMenu seguida de SampleScene.
5. Entrar em Play Mode e testar as ações visíveis. `MainMenu.ChangeScene()` usa índice seguinte; abrir uma cena diferente diretamente não reproduz necessariamente o fluxo da build.
6. Para testar gameplay isolado, abrir `Assets/Scenes/SampleScene.unity` depois de guardar/decidir o destino de eventuais alterações pendentes na cena atual.

O GDD pretende Menu → Briefing → Partida e percursos separados de sucesso/falha. Os passos atuais StartMenu → SampleScene descrevem o protótipo, não comprovam que o briefing ou a vitória estejam implementados.

Nunca abrir uma segunda instância Unity em batch mode sobre o mesmo projeto enquanto este está aberto no Editor. Não fechar o Editor nem descartar cenas para contornar essa restrição sem autorização.

## 3. Menu principal e opções

Fontes: `Assets/Scripts/MainMenu.cs`, `OptionsMenu.cs`, `MenuLocalization.cs`, `MenuLocalizedText.cs` e `Editor/OptionsMenuBuilder.cs`.

Para gerar/atualizar a UI:

1. Sair de Play Mode apenas depois de avaliar se há estado que precisa de ser preservado.
2. Abrir `Assets/Scenes/StartMenu.unity` e confirmar que é a cena ativa.
3. Inspecionar os elementos existentes. O builder substitui os objetos gerados com os nomes `OptionsMenuPanel` e `OptionsButton`; alterações manuais dentro desses objetos podem perder-se na reconstrução.
4. Executar **Tools → KaMoon → Build Options Menu**.
5. Inspecionar o resultado e os campos atribuídos, incluindo os listeners de Jogar/Sair e a ligação entre painéis.
6. Guardar a cena conscientemente. O builder marca-a como alterada, mas não faz Save automaticamente. Há Undo para a construção.
7. Testar novamente. Executar o builder não prova que as opções funcionam em Play Mode.

**Atualização:** StartMenu foi guardada com painel de opções, briefing e cenário de menu composto por 18 malhas da Sala Principal, sem scripts de gameplay, câmaras ou colliders nessa composição. Foi observada uma captura do menu em Game View a 880×426; o acabamento visual continua por melhorar. Em Play Mode, um comando verificou a abertura e fecho dos painéis e o foco ao regressar às opções. Não foram verificados os inputs físicos, idiomas, persistência, layout nas resoluções alvo ou build standalone. O builder de opções foi criado para o layout anterior; executá-lo novamente requer inspecionar o resultado para não desfazer ligações do menu atual.

O menu usa `Assets/Fonts/KaMoon/Rajdhani-Regular.ttf` e o respetivo asset TMP, com licença em `Assets/Fonts/KaMoon/OFL-Rajdhani.txt`. `MenuAmbientMotion` aplica oscilação discreta à câmara e luz de destaque. O toggle de opções `Reduzir movimento` guarda `MenuReducedMotion` e desativa a oscilação e o fade de entrada dos painéis. A função de onda foi verificada em três valores e o componente observado ativo em Play Mode; a validação da preferência entre sessões, do movimento ao longo do tempo e de conforto ainda está pendente.

`MenuCancel` encaminha o cancelamento para Voltar; foi verificado por comando no briefing e no dropdown de idioma fechado. O teste automatizado de dropdown aberto gerou uma exceção em `Dropdown.Show()` quando o tween ainda não estava inicializado; o código uGUI inicializa-o em `Dropdown.Start()`. É necessário repetir em interação física após a inicialização. A consulta final da Console não devolveu erros; não foi limpa.

### Checklist funcional

- [ ] Menu principal é o painel inicial; opções começam ocultas.
- [ ] Jogar abre SampleScene a partir de StartMenu e repõe o tempo.
- [ ] Options/Opções abre só o painel correto; a UI subjacente deixa de receber input.
- [ ] Voltar fecha opções e repõe foco num controlo válido.
- [ ] Português/Inglês atualiza todos os textos dos dois menus e mantém acentos legíveis.
- [ ] Volume mostra percentagem, altera som real e mute preserva o volume escolhido para unmute.
- [ ] Qualidade mostra os presets reais; resolução/fullscreen não alteram a janela só por abrir o painel.
- [ ] Preferências mantêm-se após fechar/reabrir opções e após iniciar uma nova sessão sem abrir opções.
- [ ] Navegação por rato, teclado e comando funciona sem submissões duplicadas.
- [ ] Layout, dropdowns e texto continuam legíveis a 16:9, 4:3 e numa janela pequena.
- [ ] Sair termina a build; no Editor, o método implementado termina Play Mode.

FullScreen e resolução devem ser verificados numa build standalone: o Game View não demonstra o mesmo comportamento de uma janela do sistema operativo.

## 4. Validação do gameplay

Executar apenas os testes relevantes à alteração, preservando os sistemas não relacionados:

- Dois jogadores: câmaras, viewports, luzes, inventários e identificação P1/P2.
- Inputs: testar com o número real de comandos pretendido; vários componentes usam dispositivos globais, por isso a associação por jogador não está garantida só pela existência de campos P1/P2.
- Interações: proximidade, raycast, abertura/fecho de pistas/keypad/cabos e foco dos painéis.
- Inventário: pickup aceite/recusado, seleção/uso de slots, bateria e lanterna correspondentes ao jogador.
- Temporizador: textos dos dois jogadores, chegada a zero e passagem à UI de derrota.
- Derrota: explosão, atraso, pausa, cursor e restart. Testar também erros de puzzle próximos do fim do tempo, pois há múltiplas origens possíveis para GameOver.
- Voz: permissão e dispositivo de microfone, push-to-talk de cada jogador, bateria, monitorização e efeito de volume/mute global. Não é teste de chat online.

Não apagar PlayerPrefs reais para testar defaults. Usar ambiente/perfil de teste ou preservar/restaurar os valores com autorização. Não remover EventSystems sem perceber se pertencem a contextos de jogador diferentes.

## 5. Assemblies, testes e build

- Não foram encontrados `.asmdef`/`.asmref` em `Assets`. A organização atual usa as assemblies padrão, com os scripts sob `Editor/` separados do runtime pelo Unity.
- Não foram encontrados testes próprios por ficheiros `*Test*` ou usos `[Test]`, `[UnityTest]`, `NUnit` nos fontes de `Assets`. O Test Framework instalado não constitui uma suite de testes aprovada.
- Não foi encontrada infraestrutura de CI em `.github/workflows` nem scripts versionados de build/test no levantamento. Não indicar `npm test`, `dotnet test` ou outro comando como fluxo existente do projeto.
- Não foi demonstrado um build profile próprio versionado; a ordem confirmada está em `ProjectSettings/EditorBuildSettings.asset`. Confirmar o perfil ativo no Editor antes de criar uma build.
- O mapa `scriptingBackend` consultado só explicita Android (`1`); não determina por si só o backend Windows. Confirmar Player Settings/plataforma no Editor em vez de assumir Mono ou IL2CPP.

### Procedimento de build manual

1. Garantir compilação sem erros e guardar conscientemente as cenas pretendidas.
2. Abrir **File → Build Profiles**, escolher **Windows 64-bit**, alvo do GDD, e confirmar que o módulo correspondente está instalado. Verificar DirectX 11 conforme o requisito; não alterar Graphics APIs como efeito secundário da documentação.
3. Confirmar as cenas incluídas e a ordem StartMenu → SampleScene. Perfis podem alterar a configuração: verificar o perfil efetivamente ativo.
4. Usar **Build** ou **Build And Run**, com saída numa pasta de build ignorada, não em Assets.
5. Exercitar menu, fullscreen/resolução, inputs, microfone e encerramento na aplicação construída.

Este é um procedimento recomendado, não uma build executada pela auditoria. Não há método de build customizado confirmado para invocar por `-executeMethod`.

### Quando acrescentar testes

Para lógica nova não trivial, criar um teste pequeno no Unity Test Framework e definir as test assemblies/referências necessárias. Separar testes EditMode de PlayMode conforme a dependência do ciclo Unity. Não introduzir uma arquitetura de assemblies extensa apenas para documentar ou alterar um rótulo.

O comando CLI do Test Framework só deve ser documentado como fluxo real depois de existirem testes e se verificar a descoberta/resultados. Não combinar `-quit` com um teste assíncrono supondo que isso aguarda a conclusão; consultar a documentação da versão instalada. Com o Editor aberto, preferir Test Runner/MCP em vez de iniciar uma segunda instância sobre o mesmo projeto.

## 6. MCP e skills locais

A configuração do OpenCode e os plugins não fazem parte da instalação Unity da equipa nem são distribuídos pelo Git:

| Caminho local | Conteúdo |
| --- | --- |
| `opencode.jsonc` | Servidor MCP `unity` e diretórios dos plugins. Tem caminho do projeto específico da máquina. |
| `.opencode/skills/unity-mcp/` | Procedimento de automação segura pelo MCP oficial. |
| `.opencode/skills/unity-menus/` | Regras de uGUI/TMP, layout e inputs dos menus. |
| `.opencode/skills/unity-options/` | Idioma, áudio, ecrã e persistência. |
| `.opencode/vendor/ponytail/` | Cópia local do plugin Ponytail. |
| `.opencode/vendor/superpowers/` | Cópia local do plugin Superpowers. |
| `.opencode/state/ponytail/` | Modo ativo e default exclusivos deste projeto. |
| `.opencode/LOCAL-PLUGINS.md` | Versões fixadas e adaptação local do estado do Ponytail. |

Noutra máquina, a documentação não instala estas ferramentas. Confirmar a disponibilidade das skills e do catálogo MCP antes de usá-las.

### Ligação oficial

1. Ter o pacote `com.unity.ai.assistant` instalado e o projeto aberto no Editor.
2. Em **Edit → Project Settings → AI → Unity MCP**, confirmar a bridge ativa e aprovar a ligação externa, se estiver pendente.
3. O cliente inicia `%USERPROFILE%/.unity/relay/relay_win.exe --mcp` com `--project-path` apontado para a sua cópia do projeto.
4. Verificar a ligação com leitura do projeto/cena/Console, não com uma alteração destrutiva.

O servidor oficial não é o mesmo produto que UnitySkills REST, CoplayDev ou IvanMurzak. Não instalar outro servidor para satisfazer nomes de ferramentas copiados de um tutorial.

O OpenCode recarrega a configuração local; se a interface mostrar um catálogo antigo, usar `/reload`. Não é necessário reiniciar o serviço apenas por editar uma configuração.

## 7. Git e organização

Antes de trabalhar:

```powershell
git status --short --branch
```

Após alterações, verificar o diff e espaços inválidos:

```powershell
git diff --check
git diff --stat
```

No PowerShell deste ambiente, não usar `&&`. Comandos dependentes precisam de verificação explícita do sucesso do anterior. Caminhos com espaços devem ser citados.

- Nunca fazer reset/checkout de ficheiros para ocultar alterações produzidas pelo Unity.
- Mover `.meta` com o asset; não regenerar GUIDs para resolver referências sem investigar a causa.
- Não editar `.csproj` gerados para adicionar fontes: o Unity determina as assemblies pela organização/asmdefs.
- Não renomear callbacks públicos/serializados sem procurar usos em cenas/prefabs e testar no Editor.
- Não incluir `.opencode/`, `opencode.json(c)`, credenciais ou caminhos absolutos pessoais em commits.
- Os documentos em `docs/` e `AGENTS.md` não são parte do setup local ignorado; a sua eventual inclusão num commit exige autorização do utilizador.

## 8. Riscos e decisões pendentes

Estes pontos vêm da leitura de implementação/configuração; não são falhas reproduzidas por esta auditoria:

| Ponto | Impacto / verificação necessária |
| --- | --- |
| Cena seguinte por índice | Reordenar a build altera o destino de Jogar. |
| Opções não gravadas em StartMenu | Scripts disponíveis não significam UI integrada; executar/validar builder e guardar a cena. |
| **TimerScript.gameOver nulo em SampleScene** | Referência confirmada no YAML; o código chama `gameOver.GameOver()` ao expirar sem null-check. A derrota pelo timer não está corretamente ligada no snapshot. |
| **PlayerInteraction P1.gameOver nulo** | Componente ativo; o ramo WrongCable chama essa referência. P2 tem o mesmo campo nulo, mas o componente está desativado. |
| Hotbar P1 desativada / Hotbar P2 ativa | Existem dois inventários configurados, mas não afirmar que ambos estão em execução. Há duas Battery com números 1/2. |
| Input global nos sistemas P1/P2 | Testar isolamento com hardware real antes de afirmar suporte a comandos independentes. |
| GameOver chamado de vários locais | Sem guarda central, pedidos simultâneos podem repetir explosão/coroutines; `WaitForSeconds` depende do tempo escalado. |
| Timer cruza zero antes de finalizar | Pode apresentar valor negativo por um frame; não assumir que a guarda do timer protege todos os outros callers de GameOver. |
| Cabos/formas sem dados suficientes | FormRandomizer não guarda zero filhos; logs do randomizador pressupõem três cabos. Configuração do prefab precisa ser validada. |
| Master volume global | Pode silenciar também voz; não há canais independentes de música/SFX implementados. |
| Divergências nome de ficheiro/classe | Não renomear mecanicamente; verificar MonoScripts, referências e callbacks. |
| Pastas/cenas adicionais | Finalidade de David2/Goncalo/Samuel/KaMoon_Room requer confirmação da equipa; nomes não são prova de autoria/obsolescência. |

O GDD final resolve parte das antigas dúvidas: vitória significa todos os módulos ativos antes de zero, o alvo principal é co-op local com rádio PTT e existe uma bateria central partilhada. **Continuam por confirmar**: âmbito online mencionado no roadmap, conjunto exato de módulos/terceiro puzzle, duração inicial, serial/tabelas de pistas, bloqueio temporário do keypad, transmissão simultânea/recuperação PTT, pareamento de dispositivos e métricas da vitória. A cena canónica de produção e volumes separados também não são definidos pelo GDD. Ver [GAME_DESIGN.md](GAME_DESIGN.md), que distingue especificações de ambiguidades.

As referências nulas/estados desativados acima são constatações de integração em disco, não erros observados em Play Mode nesta auditoria. Corrigi-los é uma tarefa separada; esta documentação não alterou os assets para os ocultar.

## 9. Critérios de aceitação do GDD — ainda por executar

Esta checklist traduz o design pretendido em verificações futuras. Não marca funcionalidades como concluídas; os desvios atuais estão no PROJECT_MAP. Antes de testar, resolver referências de cena necessárias e confirmar os parâmetros de design em falta.

### Cooperação, salas e dispositivos

- [ ] P1 só obtém pistas/diagramas e P2 executa módulos sem acesso às soluções; nenhum jogador resolve sozinho com a informação da sua sala.
- [ ] Vidro permite contacto visual mas impede passagem e transferência de objetos; escuridão/lanternas tornam a negociação de visão relevante.
- [ ] Inputs de P1/P2 estão isolados com a combinação de dispositivos aprovada. E/A interage, F/Y gere lanterna, V/**LT** mantidos controla rádio, Esc/B cancela; D-pad e clique navegam nos puzzles.
- [ ] Abrir puzzle fullscreen bloqueia look; cancelar, resolver ou terminar a partida restaura o estado de input adequado, sem ações duplicadas por EventSystem/navegador manual.

### Energia: verificar lógica isolada e integração

- [ ] Existe uma só carga partilhada, limitada a 0..100%; ambos veem o mesmo valor. A gestão não mantém reservas independentes ocultas por jogador.
- [ ] Ativar uma lanterna desliga a outra sem frame de sobreposição; ambas podem permanecer apagadas para recarregar.
- [ ] Partindo de 100%, após 10 s de tempo de jogo: só lanterna → **85%**, só rádio → **70%**, ambos → **55%** (tolerância numérica pequena, sem depender do FPS).
- [ ] Com tudo desligado, partir de 0% e esperar 5 s → **10%**, com dispositivos ainda bloqueados; 7,5 s → **15%**, atingindo o limiar de recuperação. A forma de reativar segue decisão explícita, não uma suposição do teste.
- [ ] Uso impede recarga simultânea; idle repõe +2 pontos percentuais/s sem ultrapassar 100%. A 0%, rádio/luzes cortam e não reativam antes de 15%.
- [ ] Tentativas simultâneas de rádio seguem política aprovada; pausar/reiniciar não duplica drenagem nem deixa luz/transmissão em estado inconsistente.

### Puzzles, relógio e fim

- [ ] CRT mostra exatamente quatro cabos verticais vermelho/azul/amarelo/verde; a resposta segue uma tabela aprovada baseada no serial e visível como pista a P1.
- [ ] Cabo correto resolve o módulo uma vez; cabo errado desencadeia falha crítica imediatamente, independentemente de um atraso posterior da animação/UI de derrota.
- [ ] Keypad tem matriz 3×4/12 botões, quatro dígitos, CLR/OK e notas coerentes com a solução; código correto resolve uma vez.
- [ ] Código errado desconta **20 s** do tempo global e bloqueia pelo intervalo aprovado. Não provoca explosão direta quando ainda há tempo; se esgotar o tempo, termina por timeout sem estados de vitória concorrentes.
- [ ] Vitória só ocorre com todos os módulos ativos desativados e tempo positivo; um módulo futuro/inativo não impede conclusão. A precedência de eventos simultâneos deve estar decidida antes do teste.
- [ ] Relógio central MM:SS não fica negativo; duração inicial é a aprovada pela equipa, não automaticamente os 60 s do código atual.
- [ ] Fluxo final: menu → briefing → partida; falha → explosão/Game Over → Play Again; sucesso → tempo/erros/eficiência de bateria → menu. Métricas usam definições aprovadas e reinício limpa o estado da partida.

### Apresentação e hardware alvo

- [ ] HUD mostra bateria central com percentagem/estado e timer no topo central; fullscreen e split-screen não ocultam controlos.
- [ ] Spotlights têm cone 60°, alcance 20 m, atenuação suave e sombras dinâmicas em URP.
- [ ] Rádio usa bandpass/ruído/cliques PTT; teste distingue bandpass de lowpass. Verificar microfone/saída e efeito do volume/mute sobre o rádio.
- [ ] Pulso do timer acelera **abaixo de 60 s**; cortes, teclado e explosão têm feedback; derrota inclui camera shake segundo parâmetros aprovados.
- [ ] Build Windows 10/11 64-bit/DirectX 11 é exercitada nos mínimos declarados (8 GB RAM, CPU/GPU do GDD, 2 GB disponíveis), registando hardware e medições reais. Não afirmar FPS/meta de desempenho não especificados.

O módulo C (<1 s entre chaves) só entra em aceitação quando a expansão fizer parte do âmbito aprovado. Online, procedural e outras plataformas não são adicionados à checklist obrigatória sem decisão de âmbito.

## 10. O que uma entrega deve declarar

Separar sempre:

1. Alterações efetivamente feitas e ficheiros tocados.
2. Compilação C# validada ou não.
3. Console e referências inspecionadas ou não.
4. Testes automatizados executados, com resultado real; ausência de testes não equivale a sucesso.
5. Play Mode, hardware/inputs e validação visual efetivamente exercitados.
6. Cenas/assets guardados versus alterações ainda só no Editor.
7. Build standalone executada ou não e limitações conhecidas.
8. Requisitos GDD atendidos, desvios ainda existentes e decisões de design em falta; roadmap/declarado no PDF não substitui evidência de implementação.

## 11. Referências de procedimento

- [Unity: argumentos da linha de comandos do Editor](https://docs.unity3d.com/6000.0/Documentation/Manual/EditorCommandLineArguments.html).
- [Unity MCP oficial: início rápido](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.0/manual/unity-mcp-get-started.html).
- [OpenCode V2: instruções AGENTS.md](https://opencode.ai/v2/docs/instructions).

As referências externas explicam a plataforma. O comportamento do KaMoon deve ser confirmado nos fontes, nas configurações e nas cenas atuais.
