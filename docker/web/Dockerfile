# Build context: apps/web, plus the named context `docker-web` pointing at docker/web for nginx.conf
# (see docker/docker-compose.yml). Stage 1 builds the Angular production bundle; stage 2 serves it
# with Nginx, which also proxies /api to the API container so the client runs on one origin.

FROM node:24-alpine AS build
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY . .
# PrimeUI license key for PrimeNG, from docker/.env through compose's build args; empty keeps the license badge.
ARG PRIMEUI_LICENSE=""
RUN npx ng build --configuration production --define "PRIMEUI_LICENSE='${PRIMEUI_LICENSE}'"

FROM nginx:1.30-alpine
COPY --from=docker-web nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist/web/browser /usr/share/nginx/html
EXPOSE 80
