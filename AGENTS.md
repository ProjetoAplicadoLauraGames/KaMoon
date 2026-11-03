# KaMoon — contexto para agentes

## Começar aqui

- Comunicar em português; distinguir factos verificados, inferências e testes ainda não executados.
- Projeto Unity `6000.6.2f1`, com URP e uGUI/TextMeshPro. A lista de build consultada começa em `Assets/Scenes/StartMenu.unity`, seguida de `Assets/Scenes/SampleScene.unity`; confirmar perfil/cenas atuais antes de executar.
- Ler [o design pretendido](docs/GAME_DESIGN.md), [o mapa do projeto](docs/PROJECT_MAP.md) para localizar sistemas e referências, e [o guia de desenvolvimento](docs/DEVELOPMENT.md) antes de executar ou testar.
- Os documentos são um mapa, não substituem a inspeção dos ficheiros atuais. Confirmar versões, cenas e referências antes de editar; atualizar a documentação quando uma mudança alterar o fluxo ou a organização.
- StartMenu tem opções e briefing guardados; só a abertura/fecho dos painéis foi exercitada por comando em Play Mode. Validação visual, inputs físicos e persistência continuam pendentes. O snapshot de SampleScene tem referências `gameOver` nulas no timer/interação P1; não corrigir fora do âmbito pedido.

## Contrato de jogo — GDD final

- Co-op local assimétrico para dois astronautas em primeira pessoa/split-screen, Windows 10/11 de 64 bits, Unity 6 URP. P1 lê pistas na Sala 1; P2 executa o desarmamento na Sala 2, sem acesso às soluções. Vidro permite visão, não passagem/transferência física.
- No máximo uma lanterna ativa; **uma bateria central partilhada**, não reservas por jogador. Drenagem: lanterna −1,5 pontos percentuais/s, rádio −3,0, ambos −4,5; recarga +2,0 apenas com tudo desligado. A 0% cortar ambos e bloquear até recuperar 15%.
- CRT: quatro cabos vermelho/azul/amarelo/verde, solução por número de série/pista de P1; cabo errado detona. Keypad 3×4, quatro dígitos; erro retira **20 s** e bloqueia temporariamente, não detona diretamente salvo esgotamento do tempo.
- Rádio PTT: **V/LT mantidos**. Puzzles fullscreen bloqueiam rotação da câmara. Vitória exige todos os módulos ativos antes de zero; briefing e painel de vitória/estatísticas fazem parte do fluxo pretendido.
- Preservar a separação entre **requisito GDD**, **estado implementado** e **estado testado**. Os defaults do protótipo (60 s, baterias individuais, RB para rádio, erro de keypad fatal) não substituem o GDD.
- Há ambiguidades documentadas sobre online versus local, terceiro puzzle/expansão, soluções e temporizações. Pedir decisão antes de implementar o ponto afetado; não expandir o âmbito desta tarefa para resolver tudo.

## Execução até à conclusão

- Quando o utilizador pedir para implementar uma tarefa ou plano, assumir responsabilidade pela entrega completa: percorrer todos os requisitos acordados, integrações, testes e documentação relevantes, não apenas o primeiro passo funcional.
- Definir e acompanhar critérios de conclusão. Depois de cada etapa, continuar autonomamente para a seguinte; um plano escrito, uma alteração parcial ou uma mensagem de progresso não substituem a implementação pedida. Não pedir autorização para continuar passos já autorizados.
- Procurar qualidade adequada ao objetivo, incluindo acabamento, usabilidade e validação quando solicitados. Escolher a solução mais simples que cumpra **todos** os requisitos, não reduzir o âmbito ou a ambição para produzir o mínimo. Não acrescentar funcionalidades fora do pedido.
- Resolver dificuldades recuperáveis: investigar a causa, corrigir e voltar a verificar; tentar alternativas seguras disponíveis. Se uma ferramenta ou teste específico estiver indisponível, continuar com o restante trabalho independente e indicar claramente a validação em falta.
- Interromper para pedir decisão apenas perante uma ambiguidade que altere materialmente o resultado, uma ação que exija autorização, risco de perda de trabalho/dados ou outro bloqueio real que impeça avançar com segurança. Limitações de ferramentas ou de tempo não são prova de conclusão.
- Antes da resposta final, confrontar o resultado com cada requisito e executar as verificações possíveis. Se ainda houver trabalho autorizado e exequível por fazer, continuar em vez de terminar com uma entrega parcial. Se for realmente impossível concluir, explicar o bloqueio concreto, o que foi tentado, o que falta e como retomar; nunca apresentar trabalho não verificado como concluído.
- Estas regras não autorizam commits, operações destrutivas, alterações fora do âmbito, geração de assets sem consentimento ou violação das restantes regras de segurança deste ficheiro.

## Qualidade de planos e entregas visuais

- Interpretar pedidos como «espetacular», «profissional» ou «step up» como requisitos de qualidade e acabamento, não como permissão para entregar um protótipo genérico. Maximizar a qualidade **dentro do âmbito e das ferramentas autorizadas**, não o volume de código ou de texto.
- Antes de executar um plano de interface visual, confrontá-lo com o pedido e o estado atual. Um plano vago não basta: detalhar a direção artística derivada do GDD, composição de cada ecrã, hierarquia visual, tipografia, paleta, iluminação/ambiente, assets identificados, estados interativos, movimento e áudio quando relevantes, fluxos, acessibilidade e comportamento em diferentes resoluções.
- Dividir o plano em entregas verificáveis, identificando ficheiros/cenas afetados, referências existentes a preservar, critérios de aceitação por requisito e métodos concretos de teste e captura. Distinguir requisitos aprovados de escolhas de implementação; não substituir um plano incompleto por suposições apresentadas como decisões do utilizador.
- Inspecionar os assets existentes e selecionar os que contribuem para a identidade do jogo; não tratar texto decorativo, um fundo genérico e controlos por defeito como equivalente a um menu visualmente acabado. Reutilizar assets adequados, criar elementos por meios autorizados ou pedir consentimento quando a geração de assets o exigir.
- Iterar sobre o resultado real: capturar ou observar os ecrãs efetivamente renderizados, incluindo UI overlay, avaliar composição, legibilidade, contraste, estados, recorte e consistência em 16:9, 4:3 e janela pequena, corrigir problemas encontrados e voltar a verificar. Uma inspeção de hierarquia, callbacks ou captura de câmara sem overlay não prova qualidade visual.
- Exercitar os fluxos completos e os inputs relevantes em Play Mode, incluindo idiomas, foco, opções e persistência, e validar numa build quando o comportamento do sistema operativo o exigir. Distinguir testes automatizados, interação simulada e hardware físico efetivamente testado.
- Não declarar um menu «acabado», «polido» ou conforme ao plano sem evidência funcional **e visual** proporcional ao pedido. Se a visualização ou um teste essencial estiver indisponível, tentar alternativas seguras e reportar a limitação concreta; continuar o trabalho independente e não alegar que o critério foi satisfeito.

## Limites de trabalho

- Verificar `git status --short --branch` antes de alterações. Preservar trabalho existente, incluindo ficheiros não rastreados e alterações feitas pelo Unity.
- Não fazer commits/push, mudar identidade Git, instalar dependências, trocar de branch ou descartar alterações sem autorização explícita.
- Ler o sistema afetado e os seus callers antes de alterar comportamento. Reutilizar os padrões existentes e limitar o diff ao pedido.
- Scripts próprios: `Assets/Scripts/`. Scripts exclusivos do Editor: `Assets/Scripts/Editor/`. Não mover código de terceiros/tutorial nem reorganizar outros sistemas por iniciativa própria.
- Mover assets com os seus `.meta` para preservar GUIDs; com o Editor disponível, preferir `AssetDatabase.MoveAsset`.
- Nomes de classes, campos serializados e métodos usados por `UnityEvent` são contratos com cenas/prefabs. Inspecionar e preservar referências ao renomear; usar migração de serialização quando necessária.
- Preferir APIs do Unity/MCP a edições manuais de YAML de cenas e prefabs. Não editar `Library/`, `Temp/`, `Logs/` ou ficheiros de projeto gerados como fonte de configuração.
- No Windows PowerShell deste ambiente, não usar `&&`; verificar códigos de saída antes de executar operações dependentes.

## Skills Unity instaladas localmente

Estas skills estão em `.opencode/skills/`, ignoradas pelo Git. Carregá-las pela ferramenta `skill` quando o assunto se aplicar; a presença deste ficheiro não as instala noutra máquina.

| ID | Localização | Usar para |
| --- | --- | --- |
| `unity-mcp` | `.opencode/skills/unity-mcp/SKILL.md` | Inspeção e automação segura pelo MCP oficial: projeto/cena corretos, Undo, referências, compilação e Console. |
| `unity-menus` | `.opencode/skills/unity-menus/SKILL.md` | uGUI, TextMeshPro, CanvasScaler, painéis, callbacks e navegação por rato/teclado/comando. |
| `unity-options` | `.opencode/skills/unity-options/SKILL.md` | Idioma dos menus, volume/mute, ecrã, qualidade e persistência das preferências. |

Também estão instalados localmente Ponytail (`full` por defeito) e Superpowers. Usar as skills de processo aplicáveis, sem duplicar aqui os seus conteúdos. Detalhes e versões: `.opencode/LOCAL-PLUGINS.md` (local).

## MCP Unity

- Servidor configurado: **`unity`**, do pacote oficial `com.unity.ai.assistant`. Não confundir com servidores CoplayDev, IvanMurzak ou UnitySkills REST: não são a integração instalada neste projeto.
- Configuração: `opencode.jsonc`, local e ignorada. Transporte stdio pelo relay `%USERPROFILE%/.unity/relay/relay_win.exe --mcp`, com o projeto selecionado por `--project-path`.
- Descobrir nomes e schemas das ferramentas na sessão; não inventar ferramentas nem reutilizar parâmetros de outro servidor.
- Capacidades expostas no catálogo desta instalação: executar C# no Editor, ler a Console, capturar câmaras/vistas e consultar/gerar assets por IA. A disponibilidade depende do catálogo ativo e das permissões; catálogo não equivale a teste de cada ferramenta.
- Antes de escrever: confirmar `Application.dataPath`, cenas abertas/ativa, alterações não guardadas, Play Mode e compilação. Não mudar/fechar cenas com trabalho por guardar nem sair de Play Mode sem avaliar o impacto e obter autorização quando necessário.
- Registar criações/modificações/destruições com os mecanismos de Undo/tracking suportados pela ferramenta. Uma operação bem-sucedida não prova que a cena ficou guardada.
- Não limpar a Console, apagar preferências ou alterar assets para facilitar testes sem autorização. Geração de assets exige pedido explícito e o consentimento requerido pela ferramenta.
- Preferências e ferramentas do Assistant dentro do Unity não são automaticamente skills do OpenCode. Se a ligação estiver indisponível, reportar o bloqueio; não fingir que ações do Editor foram executadas.

## Verificação e documentação

- Após mudanças C#, confirmar compilação e inspecionar a Console. Para lógica nova não trivial, acrescentar um teste pequeno ou self-check adequado.
- Verificar referências serializadas e comportamento no Editor; para UI, verificar layout, foco, dropdowns, idioma e inputs relevantes. Uma captura de câmara pode não mostrar UI Screen Space Overlay.
- Testar resolução/fullscreen numa build quando necessário; não apresentar comportamento do Game View como prova de comportamento standalone.
- Reportar separadamente: compilação, execução do comando, testes, Play Mode, captura visual e gravação da cena. Não afirmar validações não executadas.
- `.opencode/`, `opencode.json` e `opencode.jsonc` são setup local ignorado pelo Git. Não adicionar caminhos absolutos pessoais ou segredos à documentação partilhável.
- A entrega deve indicar o que mudou, o que foi efetivamente verificado e os limites/riscos ainda existentes.
