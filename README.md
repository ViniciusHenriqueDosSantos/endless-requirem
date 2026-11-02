# U.N.I.F.E.S.P.

Jogo de ação e tiro top-down feito em **Godot 4.7.2 (.NET / C#)** para a disciplina de games.

2061. A IA chamada MENTORA fechou as faculdades. Você volta ao campus abandonado, cheio de fantasmas de alunos que nunca se formaram, para buscar o seu diploma.

> Estado atual: arena de testes com ondas infinitas de inimigos. As salas, os chefes e as fases estão sendo construídos aos poucos.

---

## 1. O que instalar (uma vez só)

| Programa | Onde baixar | Atenção |
|---|---|---|
| **Godot 4.7.2 – versão .NET** | <https://godotengine.org/download/archive/4.7.2-stable/> | Baixe a versão **.NET** (a outra não roda C#). É um .zip: extraia numa pasta fixa, como `C:\Godot`. |
| **.NET SDK 8** | <https://dotnet.microsoft.com/download/dotnet/8.0> | Baixe o **SDK** (não só o "Runtime"), x64, e instale. |
| **GitHub Desktop** | <https://desktop.github.com> | Entre com a sua conta do GitHub. |

Depois de instalar o .NET SDK, reinicie o computador.

## 2. Como baixar e abrir o projeto

1. No GitHub Desktop: **File → Clone repository → URL**, cole `https://github.com/ViniciusHenriqueDosSantos/endless-requirem` e clique em **Clone**.
2. Abra o Godot (o `.exe` com "mono" no nome).
3. Clique em **Import**, vá até a pasta que o GitHub Desktop criou e escolha o arquivo `project.godot`.
4. Com o projeto aberto, aperte **F5** para jogar.

Na primeira vez, o Godot compila o código C# e pode demorar alguns segundos.

## 3. Controles

| Ação | Tecla |
|---|---|
| Andar | **W A S D** ou **setas** |
| Mirar | Mouse |
| Atirar | **Botão esquerdo** do mouse (segure) |
| Dash (esquiva rápida) | **Espaço** (andando) |
| Trocar de arma | **1**, **2**, **3** ou **roda do mouse** |

Armas: 1 = Metralhadora · 2 = Escopeta · 3 = Canhão Perfurante.

## 4. Como o código está organizado

```
main.tscn / Main.cs     cena principal (arena de testes e gerador de inimigos)
objects/                uma pasta por objeto do jogo
  player/               jogador: movimento, dash, vida, tiro
  enemy/                inimigos (Normal, Fast, Tank)
  bullet/               projétil
  weapon/               armas (cada .tres é uma arma)
  heal_drop/            item de cura
  hud/                  textos na tela
  obstacle/             obstáculo
  arena_grid/           grade do chão
sprites/                imagens
```

---

## 5. Como trabalhamos com Git (GitHub Desktop)

**Regra de ouro:** ninguém trabalha direto na branch `main`. O `main` é a versão que sempre funciona.

### Começar uma tarefa
1. Clique em **Fetch origin** (e depois em **Pull origin**, se aparecer) para pegar a versão mais nova.
2. Clique em **Current Branch → New Branch**, dê um nome curto (ex.: `mvp-1-salas`) e confirme.

### Salvar o seu trabalho (commit)
1. Faça as mudanças no Godot e salve (Ctrl+S).
2. No GitHub Desktop, na aba **Changes**, confira os arquivos alterados.
3. Escreva um resumo curto em **Summary** (ex.: `Cria a porta que tranca`).
4. Clique em **Commit to nome-da-branch**.

### Mandar para o GitHub (push)
- Na primeira vez: **Publish branch**. Nas próximas: **Push origin**.

### Pedir revisão (Pull Request)
1. Clique em **Create Pull Request** (ou **Preview Pull Request → Create pull request**). O navegador abre.
2. Escreva o que mudou e como testar, e clique em **Create pull request**.
3. Avise o grupo.

### Revisar o Pull Request de um colega
1. No GitHub Desktop: **Current Branch** → escolha a branch do colega.
2. Abra o Godot e teste com **F5** seguindo o "Como testar" do Pull Request.
3. Se estiver tudo certo: no site, aba **Files changed → Review changes → Approve → Submit review**, depois **Merge pull request**.

### Depois que algo entrou no `main`
Todos: **Current Branch → main**, depois **Fetch origin → Pull origin**.

### Cuidados
- Evite que duas pessoas editem a **mesma cena** (`.tscn`) ao mesmo tempo. Combinem no grupo antes.
- Não apague arquivos de outra pessoa sem perguntar.

---

## 6. Problemas comuns

- **Erros de C# ao abrir ou o botão de compilar não aparece:** confira se o Godot baixado é a versão **.NET** e se o **.NET SDK 8** está instalado. Reinicie o computador e tente de novo.
- **"Unable to load .NET runtime":** falta o .NET SDK 8 (ou o computador não foi reiniciado depois de instalar).
- **O GitHub Desktop mostra mudanças que você não fez:** não faça commit delas. Clique com o botão direito no arquivo → **Discard changes**, ou pergunte ao grupo.

## Equipe

- ViniciusHenriqueDosSantos
- rafael-figueredo
- joaopcosta12
