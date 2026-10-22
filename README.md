# Ancient & Medieval Japan - Rice Cultivation（中世日本 - 水田稲作）

**開発版 0.1.0-dev / Development build — 未公開（not publicly released）**

RimWorld 1.6向けの水田造成Modです。プレイヤーが矩形を指定すると、ポーンが建築作業で造成します。完成した区画は元の土壌・肥沃度を保持し、外周の畦と湛水状態を描画します。通常のVanilla栽培Zoneと共通の`Plant_Rice`を使用します。

- **必須 / Required:** Ancient & Medieval Japan - Waterworks。露出した通水中の水路または適格な天然水源への隣接で、自動給水を判定します。
- **任意 / Optional:** Ancient & Medieval Japan - Grains。稲を別品種にせず、Grainsが導入済みならその収穫後加工系を使用します。
- **対応 / Supported:** RimWorld 1.6。1.7は未検証です。
- **確認済み / Tested:** Vanilla構成とGrains併用構成で、実ポーン造成、中断・再開、取水状態変化、既存Zone非破壊、実ディスク保存・再読込が各Pickle 1/1 PASS（Unity ERROR0）。[検証証拠](Docs/Validation/RiceNative16-20261022.md)。
- **未実装・未承認 / Pending:** 稲の水田成長ボーナス、造成の正式工数・資材バランス、最終的な畦・水面の見た目、一般セーブへの後付け可否、1.7、公開判定。土壌・通常の播種・収穫処理は変更しません。

**用途:** 現時点では開発・検証向けです。Workshopでの一般公開版とは扱わないでください。

**English:** Development-only 1.6 paddy plots built by pawn Construction jobs, using the original soil, Vanilla Growing Zones and shared rice plant. Waterworks is required and Grains optional. Two bounded original-game 1.6 test profiles passed, but rice growth bonus, construction balance, rendered visual acceptance and public-release readiness are not complete.
