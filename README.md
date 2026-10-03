# StackDuel Server

Backend Server for the StackDuel product.

### Getting Started

#### Bruno

This project was built with bruno. It is an alternative to Postman. To get started with this project you need to create a `.env` file in the bruno folder. You can do this by running:

```bash
cp .env.example .env
```

After this you will need to get these values and put it in your `.env` file:

| Variable              | What it needs                            | How to get it                                                                                   |
| ---------------------- | ------------------------------------------ | -------------------------------------------------------------------------------------------------- |
| `CLERK_SECRET_KEY`    | Your Clerk instance's secret key          | Clerk Dashboard → your app → API Keys → Secret key (use a **development** instance key, not live) |
| `CLERK_TEST_USER_ID`  | The Clerk user ID to authenticate as      | Clerk Dashboard → Users → pick/create a test user → copy their User ID (`user_xxx`)                |
| `STAGING_BASE_URL`    | Hostname of the deployed staging API      | From your staging deployment once it exists                                                        |
| `PRODUCTION_BASE_URL` | Hostname of the deployed production API   | From your production deployment once it exists                                                     |

Auth works differently than a standard OAuth2 flow because Clerk doesn't expose a `/oauth/token`-style exchange for first-party apps — it's SDK/session driven instead. To get a bearer token into Bruno, run the two requests in the **Auth** folder before anything else:

1. **`1 - Create Session`** — calls Clerk's Backend API (`POST /sessions`) to create a session directly for `CLERK_TEST_USER_ID`. This endpoint is explicitly testing-only (Clerk disables it on production instances), which is exactly what we want here.
2. **`2 - Create Session Token`** — exchanges that session for a JWT (`POST /sessions/{id}/tokens`), minted with a 1 hour lifetime so it doesn't expire mid-testing-session like Clerk's default ~60s session tokens do.

Both requests auth with `CLERK_SECRET_KEY` and store their results (`session_id`, then `access_token`) as environment variables via post-response scripts. Every other request in the collection inherits `access_token` automatically as its bearer token — run the two Auth requests once per session, then everything else just works.
