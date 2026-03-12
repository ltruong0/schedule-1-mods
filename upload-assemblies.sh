#!/bin/bash
# Upload reference assemblies to a GitHub release so CI can use them.
# Run this once (and again if assemblies change, e.g. game update).
# Only includes reference DLLs that are actual build dependencies.

set -e

staging=$(mktemp -d)
trap 'rm -rf "$staging"' EXIT

echo "Packaging build dependencies..."

# Copy core assemblies
cp -r Il2CppAssemblies "$staging/"
cp -r net6 "$staging/"

# Copy only referenced DLLs from references/
mkdir -p "$staging/references"
for csproj in */*.csproj; do
    # Extract HintPaths, decode XML entities, normalize separators
    grep 'HintPath.*references' "$csproj" 2>/dev/null \
        | sed 's/.*<HintPath>//; s/<\/HintPath>.*//' \
        | sed 's/&amp;/\&/g' \
        | sed 's|\\|/|g; s|^\.\./||' \
        | while read -r ref; do
            if [ -f "$ref" ]; then
                cp "$ref" "$staging/references/"
                echo "  Including $ref"
            fi
        done
done

tar czf /tmp/assemblies.tar.gz -C "$staging" .

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
