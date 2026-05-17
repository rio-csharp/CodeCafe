FROM nginx:1.29-alpine

COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY frontend/docker-entrypoint.d /docker-entrypoint.d
RUN chmod +x /docker-entrypoint.d/10-write-runtime-config.sh
COPY .artifacts/frontend/ /usr/share/nginx/html

EXPOSE 8080
