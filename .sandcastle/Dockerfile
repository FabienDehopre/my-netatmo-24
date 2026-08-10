FROM mcr.microsoft.com/dotnet/sdk:10.0-noble@sha256:72dd743782f2ae7e5476fd64f6a460045e3998dc862218b80e6944cba79a01b0

# Install system dependencies including Node.js
RUN apt-get update && apt-get install -y git curl jq \
  && curl -sL https://deb.nodesource.com/setup_24.x | bash - \
  && apt-get install -y nodejs \
  && apt-get clean \
  && rm -rf /var/lib/apt/lists/* \
  && echo 'node version:' $(node -v) \
  && echo 'dotnet version:' $(dotnet --version)

# Install GitHub CLI
RUN curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg \
  | dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg \
  && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" \
  | tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
  && apt-get update && apt-get install -y gh \
  && apt-get clean \
  && rm -rf /var/lib/apt/lists/*

# Install Docker CLI (Docker-outside-of-Docker: host socket bind-mounted at container start)
RUN install -m 0755 -d /etc/apt/keyrings \
  && curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc \
  && chmod a+r /etc/apt/keyrings/docker.asc \
  && echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu noble stable" \
  | tee /etc/apt/sources.list.d/docker.list > /dev/null \
  && apt-get update && apt-get install -y docker-ce-cli \
  && apt-get clean \
  && rm -rf /var/lib/apt/lists/*

# Build-args for UID/GID alignment: sandcastle docker build-image
# defaults these to the host user's UID/GID so image-built files
# and bind-mounted files share an owner without runtime chown.
ARG AGENT_UID=1000
ARG AGENT_GID=1000

# Create the "agent" user and align UID/GID.
RUN groupadd -o -g ${AGENT_GID} agent \
  && useradd -o -u ${AGENT_UID} -g ${AGENT_GID} -d /home/agent -m agent
USER ${AGENT_UID}:${AGENT_GID}
WORKDIR /home/agent

# Install Claude Code CLI
RUN curl -fsSL https://claude.ai/install.sh | bash

# Pre-configure claude code
COPY --chown=${AGENT_UID}:${AGENT_GID} claude-settings.json /home/agent/.claude/settings.json

# Install Auth0 CLI
RUN mkdir -p /home/agent/.local/bin \
  && curl -sSfL https://raw.githubusercontent.com/auth0/auth0-cli/main/install.sh | sh -s -- -b /home/agent/.local/bin

# Build-arg for PNPM version
ARG PNPM_VERSION=11.20.0

# Install PNPM CLI
# RUN curl -fsSL https://get.pnpm.io/install.sh | PNPM_VERSION=${PNPM_VERSION} ENV="$HOME/.bashrc" SHELL="$(which bash)" bash -
RUN curl -fsSL https://get.pnpm.io/install.sh | PNPM_VERSION=${PNPM_VERSION} ENV="$HOME/.shrc" SHELL="$(which sh)" sh -

# Add Claude, PNPM, and dotnet global tools to PATH
ENV PNPM_HOME=/home/agent/.local/share/pnpm
ENV PNPM_CONFIG_STORE_DIR="$PNPM_HOME/store"
ENV PATH="/home/agent/.local/bin:$PNPM_HOME/bin:/home/agent/.dotnet/tools:$PATH"

# Install dotnet-ef for EF Core migrations
RUN dotnet tool install --global dotnet-ef

# Install Claude Code plugins
RUN claude plugin marketplace add https://github.com/anthropics/claude-plugins-official.git \
  && claude plugin marketplace add https://github.com/JuliusBrussee/caveman.git \
  && claude plugin install caveman@caveman --scope user \
  && claude plugin install commit-commands@claude-plugins-official --scope user \
  && claude plugin install csharp-lsp@claude-plugins-official --scope user \
  && claude plugin install security-guidance@claude-plugins-official --scope user \
  && claude plugin install typescript-lsp@claude-plugins-official --scope user

# Set CI=true to disable interactive prompts in tools that support it (e.g., PNPM)
ENV CI=true

# In worktree sandbox mode, Sandcastle bind-mounts the git worktree at /home/agent/workspace
# and overrides the working directory to /home/agent/workspace at container start.
# Structure your Dockerfile so that /home/agent/workspace can serve as the project root.
ENTRYPOINT ["sleep", "infinity"]
