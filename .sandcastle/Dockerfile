FROM node:24-bookworm@sha256:934240a162082fd8b8a2f90cd5114446443f1eba1c5378f6687167ca405e6584

# Install system dependencies
RUN apt-get update && apt-get install -y \
  git \
  curl \
  jq \
  && rm -rf /var/lib/apt/lists/*

# Install GitHub CLI
RUN curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg \
  | dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg \
  && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" \
  | tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
  && apt-get update && apt-get install -y gh \
  && rm -rf /var/lib/apt/lists/*

# Build-args for UID/GID alignment: sandcastle docker build-image
# defaults these to the host user's UID/GID so image-built files
# and bind-mounted files share an owner without runtime chown.
ARG AGENT_UID=1000
ARG AGENT_GID=1000

# Rename the base image's "node" user to "agent" and align UID/GID.
RUN groupmod -o -g ${AGENT_GID} node \
  && usermod -o -u ${AGENT_UID} -g ${AGENT_GID} -d /home/agent -m -l agent node \
  && chown -R ${AGENT_UID}:${AGENT_GID} /home/agent
# USER ${AGENT_UID}:${AGENT_GID}
USER agent
WORKDIR /home/agent

# Install Claude Code CLI
RUN curl -fsSL https://claude.ai/install.sh | bash

# Pre-configure plugins
RUN mkdir -p /home/agent/.claude && cat > /home/agent/.claude/settings.json << 'EOF'
{
  "enabledPlugins": {
    "caveman@caveman": true,
    "commit-commands@claude-plugins-official": true,
    "csharp-lsp@claude-plugins-official": true,
    "security-guidance@claude-plugins-official": true,
    "typescript-lsp@claude-plugins-official": true
  },
  "extraKnownMarketplaces": {
    "caveman": {
      "source": {
        "source": "github",
        "repo": "JuliusBrussee/caveman"
      }
    }
  },
  "alwaysThinkingEnabled": true,
  "effortLevel": "high",
  "advisorModel": "opus"
}
EOF

# Install Auth0 CLI
RUN mkdir -p /home/agent/.local/bin \
  && curl -sSfL https://raw.githubusercontent.com/auth0/auth0-cli/main/install.sh | sh -s -- -b /home/agent/.local/bin

# Build-arg for PNPM version
ARG PNPM_VERSION=11.20.0

# Install PNPM CLI
# RUN curl -fsSL https://get.pnpm.io/install.sh | PNPM_VERSION=${PNPM_VERSION} ENV="$HOME/.bashrc" SHELL="$(which bash)" bash -
RUN curl -fsSL https://get.pnpm.io/install.sh | PNPM_VERSION=${PNPM_VERSION} ENV="$HOME/.shrc" SHELL="$(which sh)" sh -

# Add Claude and PNPM to PATH
ENV PNPM_HOME=/home/agent/.local/share/pnpm
ENV PNPM_CONFIG_STORE_DIR="$PNPM_HOME/store"
ENV PATH="/home/agent/.local/bin:$PNPM_HOME/bin:$PATH"

# Set CI=true to disable interactive prompts in tools that support it (e.g., PNPM)
ENV CI=true

# In worktree sandbox mode, Sandcastle bind-mounts the git worktree at /home/agent/workspace
# and overrides the working directory to /home/agent/workspace at container start.
# Structure your Dockerfile so that /home/agent/workspace can serve as the project root.
ENTRYPOINT ["sleep", "infinity"]
