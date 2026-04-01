#!/bin/sh
set -e

# Substitute the BFF_URL environment variable into the nginx config template
envsubst '${BFF_URL}' < /etc/nginx/templates/default.conf.template > /etc/nginx/conf.d/default.conf

exec nginx -g 'daemon off;'
