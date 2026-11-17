#!/usr/bin/env python3
"""Independent, read-only verification of both exact-base BPS stages."""
import hashlib
import json
import struct
import sys
import zlib
from pathlib import Path


def decode(source, patch):
    if len(patch) < 19 or patch[:4] != b'BPS1':
        raise ValueError('Invalid BPS header')
    end = len(patch) - 12
    if zlib.crc32(patch[:-4]) != struct.unpack_from('<I', patch, end + 8)[0]:
        raise ValueError('Patch CRC mismatch')
    position = 4

    def number():
        nonlocal position
        value, shift = 0, 1
        for _ in range(10):
            if position >= end:
                raise ValueError('Truncated command')
            byte = patch[position]
            position += 1
            value += (byte & 127) * shift
            if byte & 128:
                return value
            shift <<= 7
            value += shift
        raise ValueError('Integer overflow')

    source_length, target_length, metadata_length = number(), number(), number()
    if source_length != len(source) or target_length > 64 * 1024 * 1024:
        raise ValueError('Wrong base or excessive target length')
    if zlib.crc32(source) != struct.unpack_from('<I', patch, end)[0]:
        raise ValueError('Source CRC mismatch')
    position += metadata_length
    if position > end:
        raise ValueError('Truncated metadata')
    output = bytearray()
    source_relative = target_relative = 0
    commands = [0, 0, 0, 0]
    while position < end:
        action = number()
        length, mode = (action >> 2) + 1, action & 3
        if length > target_length - len(output):
            raise ValueError('Output overrun')
        commands[mode] += 1
        if mode == 0:
            if len(output) + length > len(source):
                raise ValueError('Source overrun')
            output += source[len(output):len(output) + length]
        elif mode == 1:
            if position + length > end:
                raise ValueError('Truncated literal')
            output += patch[position:position + length]
            position += length
        else:
            offset = number()
            signed = -(offset >> 1) if offset & 1 else offset >> 1
            if mode == 2:
                source_relative += signed
                if source_relative < 0 or source_relative + length > len(source):
                    raise ValueError('Source copy overrun')
                output += source[source_relative:source_relative + length]
                source_relative += length
            else:
                target_relative += signed
                if target_relative < 0 or target_relative >= len(output):
                    raise ValueError('Unwritten target reference')
                for _ in range(length):
                    output.append(output[target_relative])
                    target_relative += 1
    if position != end or len(output) != target_length:
        raise ValueError('Incomplete target')
    if zlib.crc32(output) != struct.unpack_from('<I', patch, end + 4)[0]:
        raise ValueError('Target CRC mismatch')
    return bytes(output), commands


def sha(data):
    return hashlib.sha256(data).hexdigest()


if __name__ == '__main__':
    if len(sys.argv) != 5:
        raise SystemExit('Pass repository root, clean FireRed 1.0, build folder, and a new JSON report path')
    repo, clean_path, build, report_path = map(Path, sys.argv[1:])
    clean = clean_path.read_bytes()
    scade, first_commands = decode(clean, (repo / 'OptionalAssets/SCADE/SCADE.bps').read_bytes())
    if sha(scade) != '67c78da450f8631b7c57d92fb9345acd822ac2b3a37efa585656a6f55b15f99c':
        raise ValueError('Unsupported SCADE build')
    patch = (build / 'files/Ikarus-SCADE-Compatibility.bps').read_bytes()
    target, commands = decode(scade, patch)
    if target != (build / 'candidate.gba').read_bytes():
        raise ValueError('Output differs from reviewed candidate')
    report = {
        'independentDecoder': 'Python implementation, separate from HMA',
        'bothBpsStagesVerified': True,
        'baseSha256': sha(scade), 'targetSha256': sha(target),
        'patchSha256': sha(patch), 'scadeCommandCounts': first_commands,
        'compatibilityCommandCounts': commands,
        'sourceCrc32': f'{zlib.crc32(scade):08X}',
        'targetCrc32': f'{zlib.crc32(target):08X}',
        'patchCrc32': f'{zlib.crc32(patch[:-4]):08X}',
        'originalCleanInputUnchanged': clean_path.read_bytes() == clean,
    }
    with report_path.open('x') as stream:
        json.dump(report, stream, indent=2)
        stream.write('\n')
    print('PASS: both BPS stages independently decode to the exact reviewed target')
