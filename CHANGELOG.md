# Changelog

## [1.1.0] - 2026-03-19

### Breaking

- Namespace `RSG` → `FluentMachine`.
- NuGet/package id and assembly name `FluentMachine`; strong-name signing removed.
- Targets **.NET Standard 2.1**; library layout is SDK-style (`FluentMachine/`, `FluentMachine.sln`).

### Added

- Broad **xUnit** test coverage (regression, concurrency, lifecycle, etc.).
- **XML documentation** emitted for the library assembly.

### Removed

- Legacy **Unity** example project under `Examples/Unity Example/`.

### Fixed

- **Memory leaks** from callbacks holding builder/state references; actions are typed and disposed with state teardown.
