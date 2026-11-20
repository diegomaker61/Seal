# Seal

[English](README.md) | **Português (Brasil)**

Seal é um aplicativo open source de foco para Windows, desenvolvido em C# e WPF com .NET 10. Combina um cronômetro Pomodoro configurável de 10 a 60 minutos, tarefas e estatísticas em uma interface minimalista inspirada no tema escuro do Windows 11.

## Recursos

- Cronômetro com pausa, retomada, reinício e barra de progresso.
- Borda visual de foco em todos os monitores conectados.
- Janelas sempre no topo, com bordas arredondadas e movimentação por arraste.
- Gerenciamento de tarefas e associação das sessões à tarefa escolhida.
- Calendário anual, indicadores diários e gráfico de sessões por hora.
- Histórico local e proteção contra múltiplas instâncias.

## Instalação

Baixe o pacote para Windows na seção **Releases**, extraia o ZIP e execute `Seal.exe`. Mantenha os arquivos do pacote na mesma pasta. Pacotes autocontidos incluem o .NET; versões dependentes do framework exigem o **.NET Desktop Runtime 10**.

## Como usar

1. Escolha uma tarefa no seletor acima do cronômetro.
2. Clique em **Play** para iniciar. Use **Pause** para pausar e **Play** para continuar.
3. Use **Reset** para registrar o tempo realizado e preparar outra sessão. Ao concluir o tempo configurado, um som sinaliza o término.
4. Abra **Tasks** para adicionar, renomear ou remover tarefas. Os nomes aceitam até 15 caracteres. É necessário manter pelo menos uma tarefa; `default` pode ser removida quando outra existir.
5. Abra **Statistics** para consultar o histórico. Selecione uma tarefa, navegue entre anos e dias ou clique em um dia do calendário. Os botões **Today** e **Refresh** retornam ao dia atual e atualizam os dados.

**Atalho global:** use `Ctrl + Alt + P` para iniciar, pausar ou continuar a sessão, mesmo em outro aplicativo. Se a combinação já estiver registrada por outro programa ou outra versão do Seal, a dica do botão informa que o atalho está indisponível.

Passe o mouse sobre os ícones para ver suas funções. Arraste uma área vazia ou o cabeçalho para mover as janelas. A tarefa de uma sessão só pode ser trocada após concluí-la ou reiniciá-la.

Nas estatísticas, **Rounds** conta sessões concluídas, **Focus time** inclui o tempo das sessões parciais e **Completion** mostra a proporção de sessões concluídas. Tarefas removidas ficam ocultas nos filtros e totais.

Durante a contagem, uma borda discreta de 2 pixels, na cor #03FCEC, aparece em cada monitor conectado. Ela desaparece ao pausar, reiniciar ou concluir a sessão, não recebe foco e permite clicar normalmente nos outros aplicativos.

## Configurações

Abra **Settings** pelo botão de engrenagem ao lado de **Tasks**. Clique em **Change shortcut** e pressione Ctrl, Alt ou Windows junto com uma tecla; o botão indica quando está aguardando a combinação. Pressione Esc para cancelar; atalhos já utilizados por outro aplicativo são recusados. Você também pode ativar ou desativar a borda em todos os monitores, escolher **Start with Windows** e definir o foco entre 10 e 60 minutos (padrão: 30). Clique em **Save** para aplicar ou **Cancel** para descartar.

A borda e o atalho mudam imediatamente. A duração vale para a próxima sessão, sem alterar a contagem em andamento. As preferências ficam em `settings.json`, na pasta de dados do perfil. A inicialização com Windows usa uma entrada por usuário e por perfil; ativá-la na build de desenvolvimento não altera a configuração da versão instalada.

## Dados locais

O histórico e as tarefas são salvos em `%LOCALAPPDATA%\Seal`, nos arquivos `sessions.json` e `tasks.json`. O cronômetro não retoma automaticamente ao reabrir o aplicativo. Esta versão não possui intervalos automáticos ou sincronização em nuvem.

## Desenvolvimento

Requisitos: Windows e SDK .NET 10.

Builds **Debug** usam `%LOCALAPPDATA%\Seal.Development`, mostram **Seal (Dev)** e podem rodar ao lado da versão instalada. Seus testes não alteram o histórico ou as tarefas reais, nem importam o histórico antigo. Builds **Release** usam `%LOCALAPPDATA%\Seal` e devem ser utilizadas nas releases públicas.

```powershell
dotnet run --project Seal.csproj
```

Para gerar um pacote autocontido para Windows x64:

```powershell
dotnet publish Seal.csproj -c Release -r win-x64 --self-contained true -o bin/publish/win-x64
```

O código e a interface estão em inglês; a documentação está disponível em inglês e português brasileiro. A arquitetura separa cronômetro, persistência, tarefas e apresentação.

## Estrutura do projeto

```text
Seal/
├── Assets/
│   ├── Icons/                 # Imagens e ícone do aplicativo
│   ├── MaterialIcons/         # Ícones vetoriais e licença
│   └── Theme/                 # Estilos dos controles
├── Models/                    # Sessões, tarefas e filtros
├── Services/                  # Cronômetro, estatísticas e persistência
├── App.xaml / App.xaml.cs     # Recursos e inicialização
├── MainWindow.xaml(.cs)       # Cronômetro e seleção de tarefa
├── StatisticsWindow.xaml(.cs) # Calendário e indicadores
├── TasksWindow.xaml(.cs)      # Gerenciamento de tarefas
├── WindowDrag.cs              # Arraste das janelas
├── Seal.csproj / Seal.slnx    # Projeto e solução
├── README.md                  # Documentação em inglês
├── README.pt-BR.md            # Documentação em português
└── LICENSE                    # GNU GPL v3.0
```

`FocusTimer` controla as sessões. `TaskCatalog` administra e valida as tarefas. Os repositórios JSON implementam as interfaces de persistência, e `StatisticsService` consulta o histórico. As dependências são montadas em `App.xaml.cs`. As pastas `bin/`, `obj/` e `.vs/` contêm arquivos gerados.

## Licença

Distribuído sob a **GNU GPL v3.0**. Consulte [LICENSE](LICENSE).

Os [Material Icons do Google](https://github.com/google/material-design-icons) utilizam a licença Apache 2.0, disponível em [Assets/MaterialIcons/LICENSE.txt](Assets/MaterialIcons/LICENSE.txt).




Ao fechar o Seal, o tempo restante e a tarefa da sessão são salvos. Ao abrir novamente, a sessão incompleta retorna pausada; o período com o aplicativo fechado não é contado. Reiniciar ou concluir a sessão limpa o estado salvo do cronômetro.


Selecione uma meta diária de **0h a 10h**; **0h** é o padrão e significa sem meta. Clique em **New** para selecionar uma tarefa provisória com nome em branco. Informe o nome e clique em **Save** para criá-la. Selecione uma tarefa existente para editar nome ou meta e clique em **Save**. As estatísticas mostram o progresso do dia selecionado, incluindo sessões parciais.

Ative **Pause media when pausing the timer** em Settings para pausar todas as sessões em reprodução disponíveis nos controles de mídia do Windows, pelo botão de pausa ou atalho global. Desativado por padrão. Players sem integração com os controles de mídia do Windows não podem ser controlados. Ao continuar o cronômetro, somente as mídias pausadas com sucesso pelo Seal nesta execução são retomadas. Reiniciar o cronômetro ou desativar a opção limpa essa lista.

Main e Statistics compartilham a tarefa selecionada. Segure Reset ou Complete por 2 segundos; soltar antes cancela. Complete marca a sessão como concluída e registra somente o tempo realmente focado.

Para tarefas com Goal diário, o seletor principal mostra as rodadas concluídas hoje e as necessárias, como **Study · 2/4 rounds**. O total usa a duração de foco configurada, arredondado para cima; tarefas sem Goal mostram somente o nome.

Com o cronômetro rodando e o mouse fora da janela principal, os botões de menu, fechar e ações ficam ocultos, e a barra de progresso fica mais espessa e brilhante. Ao voltar com o mouse ou pausar, os controles reaparecem.

Trocar de tarefa pausa e preserva cada round incompleto separadamente. Ao voltar, o tempo restante é restaurado, pausado, mantendo o mesmo ID da sessão. Os rounds salvos também são mantidos ao fechar o aplicativo. Reset e Complete afetam somente o round da tarefa selecionada; o histórico continua vinculado à tarefa original.

As tarefas usam apenas nomes, sem cores individuais. As estatísticas usam a cor de destaque do Seal. São permitidas até 20 tarefas ativas. Tarefas, metas e histórico existentes continuam compatíveis.
