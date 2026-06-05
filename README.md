# muse-game-jam-2026

Gioco realizzato per la **Museum Game Jam @ MUSE** (5–6 giugno 2026, Trento), in **Unity 6.3 LTS** (`6000.3.6f1`), pipeline **URP**. Consegna = **APK Android** su itch.io; build **Windows** `.exe` come extra per la demo.

Progetto **Pigna Labs**. Team: Andrea, Amerigo, Ovidiu, Davide. Tracking task su Plane (progetto **MGJ — Muse Game Jam**).

---

## 🎮 La jam

**Tech Tales — Museum Game Jam**, prima edizione, organizzata da **IGDA Trentino** + **MUSE — Museo delle Scienze di Trento**.

| | |
|---|---|
| **Quando** | Start **ven 5 giugno ore 15:00** → fine **sab 6 giugno ore 15:00** (24h, si dorme al museo) |
| **Dove** | MUSE — Museo delle Scienze, Trento (apertura 10:00–18:00) |
| **Cosa** | Realizzare un videogioco partendo *dall'atmosfera e da ciò che è custodito tra le mura del MUSE* |
| **🚨 Deadline build** | **Sab 6 giugno ore 14:30** — upload della build giocabile su **itch.io** (sito della jam) |
| **Consegna** | **APK Android scaricabile** caricato su itch.io. Windows `.exe` = extra per la demo |
| **Pitch** | Sab ore 15:30 — **5 min pitch + 5 min Q/A** per progetto |

> ⚠️ **Niente "tema segreto" rivelato all'ora X** (non è la Global Game Jam): il tema *è il MUSE stesso* — la sua atmosfera, i suoi exhibit, i suoi contenuti scientifici. L'angolo preciso lo scegliamo allo start, sul posto, ispirandoci al museo.

### 📅 Programma ufficiale

| Quando | Cosa |
|---|---|
| **Ven 5 — 15:00** | 🚦 **START** — ideazione / coding con supporto IGDA |
| Ven 5 — 18:00 | Chiusura museo al pubblico; cena Jammers in terrazza → **notte di programmazione** |
| Sab 6 — 08:00–09:00 | Colazione al MUSE Cafè |
| Sab 6 — 12:00–14:00 | Pranzo (panino/insalata) al MUSE Cafè |
| **Sab 6 — 14:30** | 🚨 **DEADLINE: upload build su itch.io** |
| Sab 6 — 15:00 | Fine jam + consegna presentazione (15:00/15:30) |
| **Sab 6 — 15:30** | 🎤 Presentazioni: **pitch 5 min + Q/A 5 min** |
| Sab 6 — 16:30 | Giuria + **demo giocabile aperta al pubblico** (postazioni playtest) |
| **Sab 6 — 17:00** | 🏆 Cerimonia di chiusura + **premiazione** |

> 🍕 Cena ven + colazione/pranzo sab inclusi e gratuiti. Si può portare cibo/bevande in autonomia o farsi consegnare delivery. Si dorme al museo: porta **sacco a pelo, materassino-extra, cuscino, tappi/mascherina**.
>
> 📜 Partecipando accetti regolamento + **Code of Conduct/Ethics di IGDA**. Rispetta gli spazi, raccolta differenziata, niente download illegali sulla rete del museo, vietato fumare dentro. Per ogni necessità → volontari col badge **Mentor/Volunteers**.

## 🏔️ Il contesto: cos'è e cosa custodisce il MUSE

Il MUSE racconta la vita sulla Terra con la **metafora della montagna**: si parte dalla cima e si scende, piano dopo piano, dai ghiacci alla biodiversità. È materiale d'ispirazione ricchissimo per un gioco. Spunti tematici concreti:

- **La montagna / i ghiacci** — terrazza e piani alti: sole, ghiacciai, alta quota, cambiamento climatico.
- **Evoluzione e tempo profondo** — dalla comparsa delle prime molecole ai **dinosauri** (la più grande mostra di dinosauri dell'arco alpino) e ai mammiferi.
- **Biodiversità & sostenibilità** — la rete della vita, l'impatto umano, lo sviluppo sostenibile (tema portante del museo).
- **Serra tropicale** — 600 m² che ricreano la foresta pluviale dei Monti Udzungwa (Tanzania): caldo, umido, vita brulicante.
- **Geologia & preistoria** — le Dolomiti, le rocce, la storia profonda del territorio.
- **Tech Tales (cornice dell'evento)** — il rapporto quotidiano con il digitale, il visitatore come esploratore/scienziato/narratore.

## 💡 Spunti di idea (da validare in jam)

Brainstorm di partenza — *non vincolante*, serve solo a non partire da foglio bianco alle 14:00:

- **Esplora-museo**: il giocatore è un visitatore/esploratore che attraversa i "piani" del MUSE risolvendo mini-sfide a tema (ghiaccio → evoluzione → serra). Loop corto, mobile-friendly.
- **Tap dell'evoluzione**: idle/tap game dove fai evolvere la vita dalla molecola al dinosauro al mammifero, sbloccando exhibit reali del museo.
- **Survival nella serra tropicale**: gestisci un piccolo ecosistema (acqua, luce, specie) — touch, sessioni brevi.
- **Catena della biodiversità**: puzzle/match dove ricomponi reti alimentari; messaggio sostenibilità.
- **Ghiacciaio che si scioglie**: gioco a tempo sul cambiamento climatico, con scelte che lo accelerano/rallentano.

> Criterio di scelta: **loop semplice, sessione breve, input touch, leggibile in verticale**, qualcosa di *fattibile in 24h* e *riconoscibilmente legato al MUSE*.

## 🛠️ Setup tecnico

### Requisiti
- **Unity 6.3 LTS** — versione esatta `6000.3.6f1` (via Unity Hub).
- **Modulo Android Build Support** installato nell'editor (SDK/NDK/OpenJDK: spuntali in Hub → Installs → Add modules). *Necessario per buildare l'APK.*
- **Git LFS** — asset binari (immagini, audio, modelli, font) tracciati via `.gitattributes`. Dopo il clone:
  ```bash
  git lfs install
  git lfs pull
  ```

### Aprire il progetto
1. Unity Hub → **Add** → seleziona la cartella di questa repo (`muse-game-jam-2026`).
2. Apri con la versione `6000.3.6f1` (evita upgrade involontari).

Il gioco va buildato per **entrambi** i target. Tienili in mente da subito: input **touch + mouse/tastiera**, UI leggibile sia su telefono (verticale) sia su monitor.

**Android (consegna jam)**
1. `File → Build Profiles` → **Android** → **Switch Platform**.
2. `Project Settings → Player`:
   - **Package name** univoco (es. `it.pignalabs.musejam`).
   - **Orientation** coerente col gioco (portrait per la maggior parte degli spunti mobile).
   - **Minimum API Level** ragionevole (Android 7.0+).
3. **Build** → APK in `Builds/Android/` (ignorata da git). Testa su un device reale prima del deadline.

**Windows (demo premiazione)**
1. `File → Build Profiles` → **Windows** → **Switch Platform** (oppure tieni due Build Profile salvati e cambia al volo).
2. **Build** → `.exe` in `Builds/Windows/` (ignorata da git).

> 💡 Lavora di solito su un target e fai lo **switch platform** solo per buildare l'altro (la reimportazione asset può richiedere qualche minuto). Per non perdere tempo a fine jam, fai un build di prova su **entrambi** già nelle prime ore.

> ⚠️ **Nota URP su mobile**: il progetto è nato con template **Universal 3D (URP)**. Su Android va benissimo ma tieni d'occhio **performance e peso**: usa il render pipeline asset *Mobile* (già in `Assets/Settings/`), evita post-processing pesante, batcha e usa texture compresse. Su Windows non hai questi vincoli, quindi progetta per il device più debole (il telefono).

## 📐 Convenzioni

- **Lavoro in parallelo**: 1 sistema = 1 cartella sotto `Assets/`, owner unico, per ridurre i conflitti di merge in 24h.
- Serializzazione asset: **Force Text** → diff/merge leggibili (impostato).
- **Meta files visibili e versionati**: NON cancellarli mai a mano (li gestisce Unity).
- Non committare `Library/`, `Temp/`, `Builds/`, `UserSettings/` (vedi `.gitignore`).
- Commit piccoli e frequenti: in 24h un repo pulito vale oro.

## 🔗 Link utili

- Repo: <https://github.com/Pigna-Labs/muse-game-jam-2026>
- Evento (Eventbrite, IGDA Italy): *Tech Tales — Museum Game Jam*
- MUSE: <https://www.muse.it>

---

## ⏱️ Checklist

**Allo start (ven 5, 15:00)**
- [ ] Tutti hanno clonato + `git lfs install` + aperto il progetto in `6000.3.6f1`
- [ ] Modulo Android installato; build di prova **APK** funzionante (è la consegna!) + `.exe` di prova
- [ ] Account **itch.io** pronto per l'upload (verifica chi carica)
- [ ] Tema/angolo museale scelto e scritto in fondo a questo README
- [ ] Ruoli assegnati (code / design / art / audio) e cartelle `Assets/` divise per owner
- [ ] Primo commit del "vertical slice" entro sera

**🚨 Sabato mattina (margine sul 14:30)**
- [ ] APK finale buildato e **testato su device reale**
- [ ] APK caricato su **itch.io** (entro 14:30 — non aspettare l'ultimo minuto)
- [ ] Pitch pronto (5 min) + risposte alle domande prevedibili (5 min Q/A)
- [ ] Build pronta per le postazioni di **playtest** pubblico (16:30)

## 🎯 TEMA / IDEA SCELTA *(da compilare in jam)*

> _Scrivi qui l'angolo museale e il concept del gioco una volta deciso sul posto._
