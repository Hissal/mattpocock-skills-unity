#!/usr/bin/env bash
# Play loop: enter Play, wait for the scenario, read the console since a cursor, stop.
# Copy to the scratch dir and set the four variables below. Run from anywhere.
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
  local cursor wait log timeout="$TIMEOUT_S" n=0
  cursor=$(u console_status | num cursor)
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
  log=$(u console --since "$cursor" --tail 1000 | grep '"message"' | grep -v 'No graphic device is available')
  u editor_stop >/dev/null
  echo "$wait" | grep -q '"met": *true' || { echo "no verdict: $DONE never held" >&2; return 2; }
  if echo "$log" | grep -Eq "$SYMPTOM"; then echo red; return 1; fi
  echo green; return 0
}

reload_off=false
u eval --code 'return UnityEditor.EditorSettings.enterPlayModeOptionsEnabled && UnityEditor.EditorSettings.enterPlayModeOptions.HasFlag(UnityEditor.EnterPlayModeOptions.DisableDomainReload);' | is_true && reload_off=true
if $reload_off; then
  # Statics survive between Play sessions: reload the domain so the first run is a true first Play.
  u eval --code 'UnityEditor.EditorUtility.RequestScriptReload(); return 0;' >/dev/null
  for _ in $(seq 1 60); do
    u editor_status 2>/dev/null | grep -q '"domainReloadInProgress": *false' && break
    sleep 1
  done
fi
run_once; first=$?
[ "$first" = 2 ] && exit 2
if $reload_off; then
  run_once; second=$?
  [ "$second" = 2 ] && exit 2
  if [ "$first" != "$second" ]; then echo "runs disagree: a second-Play bug" >&2; exit 3; fi
fi
exit "$first"
