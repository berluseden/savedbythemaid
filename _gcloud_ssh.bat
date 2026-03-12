@echo off
"C:\Users\eberlus\AppData\Local\Google\Cloud SDK\google-cloud-sdk\bin\gcloud.cmd" compute ssh instancia-gratis-ubuntu --zone=us-central1-a --command="bash /tmp/_run_deploy.sh"
