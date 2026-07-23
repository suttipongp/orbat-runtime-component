# ORBAT Web Harness

Run from the repository root:

```powershell
npm run plugin:build
npm run web:harness
```

Open `http://localhost:4173/samples/Orbat.WebHarness/`.

The harness loads `dist/c4isr-orbat-plugin/manifest.json`, all checksummed definitions, and the packaged ES-module renderer. It does not use a duplicate renderer or WinForms code.
