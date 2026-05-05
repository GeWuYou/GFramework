# Runtime / Generator Boundary Trace

## 2026-05-05

### RGB-RP-001 Runtime package boundary repair

- Trigger:
  - external consumers restoring `GeWuYou.GFramework.Game` failed because NuGet looked for
    `GFramework.Core.SourceGenerators.Abstractions`
  - repository inspection showed `GFramework.Game` had a direct project reference to a non-packable generator
    abstractions project and used `[GenerateEnumExtensions]`
- Decisions:
  - treat the issue as a runtime/generator boundary violation, not as a missing publish target
  - remove the runtime-side attribute usage instead of turning generator abstractions into public runtime packages
  - add repository guardrails at both source-validation time and packed-package validation time
- Expected implementation:
  - `GFramework.Game` removes the generator abstractions project reference
  - `GFramework.Game` removes the two unused enum generator attributes
  - CI and publish workflows run a dedicated boundary validator script
- Immediate next step:
  - complete implementation and run Release build plus pack verification
