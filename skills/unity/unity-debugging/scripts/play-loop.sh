#!/usr/bin/env bash
# Play loop: enter Play, wait for the scenario, read the console since a cursor, stop.
# Copy to the scratch dir and set the variables below.
# Exit 0 green, 1 red, 2 no verdict, 3 the two runs disagree (a second-Play bug).
set -u
PROJECT="${PROJECT:-.}"                        # the Unity project path
DONE='ReproProbe.Done'                         # a static C# bool that turns true once the scenario has run
SYMPTOM='\[DEBUG-a4f2\] health went negative'  # grep -E pattern for the symptom in the console
TIMEOUT_S=30                                   # wait budget when the editor has focus
MAX_FRAMES=600                                 # step budget when it does not

u() { unity command --project-path "$PROJECT" "$@" --result-only; }
num() { grep -o "\"$1\": *[0-9]*" | head -1 | grep -o '[0-9]*$'; }
is_true() { grep -q '"result": *true'; }

run_once() {
  local cursor wait console timeout="$TIMEOUT_S" n=0
  cursor=$(u console_status | num cursor)
  [ -n "$cursor" ] || { echo "no verdict: console_status gave no cursor" >&2; return 2; }
  u editor_play >/dev/null || return 2
  # An unfocused GUI editor does not advance Play frames: pause and step them instead.
  if ! u eval --code 'return UnityEditorInternal.InternalEditorUtility.isApplicationActive;' | is_true; then
    u editor_pause >/dev/null
    while [ "$n" -lt "$MAX_FRAMES" ] && ! u eval --code "for (int i = 0; i < 30; i++) { if ($DONE) break; UnityEditor.EditorApplication.Step(); } return $DONE;" | is_true; do
      n=$((n + 30))
    done
    timeout=1
  fi
  wait=$(u wait_for --condition "{\"member\":\"$DONE\",\"op\":\"equals\",\"value\":true}" --timeout_s "$timeout")
  console=$(u console --since "$cursor" --tail 1000)
  u editor_stop >/dev/null
  echo "$wait" | grep -q '"met": *true' || { echo "no verdict: $DONE never held" >&2; return 2; }
  echo "$console" | grep -q '"entries"' || { echo "no verdict: console could not be read" >&2; return 2; }
  # Headless editors log graphics-device errors on entering Play: noise, never the symptom.
  if echo "$console" | grep '"message"' | grep -v 'No graphic device is available' | grep -Eq "$SYMPTOM"; then echo red; return 1; fi
  echo green; return 0
}

reload_off=false
u eval --code 'return UnityEditor.EditorSettings.enterPlayModeOptionsEnabled && UnityEditor.EditorSettings.enterPlayModeOptions.HasFlag(UnityEditor.EnterPlayModeOptions.DisableDomainReload);' | is_true && reload_off=true
if $reload_off; then
  # Statics survive between Play sessions: reload the domain so the first run is a true first Play.
  # The reload is asynchronous: mark the current domain, then wait until a fresh one has no mark.
  u eval --code 'System.AppDomain.CurrentDomain.SetData("play-loop", true); UnityEditor.EditorUtility.RequestScriptReload(); return 0;' >/dev/null
  settled=false
  for _ in $(seq 1 60); do
    sleep 1
    u editor_status 2>/dev/null | grep -q '"domainReloadInProgress": *false' || continue
    u eval --code 'return System.AppDomain.CurrentDomain.GetData("play-loop") == null;' 2>/dev/null | is_true && { settled=true; break; }
  done
  $settled || { echo "no verdict: the domain reload did not finish" >&2; exit 2; }
fi
run_once; first=$?
[ "$first" = 2 ] && exit 2
if $reload_off; then
  run_once; second=$?
  [ "$second" = 2 ] && exit 2
  if [ "$first" != "$second" ]; then echo "runs disagree: a second-Play bug" >&2; exit 3; fi
fi
exit "$first"
