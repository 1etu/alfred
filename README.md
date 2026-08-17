<p align="center">
  <img src="assets/logo.png" width="120" alt="alfred">
</p>

<h1 align="center">alfred</h1>

<p align="center">
  <a href="https://github.com/1etu/alfred/actions/workflows/ci.yml"><img src="https://github.com/1etu/alfred/actions/workflows/ci.yml/badge.svg" alt="ci"></a>
  <a href="https://github.com/1etu/alfred/releases/latest"><img src="https://img.shields.io/github/v/release/1etu/alfred?sort=semver" alt="release"></a>
  <a href="https://github.com/1etu/alfred/releases"><img src="https://img.shields.io/github/downloads/1etu/alfred/total" alt="downloads"></a>
  <img src="https://img.shields.io/badge/platform-windows-blue" alt="windows">
</p>

<p align="center">one place for the things you have to keep track of.</p>

## what it is

alfred is local-first friend that keeps your payments, subscriptions, incoming money, plans, todos, notes,
kanban boards and much more in one small windows app.

## install

grab the latest `alfred-vX.Y.Z-win-x64.zip` from
[releases](https://github.com/1etu/alfred/releases/latest), unzip it, run
`Alfred.exe`. nothing else to install.

your data lives at `%APPDATA%\Alfred\alfred.db`.

## build

you need the [.net 10 sdk](https://dotnet.microsoft.com/download).

```powershell
winget install Microsoft.DotNet.SDK.10
```

then:

```powershell
git clone https://github.com/1etu/alfred.git
cd alfred
dotnet build Alfred.slnx
dotnet run --project src/Alfred.App
```

tests:

```powershell
dotnet test Alfred.slnx
```

a standalone exe:

```powershell
dotnet publish src/Alfred.App -c Release -r win-x64 --self-contained
```

## layout

```
src/Alfred.App       the wpf app — pages and view models
src/Alfred.Core      domain, rules, storage
src/Alfred.Theme     theme framework and the built-in light/dark themes
src/Alfred.UIKit     the component library every page is built from
src/Alfred.Widgets   windows 11 widget provider
tests/               unit tests
```

## releasing

tag it and push. the release workflow builds, tests, packages and publishes to
github releases.

```powershell
git tag v0.1.0
git push origin v0.1.0
```

## status

under active development.
