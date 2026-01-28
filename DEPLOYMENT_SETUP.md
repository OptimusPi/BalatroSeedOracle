# Deployment Setup Guide: Vercel & Cloudflare

## Overview

This guide walks through setting up automated deployments for Balatro Seed Oracle Browser to both **Vercel** (staging) and **Cloudflare Pages** (production).

## Prerequisites

- Vercel account (https://vercel.com)
- Cloudflare account with Pages enabled (https://pages.cloudflare.com)
- Admin access to GitHub repository
- Avalonia UI license key (for WASM builds)

## Setup Steps

### 1. Vercel Configuration (Staging)

#### Create Vercel Project

1. Go to https://vercel.com/dashboard
2. Click "Add New..." → "Project"
3. Select your GitHub repository (`BalatroSeedOracle`)
4. Configure project settings:
   - **Framework Preset**: Other
   - **Build Command**: `dotnet publish src/BalatroSeedOracle.Browser/BalatroSeedOracle.Browser.csproj -c Release -o publish`
   - **Output Directory**: `publish`
   - **Install Command**: (Leave empty)

5. Add environment variables:
   - `DOTNET_VERSION` = `10.0.x`
   - `AVALONIA_LICENSE_KEY` = Your Avalonia UI license key (mark as secret)

#### Add Vercel Secrets to GitHub

In your GitHub repository Settings → Secrets and variables → Actions:

```
VERCEL_TOKEN=<your-vercel-token>
VERCEL_PROJECT_ID=<project-id-from-vercel>
VERCEL_ORG_ID=<org-id-from-vercel>
```

**To get these values:**

- **VERCEL_TOKEN**: Visit https://vercel.com/account/tokens, create a new token
- **VERCEL_PROJECT_ID**: From Vercel dashboard, click your project, copy from URL or settings
- **VERCEL_ORG_ID**: From Vercel account settings

### 2. Cloudflare Pages Setup (Production)

#### Create Cloudflare Pages Project

1. Go to https://dash.cloudflare.com
2. Navigate to Workers & Pages → Pages
3. Click "Create application" → "Connect to Git"
4. Select your GitHub repository
5. Configure build settings:
   - **Production branch**: `main`
   - **Build command**: `dotnet publish src/BalatroSeedOracle.Browser/BalatroSeedOracle.Browser.csproj -c Release -o publish`
   - **Build output directory**: `publish`

6. Add environment variables:
   - `DOTNET_VERSION` = `10.0.x`
   - `AVALONIA_LICENSE_KEY` = Your Avalonia UI license key (mark as secret)

#### Add Cloudflare Secrets to GitHub

In your GitHub repository Settings → Secrets and variables → Actions:

```
CLOUDFLARE_ACCOUNT_ID=<your-account-id>
CLOUDFLARE_API_TOKEN=<api-token>
CLOUDFLARE_ZONE_ID=<zone-id>
```

**To get these values:**

- **CLOUDFLARE_ACCOUNT_ID**: Visit https://dash.cloudflare.com/profile/api-tokens, copy Account ID
- **CLOUDFLARE_API_TOKEN**: Create a new API token with "Cloudflare Pages" permission
- **CLOUDFLARE_ZONE_ID**: From Cloudflare dashboard, select your domain, Zone ID is on right sidebar

### 3. GitHub Workflow Configuration

The deployment workflow is configured in `.github/workflows/deploy-browser.yml`:

- **Automatic Staging Deploy**: Triggers on push to `main` branch
  - Deploys to Vercel
  - Automatically assigns preview URL

- **Manual Production Deploy**: Triggers via workflow dispatch
  - Select `production` environment input
  - Deploys to Cloudflare Pages
  - Goes live on your domain

### 4. Custom Domain Setup

#### For Vercel (Staging)

1. In Vercel dashboard, go to your project
2. Settings → Domains
3. Add your staging domain (e.g., `staging.yourdomain.com`)
4. Update DNS records as prompted

#### For Cloudflare Pages (Production)

1. In Cloudflare dashboard, go to Pages
2. Select your project
3. Settings → Custom domain
4. Add your production domain
5. Cloudflare will automatically route traffic

### 5. Environment Variables

Add Avalonia license key to each platform:

**Vercel:**
- Settings → Environment Variables
- Add `AVALONIA_LICENSE_KEY` as secret

**Cloudflare:**
- Workers & Pages → Project → Settings → Environment variables
- Add `AVALONIA_LICENSE_KEY` as secret

### 6. Testing the Setup

#### Test Vercel Staging Deploy

1. Push a commit to `main` branch
2. GitHub Actions workflow triggers
3. Check deployment status in Actions tab
4. Vercel dashboard shows build status
5. Preview URL generated automatically

#### Test Cloudflare Production Deploy

1. Go to GitHub Actions
2. Run "Deploy Browser Version" workflow
3. Select `production` environment
4. Monitor workflow progress
5. Cloudflare Pages dashboard shows deployment

## Troubleshooting

### Build Failures

- Check build logs in respective dashboards (Vercel/Cloudflare)
- Ensure `DOTNET_VERSION=10.0.x` is set
- Verify Avalonia license key is correct

### Deployment Errors

- Verify GitHub secrets are correctly named
- Confirm API tokens have proper permissions
- Check firewall/security rules on Cloudflare

### Domain Issues

- Allow time for DNS propagation (up to 24 hours)
- Clear browser cache
- Verify DNS records in Cloudflare dashboard

## Performance Optimization

Both Vercel and Cloudflare config files include:

- **Static asset caching** (1 year for `.wasm`, `.js`, `.css`)
- **HTML cache busting** (no-cache for `index.html`)
- **Security headers** (X-Frame-Options, CSP, etc.)
- **SPA routing** (all paths redirect to `index.html`)

## Cost Considerations

**Vercel (Free Tier)**:
- 1 project per free team
- Unlimited deployments
- Auto-scaling included

**Cloudflare Pages (Free Tier)**:
- Unlimited projects
- 500 builds/month
- Free SSL/TLS
- Free CDN globally

## Next Steps

1. Set up custom domains
2. Configure redirects (HTTP → HTTPS)
3. Enable analytics (Vercel/Cloudflare dashboards)
4. Set up alerts for build failures
5. Consider purchasing domain from Cloudflare for integrated DNS
