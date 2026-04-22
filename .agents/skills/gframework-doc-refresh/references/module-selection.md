# Module Selection

Use `.agents/skills/_shared/module-map.json` as the canonical source for:

- supported modules
- aliases
- source paths
- test projects
- README paths
- docs landing/topic/fallback pages
- `ai-libs/` reference roots

Selection rules:

1. Prefer explicit canonical module names.
2. Resolve docs section aliases back to source modules before scanning docs.
3. If an alias maps to multiple modules, stop and return the candidate list.
4. If a module has no dedicated docs section, fall back to the nearest existing landing page or API index instead of inventing a fake section.

Representative ambiguous inputs:

- `source-generators` -> likely one of `Core.SourceGenerators`, `Game.SourceGenerators`, `Cqrs.SourceGenerators`, `Godot.SourceGenerators`
- `abstractions` -> likely one of `Core.Abstractions`, `Game.Abstractions`, `Ecs.Arch.Abstractions`

Representative resolvable aliases:

- `core-abstractions` -> `Core.Abstractions`
- `godot generators` -> `Godot.SourceGenerators`
- `ecs` -> `Ecs.Arch`
