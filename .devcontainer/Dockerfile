FROM mcr.microsoft.com/dotnet/sdk:10.0

ARG TZ
ENV TZ="$TZ"

ARG CLAUDE_CODE_VERSION=latest
ARG COPILOT_CLI_VERSION=latest

# ---------------------------------------------------------------------------
# System packages: core dev tools + firewall utilities
# ---------------------------------------------------------------------------
RUN apt-get update && apt-get install -y --no-install-recommends \
  less \
  git \
  procps \
  sudo \
  fzf \
  zsh \
  man-db \
  unzip \
  gnupg2 \
  jq \
  nano \
  vim \
  # DNS-based firewall
  dnsmasq \
  iptables \
  iproute2 \
  dnsutils \
  # Uno / Skia desktop dependencies (from build/ci/templates/linux-install-deps.yml)
  xvfb \
  fluxbox \
  vlc \
  libvlc-dev \
  libgtk-3-dev \
  libwebkit2gtk-4.1-dev \
  # File picker dialog support (xdg-desktop-portal chain + zenity fallback)
  xdg-desktop-portal \
  xdg-desktop-portal-gtk \
  zenity \
  dbus \
  dbus-user-session \
  # SSH client (WSL host terminal)
  openssh-client \
  # Mono profiler (mprof-report CLI + profiler .so libraries)
  mono-utils \
  libmono-profiler \
  # Misc
  curl \
  wget \
  ca-certificates \
  && apt-get clean && rm -rf /var/lib/apt/lists/*

# ---------------------------------------------------------------------------
# Azure CLI + DevOps extension (az devops)
# ---------------------------------------------------------------------------
RUN curl -sL https://aka.ms/InstallAzureCLIDeb | bash \
  && az extension add --name azure-devops --system \
  && apt-get clean && rm -rf /var/lib/apt/lists/*

# ---------------------------------------------------------------------------
# GitHub CLI (not in bookworm defaults)
# ---------------------------------------------------------------------------
RUN curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg \
      | dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg \
  && chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg \
  && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" \
      > /etc/apt/sources.list.d/github-cli.list \
  && apt-get update && apt-get install -y --no-install-recommends gh \
  && apt-get clean && rm -rf /var/lib/apt/lists/*

# ---------------------------------------------------------------------------
# Node.js 20 (required for Playwright install-deps, Claude Code npm install, and npm tooling)
# ---------------------------------------------------------------------------
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
  && apt-get install -y --no-install-recommends nodejs \
  && apt-get clean && rm -rf /var/lib/apt/lists/*

# ---------------------------------------------------------------------------
# Playwright system dependencies (Chromium needs libnss3, libnspr4, etc.)
# ---------------------------------------------------------------------------
RUN npx playwright install-deps chromium

# ---------------------------------------------------------------------------
# git-delta (better diffs)
# ---------------------------------------------------------------------------
ARG GIT_DELTA_VERSION=0.18.2
RUN ARCH=$(dpkg --print-architecture) && \
  wget -q "https://github.com/dandavison/delta/releases/download/${GIT_DELTA_VERSION}/git-delta_${GIT_DELTA_VERSION}_${ARCH}.deb" && \
  dpkg -i "git-delta_${GIT_DELTA_VERSION}_${ARCH}.deb" && \
  rm "git-delta_${GIT_DELTA_VERSION}_${ARCH}.deb"

# ---------------------------------------------------------------------------
# Non-root user: developer (uid 1000)
# Container is the security boundary, not in-container privileges.
# ---------------------------------------------------------------------------
ARG USERNAME=developer
RUN existing_user=$(getent passwd 1000 | cut -d: -f1 || true) && \
  if [ -n "$existing_user" ] && [ "$existing_user" != "${USERNAME}" ]; then \
    usermod -l ${USERNAME} -d /home/${USERNAME} -m "$existing_user" && \
    groupmod -n ${USERNAME} "$existing_user" 2>/dev/null || true; \
  elif [ -z "$existing_user" ]; then \
    useradd -m -s /bin/zsh -u 1000 ${USERNAME}; \
  fi && \
  chsh -s /bin/zsh ${USERNAME} && \
  echo "${USERNAME} ALL=(ALL) NOPASSWD:ALL" > /etc/sudoers.d/${USERNAME} && \
  chmod 0440 /etc/sudoers.d/${USERNAME}

# Persist shell history (env vars are set in the zsh-in-docker -a flag below)
RUN mkdir -p /commandhistory && \
  touch /commandhistory/.bash_history && \
  chown -R ${USERNAME} /commandhistory

# Set DEVCONTAINER env
ENV DEVCONTAINER=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# Ensure non-root user owns /usr/local/share for npm global installs
RUN mkdir -p /usr/local/share/npm-global && \
  chown -R ${USERNAME}:${USERNAME} /usr/local/share/npm-global

# Create workspace and config directories
RUN mkdir -p /daily-reflection /home/${USERNAME}/.claude /home/${USERNAME}/.copilot /home/${USERNAME}/.nuget && \
  chown -R ${USERNAME}:${USERNAME} /daily-reflection /home/${USERNAME}/.claude /home/${USERNAME}/.copilot /home/${USERNAME}/.nuget

WORKDIR /daily-reflection

# ---------------------------------------------------------------------------
# zsh + powerlevel10k (shell UX from reference)
# ---------------------------------------------------------------------------
ARG ZSH_IN_DOCKER_VERSION=1.2.0
USER ${USERNAME}
RUN sh -c "$(wget -O- https://github.com/deluan/zsh-in-docker/releases/download/v${ZSH_IN_DOCKER_VERSION}/zsh-in-docker.sh)" -- \
  -p git \
  -p fzf \
  -a "source /usr/share/doc/fzf/examples/key-bindings.zsh 2>/dev/null || true" \
  -a "source /usr/share/doc/fzf/examples/completion.zsh 2>/dev/null || true" \
  -a "export PROMPT_COMMAND='history -a' && export HISTFILE=/commandhistory/.bash_history" \
  -x

ENV SHELL=/bin/zsh
ENV EDITOR=nano
ENV VISUAL=nano

# npm global prefix — writable by non-root user
ENV NPM_CONFIG_PREFIX=/usr/local/share/npm-global
ENV PATH=$PATH:/usr/local/share/npm-global/bin

# ---------------------------------------------------------------------------
# Claude Code CLI (native installer — installs to ~/.local/bin/claude)
# ---------------------------------------------------------------------------
RUN curl -fsSL https://claude.ai/install.sh | bash -s -- ${CLAUDE_CODE_VERSION}
ENV PATH="/home/${USERNAME}/.local/bin:${PATH}"

# ---------------------------------------------------------------------------
# GitHub Copilot CLI (installs to ~/.local/bin/copilot)
# ---------------------------------------------------------------------------
RUN curl -fsSL https://gh.io/copilot-install | bash -s -- ${COPILOT_CLI_VERSION}

# ---------------------------------------------------------------------------
# Microsoft Build of OpenJDK 21 — required by the Android SDK tooling chain
# (sdkmanager, aapt2, d8). Installed manually because uno-check does not
# provision a JDK on Linux. Follows the install flow from
# https://learn.microsoft.com/en-us/java/openjdk/install#install-on-ubuntu
# (the MS package repo serves both Ubuntu and Debian via the same layout).
# ---------------------------------------------------------------------------
USER root
RUN . /etc/os-release && \
  if [ "$ID" = "ubuntu" ]; then \
    MS_REPO_PATH="ubuntu/${VERSION_ID}"; \
  else \
    MS_REPO_PATH="debian/${VERSION_ID%%.*}"; \
  fi && \
  wget -qO /tmp/packages-microsoft-prod.deb \
    "https://packages.microsoft.com/config/${MS_REPO_PATH}/packages-microsoft-prod.deb" && \
  dpkg -i /tmp/packages-microsoft-prod.deb && \
  rm /tmp/packages-microsoft-prod.deb && \
  apt-get update && apt-get install -y --no-install-recommends msopenjdk-21 && \
  apt-get clean && rm -rf /var/lib/apt/lists/* && \
  # The msopenjdk-21 package installs at `/usr/lib/jvm/msopenjdk-21-<arch>`,
  # where <arch> is amd64 / arm64 / etc. Resolve the actual install dir by
  # walking from `javac` and pin a stable symlink so JAVA_HOME below is
  # arch-agnostic — required because mcr.microsoft.com/dotnet/sdk:10.0 is
  # multi-arch and devcontainers may be built on Apple Silicon hosts.
  ln -sfn "$(dirname "$(dirname "$(readlink -f "$(command -v javac)")")")" \
    /usr/lib/jvm/msopenjdk-21-current

ENV JAVA_HOME=/usr/lib/jvm/msopenjdk-21-current
ENV PATH=${JAVA_HOME}/bin:${PATH}

# ---------------------------------------------------------------------------
# .NET workloads (pre-install so builds work immediately)
# ---------------------------------------------------------------------------
RUN dotnet workload install wasm-tools android

# ---------------------------------------------------------------------------
# uno-check (validate environment, from build/ci/scripts/run-uno-check.sh)
# Installed as developer so the tool is usable interactively. Android SDK +
# platform-tools + build-tools are provisioned by uno-check (we only pre-install
# the JDK above because uno-check does not handle it on Linux).
# --skip androidemulator stays: agents + CI build APKs, they do not run them.
# ---------------------------------------------------------------------------
USER ${USERNAME}
ENV PATH="${PATH}:/home/${USERNAME}/.dotnet/tools"
RUN dotnet tool install --global uno.check
USER root
RUN uno-check -v --ci --non-interactive --fix \
  --skip xcode --skip gtk3 --skip vswin --skip vswinworkloads --skip unosdk \
  --skip dotnetnewunotemplates --skip vsmac --skip openjdk --skip androidemulator \
  --tfm net10.0-desktop --tfm net10.0-browserwasm --tfm net10.0-android \
  || true

# ---------------------------------------------------------------------------
# Neutralise VS Code git-credential forwarding
# dpkg-divert moves the real binary to /usr/bin/git.distrib so that even
# direct /usr/bin/git calls go through the wrapper.
# ---------------------------------------------------------------------------
RUN dpkg-divert --rename --add /usr/bin/git && \
  printf '#!/bin/sh\nunset GIT_ASKPASS VSCODE_GIT_ASKPASS_MAIN VSCODE_GIT_ASKPASS_NODE VSCODE_GIT_IPC_HANDLE SSH_AUTH_SOCK\nexec /usr/bin/git.distrib -c credential.helper= "$@"\n' \
  > /usr/bin/git && \
  chmod +x /usr/bin/git

# ---------------------------------------------------------------------------
# Firewall script
# ---------------------------------------------------------------------------
COPY init-firewall.sh /usr/local/bin/
COPY ssh-wsl-host.sh /usr/local/bin/
RUN chmod +x /usr/local/bin/init-firewall.sh /usr/local/bin/ssh-wsl-host.sh && \
  echo "${USERNAME} ALL=(root) NOPASSWD: /usr/local/bin/init-firewall.sh" > /etc/sudoers.d/${USERNAME}-firewall && \
  chmod 0440 /etc/sudoers.d/${USERNAME}-firewall

USER ${USERNAME}
