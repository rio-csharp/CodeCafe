FROM nginx:1.29-alpine
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY frontend/docker-entrypoint.d /docker-entrypoint.d
COPY .artifacts/frontend/ /usr/share/nginx/html
EXPOSE 8080
