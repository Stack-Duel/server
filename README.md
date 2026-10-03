# StackDuel Server

Backend Server for the StackDuel product.

### Getting Started

#### Bruno

This project was built with bruno. It is an alternative to Postman. To get started with this project you need to create a `.env` file in the bruno folder. You can do this by running:

```bash
cp .env.example .env
```

After this you will need to get these values and put it in your `.env` file:

| Variable                   | What it needs                     | How to get it                                                                      |
| --------------------------- | ------------------------------------ | --------------------------------------------------------------------------------------- |
| `CLERK_AUTHORIZATION_URL`  | The OAuth Application's authorize URL | Generated when you create the OAuth Application below (copy from the create response/Dashboard) |
| `CLERK_TOKEN_URL`          | The OAuth Application's token URL     | Same place as above                                                                |
| `CLERK_CLIENT_ID`          | The OAuth Application's client ID     | Same place as above                                                                |
| `CLERK_SCOPE`              | Scopes to request                     | `profile email` (default), or whatever scopes you configured on the application   |
| `STAGING_BASE_URL`         | Hostname of the deployed staging API  | From your staging deployment once it exists                                       |
| `PRODUCTION_BASE_URL`      | Hostname of the deployed production API | From your production deployment once it exists                                   |

Auth happens via Clerk's **OAuth Applications** feature (Clerk acting as its own OAuth2/OIDC provider) using the Authorization Code flow with PKCE. Bruno opens Clerk's real hosted login in a popup, you log in there, and it hands the token back. No stored credentials, no pre-picked test user.

**One-time Clerk setup:**

1. In your Clerk Dashboard, create a new **OAuth Application** (Configure → OAuth Applications, confirm it's available on your plan first).
2. Set it as a **public client** with **PKCE required**. This removes the need for a client secret, same reasoning as Auth0's Native app type.
3. Add `https://oauth.usebruno.com/callback` to **Redirect URIs**. This is Bruno's built-in callback relay, no local server needed.
4. Copy the resulting `authorize_url`, `token_fetch_url`, and `client_id` into `.env`.

Note: OAuth Applications is designed for third-party "Sign in with StackDuel" use cases (other apps authenticating against your Clerk instance), not first-party testing. Using it here for Bruno is a repurposing of the same mechanism, not its primary intended use.
