# UCI Battle

Aplicação de console em C# (.NET 10) que coloca duas engines de xadrez compatíveis com o protocolo **UCI** (Stockfish, Komodo, Leela etc.) para jogar uma contra a outra e grava a partida em um arquivo **PGN**.

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- Um executável de engine UCI (por exemplo, [Stockfish](https://stockfishchess.org/download/))

## Como executar

Direto pelo SDK:

```powershell
dotnet run --project Chess.Battle.Console -- `
  --e1path C:\engines\stockfish.exe --e1name "Stockfish A" --e1threads 4 `
  --e2path C:\engines\stockfish.exe --e2name "Stockfish B" --e2threads 1 `
  --movetime 1000 -o partida.pgn
```

Ou publicando um executável nativo (Native AOT, x86-64-v3):

```powershell
dotnet publish Chess.Battle.Console -c Release -r win-x64
```

## Opções de linha de comando

| Opção | Atalho | Obrigatória | Descrição |
|---|---|---|---|
| `--e1path` | | sim | Caminho do executável da engine 1. |
| `--e1name` | | sim | Nome da engine 1 (vai para a tag `White`/`Black` do PGN). |
| `--e1threads` | | não | Threads da engine 1 (padrão `2`). |
| `--e2path` | | sim | Caminho do executável da engine 2. |
| `--e2name` | | sim | Nome da engine 2. |
| `--e2threads` | | não | Threads da engine 2 (padrão `2`). |
| `--movetime` | `-m` | sim | Milissegundos que cada engine pensa por lance. Vale para as duas engines. |
| `--output` | `-o` | sim | Caminho do PGN gerado (sobrescrito se já existir). |
| `--white` | `-w` | não | Qual engine joga de brancas: `1` ou `2` (padrão `1`). |
| `--timeout` | `-t` | não | Segundos que uma engine tem para responder antes de ser considerada travada. `0` desliga o limite (padrão `600`). |
| `--verbose` | `-v` | não | Ativa o log. `c` = console, `f` = arquivo. Sem valor usa o console. Sem a opção, nada é logado. |

## Exemplo de saída

```
[Event "UCI Battle"]
[Site "MEU-PC"]
[Date "2026.09.27"]
[Round "1"]
[White "Stockfish A"]
[Black "Stockfish B"]
[Result "1/2-1/2"]

1. e4 e5 2. Nf3 Nc6 3. Bb5 Nf6 ... 1/2-1/2
```

## Testes

```powershell
dotnet test Chess.Battle.Tests
dotnet test Chess.Uci.Connector.Test
```

Os testes do conector precisam de um `stockfish.exe` na pasta de saída do projeto de teste (`Chess.Uci.Connector.Test\bin\Debug\net10.0`).
