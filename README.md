# CallBell

Recriacao do sistema de chamados de fabrica em `C#`, `WinForms`, `WebView2` e `.NET 9`, inspirado na separacao em camadas do TeamOps.

## Apps da solution

- `CallBell.OperatorApp`: terminal do operador para abrir solicitacoes.
- `CallBell.AttendantApp`: terminal de atendimento para listar chamados abertos e fechar com FJ.
- `CallBell.MonitorApp`: painel de monitoramento por setor com WebView2, layout para TV e alerta sonoro.
- `CallBell.ManagementApp`: historico, filtros gerenciais e cadastros basicos.

## Camadas

- `CallBell.Config`: leitura de `App.config`, resolucao de caminhos e perfis locais.
- `CallBell.Core`: entidades, modelos e validacao de FJ.
- `CallBell.Data`: SQLite, schema, seed inicial, repositorios e trigger files.

## Banco de dados

O banco SQLite usa um unico arquivo configurado por `App.config`.

Exemplos validos:

- caminho relativo portable: `data\callbell.db`
- drive mapeado: `Z:\CallBell\callbell.db`
- compartilhamento UNC: `\\servidor\CallBell\callbell.db`

Observacao importante:

- SQLite aceita caminho UNC, entao nao precisa obrigatoriamente de letra mapeada.
- Em compartilhamento de rede, prefira transacoes curtas e poucos leitores concorrentes. Por isso o projeto usa operacoes simples, polling leve e `busy_timeout`.

## Configuracao

Todos os apps usam as mesmas chaves:

- `PortableMode`
- `DatabasePath`
- `TriggerDirectory`
- `ProfileDirectory`
- `MonitorBoardTitle`
- `MonitorRefreshSeconds`

Configuracao atual de testes:

- `DatabasePath = C:\CallBell\DB\callbell.db`
- `TriggerDirectory = C:\CallBell\trigger`
- `ProfileDirectory = C:\CallBell\Profiles`

Se `TriggerDirectory` ou `ProfileDirectory` nao forem informados, o sistema passa a derivar os defaults a partir do caminho do banco para manter a instalacao consistente.

## Seed inicial

Na primeira execucao o sistema cria:

- 2 setores
- 4 areas
- 5 maquinas de exemplo
- 4 motivos de solicitacao

Esses dados podem ser alterados pelo `CallBell.ManagementApp`.

## Build

```powershell
dotnet build CallBell.sln
```
