FROM node:24-slim@sha256:b506e7321f176aae77317f99d67a24b272c1f09f1d10f1761f2773447d8da26c AS base
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
RUN pnpm run -r build
RUN pnpm deploy --filter=frontend-app --prod /prod/frontend-app
RUN ls /prod/frontend-app

FROM nginx:alpine@sha256:582c496ccf79d8aa6f8203a79d32aaf7ffd8b13362c60a701a2f9ac64886c93d AS frontend-app
COPY --from=build /prod/frontend-app/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /prod/frontend-app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
