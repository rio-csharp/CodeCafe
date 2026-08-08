# Unprivileged nginx: master/worker run as the nginx user (uid 101), no root.
# TODO(supply-chain): pin the base image by digest (nginxinc/nginx-unprivileged:1.29-alpine@sha256:...)
# once the digest can be resolved from a machine with registry access; the
# immutable tag policy is documented in deploy/helm/codecafe/values.yaml.
FROM nginxinc/nginx-unprivileged:1.29-alpine

USER root
COPY clients/web/nginx.conf /etc/nginx/conf.d/default.conf
COPY clients/web/docker/security-headers.conf /etc/nginx/security-headers.conf
COPY clients/web/docker-entrypoint.d /docker-entrypoint.d
COPY .artifacts/frontend/ /usr/share/nginx/html
RUN chmod +x /docker-entrypoint.d/10-write-runtime-config.sh \
 && chown -R 101:0 /usr/share/nginx/html /docker-entrypoint.d
USER 101

EXPOSE 8080
