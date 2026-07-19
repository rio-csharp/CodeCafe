# Unprivileged nginx: master/worker run as the nginx user (uid 101), no root.
FROM nginxinc/nginx-unprivileged:1.29-alpine

COPY clients/web/nginx.conf /etc/nginx/conf.d/default.conf
COPY clients/web/docker/security-headers.conf /etc/nginx/security-headers.conf
COPY clients/web/docker-entrypoint.d /docker-entrypoint.d
RUN chmod +x /docker-entrypoint.d/10-write-runtime-config.sh
COPY .artifacts/frontend/ /usr/share/nginx/html

EXPOSE 8080
