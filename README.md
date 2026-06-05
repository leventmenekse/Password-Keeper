# PasswordKeeper

A local-first password manager for Windows. Your vault is a single encrypted file on your machine — there is no cloud, no account, and no telemetry. Unlock it with one master password; it lives quietly in the system tray and locks itself when you step away.

Built with WPF on .NET 9.

---

## Features

- **Strong encryption** — vault encrypted with **AES-256-GCM**; the key is derived from your master password with **Argon2id** (128 MiB memory, 3 iterations, 4 lanes). The encrypted header is bound as authenticated data, so tampering is detected.
- **Entries** — title, username, password, URL, notes, plus arbitrary **custom fields**, organized into **categories**.
- **Password generator** — configurable length, character classes, and an option to exclude ambiguous characters.
- **System tray app** — closing the window hides it to the tray instead of quitting; a global hotkey brings it back from anywhere.
- **Auto-lock** — the vault locks automatically after a configurable period of inactivity.
- **Clipboard auto-clear** — copied secrets are wiped from the clipboard after a configurable timeout.
- **Launch at startup** — optionally start with Windows (per-user), opening silently and locked in the tray.
- **No secrets at rest in the clear** — the vault file holds only ciphertext, the nonce, and the KDF parameters.

## Security model

- The master password is **never stored**. It is run through Argon2id to derive a 32-byte key each time you unlock.
- The vault file (`vault.json`) is a small JSON envelope containing the KDF parameters, a random nonce, the AES-GCM ciphertext, and the authentication tag — nothing else.
- The KDF parameters and nonce are included as **Additional Authenticated Data (AAD)**, so altering the header invalidates decryption.
- A correct master password is required to decrypt; there is no recovery mechanism or backdoor. **If you forget your master password, your data cannot be recovered.**

> ⚠️ This project has not undergone an independent security audit. Review the code and use it at your own risk.

## Where your data lives

| What | Location |
|------|----------|
| Vault | `%AppData%\PasswordKeeper\vault.json` |
| Preferences | `%AppData%\PasswordKeeper\preferences.json` |

These live outside the repository and are excluded by `.gitignore`. Back up `vault.json` yourself — losing it means losing your passwords.

## Requirements

- Windows 10 / 11 (x64)
- [.NET 9 SDK](https://dotnet.microsoft.com/download) to build from source

## Build & run

```powershell
# Run in development
dotnet run --project src/PasswordKeeper.App

# Run the tests
dotnet test
```

### Produce a release build

`build-release.ps1` publishes a **self-contained, single-file** executable (no .NET install required on the target machine) and zips it into `dist/`:

```powershell
./build-release.ps1
```

Output:

- `src/PasswordKeeper.App/bin/Release/net9.0-windows/win-x64/publish/PasswordKeeper.App.exe`
- `dist/PasswordKeeper-<version>-win-x64.zip`

For an ARM64 build, change `$Rid` to `win-arm64` in `build-release.ps1`.

## Project structure

```
PasswordKeeper.sln
├── src/
│   ├── PasswordKeeper.Core/        # Crypto, models, vault store, password generator (no UI)
│   │   ├── Crypto/                 # Argon2id KDF, AES-256-GCM cipher, SecureBuffer
│   │   ├── Generators/             # Password generator
│   │   ├── Models/                 # Vault, VaultEntry, Category, KDF + envelope models
│   │   └── Vault/                  # File-backed vault store + serialization
│   └── PasswordKeeper.App/         # WPF app (MVVM)
│       ├── Services/               # Tray, global hotkey, idle timer, clipboard, startup, preferences
│       ├── ViewModels/
│       └── Views/
└── tests/
    └── PasswordKeeper.Tests/       # Unit + integration tests for the Core library
```

The core cryptography and vault logic live in `PasswordKeeper.Core`, which has no UI dependencies and is covered by the test project. The WPF layer (`PasswordKeeper.App`) follows MVVM and uses dependency injection (`Microsoft.Extensions.DependencyInjection`) and the [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) source generators.

## Tech stack

- .NET 9, C#
- WPF (`net9.0-windows`)
- [Konscious.Security.Cryptography.Argon2](https://github.com/kmaragon/Konscious.Security.Cryptography) — Argon2id key derivation
- `System.Security.Cryptography.AesGcm` — authenticated encryption
- CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection

## License

Released under the [MIT License](LICENSE) — © 2026 Levent Menekse. You're free to use, modify, and distribute this software; it is provided "as is", without warranty of any kind.
