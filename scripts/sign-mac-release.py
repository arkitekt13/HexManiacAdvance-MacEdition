#!/usr/bin/env python3
"""Sign a new release copy, inside out; never rebuild or alter the development app.

Requires a Developer ID Application identity with its private key in Keychain.
This creates a signed staging app and evidence, not a notarized/public release.
"""
import argparse
import hashlib
import json
import plistlib
import shutil
import subprocess
from pathlib import Path

MAGIC = {bytes.fromhex(s) for s in (
    'feedface', 'cefaedfe', 'feedfacf', 'cffaedfe',
    'cafebabe', 'bebafeca', 'cafebabf', 'bfbafeca')}


def run(*args):
    result = subprocess.run(args, text=True, capture_output=True)
    if result.returncode:
        raise RuntimeError(f'{args[0]} failed: {result.stdout}{result.stderr}')
    return result.stdout + result.stderr


def inventory(root):
    return {str(p.relative_to(root)): hashlib.sha256(p.read_bytes()).hexdigest()
            for p in root.rglob('*') if p.is_file() and not p.is_symlink()}


def sign(source, destination, identity):
    source, destination = source.resolve(), destination.absolute()
    if source.suffix != '.app' or not source.is_dir():
        raise ValueError('Source must be an existing .app bundle.')
    if destination.suffix != '.app' or destination.exists() or destination.is_symlink():
        raise ValueError('Choose a new, unused .app destination.')
    if source == destination or source in destination.resolve().parents:
        raise ValueError('Release copy must be outside the source app.')
    record_path = destination.with_suffix('.signing.json')
    if record_path.exists():
        raise ValueError('Signing record already exists; choose a new destination.')
    run('codesign', '--verify', '--deep', '--strict', str(source))
    initial = inventory(source)
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copytree(source, destination, symlinks=True)
    info = plistlib.loads((destination / 'Contents/Info.plist').read_bytes())
    main = destination / 'Contents/MacOS' / info['CFBundleExecutable']
    # Managed PE assemblies/data belong in Resources, outside native-code locations.
    # Keep the self-contained payload together; regenerate only the native apphost
    # using the SDK HostWriter API and the matching installed runtime host pack.
    resources = destination / 'Contents/Resources'
    resources.mkdir(exist_ok=True)
    mapping = {}
    for path in (destination / 'Contents/MacOS').iterdir():
        if path == main:
            continue
        target = resources / path.name
        if target.exists():
            raise ValueError('Resource destination collision: ' + path.name)
        shutil.move(str(path), str(target))
    for name in initial:
        mapping[name] = name.replace('Contents/MacOS/', 'Contents/Resources/', 1) if name.startswith('Contents/MacOS/') and name != str(main.relative_to(destination)) else name
    managed_name = info['CFBundleExecutable'] + '.dll'
    runtime = json.loads((resources / (info['CFBundleExecutable'] + '.runtimeconfig.json')).read_text())
    version = next(f['version'] for f in runtime['runtimeOptions']['includedFrameworks'] if f['name'] == 'Microsoft.NETCore.App')
    deps = json.loads((resources / (info['CFBundleExecutable'] + '.deps.json')).read_text())
    rid = deps['runtimeTarget']['name'].rsplit('/', 1)[1]
    dotnet_root = Path(shutil.which('dotnet')).resolve().parent
    template = dotnet_root / f'packs/Microsoft.NETCore.App.Host.{rid}/{version}/runtimes/{rid}/native/apphost'
    tool = Path(__file__).resolve().parents[1] / 'tools/MacReleaseHost'
    run('dotnet', 'build', str(tool), '--disable-build-servers', '-c', 'Release',
        '-p:UseSharedCompilation=false', '-p:NuGetAudit=false', '-m:1', '-nr:false', '-v', 'quiet')
    main.unlink()
    binding = '../Resources/' + managed_name
    print('Creating release apphost for', binding, flush=True)
    run('dotnet', str(tool / 'bin/Release/net8.0/MacReleaseHost.dll'), str(template), str(main), binding)
    main.chmod(0o755)
    native = []
    for path in destination.rglob('*'):
        if not path.is_file() or path.is_symlink():
            continue
        with path.open('rb') as f:
            if f.read(4) in MAGIC:
                native.append(path)
    if main not in native:
        raise ValueError('Bundle executable is not a recognized Mach-O file.')
    # This app has loose native libraries and one helper, not nested framework/app bundles.
    if any(p.is_dir() and p.suffix in ('.app', '.framework', '.xpc') for p in destination.rglob('*')):
        raise ValueError('Nested bundles require an explicit signing plan.')
    common = ('codesign', '--force', '--sign', identity, '--timestamp', '--options', 'runtime')
    for path in sorted((p for p in native if p != main), key=lambda p: (-len(p.parts), str(p))):
        print('Signing', path.relative_to(destination), flush=True)
        run(*common, str(path))
    entitlements = Path(__file__).with_name('macos-release-entitlements.plist')
    print('Signing application host and bundle', flush=True)
    run(*common, '--entitlements', str(entitlements), str(destination))
    run('codesign', '--verify', '--deep', '--strict', str(destination))
    signatures = {}
    teams = set()
    for path in native:
        run('codesign', '--verify', '--strict', str(path))
        details = run('codesign', '--display', '--verbose=4', str(path))
        if 'Authority=Developer ID Application:' not in details or 'Timestamp=' not in details or '(runtime)' not in details:
            raise ValueError('Missing Developer ID, timestamp or Hardened Runtime: ' + str(path))
        team = next(line.split('=', 1)[1] for line in details.splitlines() if line.startswith('TeamIdentifier='))
        teams.add(team)
        signatures[str(path.relative_to(destination))] = details.splitlines()
    if len(teams) != 1:
        raise ValueError('All native code must use the same Developer ID team.')
    effective = plistlib.loads(subprocess.check_output(['codesign', '--display', '--entitlements', ':-', str(destination)], stderr=subprocess.DEVNULL))
    if effective != {'com.apple.security.cs.allow-jit': True}:
        raise ValueError('Unexpected release entitlements.')
    final = inventory(destination)
    native_names = {str(p.relative_to(destination)) for p in native}
    for name, digest in initial.items():
        target = mapping[name]
        if name == 'Contents/_CodeSignature/CodeResources' or target in native_names:
            continue
        if final.get(target) != digest:
            raise ValueError('Non-native application data changed during signing: ' + name)
    if inventory(source) != initial:
        raise ValueError('Development app changed during signing.')
    record = {
        'version': info['CFBundleShortVersionString'], 'bundle_version': info['CFBundleVersion'],
        'bundle_identifier': info['CFBundleIdentifier'], 'team': next(iter(teams)),
        'native_files': len(native), 'native_signatures': signatures, 'entitlements': effective,
        'deep_strict_verification': True, 'source_app_unchanged': True,
        'managed_code_and_resources_unchanged': True, 'notarized': False,
        'release_layout': 'MacOS apphost; self-contained payload in Resources',
        'managed_binding': binding, 'host_template_sha256': hashlib.sha256(template.read_bytes()).hexdigest(),
        'source_to_release_paths': mapping,
        'source_file_hashes': initial, 'signed_file_hashes': final,
    }
    with record_path.open('x') as stream:
        json.dump(record, stream, indent=2)
        stream.write('\n')
    print('Signed release copy:', destination, flush=True)
    print('Verification:', record_path, flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--destination', type=Path, required=True)
    parser.add_argument('--identity', required=True)
    args = parser.parse_args()
    sign(args.source, args.destination, args.identity)
