#!/bin/bash
DEST="/Volumes/SyNas_4TB/schedule_1_modding"

if [ ! -d "$DEST" ]; then
    echo "Error: $DEST not mounted"
    exit 1
fi

for mod in M1911MagMod PackRatFPSFix; do
    dll="$mod/bin/Release/net6.0/$mod.dll"
    if [ -f "$dll" ]; then
        cp "$dll" "$DEST/"
        echo "Copied $mod.dll to $DEST"
    fi
done
