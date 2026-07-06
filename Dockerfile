ARG APP_NAME=angular-frontend
ARG APP_VERSION=1.0.0
ARG OTEL_RESOURCE_ATTRIBUTES=""
ARG OTEL_EXPORTER_OTLP_HEADERS=""

FROM node:24-slim@sha256:b31e7a42fdf8b8aa5f5ed477c72d694301273f1069c5a2f71d53c6482e99a2fc AS base
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

FROM nginx:alpine@sha256:54f2a904c251d5a34adf545a72d32515a15e08418dae0266e23be2e18c66fefa AS frontend-app
COPY --from=build /prod/frontend-app/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /prod/frontend-app/dist/browser /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
