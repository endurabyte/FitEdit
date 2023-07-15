#!/bin/bash
#
# https://stackoverflow.com/a/70599991
extension_dir=~/.vscode/extensions/
latest_csharp_dir=$(ls -td -- "$extension_dir"/ms-dotnettools.csharp-* | head -n 1)
vsdbg_dir="$latest_csharp_dir/.debugger"

pushd $vsdbg_dir

if [ ! -e "./vsdbg-ui2" ]
then
  mv ./vsdbg-ui ./vsdbg-ui2
  if [ $? -eq 0 ]
  then
    absolute_path=$(realpath ./vsdbg-ui2) # store the absolute path of vsdbg-ui2
    echo "pkexec $absolute_path" > vsdbg-ui
    if [ $? -eq 0 ]
    then
      chmod +x vsdbg-ui
    else
      echo "Failed to write to vsdbg-ui"
    fi
  else
    echo "Failed to move vsdbg-ui to vsdbg-ui2"
  fi
fi

popd
