# Hiragana Trainer

A [Dalamud](https://dalamud.dev/) plugin for FFXIV: a hiragana/katakana
trainer using spaced repetition (SRS, Leitner system), inspired by the
Hiragana Pro app.

**Status: in development — not yet installable.**

## What it does

- Trains base hiragana and katakana (no dakuten/handakuten/combinations).
- Spaced repetition via Leitner boxes, scheduled per training session
  (not by wall-clock time).
- Triggers via manual command (`/kana`) or automatically on loading screens
  and duty pop, with a configurable cooldown.
- Progress and stats (correct/incorrect per kana, streak) saved per
  character.

## Building locally

Requirements: .NET SDK 10+, [XIVLauncher/Dalamud](https://github.com/goatcorp/FFXIVQuickLauncher)
installed.

```bash
dotnet build
```

To test in-game, point the `HiraganaTrainer` project's build output at the
XIVLauncher `devPlugins` folder (or set a "Dev Plugin Location" in the
Dalamud settings).

## Tests

```bash
dotnet test
```

## License

MIT — see [LICENSE](LICENSE).
