#!/bin/bash
# Run this on your Ubuntu/Debian server ONCE to set up the deployment environment

set -e

echo "=== PathlyAI Server Setup ==="

# 1. Install .NET 10 SDK
echo "Installing .NET 10..."
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0

# 2. Create deployment directory
echo "Creating deployment directory..."
sudo mkdir -p /opt/pathly-api/publish
sudo chown -R $USER:$USER /opt/pathly-api

# 3. Create www-data user if not exists
id -u www-data &>/dev/null || sudo useradd -r -s /bin/false www-data

# 4. Install systemd service
echo "Installing systemd service..."
sudo cp deploy/pathly-api.service /etc/systemd/system/pathly-api.service
sudo systemctl daemon-reload
sudo systemctl enable pathly-api

# 5. Configure firewall (UFW)
echo "Configuring firewall..."
sudo ufw allow 8080/tcp comment "PathlyAI API"
sudo ufw --force enable

# 6. Install nginx (reverse proxy)
echo "Installing nginx..."
sudo apt-get install -y nginx

# 7. Create nginx config
sudo tee /etc/nginx/sites-available/pathly-api > /dev/null <<'EOF'
server {
    listen 80;
    server_name _;

    location / {
        proxy_pass http://localhost:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
EOF

sudo ln -sf /etc/nginx/sites-available/pathly-api /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t && sudo systemctl restart nginx

echo ""
echo "=== Setup Complete ==="
echo ""
echo "Next steps:"
echo "1. Add these GitHub Secrets (Settings > Secrets > Actions):"
echo "   SSH_HOST        = your.server.ip"
echo "   SSH_USERNAME    = $USER"
echo "   SSH_PRIVATE_KEY = (your private SSH key, NOT the public one)"
echo "   SSH_PORT        = 22"
echo ""
echo "2. Configure app settings on server:"
echo "   sudo mkdir -p /opt/pathly-api/publish"
echo "   Create /opt/pathly-api/publish/appsettings.Production.json with your connection strings and API keys"
echo ""
echo "3. Push to master to trigger first deployment"