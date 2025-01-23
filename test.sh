
# tar module
tar -czvf --exclude='.DS_Store' -f my_module.tgz my_module || true

#!/bin/bash
API_KEY="Cd16694YKkKxvjJC1rzCfe0T/fmQLTajDND3vQ2ic1I="
curl -X 'POST' \
  'https://localhost:443/v1/module/acme/something/2.0.0' \
  -H 'accept: */*' \
  -H 'Content-Type: multipart/form-data' \
  -H "X-API-Key: $API_KEY" \
  -F 'file=@my_module.tgz;type=application/x-gzip'


