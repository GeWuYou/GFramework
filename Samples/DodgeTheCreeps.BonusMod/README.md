# Bonus Score mod

An independently compiled trusted DLL adds one point to each score increment through `IDodgeModApi`. Its `score` state records the number of awards in the mod's isolated save namespace. The game supplies the current run's storage scope, so starting or loading another run does not leak state.

Run `python3 scripts/build-dodge-mods.py` from the repository root, then enable **Bonus Score** in the sample content manager and confirm code execution. Restart to activate it.
