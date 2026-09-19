# Hiragana Trainer

A [Dalamud](https://dalamud.dev/) plugin for FFXIV: a hiragana/katakana
trainer using spaced repetition (SRS, Leitner system), inspired by the
Hiragana Pro app.

**Status: in development — not yet installable.**

## What it does

- Trains base hiragana and katakana (no dakuten/handakuten/combinations),
  one script at a time so they're never mixed in the same session.
- New kana are introduced one at a time — taught first, then quizzed —
  with a configurable pace before the next one unlocks.
- Spaced repetition via Leitner boxes, scheduled per training session
  (not by wall-clock time); a kana graduates out of review after enough
  correct answers, shown as a star progress row.
- Triggers via manual command (`/kana`) or automatically while queuing for
  a duty, with a configurable cooldown — closes itself the moment a duty
  is found so it never blocks the confirm dialog.
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
