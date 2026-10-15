#!/usr/bin/env bash
# PostToolUse hook: greps an edited .cs file for never-break rules from AGENTS.md.
# Exit 2 = Claude sees the message and must fix it. Prose rules can be ignored, this can't.
# shortcut: plain grep (no C# parser), so a match inside a comment or string also counts; upgrade to Microsoft.Unity.Analyzers if false positives get annoying.

file=$(grep -o '"file_path"[[:space:]]*:[[:space:]]*"[^"]*"' | head -1 | sed 's/.*:[[:space:]]*"//; s/"$//; s/\\\\/\//g')
case "$file" in
  *.cs) ;;
  *) exit 0 ;;
esac
case "$file" in
  */Assets/Synty/*|*/Editor/*) exit 0 ;;
esac
[ -f "$file" ] || exit 0

problems=""
add() { problems="$problems"$'\n'"- $1"; }
hits() { grep -nE "$1" "$file" | head -3 | sed 's/^/    line /'; }

if grep -qE '(UnityEngine\.)?\bInput\.(Get|mouse|any|touch|inputString)' "$file"; then
  add "Rule 2: UnityEngine.Input throws in this project. Use GameInput."$'\n'"$(hits '\bInput\.(Get|mouse|any|touch|inputString)')"
fi
if grep -q 'using UnityEngine.InputSystem;' "$file" && [ "$(basename "$file")" != "GameInput.cs" ]; then
  add "Rule 2: only GameInput.cs may use UnityEngine.InputSystem."
fi
if grep -q 'using UnityEditor' "$file" && ! grep -q '#if UNITY_EDITOR' "$file"; then
  add "Rule 3: 'using UnityEditor' in a runtime script breaks the build. Move it to an Editor/ folder or wrap it in #if UNITY_EDITOR."
fi
if grep -qE 'FindObjects?OfType' "$file"; then
  add "Rule 5: FindObjectOfType is obsolete. Use a serialized ref, Instance, or FindAnyObjectByType."$'\n'"$(hits 'FindObjects?OfType')"
fi
# strip the allowed 'event?.Invoke(' first so it can't hide a bad '?.' on the same line
nullOps=$(grep -nE '\?\.|\?\?' "$file" | sed 's/?\.Invoke(//g' | grep -E '\?\.|\?\?')
if [ -n "$nullOps" ]; then
  add "Rule 4: '?.' / '??' skip Unity's destroyed-object check. Use '!= null' (only 'event?.Invoke' is allowed)."$'\n'"$(echo "$nullOps" | cut -d: -f1 | head -3 | while read -r n; do echo "    line $n: $(sed -n "${n}p" "$file")"; done)"
fi
if grep -q 'StandaloneInputModule' "$file"; then
  add "Rule 2: StandaloneInputModule throws every frame. Use InputSystemUIInputModule."
fi

[ -z "$problems" ] && exit 0
echo "AGENTS.md never-break rules violated in $file:$problems" >&2
exit 2
