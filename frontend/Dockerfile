FROM node:20-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci || npm install

COPY . .
RUN npm run build:prod

FROM nginx:1.27-alpine AS runtime
WORKDIR /usr/share/nginx/html

COPY --from=build /app/dist/medcarehub-web/browser/ ./

COPY nginx/default.conf /etc/nginx/conf.d/default.conf
COPY nginx/99-env-config.sh /docker-entrypoint.d/99-env-config.sh
RUN chmod +x /docker-entrypoint.d/99-env-config.sh

EXPOSE 80