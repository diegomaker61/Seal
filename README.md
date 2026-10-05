# Seal

Aplicativo de foco para Windows, desenvolvido em C# e WPF (.NET 10).

## Executar

Instale o SDK .NET 10 no Windows e execute `dotnet run --project Seal.csproj` na pasta do projeto. Para compilar, use `dotnet build Seal.csproj`.

## Utilização

A janela principal inicia com 30 minutos. **Start** começa a sessão, **Pause** interrompe a contagem e **Resume** continua do mesmo ponto. O botão de reiniciar registra o tempo realizado e prepara outra sessão. Ao concluir, um som do sistema sinaliza o término; a próxima sessão começa apenas ao clicar em **Start**. Arraste uma área vazia da janela principal ou o texto do cronômetro para reposicioná-la. A janela de estatísticas tem tamanho fixo de 790 × 580 unidades WPF, sem redimensionamento ou moldura nativa. As bordas são arredondadas, com raio de 8 unidades WPF. Arraste seu fundo ou cabeçalho escuro para movê-la e use × para fechá-la. Clicar no calendário seleciona o dia sem iniciar o arraste. Os botões mantêm seu comportamento de clique.

**Stats** abre a segunda janela. As duas permanecem acima das janelas comuns do Windows. O calendário anual usa domingo como primeira linha e intensidade proporcional aos minutos de foco. Clique em um dia para consultar suas estatísticas, ou navegue usando as setas. **Today** retorna ao dia atual e **Refresh** recarrega o histórico.

As rodadas representam sessões concluídas. O tempo de foco inclui sessões parciais. A conclusão é o percentual de sessões concluídas sobre as iniciadas no dia. As barras contam sessões concluídas por hora de início. Uma sessão é atribuída à data e hora locais de início, inclusive quando atravessa a meia-noite. Pausas não entram no tempo de foco.

## Dados e arquitetura

O histórico fica em `%LOCALAPPDATA%\Seal\sessions.json`, salvo por substituição de arquivo temporário. A sessão é atualizada a cada 30 segundos, ao pausar, reiniciar, abrir estatísticas e fechar. Sessões interrompidas ficam no histórico como parciais; o cronômetro não retoma automaticamente após reabrir o aplicativo. Um encerramento abrupto pode perder os últimos 30 segundos. O aplicativo permite uma única instância por sessão do Windows. Tentativas de abrir outra instância são encerradas antes de criar janelas ou acessar o histórico. Um mutex nomeado mantém essa proteção enquanto o processo está em execução e é liberado ao sair.

`FocusTimer` controla a contagem monotônica usando `Stopwatch`. `ISessionRepository` abstrai a persistência e `JsonSessionRepository` implementa o armazenamento JSON. `StatisticsService` consulta os dados; as janelas cuidam da apresentação e das interações. As dependências são montadas em `App`, mantendo responsabilidades separadas e permitindo substituir o repositório.

A interface usa a paleta escura do Windows 11 e Segoe UI Variable, com Segoe UI como alternativa. Código e interface estão em inglês; a documentação está em português. Não existem intervalos automáticos, sincronização em nuvem ou duração configurável nesta versão.

O cabeçalho fica separado do conteúdo em duas linhas de Grid. O bloco de estatísticas tem largura de 748 unidades WPF e fica centralizado horizontalmente, mantendo os alinhamentos internos dos controles e textos. Os gráficos possuem largura explícita para que suas coordenadas não gerem espaço lateral excedente.

## Ícones

Os botões utilizam Material Icons oficiais do Google, convertidos de SVG para geometrias vetoriais nativas do WPF. Os arquivos originais estão em `Assets/MaterialIcons`, junto da licença Apache 2.0. O aplicativo não depende de fontes de ícones instaladas ou acesso à internet durante a execução. Os botões possuem dicas de ferramenta e nomes de acessibilidade; iniciar/continuar e pausar alternam os ícones de reprodução e pausa.

Fonte: https://github.com/google/material-design-icons
Na primeira execução do Seal, o histórico anterior de %LOCALAPPDATA%\Tomato\sessions.json é copiado para %LOCALAPPDATA%\Seal\sessions.json caso o novo arquivo ainda não exista. O arquivo antigo é preservado. Feche a versão anterior antes de usar o Seal.

## Dados temporários de visualização

O histórico local recebeu 482 sessões fictícias para visualizar os gráficos. A interface utiliza esse mesmo histórico, sem modo de amostra ou alternância. Os oito registros existentes foram preservados. Uma cópia anterior à inclusão fica ao lado de sessions.json, com sufixo .before-fictional e data/hora. Para remover os dados fictícios posteriormente, restaure essa cópia se não precisar dos registros criados depois dela.

## Tarefas

O botão **Tasks**, ao lado de **Statistics**, abre o gerenciamento de tarefas. Digite um nome e use **Add** para criar, ou selecione uma tarefa para **Rename** ou **Remove**. Os nomes devem ter entre 1 e 15 caracteres; nomes ativos duplicados não são permitidos. A tarefa `default` é criada automaticamente e pode ser removida quando existir pelo menos outra tarefa ativa. Sua renomeação permanece bloqueada. O sistema mantém pelo menos uma tarefa ativa; o botão de remoção fica desabilitado para a última tarefa. Uma tarefa removida não é recriada ao reabrir o aplicativo, e seu histórico continua disponível nas estatísticas.

O seletor acima do cronômetro escolhe a tarefa da próxima sessão. A seleção fica bloqueada enquanto a sessão estiver em andamento ou pausada. Conclua ou reinicie a sessão para escolher outra tarefa. Cada sessão guarda o identificador estável da tarefa e uma cópia do seu nome no início. Renomear uma tarefa mantém suas sessões associadas; remover uma tarefa retira-a das opções de novas sessões, preservando sua identificação no histórico.

Na janela **Statistics**, o seletor no cabeçalho filtra o calendário anual, os indicadores diários e o gráfico por tarefa. **All tasks** mostra os totais gerais. Tarefas removidas não aparecem no filtro nem nos totais, calendário ou gráficos das estatísticas. Seus registros permanecem salvos no histórico. Sessões antigas, incluindo os dados fictícios já existentes, pertencem à tarefa `default`.

As tarefas ficam em `%LOCALAPPDATA%\Seal\tasks.json`. `ITaskRepository` abstrai a persistência, `JsonTaskRepository` salva o arquivo por substituição e `TaskCatalog` valida nomes e controla criação, renomeação e remoção. As janelas compartilham o catálogo e atualizam os seletores após mudanças.