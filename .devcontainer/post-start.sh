#!/usr/bin/env bash
set -euo pipefail

sudo /usr/local/bin/init-firewall.sh

mkdir -p "$HOME/.local/share/Uno Platform"
if [ -d /tmp/uno-platform-host ]; then
  find /tmp/uno-platform-host -maxdepth 1 -type f -exec cp {} "$HOME/.local/share/Uno Platform/" \;
else
  echo "Note: /tmp/uno-platform-host not found — skipping Uno Platform license copy"
fi

dotnet dev-certs https --trust || true

# ---------------------------------------------------------------------------
# D-Bus session + xdg-desktop-portal (file picker dialogs on Linux/GTK)
#
# Uno's FileSavePicker/FileOpenPicker on the Skia GTK backend delegates to
# xdg-desktop-portal, which requires a running D-Bus session. The host's
# DBUS_SESSION_BUS_ADDRESS is not forwarded because the host socket isn't
# mounted into the container — we start our own session bus here and pin
# the address in shell rc files so new terminals inherit it.
# ---------------------------------------------------------------------------
DBUS_RUNTIME_DIR="/run/user/$(id -u)"
DBUS_SOCKET="${DBUS_RUNTIME_DIR}/bus"

sudo mkdir -p "$DBUS_RUNTIME_DIR"
sudo chown "$(id -u):$(id -g)" "$DBUS_RUNTIME_DIR"
chmod 700 "$DBUS_RUNTIME_DIR"

if [ ! -S "$DBUS_SOCKET" ] || ! dbus-send \
    --address="unix:path=${DBUS_SOCKET}" \
    --dest=org.freedesktop.DBus \
    --type=method_call \
    /org/freedesktop/DBus \
    org.freedesktop.DBus.ListNames &>/dev/null; then
  rm -f "$DBUS_SOCKET"
  dbus-daemon --session \
    --address="unix:path=${DBUS_SOCKET}" \
    --nopidfile \
    --fork 2>/dev/null || true
fi

export DBUS_SESSION_BUS_ADDRESS="unix:path=${DBUS_SOCKET}"
export XDG_RUNTIME_DIR="$DBUS_RUNTIME_DIR"
# xdg-desktop-portal picks the backend based on XDG_CURRENT_DESKTOP.
export XDG_CURRENT_DESKTOP="${XDG_CURRENT_DESKTOP:-GNOME}"

# Persist for future shells (aliased terminals, VS Code tasks, etc.)
for rc in "$HOME/.bashrc" "$HOME/.zshrc"; do
  [ -f "$rc" ] || continue
  # Drop previous snippet so updates propagate on container rebuild.
  sed -i '/# devcontainer: dbus session bus/,/# end devcontainer: dbus session bus/d' "$rc"
  cat >> "$rc" <<'DBUSRC'

# devcontainer: dbus session bus
_dbus_socket="/run/user/$(id -u)/bus"
if [ ! -S "$_dbus_socket" ] || ! dbus-send --address="unix:path=$_dbus_socket" \
    --dest=org.freedesktop.DBus --type=method_call \
    /org/freedesktop/DBus org.freedesktop.DBus.ListNames &>/dev/null; then
  rm -f "$_dbus_socket"
  dbus-daemon --session --address="unix:path=$_dbus_socket" --nopidfile --fork 2>/dev/null || true
fi
export DBUS_SESSION_BUS_ADDRESS="unix:path=$_dbus_socket"
export XDG_RUNTIME_DIR="/run/user/$(id -u)"
export XDG_CURRENT_DESKTOP="${XDG_CURRENT_DESKTOP:-GNOME}"
unset _dbus_socket
# end devcontainer: dbus session bus
DBUSRC
done

printf 'claude --dangerously-skip-permissions\n' >> "$HOME/.bash_history"
printf 'copilot --autopilot --allow-all\n' >> "$HOME/.bash_history"

# Alias so bare `claude` always starts in bypass-permissions mode (safe in devcontainer)
# and disables adaptive thinking (CLAUDE_CODE_DISABLE_ADAPTIVE_THINKING=1 is scoped to the command).
for rc in "$HOME/.bashrc" "$HOME/.zshrc"; do
  if [ -f "$rc" ]; then
    # Drop any previous `alias claude=` line so env-var/flag changes propagate on container restart.
    sed -i '/^alias claude=/d' "$rc"
    printf '\nalias claude="CLAUDE_CODE_DISABLE_ADAPTIVE_THINKING=1 claude --dangerously-skip-permissions"\n' >> "$rc"
  fi
done

# Alias so bare `copilot` runs in full autopilot mode (safe in devcontainer)
for rc in "$HOME/.bashrc" "$HOME/.zshrc"; do
  if [ -f "$rc" ] && ! grep -q 'alias copilot=' "$rc"; then
    printf '\nalias copilot="copilot --autopilot --allow-all"\n' >> "$rc"
  fi
done

# ---------------------------------------------------------------------------
# GitHub MCP: read-only token validation + registration
# ---------------------------------------------------------------------------
if [ -n "${GH_TOKEN:-}" ]; then
  echo "Validating GitHub token is read-only..."

  # First, verify the token is actually valid
  AUTH_STATUS=$(curl -sS -o /dev/null -w "%{http_code}" \
    -H "Authorization: token ${GH_TOKEN}" \
    https://api.github.com/user 2>/dev/null || echo "000")

  # Define write scopes that MUST NOT be present
  WRITE_SCOPES="repo,public_repo,delete_repo,gist,workflow,write:org,admin:org,write:public_key,admin:public_key,write:repo_hook,admin:repo_hook,admin:org_hook,write:packages,write:gpg_key,admin:gpg_key,write:discussion,admin:enterprise"

  TOKEN_REJECTED=false
  if [ "$AUTH_STATUS" != "200" ]; then
    echo "ERROR: GitHub token is invalid or unauthorized (HTTP ${AUTH_STATUS})." >&2
    TOKEN_REJECTED=true
  else
    # Fetch token scopes from the API response headers (classic PATs)
    SCOPES_HEADER=$(curl -sS -f -H "Authorization: token ${GH_TOKEN}" \
      -I https://api.github.com/user 2>/dev/null \
      | grep -i '^x-oauth-scopes:' | cut -d: -f2- | tr -d '[:space:]') || true

    if [ -n "$SCOPES_HEADER" ]; then
      # Classic PAT — check each write scope
      IFS=',' read -ra BLOCKED <<< "$WRITE_SCOPES"
      for scope in "${BLOCKED[@]}"; do
        scope=$(echo "$scope" | xargs)  # trim whitespace
        if echo ",$SCOPES_HEADER," | grep -qi ",$scope,"; then
          echo "ERROR: GitHub token has write scope '$scope'. Only read-only tokens are allowed in the devcontainer." >&2
          TOKEN_REJECTED=true
          break
        fi
      done
    else
      # Fine-grained PAT — no X-OAuth-Scopes header is returned.
      # Probe a write endpoint with invalid payload: 403 = no write access (good),
      # 422 = has write access but bad payload (rejected).
      WRITE_TEST=$(curl -sS -o /dev/null -w "%{http_code}" \
        -H "Authorization: token ${GH_TOKEN}" \
        -H "Content-Type: application/json" \
        -X POST https://api.github.com/user/repos \
        -d '{"name":""}' 2>/dev/null) || true

      if [ "$WRITE_TEST" = "422" ] || [ "$WRITE_TEST" = "201" ]; then
        echo "ERROR: GitHub token has write permissions (repo create returned HTTP $WRITE_TEST). Only read-only tokens are allowed in the devcontainer." >&2
        TOKEN_REJECTED=true
      fi
      # 403 or 404 = no write access, which is what we want
    fi
  fi

  if [ "$TOKEN_REJECTED" = true ]; then
    echo "GitHub MCP will NOT be registered. Please use a classic PAT with only read scopes." >&2
    unset GH_TOKEN
  else
    echo "GitHub token validated — no write scopes detected."

    # Register GitHub MCP for Claude Code
    # The server reads GITHUB_PERSONAL_ACCESS_TOKEN; map from GH_TOKEN.
    # NOTE: -e must come AFTER the server name — the flag is variadic and
    # consumes all subsequent args until `--` if placed before the name.
    claude mcp remove github || true
    claude mcp add --scope user --transport stdio \
      github -e "GITHUB_PERSONAL_ACCESS_TOKEN=${GH_TOKEN}" \
      -- npx -y @modelcontextprotocol/server-github 2>/dev/null || true
    echo "Claude GitHub MCP registered."
  fi
else
  echo "Note: GH_TOKEN not set — GitHub MCP will not be registered. Set GH_TOKEN in your WSL environment to enable it."
fi

# ---------------------------------------------------------------------------
# Azure DevOps PAT: read-only token validation
# ---------------------------------------------------------------------------
AZDO_ORG="https://dev.azure.com/uno-platform"
AZDO_PROJECT="uno-private"

if [ -n "${AZURE_DEVOPS_EXT_PAT:-}" ]; then
  echo "Validating Azure DevOps PAT..."

  # Validate by hitting the target org/project builds endpoint directly.
  # This confirms both authentication and org/project access in one call.
  AZDO_STATUS=$(curl -sS -o /dev/null -w "%{http_code}" \
    -u ":${AZURE_DEVOPS_EXT_PAT}" \
    "${AZDO_ORG}/${AZDO_PROJECT}/_apis/build/builds?api-version=7.0" 2>/dev/null) || true

  if [ "$AZDO_STATUS" = "200" ]; then
    echo "Azure DevOps PAT validated — org access confirmed."
    az devops configure --defaults organization="$AZDO_ORG" project="$AZDO_PROJECT" 2>/dev/null || true
    echo "  az devops defaults set to ${AZDO_ORG} / ${AZDO_PROJECT}."
  elif [ "$AZDO_STATUS" = "000" ]; then
    echo "WARNING: Could not reach ${AZDO_ORG} (network/DNS error)." >&2
    echo "  Check that dev.azure.com is reachable from the container." >&2
    echo "  az devops commands may not work until connectivity is restored." >&2
  elif [ "$AZDO_STATUS" = "401" ]; then
    echo "ERROR: Azure DevOps PAT is invalid or expired (HTTP 401)." >&2
    echo "  Generate a new token at ${AZDO_ORG}/_usersSettings/tokens" >&2
    echo "  and update AZDO_PAT_READONLY in your WSL shell profile." >&2
  else
    echo "ERROR: Azure DevOps PAT cannot access ${AZDO_ORG}/${AZDO_PROJECT} (HTTP ${AZDO_STATUS})." >&2
    echo "  Ensure the token has Build (Read) scope for the uno-platform org." >&2
    echo "  Update AZDO_PAT_READONLY in your WSL shell profile if needed." >&2
  fi
else
  echo "Note: AZURE_DEVOPS_EXT_PAT not set — az devops commands will not be authenticated. Set AZDO_PAT_READONLY in your WSL environment to enable it."
fi

echo "Registering Claude MCPs for Uno Platform: uno (HTTP docs server) and uno-app (stdio app tooling)."
echo "To verify, run: claude mcp list"

claude mcp remove uno || true
claude mcp add --scope user --transport http uno https://mcp.platform.uno/v1 || true
claude mcp remove uno-app || true
claude mcp add --scope user --transport stdio uno-app -- dotnet dnx -y uno.devserver --mcp-app --solution-dir /daily-reflection || true

echo "Claude MCP registration complete. If you encounter issues, run 'claude mcp list' or 'claude mcp inspect uno' / 'claude mcp inspect uno-app'."

# ---------------------------------------------------------------------------
# Copilot CLI: MCP servers & settings
# ---------------------------------------------------------------------------
COPILOT_DIR="$HOME/.copilot"
mkdir -p "$COPILOT_DIR"

echo "Registering Copilot MCPs for Uno Platform: uno (HTTP docs server) and uno-app (stdio app tooling)."

COPILOT_MCP_CONFIG="$COPILOT_DIR/mcp-config.json"

# Build MCP servers JSON — include GitHub MCP only if GH_TOKEN is available
if [ -n "${GH_TOKEN:-}" ]; then
  MCP_SERVERS=$(jq -n \
    --arg gh_token "$GH_TOKEN" \
    '{
      "mcpServers": {
        "uno": { "type": "http", "url": "https://mcp.platform.uno/v1" },
        "uno-app": {
          "type": "stdio",
          "command": "dotnet",
          "args": ["dnx", "-y", "--prerelease", "uno.devserver", "--mcp-app", "--solution-dir", "/daily-reflection"]
        },
        "github": {
          "type": "stdio",
          "command": "npx",
          "args": ["-y", "@modelcontextprotocol/server-github"],
          "env": { "GITHUB_PERSONAL_ACCESS_TOKEN": $gh_token }
        }
      }
    }')
  echo "Copilot MCPs: uno, uno-app, github"
else
  MCP_SERVERS='{
    "mcpServers": {
      "uno": { "type": "http", "url": "https://mcp.platform.uno/v1" },
      "uno-app": {
        "type": "stdio",
        "command": "dotnet",
        "args": ["dnx", "-y", "--prerelease", "uno.devserver", "--mcp-app", "--solution-dir", "/daily-reflection"]
      }
    }
  }'
  echo "Copilot MCPs: uno, uno-app (GitHub MCP skipped — no GH_TOKEN)"
fi

if [ -f "$COPILOT_MCP_CONFIG" ]; then
  if jq empty "$COPILOT_MCP_CONFIG" 2>/dev/null; then
    # Deep-merge new servers into existing config (preserves user-added servers)
    echo "$MCP_SERVERS" | jq -s '.[0] * .[1]' "$COPILOT_MCP_CONFIG" - > "${COPILOT_MCP_CONFIG}.tmp" \
      && mv "${COPILOT_MCP_CONFIG}.tmp" "$COPILOT_MCP_CONFIG"
  else
    echo "Warning: $COPILOT_MCP_CONFIG contains invalid JSON. Backing up to ${COPILOT_MCP_CONFIG}.bak" >&2
    cp "$COPILOT_MCP_CONFIG" "${COPILOT_MCP_CONFIG}.bak"
    echo "$MCP_SERVERS" | jq . > "$COPILOT_MCP_CONFIG"
  fi
else
  echo "$MCP_SERVERS" | jq . > "$COPILOT_MCP_CONFIG"
fi

echo "Copilot MCP registration complete. Config written to $COPILOT_MCP_CONFIG."

# ---------------------------------------------------------------------------
# Claude Code status line
# ---------------------------------------------------------------------------
cat > "$HOME/.claude/statusline.sh" << 'STATUSLINE'
#!/bin/bash
export LANG=C.UTF-8
input=$(cat)

MODEL=$(echo "$input" | jq -r '.model.display_name')
DIR=$(echo "$input" | jq -r '.workspace.current_dir')
COST=$(echo "$input" | jq -r '.cost.total_cost_usd // 0')
PCT=$(echo "$input" | jq -r '.context_window.used_percentage // 0' | cut -d. -f1)
DURATION_MS=$(echo "$input" | jq -r '.cost.total_duration_ms // 0')

CYAN='\033[36m'; GREEN='\033[32m'; YELLOW='\033[33m'; RED='\033[31m'; RESET='\033[0m'

# Pick bar color based on context usage
if [ "$PCT" -ge 90 ]; then BAR_COLOR="$RED"
elif [ "$PCT" -ge 70 ]; then BAR_COLOR="$YELLOW"
else BAR_COLOR="$GREEN"; fi

FILLED=$((PCT / 10)); EMPTY=$((10 - FILLED))
BAR=$(printf "%${FILLED}s" | tr ' ' '=')$(printf "%${EMPTY}s" | tr ' ' '-')

MINS=$((DURATION_MS / 60000)); SECS=$(((DURATION_MS % 60000) / 1000))

BRANCH=""
git rev-parse --git-dir > /dev/null 2>&1 && BRANCH=" | 🌿 $(git branch --show-current 2>/dev/null)"

echo -e "${CYAN}[$MODEL]${RESET} 📁 ${DIR##*/}$BRANCH"
COST_FMT=$(printf '$%.2f' "$COST")
echo -e "${BAR_COLOR}${BAR}${RESET} ${PCT}% | ${YELLOW}${COST_FMT}${RESET} | ⏱️ ${MINS}m ${SECS}s"
STATUSLINE
chmod +x "$HOME/.claude/statusline.sh"

# Deep-merge statusLine config into Claude settings (preserves existing nested keys)
CLAUDE_SETTINGS="$HOME/.claude/settings.json"
STATUSLINE_CFG='{"skipDangerousModePermissionPrompt":true,"permissions":{"defaultMode":"bypassPermissions"},"statusLine":{"type":"command","command":"~/.claude/statusline.sh","padding":2}}'
if [ -f "$CLAUDE_SETTINGS" ]; then
  if jq empty "$CLAUDE_SETTINGS" 2>/dev/null; then
    jq ". * $STATUSLINE_CFG" "$CLAUDE_SETTINGS" > "${CLAUDE_SETTINGS}.tmp" && mv "${CLAUDE_SETTINGS}.tmp" "$CLAUDE_SETTINGS"
  else
    echo "Warning: $CLAUDE_SETTINGS contains invalid JSON. Backing up to ${CLAUDE_SETTINGS}.bak" >&2
    cp "$CLAUDE_SETTINGS" "${CLAUDE_SETTINGS}.bak"
    echo "$STATUSLINE_CFG" | jq . > "$CLAUDE_SETTINGS"
  fi
else
  echo "$STATUSLINE_CFG" | jq . > "$CLAUDE_SETTINGS"
fi
