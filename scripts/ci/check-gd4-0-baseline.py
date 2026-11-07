#!/usr/bin/env python3
"""GD4.0 static, read-only UI/UX baseline audit. No database or network."""
from __future__ import annotations

import csv
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
DOCUMENTS = ROOT / 'docs'
MANIFEST = ROOT / 'audit' / 'gd4_0_baseline.json'


def require(cond: bool, message: str) -> None:
    if not cond:
        raise AssertionError(message)


def main() -> None:
    data = json.loads(MANIFEST.read_text(encoding='utf-8'))
    require(data['stage'] == 'GD4.0', 'Unexpected stage')
    require(re.fullmatch(r'[a-f0-9]{40}', data['base_commit']) is not None, 'Bad baseline SHA')
    require(len(data['controllers']) == 16, 'Controller inventory drift')
    require(len(data['views']) == 38, 'View inventory drift; update GD4 audit if intentional')
    require(len(data['guarded_actions']) == 21, 'Action inventory drift')

    for controller in data['controllers']:
        path = ROOT / 'RentalPlatform' / 'Controllers' / (controller + 'Controller.cs')
        require(path.is_file(), f'Missing controller: {path.relative_to(ROOT)}')

    for row in data['views']:
        view = ROOT / 'RentalPlatform' / 'Views' / (row['view'] + '.cshtml')
        require(view.is_file(), f'Missing Razor View: {view.relative_to(ROOT)}')
        require(row['method'] == 'GET', f'Expected GET: {row}')
        require(row['access'] and row['user_intent'] and row['phase'], f'Incomplete inventory: {row}')

    for row in data['guarded_actions']:
        controller, action = row['source'].split('.')
        code_path = ROOT / 'RentalPlatform' / 'Controllers' / (controller + '.cs')
        require(code_path.is_file(), f'Missing guarded controller: {code_path.relative_to(ROOT)}')
        source = code_path.read_text(encoding='utf-8-sig')
        require(re.search(r'\b' + re.escape(action) + r'\s*\(', source),
                f'Missing guarded action: {row["source"]}')

    for path in data['known_layouts']:
        require((ROOT / path).is_file(), f'Missing existing layout: {path}')

    with (DOCUMENTS / 'GD4_0_PAGE_INVENTORY.csv').open(encoding='utf-8-sig', newline='') as f:
        pages = list(csv.DictReader(f))
    with (DOCUMENTS / 'GD4_0_ACTION_INVENTORY.csv').open(encoding='utf-8-sig', newline='') as f:
        actions = list(csv.DictReader(f))
    with (DOCUMENTS / 'GD4_0_ACCEPTANCE_MATRIX.csv').open(encoding='utf-8-sig', newline='') as f:
        tests = list(csv.DictReader(f))
    require(len(pages) == len(data['views']), 'CSV page count mismatch')
    require(len(actions) == len(data['guarded_actions']), 'CSV action count mismatch')
    require(len(tests) == 26, 'Acceptance matrix must contain 26 baseline scenarios')
    require(len({r['RoutePattern'] for r in pages}) == len(pages), 'Duplicate route in page inventory')
    require(len({r['ID'] for r in tests}) == len(tests), 'Duplicate acceptance ID')
    require(all(r['Priority'] in ('P0', 'P1') for r in tests), 'Unknown scenario priority')

    for doc in ('GD4_0_UI_UX_BASELINE.md', 'GD4_0_USER_JOURNEYS.md',
                'GD4_0_COMPONENT_CONTRACTS.md'):
        require((DOCUMENTS / doc).is_file(), f'Missing audit document: {doc}')

    # The script checks source-to-inventory correspondence, not user-experience outcomes.
    print(f'GD4.0 STATIC AUDIT PASS: {len(data["controllers"])} controllers, '
          f'{len(pages)} GET pages, {len(actions)} guarded actions, '
          f'{len(tests)} acceptance scenarios.')
    print('No database, migrations, runtime files, or user data were modified.')


if __name__ == '__main__':
    try:
        main()
    except (AssertionError, OSError, ValueError, KeyError) as exc:
        print(f'GD4.0 STATIC AUDIT FAIL: {exc}', file=sys.stderr)
        sys.exit(1)
