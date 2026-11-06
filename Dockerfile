ARG APP_NAME=angular-frontend
ARG APP_VERSION=1.0.0
ARG OTEL_RESOURCE_ATTRIBUTES=""
ARG OTEL_EXPORTER_OTLP_HEADERS=""

FROM node:26-slim@sha256:5adad64f9a1fca2d51972caa65983634c8a774eb300d72b20f7f2a0f60368072 AS base
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN corepack enable

FROM base AS build
COPY MyNetatmo24.Frontend /usr/src/app/MyNetatmo24.Frontend
COPY .npmrc /usr/src/app
COPY package.json /usr/src/app
COPY pnpm-lock.yaml /usr/src/app
COPY pnpm-workspace.yaml /usr/src/app
WORKDIR /usr/src/app
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --frozen-lockfile --prefer-offline
ENV OTEL_RESOURCE_ATTRIBUTES="service.name=$APP_NAME,service.version=$APP_VERSION,$OTEL_RESOURCE_ATTRIBUTES"
ENV OTEL_EXPORTER_OTLP_HEADERS=$OTEL_EXPORTER_OTLP_HEADERS
RUN pnpm run -r build
RUN pnpm deploy --filter=frontend-app --prod /prod/frontend-app
RUN ls /prod/frontend-app

FROM nginx:alpine@sha256:b2f80f8f88146b0772fdcc7d92cea621aa8c2889fa8d07ed61d4884fcea53e09 AS frontend-app
COPY --from=build /prod/frontend-app/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /prod/frontend-app/dist/browser /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
