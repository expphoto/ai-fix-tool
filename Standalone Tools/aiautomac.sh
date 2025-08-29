cat <<'EOF' > /tmp/ai-auto.sh
#!/bin/bash
# ==== ai-auto.sh ====

#!/bin/bash
# ==================================================
# ai-auto.sh — Autonomous GPT-5 Nano Troubleshooter
# Native Bash version for macOS/Linux
# Requires: curl, jq, OPENAI_API_KEY in env
# ==================================================

# Parse flags
ALLOW_DANGEROUS=0
for arg in "$@"; do
  if [[ "$arg" == "--danger" ]]; then
    ALLOW_DANGEROUS=1
    # remove it from the goal text
    set -- "${@/--danger/}"
  fi
done
GOAL="$*"
[ -z "$GOAL" ] && GOAL="Investigate system slowness"

MAX_STEPS=5
API_KEY="${OPENAI_API_KEY}"
MODEL="gpt-5-nano"
URL="https://api.openai.com/v1/chat/completions"

if [ -z "$API_KEY" ]; then
  echo "[!] OPENAI_API_KEY not set"; exit 1
fi

# -------- Guardrails --------
# Always blocked (even with --danger)
HARD_DENY='(mkfs|dd|shutdown|halt|reboot)'

# Safe diagnostics allowed by default
ALLOWLIST='^(ps|top|uptime|vm_stat|iostat|netstat|ifconfig|ip|df|du|sysctl|sw_vers|uname|whoami|id|last|dmesg|log show|tail -n 100 /var/log|lsblk|free)$'

is_allowed() {
  local cmd="$1"
  # hard block
  if [[ "$cmd" =~ $HARD_DENY ]]; then return 1; fi

  # dangerous mode overrides
  if [[ $ALLOW_DANGEROUS -eq 1 ]]; then return 0; fi

  # safe mode only
  if [[ "$cmd" =~ $ALLOWLIST ]]; then return 0; fi
  return 1
}

# -------- OpenAI Chat Helper --------
chat_request() {
  local msgs="$1"
  curl -sS -X POST "$URL" \
    -H "Authorization: Bearer $API_KEY" \
    -H "Content-Type: application/json" \
    -d "$msgs"
}

# -------- Start Conversation --------
messages=$(jq -n \
  --arg g "$GOAL" \
  '[{"role":"system","content":"You are a cautious macOS/Linux troubleshooting agent. Goal: \($g). Rules: JSON only {\"commands\":[{\"cmd\":\"<one-liner>\",\"why\":\"<short>\"}]}."},
    {"role":"user","content":"Begin."}]')

transcript="[]"

for ((i=1;i<=MAX_STEPS;i++)); do
  echo "[AI] Planning step $i..."
  resp=$(chat_request "{\"model\":\"$MODEL\",\"messages\":$messages}")
  content=$(echo "$resp" | jq -r '.choices[0].message.content')
  echo "[RAW] $content" >&2

  cmds=$(echo "$content" | jq -r '.commands[]?.cmd')
  [ -z "$cmds" ] && break

  results="[]"
  while IFS= read -r cmd; do
    echo "[AI] Running: $cmd"
    if is_allowed "$cmd"; then
      out=$(bash -c "$cmd" 2>&1 | head -c 4000)
    else
      out="[blocked by policy]"
    fi
    results=$(echo "$results" | jq --arg c "$cmd" --arg o "$out" '.+=[{"cmd":$c,"output":$o}]')
  done <<< "$cmds"

  transcript=$(echo "$transcript" | jq --argjson r "$results" --arg c "$content" '.+=[{"plan":$c,"results":$r}]')

  messages=$(echo "$messages" | jq --arg c "$content" --argjson r "$results" '.+=[{"role":"assistant","content":$c},{"role":"user","content":"Results: \($r)"}]')
done

# -------- Final Report --------
report_prompt="Summarize findings for sysadmins:
1) Executive Summary
2) Key Findings
3) Likely Root Causes
4) Recommended Next Steps
5) Optional Maintenance"

final=$(chat_request "{\"model\":\"$MODEL\",\"messages\":[{\"role\":\"system\",\"content\":\"$report_prompt\"},{\"role\":\"user\",\"content\":\"$transcript\"}]}")

echo
echo "===== Investigation Report ====="
echo "$final" | jq -r '.choices[0].message.content'

EOF

chmod +x /tmp/ai-auto.sh
/tmp/ai-auto.sh "Investigate slowness”