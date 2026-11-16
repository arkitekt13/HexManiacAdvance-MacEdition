# Tutorial Village distribution

This package contains the finished Tutorial Village IPS and the original matching HMA TOML.
Utilities → Create Tutorial ROM… is available when the exact unmodified English FireRed 1.1
base is open. It creates a separate project; the open base remains unchanged.

- **tutorial.ips** — supplied Tutorial Village patch, preserved byte for byte.
- **tutorial.toml** — supplied Tutorial Village (2).toml, preserved byte for byte.
- **tutorial.json** — verified base, patch, output and metadata identities; explicitly selects tutorial.ips.

The application bundles these files under resources/Optional/Tutorial/. No base or patched
ROM is included in the repository or release. Users supply their own matching base ROM.

The required base is a 16 MiB English FireRed 1.1 ROM (BPRE1), CRC32 84EE4776, SHA-256
729041b940afe031302d630fdbe57c0c145f3f7b6d9b8eca5e98678d0ca4d059.
Wrong game/revision, edited bytes or altered length are rejected. Header identity alone
is insufficient. IPS has no built-in source checksum, so the loader enforces this pinned
base hash and verifies the full patched output hash before any files are published.

The supplied output is 16 MiB, CRC32 5EB1EF21, SHA-256
8e611cc0ebe03aca59418c2b55d728776327582975ed9fa96fdbb1b88f3b7c69.
An independent IPS decoder reproduces the user's finished working tutorial byte for byte.
The original supplied metadata has 547 named definitions; all addresses and formats must
survive HMA loading. The original TOML is used directly, without regeneration from the base.

The utility asks for a folder and a new ROM name. It verifies the assets, applies the IPS
to detached bytes, verifies the output and checks supplied metadata. It publishes
Chosen Name.toml before Chosen Name.gba, then opens the new pair. Keep those files together.
Neither an existing ROM nor an existing TOML or directory is overwritten. Cancellation
creates no project. Publication failures clean up owned partial output and staging files;
power loss between the two renames can leave an orphan TOML. A later opening failure keeps
the verified pair for retry. Normal subsequent Save serializes metadata as usual.

The loader also retains legacy tutorial.ups support for existing packages. The manifest
may select only tutorial.ips or tutorial.ups; omitting patchFile selects tutorial.ups for
backward compatibility. Missing/mismatched assets, unsupported filenames, malformed patches,
outdated/empty metadata and incorrect output identities prevent successful creation.
Assets load at startup; rebuild and restart after updates. Preserve future supplied TOML
bytes and recompute the manifest from independently verified output when replacing assets.

Automated package checks cover exact output, all supplied definitions, map rendering and
normal Save/reopen in disposable copies. They do not certify every tutorial event or gameplay path.
