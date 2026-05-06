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
- PR review follow-up:
  - verified CodeRabbit and Greptile findings against local source before acting on them
  - accepted the validator regex finding because the original pattern missed standalone
    `[GenerateEnumExtensions]` declarations in runtime code
  - added comment-line filtering after the first regex repair surfaced false positives from XML documentation examples
    such as `/// [ContextAware]`
  - rejected the documentation reposition suggestion as stated and removed the
    `代码生成器边界` block from `docs/zh-CN/contributing.md` because it documents internal governance rather than
    reader-facing contributor guidance
  - added a Python regression test covering standalone, parameterized, fully qualified, and multi-attribute matches
- Validation milestone:
  - `python3 scripts/test_validate_runtime_generator_boundaries.py` passed
  - `python3 scripts/validate-runtime-generator-boundaries.py` passed
  - `python3 scripts/license-header.py --check` passed
  - `dotnet build GFramework.Game/GFramework.Game.csproj -c Release` passed with 0 warnings and 0 errors
- Immediate next step:
  - push the PR follow-up commit and resolve the remaining review threads
