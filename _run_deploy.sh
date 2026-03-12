#!/bin/bash
set -e
chmod +x /tmp/savedbythemaid-deploy.sh
sudo mv /tmp/savedbythemaid-deploy.sh /opt/savedbythemaid-deploy.sh
sudo chown "$USER":"$USER" /opt/savedbythemaid-deploy.sh
cd /opt
./savedbythemaid-deploy.sh
