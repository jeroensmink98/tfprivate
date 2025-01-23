
# tar module
tar -czvf --exclude='.DS_Store' -f my_module.tgz my_module || true

#!/bin/bash
API_KEY="Cd16694YKkKxvjJC1rzCfe0T/fmQLTajDND3vQ2ic1I="
NAMESPACE="acme"
MODULE_NAME="something"
VERSION="2.0.1"

curl -X 'POST' \
  "https://localhost:443/v1/module/$NAMESPACE/$MODULE_NAME/$VERSION" \
  -H 'accept: */*' \
  -H 'Content-Type: multipart/form-data' \
  -H "X-API-Key: $API_KEY" \
  -F 'file=@my_module.tgz;type=application/x-gzip'


curl -X 'POST' \
  "https://localhost:443/v1/module/new/$MODULE_NAME/$VERSION" \
  -H 'accept: */*' \
  -H 'Content-Type: multipart/form-data' \
  -H "X-API-Key: $API_KEY" \
  -F 'file=@my_module.tgz;type=application/x-gzip'

sleep 2

curl -X 'DELETE' \
  "https://localhost:443/v1/module/$NAMESPACE/$MODULE_NAME/$VERSION" \
  -H 'accept: */*' \
  -H "X-API-Key: $API_KEY"
