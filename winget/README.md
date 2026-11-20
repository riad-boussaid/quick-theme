# Winget packaging

Package ID: **`RiadBoussaid.QuickTheme`**

The manifests in this directory describe the current GitHub release. They are
submitted to [`microsoft/winget-pkgs`](https://github.com/microsoft/winget-pkgs)
so users can install with:

```powershell
winget install RiadBoussaid.QuickTheme
```

## Generate manifests

```powershell
.\winget\generate-manifests.ps1 `
  -Version 1.7.0 `
  -InstallerUrl "https://github.com/riad-boussaid/quick-theme/releases/download/v1.7.0/quick-theme-1.7.0-setup.exe" `
  -InstallerSha256 "820729CEFEAC4C1F9F8D01002C785A2F70C87B01FBC0517D164B30E853FA58AE"
```

This writes the three files required by winget under
`winget/manifests/r/RiadBoussaid/QuickTheme/<version>/`.

## Validate locally

```powershell
winget validate --manifest .\winget\manifests\r\RiadBoussaid\QuickTheme\1.7.0
```

## Test locally

Local manifest installs require the feature to be enabled by an administrator:

```powershell
winget settings --enable LocalManifestFiles
winget install --manifest .\winget\manifests\r\RiadBoussaid\QuickTheme\1.7.0
```

## Submit

The Microsoft WinGet Releaser action requires **at least one version** of the
package to already exist in `microsoft/winget-pkgs`. Because of that, the first
version must be submitted manually.

For the initial submission you can either:

- Open a PR to `microsoft/winget-pkgs` adding the files under
  `manifests/r/RiadBoussaid/QuickTheme/1.6.1/`, or
- Install WingetCreate and run:

  ```powershell
  winget install --id Microsoft.WingetCreate --exact
  wingetcreate submit .\winget\manifests\r\RiadBoussaid\QuickTheme\1.6.1 --token <token>
  ```

After the initial package is merged, the release workflow dispatches
`.github/workflows/winget.yml` for future `v*` releases, provided the
`WINGET_TOKEN` repository secret is configured. The explicit dispatch is needed
because GitHub does not run workflows from `release` events created with the
repository's own `GITHUB_TOKEN`.

Create a classic personal access token with the `public_repo` and `workflow`
scopes and add it as a repository secret named `WINGET_TOKEN`. The token must be
allowed to fork `microsoft/winget-pkgs` and open pull requests from your account.

Fork `microsoft/winget-pkgs` once under the same GitHub account. The action uses
that fork to open the manifest PR.

You can trigger the first automated submission manually from the **Winget**
workflow with tag `v1.6.1` after the initial version is in the repository.
Without the secret, the workflow skips submission safely.
