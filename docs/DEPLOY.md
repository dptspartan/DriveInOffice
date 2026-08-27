# Deploying City-Drive to GitHub Pages

This repo builds **Unity WebGL** (`City-Drive` scene) and publishes to GitHub Pages.

## One-time GitHub setup

### 1. Enable Pages

1. Repo **Settings → Pages**
2. **Build and deployment → Source**: **GitHub Actions**

### 2. Unity license secrets (required for CI)

GameCI needs a Unity license on the runner. For a **Personal** license:

1. Install [Unity Hub](https://unity.com/download) locally (same version as the project: **6000.5.8f1**).
2. Follow [GameCI activation](https://game.ci/docs/github/activation):
   - Request a manual activation file from Unity.
   - Upload it to Unity to get a `.ulf` license file.
3. In GitHub: **Settings → Secrets and variables → Actions → New repository secret**
   - `UNITY_LICENSE` — paste the **entire** contents of the `.ulf` file.
   - `UNITY_EMAIL` — your Unity account email.
   - `UNITY_PASSWORD` — your Unity account password.

For **Unity Pro**, you can use `UNITY_SERIAL` instead of the personal flow (see GameCI docs).

## Deploy with a tag (recommended)

Tag the commit you want on any branch, then push the tag:

```bash
git checkout some-more-work   # or main
git pull
git tag deploy-1.1
git push origin deploy-1.1
```

Any tag matching `deploy-*` triggers the workflow and deploys **that commit** to Pages.

Examples: `deploy-1.0`, `deploy-1.1`, `deploy-beta`

## Deploy a branch without a tag

1. GitHub → **Actions** → **Deploy WebGL to GitHub Pages**
2. **Run workflow**
3. Choose **branch** (e.g. `main`, `some-more-work`)
4. Optional **deploy label** (e.g. `deploy-1.1`)
5. Run

## After deploy

- Site URL: `https://dptspartan.github.io/DriveInOffice/` (if repo is public and default Pages URL)
- Check **Actions** tab for build logs (first WebGL build can take 30–60+ minutes).
- Build info is written to `deploy-info.txt` in the published site root.

## Local WebGL build (optional)

```bash
# Unity Editor: File → Build Settings → WebGL → Build
# Or use the same method as CI via batchmode once WebGLBuild.cs is in the project.
```

## Troubleshooting

| Issue | Fix |
|-------|-----|
| License activation failed | Re-create `UNITY_LICENSE` secret; check email/password |
| Build runs out of disk | Workflow already frees disk space; retry |
| Blank page / WASM errors | Ensure Pages source is **GitHub Actions**; `.nojekyll` is added automatically |
| Wrong scene | CI builds only `Assets/Scenes/City-Drive.unity` via `WebGLBuild.cs` |
