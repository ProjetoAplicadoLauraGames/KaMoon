# KaMoon! — jogo pretendido pelo GDD final

> Fonte: GDD final fornecido pelo utilizador nesta conversa, com 11 páginas, texto extraído e imagens, incluindo o diagrama de Game Flow. Esta é uma síntese para implementação, não uma transcrição integral nem prova de funcionalidades concluídas. O PDF original não foi adicionado ao repositório nesta tarefa.

Este documento define **o pretendido**. [PROJECT_MAP.md](PROJECT_MAP.md) descreve **o implementado/serializado** e os desvios identificados; [DEVELOPMENT.md](DEVELOPMENT.md) contém procedimentos e critérios de verificação. Não substituir os factos do código por afirmações do GDD sobre trabalho concluído.

## 1. Identidade e plataforma — GDD §1

- **Título:** KaMoon!
- **Slogan:** Save the Moon or it Goes Boom!
- **Equipa:** LAURA Games.
- **Autores:** David Barbosa, Gonçalo Sousa e Samuel Furtado.
- **Experiência:** co-op local assimétrico para dois jogadores, em primeira pessoa e ecrã dividido; puzzle/escape game/thriller sci-fi.
- **Objetivo:** dois astronautas coordenam luz, comunicação e ações para desarmar uma ogiva numa estação lunar antes da detonação.
- **Plataforma alvo:** PC, Windows 10/11 de 64 bits; Unity 6 com URP.
- **Público:** 12–35 anos, apreciadores de cooperação e comunicação sob pressão. Referências: Keep Talking and Nobody Explodes, We Were Here e Operation: Tango.

### Requisitos mínimos declarados, não medidos

| Componente | GDD |
| --- | --- |
| Sistema operativo | Windows 10, 64 bits. |
| CPU | Intel Core i5-4460 / AMD FX-6300. |
| RAM | 8 GB. |
| GPU | NVIDIA GeForce GTX 1050 (2 GB) / AMD Radeon RX 560. |
| API gráfica | DirectX 11. |
| Armazenamento | 2 GB disponíveis. |

Não são benchmarks nem garantia de desempenho. O GDD não fixa FPS, resolução de referência ou orçamento de memória além destes mínimos.

## 2. Mundo, espaço e assimetria — GDD §§2–3

**Premissa:** em 2087, uma expedição no lado oculto da Lua investiga transmissões encriptadas de um posto militar desativado. A entrada ativa contenção e autodestruição; o reator de emergência falha e deixa a estação às escuras. Os astronautas precisam desarmar a bomba e regressar vivos.

| Papel | Localização, informação e ação |
| --- | --- |
| **P1 — Operador de Sistemas / Pistas** | Sala 1, Comando/Pistas. Consulta armários, manuais, notas, placares e diagramas. Analisa e transmite instruções; não tem acesso direto à bomba/fios. |
| **P2 — Especialista em Desminagem / Execução** | Sala 2, Núcleo da Bomba/Terminais. Interage com bomba, cabos e teclado. Não possui os diagramas/soluções necessários para resolver sozinho. |

- As câmaras estanques são separadas por vidro reforçado, resistente a impacto e à prova de som: há contacto visual, mas não passagem de jogadores nem transferência física de objetos.
- Nenhuma sala contém informação suficiente para resolver um módulo isoladamente. Não apresentar ao executor as respostas reservadas ao analista.
- Ambiente severamente escuro, teto sem iluminação funcional; a leitura dos elementos interativos depende das lanternas de capacete.
- A barreira sonora é uma regra do mundo/áudio; o GDD não explica como impedir conversa ou observação da metade do ecrã do parceiro no mesmo espaço físico. Não inventar essa capacidade técnica.

## 3. Luz e energia partilhada — GDD §4 e §7

### Exclusividade de visão

No máximo **uma lanterna ligada** de cada vez. Ativar P1 desliga imediatamente P2, e vice-versa. Também deve ser possível ficar com **ambas desligadas**, pois a recarga exige ausência de consumo. O GDD não especifica completamente a semântica de desligar a própria lanterna.

Lanternas: Spotlights de capacete URP, **cone de 60°**, **alcance de 20 m**, atenuação suave e sombras dinâmicas.

### Shared Power Cell

Existe **uma única carga central de 100%**, comum a ambos os jogadores e aos sistemas de lanterna/rádio; não duas reservas independentes por jogador.

| Estado | Variação da carga por segundo |
| --- | ---: |
| Apenas lanterna ligada | −1,5 pontos percentuais/s. |
| Apenas rádio PTT ativo | −3,0 pontos percentuais/s. |
| Lanterna + rádio simultâneos | −4,5 pontos percentuais/s. |
| Todas as lanternas e rádio desligados | +2,0 pontos percentuais/s. |

Se a representação for normalizada em 0..1, as taxas equivalentes são 0,015; 0,030; 0,045 e 0,020 por segundo. Não confundir pontos percentuais com redução multiplicativa da carga restante.

- A carga deve permanecer no intervalo 0..100%.
- Ao atingir **0%**, cortar rádio e luzes. O bloqueio de recuperação mantém-se até atingir **15%**, não basta recuperar um valor positivo.
- A recarga é passiva e só ocorre com os dois sistemas desligados; usar um deles não pode recarregar ao mesmo tempo.
- A recuperação até 15% não determina por si só reativação automática dos dispositivos: o GDD não diz se é preciso novo input, nem o que acontece a um PTT mantido premido durante a recuperação.
- O GDD não define acumulação de consumo se os dois jogadores tentarem transmitir simultaneamente. Não assumir custo duplo ou protocolo half-duplex sem confirmação.

## 4. Módulos e penalizações — GDD §5

### A — Quatro cabos no CRT

- Interface de puzzle em ecrã inteiro num ecrã CRT retro.
- **Quatro condutores verticais:** vermelho, azul, amarelo e verde.
- P2 navega com indicador de seleção e escolhe o cabo a cortar.
- P1 lê uma folha de especificações no armário de emergência da Sala 1; a solução depende do **número de série da estação**.
- Cabo correto desativa o módulo; cabo errado é erro crítico e causa **detonação imediata**, não apenas uma penalização de tempo.
- O GDD não fornece a tabela número-de-série → cor correta nem a geração/representação do número de série.

### B — Terminal criptográfico

- Monitor industrial de fósforo verde; teclado de **12 botões**, `0–9`, `CLR`, `OK`, em matriz **3×4**.
- P2 introduz/submete um código de **quatro dígitos**; P1 compila referências numéricas das notas da Sala 1.
- Cada código errado subtrai **20 segundos** ao tempo global e **bloqueia temporariamente** o terminal.
- Não transformar todo erro do keypad em detonação direta. Se uma penalização esgotar o tempo, aplicar a regra de derrota por tempo.
- O GDD não fixa o código/tabela de pistas, a duração do bloqueio ou o tratamento de submissões incompletas/durante bloqueio.

### C — Expansão futura

Disjuntores de pressão/alavancas binárias: ambos acionam chaves com **menos de 1 segundo de intervalo**. Está explicitamente marcado como expansão futura; não o incluir automaticamente no conjunto obrigatório de módulos ativos da versão atual. Mais módulos são uma intenção futura sem especificação concreta.

### Regra geral e exceções explícitas

O parágrafo geral menciona perda de tempo variável com gravidade, mas o módulo A descreve detonação imediata e o B especifica −20 s + bloqueio. Preservar esses casos explícitos; não uniformizar todas as penalizações a partir da frase genérica. Erros não descritos exigem decisão da equipa.

## 5. Fim da partida e Game Flow — GDD §4 e diagrama §8

- **Vitória:** todos os módulos **ativos** desativados antes de `00:00`.
- **Derrota:** tempo esgotado ou erro crítico irrecuperável, como cabo errado.
- O GDD não fixa duração inicial do cronómetro. O default atual de 60 s no projeto não é um requisito do GDD.

```text
Game Start → Menu Principal → Briefing → Início da partida
  → loop de investigação, comunicação e desarmamento
  → condição final
       ├─ falha → explosão + Game Over → Play Again → início da partida
       └─ sucesso → painel de vitória (tempo, erros, eficiência da bateria)
                    → Menu Principal
```

O diagrama especifica ecrãs/transições, não exige uma cena Unity por ecrã. A fórmula de eficiência da bateria e as restantes métricas não estão definidas. É também necessário decidir a precedência se conclusão do último módulo e expiração ocorrerem no mesmo instante.

## 6. Inputs e apresentação — GDD §6

| Ação | Teclado/rato | Comando |
| --- | --- | --- |
| Movimento | W/A/S/D | Analógico esquerdo. |
| Rotação da câmara | Movimento do rato | Analógico direito. |
| Navegação UI/puzzles | Clique do rato | D-pad, quatro direções. |
| Interação/ação | E | A / South. |
| Lanterna | F | Y / North. |
| Rádio push-to-talk | **V mantido** | **LT / gatilho esquerdo mantido**. |
| Cancelar/sair da vista | Esc | B / East. |

Os dois papéis admitem teclado/rato ou comando no texto, mas a combinação de dispositivos por sessão e o seu pareamento não estão especificados. Não assumir que dois jogadores locais podem partilhar um único rato de forma independente.

HUD pretendido:

- Barra **central partilhada** de bateria, visível a ambos, com percentagem e estados de drenagem.
- Cronómetro digital **MM:SS** no topo do visor central.
- Puzzles em Canvas de ecrã inteiro, com **rotação da câmara temporariamente bloqueada**; ao sair, devolver o controlo apropriado. Não basta abrir um painel sobre uma câmara que continua a rodar.

## 7. Áudio e feedback — GDD §§2 e 7

- Rádio de voz **local**, ativado por botão, com **bandpass** (banda estreita), estática/ruído e cliques PTT para efeito walkie-talkie.
- Ambiente minimalista de condutas/reverberação, estática e pulso do cronómetro. A cadência acelera quando restam **menos de 60 s**.
- Cortes de fio e cliques metálicos do teclado têm feedback sonoro tátil.
- Derrota termina em explosão com **camera shake violento**, além dos efeitos visuais/áudio. Parâmetros de intensidade/duração e opções de conforto não são definidos pelo GDD.
- Microfone, destino de reprodução entre jogadores e política de escuta/monitorização precisam de desenho técnico; a presença de AudioSource não demonstra comunicação concluída.

## 8. Arquitetura, produção e fronteiras — GDD §§8–9

Separar responsabilidades de movimento, alternância de luz, navegação e regras dos puzzles. Reutilizar prefabs independentes, especialmente Sala Principal e Puzzles, para reduzir edição concorrente de cenas; prefabs não eliminam todos os conflitos de merge.

Os nomes do GDD (`NavegadorKeypad.cs`, `PuzzleCabosTV.cs`, `PuzzleKeypad.cs`, `ExplosãoBomba.cs` no diagrama) não coincidem todos com os nomes reais dos ficheiros. Usar os caminhos de PROJECT_MAP.md; não renomear scripts/GUIDs para os fazer coincidir com o documento.

| Pessoa | Responsabilidades declaradas |
| --- | --- |
| David Barbosa | Mecânicas de módulos, puzzles lógicos, bateria e UI de fim de jogo. |
| Gonçalo Sousa | Personagens, split-screen, animações, áudio/chat de voz; o GDD também menciona jogo em rede. |
| Samuel Furtado | Level design, prefabs, iluminação/URP, partículas/explosão e navegação dos puzzles. |

Roadmap declarado (não resultado da auditoria): fase 1 descrita como concluída com protótipo, escuridão, lanternas, três puzzles/comando e explosão; fase 2 atual com GDD, bateria partilhada, rádio e animações, sendo que a tabela também lista online/restantes puzzles; fase 3 com polimento retro sci-fi, áudio diegético, balanceamento e playtesting formal.

**Ambiguidades de âmbito a resolver antes da respetiva implementação:**

1. A experiência principal e o rádio são descritos como **locais**, mas responsabilidades/roadmap mencionam **online/jogo em rede**. Não escolher transporte, sincronização ou requisitos de rede sem confirmação.
2. O roadmap diz “3 puzzles funcionais”, enquanto a especificação detalha A/B e marca C como futuro. Falta identificar o terceiro puzzle, o conjunto de módulos ativos da versão final e o que foi realmente validado.
3. Não há especificação completa do tempo inicial, das soluções/pistas dos módulos, do bloqueio do keypad, do rádio simultâneo nem das transições após recuperação de energia. Não preencher estas lacunas com os defaults do protótipo.

SWOT: comunicação e restrições partilhadas são os diferenciais; dependência de dois jogadores e repetitividade são riscos; dificuldade/frustração precisam de playtesting. Procedural, consolas e cross-platform são oportunidades, **não requisitos imediatos**. O sorteio de cabos existente não prova um gerador procedural de módulos completo.

## 9. Pedidos adicionais já feitos pelo utilizador

Menu principal e opções na cena StartMenu, com Português/Inglês, som e opções de ecrã, foram pedidos na conversa. São requisitos adicionais ao GDD aqui fornecido; não atribuir ao PDF exigências de idioma, volumes separados ou menu de pausa que ele não especifica. Implementar o briefing do GDD não foi autorizado por esta atualização documental.
