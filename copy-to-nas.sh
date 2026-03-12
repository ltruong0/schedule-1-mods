#!/bin/bash
DEST="/Volumes/SyNas_4TB/schedule_1_modding"

if [ ! -d "$DEST" ]; then
    echo "Error: $DEST not mounted"
    exit 1
fi

cp M1911MagMod/bin/Release/net6.0/M1911MagMod.dll "$DEST/"
echo "Deployed M1911MagMod.dll to $DEST"
