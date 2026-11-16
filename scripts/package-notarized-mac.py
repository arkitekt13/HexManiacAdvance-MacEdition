#!/usr/bin/env python3
"""Archive an already-notarized payload without rebuilding or re-signing it."""
import argparse
import hashlib
import json
import os
import subprocess
import tempfile
from pathlib import Path


def run(*args):
    result = subprocess.run(args, text=True, capture_output=True)
    if result.returncode:
        raise RuntimeError(result.stdout + result.stderr)
    return result.stdout + result.stderr


def validate(app):
    signature = run('codesign', '--verify', '--deep', '--strict', '--verbose=2', str(app))
    ticket = run('xcrun', 'stapler', 'validate', str(app))
    gatekeeper = run('spctl', '--assess', '--type', 'execute', '--verbose=4', str(app))
    if 'source=Notarized Developer ID' not in gatekeeper:
        raise ValueError('Gatekeeper must identify a notarized Developer ID app.')
    return {'signature': signature, 'ticket': ticket, 'gatekeeper': gatekeeper}


def package(payload, destination):
    payload, destination = payload.resolve(), destination.absolute()
    checksum = Path(str(destination) + '.sha256')
    record = Path(str(destination) + '.verification.json')
    if destination.suffix != '.zip' or any(p.exists() or p.is_symlink() for p in (destination, checksum, record)):
        raise ValueError('Choose a new ZIP/checksum/record destination.')
    for name in ('HexManiacAdvance.app', 'LICENSE', 'START-HERE.txt', 'BUILD-INFO.txt', 'DEPENDENCY-NOTICES'):
        if not (payload / name).exists():
            raise ValueError('Missing release payload entry: ' + name)
    initial = validate(payload / 'HexManiacAdvance.app')
    destination.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='.notarized-package-', dir=destination.parent) as temporary:
        temporary = Path(temporary)
        staged = temporary / destination.stem
        run('ditto', str(payload), str(staged))
        validate(staged / 'HexManiacAdvance.app')
        archive = temporary / destination.name
        run('ditto', '-c', '-k', '--sequesterRsrc', '--keepParent', str(staged), str(archive))
        run('unzip', '-tq', str(archive))
        extracted = temporary / 'extracted'
        run('ditto', '-x', '-k', str(archive), str(extracted))
        extracted_checks = validate(extracted / destination.stem / 'HexManiacAdvance.app')
        digest = hashlib.sha256(archive.read_bytes()).hexdigest()
        sums = temporary / checksum.name
        sums.write_text(digest + '  ' + destination.name + '\n')
        proof = temporary / record.name
        proof.write_text(json.dumps({'zip': destination.name, 'sha256': digest,
            'bytes': archive.stat().st_size, 'input_checks': initial,
            'extracted_checks': extracted_checks, 'zip_integrity': True,
            'app_not_rebuilt_or_resigned': True}, indent=2) + '\n')
        published = []
        try:
            for src, dst in ((archive, destination), (sums, checksum), (proof, record)):
                os.link(src, dst)
                published.append(dst)
        except Exception:
            for path in published:
                path.unlink()
            raise
    print('Prepared:', destination)
    print('SHA-256:', digest)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--payload', type=Path, required=True)
    parser.add_argument('--destination', type=Path, required=True)
    args = parser.parse_args()
    package(args.payload, args.destination)
