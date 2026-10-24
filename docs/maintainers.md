# Documentation maintenance

## Work on the docs locally

Install the Python documentation dependencies and serve the site:

```powershell
python -m pip install --requirement requirements-docs.txt
zensical serve
```

Before opening a pull request, run the same strict build used by CI:

```powershell
zensical build --clean --strict
```

Keep documentation changes in the same pull request as the feature or behavior change they describe. The PR workflow builds the documentation to catch configuration errors and warnings.

Zensical reads the existing MkDocs-compatible `mkdocs.yml`, so the project does not need a second configuration file. Do not add MkDocs-only plugins such as Mike: Zensical currently has no native version selector, so older release snapshots are not maintained.

## Publishing

The Pages artifact contains documentation from the latest stable tag that includes `mkdocs.yml` at the site root and documentation from the triggering commit under `/rc/`. RC builds use the `main` commit that triggered the build; release builds use the tagged release commit. Until a stable release includes the documentation, the root site is built from the triggering commit as an initial stable snapshot. Both trees are rebuilt for each deployment because GitHub Pages deploys the complete artifact rather than merging individual paths.

The release workflow publishes after the release ZIP has been created, and the RC workflow publishes after a push to `main`. Both use GitHub Pages artifacts; the source Markdown remains in `docs/`.

Configure GitHub Pages once in the repository settings:

1. Open **Settings → Pages**.
2. Under **Build and deployment**, select **GitHub Actions** as the source.

The site URL configured in `mkdocs.yml` is `https://starmaploader.github.io/StarMap/`.

## Zensical configuration

The [Zensical MkDocs compatibility guide](https://zensical.org/docs/compatibility/mkdocs/migration/) explains how Zensical reads `mkdocs.yml`. Zensical silently ignores unsupported MkDocs plugins, so verify plugin support before adding one.
