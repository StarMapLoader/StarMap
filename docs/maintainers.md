# Documentation maintenance

## Work on the docs locally

Install the Python documentation dependencies and serve the site:

```powershell
python -m pip install --requirement requirements-docs.txt
python -m mkdocs serve
```

Before opening a pull request, run the same strict build used by CI:

```powershell
python -m mkdocs build --strict
```

Keep documentation changes in the same pull request as the feature or behavior change they describe. The PR workflow builds the documentation to catch configuration errors and warnings.

## Versioned publishing

The release workflow runs from semantic-version tags in `X.Y.Z` format. It publishes a documentation snapshot when the major/minor portion changes from the previous reachable release tag. Mike names the snapshot `X.Y`, preserves older snapshots, updates the `latest` alias, and makes that alias the site's default. Patch releases do not create additional snapshots. If the `gh-pages` branch does not exist yet, the first tagged release bootstraps the documentation site.

The workflow publishes documentation after the release ZIP has been created. It pushes generated site files to the `gh-pages` branch; the source Markdown remains in `docs/`.

After the first deployment, configure GitHub Pages once in the repository settings:

1. Open **Settings → Pages**.
2. Under **Build and deployment**, select **Deploy from a branch**.
3. Select the `gh-pages` branch and the `/(root)` folder, then save.

The site URL configured in `mkdocs.yml` is `https://starmaploader.github.io/StarMap/`.

## Theme maintenance

The current Material for MkDocs release announces end-of-life on May 5, 2027. Mike provides the versioning workflow separately from the theme, so review theme support before that date. The [Zensical MkDocs compatibility guide](https://zensical.org/docs/compatibility/mkdocs/migration/) describes the successor project's migration path.
