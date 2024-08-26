FROM node:lts-alpine AS build

WORKDIR /app
RUN corepack enable pnpm

COPY package.json package.json
COPY pnpm-lock.yaml pnpm-lock.yaml

RUN pnpm install --frozen-lockfile

COPY . .

RUN pnpm build

FROM nginx:alpine

COPY --from=build /app/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist/my-netatmo24.frontend/browser /usr/share/nginx/html

# Expose the default nginx port
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
