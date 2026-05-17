# Version Control Flow

Use three long-lived promotion branches:

- `test`: first landing branch for new changes and web testing.
- `bronze`: pre-release branch after `test` is verified.
- `silver`: stable release branch.

## Daily Flow

1. Commit new work to `test`.
2. Deploy or test from `test`.
3. Promote the exact tested commit to `bronze`.
4. Promote the exact bronze-approved commit to `silver`.

## Promotion Commands

Promote `test` to `bronze`:

```powershell
git switch bronze
git merge --ff-only test
git push origin bronze
```

Promote `bronze` to `silver`:

```powershell
git switch silver
git merge --ff-only bronze
git push origin silver
```

## Rollback

To return a branch to a known older commit:

```powershell
git switch silver
git reset --hard <known-good-commit>
git push --force-with-lease origin silver
```

Use rollback only when a deployed branch must be moved back. For normal fixes, make a new commit on `test` and promote it forward.

## Current Branch Purpose

- `master`: existing historical baseline.
- `test`: active candidate work.
- `bronze`: verified pre-release.
- `silver`: stable release.

Keep database connection strings pointed at the intended database. Do not enable sample data seeding except for disposable local demos.
