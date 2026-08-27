# Deploying City-Drive to GitHub Pages

Unity WebGL build settings in this repo (for GitHub Pages):

- **Compression:** Gzip + decompression fallback (not Brotli)
- **Template:** `Assets/WebGLTemplates/Fullscreen` (canvas fills the page)

## One-time GitHub setup

### 1. Enable Pages

1. Repo **Settings → Pages**
2. **Build and deployment → Source**: **Deploy from a branch**
3. **Branch**: `gh-pages` / **/ (root)** → Save

The workflow pushes the WebGL build to the `gh-pages` branch on each deploy.

If `gh-pages` does not appear yet, run one successful deploy first (step below), then set Pages source.

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

```bash
git checkout your-branch
git pull
git add -A && git commit -m "your message"   # include WebGL fixes
git push origin your-branch
git tag deploy-1.3
git push origin deploy-1.3
```

Any tag matching `deploy-*` triggers a full WebGL build (~15–25 min) and deploy to `gh-pages`.

## Deploy a branch without a tag

1. GitHub → **Actions** → **Deploy WebGL to GitHub Pages**
2. **Run workflow**
3. Choose **branch**
4. Run

## After deploy

- Site: `https://dptspartan.github.io/DriveInOffice/`
- Game fills the browser window; click overlay (or wait briefly) for browser fullscreen if allowed.

## Troubleshooting

| Issue | Fix |
|-------|-----|
| `.br` / brotli errors | Rebuild after pulling — project uses **Gzip** now |
| `content-encoding: gzip` but still broken | Enable **Decompression Fallback** (already in project); redeploy |
| License activation failed | Re-create `UNITY_LICENSE` secret |
| `gh-pages` branch missing | Complete one deploy; then set Pages source |
| Blank page | Pages → **gh-pages** branch, **/ (root)**; check Console for errors |
| Fullscreen blocked | Normal on some browsers — click the page once; canvas still fills the tab |

## Verify in Unity (optional)

**Edit → Project Settings → Player → WebGL → Publishing Settings**

- Compression Format: **Gzip**
- Decompression Fallback: **enabled**
- WebGL Template: **Fullscreen**
