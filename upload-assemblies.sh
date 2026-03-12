#!/bin/bash
# Upload reference assemblies to a GitHub release so CI can use them.
# Run this once (and again if assemblies change, e.g. game update).

set -e

echo "Packaging reference assemblies..."
tar czf /tmp/assemblies.tar.gz Il2CppAssemblies/ net6/ references/

echo "Creating/updating 'assemblies' release..."
if gh release view assemblies &>/dev/null; then
    gh release delete assemblies --yes
fi

gh release create assemblies /tmp/assemblies.tar.gz \
    --title "Reference Assemblies" \
    --notes "MelonLoader and game assemblies for CI builds. Not for end users." \
    --prerelease

rm /tmp/assemblies.tar.gz
echo "Done! CI can now download assemblies."
