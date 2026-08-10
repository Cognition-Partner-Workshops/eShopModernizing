# CI/CD (NET-72)

`.github/workflows/ci.yml` is built around the modernized .NET 8 solution: everything except the
two genuinely Windows-only jobs runs on `ubuntu-latest`. The workflow triggers on pushes and pull
requests to `main` and `devin/net-modernization-integration`, and cancels superseded runs of the
same ref.

## Jobs

| Job | Runner | What it does | Artifacts |
| --- | --- | --- | --- |
| `build-test` | ubuntu-latest | `dotnet restore` / `build -c Release` / `test` over `eShop.sln`, the banned-serializer gate, and a vulnerable-package report | `test-results` (`.trx` + Cobertura coverage + `vulnerable-packages.txt`) |
| `docker-build` | ubuntu-latest | `docker compose build web api grpc` — all three images from NET-71 — and exports them as a tarball | `container-images` (`eshop-images.tar.gz`, 7-day retention) |
| `compose-smoke` | ubuntu-latest | loads those images and runs `scripts/compose-smoke.sh` (mock-data profile): health checks plus the HTTP, UI and gRPC assertions | — (container logs printed on failure) |
| `publish` | ubuntu-latest | `dotnet publish` of `eShop.Web`, `eShop.Catalog.Api` and `eShop.Catalog.Grpc` (matrix) | `eShop.Web-linux`, `eShop.Catalog.Api-linux`, `eShop.Catalog.Grpc-linux` |
| `winforms-client` | windows-latest | self-contained `win-x64` publish of the WinForms client (NET-68) | `eShop.WinForms.Client-win-x64` |
| `legacy-mvc` | windows-latest | NuGet + MSBuild build of the legacy ASP.NET MVC 5 app and utilities, plus the 48 MSTest cases | — |

`compose-smoke` consumes the images produced by `docker-build` instead of rebuilding them, so the
containers CI asserts on are byte-identical to the ones it built. `scripts/compose-smoke.sh` honours
`COMPOSE_BUILD=0` for exactly that case; run it with no environment at all locally and it builds
first, as before.

NuGet packages are cached (`~/.nuget/packages`, keyed on `Directory.Packages.props` + every
`.csproj`) in the two jobs that restore.

## Gates

- **Build**: `TreatWarningsAsErrors` is on for the modernized projects, so any warning fails the job.
- **Tests**: all `eShop.sln` test projects (216 cases); results are published as `.trx` with
  Cobertura coverage. Coverage needs `coverlet.collector`, which is referenced by each test project
  rather than injected from `Directory.Build.props` — a `PackageReference` added there is applied
  during NuGet's TargetFramework-less evaluation pass and breaks restore (`NETSDK1013`).
- **Banned serializers**: `scripts/check-no-binaryformatter.sh` (NET-63) runs unconditionally and
  fails the job — the analyzer gate (`RS0030`) already covers compiled code, this catches the rest.
- **Containers**: an image that fails to build, or a container that never reports healthy or answers
  wrongly, fails CI in `docker-build` / `compose-smoke` rather than in a downstream parity run.
- **Vulnerable packages**: `dotnet list package --vulnerable --include-transitive` is captured as an
  artifact but is **advisory, not blocking**. The current graph reports transitive advisories that
  come from `Microsoft.AspNetCore.Mvc.Testing` / the Azure Monitor exporter and cannot be resolved
  from this repository's direct references, so making it a hard gate would leave CI permanently red.
  Turning it into a gate is a follow-up once those transitive pins are bumped: change the step to
  `dotnet list ... | grep -q "has the following vulnerable packages" && exit 1`.

## Which legacy jobs remain, and why

- `legacy-mvc` (windows-latest) — the legacy ASP.NET MVC 5 solution is still in the tree. NET-69
  ported it to `src/eShop.Web`, but the legacy app has not been deleted yet, so it keeps building
  and running its MSTest suite until the cutover ticket removes it. Delete this job together with
  `eShopLegacyMVCSolution/`.
- The Web Forms job never existed and the solution is gone (NET-70).
- `eShopLegacyNTier/` (WCF service + legacy WinForms client) is still **not** built by CI. It is
  .NET Framework + MSBuild, superseded by `src/eShop.Catalog.Grpc` (NET-66) and
  `src/eShop.WinForms.Client` (NET-68), and adding an unverified Windows msbuild job to a wave that
  is about moving CI to Linux would trade risk for no signal. If it must be covered before the
  cutover, add a `legacy-ntier` job mirroring `legacy-mvc` against `eShopLegacyNTier.sln`.
- `winforms-client` (windows-latest) stays: publishing a `net8.0-windows` WinExe needs a Windows
  agent. The project still *compiles* on Linux as part of `eShop.sln` (`EnableWindowsTargeting`).

## Publishing images to a registry (documented, not implemented)

No container registry is configured for this repository, so nothing is pushed. When one exists, the
push is a tag-triggered addition to `docker-build` — the images are already tagged from
`docker-compose.yml` via `ESHOP_TAG`:

```yaml
on:
  push:
    tags: ['v*']

jobs:
  docker-build:
    permissions:
      contents: read
      packages: write        # GHCR; for another registry use a repository secret instead
    steps:
      # ... existing checkout / buildx steps ...
      - uses: docker/login-action@v3
        if: startsWith(github.ref, 'refs/tags/v')
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Push images
        if: startsWith(github.ref, 'refs/tags/v')
        env:
          ESHOP_TAG: ${{ github.ref_name }}
          REGISTRY: ghcr.io/${{ github.repository_owner }}
        run: |
          for image in web catalog-api catalog-grpc; do
            docker tag "eshop/${image}:${ESHOP_TAG}" "${REGISTRY}/eshop-${image}:${ESHOP_TAG}"
            docker push "${REGISTRY}/eshop-${image}:${ESHOP_TAG}"
          done
```

Notes for whoever implements it: build with `docker/build-push-action` and `cache-from/cache-to:
type=gha` if build time becomes a problem; tag both the version and a moving `latest`; and keep the
smoke test as a required check ahead of the push so no untested image reaches the registry.
