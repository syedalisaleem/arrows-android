#!/bin/bash
# Diagnostic: can this container obtain a Unity licence now that the account
# actually holds a Unity Personal entitlement?
#
# Runs INSIDE unityci/editor. Output is written to /out so it survives the
# container and can be uploaded as a CI artifact.

OUT=/out
mkdir -p "$OUT"

echo "############################################################"
echo "# environment"
echo "############################################################"
echo "machine-id : $(cat /etc/machine-id 2>/dev/null)"
echo "uname      : $(uname -a)"
echo "unity ver  : $(cat /opt/unity/version 2>/dev/null)"
echo "email set  : $([ -n "$UNITY_EMAIL" ] && echo yes || echo NO)"
echo "pass set   : $([ -n "$UNITY_PASSWORD" ] && echo yes || echo NO)"
echo

echo "############################################################"
echo "# pre-existing licence state"
echo "############################################################"
echo "--- \$HOME=$HOME"
ls -la "$HOME/.local/share/unity3d/Unity/" 2>&1 | head -20
echo "--- any ulf / entitlement already present"
find / -xdev \( -iname '*.ulf' -o -iname '*Entitlement*' \) 2>/dev/null | head -20
echo

echo "############################################################"
echo "# ATTEMPT 1 - activate using account credentials"
echo "############################################################"
unity-editor \
  -username "$UNITY_EMAIL" \
  -password "$UNITY_PASSWORD" \
  -quit -nographics \
  -logFile /dev/stdout 2>&1 | tail -120
echo "unity-editor exit: $?"
echo

echo "############################################################"
echo "# licence state AFTER credential activation"
echo "############################################################"
find / -xdev \( -iname '*.ulf' -o -iname '*Entitlement*' \) 2>/dev/null | tee "$OUT/found-after-cred.txt"
echo "--- $HOME/.local/share/unity3d/Unity/"
ls -la "$HOME/.local/share/unity3d/Unity/" 2>&1 | head -30
echo "--- ~/.config/unity3d"
ls -la "$HOME/.config/unity3d/" 2>&1 | head -20
echo "--- copy anything we found into /out"
i=0
while IFS= read -r f; do
  [ -f "$f" ] || continue
  i=$((i+1))
  cp "$f" "$OUT/licence-$i-$(basename "$f")" 2>/dev/null && echo "copied: $f"
done < "$OUT/found-after-cred.txt"
echo

echo "############################################################"
echo "# ATTEMPT 2 - run WITHOUT credentials: is it licensed now?"
echo "############################################################"
unity-editor -quit -nographics -logFile /dev/stdout 2>&1 \
  | grep -iE 'licen[cs]e|entitlement|serial|activation|access token' | head -60
echo

echo "############################################################"
echo "# done - listing /out"
echo "############################################################"
ls -la "$OUT"
