
# tar module


#!/bin/bash
API_KEY="Cd16694YKkKxvjJC1rzCfe0T/fmQLTajDND3vQ2ic1I="
NAMESPACE="acme"
MODULE_NAME="something"
VERSION="2.0.1"
PORT=3000
DOMAIN="modules.devopsfrontier.com"

curl -X 'POST' \
  "http://$DOMAIN:$PORT/v1/module/$NAMESPACE/$MODULE_NAME/$VERSION" \
  -H 'accept: */*' \
  -H 'Content-Type: multipart/form-data' \
  -H "X-API-Key: $API_KEY" \
  -F 'file=@my_module.tgz;type=application/x-gzip'

