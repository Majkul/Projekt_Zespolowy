#!/usr/bin/env bash
# =============================================================================
# HandlujZTym — VPS hardening & dev/production environment setup
#
# Run once as root on a fresh Ubuntu/Debian VPS:
#   bash setup-vps.sh
#
# What this script does:
#   1. System update
#   2. UFW firewall — allow only 22/SSH, 80/HTTP, 443/HTTPS; deny everything else
#   3. SSH hardening — key-only auth, no root login, brute-force protection
#   4. Fail2ban — SSH + nginx jails
#   5. Unattended security upgrades
#   6. Docker + Docker Compose installation
#   7. Application directory setup
# =============================================================================

set -euo pipefail

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'
info()  { echo -e "${GREEN}[INFO]${NC}  $*"; }
warn()  { echo -e "${YELLOW}[WARN]${NC}  $*"; }
error() { echo -e "${RED}[ERROR]${NC} $*"; exit 1; }

[[ $EUID -ne 0 ]] && error "Run as root: sudo bash setup-vps.sh"

# ─────────────────────────────────────────────────────────────────────────────
# 1. System update
# ─────────────────────────────────────────────────────────────────────────────
info "Updating system packages..."
apt-get update -qq
apt-get upgrade -y -qq
apt-get install -y -qq curl git ufw fail2ban unattended-upgrades apt-listchanges

# ─────────────────────────────────────────────────────────────────────────────
# 2. UFW Firewall
#    - Deny all inbound by default
#    - Allow SSH (rate-limited), HTTP, HTTPS only
#    - Database ports (5432) are NEVER opened externally — Docker internal only
# ─────────────────────────────────────────────────────────────────────────────
info "Configuring UFW firewall..."
ufw --force reset
ufw default deny incoming
ufw default allow outgoing

# SSH — rate-limited (max 6 connections per 30s per IP)
ufw limit 22/tcp comment "SSH rate-limited"

# Web traffic
ufw allow 80/tcp  comment "HTTP"
ufw allow 443/tcp comment "HTTPS"

ufw --force enable
info "UFW status:"
ufw status verbose

# ─────────────────────────────────────────────────────────────────────────────
# 3. SSH hardening
#    - Disable password authentication (key-only)
#    - Disable root login
#    - Reduce MaxAuthTries
#    - Disable X11 forwarding and empty passwords
# ─────────────────────────────────────────────────────────────────────────────
info "Hardening SSH configuration..."
SSHD_CONF="/etc/ssh/sshd_config"
cp "$SSHD_CONF" "${SSHD_CONF}.bak.$(date +%Y%m%d%H%M%S)"

set_sshd() {
    local key="$1" val="$2"
    if grep -qE "^#?\s*${key}" "$SSHD_CONF"; then
        sed -i "s|^#\?\s*${key}.*|${key} ${val}|" "$SSHD_CONF"
    else
        echo "${key} ${val}" >> "$SSHD_CONF"
    fi
}

set_sshd PasswordAuthentication     no
set_sshd PermitRootLogin            no
set_sshd MaxAuthTries               3
set_sshd X11Forwarding              no
set_sshd PermitEmptyPasswords       no
set_sshd ChallengeResponseAuthentication no
set_sshd UsePAM                     yes
set_sshd AllowAgentForwarding       no
set_sshd AllowTcpForwarding         no
set_sshd LoginGraceTime             30

warn "Password SSH login is now DISABLED. Make sure your SSH public key is in ~/.ssh/authorized_keys before reloading SSH."
read -rp "Press ENTER to reload SSH (or Ctrl+C to abort): "
systemctl reload sshd
info "SSH reloaded."

# ─────────────────────────────────────────────────────────────────────────────
# 4. Fail2ban
#    Copies jail config from this repo, then enables service
# ─────────────────────────────────────────────────────────────────────────────
info "Configuring Fail2ban..."
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cp "${SCRIPT_DIR}/fail2ban/jail.local" /etc/fail2ban/jail.local
cp "${SCRIPT_DIR}/fail2ban/filter.d/nginx-req-limit.conf" /etc/fail2ban/filter.d/nginx-req-limit.conf

systemctl enable fail2ban
systemctl restart fail2ban
info "Fail2ban status:"
fail2ban-client status

# ─────────────────────────────────────────────────────────────────────────────
# 5. Unattended security upgrades
# ─────────────────────────────────────────────────────────────────────────────
info "Enabling unattended security upgrades..."
cat > /etc/apt/apt.conf.d/20auto-upgrades <<'EOF'
APT::Periodic::Update-Package-Lists "1";
APT::Periodic::Download-Upgradeable-Packages "1";
APT::Periodic::AutocleanInterval "7";
APT::Periodic::Unattended-Upgrade "1";
EOF

cat > /etc/apt/apt.conf.d/50unattended-upgrades <<'EOF'
Unattended-Upgrade::Allowed-Origins {
    "${distro_id}:${distro_codename}-security";
    "${distro_id}ESMApps:${distro_codename}-apps-security";
    "${distro_id}ESM:${distro_codename}-infra-security";
};
Unattended-Upgrade::AutoFixInterruptedDpkg "true";
Unattended-Upgrade::MinimalSteps "true";
Unattended-Upgrade::Remove-Unused-Dependencies "true";
Unattended-Upgrade::Automatic-Reboot "false";
EOF

# ─────────────────────────────────────────────────────────────────────────────
# 6. Docker + Docker Compose
# ─────────────────────────────────────────────────────────────────────────────
if command -v docker &>/dev/null; then
    info "Docker already installed: $(docker --version)"
else
    info "Installing Docker..."
    curl -fsSL https://get.docker.com | sh
    systemctl enable docker
    systemctl start docker
    info "Docker installed: $(docker --version)"
fi

if docker compose version &>/dev/null; then
    info "Docker Compose already available: $(docker compose version)"
else
    warn "Docker Compose plugin not found. Install Docker Desktop or the compose plugin manually."
fi

# ─────────────────────────────────────────────────────────────────────────────
# 7. Application directory
# ─────────────────────────────────────────────────────────────────────────────
APP_DIR="/opt/handlujztym"
info "Creating application directory at ${APP_DIR}..."
mkdir -p "${APP_DIR}"
chmod 750 "${APP_DIR}"

# ─────────────────────────────────────────────────────────────────────────────
# Done
# ─────────────────────────────────────────────────────────────────────────────
echo ""
echo -e "${GREEN}╔══════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║  VPS hardening complete — next steps:                    ║${NC}"
echo -e "${GREEN}╠══════════════════════════════════════════════════════════╣${NC}"
echo -e "${GREEN}║  1. Clone repo into ${APP_DIR}                   ║${NC}"
echo -e "${GREEN}║  2. Copy docker/.env.example → docker/.env              ║${NC}"
echo -e "${GREEN}║     Fill in all values (DB, PayU, email)                 ║${NC}"
echo -e "${GREEN}║  3. Place TLS certs in docker/nginx/certs/               ║${NC}"
echo -e "${GREEN}║     (fullchain.pem + privkey.pem)                        ║${NC}"
echo -e "${GREEN}║  4. cd docker && docker compose up -d                    ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════════════════════════╝${NC}"
echo ""
warn "Firewall rules: only ports 22, 80, 443 are open."
warn "PostgreSQL (5432) and pgAdmin (8080) are accessible only via localhost."
