# CodeCafe MCP Connection Guide

This document covers the current MCP connection model for CodeCafe after the
OpenIddict integration.

## Summary

CodeCafe now uses a real OAuth/OpenID Connect-style flow for MCP instead of a
manually minted development bearer token.

The shape is:

1. Claude Code connects to `https://.../mcp`
2. CodeCafe advertises MCP protected resource metadata
3. Claude opens the browser when authentication is needed
4. the user logs in through the existing React app
5. CodeCafe issues an access token through OpenIddict
6. Claude stores and reuses the token for MCP calls

The user-facing login UI remains in the existing React app. The ASP.NET Core
API owns the OAuth/OpenIddict protocol endpoints and MCP resource-server
behavior.

## What was implemented

The backend now includes:

- OpenIddict server endpoints:
  - `/connect/authorize`
  - `/connect/token`
  - `/connect/register`
- OpenIddict EF Core storage in the main application database
- MCP bearer validation via OpenIddict validation instead of a static signing
  key
- MCP protected resource metadata at:
  - `/.well-known/oauth-protected-resource/mcp`
- optional pre-registered public OAuth clients through:
  - `AuthorizationServer:PublicClients`
- dynamic client registration discovery through OIDC metadata

The frontend now includes:

- login/register pages that preserve `returnUrl`
- redirect back to the original authorization request after successful login

## Local development

### 1. Development configuration

Set these values in
[src/CodeCafe.WebApi/appsettings.Development.json](../src/CodeCafe.WebApi/appsettings.Development.json):

```json
{
  "AuthorizationServer": {
    "Issuer": "https://localhost:7239/",
    "FrontendBaseUrl": "http://localhost:5173",
    "PublicClients": [
      {
        "ClientId": "codecafe-claude",
        "DisplayName": "Claude Code",
        "RedirectUris": [
          "http://localhost/callback",
          "http://127.0.0.1/callback"
        ]
      }
    ]
  },
  "Mcp": {
    "Enabled": true,
    "EndpointPath": "/mcp",
    "ProtectedResourceMetadataPath": "/.well-known/oauth-protected-resource/mcp",
    "RequireAuthorization": true,
    "RequiredAudience": "codecafe-mcp",
    "RequiredReadScopes": [ "notes.read" ],
    "RequiredWriteScopes": [ "notes.write" ]
  }
}
```

Important notes:

- there is no `SigningKey` requirement anymore for the normal MCP flow
- development still uses real authentication
- the OpenIddict server uses development certificates in development/testing
- loopback redirect URIs are normalized without a fixed port so clients can use
  any free local callback port

### 2. Trust the local HTTPS certificate

The OAuth issuer should be HTTPS. On a fresh machine:

```powershell
dotnet dev-certs https --trust
```

### 3. Run backend and frontend

Backend:

```powershell
dotnet restore CodeCafe.slnx
dotnet run --project src/CodeCafe.WebApi
```

Frontend:

```powershell
cd frontend
npm run dev
```

Expected local URLs:

- API: `https://localhost:7239`
- frontend: `http://localhost:5173`
- MCP: `https://localhost:7239/mcp`

### 4. Connect Claude Code

Modern MCP clients should be able to self-register through OAuth discovery, so
the recommended command is just:

```powershell
claude mcp add --transport http codecafe https://localhost:7239/mcp
```

What happens next:

1. Claude tries to use the MCP server
2. CodeCafe challenges the request
3. Claude opens a browser
4. you log in through the React app
5. Claude self-registers as a public native OAuth client if needed
6. Claude completes the code flow and stores the token

### 5. Smoke test

After connection:

```powershell
claude mcp list
```

Then ask Claude to use read tools first:

- `notes_get_notebook`
- `notes_list_items`
- `notes_get_page`

Current Notes tools:

- `notes_get_notebook`
- `notes_search`
- `notes_list_items`
- `notes_get_page`
- `notes_create_page`
- `notes_update_page_content_json`
- `notes_append_blocks_to_page`
- `notes_move_item`
- `notes_reorder_items`
- `notes_archive_item`
- `notes_restore_item`
- `notes_delete_item`

## Production setup

Production needs real OpenIddict signing and encryption credentials, but they
do not need to be mounted as files. The deployment flow reads the PFX material
from GitHub Environment Secrets and recreates the runtime Kubernetes Secret on
deploy.

Set these values in production configuration:

```json
{
  "AuthorizationServer": {
    "Issuer": "https://api.your-domain.com/",
    "FrontendBaseUrl": "https://app.your-domain.com",
    "PublicClients": [
      {
        "ClientId": "codecafe-claude",
        "DisplayName": "Claude Code",
        "RedirectUris": [
          "http://localhost/callback",
          "http://127.0.0.1/callback"
        ]
      }
    ],
    "SigningCertificateBase64": "<base64-pfx>",
    "SigningCertificatePassword": "set-via-secret",
    "EncryptionCertificateBase64": "<base64-pfx>",
    "EncryptionCertificatePassword": "set-via-secret"
  },
  "Mcp": {
    "Enabled": true,
    "RequiredAudience": "codecafe-mcp",
    "AllowedOrigins": [ "https://app.your-domain.com" ]
  }
}
```

Important constraints:

- production must use real certificates, not development certificates
- production can provide them either as file paths or as base64 PFX values
- `AllowedHosts` must not be `*`
- the issuer must be the public HTTPS authority used by clients
- the frontend base URL must point at the deployed React app
- modern MCP clients should be able to self-register through `/connect/register`
- `AuthorizationServer:PublicClients` is now a compatibility/fallback list, not
  the primary connection mechanism

## Why React instead of MVC

CodeCafe already has:

- login and registration pages in the React app
- cookie auth for the browser session
- route control and auth state in the frontend

Adding MVC/Razor just for the OAuth login surface would duplicate the auth UI.
The cleaner split is:

- React app: human-facing login/register UX
- Web API: OAuth/OpenIddict endpoints and MCP protection

## Troubleshooting

### Claude does not open the browser

- verify the MCP URL is HTTPS and reachable
- verify `Mcp:Enabled` is `true`
- verify the client supports standard MCP OAuth discovery and authorization

### `401 Unauthorized` from `/mcp`

- the OAuth flow did not complete
- the token is expired or missing
- the MCP audience does not match `codecafe-mcp`

### Client cannot register automatically

- verify `/.well-known/openid-configuration` exposes `registration_endpoint`
- verify `token_endpoint_auth_methods_supported` includes `none`
- verify the client uses loopback redirect URIs on `localhost`, `127.0.0.1`,
  or `::1`

### Authorization request redirects to login repeatedly

- the API cookie was not set successfully
- the frontend and API origins are misconfigured
- `FrontendBaseUrl` or the frontend API base URL is wrong

### Startup fails in production

- signing or encryption certificate values are missing or invalid
- `AuthorizationServer:Issuer` is missing
- `Mcp:RequiredAudience` is missing

### Local HTTPS problems

- run `dotnet dev-certs https --trust`
- restart Claude Code after trusting the certificate if needed

## Database impact

Startup auto-apply still only applies migrations that already exist in source
control. The OpenIddict tables are included through the checked-in migration,
so a real database can be brought up by normal startup migration execution.
