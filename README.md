# PurgeIt

Aplicativo de limpeza automática para Windows com ícone na sistem tray do sistema. Roda silenciosamente em background, executando um ciclo de limpeza a cada 7 dias conforme as regras do modo selecionado.

## Contexto

Projeto de portfólio em C#, que surgiu da combinação do gosto pela linguagem
com a minha experiência como analista de negócios. Sempre curti esse tipo de
utilitário para Windows e resolvi construir o meu próprio, aproveitando para
aplicar a visão de regras de negócio no desenvolvimento em si.

Ao longo do desenvolvimento, os aprendizados que mais ficaram foram:

**Fundamentos de C# e .NET**
- **Mutex** não sabia que isso existia. Aprendi que processos no Windows podem
  competir pelo mesmo recurso e que existe um mecanismo específico para garantir
  que só um rode por vez
- **Tuplas como retorno de método** descobri que um método pode retornar mais
  de um valor ao mesmo tempo, e que isso resolve de forma limpa situações onde
  você precisa de uma resposta e de um motivo junto
- **Propriedades calculadas** entendi a diferença entre guardar um valor e
  calculá-lo na hora em que alguém pede, e quando cada um faz mais sentido
- **Try/catch em operações de arquivo** percebi que arquivo é um dos recursos
  mais imprevisíveis com que um programa pode interagir: pode sumir, estar em uso,
  sem permissão, corrompido. Código que mexe com arquivo precisa estar preparado
  para tudo isso
- **Parâmetros com valor padrão** aprendi que métodos podem ter parâmetros
  opcionais com valores padrão, evitando sobrecarga desnecessária de métodos
- **IDisposable** entendi como o C# gerencia recursos externos e por que
  implementar o padrão Dispose é importante para ícones, timers e streams

**Interação com o Windows**
- **Environment e caminhos de sistema** aprendi que escrever `C:\Users\João\...`
  no código é um erro clássico e como resolver caminhos de forma
  dinâmica usando `SpecialFolder` e variáveis de ambiente
- **Serviços do Windows** aprendi a verificar se um serviço está rodando antes
  de mexer em pastas que dependem dele
- **System Tray e ciclo de vida sem janela** entendi como um app pode existir
  sem nenhuma janela visível, vivendo apenas pelo ícone na bandeja do sistema
- **Notificações nativas** aprendi a disparar notificações do próprio Windows
  sem precisar construir nada visualmente
- **Atalhos `.lnk`** aprendi a criar um atalho programaticamente para que o
  app inicie com o Windows, sem precisar de permissão de administrador
- **P/Invoke e kernel32.dll** tive o primeiro contato com chamadas nativas do
  Windows direto do C#, usando `AllocConsole` e `FreeConsole` para abrir e
  fechar um console real em tempo de execução
- **Streams de console** aprendi que ao liberar e realocar um console, os
  streams padrão do C# ficam inválidos e precisam ser reconectados manualmente
  via `Console.SetOut` e `Console.SetIn`
- **Registro do Windows** tive o primeiro contato com o registro para resolver
  caminhos de instalação de apps como o Steam

**Arquitetura e organização**
- **Separação de responsabilidades** comecei a entender por que dividir o código
  em camadas com papéis bem definidos importa, especialmente quando o projeto
  cresce e você precisa achar onde está cada coisa
- **Injeção de dependência** aprendi a passar dependências pelo construtor em
  vez de criar tudo dentro da própria classe, e como isso torna o código mais
  organizado e fácil de mudar
- **Regras de negócio como código** exercitei bastante a habilidade de pegar
  um requisito escrito em português e transformar em validações concretas no código
- **Modelar consequências** aprendi a pensar em níveis de risco antes de agir:
  alguns arquivos podem ser apagados direto, outros precisam de um período de
  espera, outros só saem com confirmação explícita do usuário

---

## Índice

- [Funcionamento](#funcionamento)
- [Modos de Limpeza](#modos-de-limpeza)
- [Camadas de Deleção](#camadas-de-deleção)
- [Regras de Negócio](#regras-de-negócio)
- [Configuração](#configuração-configjson)
- [Logs](#logs)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Tecnologias](#tecnologias)

---

## Funcionamento

1. O app inicia junto com o Windows via atalho `.lnk` criado automaticamente em `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup`, sem necessidade de privilégios de administrador
2. Na primeira execução, realiza um dry-run completo e exibe ao usuário o espaço que poderia ser liberado
3. Após o primeiro ciclo, o timer interno passa a disparar a cada 7 dias
4. A limpeza ocorre em background — o console só é aberto em caso de erro, clique manual ou modo verbose ativo
5. Arquivos sensíveis são acumulados em uma lista de confirmação manual, exibida ao usuário ao final do ciclo
6. Todas as ações são registradas em `%LOCALAPPDATA%\PurgeIt\purge.log`

---

<img width="1373" height="540" alt="image" src="https://github.com/user-attachments/assets/01098f10-a504-4acc-b699-fb9d78da990a" />

---

## Modos de Limpeza

O modo ativo pode ser alterado diretamente pelo menu de contexto do ícone na bandeja, sem precisar editar o `config.json` manualmente.

<img width="397" height="183" alt="image" src="https://github.com/user-attachments/assets/4089e9c9-104f-4eed-9ff4-590a742c1941" />

### Safe
Limpeza conservadora, sem riscos. Indicado para uso geral.

| Pasta | Camada |
|---|---|
| `%TEMP%`, `C:\Windows\Temp`, `%LOCALAPPDATA%\Temp` | Hard Delete |
| `%LOCALAPPDATA%\CrashDumps` | Hard Delete |
| Cache do Chrome, Edge e Firefox | Hard Delete |
| Cache do Discord | Hard Delete |
| `Steam\logs`, `Steam\appcache`, `Steam\depotcache` | Hard Delete |

### Balanced
Inclui tudo do Safe, mais:

| Pasta | Camada | Critério |
|---|---|---|
| Downloads | Soft Delete | Último acesso > 14 dias |
| Lixeira (`C:\$Recycle.Bin`) | Soft Delete | Itens com > 30 dias |
| `Steam\shadercache` | Soft Delete | |

### Aggressive
Inclui tudo do Balanced, mais:

| Pasta | Camada | Observação |
|---|---|---|
| `C:\Windows\SoftwareDistribution\Download` | Confirmação Manual | Verifica serviço `wuauserv` antes |
| `C:\Windows\Prefetch` | Confirmação Manual | |
| `Steam\downloading` | Confirmação Manual | Ignorado se houver downloads ativos |

> Ao selecionar o modo Aggressive, um modal de aviso é exibido com a descrição dos riscos de cada pasta. O checkbox de confirmação fica desabilitado por 5 segundos e só é liberado após esse período, exigindo que o usuário marque "Li e assumo a responsabilidade" antes de prosseguir.

<img width="505" height="410" alt="image" src="https://github.com/user-attachments/assets/eeb0857e-8149-4072-9e19-135ccbf54202" />

### Custom
O usuário edita diretamente o `config.json` e define quais pastas incluir, qual camada usar e os critérios individuais de cada uma. Todas as pastas ficam disponíveis nesse modo, incluindo `Prefetch` (desabilitado por padrão no JSON).

---

## Camadas de Deleção

### Hard Delete
Arquivos apagados permanentemente e imediatamente, sem possibilidade de recuperação. Usado exclusivamente para arquivos temporários e de cache sem valor.

### Soft Delete — Quarentena
Arquivos movidos para `%LOCALAPPDATA%\PurgeIt\Quarantine` e apagados permanentemente após **2 dias**. O usuário recebe notificação ao mover arquivos para quarentena e caso o tamanho dela ultrapasse o limite configurado.

### Confirmação Manual
Arquivos que só são apagados após confirmação explícita do usuário. Ao final do ciclo, é exibida uma tela com os arquivos pendentes e três opções:

- Apagar todos de uma vez
- Apagar individualmente, com opção de pular cada um
- Exportar a lista para o log e fechar sem apagar

<!-- ![Tela de confirmação manual](docs/images/confirmation_form.png) --> (Na versão atual ele não está fazendo a verificação das pastas do Aggressive mode pois o ConfirmationForm estava como Show e não ShowDialog, então ele não parava o processo para pedir permissão e rodava direto. Esperar a atualização para uso caso queira usar o aggressive mode)

---

## Regras de Negócio

### Blocklist de Pastas
As pastas abaixo nunca podem ser alvo de limpeza, independentemente do modo ou da configuração do usuário:

```
C:\
C:\Windows
C:\Users
C:\Program Files
C:\Program Files (x86)
%USERPROFILE%  (raiz)
%APPDATA%      (raiz)
%LOCALAPPDATA% (raiz)
%SYSTEMROOT%
%SYSTEMDRIVE%
```

### Regras Gerais
- Arquivos abertos ou em uso por algum processo são ignorados
- Arquivos com atributo `ReadOnly` ou `System` são ignorados
- Atalhos (`.lnk`) são ignorados
- Arquivos abaixo do tamanho mínimo configurado são ignorados
- O serviço `wuauserv` é verificado antes de limpar `SoftwareDistribution\Download`

### Regras da Pasta Downloads
- Apenas arquivos com último acesso superior a 14 dias são elegíveis
- Downloads em andamento (`.crdownload`, `.part`) são ignorados
- Instaladores (`.exe`, `.msi`, `.msix`) com menos de 30 dias sem acesso são ignorados
- Arquivos acima do limite configurado exigem confirmação explícita do usuário

---

## Configuração (config.json)

Localizado em `%LOCALAPPDATA%\PurgeIt\config.json`. Criado automaticamente na primeira execução com valores padrão. Editável diretamente pelo usuário apenas no modo **Custom**.

```json
{
  "firstRun": false,
  "mode": "Safe",
  "cycleDays": 7,
  "minFileSizeKB": 0,
  "maxDownloadSizeGB": 1.0,
  "maxQuarantineSizeGB": 2.0,
  "verbose": false,
  "dryRun": false,
  "folders": [
    {
      "path": "C:\\Caminho\\Da\\Pasta",
      "layer": "hard",
      "minAgeDays": 0,
      "enabled": true
    }
  ]
}
```

| Campo | Tipo | Descrição |
|---|---|---|
| `firstRun` | bool | Flag de primeira execução |
| `mode` | string | Modo de limpeza: `Safe`, `Balanced`, `Aggressive`, `Custom` |
| `cycleDays` | int | Frequência do ciclo em dias |
| `minFileSizeKB` | int | Tamanho mínimo em KB para elegibilidade |
| `maxDownloadSizeGB` | double | Limite de tamanho para Downloads sem confirmação |
| `maxQuarantineSizeGB` | double | Limite da quarentena antes de notificar |
| `verbose` | bool | Abre o console nativo do Windows durante a execução |
| `dryRun` | bool | Simula sem apagar nada |
| `folders` | array | Lista de pastas no modo Custom |

---

## Verbose Mode

Quando ativado, abre um console nativo do Windows em tempo real durante a limpeza, exibindo cada arquivo avaliado, se foi aceito ou rejeitado pelo RuleEngine e o motivo.

Implementado via **P/Invoke** com `AllocConsole` e `FreeConsole` da `kernel32.dll`, com reconexão manual dos streams `Console.SetOut` e `Console.SetIn` para suportar múltiplas aberturas no mesmo ciclo de vida do app.

<img width="979" height="512" alt="image" src="https://github.com/user-attachments/assets/bc418576-fa8a-407c-a7b4-a0040c15a93d" />

---

## Logs

Registrado em `%LOCALAPPDATA%\PurgeIt\purge.log`. Cada entrada contém os campos abaixo:

| Campo | Descrição |
|---|---|
| `timestamp` | Data e hora da ação |
| `action` | Tipo de ação (`QUARANTINE`, `DELETE`, `SKIPPED`, `ERROR`, `DRYRUN`, `EXPORTED`) |
| `layer` | Camada de deleção (`hard`, `soft`, `manual`) |
| `reason` | Motivo da exclusão (ver tabela abaixo) |
| `size` | Tamanho do arquivo |
| `path` | Caminho completo do arquivo |

**Valores de `reason`:**

| Valor | Descrição |
|---|---|
| `tempFile` | Arquivo temporário |
| `cacheFile` | Arquivo de cache |
| `minAgeExceeded` | Arquivo não acessado além do mínimo configurado |
| `oversizeQuarantine` | Arquivo acima do limite configurado para quarentena |
| `userConfirmed` | Exclusão confirmada manualmente pelo usuário |
| `dryRun` | Simulação, nenhuma ação real realizada |
| `pendingManual` | Exportado para o log sem exclusão |

---

## Estrutura do Projeto

```
PurgeIt/
├── Core/
│   ├── RuleEngine.cs            # Valida arquivos contra as regras de negócio
│   ├── Scanner.cs               # Varre pastas e monta lista de elegíveis por modo
│   ├── Cleaner.cs               # Executa a limpeza por camada (hard, soft, manual)
│   └── ConsoleHelper.cs         # Gerencia o console nativo via P/Invoke (verbose mode)
├── Models/
│   ├── CleanConfig.cs           # Representa as configurações do config.json
│   ├── FileEntry.cs             # Representa um arquivo elegível para limpeza
│   └── CleanResult.cs           # Representa o resultado de um ciclo de limpeza
├── Services/
│   ├── ConfigService.cs         # Leitura e escrita do config.json
│   ├── LogService.cs            # Escrita no purge.log
│   └── QuarantineService.cs     # Gerenciamento da quarentena e expiração
├── UI/
│   ├── MainForm.cs              # Janela principal, roda oculta na inicialização
│   ├── TrayIcon.cs              # Ícone, menu e seletor de modo na bandeja do sistema
│   ├── ConfirmationForm.cs      # Tela de confirmação manual de arquivos pendentes
│   └── AggressiveWarningForm.cs # Modal de aviso ao selecionar modo Aggressive
├── Resources/                   # Ícones e recursos visuais
└── Program.cs                   # Ponto de entrada, Mutex de instância única e inicialização
```

---

## Tecnologias

| | |
|---|---|
| Linguagem | C# |
| Framework | .NET 8.0 |
| UI | Windows Forms |
| Plataforma | Windows |
| Distribuição | Self-contained, single-file, win-x64 |
