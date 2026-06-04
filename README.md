# muse-game-jam-2026

Gioco realizzato durante la **Muse Game Jam 2026**, in **Unity 6.3 LTS** (`6000.3.6f1`), build **Windows standalone** (`.exe`).

Progetto **Pigna Labs**. Tracking task su Plane (progetto **Muse Game Jam**).

## Requisiti

- **Unity 6.3 LTS** — versione esatta `6000.3.6f1` (installa via Unity Hub).
- **Git LFS** — gli asset binari (immagini, audio, modelli, font…) sono tracciati con LFS. Dopo il clone:
  ```bash
  git lfs install
  git lfs pull
  ```

## Come aprire il progetto

1. Apri **Unity Hub** → *Add* → seleziona la cartella di questa repo.
2. Hub propone la versione `6000.3.6f1`: aprilo con quella (evita upgrade involontari).

> ⚠️ L'init del progetto Unity (cartelle `ProjectSettings/`, `Packages/`, file `.meta`) viene generato da Unity al primo open e committato a parte. Questo scaffold contiene solo i placeholder non-Unity (`.gitignore`, `.gitattributes`, `README`).

## Come buildare (Windows)

1. `File → Build Profiles` → piattaforma **Windows** → *Switch Platform*.
2. *Build* → output in `Builds/Windows/` (cartella ignorata da git).

## Convenzioni

- Serializzazione asset: **Force Text** → diff/merge leggibili.
- Meta files visibili e versionati.
- Non committare le cartelle `Library/`, `Temp/`, `Builds/` (vedi `.gitignore`).

## Team

Game jam Pigna Labs — Andrea, Amerigo, Ovidiu, Davide.
