# Double Score mod

This external DLL uses the optional GFramework Harmony host extension to double the internal score calculation. Unlike Bonus Score, it targets an implementation method outside the published game API. A game update that removes that target must fail mod startup.

Run `python3 scripts/build-dodge-mods.py` from the repository root. Enable **Double Score (Harmony)** in the sample content manager, confirm code execution, and restart. The host owns patch removal during shutdown.
