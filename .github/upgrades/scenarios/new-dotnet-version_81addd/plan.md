# .NET 10 Upgrade Plan

Table of contents

- Executive Summary
- Migration Strategy
- Detailed Dependency Analysis
- Project-by-Project Plans
- Package Update Reference
- Breaking Changes Catalog
- Testing & Validation Strategy
- Risk Management
- Complexity & Effort Assessment
- Source Control Strategy
- Success Criteria

---

## Executive Summary

### Selected Strategy
**All-At-Once Strategy** - All projects upgraded simultaneously in a single coordinated operation.

**Rationale**:
- Solution size: 1 project (small)
- Project type: WinForms desktop app using SDK-style project
- Existing assessment shows NuGet compatibility for `ReaLTaiizor` and most packages, but many API incompatibilities due to Windows Forms / System.Drawing usage.
- Team can accept short period of compilation/testing work across entire solution.

### High-level Metrics (from assessment)
- Projects analyzed: 1 (`ReaLTaiizor.UI\RCL.csproj`)
- Current target: `net9.0-windows`
- Proposed target: `net10.0-windows`
- Total files: 43
- Files with incidents: 10
- API issues: large number of binary/source incompatibilities (see Detailed Analysis)

### Scope
- Update all projects to `net10.0-windows` and apply package updates specified in the assessment.
- Update project files, Directory.Build.* imports (if any), and global.json (if present).
- Restore packages and build entire solution; fix all compilation errors in the same atomic operation.
- Run unit/integration tests and repair failing tests.


## Migration Strategy

Selected approach: **All-At-Once Strategy**

- Overview: Update `ReaLTaiizor.UI\RCL.csproj` and any shared MSBuild imports to target `net10.0-windows` in a single atomic operation. After changes are made, restore packages and build the entire solution to reveal compilation issues, then fix all issues within the same upgrade commit.

- Preconditions:
  - Ensure .NET 10 SDK is installed on build machines and validated. Update `global.json` if present to reference the .NET 10 SDK.
  - Commit or stash pending changes (assessment recommended: commit). Create and switch to the upgrade branch `upgrade-to-NET10`.

- Key actions in atomic upgrade (single coordinated batch):
  1. Update `TargetFramework` in `RCL.csproj` to `net10.0-windows`.
  2. Inspect `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props` for imported framework/package settings and update if necessary.
  3. Update package references as specified in the assessment (include security updates).
  4. Restore NuGet packages.
  5. Build solution and fix all compilation errors (API and source incompatibilities).
  6. Rebuild and verify zero compilation errors.

- Testing phase (post-atomic upgrade):
  - Run all unit and integration tests. Fix failures.
  - Conduct targeted runtime validation of WinForms behaviors impacted by behavioral changes.

- Source control approach:
  - Create single upgrade branch `upgrade-to-NET10` from `master`.
  - Apply all changes in a single commit (or a small set of logically grouped commits) to keep the atomic nature of the upgrade.
  - Open PR for team review with description and known breaking changes.


## Detailed Dependency Analysis

- Projects in solution (topological order):
  - `ReaLTaiizor.UI\RCL.csproj` (leaf and root)

- Dependency graph summary:
  - Single SDK-style WinForms project with no project-to-project references. This simplifies an All-At-Once upgrade: only one project needs its TargetFramework updated.

- Migration phases (All-At-Once):
  - Phase: Atomic upgrade - update project target framework and package references for all projects simultaneously (only `RCL.csproj`).

- Critical path:
  - Modify `RCL.csproj` TargetFramework(s) → Restore packages → Build solution → Fix compilation errors reported due to API/behavioral changes.

- Circular dependencies: None detected.


## Project-by-Project Plans

### Project: `ReaLTaiizor.UI\RCL.csproj`

**Current State**:
- Target framework: `net9.0-windows`
- SDK-style: Yes
- Project type: WinForms desktop
- Files: 43 (10 with incidents)
- Noted issues from assessment: Binary incompatibilities, source incompatibilities, behavioral changes

**Target State**:
- Target framework: `net10.0-windows`
- All package updates applied as per assessment
- Solution builds with 0 compilation errors

**Migration Steps (atomic, part of All-At-Once upgrade)**:
1. Prerequisites
   - Validate .NET 10 SDK installation on developer and CI machines.
   - Commit pending changes on `master` (or as specified). Create and switch to `upgrade-to-NET10` branch.
2. Project file updates (single pass)
   - Update `<TargetFramework>` to `net10.0-windows` in `RCL.csproj`.
   - If project is multi-targeting, append `net10.0-windows` to `TargetFrameworks` instead of replacing existing targets.
3. Package updates
   - Apply package updates listed in the assessment (`ReaLTaiizor` is compatible; include any other packages listed in `assessment.md`).
   - Ensure System.Drawing usage is acceptable; add `System.Drawing.Common` if required or plan migration to alternative imaging library if necessary.
4. Build & Fix
   - Restore packages and build entire solution.
   - Fix all compilation errors caused by framework and package upgrades.
   - Pay attention to WinForms-related API changes (Control layout, TableLayoutPanel, Anchor/Size/Location properties, PlaceholderText usage, etc.).
5. Tests
   - Run all test projects (if any). Address failing tests.
6. Finalize
   - Ensure no security vulnerabilities remain in packages.
   - Create PR from `upgrade-to-NET10` to `master` with migration notes and known issues.

**Expected Breaking Areas**:
- Numerous WinForms API usages flagged as binary-incompatible; expect to update certain control properties or method calls.
- System.Drawing font/graphics constructors and usages may need explicit parameter adjustments.

**Validation Checklist**:
- [ ] `RCL.csproj` TargetFramework updated to `net10.0-windows`
- [ ] All package updates applied from assessment
- [ ] Solution builds with 0 errors
- [ ] Unit/integration tests pass
- [ ] No high-severity security vulnerabilities remain


## Package Update Reference

### Summary
Assessment found the following packages referenced by the solution. Include all suggested updates and address security issues immediately.

| Package | Current Version | Suggested Version | Projects Affected | Reason |
|---|---:|---:|---|---|
| `ReaLTaiizor` | `3.8.1.5` | (no change suggested) | `ReaLTaiizor.UI\RCL.csproj` | Compatible with .NET 10 — no update required per assessment

Notes:
- If other packages are discovered in `Directory.Packages.props` or per-project PackageReference entries, include their exact current and target versions here. The assessment reported no incompatible NuGet packages requiring upgrades besides project TFMs.
- For System.Drawing usages, if the project relies on `System.Drawing.Common`, ensure the package is referenced for compatibility on Windows or migrate to a cross-platform library if targeting non-Windows platforms in future.


## Breaking Changes Catalog

This section lists categories of expected breaking changes (from assessment) and remediation guidance.

1. Windows Forms API binary incompatibilities
   - Affected areas: `ComboBox`, `Label`, `TextBox`, `TableLayoutPanel`, `TabControl`, `PictureBox`, various properties (Size, Location, TabIndex, Margin, Anchor)
   - Remediation:
     - Recompile and inspect each compile-time error; adapt to new method/property signatures.
     - Replace removed/obsolete controls with supported alternatives (e.g., ensure `ContextMenuStrip`/`MenuStrip` used instead of legacy Menu items).
     - Verify layout behavior in runtime testing; adjust `TableLayoutPanel` RowStyles/ColumnStyles constructors where needed.

2. System.Drawing / Font constructors
   - Affected APIs: `System.Drawing.Font` constructors, `GraphicsUnit` usage
   - Remediation:
     - Update constructors to use supported overloads.
     - If cross-platform support is required later, plan migration to `SkiaSharp` or `ImageSharp`.

3. PlaceholderText on TextBox (winforms)
   - `PlaceholderText` is available beginning with recent Windows Forms; if missing APIs cause issues, replace with helper code or confirm new target supports it.

4. Behavioral changes
   - Small number of behavioral changes detected; add targeted runtime tests focusing on forms rendering, layout, and event ordering.

5. Build/SDK changes
   - Ensure `UseWindowsForms` and `UseWindowsDesktop` settings are correct for WinForms projects if required.
   - If `global.json` pins SDK, update to the .NET 10 SDK.

6. Configuration
   - Legacy `app.config` behavior: consider migrating to `Microsoft.Extensions.Configuration` if the project plans long-term modernization.

For each compilation error discovered during the atomic build, consult the API compatibility report (`assessment.md`) to find suggested fixes and replacements. Flag any unresolved binary incompatibility for deeper code change.


## Testing & Validation Strategy

Testing levels and validations to perform after the atomic upgrade:

1. Build verification (automated)
   - Restore all packages and run `dotnet build` for the solution on the upgrade branch.
   - Success criterion: build completes with 0 errors.

2. Unit and Integration Tests (automated)
   - Discover test projects and run full test suite.
   - Success criterion: all tests pass. Any test failures must be triaged and fixed on the upgrade branch.

3. WinForms runtime validation (manual/automated where possible)
   - Launch the application on Windows to verify UI loads, critical forms display correctly, and interactive features behave as before.
   - Validate `TableLayoutPanel` layouts, `PictureBox` rendering, and font rendering.

4. Behavioral regression tests
   - Focus on scenarios flagged as behavioral-change risks in the assessment. Use automated UI tests (if present) or manual validation steps.

5. Security and package validation
   - Run `dotnet list package --vulnerable` or an SCA tool to ensure no critical vulnerabilities remain.

6. Acceptance
   - Prepare a short acceptance checklist for QA to confirm UI and feature parity.

Note: Manual UI validation is outside the automated task scope; document findings and required fixes in PR comments.


## Risk Management

High-risk items:
- Large number of binary-incompatible WinForms APIs (High). Mitigation: allocate developer time to fix compile errors and design small, focused fixes; rely on the assessment to locate hotspots.
- System.Drawing font/graphics changes (Medium). Mitigation: validate constructors and consider targeted replacements.

Mitigation strategies:
- Ensure full repo backup and branch created before changes.
- Keep changes in a single upgrade branch `upgrade-to-NET10` to centralize review.
- Use feature flags or abstraction layers when replacing platform-dependent code to reduce blast radius.
- Increase test coverage for UI and behavior-heavy components.

Contingency and rollback:
- If the upgrade introduces blocking regressions, revert the upgrade branch or open a hotfix PR to master.
- For unresolved binary incompatibilities that require design changes, mark them as "Requires design change" in PR and defer to a follow-up task.

## Complexity & Effort Assessment

Per-project complexity rating (relative):
- `ReaLTaiizor.UI\RCL.csproj`: Medium–High complexity due to heavy WinForms usage and many API incompatibilities. Risk: High for UI behavior and layout.

Notes:
- No time estimates provided. Use relative complexity to decide reviewer allocation and testing effort.


## Source Control Strategy

- Start from `master` branch (current). Ensure pending changes are committed.
- Create upgrade branch: `upgrade-to-NET10`.
- Make all project file and package updates on `upgrade-to-NET10` as a single atomic change set.
- Use a single PR for review with clear description of changes, links to `assessment.md`, and list of known behavioral changes.
- Code review checklist: project file updates, package updates, compilation success, test results, UI verification notes.

## Success Criteria

The upgrade is complete when all of the following are true:
1. All projects target `net10.0-windows` (or include it in multi-targeting list).
2. All package updates from the assessment are applied.
3. Solution builds with 0 compilation errors.
4. Unit and integration tests pass.
5. No high-severity security vulnerabilities exist in referenced packages.
6. Key UI behaviors validated for WinForms features affected by the upgrade.

