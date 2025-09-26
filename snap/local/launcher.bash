#!/usr/bin/env bash

if ! snapctl is-connected dotnet-runtime-80; then
  >&2 echo "Plug 'dotnet-runtime-80' isn't connected, please run: \
  snap connect ${SNAP_NAME}:dotnet-runtime-80 dotnet-runtime-80:dotnet-runtime"
  exit 1
fi

"$SNAP"/flamenco "$@"
