# Redesign da hotbar — consola de contenção (design curto)

**Data:** 2026-11-17 · **Branch:** `feature/hotbar-design`

## Intenção

A hotbar atual são quadrados translúcidos com um número grande e a inicial do item. O jogo é uma estação lunar escura, sci-fi retro, com o menu principal em Rajdhani e âmbar. Queremos que a barra pareça o mesmo universo e que o jogador reconheça **o item** dentro de cada caixa, não o número.

## Objetivos

- Cada slot mostra o ícone do item que o jogador tem (bateria, lanterna, chave de fendas).
- Estilo «painel de instrumentos»: moldura angular, canto cortado, Rajdhani em maiúsculas, âmbar no slot selecionado.
- Número do slot reduzido ao canto superior esquerdo.
- Continuar exatamente como está: Canvas guardado na cena, âncoras de split-screen, crosshair, input (1–4/G, d-pad/R3).
- Uma linha fina de carga por baixo dos slots, ligada à `Battery.Charge` real — nunca decorativa.

## Não objetivos

- Mexer em bateria central, cronómetro, HUD global, menu, lobby ou paleta fora da hotbar.
- Criar UI em runtime.
- Rebinding de teclas ou novos comportamentos de input.
- Novas dependências, pacotes ou assets gerados por IA.

## Constraints

- Unity 6000.6.2f1, uGUI + `Text` legado (o Rajdhani TMP é do menu; a hotbar usa `LegacyRuntime.ttf` e isso mantém-se para não mudar a fonte em runtime).
- Paleta do `ModeMenuBuilder.cs:18-21`: painel `0.015,0.04,0.055,0.98`; texto `0.87,0.94,0.91`; secundário `0.62,0.72,0.7`; âmbar `0.9,0.6,0.25`.
- `HotbarItem.icon` já existe e é a fonte oficial dos ícones; os três assets estão com `icon: {fileID: 0}`.
- `HotbarBuilder` já cria e liga `HotbarCanvasP1/P2` em SampleScene, David e Goncalo; tem de continuar idempotente.

## Abordagens

- **A — estender `HotbarBuilder` (escolhida).** O builder passa a gerar o painel com moldura/canto cortado, ícones e linha de carga; os ícones e a moldura são sprites simples desenhados por código no Editor e atribuídos aos três `HotbarItem`. O `Hotbar.cs` só recebe uma referência extra para a linha de carga e a atualiza com a bateria real. Menor diff, reutiliza o que já está validado.
- **B — prefab `HotbarCanvas`.** Mais reutilizável no futuro, mas acrescenta GUIDs, um novo builder e mais ficheiros sem benefício agora (as duas barras são iguais).
- **C — construir em runtime.** Rejeitada: contraria o requisito de o Canvas estar na cena.

## Decisões de implementação

- Sprite da moldura com canto inferior direito cortado, desenhado por código; `Image` com esse sprite e cor do painel.
- Slot selecionado: contorno âmbar com brilho suave (`UnityEngine.UI.Shadow`/`Outline` ou Image extra); slots vazios com traço ciano discreto.
- Ícones: 32×32, traço âmbar/ciano, gerados no Editor e guardados em `Assets/Hotbar/Icons/`; atribuídos a `Battery.asset`, `Flashlight.asset`, `Screwdriver.asset`.
- Número: Rajdhani do `LegacyRuntime.ttf`, 11 px, canto superior esquerdo; ícone centrado.
- Linha de carga: `Image.fillMethod = Horizontal`, `fillAmount = Battery.Charge` do jogador correspondente; quando não houver `Battery`, fica a 0.
- Âmbar é reservado para seleção e ícone ativo; o resto do painel usa ciano escuro/texto claro.

## Revisão de direção visual (aprovada pelo utilizador)

O utilizador pediu «algo mais no tema do jogo» e escolheu **consola de contenção**. É uma alteração **apenas visual**: não há texto, dados da partida, nova lógica nem novos comportamentos. O utilizador pediu explicitamente «só o desenho».

Mudanças face à versão já implementada:

- Faixa superior com riscas âmbar/preto de aviso (sem texto).
- Placa metálica escura por trás dos slots, com rebites nos cantos e uma textura estrelada muito subtil desenhada por código — decisão do agente, reversível.
- **Sem cabeçalho textual:** o utilizador pediu «só o desenho»; a hotbar não mostra «CONTENÇÃO ATIVA», «P1 · SISTEMAS» nem qualquer outra frase.
- Slots com moldura mais pesada e canto cortado; seleção âmbar com brilho mantida.
- Linha de carga com marcas de escala (continua ligada à bateria real).
- Ícones dos itens mantidos, com traço mais «equipamento» (bateria, lanterna, chave de fendas).

Tudo continua a ser desenhado por código no `HotbarArt` e gerado pelo `HotbarBuilder` na cena; nada é criado em runtime e as âncoras/input/crosshair não mudam.

## Direção intermédia — Estação 2087 (substituída abaixo)

Depois de auditoria ao estilo do jogo, o utilizador escolheu **Estação 2087** entre duas opções. Substitui a direção «consola de contenção» acima no que contradisser.

Identidade a aplicar (valores do `ModeMenuBuilder.cs:18-21,316-325`):

| Elemento | Valor |
| --- | --- |
| Placa/borda | `#040A0E` (`0.015,0.04,0.055,0.98`) com contorno `#142B2E` (`0.08,0.17,0.18`) |
| Slot vazio | `#0F1F21` (`0.06,0.12,0.13`) |
| Slot selecionado | `#A66629` (`0.65,0.4,0.16`) — a cor de botão selecionado do menu |
| Ícone / número | `#DEF0E8` no selecionado; `#9EB8B2` (`0.62,0.72,0.7`) nos restantes |
| Faixa de aviso | 4 px, âmbar `#E69940` (`0.9,0.6,0.25`) / escuro, sem texto |
| Linha de carga | `#E69940` sobre `#142B2E`, marcas `#9EB8B2` |
| Tipografia | Rajdhani 10–11 px, números no canto superior esquerdo |

Correções obrigatórias face ao estado atual:

- Fundo do slot selecionado deixa de ser `#FFD933` e passa a `#A66629`; o âmbar canónico `#E69940` fica só na faixa, linha de carga e brilho.
- O canto cortado tem de existir no PNG: a condição atual `x>23 && y<8 && x+y>42` é inalcançável (máx. 38) e deve ser substituída por um corte real no canto inferior direito.
- A placa passa a cinzento/azul escuro plano — remover o pó de estrelas do `panel-plate.png`; o campo estelado pertence ao fundo do menu, não ao painel da hotbar.
- Reduzir os quatro rebites a um único rebite discreto no canto da placa.
- Manter: sem texto, sem lógica nova, Canvas na cena, âncoras/crosshair/input intactos.

## O que fica para o implementador

Editar `Editor/HotbarBuilder.cs`, gerar os sprites, atualizar `Hotbar.cs` com o campo da linha de carga, correr o builder nas três cenas e guardá-las. Verificar no Editor e em Play Mode que o número de canvases não muda, que os ícones aparecem nos slots e que a linha de carga acompanha a bateria. Sem commits.

## Direção efetiva final — Sistema do menu (plano)

O utilizador mostrou uma captura e disse que a hotbar continuava «da maneira antiga». Interpretação: a direção Estação 2087 (placa, faixa de aviso, rebites, moldura com canto) era demasiado semelhante ao que já estava e foi rejeitada na prática. A direção final é a outra opção dos mockups, **Sistema do menu**, que é a mais fiel ao menu principal.

Estado final:

- **Sem placa, sem faixa de aviso, sem rebites, sem moldura com canto, sem brilho, sem texto.**
- Quatro retângulos lisos: vazio `#0F1F21`, com item `#142B2E`, selecionado `#A66629`.
- Fundo estrelado do menu (`Assets/Materials/menu.png`) por trás dos slots, opacidade 0,1 — o motivo que liga a hotbar ao menu.
- Cada slot usa um sprite 64×64 desenhado por código (`slot-empty`, `slot-item`, `slot-selected`) com degradé suave, cantos arredondados e borda de 2 px na paleta canónica. O `Hotbar` troca o sprite em `RefreshUI()` conforme o estado.
- Slots de 72 px com 10 px de espaçamento; **sem números nas caixas** — o `Label` foi removido do builder e `RefreshUI()` já não escreve texto.
- Ícones 64×64 com traço anti-aliased e mais detalhe (bateria com terminal e raio, lanterna com cone e interruptor, chave de fendas com ranhuras), centrados com margem 4 px.
- **Sem linha acima nem abaixo dos slots**: o utilizador identificou o divisor ciano e a linha de carga com marcas como o que parecia estranho; ambos foram removidos do builder.
- Canvas na cena, âncoras/input/crosshair intactos. O campo `chargeFill` ficou no `Hotbar.cs` mas sem objeto ligado (`AtualizarCarga()` ignora referências nulas).
- As secções anteriores deste documento são históricas; esta é a direção efetiva.

Os sprites antigos (`slot-frame`, `warning-stripe`, `panel-plate`) deixaram de ser usados, mas os ficheiros foram mantidos para não regenerar GUIDs sem necessidade.

**Nota final (branch `fix/hotbar`):** a pedido do utilizador, a hotbar passou a ser **100% montada no Editor**: os geradores `HotbarArt.cs` e `HotbarBuilder.cs` foram removidos; os `HotbarCanvasP1/P2` e os sprites existem como assets/objetos normais na cena. O `Hotbar.cs` mantém só o comportamento em jogo, incluindo esconder/desativar a hotbar alheia em multiplayer.
