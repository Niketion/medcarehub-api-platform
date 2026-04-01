# Build stage
FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build:prod

# Runtime stage
FROM nginx:1.27-alpine

COPY nginx/default.conf /etc/nginx/conf.d/default.conf
COPY nginx/99-env-config.sh /docker-entrypoint.d/99-env-config.sh
RUN chmod +x /docker-entrypoint.d/99-env-config.sh

COPY --from=build /app/dist/medcarehub-web/browser/ /usr/share/nginx/html/

EXPOSE 80