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
  -Version 1.5.0 `
  -InstallerUrl "https://github.com/riad-boussaid/quick-theme/releases/download/v1.5.0/quick-theme-1.5.0-setup.exe" `
  -InstallerSha256 "B0C70BF3C58FC6B9F6830B15713023B9185D3A9C59C964830BCBBC3B45CFA226"
```

This writes the three files required by winget under
`winget/manifests/r/RiadBoussaid/QuickTheme/<version>/`.

## Validate locally

```powershell
winget validate --manifest .\winget\manifests\r\RiadBoussaid\QuickTheme\1.5.0
```

## Test locally

Local manifest installs require the feature to be enabled by an administrator:

```powershell
winget settings --enable LocalManifestFiles
winget install --manifest .\winget\manifests\r\RiadBoussaid\QuickTheme\1.5.0
```

## Submit

The Microsoft WinGet Releaser action requires **at least one version** of the
package to already exist in `microsoft/winget-pkgs`. Because of that, the first
version must be submitted manually.

For v1.5.0 you can either:

- Open a PR to `microsoft/winget-pkgs` adding the files under
  `manifests/r/RiadBoussaid/QuickTheme/1.5.0/`, or
- Install WingetCreate and run:

  ```powershell
  winget install --id Microsoft.WingetCreate --exact
  wingetcreate submit .\winget\manifests\r\RiadBoussaid\QuickTheme\1.5.0 --token <token>
  ```

After v1.5.0 is merged, `.github/workflows/winget.yml` automatically submits
future releases when a `v*` release is published, provided the `WINGET_TOKEN`
repository secret is configured.

Create a classic personal access token with the `public_repo` and `workflow`
scopes and add it as a repository secret named `WINGET_TOKEN`. The token must be
allowed to fork `microsoft/winget-pkgs` and open pull requests from your account.

Fork `microsoft/winget-pkgs` once under the same GitHub account. The action uses
that fork to open the manifest PR.

You can trigger the first automated submission manually from the **Winget**
workflow with tag `v1.5.0` after the initial version is in the repository.
Without the secret, the workflow skips submission safely.
