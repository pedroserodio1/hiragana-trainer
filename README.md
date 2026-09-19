# Hiragana Trainer

Plugin [Dalamud](https://dalamud.dev/) para FFXIV: um treinador de
hiragana/katakana com repetição espaçada (SRS, sistema Leitner), inspirado no
app Hiragana Pro.

**Status: em desenvolvimento — ainda não é instalável.**

## O que faz

- Treina hiragana e katakana base (sem dakuten/handakuten/combinações).
- Repetição espaçada por caixas Leitner, agendada por sessão de treino (não
  por relógio).
- Dispara via comando manual (`/kana`) ou automaticamente em loading screens
  e "duty pop", com cooldown configurável.
- Progresso e estatísticas (acertos/erros por kana, streak) salvos por
  personagem.

## Build local

Requisitos: .NET SDK 10+, [XIVLauncher/Dalamud](https://github.com/goatcorp/FFXIVQuickLauncher)
instalado.

```bash
dotnet build
```

Para testar no jogo, aponte a pasta de build do projeto `HiraganaTrainer`
para a pasta `devPlugins` do XIVLauncher (ou configure "Dev Plugin Location"
nas configurações do Dalamud).

## Testes

```bash
dotnet test
```

## Licença

MIT — veja [LICENSE](LICENSE).
