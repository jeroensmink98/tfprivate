#!/bin/bash
API_KEY="Cd16694YKkKxvjJC1rzCfe0T/fmQLTajDND3vQ2ic1I="
curl -X 'POST' \
  'https://localhost:443/v1/module/tres/something/1.0.4' \
  -H 'accept: */*' \
  -H 'Content-Type: multipart/form-data' \
  -H "X-API-Key: $API_KEY" \
  -F 'file=@module.tgz;type=application/x-gzip'


